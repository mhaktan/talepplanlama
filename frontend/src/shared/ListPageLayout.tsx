import React, { useRef, useEffect } from 'react';
import { TkTable, TkPagination } from '@takeoff-ui/react';
import { registerCrudHandler } from './ActionButtons';

// Re-export column type so screens can import from one place
export interface TableColumn {
  field: string;
  header: string;
  sortable?: boolean;
  searchable?: boolean;
  filterType?: 'text' | 'checkbox' | 'radio' | 'datepicker';
  filterOptions?: Array<{ label: string; value: string | number }>;
  html?: (row: Record<string, unknown>) => string;
}

interface ListPageLayoutProps {
  title: string;
  subtitle?: React.ReactNode;
  records: Record<string, unknown>[];
  columns: TableColumn[];
  dataKey: string;
  total: number;
  loading?: boolean;
  page: number;
  perPage: number;
  onPageChange: (page: number) => void;
  onPerPageChange: (perPage: number) => void;
  onTableRequest: (e: CustomEvent) => void;
  selectionMode?: 'checkbox';
  selectedRows?: Record<string, unknown>[];
  onSelectionChange?: (rows: Record<string, unknown>[]) => void;
  headerActions?: React.ReactNode;
  onCrudAction?: (action: string, id: string) => void;
}

export const ListPageLayout: React.FC<ListPageLayoutProps> = ({
  title, subtitle, records, columns, dataKey,
  total, loading, page, perPage,
  onPageChange, onPerPageChange, onTableRequest,
  selectionMode, selectedRows, onSelectionChange,
  headerActions, onCrudAction,
}) => {
  const tableRef = useRef<HTMLTkTableElement>(null);

  // Hide TkTable's built-in pagination
  useEffect(() => {
    const el = tableRef.current;
    if (!el) return;
    const hide = () => {
      const shadow = el.shadowRoot;
      if (!shadow) return;
      let style = shadow.querySelector('#hide-pagination');
      if (!style) {
        style = document.createElement('style');
        style.id = 'hide-pagination';
        style.textContent = 'tk-pagination, .pagination-wrapper, [class*="pagination"] { display: none !important; }';
        shadow.appendChild(style);
      }
    };
    hide();
    const t = setTimeout(hide, 500);
    return () => clearTimeout(t);
  }, [records]);

  // Listen for sort/filter via native DOM event on the tk-table element
  useEffect(() => {
    const el = tableRef.current;
    if (!el) return;
    const handler = (e: Event) => {
      const detail = (e as CustomEvent).detail;
      if (!detail) return;
      const normalized: Record<string, unknown> = {};
      if (detail.sortField || (detail.sorts && detail.sorts.length > 0)) {
        const sorts = detail.sorts && detail.sorts.length > 0
          ? detail.sorts
          : detail.sortField ? [{ field: detail.sortField, order: detail.sortOrder || 'asc' }] : [];
        normalized.sorts = sorts;
      }
      if (detail.filters) {
        normalized.filters = detail.filters;
      }
      onTableRequest(new CustomEvent('tk-request', { detail: normalized }));
    };
    el.addEventListener('tk-request', handler);
    return () => el.removeEventListener('tk-request', handler);
  }, [onTableRequest]);

  // Register global handler for CRUD action buttons (works through shadow DOM)
  useEffect(() => {
    if (!onCrudAction) return;
    return registerCrudHandler(onCrudAction);
  }, [onCrudAction]);

  return (
    <div>
      {/* Page header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h1 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>{title}</h1>
          {subtitle}
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          {headerActions}
        </div>
      </div>

      <div style={{ overflowX: 'auto', minWidth: 0 }}>
        <TkTable
          ref={tableRef}
          data={records}
          columns={columns as any}
          dataKey={dataKey}
          loading={loading}
          striped
          size="small"
          paginationMethod="server"
          totalItems={total}
          {...(selectionMode ? {
            selectionMode,
            selection: selectedRows,
            onTkSelectionChange: (e: CustomEvent) => onSelectionChange?.(Array.isArray(e.detail) ? e.detail : []),
          } : {})}
          onTkRequest={onTableRequest}
        />
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 12 }}>
        <TkPagination
          totalItems={total}
          rowsPerPage={perPage}
          currentPage={page}
          rowsPerPageOptions={[10, 20, 50, 100]}
          onTkPageChange={(e: CustomEvent) => onPageChange(e.detail.page)}
          onTkRowsPerPageChange={(e: CustomEvent) => { onPerPageChange(e.detail); onPageChange(1); }}
        />
      </div>
    </div>
  );
};
