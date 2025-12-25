using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using Project = EnvDTE.Project;
using Property = OpenAI_API.Functions.Property;
using Task = System.Threading.Tasks.Task;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    public static class CopilotAgent
    {
        public delegate void OnExecutingFunctionHandler(string message);
        public static event OnExecutingFunctionHandler OnExecutingFunction;

        #region Public Methods

        public static List<FunctionRequest> GetFunctions()
        {
            List<FunctionRequest> functions = [];

            functions.Add(GetFunctionGetSolutionStructure());
            functions.Add(GetFunctionGetActiveFilePath());
            functions.Add(GetFunctionReadActiveFile());
            functions.Add(GetFunctionReadFiles());
            functions.Add(GetFunctionAddFilesToProject());
            functions.Add(GetFunctionBuildSolution());
            functions.Add(GetFunctionReplaceFilesContent());
            functions.Add(GetFunctionReplaceInFilesLiteral());
            functions.Add(GetFunctionReplaceInFilesRegex());

            return functions;
        }

        public static async Task<string> ExecuteFunctionAsync(FunctionResult function)
        {
            string functionResult;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                switch (function.Function.Name)
                {
                    case nameof(GetSolutionStructure):
                        OnExecutingFunction?.Invoke("Reading solution structure...");
                        functionResult = await GetSolutionStructure();
                        break;
                    case nameof(GetActiveFilePath):
                        OnExecutingFunction?.Invoke("Getting current active (opened) file path...");
                        functionResult = await GetActiveFilePath();
                        break;
                    case nameof(ReadActiveFile):
                        OnExecutingFunction?.Invoke("Reading current active (opened) file...");
                        functionResult = await ReadActiveFile();
                        break;
                    case nameof(ReadFiles):
                        functionResult = await ReadFiles(arguments["filePaths"].ToObject<List<string>>());
                        break;
                    case nameof(AddFilesToProject):
                        functionResult = await AddFilesToProject(arguments["files"].ToObject<List<FileInfo>>());
                        break;
                    case nameof(BuildSolution):
                        OnExecutingFunction?.Invoke("Building the solution...");
                        functionResult = await BuildSolution();
                        break;
                    case nameof(ReplaceFilesContent):
                        functionResult = await ReplaceFilesContent(arguments["filesToUpdate"].ToObject<List<FileInfo>>());
                        break;
                    case nameof(ReplaceInFilesLiteral):
                        functionResult = await ReplaceInFilesLiteral(arguments["operations"].ToObject<List<FileReplaceOperation>>());
                        break;
                    case nameof(ReplaceInFilesRegex):
                        functionResult = await ReplaceInFilesRegex(arguments["operations"].ToObject<List<FileReplaceOperation>>());
                        break;
                    default:
                        functionResult = $"The function {function.Function.Name} not exists.";
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                functionResult = ex.Message;
            }

            OnExecutingFunction?.Invoke("Thinking...");

            return functionResult;
        }

        #endregion Public Methods

        #region Functions Definitions

        private static FunctionRequest GetFunctionGetSolutionStructure()
        {
            return new()
            {
                Function = new()
                {
                    Name = nameof(GetSolutionStructure),
                    Description = "Request to get the structure of the current solution, including projects and files.",
                }
            };
        }

        public static FunctionRequest GetFunctionGetActiveFilePath()
        {
            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(GetActiveFilePath),
                    Description = "Returns the full path of the file currently active (opened) in the Visual Studio editor.",
                }
            };
        }

        public static FunctionRequest GetFunctionReadActiveFile()
        {
            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(ReadActiveFile),
                    Description = "Returns the content of the file currently active (opened) in the Visual Studio editor.",
                }
            };
        }

        private static FunctionRequest GetFunctionReadFiles()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "filePaths",
                        new Property
                        {
                            Types = ["array"],
                            Description = "Array of the relative file paths: e.g., ['path/to/file1.txt', 'path/to/file2.txt']",
                            Items = new Parameter { Type = "string" }
                        }
                    }
                }
            };

            return new()
            {
                Function = new()
                {
                    Name = nameof(ReadFiles),
                    Description = "Request to read the contents of one or more files.",
                    Parameters = parameter
                }
            };
        }

        private static FunctionRequest GetFunctionAddFilesToProject()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "files",
                        new Property
                        {
                            Types = ["array"],
                            Description = "List of files to add: each item with 'path' (relative path including name and extension) and optionally 'content'.",
                            Items = new Parameter
                            {
                                Properties = new Dictionary<string, Property>
                                {
                                    {
                                        "path",
                                        new Property
                                        {
                                            Types = ["string"],
                                            Description = "The file path, relative to the project root, including the name and extension. Example: 'Controllers/WeatherController.cs'"
                                        }
                                    },
                                    {
                                        "content",
                                        new Property
                                        {
                                            Types = ["string", "null"],
                                            Description = "File content (optional). If omitted, creates empty file."
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return new()
            {
                Function = new()
                {
                    Name = nameof(AddFilesToProject),
                    Description = "Request to add one or more files to the project.",
                    Parameters = parameter
                }
            };
        }

        private static FunctionRequest GetFunctionBuildSolution()
        {
            return new()
            {
                Function = new()
                {
                    Name = nameof(BuildSolution),
                    Description = "Request to build the current solution. You can use to validate the changes made to the project and get errors if any.",
                }
            };
        }

        private static FunctionRequest GetFunctionReplaceFilesContent()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "filesToUpdate",
                        new Property
                        {
                            Types = ["array"],
                            Description = "List of files to update: each item with 'path' (relative path including name and extension) and 'content'.",
                            Items = new Parameter
                            {
                                Properties = new Dictionary<string, Property>
                                {
                                    {
                                        "path",
                                        new Property
                                        {
                                            Types = ["string"],
                                            Description = "The file path, relative to the project root, including the name and extension. Example: 'Controllers/WeatherController.cs'"
                                        }
                                    },
                                    {
                                        "content",
                                        new Property
                                        {
                                            Types = ["string", "null"],
                                            Description = "The file content to replace the existing content."
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return new()
            {
                Function = new()
                {
                    Name = nameof(ReplaceFilesContent),
                    Description = "Request to replace the content of one or more files in the project.",
                    Parameters = parameter
                }
            };
        }

        private static FunctionRequest GetFunctionReplaceInFilesLiteral()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "operations", new Property
                        {
                            Types = ["array"],
                            Description = "List of replace operations: each item has 'path', 'target', and 'replacement' as literal strings.",
                            Items = new Parameter
                            {
                                Properties = new Dictionary<string, Property>
                                {
                                    {
                                        "path", new Property
                                        {
                                            Types = ["string"],
                                            Description = "Relative path to the file to edit, including name and extension. For example: 'ConsoleApp1/Program.cs'."
                                        }
                                    },
                                    {
                                        "target", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The exact (case sensitive) string to replace."
                                        }
                                    },
                                    {
                                        "replacement", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The text to insert in place of the target."
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(ReplaceInFilesLiteral),
                    Description = "Replace all literal occurrences of the 'target' string by 'replacement' in one or more files. Always match the target exactly and case-sensitively. This is a simple literal replacement (not regex).",
                    Parameters = parameter
                }
            };
        }

        private static FunctionRequest GetFunctionReplaceInFilesRegex()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "operations", new Property
                        {
                            Types = ["array"],
                            Description = "List of replace operations: each item has 'path', a .NET regex string 'target', and 'replacement'.",
                            Items = new Parameter
                            {
                                Properties = new Dictionary<string, Property>
                                {
                                    {
                                        "path", new Property
                                        {
                                            Types = ["string"],
                                            Description = "Relative path to the file to edit, including name and extension. For example: 'ConsoleApp1/Program.cs'."
                                        }
                                    },
                                    {
                                        "target", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The regex pattern (.NET syntax) to find text to replace."
                                        }
                                    },
                                    {
                                        "replacement", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The text to insert in place of each regex match."
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = "ReplaceInFilesRegex",
                    Description = "Replace all text matching the .NET regular expression 'target' with the string 'replacement' in one or more files. Always use valid .NET regex syntax.",
                    Parameters = parameter
                }
            };
        }

        #endregion Functions Definitions

        #region Private Methods

        private static async Task<string> GetSolutionStructure()
        {
            return await SolutionExplorerHelper.GetSolutionStructureJsonAsync();
        }

        public static async Task<string> GetActiveFilePath()
        {
            Document document = await SolutionExplorerHelper.GetActiveFileAsync();

            if (document == null)
            {
                return "[No active document]";
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return document.FullName;
        }

        public static async Task<string> ReadActiveFile()
        {
            Document document = await SolutionExplorerHelper.GetActiveFileAsync();

            if (document == null)
            {
                return "[No active document]";
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (document.Object("TextDocument") is not TextDocument textDoc)
            {
                return "[Active document is not a text file]";
            }

            EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();

            return startPoint.GetText(textDoc.EndPoint);
        }

        private static async Task<string> ReadFiles(List<string> filePaths)
        {
            List<object> results = [];

            await Task.Yield();

            foreach (string filePath in filePaths)
            {
                string content = null;
                string errorMessage = null;

                try
                {
                    ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

                    if (item != null)
                    {
                        OnExecutingFunction?.Invoke($"Reading {Path.GetFileName(filePath)} file...");

                        content = await SolutionExplorerHelper.GetProjectItemContentAsync(item);
                    }
                    else
                    {
                        errorMessage = "File not found in Solution";
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error reading file: {ex.Message}";
                }

                results.Add(new { filePath, content, errorMessage });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        public static async Task<string> AddFilesToProject(List<FileInfo> files)
        {
            List<object> results = [];

            await Task.Yield();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            foreach (FileInfo newFile in files)
            {
                string status = "success";

                try
                {
                    if (string.IsNullOrWhiteSpace(newFile.Path))
                    {
                        throw new Exception("Path is empty");
                    }

                    bool fileHandled = false;

                    OnExecutingFunction?.Invoke($"Creating {newFile.Path} file...");

                    foreach (Project project in dte.Solution.Projects)
                    {
                        string projectName = Path.GetFileNameWithoutExtension(project.FullName);

                        string relativePath = newFile.Path;

                        if (relativePath.StartsWith(projectName + "/", StringComparison.OrdinalIgnoreCase) ||
                            relativePath.StartsWith(projectName + "\\", StringComparison.OrdinalIgnoreCase))
                        {
                            relativePath = relativePath.Substring(projectName.Length + 1);
                        }

                        string projectDir = Path.GetDirectoryName(project.FullName);
                        string destPath = Path.Combine(projectDir, relativePath);

                        string folder = Path.GetDirectoryName(destPath);

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        File.WriteAllText(destPath, newFile.Content ?? string.Empty);

                        bool alreadyIncluded = false;

                        foreach (ProjectItem item in project.ProjectItems)
                        {
                            if (item?.FileCount > 0 && string.Equals(item.FileNames[1], destPath, StringComparison.OrdinalIgnoreCase))
                            {
                                alreadyIncluded = true;

                                break;
                            }
                        }

                        if (!alreadyIncluded)
                        {
                            project.ProjectItems.AddFromFile(destPath);
                        }

                        fileHandled = true;

                        break; //Only add to the first matching project
                    }

                    if (!fileHandled)
                    {
                        status = "No project found to add file.";
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }

                results.Add(new { path = newFile.Path, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        private static async Task<string> BuildSolution()
        {
            await Task.Yield();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            DTE2 dte2 = dte as DTE2;

            dte2.Solution.SolutionBuild.Build(true);

            bool success = dte2.Solution.SolutionBuild.LastBuildInfo == 0;

            List<string> errors = [];

            if (!success)
            {
                ErrorList errorList = dte2.ToolWindows.ErrorList;

                ErrorItems errorItems = errorList.ErrorItems;

                for (int i = 1; i <= errorItems.Count; i++)
                {
                    ErrorItem err = errorItems.Item(i);

                    if (err.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        errors.Add($"File: {err.FileName}, Line: {err.Line}, Description: {err.Description}");
                    }
                }
            }

            BuildResult result = new() { Success = success, Errors = errors };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        public static async Task<string> ReplaceFilesContent(List<FileInfo> filesToUpdate)
        {
            List<object> results = [];

            await Task.Yield();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            DTE2 dte2 = dte as DTE2;

            foreach (FileInfo update in filesToUpdate)
            {
                OnExecutingFunction?.Invoke($"Editing {update.Path} file...");

                string status = "success";

                try
                {
                    if (string.IsNullOrWhiteSpace(update.Path))
                    {
                        throw new Exception("Path is empty.");
                    }

                    ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(update.Path);

                    if (item != null)
                    {
                        await SolutionExplorerHelper.WriteContentToFileAsync(item, update.Content);
                    }
                    else //fallback by file path
                    {
                        SolutionExplorerHelper.WriteContentToFile(update.Path, update.Content);
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }

                results.Add(new { path = update.Path, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        public static Task<string> ReplaceInFilesLiteral(List<FileReplaceOperation> operations)
        {
            static (string newContent, string error) ReplaceLiteral(string content, string target, string replacement)
            {
                if (!content.Contains(target))
                {
                    return (content, $"Target not found: {target}");
                }

                string res = content.Replace(target, replacement);

                return (res, null);
            }

            return ReplaceInFilesAsync(operations, ReplaceLiteral);
        }

        public static Task<string> ReplaceInFilesRegex(List<FileReplaceOperation> operations)
        {
            static (string newContent, string error) ReplaceRegex(string content, string pattern, string replacement)
            {
                try
                {
                    Regex rx = new(pattern, RegexOptions.Multiline);

                    if (!rx.IsMatch(content))
                    {
                        return (content, $"Regex not matched: {pattern}");
                    }

                    string res = rx.Replace(content, replacement);

                    return (res, null);
                }
                catch (Exception ex)
                {
                    return (content, ex.Message);
                }
            }

            return ReplaceInFilesAsync(operations, ReplaceRegex);
        }

        private static async Task<string> ReplaceInFilesAsync(List<FileReplaceOperation> operations,
                                                             Func<string, string, string, (string content, string error)> replaceFunc)
        {
            List<object> results = [];

            List<FileReplaceOperation> orderedOps = operations.OrderBy(o => o.Path).ToList();
            string lastPath = null;
            ProjectItem currentItem = null;
            string fileContent = null;
            bool errorInCurrentPath = false;

            for (int i = 0; i < orderedOps.Count; i++)
            {
                FileReplaceOperation update = orderedOps[i];

                // Change of file? Save/previous commit
                if (lastPath == null || !string.Equals(update.Path, lastPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (fileContent != null && !errorInCurrentPath)
                    {
                        try
                        {
                            if (currentItem != null)
                            {
                                await SolutionExplorerHelper.WriteContentToFileAsync(currentItem, fileContent);
                            }
                            else //fallback by file path
                            {
                                SolutionExplorerHelper.WriteContentToFile(update.Path, fileContent);
                            }

                            results.Add(new { path = lastPath, status = "success" });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new { path = lastPath, status = ex.Message });
                        }
                    }

                    lastPath = update.Path;
                    errorInCurrentPath = false;
                    currentItem = null;
                    fileContent = null;

                    OnExecutingFunction?.Invoke($"Editing {update.Path} file...");

                    currentItem = await SolutionExplorerHelper.FindProjectItemByPathAsync(update.Path);

                    if (currentItem == null)
                    {
                        results.Add(new { path = update.Path, status = "File not found in solution." });

                        errorInCurrentPath = true;
                    }
                    else
                    {
                        fileContent = await SolutionExplorerHelper.GetProjectItemContentAsync(currentItem);
                    }
                }

                if (!errorInCurrentPath && fileContent != null)
                {
                    if (string.IsNullOrEmpty(update.Target))
                    {
                        results.Add(new { path = update.Path, status = "Empty target for replacement." });

                        errorInCurrentPath = true;

                        continue;
                    }

                    try
                    {
                        (string newContent, string error) = replaceFunc(fileContent, update.Target, update.Replacement ?? "");

                        if (error != null)
                        {
                            results.Add(new { path = update.Path, status = error });
                            errorInCurrentPath = true;
                            continue;
                        }

                        fileContent = newContent;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { path = update.Path, status = ex.Message });

                        errorInCurrentPath = true;
                    }
                }

                bool isLastOperationForThisFile = (i == orderedOps.Count - 1) ||
                    !string.Equals(orderedOps[i + 1].Path, update.Path, StringComparison.OrdinalIgnoreCase);

                if (isLastOperationForThisFile && lastPath != null)
                {
                    if (!errorInCurrentPath && fileContent != null)
                    {
                        try
                        {
                            if (currentItem != null)
                            {
                                await SolutionExplorerHelper.WriteContentToFileAsync(currentItem, fileContent);
                            }
                            else //fallback by file path
                            {
                                SolutionExplorerHelper.WriteContentToFile(update.Path, fileContent);
                            }

                            results.Add(new { path = update.Path, status = "success" });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new { path = update.Path, status = ex.Message });
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        #endregion Private Methods
    }
}

public class FileInfo
{
    public string Path { get; set; }

    public string Content { get; set; }
}

public class BuildResult
{
    public bool Success { get; set; }

    public List<string> Errors { get; set; }
}

public class FileReplaceOperation
{
    public string Path { get; set; }

    public string Target { get; set; }

    public string Replacement { get; set; }
}
