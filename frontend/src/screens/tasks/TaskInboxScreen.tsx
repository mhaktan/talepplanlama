import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getRequestHeaders } from '../../dataProvider';

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5000';

// entityType -> App.tsx route adi (App.tsx ile ayni kaynaktan uretilir)
const ENTITY_ROUTES: Record<string, string> = {
  "requesttype": "RequestType",
  "changerequest": "ChangeRequest",
  "implementationlog": "ImplementationLog"
};

const routeForTask = (entityType: string): string | null => {
  const name = ENTITY_ROUTES[(entityType || '').toLowerCase()];
  return name ? '/' + name : null;
};

// Auto-logout on 401 (expired token) — same handler as dashboard fetch
const handle401 = (status: number): boolean => {
  if (status !== 401) return false;
  ['_auth_token', '_bearer_token', '_refresh_token'].forEach(k => localStorage.removeItem(k));
  if (window.location.pathname !== '/login') window.location.href = '/login';
  return true;
};

interface PendingTask {
  approvalRecordId: string;
  entityType: string;
  entityId: string;
  stepName: string;
  creationTime: string;
  availableActions: string[];
  formNo?: string;
  description?: string;
  creatorName?: string;
}

/** ABP hata zarfindan okunabilir mesaji cikarir. */
const parseApiError = (e: unknown): string => {
  const raw = e instanceof Error ? e.message : String(e);
  try {
    const parsed = JSON.parse(raw);
    return parsed?.error?.message || parsed?.error?.details || raw;
  } catch { return raw; }
};

const timeAgo = (dateStr: string) => {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return mins + 'm ago';
  const hours = Math.floor(mins / 60);
  if (hours < 24) return hours + 'h ago';
  return Math.floor(hours / 24) + 'd ago';
};

