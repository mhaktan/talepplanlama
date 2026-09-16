// ---------------------------------------------------------------------------
// Enum definitions — auto-generated from ER model
// Maps integer values to display labels for enum fields
// ---------------------------------------------------------------------------

// ChangeRequest.status
export const ChangeRequestStatusMap: Record<string, string> = {
  '0': 'Draft',
  '1': 'PendingFirstApproval',
  '2': 'PendingSecondApproval',
  '3': 'Revision',
  '4': 'PendingRoutePlanning',
  '5': 'PendingSystem',
  '6': 'PendingOperations',
  '7': 'Completed',
  '8': 'Cancelled'
};
export const ChangeRequestStatusOptions = [
  { label: 'Draft', value: '0' },
  { label: 'PendingFirstApproval', value: '1' },
  { label: 'PendingSecondApproval', value: '2' },
  { label: 'Revision', value: '3' },
  { label: 'PendingRoutePlanning', value: '4' },
  { label: 'PendingSystem', value: '5' },
  { label: 'PendingOperations', value: '6' },
  { label: 'Completed', value: '7' },
  { label: 'Cancelled', value: '8' }
];

// ImplementationLog.phase
export const ImplementationLogPhaseMap: Record<string, string> = {
  '0': 'RoutePlanning',
  '1': 'System',
  '2': 'Operations'
};
export const ImplementationLogPhaseOptions = [
  { label: 'RoutePlanning', value: '0' },
  { label: 'System', value: '1' },
  { label: 'Operations', value: '2' }
];
