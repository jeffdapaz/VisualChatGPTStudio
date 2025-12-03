using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils;
using VisualChatGPTStudioShared.Agents.ApiAgent;

namespace JeffPires.VisualChatGPTStudio.Agents;

public class ToolManager
{
    private readonly ConcurrentDictionary<string, Tool> _registeredTools = new();
    private readonly ConcurrentDictionary<string, ToolToCall> _pendingTools = new();
    private CancellationTokenSource? _approvalCancellationTokenSource;
    private TaskCompletionSource<bool> _approvalTcs;

    public event Func<string, Task<string>> ScriptRequested;

    private async Task<string> RunScriptAsync(string script)
    {
        if (ScriptRequested is null)
            return string.Empty;
        return await ScriptRequested.Invoke(script);
    }

    public void RegisterAllTools()
    {
        foreach (var tool in BuiltInAgent.Tools)
        {
            RegisterTool(tool);
        }

        foreach (var tool in SqlServerAgent.Tools)
        {
            RegisterTool(tool);
        }

        foreach (var tool in ApiAgent.Tools)
        {
            RegisterTool(tool);
        }
    }

    private void RegisterTool(Tool tool)
    {
        if (tool.ExecuteAsync == null)
        {
            Logger.Log($"Tool '{tool.Name}' must have an ExecuteAsync function");
        }
        else
        {
            _registeredTools[tool.Name] = tool;
        }
    }

    public Tool? GetTool(string name)
    {
        return _registeredTools.TryGetValue(name, out var tool) ? tool : null;
    }

    public IEnumerable<Tool> GetAllTools() => _registeredTools.Values;

    public void EnableToolsByCategory(string category, bool enable)
    {
        foreach (var tool in _registeredTools.Values.Where(t => t.Category == category))
        {
            tool.Enabled = enable;
        }
    }

    public IEnumerable<Tool> GetEnabledTools() => _registeredTools.Values.Where(t => t.Enabled);

    public async Task<List<ToolToCall>> RequestApprovalAsync(List<ToolToCall> tools)
    {
        var toolsRequiringApproval = tools.Where(t => t.Tool.Approval == ApprovalKind.Ask).ToList();
        var autoApprovedTools = tools.Where(t => t.Tool.Approval == ApprovalKind.AutoApprove).ToList();

        // Auto approve tools with AutoApprove
        foreach (var tool in autoApprovedTools)
        {
            tool.IsApproved = true;
            tool.IsProcessed = true;
        }

        if (!toolsRequiringApproval.Any())
        {
            return autoApprovedTools;
        }

        _approvalCancellationTokenSource = new CancellationTokenSource();

        try
        {
            _approvalTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Add tools required approval
            foreach (var tool in toolsRequiringApproval)
            {
                _pendingTools[tool.CallId] = tool;
            }

            // Отправляем запросы на подтверждение
            var toolRequests = toolsRequiringApproval.Select(tool => new
            {
                type = "tool_request",
                tool_id = tool.CallId,
                tool_name = tool.Tool.Name,
                tool_category = tool.Tool.Category,
                risk_level = tool.Tool.RiskLevel.ToString(),
                parameters = tool.Parameters,
                reasoning = tool.Tool.ApprovalDescription,
                requested_at = tool.RequestedAt
            }).ToArray();
            await RunScriptAsync(WebFunctions.UpdateLastGpt(JsonUtils.Serialize(toolRequests)));

            // Wait approval
            using (_approvalCancellationTokenSource.Token.Register(() => _approvalTcs.TrySetCanceled()))
            {
                // Waiting until calls SetResult/SetCanceled on _approvalTcs.
                await _approvalTcs.Task.ConfigureAwait(false);
            }

            var approvedTools = _pendingTools.Values
                .Where(t => t.IsApproved)
                .Concat(autoApprovedTools)
                .ToList();

            return approvedTools;
        }
        catch (OperationCanceledException)
        {
            return autoApprovedTools;
        }
        finally
        {
            _pendingTools.Clear();
            _approvalTcs = null;
        }
    }

