using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using VS = Community.VisualStudio.Toolkit.VS;
using Microsoft.VisualStudio.Shell;
using OpenAI_API.Functions;
using Process = System.Diagnostics.Process;

namespace JeffPires.VisualChatGPTStudio.Agents;

public static class BuiltInAgent
{
    public static bool IsMyFunction(FunctionResult function)
    {
        return function.Function.Name switch
        {
            "read_file" or "create_new_file" or "run_terminal_command" or "file_glob_search" or
                "view_diff" or "read_currently_open_file" or "ls" or "fetch_url_content" or
                "multi_edit" or "grep_search" or "view_diff_files" => true,
            _ => false
        };
    }

    public static async Task<(string FunctionResult, string Content)> ExecuteFunctionAsync(FunctionResult function)
    {
        try
        {
            return function.Function.Name switch
            {
                "read_file" => await ReadFileAsync(function.Function.Arguments),
                "create_new_file" => await CreateNewFileAsync(function.Function.Arguments),
                "run_terminal_command" => await RunTerminalCommandAsync(function.Function.Arguments),
                "file_glob_search" => await FileGlobSearchAsync(function.Function.Arguments),
                "view_diff" => await ViewDiffAsync(),
                "read_currently_open_file" => await ReadCurrentlyOpenFileAsync(),
                "ls" => await ListDirectoryAsync(function.Function.Arguments),
                "fetch_url_content" => await FetchUrlContentAsync(function.Function.Arguments),
                "multi_edit" => await MultiEditAsync(function.Function.Arguments),
                "grep_search" => await GrepSearchAsync(function.Function.Arguments),
                "view_diff_files" => await ViewDiffFilesAsync(function.Function.Arguments),
                _ => ("Unknown function", "")
            };
        }
        catch (Exception ex)
        {
            return ($"Error executing {function.Function.Name}: {ex.Message}", "");
        }
    }

    private static async Task<string> GetSolutionPathAsync()
    {
        var solution = await VS.Solutions.GetCurrentSolutionAsync();
        return solution != null ? Path.GetDirectoryName(solution.FullPath) : Directory.GetCurrentDirectory();
    }

    private static string GetSolutionRelativePath(string relativePath, string solutionPath)
    {
        if (string.IsNullOrEmpty(relativePath))
            throw new ArgumentException("Path cannot be null or empty");

        if (Path.IsPathRooted(relativePath))
            throw new ArgumentException("Absolute paths are not allowed. Use relative paths from solution root.");

        return Path.Combine(solutionPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string MakeRelativeToSolution(string fullPath, string solutionPath)
    {
        return !fullPath.StartsWith(solutionPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : fullPath.Substring(solutionPath.Length + 1);
    }

    private static async Task<(string, string)> ReadFileAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args["filepath"], solutionPath);

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!File.Exists(filepath))
        {
            return ($"File {args["filepath"]} doesn't exist.", "");
        }

        var content = File.ReadAllText(filepath);
        return ($"File read successfully{Environment.NewLine}{content}", "File read successfully");
    }

