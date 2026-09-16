import { useState, useCallback } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { dataProvider } from '../dataProvider';

export interface SortState {
  field: string;
  order: 'ASC' | 'DESC';
}

export interface UseListQueryOptions {
  resource: string;
  defaultPerPage?: number;
  defaultSort?: SortState;
  /** Auto-refresh interval in seconds (0 = disabled) */
  refreshRate?: number;
}

export const useListQuery = <T extends Record<string, unknown>>(opts: UseListQueryOptions) => {
  const qc = useQueryClient();
  const [searchParams] = useSearchParams();
  const [page, setPage] = useState(1);
  const [perPage, setPerPage] = useState(opts.defaultPerPage ?? 20);
  const [sort, setSort] = useState<SortState | undefined>(opts.defaultSort);
  const [filters, setFilters] = useState<Record<string, string>>({});

  // URL query params → split into API filters vs display-only params
  const urlFilter: Record<string, string> = {};
  const displayParams: Record<string, string> = {};
  searchParams.forEach((value, key) => {
    // focus=<id> kayit odaklama icin; API'ye filtre olarak gitmemeli
    if (key === 'focus') return;
    if (key.startsWith('_d_')) {
      displayParams[key.slice(3)] = value;
    } else {
      urlFilter[key] = value;
    }
  });

  const mergedFilter = { ...urlFilter, ...filters };

  const { data, isLoading, isError, error } = useQuery({
    queryKey: [opts.resource, page, perPage, mergedFilter, sort],
    queryFn: () =>
      dataProvider.list<T>(opts.resource, {
        pagination: { page, perPage },
        sort,
        filter: Object.keys(mergedFilter).length > 0 ? mergedFilter : undefined,
      }),
    refetchInterval: opts.refreshRate ? opts.refreshRate * 1000 : undefined,
    staleTime: 30_000, // Don't refetch for 30 seconds
    refetchOnWindowFocus: false,
  });

  const records: T[] = data?.data ?? [];
  const total: number = data?.total ?? records.length;
  const invalidate = () => qc.invalidateQueries({ queryKey: [opts.resource] });

  // Handle TkTable onTkRequest (sorting + filtering only — pagination via TkPagination)
  const handleTableRequest = useCallback((e: CustomEvent) => {
    const req = e.detail as {
      sorts?: Array<{ field: string; order: 'asc' | 'desc' }>;
      filters?: Array<{ field: string; value?: string | string[] }>;
    };
    if (req.sorts && req.sorts.length > 0) {
      const s = req.sorts[0];
      setSort({ field: s.field, order: s.order === 'desc' ? 'DESC' : 'ASC' });
    } else if (req.sorts && req.sorts.length === 0) {
      setSort(undefined);
    }
    if (req.filters) {
      const f: Record<string, string> = {};
      const textParts: string[] = [];
      for (const fi of req.filters) {
        if (fi.value !== undefined && fi.value !== '' && fi.value !== null) {
          const val = Array.isArray(fi.value) ? fi.value.join(',') : String(fi.value);
          // text/search filters → merge into Keyword, other types → field-specific param
          if ((fi as any).type === 'text' || !(fi as any).type) {
            textParts.push(val);
          } else {
            f[fi.field] = val;
          }
        }
      }
      if (textParts.length > 0) f['Keyword'] = textParts.join(' ');
      setFilters(f);
      setPage(1);
    }
  }, []);

  return {
    records, total, isLoading, isError, error,
    page, setPage, perPage, setPerPage,
    sort, setSort, filters, setFilters,
    displayParams, invalidate, handleTableRequest,
  };
};
