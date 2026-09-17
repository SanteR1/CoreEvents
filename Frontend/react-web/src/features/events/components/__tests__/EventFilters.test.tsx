import { describe, it, expect, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { EventFilters } from '../EventFilters';
import { checkA11y } from '@/shared/lib/test/axe';

function renderFilters(
  props: React.ComponentProps<typeof EventFilters> = {},
  loaderFn?: ({ request }: { request: Request }) => unknown,
) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <EventFilters {...props} />,
        ...(loaderFn ? { loader: loaderFn, HydrateFallback: () => null } : {}),
      },
    ],
    { initialEntries: ['/'] },
  );

  return render(<RouterProvider router={router} />);
}

describe('EventFilters', () => {
  describe('Form Fields & Initial Values', () => {
    it('renders with default empty fields and standard pageSize=6', () => {
      renderFilters();

      const titleInput = screen.getByLabelText('Название события');
      expect(titleInput).toHaveValue('');
      expect(titleInput).toHaveAttribute('name', 'title');

      const fromInput = screen.getByLabelText('Дата с');
      expect(fromInput).toHaveValue('');
      expect(fromInput).toHaveAttribute('name', 'from');

      const toInput = screen.getByLabelText('Дата по');
      expect(toInput).toHaveValue('');
      expect(toInput).toHaveAttribute('name', 'to');

      const hiddenPageSize = document.querySelector<HTMLInputElement>('input[name="pageSize"]')!;
      expect(hiddenPageSize).toBeInTheDocument();
      expect(hiddenPageSize.value).toBe('6');

      const hiddenPage = document.querySelector<HTMLInputElement>('input[name="page"]')!;
      expect(hiddenPage).toBeInTheDocument();
      expect(hiddenPage.value).toBe('1');
    });

    it('populates initial values from props and respects custom pageSize', () => {
      renderFilters({
        initialTitle: 'Конференция',
        initialFrom: '2026-07-01',
        initialTo: '2026-07-10',
        pageSize: 12,
      });

      expect(screen.getByLabelText('Название события')).toHaveValue('Конференция');
      expect(screen.getByLabelText('Дата с')).toHaveValue('2026-07-01');
      expect(screen.getByLabelText('Дата по')).toHaveValue('2026-07-10');

      const hiddenPageSize = document.querySelector<HTMLInputElement>('input[name="pageSize"]')!;
      expect(hiddenPageSize.value).toBe('12');
    });
  });

  describe('Reset Filters Link', () => {
    it('does NOT display "Сбросить" when there are no active filters', () => {
      renderFilters();
      expect(screen.queryByRole('link', { name: 'Сбросить' })).not.toBeInTheDocument();
    });

    it('displays "Сбросить" link leading to "/" when initialTitle is set', () => {
      renderFilters({ initialTitle: 'AI Fest' });
      const resetLink = screen.getByRole('link', { name: 'Сбросить' });
      expect(resetLink).toBeInTheDocument();
      expect(resetLink).toHaveAttribute('href', '/');
    });

    it('displays "Сбросить" link when initialFrom is set', () => {
      renderFilters({ initialFrom: '2026-01-01' });
      expect(screen.getByRole('link', { name: 'Сбросить' })).toBeInTheDocument();
    });

    it('displays "Сбросить" link when initialTo is set', () => {
      renderFilters({ initialTo: '2026-12-31' });
      expect(screen.getByRole('link', { name: 'Сбросить' })).toBeInTheDocument();
    });
  });

  describe('Form Submission & Searching State', () => {
    it('shows loading spinner and disables submit button while searching', async () => {
      const user = userEvent.setup();
      let resolveSearch: () => void = vi.fn();
      const pendingSearch = new Promise<void>((resolve) => {
        resolveSearch = resolve;
      });

      renderFilters({}, async ({ request }) => {
        const url = new URL(request.url);
        if (url.searchParams.has('title')) {
          await pendingSearch;
        }
        return null;
      });

      const input = await screen.findByLabelText('Название события');
      await user.type(input, 'Testing');

      const submitBtn = screen.getByRole('button', { name: 'Применить' });
      await user.click(submitBtn);

      // Now navigation state is loading with search params
      expect(await screen.findByRole('button', { name: /поиск\.\.\./i })).toBeDisabled();

      await act(async () => {
        resolveSearch();
        await pendingSearch;
      });

      // After resolution, reverts back to "Применить"
      expect(await screen.findByRole('button', { name: 'Применить' })).toBeEnabled();
    });
  });

  describe('Accessibility', () => {
    it('satisfies WCAG accessibility standards with active filters', async () => {
      const { container } = renderFilters({
        initialTitle: 'Конференция',
        initialFrom: '2026-07-01',
        initialTo: '2026-07-10',
      });

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
