using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using VS = Community.VisualStudio.Toolkit.VS;
using Process = System.Diagnostics.Process;
using Property = OpenAI_API.Functions.Property;

namespace JeffPires.VisualChatGPTStudio.Agents;

public static class BuiltInAgent
{
    public static readonly IReadOnlyList<Tool> Tools =
    [
        new()
        {
            Name = "read_file",
            Description = "To read a file with a known filepath, use the read_file tool.",
            ExampleToSystemMessage = """
                                     For example, to read a file located at 'path/to/file.txt', you would respond with this:
                                     ```tool
                                     TOOL_NAME: read_file
                                     BEGIN_ARG: filepath
                                     path/to/the_file.txt
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            Approval = ApprovalKind.AutoApprove,
            ExecuteAsync = ReadFileAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "filepath", new Property { Types = ["string"], Description = "The relative path/to/file.txt" } }
            }
        },
        new()
        {
            Name = "create_new_file",
            Description = "To create a NEW file, use the create_new_file tool with the relative filepath and new contents.",
            ExampleToSystemMessage = """
                                     For example, to create a file located at 'path/to/file.txt', you would respond with:
                                     ```tool
                                     TOOL_NAME: create_new_file
                                     BEGIN_ARG: filepath
                                     path/to/file.txt
                                     END_ARG
                                     BEGIN_ARG: contents
                                     Contents of the file
                                     END_ARG
                                     ```
                                     """,
            ExecuteAsync = CreateNewFileAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "filepath", new Property { Types = ["string"], Description = "The relative path/to/file.txt" } },
                { "contents", new Property { Types = ["string"], Description = "Contents of the file" } },
            }
        },
        new()
        {
            Name = "run_terminal_command",
            Description = """
                          To run a terminal command, use the run_terminal_command tool
                          The shell is not stateful and will not remember any previous commands.
                          When a command is run in the background ALWAYS suggest using shell commands to stop it; NEVER suggest using Ctrl+C.
                          When suggesting subsequent shell commands ALWAYS format them in shell command blocks.
                          Do NOT perform actions requiring special/admin privileges.
                          Choose terminal commands and scripts optimized for win32 and x64 and shell powershell.exe.
                          You can also optionally include the waitForCompletion argument set to false to run the command in the background, without output message.
                          """,
            ExampleToSystemMessage = """
                                     For example, to see the git log, you could respond with:
                                     ```tool
                                     TOOL_NAME: run_terminal_command
                                     BEGIN_ARG: command
                                     git log
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.High,
            ExecuteAsync = RunTerminalCommandAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "command", new Property { Types = ["string"], Description = "The powershell command" } }
            }
        },
        new()
        {
            Name = "file_glob_search",
            Description = "To return a list of files based on a glob search pattern, use the file_glob_search tool",
            ExampleToSystemMessage = """
                                     ```tool
                                     TOOL_NAME: file_glob_search
                                     BEGIN_ARG: pattern
                                     *.cs
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = FileGlobSearchAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "pattern", new Property { Types = ["string"], Description = "The search pattern" } }
            }
        },
        new()
        {
            Name = "view_diff",
            Description = "To view the current git diff, use the view_diff tool. This will show you the changes made in the working directory compared to the last commit.",
            ExampleToSystemMessage = """
                                     ```tool
                                     TOOL_NAME: view_diff
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ViewDiffAsync
        },
        new()
        {
            Name = "read_currently_open_file",
            Description = """
                          To view the user's currently open file, use the read_currently_open_file tool.
                          If the user is asking about a file and you don't see any code, use this to check the current file
                          """,
            ExampleToSystemMessage = """
                                     ```tool
                                     TOOL_NAME: read_currently_open_file
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ReadCurrentlyOpenFileAsync
        },
        new()
        {
            Name = "ls",
            Description = "To list files and folders in a given directory, call the ls tool with \"dirPath\" and \"recursive\".",
            ExampleToSystemMessage = """
                                     For example:
                                     ```tool
                                     TOOL_NAME: ls
                                     BEGIN_ARG: dirPath
                                     path/to/dir
                                     END_ARG
                                     BEGIN_ARG: recursive
                                     false
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ListDirectoryAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "dirPath", new Property { Types = ["string"], Description = "The directory path" } },
                { "recursive", new Property { Types = ["bool"], Description = "Use recursive search" } }
            }
        },
        new()
        {
            Name = "fetch_url_content",
            Description = "To fetch the content of a URL, use the fetch_url_content tool.",
            ExampleToSystemMessage = """
                                     For example, to read the contents of a webpage, you might respond with:
                                     ```tool
                                     TOOL_NAME: fetch_url_content
                                     BEGIN_ARG: url
                                     https://example.com
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = FetchUrlContentAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "url", new Property { Types = ["string"], Description = "https://example.com" } }
            }
        },
        new()
        {
            Name = "multi_edit",
            Description =
                "To make multiple edits to a single file, use the multi_edit tool with a filepath (relative to the root of the workspace) and an array of edit operations.",
            ExampleToSystemMessage = """
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
                                     """,
            RiskLevel = RiskLevel.Medium,
            ExecuteAsync = MultiEditAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "filepath", new Property { Types = ["string"], Description = "The relative path/to/file.txt" } },
                { "edits", new Property { Types = ["array"], Description = "Array of serialized objects. { \"old_string\": \"const oldVar = 'value'\", \"new_string\": \"const newVar = 'updated'\" }" } }
            }
        },
        new()
        {
            Name = "grep_search",
            Description = "To perform a grep search within the project, call the grep_search tool with the query pattern to match.",
            ExampleToSystemMessage = """
                                     For example:
                                     ```tool
                                     TOOL_NAME: grep_search
                                     BEGIN_ARG: query
                                     .*main_services.*
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = GrepSearchAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "query", new Property { Types = ["string"], Description = "The query pattern to match. Example: .*main_services.*" } }
            }
        },
        new()
        {
            Name = "view_diff_files",
            Description = "To show the difference between two files in Visual Studio interface, call the view_diff_files tool with relative file paths.",
            ExampleToSystemMessage = """
                                     For example:
                                     ```tool
                                     TOOL_NAME: view_diff_files
                                     BEGIN_ARG: file1
                                     path/to/file1.cs
                                     END_ARG
                                     BEGIN_ARG: file2
                                     path/to/file2.cs
                                     END_ARG
                                     ```
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ViewDiffFilesAsync,
            Parameters = new Dictionary<string, Property>
            {
                { "file1", new Property { Types = ["string"], Description = "The relative path/to/file1.txt" } },
                { "file2", new Property { Types = ["string"], Description = "The relative path/to/file2.txt" } },
            }
        }
    ];

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

    private static async Task<ToolResult> ReadFileAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args.GetString("filepath"), solutionPath);

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!File.Exists(filepath))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"File {args.GetString("filepath")} doesn't exist.",
                ErrorMessage = $"File {args.GetString("filepath")} doesn't exist."
            };
        }

        try
        {
            var content = File.ReadAllText(filepath);
            return new ToolResult
            {
                Result = content,
                PrivateResult = "File read successfully"
            };
        }
        catch (Exception e)
        {
            Logger.Log(e);
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"Error {e.Message}",
                ErrorMessage = e.Message
            };
        }
    }

    private static async Task<ToolResult> CreateNewFileAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args.GetString("filepath"), solutionPath);
        var contents = args.GetString("contents");

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        try
        {
            var directory = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filepath, contents);
            return new ToolResult
            {
                Result = $"File {args.GetString("filepath")} created successfully."
            };
        }
        catch (Exception e)
        {
            Logger.Log(e);
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"Error {e.Message}",
                ErrorMessage = e.Message
            };
        }
    }

    private static async Task<ToolResult> RunTerminalCommandAsync(IReadOnlyDictionary<string, object> args)
    {
        var command = args.GetString("command");
        var waitForCompletion = !args.ContainsKey("waitForCompletion") || args.GetBool("waitForCompletion");

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
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Failed to start process"
            };

        if (waitForCompletion)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await Task.Run(() => process.WaitForExit());

            var error = await errorTask;
            var output = await outputTask;
            var isSuccess = string.IsNullOrEmpty(error);
            return new ToolResult
            {
                IsSuccess = isSuccess,
                Result = isSuccess ? $"Command executed successfully: {output}" : $"Error: {error}",
                ErrorMessage = !isSuccess ? error : string.Empty,
                PrivateResult = output
            };
        }

        return new ToolResult
        {
            Result = "Command started in background"
        };
    }

    private static async Task<ToolResult> FileGlobSearchAsync(IReadOnlyDictionary<string, object> args)
    {
        var pattern = args.GetString("pattern");
        if (string.IsNullOrEmpty(pattern))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "pattern should be not null"
            };
        }

        var solutionPath = await GetSolutionPathAsync();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var files = Directory.GetFiles(solutionPath, pattern, SearchOption.AllDirectories)
            .Select(f => MakeRelativeToSolution(f, solutionPath))
            .ToArray();

        return new ToolResult
        {
            Result = $"Found {files.Length} files. Names showed to user.",
            PrivateResult = string.Join(", ", files)
        };
    }

    private static async Task<ToolResult> ViewDiffAsync(IReadOnlyDictionary<string, object> args)
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
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Failed to start git process"
            };

        var output = await process.StandardOutput.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit());

        return new ToolResult
        {
            Result = $"Git diff retrieved{Environment.NewLine}{output}"
        };
    }

    private static async Task<ToolResult> ReadCurrentlyOpenFileAsync(IReadOnlyDictionary<string, object> args)
    {
        var docView = await VS.Documents.GetActiveDocumentViewAsync();
        if (docView?.Document?.FilePath == null)
            return new ToolResult
            {
                IsSuccess = false,
                Result = "No active document"
            };

        var solutionPath = await GetSolutionPathAsync();
        var relativePath = MakeRelativeToSolution(docView.Document.FilePath, solutionPath);

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var content = File.ReadAllText(docView.Document.FilePath);
        return new ToolResult
        {
            Result = $"Read active file: {relativePath}{Environment.NewLine}{content}"
        };
    }

    private static async Task<ToolResult> ListDirectoryAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var dirPath = GetSolutionRelativePath(args.GetString("dirPath"), solutionPath);
        var recursive = args.GetBool("recursive");

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!Directory.Exists(dirPath))
            return new ToolResult
            {
                Result = $"Directory {args.GetString("dirPath")} doesn't exist",
            };

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var items = string.Join(Environment.NewLine, Directory.GetFileSystemEntries(dirPath, "*", searchOption)
            .Select(f => MakeRelativeToSolution(f, solutionPath)));

        return new ToolResult
        {
            Result = $"Listed directory {args.GetString("dirPath")}{Environment.NewLine}{items}"
        };
    }

    private static async Task<ToolResult> FetchUrlContentAsync(IReadOnlyDictionary<string, object> args)
    {
        var url = args.GetString("url");

        try
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = "Visual_ChatGpt_Studio";
            request.Timeout = 3000;


            using var response = await request.GetResponseAsync();
            using var stream = response.GetResponseStream();

            var buffer = new char[1000];
            using var reader = new StreamReader(stream);
            var read = await reader.ReadAsync(buffer, 0, buffer.Length);
            var content = new string(buffer, 0, read);
            if (content.Length >= 1000)
            {
                content = content.Substring(0, 1000) + " ...";
            }

            return new ToolResult
            {
                Result = content,
                PrivateResult = $"URL {url} fetched successfully."
            };
        }
        catch (WebException ex)
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"Web error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"Error fetching URL: {ex.Message}"
            };
        }
    }

    private static async Task<ToolResult> MultiEditAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetSolutionRelativePath(args.GetString("filepath"), solutionPath);
        var edits = args.GetObject<List<EditOperation>>("edits");

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!File.Exists(filepath))
            return new ToolResult
            {
                IsSuccess = false,
                Result = "File doesn't exist."
            };
        if (edits == null || edits.Count == 0)
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Parameter 'edits' is invalid."
            };

        var content = File.ReadAllText(filepath);

        content = edits.Aggregate(content, (current, edit) => edit.ReplaceAll
            ? current.Replace(edit.OldString, edit.NewString)
            : Regex.Replace(current, Regex.Escape(edit.OldString), edit.NewString, RegexOptions.Singleline));

        File.WriteAllText(filepath, content);
        return new ToolResult
        {
            Result = "File updated successfully."
        };
    }

    private static async Task<ToolResult> GrepSearchAsync(IReadOnlyDictionary<string, object> args)
    {
        var query = args.GetString("query");
        if (string.IsNullOrEmpty(query))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Parameter 'query' is invalid."
            };
        }

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

        return new ToolResult
        {
            Result = $"Found {results.Count} files with matches. List of files are showed to user.",
            PrivateResult = string.Join("\n", results)
        };
    }

    private static async Task<ToolResult> ViewDiffFilesAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath1 = GetSolutionRelativePath(args.GetString("file1"), solutionPath);
        var filepath2 = GetSolutionRelativePath(args.GetString("file2"), solutionPath);
        if (!File.Exists(filepath1))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "File 'file1' not found."
            };
        }

        if (!File.Exists(filepath2))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "File 'file2' not found."
            };
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
        if (!File.Exists(filepath2))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Error: DTE is null."
            };
        }

        dte?.ExecuteCommand("Tools.DiffFiles", $"\"{filepath1}\" \"{filepath2}\"");
        return new ToolResult
        {
            Result = "Diff successfully showed."
        };
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
}
