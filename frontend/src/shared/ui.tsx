import React from 'react';
import { TkCard, TkAlert, TkButton, TkTabs, TkTabsItem, TkDialog } from '@takeoff-ui/react';

// Neutral UI wrappers — dashboards/shared components import these so the same
// generated JSX renders on any UI framework. This file is the Takeoff impl.

export const UiCard: React.FC<{ header?: string; bodyStyle?: React.CSSProperties; children?: React.ReactNode }> = ({ header, bodyStyle, children }) => (
  <TkCard {...(header ? { header } : {})}>
    <div slot="content" style={bodyStyle}>{children}</div>
  </TkCard>
);

export const UiAlert: React.FC<{ variant?: string; message?: string; header?: string }> = ({ variant = 'info', message, header }) => (
  <TkAlert variant={variant as never} message={message} {...(header ? { header } : {})} />
);

export const UiButton: React.FC<{ label?: string; variant?: string; onClick?: () => void; disabled?: boolean }> = ({ label, variant = 'primary', onClick, disabled }) => (
  <TkButton label={label} variant={variant as never} onTkClick={onClick} disabled={disabled} />
);

export const UiTabs: React.FC<{ children?: React.ReactNode }> = ({ children }) => (
  <TkTabs>{children}</TkTabs>
);

export const UiTabsItem: React.FC<{ label: string; children?: React.ReactNode }> = ({ label, children }) => (
  <TkTabsItem label={label}>{children}</TkTabsItem>
);

export const UiDialog: React.FC<{ visible: boolean; header?: string; onClose: () => void; footer?: React.ReactNode; children?: React.ReactNode }> = ({ visible, header, onClose, footer, children }) => (
  <TkDialog visible={visible} header={header} onTkClose={onClose}>
    <div slot="content" style={{ padding: '16px 0', fontSize: 14, color: '#333' }}>{children}</div>
    <div slot="footer" style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', padding: '12px 16px', width: '100%', boxSizing: 'border-box' }}>{footer}</div>
  </TkDialog>
);
