import React, { useState, useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { TkButton, TkDatepicker, TkInput, TkSelect } from '@takeoff-ui/react';
import { dataProvider } from '../../dataProvider';
import { overlayStyle, modalStyle } from '../../styles';
import { LookupSelect } from '../../shared/LookupSelect';
import { useFlows } from '../../flows/FlowProvider';

type ChangeRequestRecord = {
  id: string | number;
  title: string;
  requestNumber?: string;
  description: string;
  justification?: string;
  effectiveDate?: string;
  status: string;
  firstApproverRole: string;
  secondApproverRole: string;
  revisionNote?: string;
  requestTypeId: string;
};

interface ChangeRequestEditProps {
  record: ChangeRequestRecord | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const ChangeRequestEdit: React.FC<ChangeRequestEditProps> = ({ record, onClose, onSuccess }) => {
  const [form, setForm] = useState<Partial<ChangeRequestRecord>>(record ?? {});
  const setField = (name: string, value: unknown) => setForm((p) => ({ ...p, [name]: value }));
  const { triggerFlows } = useFlows();

  useEffect(() => {
    if (record) setForm({ ...record });
  }, [record]);

  const mutation = useMutation({
    mutationFn: (values: Partial<ChangeRequestRecord>) =>
      dataProvider.update('ChangeRequest', record!.id, values),
    onSuccess: (_data, values) => { triggerFlows('update', 'ChangeRequest', values as Record<string, unknown>); onSuccess(); onClose(); },
    onError: (err: Error) => { window.dispatchEvent(new CustomEvent('app-toast', { detail: { type: 'error', message: err.message } })); },
  });

  if (!record) return null;

  return (
    <div style={overlayStyle} onClick={onClose}>
      <div style={modalStyle} onClick={(e) => e.stopPropagation()}>
        <div style={{ padding: '20px 28px 0', flexShrink: 0, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2 style={{ margin: 0, fontSize: 18, fontWeight: 700 }}>Edit ChangeRequest</h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: 20, cursor: 'pointer', color: '#666', padding: '4px 8px', borderRadius: 4 }} onMouseOver={(e) => (e.currentTarget.style.color = '#333')} onMouseOut={(e) => (e.currentTarget.style.color = '#666')}>✕</button>
        </div>
        <form onSubmit={(e) => { e.preventDefault(); mutation.mutate(form); }} style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
          <div style={{ flex: 1, overflowY: 'auto', padding: '20px 28px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div>
                  <TkInput mode="text" label="Title *" value={String(form.title ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('title', v))(e.detail)} />
                </div>
                <div>
                  <TkInput mode="text" label="Request Number" value={String(form.requestNumber ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('requestNumber', v))(e.detail)} />
                </div>
                <div>
                  <TkInput mode="text" label="Description *" value={String(form.description ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('description', v))(e.detail)} />
                </div>
                <div>
                  <TkInput mode="text" label="Justification" value={String(form.justification ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('justification', v))(e.detail)} />
                </div>
                <div>
                  <TkDatepicker label="Effective Date" value={String(form.effectiveDate ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('effectiveDate', v))(e.detail)} />
                </div>
                <div>
                  <LookupSelect label="Status *" value={String(form.status ?? '')} onChange={(v) => setField('status', v ? Number(v) : null)} searchable={false} options={[{ label: 'Draft', value: '0' }, { label: 'PendingFirstApproval', value: '1' }, { label: 'PendingSecondApproval', value: '2' }, { label: 'Revision', value: '3' }, { label: 'PendingRoutePlanning', value: '4' }, { label: 'PendingSystem', value: '5' }, { label: 'PendingOperations', value: '6' }, { label: 'Completed', value: '7' }, { label: 'Cancelled', value: '8' }]} />
                </div>
                <div>
                  <LookupSelect label="First Approver Role *" resource="AppRole" value={String(form.firstApproverRole ?? '')} onChange={(v) => setField('firstApproverRole', v)} displayField="displayName" valueField="name" />
                </div>
                <div>
                  <LookupSelect label="Second Approver Role *" resource="AppRole" value={String(form.secondApproverRole ?? '')} onChange={(v) => setField('secondApproverRole', v)} displayField="displayName" valueField="name" />
                </div>
                <div>
                  <TkInput mode="text" label="Revision Note" value={String(form.revisionNote ?? '')} onTkChange={(e: CustomEvent) => ((v) => setField('revisionNote', v))(e.detail)} />
                </div>
                <div>
                  <LookupSelect label="Talep Tipi *" resource="RequestType" value={String(form.requestTypeId ?? '')} onChange={(v) => setField('requestTypeId', v)} displayField="name" />
                </div>
            </div>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, padding: '16px 28px', borderTop: '1px solid #e8e8e8', flexShrink: 0, background: '#fff' }}>
            <TkButton label="Cancel" variant="secondary" onTkClick={onClose} />
            <TkButton label={mutation.isPending ? 'Saving…' : 'Save Changes'} variant="primary" mode="submit" disabled={mutation.isPending} />
          </div>
        </form>
      </div>
    </div>
  );
};
