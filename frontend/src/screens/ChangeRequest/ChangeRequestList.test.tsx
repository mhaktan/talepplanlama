import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ChangeRequestList } from './ChangeRequestList';

// Mock dataProvider
vi.mock('../../dataProvider', () => ({
  dataProvider: {
    list: vi.fn().mockResolvedValue({ data: [], total: 0 }),
    create: vi.fn().mockResolvedValue({}),
    update: vi.fn().mockResolvedValue({}),
    delete: vi.fn().mockResolvedValue(undefined),
  },
  isAuthenticated: () => true,
  getAuthToken: () => 'test-token',
}));

// Mock Takeoff UI components
vi.mock('@takeoff-ui/react', () => ({
  TkButton: ({ label, ...props }: any) => <button {...props}>{label}</button>,
  TkTable: ({ data, columns, ...props }: any) => (
    <table><tbody>{(data || []).map((r: any, i: number) => <tr key={i}><td>{JSON.stringify(r)}</td></tr>)}</tbody></table>
  ),
  TkPagination: () => <div data-testid="pagination" />,
  TkDialog: ({ children }: any) => <div>{children}</div>,
  TkInput: (props: any) => <input {...props} />,
  TkSelect: (props: any) => <select {...props} />,
  TkDatepicker: (props: any) => <input type="date" {...props} />,
  TkTextarea: (props: any) => <textarea {...props} />,
  TkSpinner: () => <div>Loading...</div>,
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <MemoryRouter>
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  </MemoryRouter>
);

describe('ChangeRequestList', () => {
  it('should render without crashing', () => {
    const { container } = render(<ChangeRequestList />, { wrapper });
    expect(container).toBeDefined();
  });

  it('should show title', () => {
    render(<ChangeRequestList />, { wrapper });
    expect(screen.getByText('ChangeRequest')).toBeDefined();
  });

  it('should have create button', () => {
    render(<ChangeRequestList />, { wrapper });
    expect(screen.getByText('+ Create ChangeRequest')).toBeDefined();
  });

});