    public async IAsyncEnumerable<ToolToCall> ExecuteToolsAsync(List<ToolToCall> tools)
    {
        foreach (var tool in tools)
        {
            ToolResult result;
            try
            {
                result = await tool.Tool.ExecuteAsync(tool.Parameters);
            }
            catch (Exception ex)
            {
                result = new ToolResult
                {
                    Result = $"Error executing tool {tool.Tool.Name}: {ex.Message}",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }

            result.Name = tool.Tool.Name;
            result.Args = tool.ArgumentsJson;
            tool.Result = result;
            yield return tool;
        }
    }

    public void ApproveTool(string callId, IReadOnlyDictionary<string, object>? modifiedParameters = null)
    {
        if (_pendingTools.TryGetValue(callId, out var tool))
        {
            if (modifiedParameters != null)
            {
                tool.UpdateParameters(modifiedParameters);
            }
            tool.IsApproved = true;
            tool.IsProcessed = true;

            CheckAllToolsProcessed();
        }
    }

    public void CancelTool(string callId, string reason = "user_cancelled")
    {
        if (!_pendingTools.TryGetValue(callId, out var tool))
            return;

        tool.IsApproved = false;
        tool.IsProcessed = true;
        tool.Result = new ToolResult
        {
            IsSuccess = false,
            Result = $"Tool '{tool.Tool.Name}' cancelled: {reason}"
        };

        CheckAllToolsProcessed();
        Logger.Log($"Tool '{tool.Tool.Name}' cancelled: {reason}");
    }

    private void CheckAllToolsProcessed()
    {
        if (_pendingTools.IsEmpty || _pendingTools.Values.All(t => t.IsProcessed))
        {
            _approvalTcs?.TrySetResult(true);
        }
    }

    public void CancelAllPendingTools()
    {
        _approvalCancellationTokenSource?.Cancel();

        foreach (var tool in _pendingTools.Values.Where(t => !t.IsProcessed))
        {
            tool.IsProcessed = true;
            tool.IsApproved = false;
        }

        _approvalTcs?.TrySetResult(false);
    }

    public IReadOnlyDictionary<string, ToolToCall> GetPendingTools() => _pendingTools;

    private async Task ShowPrivateResultToUser(string toolName, string privateResult)
    {
        var privateMessage = new
        {
            type = "private_result",
            tool_name = toolName,
            result = privateResult
        };

        await RunScriptAsync(WebFunctions.AddMsg(IdentifierEnum.ChatGPT, JsonUtils.Serialize(privateMessage)));
    }

    public string GetToolUseSystemInstructions()
    {
        var enabledTools = GetEnabledTools().ToList();
        if (enabledTools.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("""
                      You are a function-calling assistant.

                      ## Tool use instructions

                      You have access to several "tools" that you can use at any time to retrieve information and/or perform tasks for the User.

                      You MUST invoke tools exclusively with the following literal syntax; no other format is allowed:
                      <|tool_calls_section_begin|> <|tool_call_begin|> functions.<toolName>:<index> <|tool_call_argument_begin|> {<arg1>:<val1>,<arg2>:<val2>} <|tool_call_end|> <|tool_calls_section_end|>
                      Immediately after <|tool_calls_section_end|> - stop generation, no explanatory text.

                      Explanation:
                      <|tool_calls_section_begin|>                          // Required! Start of calls block.
                        <|tool_call_begin|> functions.<toolName>:<index>    // function header. toolName - function name. index - index number from 0 in calls_section.
                          <|tool_call_argument_begin|> {}                   // argument body JSON object
                        <|tool_call_end|>                                   // end of first call
                        <|tool_call_begin|> functions.<toolName>:<index>    // optional second function header. toolName - function name. index - index number in calls_section.
                          <|tool_call_argument_begin|> {}                   // argument body JSON object
                        <|tool_call_end|>                                   // end of second call
                      <|tool_calls_section_end|>                            // Optional. End of the calls block. All examples below without this block.

                      The following tools are available to you:

                      """);

        foreach (var tool in enabledTools)
        {
            sb.AppendLine(tool.Description);
            sb.AppendLine(tool.ExampleToSystemMessage);
            sb.AppendLine();
        }

        sb.AppendLine("""

                      If it seems like the User's request could be solved with the tools, choose the BEST tool for the job based on the user's request and the tool descriptions
                      Then send the tool_calls_section (YOU call the tool, not the user).
                      Do not perform actions with/for hypothetical files. Use tools to deduce which files are relevant.
                      You can call multiple tools in one tool_calls_section.
                      """);
        return sb.ToString();
    }
}
