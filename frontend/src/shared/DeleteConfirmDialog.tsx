import React from 'react';
import { UiDialog, UiButton } from './ui';

interface DeleteConfirmDialogProps {
  visible: boolean;
  label: string;
  isPending: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const DeleteConfirmDialog: React.FC<DeleteConfirmDialogProps> = ({
  visible, label, isPending, onConfirm, onCancel,
}) => (
  <UiDialog
    visible={visible}
    header="Confirm Delete"
    onClose={onCancel}
    footer={<>
      <UiButton label="Cancel" variant="secondary" onClick={onCancel} />
      <UiButton label={isPending ? 'Deleting…' : 'Delete'} variant="danger" onClick={onConfirm} disabled={isPending} />
    </>}
  >
    Are you sure you want to delete <strong>{label}</strong>? This action cannot be undone.
  </UiDialog>
);
