using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json;
using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisualChatGPTStudioShared.Agents.CodeEditAgent
{
    /// <summary>
    /// Allows editing code as an Agent
    /// </summary>
    public static class CodeEditAgent
    {
        /// <summary>
        /// Returns a <see cref="FunctionRequest"/> describing the "edit_code" function, which specifies the function schema structured for use with OpenAI function calling.
        /// </summary>
        /// <returns>
        /// A <see cref="FunctionRequest"/> object representing the "edit_code" function definition, including its parameters and expected properties for code editing operations.
        /// </returns>
        public static FunctionRequest GetFunction()
        {
            Parameter positionParameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "line", new Property { Types = ["integer"], Description = "Line number (1-based)" } },
                    { "column", new Property { Types = ["integer"], Description = "Column number (1-based)" } }
                }
            };

            return new FunctionRequest
            {
                Function = new Function
                {
                    Name = "edit_code",
                    Description = "Return a list of atomic code edits to apply to the active editor. Each edit must indicate operation and coordinates using 1-based line and column indexes.",
                    Parameters = new Parameter
                    {
                        Properties = new Dictionary<string, Property>
                        {
                            {
                                "edits", new Property
                                {
                                    Types = ["array"],
                                    Description = "Array of edits to apply. Apply edits in the order given (first to last).",
                                    Items = new Parameter
                                    {
                                        Properties = new Dictionary<string, Property>
                                        {
                                            {
                                                "operation", new Property
                                                {
                                                    Types = ["string"],
                                                    Description = "Type of edit: 'insert', 'replace' or 'delete'."
                                                }
                                            },
                                            {
                                                "start", new Property
                                                {
                                                    Types = ["object"],
                                                    Description = "Start position (1-based). Required for all operations.",
                                                    Properties = positionParameter.Properties
                                                }
                                            },
                                            {
                                                "end", new Property
                                                {
                                                    Types = ["object", "null"],
                                                    Description = "End position (1-based). Required for 'replace' and 'delete'.",
                                                    Properties = positionParameter.Properties
                                                }
                                            },
                                            {
                                                "content", new Property
                                                {
                                                    Types = ["string", "null"],
                                                    Description = "Text to insert or to use as replacement. Required for 'insert' and 'replace'."
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Applies a series of code edits to the currently active document in Visual Studio based on a JSON payload.
        /// </summary>
        /// <param name="functionJsonPayload">A JSON string representing the edit operations to apply.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> containing a JSON-serialized <see cref="EditCodeApplyResult"/> with the result of the operation,
        /// including success status, error messages, and the updated code if successful.
        /// </returns>
        public static async Task<string> ApplyEditCodeAsync(string functionJsonPayload)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView == null)
                {
                    return JsonConvert.SerializeObject(new EditCodeApplyResult
                    {
                        Success = false,
                        Message = "No active document found."
                    });
                }

                ITextSnapshot snapshot = docView.TextView.TextBuffer.CurrentSnapshot;

                EditCodeFunctionPayload payload = JsonConvert.DeserializeObject<EditCodeFunctionPayload>(functionJsonPayload);

                if (payload?.Edits == null || payload.Edits.Count == 0)
                {
                    return JsonConvert.SerializeObject(new EditCodeApplyResult
                    {
                        Success = false,
                        Message = "No edits found in the payload."
                    });
                }

                List<(EditOperation op, Span span, string content)> editsToApply = [];

                for (int i = 0; i < payload.Edits.Count; i++)
                {
                    EditOperation edit = payload.Edits[i];

                    if (edit.Operation != "insert" && edit.Operation != "replace" && edit.Operation != "delete")
                    {
                        return JsonConvert.SerializeObject(new EditCodeApplyResult
                        {
                            Success = false,
                            Message = $"Invalid operation '{edit.Operation}' in edits. Expected insert, replace or delete.",
                            FailedEditIndex = i
                        });
                    }

                    if (!IsValidPosition(edit.Start, snapshot))
                    {
                        return JsonConvert.SerializeObject(new EditCodeApplyResult
                        {
                            Success = false,
                            Message = $"Invalid start position: line {edit.Start?.Line}, column {edit.Start?.Column}.",
                            FailedEditIndex = i
                        });
                    }

                    if (edit.Operation == "replace" || edit.Operation == "delete")
                    {
                        if (edit.End == null)
                        {
                            return JsonConvert.SerializeObject(new EditCodeApplyResult
                            {
                                Success = false,
                                Message = $"Missing end position for operation '{edit.Operation}'.",
                                FailedEditIndex = i
                            });
                        }

                        if (!IsValidPosition(edit.End, snapshot))
                        {
                            return JsonConvert.SerializeObject(new EditCodeApplyResult
                            {
                                Success = false,
                                Message = $"Invalid end position: line {edit.End.Line}, column {edit.End.Column}. Current line count: {snapshot.LineCount}.",
                                FailedEditIndex = i
                            });
                        }

                        if (!IsStartBeforeOrEqualEnd(edit.Start, edit.End))
                        {
                            return JsonConvert.SerializeObject(new EditCodeApplyResult
                            {
                                Success = false,
                                Message = $"Start position must be before or equal to end position.",
                                FailedEditIndex = i
                            });
                        }
                    }

                    if ((edit.Operation == "insert" || edit.Operation == "replace") && string.IsNullOrEmpty(edit.Content))
                    {
                        return JsonConvert.SerializeObject(new EditCodeApplyResult
                        {
                            Success = false,
                            Message = $"Missing content for operation '{edit.Operation}'.",
                            FailedEditIndex = i
                        });
                    }
                }

                foreach (EditOperation edit in payload.Edits)
                {
                    Span span;

                    if (edit.Operation == "insert")
                    {
                        int insertPos = GetPositionFromLineColumn(edit.Start, snapshot);
                        span = new Span(insertPos, 0);
                    }
                    else
                    {
                        int startPos = GetPositionFromLineColumn(edit.Start, snapshot);
                        int endPos = GetPositionFromLineColumn(edit.End, snapshot);
                        span = Span.FromBounds(startPos, endPos);
                    }

                    editsToApply.Add((edit, span, edit.Content));
                }

                ITextEdit textEdit = docView.TextView.TextBuffer.CreateEdit();

                foreach ((EditOperation op, Span span, string content) in editsToApply.OrderByDescending(e => e.span.Start))
                {
                    switch (op.Operation)
                    {
                        case "insert":
                            textEdit.Insert(span.Start, content);
                            break;
                        case "replace":
                            textEdit.Replace(span, content);
                            break;
                        case "delete":
                            textEdit.Delete(span);
                            break;
                    }
                }

                textEdit.Apply();

                string updatedCode = docView.TextView.TextBuffer.CurrentSnapshot.GetText();

                return JsonConvert.SerializeObject(new EditCodeApplyResult
                {
                    Success = true,
                    Message = "Edits applied successfully.",
                    UpdatedCode = updatedCode
                });
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                return JsonConvert.SerializeObject(new EditCodeApplyResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Determines whether the specified <paramref name="pos"/> represents a valid position within the given <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="pos">The position to validate, containing line and column information.</param>
        /// <param name="snapshot">The text snapshot to validate against.</param>
        /// <returns>
        /// True if the position is valid within the snapshot; otherwise, false.
        /// </returns>
        private static bool IsValidPosition(Position pos, ITextSnapshot snapshot)
        {
            if (pos == null)
            {
                return false;
            }

            if (pos.Line < 1 || pos.Line > snapshot.LineCount)
            {
                return false;
            }

            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(pos.Line - 1);

            int lineLength = line.LengthIncludingLineBreak;

            if (pos.Column < 1 || pos.Column > lineLength + 1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the start position is before or equal to the end position.
        /// </summary>
        /// <param name="start">The starting <see cref="Position"/>.</param>
        /// <param name="end">The ending <see cref="Position"/>.</param>
        /// <returns>
        /// True if the start position is before or equal to the end position; otherwise, false.
        /// </returns>
        private static bool IsStartBeforeOrEqualEnd(Position start, Position end)
        {
            if (start.Line < end.Line)
            {
                return true;
            }

            if (start.Line == end.Line && start.Column <= end.Column)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the absolute position within a text snapshot based on a given line and column position.
        /// </summary>
        /// <param name="pos">The <see cref="Position"/> object containing the line and column (1-based).</param>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> representing the current text buffer.</param>
        /// <returns>
        /// The zero-based absolute character position in the snapshot corresponding to the specified line and column.
        /// </returns>
        private static int GetPositionFromLineColumn(Position pos, ITextSnapshot snapshot)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(pos.Line - 1);

            return line.Start.Position + (pos.Column - 1);
        }
    }
}