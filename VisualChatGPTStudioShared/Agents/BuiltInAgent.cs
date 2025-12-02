using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using JeffPires.VisualChatGPTStudio.Utils;
using OpenAI_API.Functions;
using Shell = Microsoft.VisualStudio.Shell;
using Toolkit = Community.VisualStudio.Toolkit;
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
            Name = "read_files",
            Description = "Request to read the contents of one or more files. The tool outputs line-numbered content (e.g. \"1 | const x = 1\")",
            ExampleToSystemMessage = """
                                     For example, to read 2 files, you would respond with this:
                                     <|tool_call_begin|> functions.read_files:1 <|tool_call_argument_begin|> {"files": [ \"path/to/the_file1.txt\", \"path/to/the_file2.txt\" ]} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            Approval = ApprovalKind.AutoApprove,
            ExecuteAsync = ReadFileAsync,
            Properties = new Dictionary<string, Property>
            {
                { "files", new Property
                    {
                        Types = ["array"],
                        Description = "Array of the relative path/to/file.txt, path/to/the_file2.txt",
                        Items = new Parameter { Type = "string" }
                    }
                }
            }
        },
        new()
        {
            Name = "create_new_file",
            Description = "To create a NEW file, use the create_new_file tool with the relative filepath and new contents.",
            ExampleToSystemMessage = """
                                     For example, to create a file located at 'path/to/file.txt', you would respond with:
                                     <|tool_call_begin|> functions.create_new_file:1 <|tool_call_argument_begin|> {"filepath": "path/to/file.txt", "contents": "Contents of the file"} <|tool_call_end|>
                                     """,
            ExecuteAsync = CreateNewFileAsync,
            Properties = new Dictionary<string, Property>
            {
                { "filepath", new Property { Types = ["string"], Description = "The relative path/to/file.txt" } },
                { "contents", new Property { Types = ["string"], Description = "Contents of the file" } },
            }
        },
        new()
        {
            Name = "run_terminal_command",
            Description = """
                          To run a terminal command, use the run_terminal_command tool in
                          The shell is not stateful and will not remember any previous commands.
                          When a command is run in the background ALWAYS suggest using shell commands to stop it; NEVER suggest using Ctrl+C.
                          When suggesting subsequent shell commands ALWAYS format them in shell command blocks.
                          Do NOT perform actions requiring special/admin privileges.
                          Choose terminal commands and scripts optimized for win32 and x64 and shell powershell.exe.
                          You can also optionally include the waitForCompletion argument set to false to run the command in the background, without output message.
                          """,
            ExampleToSystemMessage = """
                                     For example, to see the git log, you could respond with:
                                     <|tool_call_begin|> functions.run_terminal_command:1 <|tool_call_argument_begin|> {"exe": "dotnet", "command": "restore"} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.High,
            ExecuteAsync = RunTerminalCommandAsync,
            Properties = new Dictionary<string, Property>
            {
                { "exe", new Property { Types = ["string"], Description = "powershell or cmd or dotnet"} },
                { "command", new Property { Types = ["string"], Description = "The command" } },
                { "waitForCompletion", new Property { Types = ["boolean"], Description = "Set to false to run the command in the background, without output message" } }
            }
        },
        new()
        {
            Name = "search_files",
            Description = "To return a list of files with patches in solution directory based on a search regex pattern, use the search_files tool.",
            ExampleToSystemMessage = """
                                     For example:
                                     <|tool_call_begin|> functions.search_files:1 <|tool_call_argument_begin|> {"regex": "^.*\.cs$"} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = FileSearchAsync,
            Properties = new Dictionary<string, Property>
            {
                { "regex", new Property { Types = ["string"], Description = "The regex pattern for files in all solution directory. Example: '^.*\\.cs$'" } }
            }
        },
        new()
        {
            Name = "grep_search",
            Description = "To perform a grep search within the project, call the grep_search tool with the regex pattern to match.",
            ExampleToSystemMessage = """
                                     For example:
                                     <|tool_call_begin|> functions.grep_search:1 <|tool_call_argument_begin|> {"regex": "^.*?main_services.*"} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            Approval = ApprovalKind.AutoApprove,
            ExecuteAsync = GrepSearchAsync,
            Properties = new Dictionary<string, Property>
            {
                { "regex", new Property { Types = ["string"], Description = "The regex pattern to match. Example: .*main_services.*" } }
            }
        },
        new()
        {
            Name = "view_diff",
            Description = "To view the current git diff, use the view_diff tool. This will show you the changes made in the working directory compared to the last commit.",
            ExampleToSystemMessage = """
                                     For example
                                     <|tool_call_begin|> functions.view_diff:1 <|tool_call_argument_begin|> <|tool_call_end|>
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
                                     For example
                                     <|tool_call_begin|> functions.read_currently_open_file:1 <|tool_call_argument_begin|> <|tool_call_end|>
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
                                     <|tool_call_begin|> functions.ls:1 <|tool_call_argument_begin|> {"dirPath": "path/to/dir", "recursive": false} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ListDirectoryAsync,
            Properties = new Dictionary<string, Property>
            {
                { "dirPath", new Property { Types = ["string"], Description = "The directory path" } },
                { "recursive", new Property { Types = ["boolean"], Description = "Use recursive search" } }
            }
        },
        new()
        {
            Name = "fetch_url_content",
            Description = "To fetch the content of a URL, use the fetch_url_content tool.",
            ExampleToSystemMessage = """
                                     For example, to read the contents of a webpage, you might respond with:
                                     <|tool_call_begin|> functions.fetch_url_content:1 <|tool_call_argument_begin|> {"url": "https://example.com"} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = FetchUrlContentAsync,
            Properties = new Dictionary<string, Property>
            {
                { "url", new Property { Types = ["string"], Description = "https://example.com" } }
            }
        },
        new()
        {
            Name = "apply_diff",
            Description = """
                          To make multiple edits to a single file, use the apply_diff tool with a path and diff.
                          If you're not confident in the exact content to search for, use the read_files tool first to get the exact content.
                          When applying the diffs, be extra careful to remember to change any closing brackets or other syntax that may be affected by the diff farther down in the file.
                          """,
            ExampleToSystemMessage = """
                                     The SEARCH section must exactly match existing content including whitespace and indentation.
                                     start_line - The starting line of the file for SEARCH block.
                                     If you're not confident in the exact content to search for, use the read_file tool first to get the exact content.
                                     For example, you could respond with:
                                     <|tool_call_begin|> functions.apply_diff:1 <|tool_call_argument_begin|>
                                     { "path": "path/to/file.txt",
                                       "diffs": [
                                         {
                                            "start_line": 12,
                                            "search": "    // Old calculation logic\n    const result = value * 0.9;\n    return result;",
                                            "replace": "    // Updated calculation logic with logging\n    console.log(`Calculating for value: ${value}`);\n    const result = value * 0.95; // Adjusted factor\n    return result;"
                                         }
                                       ]
                                     <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Medium,
            ExecuteAsync = ApplyDiffAsync,
            Properties = new
            {
                path = new { type = "string", description = "Relative or absolute path to the file in solution." },
                diffs = new
                {
                    type = "array",
                    description = "Array of diffs to apply",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            start_line = new { type = "integer" },
                            search = new { type = "array", description = "The SEARCH lines must exactly match existing content including whitespace and indentation.", items = new { type = "string" } },
                            replace = new { type = "array", description = "The REPLACE lines", items = new { type = "string" } }
                        },
                        required = new[] { "start_line", "search", "replace" }
                    }
                }
            }
        },
        new()
        {
            Name = "show_diff_files_to_user",
            Description = "To show the difference between two files in Visual Studio interface, call the show_diff_files_to_user tool with relative file paths.",
            ExampleToSystemMessage = """
                                     For example:
                                     <|tool_call_begin|> functions.show_diff_files_to_user:1 <|tool_call_argument_begin|> {"file1": "path/to/file1.cs", "file2": "path/to/file2.cs"} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = ViewDiffFilesAsync,
            Properties = new Dictionary<string, Property>
            {
                { "file1", new Property { Types = ["string"], Description = "The relative path/to/file1.txt" } },
                { "file2", new Property { Types = ["string"], Description = "The relative path/to/file2.txt" } },
                { "file1Title", new Property { Types = ["string"], Description = "The title of file 1" } },
                { "file2Title", new Property { Types = ["string"], Description = "The title of file 2" } },
            }
        },
        new()
        {
            Name = "build_solution",
            Description = "To build solution in Visual Studio. With action - Build, Rebuild or Clean. When any errors returns errors list.",
            ExampleToSystemMessage = """
                                     For example:
                                     <|tool_call_begin|> functions.build_solution:1 <|tool_call_argument_begin|> {"action": 0} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = BuildSolutionAsync,
            Properties = new Dictionary<string, Property>
            {
                { "action", new Property { Types = ["integer"], Description = "The build action enum. 0 - Build, 1 - Rebuild, 2 - Clean." } }
            }
        },
        new()
        {
            Name = "get_error_list",
            Description = "To get error list of current solution and current file from Visual Studio.",
            ExampleToSystemMessage = """
                                     For example:
                                     <|tool_call_begin|> functions.get_error_list:1 <|tool_call_argument_begin|> {} <|tool_call_end|>
                                     """,
            RiskLevel = RiskLevel.Low,
            ExecuteAsync = GetErrorListAsync
        }
    ];

    private static async Task<ToolResult> ReadFileAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filesInString = args.GetString("files");
        var files = JsonUtils.Deserialize<List<string>>(filesInString);

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var stingBuilder = new StringBuilder();
        var isSuccess = true;
        foreach (var fileName in files)
        {
            var filepath = GetAbsolutePath(fileName, solutionPath);
            if (!File.Exists(filepath))
            {
                stingBuilder.AppendLine($"File \"{fileName}\" doesn't exist.");
                break;
            }

            stingBuilder.AppendLine($"\"{fileName}\" content");

            try
            {
                var lineNum = 0;
                foreach (var readLine in File.ReadLines(filepath))
                {
                    stingBuilder.AppendLine($"{++lineNum} | {readLine}");
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);
                stingBuilder.AppendLine($"Error {e.Message}");
                isSuccess = false;
            }
        }

        return new ToolResult
        {
            IsSuccess = isSuccess,
            Result = stingBuilder.ToString(),
            PrivateResult = isSuccess ? "Files read successfully" : "Errors in log"
        };
    }

    private static async Task<ToolResult> CreateNewFileAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath = GetAbsolutePath(args.GetString("filepath"), solutionPath);
        var contents = args.GetString("contents");

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
        var exe = args.GetString("exe");
        var command = args.GetString("command");
        var waitForCompletion = !args.ContainsKey("waitForCompletion") || args.GetBool("waitForCompletion");

        var solutionPath = await GetSolutionPathAsync();

        if (exe is not ("cmd" or "powershell" or "dotnet"))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = $"{exe} is unsupported."
            };
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Command should be not empty."
            };
        }

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var startInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = exe is "powershell"
                ? $"-Command \"{command}\""
                : command,
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

    private static async Task<ToolResult> FileSearchAsync(IReadOnlyDictionary<string, object> args)
    {
        var pattern = args.GetString("regex");
        if (string.IsNullOrEmpty(pattern))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Regex pattern should be not empty."
            };
        }

        var solutionPath = await GetSolutionPathAsync();
        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var files = (await GetAllSolutionFilesAsync()).Where(f => regex.IsMatch(f))
            .Select(f => MakeRelativeToSolution(f, solutionPath))
            .ToArray();

        return new ToolResult
        {
            Result = files.Length == 0 ? "Nothing found." : string.Join(", ", files)
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

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var files = await GetAllSolutionFilesAsync();
        var regex = new Regex(query, RegexOptions.Multiline);

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var matches = regex.Matches(content);
                if (matches.Count <= 0)
                    continue;
                var relativePath = MakeRelativeToSolution(file, solutionPath);
                results.Add($"{relativePath} - {matches.Count} matches");
            }
            catch
            {
                // Skip files that can't be read
            }
        }

        return new ToolResult
        {
            Result = results.Count == 0 ? "Nothing found." : string.Join("\n", results)
        };
    }

    private static async Task<ToolResult> ViewDiffAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var content = File.ReadAllText(docView.Document.FilePath);
        return new ToolResult
        {
            Result = $"Read active file: {relativePath}{Environment.NewLine}{content}"
        };
    }

    private static async Task<ToolResult> ListDirectoryAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var dirPath = GetAbsolutePath(args.GetString("dirPath"), solutionPath);
        var recursive = args.GetBool("recursive");

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
        if (string.IsNullOrEmpty(url))
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Error: url is empty."
            };
        }

        try
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = "Visual_ChatGpt_Studio";
            request.Timeout = 3000;

            using var response = await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            if (stream == null)
            {
                return new ToolResult
                {
                    IsSuccess = false,
                    Result = "Error: response stream is null."
                };
            }

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

    private static async Task<ToolResult> ApplyDiffAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var inputFileName = args.GetString("path");
        var filepath = GetAbsolutePath(inputFileName, solutionPath);
        var replacements = args.GetObject<List<DiffReplacement>>("diffs");

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!File.Exists(filepath))
            return new ToolResult { IsSuccess = false, Result = "File doesn't exist." };

        if (replacements == null || replacements.Count == 0)
            return new ToolResult { IsSuccess = false, Result = "No valid replacements found in diff." };

        var lines = File.ReadAllLines(filepath).ToList();
        var totalReplacements = 0;
        var appliedReplacements = new List<string>();

        replacements = replacements.OrderByDescending(r => r.StartLine).ToList();

        foreach (var replacement in replacements)
        {
            var startIndex = replacement.StartLine - 1;
            var endLine = replacement.StartLine + replacement.Search.Count - 1;
            var endIndex = endLine - 1;

            if (replacement.StartLine < 1 || endLine > lines.Count)
            {
                continue;
            }

            var currentLines = lines.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

            if (!currentLines.SequenceEqual(replacement.Search))
            {
                continue;
            }

            // Replace lines
            lines.RemoveRange(startIndex, currentLines.Count);
            lines.InsertRange(startIndex, replacement.Replace);

            totalReplacements++;
            appliedReplacements.Add($"{replacement.StartLine}-{endLine}");
        }

        File.WriteAllLines(filepath, lines);

        return new ToolResult
        {
            Result = $"File {inputFileName} updated. Applied {totalReplacements} replacements: {string.Join(", ", appliedReplacements)}"
        };
    }

    private static async Task<ToolResult> ViewDiffFilesAsync(IReadOnlyDictionary<string, object> args)
    {
        var solutionPath = await GetSolutionPathAsync();
        var filepath1 = GetAbsolutePath(args.GetString("file1"), solutionPath);
        var filepath2 = GetAbsolutePath(args.GetString("file2"), solutionPath);
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

        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = Shell.Package.GetGlobalService(typeof(DTE)) as DTE;
        dte?.ExecuteCommand("Tools.DiffFiles", $"\"{filepath1}\" \"{filepath2}\"");
        return new ToolResult
        {
            Result = "Diff successfully showed."
        };
    }

    private static async Task<ToolResult> BuildSolutionAsync(IReadOnlyDictionary<string, object> args)
    {
        var buildAction = (Toolkit.BuildAction)args.GetInt("action");
        var result = await VS.Build.BuildSolutionAsync(buildAction);

        if (!result)
        {
            var errorList = await GetErrorListAsync(null);

            return new ToolResult
            {
                IsSuccess = false,
                Result = $"""
                          Build is failed.

                          {errorList.Result}
                          """,
                PrivateResult = errorList.PrivateResult
            };
        }

        return new ToolResult
        {
            Result = "Build is successful."
        };
    }

    private static async Task<ToolResult> GetErrorListAsync(IReadOnlyDictionary<string, object> args)
    {
        await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dte = Shell.Package.GetGlobalService(typeof(DTE)) as DTE2;
        var errorList = dte?.ToolWindows?.ErrorList?.ErrorItems;
        if (errorList == null)
        {
            return new ToolResult
            {
                IsSuccess = false,
                Result = "Error list is null"
            };
        }

        var errors = new List<BuildError>();

        for (var i = 1; i <= errorList.Count; i++)
        {
            var errorItem = errorList.Item(i);
            try
            {
                errors.Add(new BuildError
                {
                    Message = errorItem.Description,
                    FileName = errorItem.FileName,
                    Line = errorItem.Line
                });
            }
            catch
            {
                // safe skip error
            }
        }

        return new ToolResult
        {
            Result = JsonUtils.Serialize(errors)
        };
    }

    private static async Task<string> GetSolutionPathAsync()
    {
        var solution = await VS.Solutions.GetCurrentSolutionAsync();
        return solution != null ? Path.GetDirectoryName(solution.FullPath) : Directory.GetCurrentDirectory();
    }

    private static string GetAbsolutePath(string relativePath, string solutionPath)
    {
        if (string.IsNullOrEmpty(relativePath))
            throw new ArgumentException("Path cannot be null or empty.");

        var path = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimStart('.', Path.DirectorySeparatorChar);
        return path.StartsWith(solutionPath) ? path : $"{solutionPath}{Path.DirectorySeparatorChar}{path}";
    }

    private static string MakeRelativeToSolution(string fullPath, string solutionPath)
    {
        return !fullPath.StartsWith(solutionPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : fullPath.Substring(solutionPath.Length + 1);
    }

    /// <summary>
    /// Get list with full file paths included in solution.
    /// </summary>
    private static async Task<List<string>> GetAllSolutionFilesAsync()
    {
        var files = new List<string>();
        var projects = await VS.Solutions.GetAllProjectsAsync();
        foreach (var project in projects)
        {
            await WalkItemsAsync(project.Children, files);
        }

        return files;
    }

    private static async Task WalkItemsAsync(IEnumerable<Toolkit.SolutionItem> items, List<string> files)
    {
        foreach (var item in items)
        {
            switch (item.Type)
            {
                case Toolkit.SolutionItemType.PhysicalFile when (item as Toolkit.PhysicalFile)?.Extension is not (".zip" or ".bin" or ".dll" or ".exe") :
                    files.Add(item.FullPath);
                    break;
                case Toolkit.SolutionItemType.Project :
                    files.Add(item.FullPath);
                    await WalkItemsAsync(item.Children, files);
                    break;
                case Toolkit.SolutionItemType.PhysicalFolder or Toolkit.SolutionItemType.SolutionFolder :
                    await WalkItemsAsync(item.Children, files);
                    break;
            }
        }
    }

    private class BuildError
    {
        [JsonPropertyName("message")]
        public string Message { get; init; }

        [JsonPropertyName("file_name")]
        public string FileName { get; init; }

        [JsonPropertyName("line")]
        public int Line { get; init; }
    }

    private class DiffReplacement
    {
        [JsonPropertyName("start_line")]
        public int StartLine { get; init; } = -1;

        [JsonPropertyName("search")]
        public List<string> Search { get; init; } = [];

        [JsonPropertyName("replace")]
        public List<string> Replace { get; init; } = [];
    }
}
