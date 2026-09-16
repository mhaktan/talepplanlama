import React from 'react';
import { TkButton, TkInput, TkCheckbox, TkSelect, TkTable } from '@takeoff-ui/react';
import { actionColumn, registerCrudHandler } from '../shared/ActionButtons';
import { DeleteConfirmDialog } from '../shared/DeleteConfirmDialog';
import { roleApi, type AppRoleDto } from './rbacApi';
import { PermissionPicker } from './PermissionPicker';

const BASE_COLUMNS = [
  { field: 'name', header: 'Name', sortable: true, searchable: true },
  { field: 'displayName', header: 'Display Name', sortable: true, searchable: true },
  { field: 'description', header: 'Description', sortable: true, searchable: true },
  { field: 'permissionCount', header: 'Permissions', sortable: true },
  { field: 'type', header: 'Type', sortable: true },
];

export default function RoleListScreen() {
  const [roles, setRoles] = React.useState<AppRoleDto[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [showCreate, setShowCreate] = React.useState(false);
  const [editRole, setEditRole] = React.useState<AppRoleDto | null>(null);
  const [confirmDelete, setConfirmDelete] = React.useState<AppRoleDto | null>(null);

  const load = React.useCallback(() => {
    setLoading(true);
    roleApi.list()
      .then(r => setRoles(r.items ?? []))
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  React.useEffect(() => { load(); }, [load]);

  React.useEffect(() => {
    return registerCrudHandler((action, id) => {
      const r = roles.find(x => String(x.id) === id);
      if (!r) return;
      if (action === 'edit') { setEditRole(r); return; }
      if (action === 'delete') {
        if (r.isSystem) { alert('System roles cannot be deleted.'); return; }
        setConfirmDelete(r);
      }
    });
  }, [roles, load]);

  const rows = roles.map(r => ({
    ...r,
    type: r.isSystem ? 'System' : 'Custom',
    permissionCount: r.permissions.length,
  }));

  // System roles are read-only (Admin/User) — only emit Edit, no Delete column entry
  const columns = [...BASE_COLUMNS, ...actionColumn('id', { hasEdit: true, hasDelete: true })];

  return (
    <div style={{ padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <h1 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>Roles</h1>
        <TkButton label="+ New Role" variant="primary" onTkClick={() => setShowCreate(true)} />
      </div>
      {error && <div style={{ padding: 12, background: '#fdecea', color: '#d32f2f', borderRadius: 4, marginBottom: 12 }}>{error}</div>}

      <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 6, overflow: 'hidden' }}>
        <TkTable data={rows} columns={columns as any} dataKey="id" loading={loading} />
        {roles.length === 0 && !loading && (
          <div style={{ padding: 24, textAlign: 'center', color: '#999' }}>No roles yet.</div>
        )}
      </div>

      <RoleCreateModal open={showCreate} onClose={() => setShowCreate(false)} onSuccess={load} />
      <RoleEditModal role={editRole} onClose={() => setEditRole(null)} onSuccess={load} />

      <DeleteConfirmDialog
        visible={!!confirmDelete}
        label={confirmDelete?.name ?? ''}
        isPending={false}
        onCancel={() => setConfirmDelete(null)}
        onConfirm={() => {
          if (!confirmDelete) return;
          const r = confirmDelete;
          roleApi.delete(r.id).then(load).catch(e => alert(e.message));
          setConfirmDelete(null);
        }}
      />
    </div>
  );
}


const ModalShell: React.FC<{ open: boolean; title: string; onClose: () => void; children: React.ReactNode }> = ({ open, title, onClose, children }) => {
  if (!open) return null;
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }} onClick={onClose}>
      <div style={{ background: '#fff', borderRadius: 8, width: '90%', maxWidth: 720, maxHeight: '90vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }} onClick={(e) => e.stopPropagation()}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '14px 20px', borderBottom: '1px solid #e0e0e0' }}>
          <span style={{ fontWeight: 600, fontSize: 16 }}>{title}</span>
          <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: 18, cursor: 'pointer', color: '#666' }}>✕</button>
        </div>
        <div style={{ flex: 1, overflowY: 'auto', padding: 20 }}>{children}</div>
      </div>
    </div>
  );
};

const Field: React.FC<{ label: string; required?: boolean; children: React.ReactNode }> = ({ label, required, children }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
    <label style={{ fontSize: 12, color: '#666' }}>{label}{required && <span style={{ color: '#d32f2f' }}> *</span>}</label>
    {children}
  </div>
);

// ---------------------------------------------------------------------------
// Role Create Modal
// ---------------------------------------------------------------------------

