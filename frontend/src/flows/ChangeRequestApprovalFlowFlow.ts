// Auto-generated flow: ChangeRequest Approval Flow
// Auto-generated approval flow for ChangeRequest. Customize email templates and add conditions as needed.
// Resource: ChangeRequest
// Enabled: true
//
// Nodes:
  // trigger: On ChangeRequest Submit
  // condition: Status = PendingFirstApproval?
  // approval: ChangeRequest Approval
  // action: Send Approval Email
  // trigger: On ChangeRequest Approved
  // action: Send Completion Email
//
// Edges:
  // On ChangeRequest Submit → Status = PendingFirstApproval?
  // Status = PendingFirstApproval? → ChangeRequest Approval (true)
  // ChangeRequest Approval → Send Approval Email
  // On ChangeRequest Approved → Send Completion Email
//
// This file is for documentation purposes.
// Flow execution is handled by FlowEngine.ts using flowDefinitions.json.

export const FLOW_CHANGEREQUEST_APPROVAL_FLOW_ID = 'flow-ChangeRequest-approval';
