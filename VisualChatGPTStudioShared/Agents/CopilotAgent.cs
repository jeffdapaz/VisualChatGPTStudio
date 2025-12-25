using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;

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
            functions.Add(GetFunctionReadFiles());

            return functions;
        }

        public static async Task<string> ExecuteFunctionAsync(FunctionResult function)
        {
            string functionResult;

            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                if (function.Function.Name.Equals(nameof(GetSolutionStructure)))
                {
                    OnExecutingFunction?.Invoke($"Reading solution structure...");

                    functionResult = await GetSolutionStructure();
                }
                else if (function.Function.Name.Equals(nameof(ReadFiles)))
                {
                    functionResult = await ReadFiles(arguments["filePaths"].ToObject<List<string>>());
                }
                else
                {
                    functionResult = $"The function {function.Function.Name} not exists.";
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                functionResult = ex.Message;
            }

            OnExecutingFunction?.Invoke($"Thinking...");

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

        #endregion Functions Definitions

        #region Private Methods

        private static async Task<string> GetSolutionStructure()
        {
            return await SolutionExplorerHelper.GetSolutionStructureJsonAsync();
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
                    EnvDTE.ProjectItem item = await SolutionExplorerHelper.FindProjectItemByPathAsync(filePath);

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
    }

    #endregion Private Methods
}
