using System;
using System.Collections.Generic;

namespace talepplanlama.Flows
{
    public class FlowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ResourceName { get; set; }
        public bool Enabled { get; set; }
        public List<FlowVariable> Variables { get; set; } = new();
        public List<FlowNode> Nodes { get; set; } = new();
        public List<FlowEdge> Edges { get; set; } = new();
    }

    public class FlowNode
    {
        public string Id { get; set; }
        public string Type { get; set; } // trigger, action, condition, loop, delay, approval
        public string Label { get; set; }
        public FlowTrigger Trigger { get; set; }
        public FlowAction Action { get; set; }
        public FlowCondition Condition { get; set; }
        public FlowApproval Approval { get; set; }
        public string CollectionVariable { get; set; }
        public string ItemVariable { get; set; }
        public int DelayMs { get; set; }
    }

    public class FlowApproval
    {
        public List<FlowApprovalStep> Steps { get; set; } = new();
        public bool TrackRevisionHistory { get; set; }
        public string EmailSubjectTemplate { get; set; }
        public string EmailBodyTemplate { get; set; }
    }

    public class FlowApprovalStep
    {
        public string Name { get; set; }
        public string AssigneeType { get; set; } // "field" | "fixed" | "role"
        public string AssigneeValue { get; set; }
        public List<FlowApprovalAction> Actions { get; set; } = new();
    }

    public class FlowApprovalAction
    {
        public string Label { get; set; }
        public string Type { get; set; } // "approve" | "revise"
        public List<string> RequiredFields { get; set; } = new();
    }

    public class FlowEdge
    {
        public string Id { get; set; }
        public string SourceNodeId { get; set; }
        public string TargetNodeId { get; set; }
        public string Branch { get; set; } // "true" or "false" for condition nodes
        public string Label { get; set; }
    }

    public class FlowVariable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
    }

    public class FlowTrigger
    {
        public string Type { get; set; }
        public string ResourceName { get; set; }
        public string FieldName { get; set; }
        public string CronExpression { get; set; }
        public string WebhookPath { get; set; }
    }

    public class FlowAction
    {
        public string Type { get; set; }
        public Dictionary<string, object> Config { get; set; } = new();
    }

    public class FlowCondition
    {
        public string Logic { get; set; } // "and" or "or"
        public List<ConditionRule> Rules { get; set; } = new();
    }

    public class ConditionRule
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }

    public class FlowContext
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public Dictionary<string, object> TriggerData { get; set; } = new();
    }

    public class ExecutionResult
    {
        public bool Success { get; set; }
        public List<LogEntry> Log { get; set; } = new();
        public List<string> VisitedNodeIds { get; set; } = new();
    }

    public class LogEntry
    {
        public string NodeId { get; set; }
        public string NodeLabel { get; set; }
        public string Status { get; set; } // executed, skipped, error
        public string Message { get; set; }
    }
}
