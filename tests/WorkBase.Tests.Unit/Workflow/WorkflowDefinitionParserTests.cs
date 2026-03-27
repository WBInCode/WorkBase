using WorkBase.Modules.Workflow.Application.Models;
using WorkBase.Modules.Workflow.Application.Services;
using Xunit;

namespace WorkBase.Tests.Unit.Workflow;

public class WorkflowDefinitionParserTests
{
    private const string ValidLeaveRequestJson = """
    {
        "name": "leave-request-v1",
        "version": 1,
        "entityType": "LeaveRequest",
        "initialStep": "submitted",
        "steps": [
            {
                "name": "submitted",
                "type": "action",
                "transitions": [
                    { "outcome": "submit", "targetStep": "pending_approval" }
                ],
                "actions": [
                    { "type": "notify", "trigger": "on_enter", "payload": { "template": "leave_submitted" } }
                ]
            },
            {
                "name": "pending_approval",
                "type": "approval",
                "approverStrategy": "supervisor",
                "transitions": [
                    { "outcome": "approved", "targetStep": "approved" },
                    { "outcome": "rejected", "targetStep": "rejected" },
                    { "outcome": "returned", "targetStep": "submitted" }
                ]
            },
            {
                "name": "approved",
                "type": "end"
            },
            {
                "name": "rejected",
                "type": "end"
            }
        ]
    }
    """;

    [Fact]
    public void Parse_ValidJson_ReturnsSuccess()
    {
        var result = WorkflowDefinitionParser.Parse(ValidLeaveRequestJson);

        Assert.True(result.IsSuccess);
        Assert.Equal("leave-request-v1", result.Value.Name);
        Assert.Equal("LeaveRequest", result.Value.EntityType);
        Assert.Equal("submitted", result.Value.InitialStep);
        Assert.Equal(4, result.Value.Steps.Count);
    }

    [Fact]
    public void Parse_ValidJson_ParsesTransitionsCorrectly()
    {
        var result = WorkflowDefinitionParser.Parse(ValidLeaveRequestJson);

        Assert.True(result.IsSuccess);
        var pendingApproval = result.Value.Steps.First(s => s.Name == "pending_approval");
        Assert.Equal(3, pendingApproval.Transitions.Count);
        Assert.Contains(pendingApproval.Transitions, t => t.Outcome == "approved" && t.TargetStep == "approved");
        Assert.Contains(pendingApproval.Transitions, t => t.Outcome == "rejected" && t.TargetStep == "rejected");
        Assert.Contains(pendingApproval.Transitions, t => t.Outcome == "returned" && t.TargetStep == "submitted");
    }

    [Fact]
    public void Parse_ValidJson_ParsesActionsCorrectly()
    {
        var result = WorkflowDefinitionParser.Parse(ValidLeaveRequestJson);

        Assert.True(result.IsSuccess);
        var submitted = result.Value.Steps.First(s => s.Name == "submitted");
        Assert.NotNull(submitted.Actions);
        Assert.Single(submitted.Actions);
        Assert.Equal("notify", submitted.Actions[0].Type);
        Assert.Equal("on_enter", submitted.Actions[0].Trigger);
    }

    [Fact]
    public void Parse_ValidJson_ParsesApproverStrategy()
    {
        var result = WorkflowDefinitionParser.Parse(ValidLeaveRequestJson);

        Assert.True(result.IsSuccess);
        var approval = result.Value.Steps.First(s => s.Name == "pending_approval");
        Assert.Equal("supervisor", approval.ApproverStrategy);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsFailure()
    {
        var result = WorkflowDefinitionParser.Parse("{ invalid json }");

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InvalidJson", result.Error.Code);
    }

    [Fact]
    public void Parse_EmptyName_ReturnsValidationError()
    {
        var json = """
        {
            "name": "",
            "version": 1,
            "entityType": "LeaveRequest",
            "initialStep": "submitted",
            "steps": [
                { "name": "submitted", "type": "action", "transitions": [{ "outcome": "done", "targetStep": "end" }] },
                { "name": "end", "type": "end" }
            ]
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.PropertyName == "name");
    }

    [Fact]
    public void Parse_NoSteps_ReturnsValidationError()
    {
        var json = """
        {
            "name": "test",
            "version": 1,
            "entityType": "Test",
            "initialStep": "start",
            "steps": []
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.PropertyName == "steps");
    }

    [Fact]
    public void Parse_InvalidInitialStep_ReturnsValidationError()
    {
        var json = """
        {
            "name": "test",
            "version": 1,
            "entityType": "Test",
            "initialStep": "nonexistent",
            "steps": [
                { "name": "start", "type": "action", "transitions": [{ "outcome": "done", "targetStep": "end" }] },
                { "name": "end", "type": "end" }
            ]
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.PropertyName == "initialStep");
    }

    [Fact]
    public void Parse_NoEndStep_ReturnsValidationError()
    {
        var json = """
        {
            "name": "test",
            "version": 1,
            "entityType": "Test",
            "initialStep": "start",
            "steps": [
                { "name": "start", "type": "action", "transitions": [{ "outcome": "done", "targetStep": "middle" }] },
                { "name": "middle", "type": "action", "transitions": [{ "outcome": "done", "targetStep": "start" }] }
            ]
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.ErrorMessage.Contains("krok końcowy"));
    }

    [Fact]
    public void Parse_InvalidTargetStep_ReturnsValidationError()
    {
        var json = """
        {
            "name": "test",
            "version": 1,
            "entityType": "Test",
            "initialStep": "start",
            "steps": [
                { "name": "start", "type": "action", "transitions": [{ "outcome": "done", "targetStep": "nonexistent" }] },
                { "name": "end", "type": "end" }
            ]
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.ErrorMessage.Contains("nonexistent"));
    }

    [Fact]
    public void Parse_StepWithoutTransitions_NonEndType_ReturnsValidationError()
    {
        var json = """
        {
            "name": "test",
            "version": 1,
            "entityType": "Test",
            "initialStep": "start",
            "steps": [
                { "name": "start", "type": "approval", "transitions": [] },
                { "name": "end", "type": "end" }
            ]
        }
        """;

        var result = WorkflowDefinitionParser.Parse(json);

        Assert.True(result.IsFailure);
        Assert.Contains(result.ValidationErrors, e => e.ErrorMessage.Contains("przejście"));
    }
}
