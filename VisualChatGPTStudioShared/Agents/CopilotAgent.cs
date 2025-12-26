using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Markdig.Helpers;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using VisualChatGPTStudioShared.Utils;
using Parameter = OpenAI_API.Functions.Parameter;
using Project = EnvDTE.Project;
using Property = OpenAI_API.Functions.Property;
using Task = System.Threading.Tasks.Task;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Represents a static class for CopilotAgent containing utility methods and functionalities for agent operations.
    /// </summary>
    public static class CopilotAgent
    {
        /// <summary>
        /// Represents the method that will handle an event when a function is being executed, providing a message as an argument.
        /// </summary>
        public delegate void OnExecutingFunctionHandler(string message);
        public static event OnExecutingFunctionHandler OnExecutingFunction;

        #region Public Methods

        /// <summary>
        /// Retrieves a list of available <see cref="FunctionRequest"/> objects that represent supported operations,
        /// such as solution structure retrieval, file management, build actions, error listing, content replacement, diff viewing, and file searching.
        /// </summary>
        /// <returns>
        /// A <see cref="List{T}"/> of <see cref="FunctionRequest"/> objects describing all supported functions.
        /// </returns>
        public static List<FunctionRequest> GetFunctions()
        {
            List<FunctionRequest> functions = [];

            functions.Add(GetFunctionGetSolutionStructure());
            functions.Add(GetFunctionOpenFileInEditor());
            functions.Add(GetFunctionGetActiveFilePath());
            functions.Add(GetFunctionReadActiveFile());
            functions.Add(GetFunctionReadFiles());
            functions.Add(GetFunctionAddFilesToProject());
            functions.Add(GetFunctionDeleteFiles());
            functions.Add(GetFunctionRenameFiles());
            functions.Add(GetFunctionBuildSolution());
            functions.Add(GetFunctionGetCurrentErrorsList());
            functions.Add(GetFunctionReplaceFilesContent());
            functions.Add(GetFunctionReplaceInFilesLiteral());
            functions.Add(GetFunctionReplaceInFilesRegex());
            functions.Add(GetFunctionShowDiff());
            functions.Add(GetFunctionShowDiffWithFile());
            functions.Add(GetFunctionDownloadFromUrl());
            functions.Add(GetFunctionFindFilesContainingContent());

            return functions;
        }

        /// <summary>
        /// Executes a specified asynchronous function by its name with parsed arguments,
        /// switching execution to the main thread as required (e.g., for Visual Studio automation).
        /// Handles various function names to perform actions such as getting the solution structure,
        /// opening files in the editor, reading or modifying files, building the solution,
        /// showing diffs, downloading content, and more.
        /// In case of an exception, logs the error and returns the exception message.
        /// Triggers the <see cref="OnExecutingFunction"/> event with a "Thinking..." message before returning.
        /// </summary>
        /// <param name="function">A <see cref="FunctionResult"/> instance containing the function name and arguments as JSON.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation, with the result of the function execution as a string,
        /// or an error message if the function does not exist or an exception occurred.
        /// </returns>
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
                        functionResult = await GetSolutionStructure();
                        break;
                    case nameof(OpenFileInEditor):
                        functionResult = await OpenFileInEditor(arguments["filePath"].ToObject<string>());
                        break;
                    case nameof(GetActiveFilePath):
                        functionResult = await GetActiveFilePath();
                        break;
                    case nameof(ReadActiveFile):
                        functionResult = await ReadActiveFile();
                        break;
                    case nameof(ReadFiles):
                        functionResult = await ReadFiles(arguments["filePaths"].ToObject<List<string>>());
                        break;
                    case nameof(AddFilesToProject):
                        functionResult = await AddFilesToProject(arguments["files"].ToObject<List<FileInfo>>());
                        break;
                    case nameof(DeleteFiles):
                        functionResult = await DeleteFiles(arguments["filePaths"].ToObject<List<string>>());
                        break;
                    case nameof(RenameFiles):
                        functionResult = await RenameFiles(arguments["files"].ToObject<List<FileRenameInfo>>());
                        break;
                    case nameof(BuildSolution):
                        functionResult = await BuildSolution();
                        break;
                    case nameof(GetCurrentErrorsList):
                        functionResult = await GetCurrentErrorsList();
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
                    case nameof(ShowDiff):
                        functionResult = await ShowDiff(arguments["filesExtension"].ToObject<string>(), arguments["leftText"].ToObject<string>(), arguments["rightText"].ToObject<string>());
                        break;
                    case nameof(ShowDiffWithFile):
                        functionResult = await ShowDiffWithFile(arguments["filePath"].ToObject<string>(), arguments["proposedCode"].ToObject<string>());
                        break;
                    case nameof(DownloadFromUrl):
                        functionResult = await DownloadFromUrl(arguments["url"].ToObject<string>());
                        break;
                    case nameof(FindFilesContainingContent):
                        functionResult = await FindFilesContainingContent(arguments["content"].ToObject<string>());
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

        /// <summary>
        /// Creates a <c>FunctionRequest</c> for retrieving the structure of the current solution, including its projects and files.
        /// </summary>
        /// <returns>
        /// A <c>FunctionRequest</c> configured with the function name and description for getting solution structure.
        /// </returns>
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

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for opening a file in the Visual Studio editor using its relative path within the solution.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="FunctionRequest"/> configured to execute the 'OpenFileInEditor' function, which requires a 'filePath' parameter indicating the relative file path (e.g., 'ConsoleApp1/Program.cs').
        /// </returns>
        public static FunctionRequest GetFunctionOpenFileInEditor()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "filePath",
                        new Property
                        {
                            Types = ["string"],
                            Description = "The relative path to the file in the solution to open, e.g. 'ConsoleApp1/Program.cs'."
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(OpenFileInEditor),
                    Description = "Opens a specified file in the Visual Studio editor given its relative path in the solution.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for retrieving the full path of the file currently active (opened) in the Visual Studio editor.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured to call the <c>GetActiveFilePath</c> function, which returns the absolute file path of the active document in the IDE.
        /// </returns>
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

        /// <summary>
        /// Creates a FunctionRequest for the ReadActiveFile function, which returns the content of the file currently active (opened) in the Visual Studio editor.
        /// </summary>
        /// <returns>
        /// A FunctionRequest configured to retrieve the contents of the active file in the Visual Studio editor.
        /// </returns>
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

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> to read the contents of one or more files.
        /// The function request includes a parameter called "filePaths", which is an array of relative file paths to be read.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured to invoke the "ReadFiles" function with the required file path parameters.
        /// </returns>
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

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for adding one or more files to a project.
        /// Each file entry should specify a relative 'path' (including name and extension) and optionally 'content'.
        /// If 'content' is omitted, the file will be created empty.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> variant containing parameter definitions for adding files, including
        /// supported properties for file path and optional content.
        /// </returns>
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

        /// <summary>
        /// Creates and returns a <c>FunctionRequest</c> object specific to the <c>DeleteFiles</c> function.  
        /// The request is configured to accept an array of relative file paths that specify which files should be deleted from the project.
        /// </summary>
        /// <returns>
        /// A <c>FunctionRequest</c> configured to request deletion of the files provided in the <c>filePaths</c> array parameter.
        /// </returns>
        private static FunctionRequest GetFunctionDeleteFiles()
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
                            Description = "Array of the relative file paths to delete: e.g., ['path/to/file1.txt', 'path/to/file2.txt']",
                            Items = new Parameter { Type = "string" }
                        }
                    }
                }
            };

            return new()
            {
                Function = new()
                {
                    Name = nameof(DeleteFiles),
                    Description = "Request to delete one or more files from the project.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a <c>FunctionRequest</c> for renaming and/or moving one or more files within the project structure.
        /// Allows each file operation to specify a new name, new directory, or both, supporting use cases such as refactoring or reorganizing project files.
        /// The operation requires specifying the current file path (<c>oldPath</c>) and the desired new path and/or name (<c>newPath</c>), both relative to the project root.
        /// Intermediate directories will be created as needed.
        /// </summary>
        /// <returns>
        /// A <c>FunctionRequest</c> object configured for file rename and move operations, including all necessary parameter definitions for batch processing.
        /// Each operation returns 'success' if completed or an error message on failure.
        /// </returns>
        private static FunctionRequest GetFunctionRenameFiles()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "files", new Property
                        {
                            Types = ["array"],
                            Description = "List of file rename/move operations. Each item allows you to rename a file, move it to another directory, or both. For rename, provide the new name. For move, include the target directory in 'newPath'. For both, specify a new directory and name.",
                            Items = new Parameter
                            {
                                Properties = new Dictionary<string, Property>
                                {
                                    {
                                        "oldPath", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The current path of the file to rename or move, relative to the project root. Example: 'Controllers/WeatherController.cs'."
                                        }
                                    },
                                    {
                                        "newPath", new Property
                                        {
                                            Types = ["string"],
                                            Description = "The desired new path (including name and/or directory), relative to the project root. Example: 'Controllers/WeatherController2.cs' (rename), 'Models/WeatherController.cs' (move), or 'Models/NewWeatherController.cs' (move and rename). Intermediate directories will be created if needed."
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
                    Name = "RenameFiles",
                    Description = "Renames and/or moves one or more files in the project. For each file, you can change its name, its directory, or both. The project structure is updated accordingly. Use this to refactor or reorganize your code files. Returns 'success' for each operation or an error message if something went wrong.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates and returns a <see cref="FunctionRequest"/> for building the current solution.
        /// The request can be used to validate project changes and retrieve build errors, if any.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured to execute the build solution operation.
        /// </returns>
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

        /// <summary>
        /// Creates a <c>FunctionRequest</c> to retrieve the current list of errors in the solution.
        /// </summary>
        /// <returns>
        /// A new <c>FunctionRequest</c> object configured for the <c>GetCurrentErrorsList</c> function.
        /// </returns>
        private static FunctionRequest GetFunctionGetCurrentErrorsList()
        {
            return new()
            {
                Function = new()
                {
                    Name = nameof(GetCurrentErrorsList),
                    Description = "Request to get the current list of errors in the solution.",
                }
            };
        }

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for replacing the content of one or more files in the project.
        /// Configures parameters to specify a list of files to update, each with its relative path and new content.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> object representing the request to replace the content of specified files.
        /// </returns>
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

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for the "ReplaceInFilesLiteral" function, which performs literal, case-sensitive string replacement in one or more files.
        /// Each operation specifies the file path, the exact target string to be replaced, and the replacement text.
        /// This method constructs the function request with the required parameters for bulk, literal (non-regex) replacements.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured to replace all literal occurrences of a specified target string with a replacement string in the given files, based on the provided operations.
        /// </returns>
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

        /// <summary>
        ///     Creates a <c>FunctionRequest</c> for performing multiple regex-based replacements in files.
        ///     Each operation specifies a file path, a .NET regular expression pattern ("target"),
        ///     and the replacement string ("replacement") to use. Supports processing multiple files and replacement instructions in a single request.
        ///     The regex patterns must use valid .NET regular expression syntax.
        /// </summary>
        /// <returns>
        ///     A <c>FunctionRequest</c> that defines the "ReplaceInFilesRegex" function, its description, and input parameters.
        ///     The "operations" parameter is an array where each item includes:
        ///     - "path": The relative file path to modify (e.g., "Project/File.cs").
        ///     - "target": A .NET regex pattern identifying the text to replace.
        ///     - "replacement": The string to insert for each regex match.
        /// </returns>
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
                    Name = nameof(ReplaceInFilesRegex),
                    Description = "Replace all text matching the .NET regular expression 'target' with the string 'replacement' in one or more files. Always use valid .NET regex syntax.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a request to display a Visual Studio diff window comparing two text blocks.
        /// The diff highlights differences between the original (left) and modified (right) file contents,
        /// using the specified file extension for syntax highlighting (e.g., ".cs", ".txt").
        /// Enables users to review changes before confirming.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured with parameters:
        /// - <c>filesExtension</c>: Syntax highlighting for the diff view.
        /// - <c>leftText</c>: The original file content.
        /// - <c>rightText</c>: The modified file content.
        /// </returns>
        private static FunctionRequest GetFunctionShowDiff()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "filesExtension", new Property
                        {
                            Types = ["string"],
                            Description = "File extension used for syntax highlighting in the diff view, for example: '.cs', '.txt', '.json'."
                        }
                    },
                    {
                        "leftText", new Property
                        {
                            Types = ["string"],
                            Description = "The original (left side) file content to display in the diff window."
                        }
                    },
                    {
                        "rightText", new Property
                        {
                            Types = ["string"],
                            Description = "The modified (right side) file content to display in the diff window."
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(ShowDiff),
                    Description = "Displays a Visual Studio diff window comparing any two text blocks. Use 'filesExtension' for syntax highlighting. The user can compare and review differences before confirming any change.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for displaying a Visual Studio diff window.
        /// Compares the content of a specified project file with proposed new content, allowing the user to visually review changes.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> configured to show a diff window between the current file content and a proposed modification,
        /// using the "filePath" and "proposedCode" parameters.
        /// </returns>
        private static FunctionRequest GetFunctionShowDiffWithFile()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "filePath", new Property
                        {
                            Types = ["string"],
                            Description = "The relative path to the file in the solution to show in the diff, e.g. 'ConsoleApp1/Program.cs'."
                        }
                    },
                    {
                        "proposedCode", new Property
                        {
                            Types = ["string"],
                            Description = "The proposed new content (to appear on the right side of the diff window)."
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(ShowDiffWithFile),
                    Description = "Displays a Visual Studio diff window comparing the current project file content and a proposed modification. The user can review changes visually before proceeding.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> configured for downloading content from a specified URL.
        /// The function downloads the content available at the given HTTP or HTTPS URL and returns it as plain text.
        /// Intended for use with 
        private static FunctionRequest GetFunctionDownloadFromUrl()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "url", new Property
                        {
                            Types = ["string"],
                            Description = "A HTTP or HTTPS URL to download and return the content. Should be public textual content (html, json, markdown, code, etc)."
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(DownloadFromUrl),
                    Description = "Downloads the content at a given URL and returns as plain text. Use only for fetching small and medium contents as public documentation, code examples, JSON, etc.",
                    Parameters = parameter
                }
            };
        }

        /// <summary>
        /// Creates a <see cref="FunctionRequest"/> for finding all files in the solution whose content contains exactly the provided text.
        /// The function returns a JSON list of relative file paths matching the specified content.
        /// The search is exact (not regex or case-insensitive).
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> object configured to find files with content exactly matching the given string.
        /// </returns>
        private static FunctionRequest GetFunctionFindFilesContainingContent()
        {
            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    {
                        "content", new Property
                        {
                            Types = ["string"],
                            Description = "The content (text) to be searched in the solution's files. The search is exact, not regex/case-insensitive."
                        }
                    }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = nameof(FindFilesContainingContent),
                    Description = "Returns a JSON list of relative filePaths to the solution, of all files whose content contains exactly the provided text.",
                    Parameters = parameter
                }
            };
        }

        #endregion Functions Definitions

        #region Private Methods

        /// <summary>
        /// Reads the structure of the current solution using SolutionExplorerHelper and returns it as a JSON string.
        /// Invokes an event to indicate the start of the operation.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation, containing the solution structure in JSON format.
        /// </returns>
        private static async Task<string> GetSolutionStructure()
        {
            await InvokeOnExecutingFunctionAsync("Reading solution structure...");

            return await SolutionExplorerHelper.GetSolutionStructureJsonAsync();
        }

        /// <summary>
        /// Asynchronously opens the specified file in the code editor within the solution, making its window visible and active. 
        /// Logs execution status and errors, and returns success or an error message.
        /// </summary>
        /// <param name="filePath">The full path of the file to be opened.</param>
        /// <returns>
        /// Returns "success" if the file is opened successfully; otherwise, returns an error message if the file is not found or an exception occurs.
        /// </returns>
        public static async Task<string> OpenFileInEditor(string filePath)
        {
            try
            {
                await InvokeOnExecutingFunctionAsync($"Opening File: {filePath}");

                ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

                if (item == null)
                {
                    return $"File not found in Solution: {filePath}";
                }

                Window window = item.Open(EnvDTE.Constants.vsViewKindCode);

                window.Visible = true;

                window.Activate();

                return "success";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                return ex.Message;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the file path of the currently active (opened) document in the solution explorer.
        /// If no document is active, returns "[No active document]".
        /// </summary>
        /// <returns>
        /// The full file path of the active document, or "[No active document]" if none are active.
        /// </returns>
        public static async Task<string> GetActiveFilePath()
        {
            await InvokeOnExecutingFunctionAsync("Getting current active (opened) file path...");

            Document document = await SolutionExplorerHelper.GetActiveFileAsync();

            if (document == null)
            {
                return "[No active document]";
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return document.FullName;
        }

        /// <summary>
        /// Reads the contents of the currently active (opened) file in the editor.
        /// Invokes a notification when the function is executing. If no active document exists,
        /// a message indicating such is returned. If the active document is not a text file,
        /// a corresponding message is returned. Otherwise, returns the full text of the active document.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{String}"/> containing the text of the active file, or an informational message if unavailable.
        /// </returns>
        public static async Task<string> ReadActiveFile()
        {
            await InvokeOnExecutingFunctionAsync("Reading current active (opened) file...");

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

        /// <summary>
        /// Asynchronously reads the content of each file from the specified list of file paths within the solution,
        /// capturing any errors encountered during the process, and returns a JSON-formatted string containing the results.
        /// </summary>
        /// <param name="filePaths">A list of full file paths to read from the solution.</param>
        /// <returns>
        /// A JSON-formatted string representing an array of results for each file, including file path, content, and any error messages.
        /// </returns>
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
                        await InvokeOnExecutingFunctionAsync($"Reading {Path.GetFileName(filePath)} file...");

                        content = await SolutionExplorerHelper.GetProjectItemContentAsync(item);
                    }
                    else
                    {
                        errorMessage = "File not found in Solution";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    errorMessage = $"Error reading file: {ex.Message}";
                }

                results.Add(new { filePath, content, errorMessage });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Adds a list of files to the appropriate projects in the current Visual Studio solution. Each file is written to disk,
        /// included in the first matching project based on the file path, and a result object is created per file indicating success or error status.
        /// Returns a JSON string summarizing the outcome for each file.
        /// </summary>
        /// <param name="files">A list of <see cref="FileInfo"/> instances, each specifying the file path and content to add.</param>
        /// <returns>
        /// An asynchronous task returning a JSON-formatted string containing the path and status (success or error message) for each file processed.
        /// </returns>
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

                    await InvokeOnExecutingFunctionAsync($"Creating {newFile.Path} file...");

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
                    Logger.Log(ex);
                    status = ex.Message;
                }

                results.Add(new { path = newFile.Path, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Deletes the specified list of files from both the physical file system and the solution.
        /// Reports the deletion status for each file in the returned JSON string.
        /// </summary>
        /// <param name="filePaths">
        /// A list of file paths representing the files to be deleted.
        /// </param>
        /// <returns>
        /// A JSON-formatted string detailing the status ("success" or error message) for each file.
        /// </returns>
        public static async Task<string> DeleteFiles(List<string> filePaths)
        {
            List<object> results = [];

            foreach (string filePath in filePaths)
            {
                await InvokeOnExecutingFunctionAsync($"Deleting {filePath}...");

                string status = "success";

                try
                {
                    ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

                    if (item == null)
                    {
                        throw new Exception("File not found in solution");
                    }

                    if (item.FileCount > 0)
                    {
                        string physPath = item.FileNames[1];

                        if (File.Exists(physPath))
                        {
                            File.Delete(physPath);
                        }
                    }

                    item.Remove();
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }

                results.Add(new { path = filePath, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Renames or moves a set of files within a Visual Studio solution, handling project structure updates and physical file movement.
        /// For each file in the input list, moves the file on disk, removes the old ProjectItem from the project, and adds the file
        /// to the appropriate folder within the project. Ensures folders are created if necessary and avoids overwriting existing files.
        /// Returns a JSON-formatted string reporting the outcome (success or error message) for each attempted rename/move.
        /// </summary>
        /// <param name="files">A list of <see cref="FileRenameInfo"/> objects specifying old and new file paths.</param>
        /// <returns>
        /// A JSON-formatted string array, where each item includes the original file path and the status of the operation ("success" or error details).
        /// </returns>
        public static async Task<string> RenameFiles(List<FileRenameInfo> files)
        {
            List<object> results = [];

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            foreach (FileRenameInfo file in files)
            {
                await InvokeOnExecutingFunctionAsync($"Renaming/moving {file.OldPath} to {file.NewPath}...");

                string status = "success";

                try
                {
                    if (string.IsNullOrWhiteSpace(file.OldPath) || string.IsNullOrWhiteSpace(file.NewPath))
                    {
                        throw new Exception("OldPath or NewPath is empty.");
                    }

                    // Find the ProjectItem that matches the old path
                    ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(file.OldPath);

                    if (item == null)
                    {
                        throw new Exception($"File not found in solution: {file.OldPath}");
                    }

                    // Ensure the ProjectItem has a physical file path
                    if (item.FileCount == 0)
                    {
                        throw new Exception("Could not determine physical file path.");
                    }

                    // Get the full path to the old file
                    string oldFullPath = item.FileNames[1];

                    // Get the containing project's directory
                    Project project = item.ContainingProject;

                    string solutionDir = Path.GetDirectoryName(project.FullName);

                    string projectName = Path.GetFileNameWithoutExtension(project.FullName);

                    string cleanedOldPath = file.OldPath;

                    // Remove duplicated prefix from oldPath, if present.
                    if (cleanedOldPath.StartsWith(projectName + "/", StringComparison.OrdinalIgnoreCase) ||
                        cleanedOldPath.StartsWith(projectName + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanedOldPath = cleanedOldPath.Substring(projectName.Length + 1);
                    }

                    string cleanedNewPath = file.NewPath;

                    // Remove duplicated prefix from newPath, if present.
                    if (cleanedNewPath.StartsWith(projectName + "/", StringComparison.OrdinalIgnoreCase) ||
                        cleanedNewPath.StartsWith(projectName + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanedNewPath = cleanedNewPath.Substring(projectName.Length + 1);
                    }

                    // Determine the full path for the new file
                    string newFullPath;

                    if (Path.IsPathRooted(cleanedNewPath))
                    {
                        // If the new path is an absolute path, use it directly
                        newFullPath = cleanedNewPath;
                    }
                    else
                    {
                        // Otherwise, build the new path relative to the solution directory
                        newFullPath = Path.Combine(solutionDir, cleanedNewPath);
                    }

                    // Ensure the destination folder exists
                    string destFolder = Path.GetDirectoryName(newFullPath);

                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    // Check if the destination file already exists to avoid overwriting
                    if (File.Exists(newFullPath))
                    {
                        throw new Exception($"File with new name already exists: {newFullPath}");
                    }

                    // Move the physical file to the new location
                    File.Move(oldFullPath, newFullPath);

                    // Remove the old ProjectItem from the project
                    item.Remove();

                    // Traverse and/or create folders in the project to match the new path
                    ProjectItems parentItems = project.ProjectItems;

                    string[] parts = cleanedNewPath.Replace("\\", "/").Split('/');

                    if (parts.Length > 1)
                    {
                        for (int i = 0; i < parts.Length - 1; i++)
                        {
                            string folderName = parts[i];

                            ProjectItem foundFolder = null;

                            foreach (ProjectItem pi in parentItems)
                            {
                                if (pi.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder && pi.Name == folderName)
                                {
                                    foundFolder = pi;
                                    break;
                                }
                            }

                            if (foundFolder == null)
                            {
                                foundFolder = parentItems.AddFolder(folderName);
                            }

                            parentItems = foundFolder.ProjectItems;
                        }
                    }

                    // Add the new file to the final folder in the project
                    parentItems.AddFromFile(newFullPath);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    status = ex.Message;
                }

                results.Add(new { path = file.OldPath, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Builds the current Visual Studio solution asynchronously and gathers build errors if any occur.
        /// Invokes a status update callback before starting the build process. 
        /// After building, collects all high-level errors from the Error List and returns a JSON-formatted string
        /// containing the build success status and a list of error messages.
        /// </summary>
        /// <returns>
        /// A Task returning a JSON string representing the build result, including overall success status and detailed error information if the build fails.
        /// </returns>
        private static async Task<string> BuildSolution()
        {
            await InvokeOnExecutingFunctionAsync("Building the solution...");

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

        /// <summary>
        /// Asynchronously retrieves the list of current high-level (critical) errors from Visual Studio's Error List tool window,
        /// formats each error with file name, line, and description, and serializes the result as an indented JSON string.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation; the string result is a JSON array of formatted error details.
        /// </returns>
        public static async Task<string> GetCurrentErrorsList()
        {
            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            DTE2 dte2 = dte as DTE2;

            List<string> errors = [];

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

            return JsonConvert.SerializeObject(errors, Formatting.Indented);
        }

        /// <summary>
        /// Asynchronously replaces the content of files specified in the <paramref name="filesToUpdate"/> list.
        /// Updates each file with the given content and logs the operation status for each file.
        /// Utilizes Visual Studio services to locate and update files within the solution, falling back to direct file I/O if necessary.
        /// Returns a JSON-formatted string containing the result status for each file.
        /// </summary>
        /// <param name="filesToUpdate">A list of <see cref="FileInfo"/> objects representing the files to update and their new content.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that resolves to a JSON string with each file's path and the update status ("success" or error message).
        /// </returns>
        public static async Task<string> ReplaceFilesContent(List<FileInfo> filesToUpdate)
        {
            List<object> results = [];

            await Task.Yield();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            DTE2 dte2 = dte as DTE2;

            foreach (FileInfo update in filesToUpdate)
            {
                await InvokeOnExecutingFunctionAsync($"Editing {update.Path} file...");

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
                    Logger.Log(ex);
                    status = ex.Message;
                }

                results.Add(new { path = update.Path, status });
            }

            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Processes a list of file replacement operations, performing literal string replacements on each file.
        /// </summary>
        /// <param name="operations">A list of <see cref="FileReplaceOperation"/> representing the files and replacement targets.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation with result containing any error messages, or an empty string if all replacements succeed.
        /// </returns>
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

        /// <summary>
        /// Replaces text in multiple files using regular expressions as specified in the provided list of <see cref="FileReplaceOperation"/>.
        /// Each file is processed by matching and replacing patterns using regex, and errors are logged.
        /// </summary>
        /// <param name="operations">A list of file replacement operations, each specifying the file path, regex pattern, and replacement string.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous completion of the file replacements, with a summary of any errors encountered.
        /// </returns>
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
                    Logger.Log(ex);
                    return (content, ex.Message);
                }
            }

            return ReplaceInFilesAsync(operations, ReplaceRegex);
        }

        /// <summary>
        /// Performs a batch of file content replacements based on the specified operations, using the provided replacement function.
        /// Iterates through the list of file operations, applies text replacements to each targeted file, handles errors, and writes the updated contents back to disk or project item.
        /// Returns a JSON-formatted string summarizing the success or failure of each operation.
        /// </summary>
        /// <param name="operations">A list of file replacement operations containing the file path, target string, and replacement string.</param>
        /// <param name="replaceFunc">A function that performs the text replacement on the file content and returns the updated content and an error string, if any.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, containing a JSON-formatted string with the result status for each operation.
        /// </returns>
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
                            Logger.Log(ex);
                            results.Add(new { path = lastPath, status = ex.Message });
                        }
                    }

                    lastPath = update.Path;
                    errorInCurrentPath = false;
                    currentItem = null;
                    fileContent = null;

                    await InvokeOnExecutingFunctionAsync($"Editing {update.Path} file...");

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
                        Logger.Log(ex);

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
                            Logger.Log(ex);
                            results.Add(new { path = update.Path, status = ex.Message });
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(results, Formatting.Indented);
        }

        /// <summary>
        /// Displays a diff view comparing two text inputs with the specified file extension.
        /// </summary>
        /// <param name="filesExtension">The file extension to determine syntax highlighting.</param>
        /// <param name="leftText">The left side text for comparison.</param>
        /// <param name="rightText">The right side text for comparison.</param>
        /// <returns>
        /// Returns a string indicating that the diff view has been shown to the user.
        /// </returns>
        public static async Task<string> ShowDiff(string filesExtension, string leftText, string rightText)
        {
            await InvokeOnExecutingFunctionAsync($"Showing diff view...");

            await DiffView.ShowDiffViewAsync(filesExtension, leftText, rightText);

            return "Diff showed to user";
        }

        /// <summary>
        /// Displays a diff view comparing the contents of a file at the specified path with the provided proposed code.
        /// Returns status messages based on the outcome (e.g., file not found, diff displayed).
        /// </summary>
        /// <param name="filePath">The file path of the original file in the project.</param>
        /// <param name="proposedCode">The proposed code to compare against the original file content.</param>
        /// <returns>
        /// A message string indicating the result of the diff operation ("filePath is null or empty", "File not found in Solution", or "Diff showed to user").
        /// </returns>
        public static async Task<string> ShowDiffWithFile(string filePath, string proposedCode)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "filePath is null or empty";
            }

            ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

            if (item == null)
            {
                return "File not found in Solution";
            }

            await InvokeOnExecutingFunctionAsync($"Showing diff view...");

            string originalContent = await SolutionExplorerHelper.GetProjectItemContentAsync(item);

            await DiffView.ShowDiffViewAsync(Path.GetExtension(filePath), originalContent, proposedCode);

            return "Diff showed to user";
        }

        /// <summary>
        /// Downloads data from the specified URL using an HTTP GET request, logs the operation, and returns the result as a JSON string.
        /// The returned JSON includes the response content, content type, HTTP status code, and any error information.
        /// </summary>
        /// <param name="url">
        /// The URL to download data from.
        /// </param>
        /// <returns>
        /// A JSON-formatted string containing the downloaded content, content type, status code, and error details (if any).
        /// </returns>
        public static async Task<string> DownloadFromUrl(string url)
        {
            await InvokeOnExecutingFunctionAsync($"Downloading from URL: {url}");

            string content = "";
            string contentType = "";
            int statusCode = 0;
            string error = null;

            using HttpClient http = new();

            http.Timeout = TimeSpan.FromSeconds(15);

            HttpResponseMessage resp = await http.GetAsync(url);

            statusCode = (int)resp.StatusCode;
            contentType = resp.Content.Headers.ContentType?.MediaType ?? "";

            if (resp.IsSuccessStatusCode)
            {
                content = await resp.Content.ReadAsStringAsync();
            }
            else
            {
                error = $"HTTP error {statusCode}: {resp.ReasonPhrase}";
            }

            var result = new { content, contentType, statusCode, error };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        /// <summary>
        /// Searches all text files in the solution for the specified content and returns a JSON array of file paths containing that content.
        /// </summary>
        /// <param name="content">The string content to search for in the files.</param>
        /// <returns>
        /// An indented JSON string representing the list of file paths that contain the specified content.
        /// </returns>
        public static async Task<string> FindFilesContainingContent(string content)
        {
            List<string> matches = [];

            await InvokeOnExecutingFunctionAsync("Searching for contents...");

            List<string> allFiles = await SolutionExplorerHelper.GetAllTextFilePathsAsync();

            foreach (string filePath in allFiles)
            {
                try
                {
                    ProjectItem projectItem = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

                    if (projectItem != null)
                    {
                        string fileContent = await SolutionExplorerHelper.GetProjectItemContentAsync(projectItem);

                        if (!string.IsNullOrWhiteSpace(fileContent) && fileContent.Contains(content, StringComparison.Ordinal))
                        {
                            matches.Add(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            return JsonConvert.SerializeObject(matches, Formatting.Indented);
        }

        /// <summary>
        /// Invokes the <c>OnExecutingFunction</c> delegate asynchronously with the specified message.
        /// </summary>
        /// <param name="message">The message to pass to the <c>OnExecutingFunction</c> delegate.</param>
        private static async Task InvokeOnExecutingFunctionAsync(string message)
        {
            OnExecutingFunction?.Invoke("Copilot Agent: " + message);

            //pause current thread to allow UI update
            await Task.Delay(50);
        }

        #endregion Private Methods
    }
}

/// <summary>
/// Represents information about a file, such as its name, size, attributes, and creation/modification dates.
/// </summary>
public class FileInfo
{
    /// <summary>
    /// Gets or sets the file or directory path.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the main textual content.
    /// </summary>
    public string Content { get; set; }
}

/// <summary>
/// Represents the result of a build process, containing information about the outcome, errors, and related data.
/// </summary>
public class BuildResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the list of error messages associated with the current object or operation.
    /// </summary>
    public List<string> Errors { get; set; }
}

/// <summary>
/// Represents an operation to replace contents of a file, potentially supporting backup, validation, and error handling mechanisms.
/// </summary>
public class FileReplaceOperation
{
    /// <summary>
    /// Gets or sets the file or directory path associated with this instance.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the target value, specifying the intended recipient or destination.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the replacement string value.
    /// </summary>
    public string Replacement { get; set; }
}

/// <summary>
/// Represents information used for renaming a file, such as original and new file names.
/// </summary>
public class FileRenameInfo
{
    /// <summary>
    /// Gets or sets the original file or directory path before any changes are made.
    /// </summary>
    public string OldPath { get; set; }

    /// <summary>
    /// Gets or sets the new file or directory path.
    /// </summary>
    public string NewPath { get; set; }
}