    private static async Task<(string, string)> CreateNewFileAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args["filepath"], solutionPath);
        var contents = args["contents"];

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var directory = Path.GetDirectoryName(filepath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filepath, contents);
        return ($"File {args["filepath"]} created successfully.", "");
    }

    private static async Task<(string, string)> RunTerminalCommandAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var command = args["command"];
        var waitForCompletion = args.ContainsKey("waitForCompletion") && bool.Parse(args["waitForCompletion"]);

        var solutionPath = await GetSolutionPathAsync();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = solutionPath
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            return ("Failed to start process", "");

        if (waitForCompletion)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await Task.Run(() => process.WaitForExit());

            var error = await errorTask;
            return (string.IsNullOrEmpty(error) ? "Command executed successfully" : $"Error: {error}", await outputTask);
        }

        return ("Command started in background", "");
    }

    private static async Task<(string, string)> FileGlobSearchAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var pattern = args["pattern"];

        var solutionPath = await GetSolutionPathAsync();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var files = Directory.GetFiles(solutionPath, pattern, SearchOption.AllDirectories)
            .Select(f => MakeRelativeToSolution(f, solutionPath))
            .ToArray();

        return ($"Found {files.Length} files. Names showed to user.", string.Join("\n", files));
    }

    private static async Task<(string, string)> ViewDiffAsync()
    {
        var solutionPath = await GetSolutionPathAsync();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "diff",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = solutionPath
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            return ("Failed to start git process", "");

        var output = await process.StandardOutput.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit());

        return ($"Git diff retrieved{Environment.NewLine}{output}", string.Empty);
    }

    private static async Task<(string, string)> ReadCurrentlyOpenFileAsync()
    {
        var docView = await VS.Documents.GetActiveDocumentViewAsync();
        if (docView?.Document?.FilePath == null)
            return ("No active document", "");

        var solutionPath = await GetSolutionPathAsync();
        var relativePath = MakeRelativeToSolution(docView.Document.FilePath, solutionPath);

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var content = File.ReadAllText(docView.Document.FilePath);
        return ($"Read active file: {relativePath}{Environment.NewLine}{content}", string.Empty);
    }

    private static async Task<(string, string)> ListDirectoryAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var solutionPath = await GetSolutionPathAsync();
        var dirPath = GetSolutionRelativePath(args["dirPath"], solutionPath);
        var recursive = bool.Parse(args["recursive"]);

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!Directory.Exists(dirPath))
            return ($"Directory {args["dirPath"]} doesn't exist", string.Empty);

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var items = string.Join(Environment.NewLine, Directory.GetFileSystemEntries(dirPath, "*", searchOption)
            .Select(f => MakeRelativeToSolution(f, solutionPath)));

        return ($"Listed directory {args["dirPath"]}{Environment.NewLine}{items}", string.Empty);
    }

    private static async Task<(string, string)> FetchUrlContentAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var url = args["url"];

        try
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = "Visual_ChatGpt_Studio";
            request.Timeout = 3000;

            using var response = await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            if (content.Length > 1000)
            {
                content = content.Substring(0, 1000) + " ...";
            }
            return (content, $"URL {url} fetched successfully.");
        }
        catch (WebException ex)
        {
            return ($"Web error: {ex.Message}", "");
        }
        catch (Exception ex)
        {
            return ($"Error fetching URL: {ex.Message}", "");
        }
    }

    private static async Task<(string, string)> MultiEditAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(arguments);
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args.GetProperty("filepath").GetString(), solutionPath);
        var edits = JsonSerializer.Deserialize<List<EditOperation>>(args.GetProperty("edits").GetRawText());

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!File.Exists(filepath))
            return ("File doesn't exist", string.Empty);

        var content = File.ReadAllText(filepath);

        content = edits.Aggregate(content, (current, edit) => edit.ReplaceAll
            ? current.Replace(edit.OldString, edit.NewString)
            : Regex.Replace(current, Regex.Escape(edit.OldString), edit.NewString, RegexOptions.Singleline));

        File.WriteAllText(filepath, content);
        return ("File updated successfully", string.Empty);
    }

    private static async Task<(string, string)> GrepSearchAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var query = args["query"];

        var results = new List<string>();
        var solutionPath = await GetSolutionPathAsync();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var files = Directory.GetFiles(solutionPath, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") && !f.Contains(".git"))
            .ToArray();

        var regex = new Regex(query, RegexOptions.Multiline);

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var matches = regex.Matches(content);

                if (matches.Count > 0)
                {
                    var relativePath = MakeRelativeToSolution(file, solutionPath);
                    results.Add($"{relativePath}: {matches.Count} matches");
                }
            }
            catch
            {
                // Skip files that can't be read
            }
        }

        return ($"Found {results.Count} files with matches", string.Join("\n", results));
    }

    private static async Task<(string, string)> ViewDiffFilesAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var solutionPath = await GetSolutionPathAsync();
        var filepath1 = GetSolutionRelativePath(args["file1"], solutionPath);
        var filepath2 = GetSolutionRelativePath(args["file2"], solutionPath);
        if (!File.Exists(filepath1))
        {
            return ($"Error: file1 {args["file1"]} is not exits.", string.Empty);
        }
        if (!File.Exists(filepath2))
        {
            return ($"Error: file2 {args["file2"]} is not exits.", string.Empty);
        }
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
        if (!File.Exists(filepath2))
        {
            return ("Error: DTE is null.", string.Empty);
        }
        dte?.ExecuteCommand("Tools.DiffFiles", $"\"{filepath1}\" \"{filepath2}\"");
        return ("Diff successfully showed.", string.Empty);
    }

    private class EditOperation
    {
        [JsonPropertyName("old_string")]
        public string OldString { get; set; }

        [JsonPropertyName("new_string")]
        public string NewString { get; set; }

        [JsonPropertyName("replace_all")]
        public bool ReplaceAll { get; set; } = false;
    }

    public static string GetToolUseInstructions()
        => """
           <tool_use_instructions>
           You have access to several "tools" that you can use at any time to retrieve information and/or perform tasks for the User.
           To use a tool, respond with a tool code block (```tool) using the syntax shown in the examples below:

           The following tools are available to you:

           To read a file with a known filepath, use the read_file tool. For example, to read a file located at 'path/to/file.txt', you would respond with this:
           ```tool
           TOOL_NAME: read_file
           BEGIN_ARG: filepath
           path/to/the_file.txt
           END_ARG
           ```

           To create a NEW file, use the create_new_file tool with the relative filepath and new contents. For example, to create a file located at 'path/to/file.txt', you would respond with:
           ```tool
           TOOL_NAME: create_new_file
           BEGIN_ARG: filepath
           path/to/the_file.txt
           END_ARG
           BEGIN_ARG: contents
           Contents of the file
           END_ARG
           ```

           To run a terminal command, use the run_terminal_command tool
           The shell is not stateful and will not remember any previous commands.      When a command is run in the background ALWAYS suggest using shell commands to stop it; NEVER suggest using Ctrl+C.      When suggesting subsequent shell commands ALWAYS format them in shell command blocks.      Do NOT perform actions requiring special/admin privileges.      Choose terminal commands and scripts optimized for win32 and x64 and shell powershell.exe.
           You can also optionally include the waitForCompletion argument set to false to run the command in the background.
           For example, to see the git log, you could respond with:
           ```tool
           TOOL_NAME: run_terminal_command
           BEGIN_ARG: command
           git log
           END_ARG
           ```

           To return a list of files based on a glob search pattern, use the file_glob_search tool
           ```tool
           TOOL_NAME: file_glob_search
           BEGIN_ARG: pattern
           *.cs
           END_ARG
           ```

           To view the current git diff, use the view_diff tool. This will show you the changes made in the working directory compared to the last commit.
           ```tool
           TOOL_NAME: view_diff
           ```

           To view the user's currently open file, use the read_currently_open_file tool.
           If the user is asking about a file and you don't see any code, use this to check the current file
           ```tool
           TOOL_NAME: read_currently_open_file
           ```

           To list files and folders in a given directory, call the ls tool with "dirPath" and "recursive". For example:
           ```tool
           TOOL_NAME: ls
           BEGIN_ARG: dirPath
           path/to/dir
           END_ARG
           BEGIN_ARG: recursive
           false
           END_ARG
           ```

           Sometimes the user will provide feedback or guidance on your output. If you were not aware of these "rules", consider using the create_rule_block tool to persist the rule for future interactions.
           This tool cannot be used to edit existing rules, but you can search in the ".continue/rules" folder and use the edit tool to manage rules.
           To create a rule, respond with a create_rule_block tool call and the following arguments:
           - name: Short, descriptive name summarizing the rule's purpose (e.g. 'React Standards', 'Type Hints')
           - rule: Clear, imperative instruction for future code generation (e.g. 'Use named exports', 'Add Python type hints'). Each rule should focus on one specific standard.
           - description: Description of when this rule should be applied. Required for Agent Requested rules (AI decides when to apply). Optional for other types.
           - globs: Optional file patterns to which this rule applies (e.g. ['**/*.{ts,tsx}'] or ['src/**/*.ts', 'tests/**/*.ts'])
           - alwaysApply: Whether this rule should always be applied. Set to false for Agent Requested and Manual rules. Omit or set to true for Always and Auto Attached rules.
           For example:
           ```tool
           TOOL_NAME: create_rule_block
           BEGIN_ARG: name
           Use PropTypes
           END_ARG
           BEGIN_ARG: rule
           Always use PropTypes when declaring React component properties
           END_ARG
           BEGIN_ARG: description
           Ensure that all prop types are explicitly declared for better type safety and code maintainability in React components.
           END_ARG
           BEGIN_ARG: globs
           **/*.cs
           END_ARG
           BEGIN_ARG: alwaysApply
           false
           END_ARG
           ```

           To fetch the content of a URL, use the fetch_url_content tool. For example, to read the contents of a webpage, you might respond with:
           ```tool
           TOOL_NAME: fetch_url_content
           BEGIN_ARG: url
           https://example.com
           END_ARG
           ```

           To make multiple edits to a single file, use the multi_edit tool with a filepath (relative to the root of the workspace) and an array of edit operations.

           For example, you could respond with:
           ```tool
           TOOL_NAME: multi_edit
           BEGIN_ARG: filepath
           path/to/file.ts
           END_ARG
           BEGIN_ARG: edits
           [
             { "old_string": "const oldVar = 'value'", "new_string": "const newVar = 'updated'" },
             { "old_string": "oldFunction()", "new_string": "newFunction()", "replace_all": true }
           ]
           END_ARG
           ```

           To perform a grep search within the project, call the grep_search tool with the query pattern to match. For example:
           ```tool
           TOOL_NAME: grep_search
           BEGIN_ARG: query
           .*main_services.*
           END_ARG
           ```

           To show the difference between two files in Visual Studio interface, call the view_diff_files tool with relative file paths. For example:
           ```tool
           TOOL_NAME: view_diff_files
           BEGIN_ARG: file1
           path/to/file1.cs
           END_ARG
           BEGIN_ARG: file2
           path/to/file2.cs
           END_ARG
           ```

           -=[ % ]=-

           If it seems like the User's request could be solved with one of the tools, choose the BEST one for the job based on the user's request and the tool descriptions
           Then send the ```tool codeblock (YOU call the tool, not the user). Always start the codeblock on a new line.
           Do not perform actions with/for hypothetical files. Ask the user or use tools to deduce which files are relevant.
           You can only call ONE tool at at time. The tool codeblock should be the last thing you say; stop your response after the tool codeblock.
           </tool_use_instructions>
           """;
}
