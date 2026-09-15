import { describe, it, expect } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { EventPagination } from '../EventPagination';
import { checkA11y } from '@/shared/lib/test/axe';

function renderPagination(props: React.ComponentProps<typeof EventPagination>) {
  return render(
    <MemoryRouter>
      <EventPagination {...props} />
    </MemoryRouter>,
  );
}

describe('EventPagination', () => {
  const baseProps = {
    currentPage: 1,
    totalPages: 5,
    totalCount: 30,
    pageSize: 6,
    filters: {},
  };

  describe('Page Numbering Algorithm (getPageNumbers)', () => {
    it('renders all page numbers without ellipsis when totalPages <= 7', () => {
      renderPagination({ ...baseProps, totalPages: 5, currentPage: 3 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '2' })).toBeInTheDocument();
      // Current page is rendered as non-link active span
      expect(within(controls).getByText('3', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).queryByRole('link', { name: '3' })).not.toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '4' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '5' })).toBeInTheDocument();
      expect(within(controls).queryByText('...')).not.toBeInTheDocument();
    });

    it('renders exactly 7 numbers when totalPages = 7 without ellipsis', () => {
      renderPagination({ ...baseProps, totalPages: 7, currentPage: 4 });

      const controls = screen.getByText('← Назад').parentElement!;
      for (let i = 1; i <= 7; i++) {
        if (i === 4) {
          expect(within(controls).getByText('4', { selector: 'span' })).toBeInTheDocument();
        } else {
          expect(within(controls).getByRole('link', { name: String(i) })).toBeInTheDocument();
        }
      }
      expect(within(controls).queryByText('...')).not.toBeInTheDocument();
    });

    it('does not render page number buttons when totalPages = 1', () => {
      renderPagination({ ...baseProps, totalPages: 1, currentPage: 1, totalCount: 4 });

      expect(screen.queryByRole('link', { name: '1' })).not.toBeInTheDocument();
      expect(screen.queryByText('← Назад')).not.toBeInTheDocument();
      expect(screen.queryByText('Вперед →')).not.toBeInTheDocument();
      // But page info and page sizes are still displayed
      expect(screen.getByText(/показано/i)).toBeInTheDocument();
      expect(screen.getByText('Показывать по:')).toBeInTheDocument();
    });

    it('renders [1, 2, "...", 10] when totalPages = 10 and currentPage = 1', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 1 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByText('1', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '2' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(1);
      expect(within(controls).queryByRole('link', { name: '3' })).not.toBeInTheDocument();
    });

    it('renders [1, 2, 3, "...", 10] when totalPages = 10 and currentPage = 2', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 2 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByText('2', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '3' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(1);
    });

    it('renders [1, 2, 3, 4, "...", 10] when totalPages = 10 and currentPage = 3', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 3 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '2' })).toBeInTheDocument();
      expect(within(controls).getByText('3', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '4' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(1);
    });

    it('renders [1, "...", 3, 4, 5, "...", 10] when totalPages = 10 and currentPage = 4 (both ellipses)', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 4 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '3' })).toBeInTheDocument();
      expect(within(controls).getByText('4', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '5' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(2);
    });

    it('renders [1, "...", 4, 5, 6, "...", 10] when totalPages = 10 and currentPage = 5', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 5 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '4' })).toBeInTheDocument();
      expect(within(controls).getByText('5', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '6' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(2);
    });

    it('renders [1, "...", 7, 8, 9, 10] when totalPages = 10 and currentPage = 8 (no trailing ellipsis)', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 8 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '7' })).toBeInTheDocument();
      expect(within(controls).getByText('8', { selector: 'span' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '9' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '10' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(1);
    });

    it('renders [1, "...", 9, 10] when totalPages = 10 and currentPage = 10', () => {
      renderPagination({ ...baseProps, totalPages: 10, currentPage: 10 });
      const controls = screen.getByText('← Назад').parentElement!;

      expect(within(controls).getByRole('link', { name: '1' })).toBeInTheDocument();
      expect(within(controls).getByRole('link', { name: '9' })).toBeInTheDocument();
      expect(within(controls).getByText('10', { selector: 'span' })).toBeInTheDocument();

      const dots = within(controls).getAllByText('...');
      expect(dots).toHaveLength(1);
    });
  });

  describe('Item Range Calculations', () => {
    it('shows "Страница X из Y" when totalCount = 0', () => {
      renderPagination({ ...baseProps, totalCount: 0, currentPage: 1, totalPages: 1 });

      expect(screen.getByText('Страница 1 из 1')).toBeInTheDocument();
    });

    it('falls back to "Страница 1 из 1" when totalCount = 0 and totalPages = 0', () => {
      renderPagination({ ...baseProps, totalCount: 0, currentPage: 1, totalPages: 0 });

      expect(screen.getByText('Страница 1 из 1')).toBeInTheDocument();
    });

    it('shows correct range "1–6 из 15" on first page', () => {
      renderPagination({ ...baseProps, totalCount: 15, pageSize: 6, currentPage: 1 });

      expect(screen.getByText('1–6')).toBeInTheDocument();
      expect(screen.getByText('15')).toBeInTheDocument();
    });

    it('shows correct range "13–15 из 15" on last partial page', () => {
      renderPagination({
        ...baseProps,
        totalCount: 15,
        pageSize: 6,
        currentPage: 3,
        totalPages: 3,
      });

      expect(screen.getByText('13–15')).toBeInTheDocument();
      expect(screen.getByText('15')).toBeInTheDocument();
    });
  });

  describe('Navigation Buttons (Prev / Next)', () => {
    it('disables "← Назад" on first page and enables "Вперед →"', () => {
      renderPagination({ ...baseProps, currentPage: 1, totalPages: 3 });

      const prev = screen.getByText('← Назад');
      expect(prev.tagName).toBe('SPAN');
      expect(prev).toHaveClass('cursor-not-allowed');

      const next = screen.getByRole('link', { name: 'Вперед →' });
      expect(next).toHaveAttribute('href', '/?page=2');
    });

    it('enables both "← Назад" and "Вперед →" on middle page', () => {
      renderPagination({ ...baseProps, currentPage: 2, totalPages: 3 });

      const prev = screen.getByRole('link', { name: '← Назад' });
      expect(prev).toHaveAttribute('href', '/'); // page 1 is omitted

      const next = screen.getByRole('link', { name: 'Вперед →' });
      expect(next).toHaveAttribute('href', '/?page=3');
    });

    it('disables "Вперед →" on last page and enables "← Назад"', () => {
      renderPagination({ ...baseProps, currentPage: 3, totalPages: 3 });

      const prev = screen.getByRole('link', { name: '← Назад' });
      expect(prev).toHaveAttribute('href', '/?page=2');

      const next = screen.getByText('Вперед →');
      expect(next.tagName).toBe('SPAN');
      expect(next).toHaveClass('cursor-not-allowed');
    });
  });

  describe('Page Size Selector and URL Builder', () => {
    it('renders page size selector options 6, 12, 24 with active styling', () => {
      renderPagination({ ...baseProps, pageSize: 12 });

      const size12 = screen.getByRole('link', { name: '12' });
      expect(size12).toHaveClass('font-bold');

      const size6 = screen.getByRole('link', { name: '6' });
      expect(size6).not.toHaveClass('font-bold');
      expect(size6).toHaveAttribute('href', '/');

      const size24 = screen.getByRole('link', { name: '24' });
      expect(size24).toHaveAttribute('href', '/?pageSize=24');
    });

    it('preserves search filters (title, from, to) in pagination and size links', () => {
      renderPagination({
        ...baseProps,
        currentPage: 1,
        totalPages: 4,
        pageSize: 6,
        filters: {
          title: 'React Meetup',
          from: '2026-10-01',
          to: '2026-10-05',
        },
      });

      const page2Link = screen.getByRole('link', { name: '2' });
      const href = page2Link.getAttribute('href');
      expect(href).toContain('page=2');
      expect(href).toContain('title=React+Meetup');
      expect(href).toContain('from=2026-10-01');
      expect(href).toContain('to=2026-10-05');

      const size12Link = screen.getByRole('link', { name: '12' });
      const sizeHref = size12Link.getAttribute('href');
      expect(sizeHref).toContain('pageSize=12');
      expect(sizeHref).toContain('title=React+Meetup');
      // page 1 should be omitted from query
      expect(sizeHref).not.toContain('page=');
    });
  });

  describe('Accessibility', () => {
    it('satisfies WCAG accessibility rules', async () => {
      const { container } = renderPagination({
        ...baseProps,
        currentPage: 2,
        totalPages: 5,
      });

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