const RoleCreateModal: React.FC<{ open: boolean; onClose: () => void; onSuccess: () => void }> = ({ open, onClose, onSuccess }) => {
  const [name, setName] = React.useState('');
  const [displayName, setDisplayName] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [isActive, setIsActive] = React.useState(true);
  const [permissions, setPermissions] = React.useState<string[]>([]);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!open) {
      setName(''); setDisplayName(''); setDescription('');
      setIsActive(true); setPermissions([]); setError(null);
    }
  }, [open]);

  const handleSave = async () => {
    setError(null); setSaving(true);
    try {
      await roleApi.create({ name, displayName, description, isActive, permissions });
      onSuccess(); onClose();
    } catch (e: any) { setError(e.message); }
    finally { setSaving(false); }
  };

  return (
    <ModalShell open={open} title="New Role" onClose={onClose}>
      {error && <div style={{ padding: 12, background: '#fdecea', color: '#d32f2f', borderRadius: 4, marginBottom: 16 }}>{error}</div>}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
        <Field label="Name (system identifier)" required>
          <TkInput mode="text" value={name} onTkChange={(e: any) => setName(e.detail ?? '')} />
        </Field>
        <Field label="Display Name">
          <TkInput mode="text" value={displayName} onTkChange={(e: any) => setDisplayName(e.detail ?? '')} />
        </Field>
        <Field label="Description">
          <TkInput mode="text" value={description} onTkChange={(e: any) => setDescription(e.detail ?? '')} />
        </Field>
        <Field label="Status">
          <TkCheckbox value={isActive} label="Active" onTkChange={(e: any) => setIsActive(!!e.detail)} />
        </Field>
      </div>
      <Field label="Permissions">
        <PermissionPicker selected={permissions} onChange={setPermissions} />
      </Field>
      <div style={{ display: 'flex', gap: 8, marginTop: 24, justifyContent: 'flex-end' }}>
        <TkButton label="Cancel" variant="neutral" onTkClick={() => onClose()} />
        <TkButton label={saving ? 'Saving...' : 'Save'} variant="primary" onTkClick={() => handleSave()} disabled={saving} />
      </div>
    </ModalShell>
  );
};

// ---------------------------------------------------------------------------
// Role Edit Modal
// ---------------------------------------------------------------------------

const RoleEditModal: React.FC<{ role: AppRoleDto | null; onClose: () => void; onSuccess: () => void }> = ({ role, onClose, onSuccess }) => {
  const [name, setName] = React.useState('');
  const [displayName, setDisplayName] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [isActive, setIsActive] = React.useState(true);
  const [permissions, setPermissions] = React.useState<string[]>([]);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (role) {
      setName(role.name); setDisplayName(role.displayName);
      setDescription(role.description); setIsActive(role.isActive);
      setPermissions(role.permissions); setError(null);
    }
  }, [role]);

  const handleSave = async () => {
    if (!role) return;
    setError(null); setSaving(true);
    try {
      await roleApi.update({ id: role.id, name, displayName, description, isActive, permissions });
      onSuccess(); onClose();
    } catch (e: any) { setError(e.message); }
    finally { setSaving(false); }
  };

  return (
    <ModalShell open={!!role} title={`Edit Role: ${role?.name ?? ''}${role?.isSystem ? ' (System)' : ''}`} onClose={onClose}>
      {error && <div style={{ padding: 12, background: '#fdecea', color: '#d32f2f', borderRadius: 4, marginBottom: 16 }}>{error}</div>}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
        <Field label="Name (system identifier)" required>
          <TkInput mode="text" value={name} disabled={role?.isSystem ?? false} onTkChange={(e: any) => setName(e.detail ?? '')} />
        </Field>
        <Field label="Display Name">
          <TkInput mode="text" value={displayName} onTkChange={(e: any) => setDisplayName(e.detail ?? '')} />
        </Field>
        <Field label="Description">
          <TkInput mode="text" value={description} onTkChange={(e: any) => setDescription(e.detail ?? '')} />
        </Field>
        <Field label="Status">
          <TkCheckbox value={isActive} label="Active" onTkChange={(e: any) => setIsActive(!!e.detail)} />
        </Field>
      </div>
      <Field label="Permissions">
        <PermissionPicker selected={permissions} onChange={setPermissions} />
      </Field>
      <div style={{ display: 'flex', gap: 8, marginTop: 24, justifyContent: 'flex-end' }}>
        <TkButton label="Cancel" variant="neutral" onTkClick={() => onClose()} />
        <TkButton label={saving ? 'Saving...' : 'Save'} variant="primary" onTkClick={() => handleSave()} disabled={saving} />
      </div>
    </ModalShell>
  );
};