export default function TaskInboxScreen() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [filter, setFilter] = React.useState('');
  const [comment, setComment] = React.useState('');
  const [modal, setModal] = React.useState<{ task: PendingTask; action: string } | null>(null);
  const [error, setError] = React.useState('');

  const { data: tasks = [], isLoading } = useQuery<PendingTask[]>({
    queryKey: ['pending-tasks'],
    queryFn: async () => {
      const res = await fetch(API_BASE + '/api/services/app/Approval/GetMyPendingApprovals', {
        headers: getRequestHeaders(),
      });
      if (handle401(res.status)) return [];
      if (!res.ok) return [];
      const json = await res.json();
      return json.result ?? json;
    },
    refetchInterval: 30000,
  });

  const process = useMutation({
    mutationFn: async (p: { approvalRecordId: string; action: string; comment?: string }) => {
      const res = await fetch(API_BASE + '/api/services/app/Approval/ProcessApproval', {
        method: 'POST',
        headers: { ...getRequestHeaders(), 'Content-Type': 'application/json' },
        body: JSON.stringify(p),
      });
      if (handle401(res.status)) throw new Error('Unauthorized');
      if (!res.ok) throw new Error(await res.text());
      return res.json();
    },
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['pending-tasks'] }); setModal(null); setComment(''); setError(''); },
    // Onay islemi sessizce basarisiz olmamali: durum degismezse kullanici nedenini gormeli.
    onError: (e: unknown) => setError(parseApiError(e)),
  });

  const filtered = tasks.filter(t => {
    if (!filter) return true;
    const q = filter.toLowerCase();
    return t.entityType.toLowerCase().includes(q) || t.stepName.toLowerCase().includes(q) ||
      (t.formNo?.toLowerCase().includes(q)) || t.entityId.includes(q);
  });

  const handleAction = (task: PendingTask, action: string) => {
    if (action === 'Revise' || action === 'Reject') setModal({ task, action });
    else process.mutate({ approvalRecordId: task.approvalRecordId, action });
  };

  return (
    <div style={{ padding: 24 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <h1 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>My Tasks</h1>
          {tasks.length > 0 && (
            <span style={{ background: '#1976d2', color: '#fff', borderRadius: 12, padding: '2px 10px', fontSize: 13, fontWeight: 600 }}>
              {tasks.length}
            </span>
          )}
        </div>
        <input
          placeholder="Search tasks..."
          value={filter}
          onChange={e => setFilter(e.target.value)}
          style={{ padding: '8px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 13, width: 250 }}
        />
      </div>

      {error && (
        <div style={{ padding: '10px 14px', background: '#ffebee', border: '1px solid #ef9a9a', borderRadius: 6, color: '#c62828', fontSize: 13, marginBottom: 12 }}>
          {error}
        </div>
      )}

      {isLoading && <p style={{ color: '#888' }}>Loading...</p>}

      {!isLoading && filtered.length === 0 && (
        <div style={{ textAlign: 'center', padding: 60, color: '#888' }}>
          <div style={{ fontSize: 40, marginBottom: 8 }}>\u2713</div>
          <div style={{ fontSize: 16, fontWeight: 600, color: '#4caf50' }}>All caught up!</div>
          <div style={{ fontSize: 13, marginTop: 4 }}>No pending approvals.</div>
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {filtered.map(task => (
          <div key={task.approvalRecordId} style={{
            padding: '14px 16px', background: '#fff', borderBottom: '1px solid #f0f0f0',
            display: 'flex', alignItems: 'center', gap: 16,
          }}>
            <div style={{ width: 4, height: 36, borderRadius: 2, background: '#1976d2', flexShrink: 0 }} />
            <div
              style={{ flex: 1, cursor: routeForTask(task.entityType) ? 'pointer' : 'default' }}
              title={routeForTask(task.entityType) ? 'Kayda git' : undefined}
              onClick={() => {
                const route = routeForTask(task.entityType);
                if (route) navigate(route + '?focus=' + encodeURIComponent(task.entityId));
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 3 }}>
                <span style={{ fontSize: 10, padding: '1px 6px', borderRadius: 3, background: '#e3f2fd', color: '#1565c0', fontWeight: 600 }}>{task.entityType}</span>
                {task.formNo && <span style={{ fontSize: 12, fontWeight: 600 }}>{task.formNo}</span>}
                <span style={{ fontSize: 11, color: '#aaa' }}>#{task.entityId}</span>
              </div>
              <div style={{ fontSize: 13, fontWeight: 500, color: '#444' }}>{task.stepName}</div>
              <div style={{ fontSize: 11, color: '#aaa', marginTop: 2 }}>{task.creatorName ? task.creatorName + ' \u00B7 ' : ''}{timeAgo(task.creationTime)}</div>
            </div>
            <div style={{ display: 'flex', gap: 6 }}>
              {task.availableActions.map(a => (
                <button key={a} onClick={() => handleAction(task, a)} disabled={process.isPending}
                  style={{
                    padding: '6px 14px', borderRadius: 6, fontSize: 12, fontWeight: 600, cursor: 'pointer',
                    border: a === 'Approve' || a === 'Convenient' ? 'none' : '1px solid #ddd',
                    background: a === 'Approve' || a === 'Convenient' ? '#1976d2' : '#fff',
                    color: a === 'Approve' || a === 'Convenient' ? '#fff' : '#555',
                  }}>
                  {a}
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>

      {modal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div style={{ background: '#fff', borderRadius: 12, padding: 24, width: 460, boxShadow: '0 8px 32px rgba(0,0,0,0.2)' }}>
            <h3 style={{ margin: '0 0 12px' }}>{modal.action} — {modal.task.stepName}</h3>
            <textarea value={comment} onChange={e => setComment(e.target.value)} placeholder="Enter reason..."
              style={{ width: '100%', minHeight: 80, padding: 10, borderRadius: 6, border: '1px solid #ddd', fontSize: 13, boxSizing: 'border-box' }} />
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 12 }}>
              <button onClick={() => { setModal(null); setComment(''); }} style={{ padding: '6px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' }}>Cancel</button>
              <button onClick={() => process.mutate({ approvalRecordId: modal.task.approvalRecordId, action: modal.action, comment })}
                disabled={!comment.trim() || process.isPending}
                style={{ padding: '6px 16px', borderRadius: 6, border: 'none', background: '#1976d2', color: '#fff', cursor: 'pointer', fontWeight: 600 }}>
                Submit
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
