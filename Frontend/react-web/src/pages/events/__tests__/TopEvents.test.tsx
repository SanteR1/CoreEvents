import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, TopEventsPage } from '../TopEvents';
import { TopEventsForm } from '@/features/events/components/TopEventsForm';
import { getTopEvents } from '@/features/events/api/eventsApi';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse, TopEventsResult } from '@/features/events/api/eventsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  getTopEvents: vi.fn(),
}));

describe('TopEvents', () => {
  const mockTopEvent1: EventResponse = {
    id: 'top-1',
    title: 'React Summit 2026',
    description: 'Главная конференция по React',
    startAt: new Date('2026-10-15T09:00:00Z'),
    endAt: new Date('2026-10-15T18:00:00Z'),
    totalSeats: 200,
    availableSeats: 50,
  };

  const mockTopEvent2: EventResponse = {
    id: 'top-2',
    title: 'Rust Workshop',
    description: null,
    startAt: new Date('2026-10-16T10:00:00Z'),
    endAt: new Date('2026-10-16T14:00:00Z'),
    totalSeats: 30,
    availableSeats: 0,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('loader function', () => {
    it('returns { res } on successful getTopEvents call', async () => {
      const mockResult: TopEventsResult = {
        success: true,
        httpStatus: 200,
        event: [mockTopEvent1],
      };
      vi.mocked(getTopEvents).mockResolvedValueOnce(mockResult);

      const data = await loader();
      expect(data).toEqual({ res: mockResult });
    });

    it('catches Error and returns { error: err.message }', async () => {
      vi.mocked(getTopEvents).mockRejectedValueOnce(new Error('Connection timed out'));

      const data = await loader();
      expect(data).toEqual({ error: 'Connection timed out' });
    });

    it('catches non-Error thrown value and returns fallback error message', async () => {
      vi.mocked(getTopEvents).mockRejectedValueOnce('string error');

      const data = await loader();
      expect(data).toEqual({ error: 'Не удалось найти события' });
    });
  });

  describe('TopEventsPage Component & Navigation', () => {
    function renderTopEventsPage(resData: TopEventsResult | null = null) {
      const router = createMemoryRouter(
        [
          {
            path: '/events/topevents',
            element: <TopEventsPage />,
            loader: () => ({ res: resData }),
            HydrateFallback: () => null,
          },
          {
            path: '/bookings/create/:eventId',
            element: <div>Страница бронирования</div>,
          },
        ],
        { initialEntries: ['/events/topevents'] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders heading, description and top events list', async () => {
      renderTopEventsPage({
        success: true,
        httpStatus: 200,
        event: [mockTopEvent1, mockTopEvent2],
      });

      expect(await screen.findByRole('heading', { name: 'Топ событий' })).toBeInTheDocument();
      expect(screen.getByText('Список популярных событий')).toBeInTheDocument();

      expect(screen.getByText(mockTopEvent1.title)).toBeInTheDocument();
      expect(screen.getByText(mockTopEvent2.title)).toBeInTheDocument();
      expect(screen.getByText('50 / 200')).toBeInTheDocument();
      expect(screen.getByText('0 / 30')).toBeInTheDocument();
    });

    it('navigates to /bookings/create/:id when available event button is clicked', async () => {
      const user = userEvent.setup();

      renderTopEventsPage({
        success: true,
        httpStatus: 200,
        event: [mockTopEvent1],
      });

      const bookBtn = await screen.findByRole('button', { name: 'Забронировать' });
      await user.click(bookBtn);

      expect(await screen.findByText('Страница бронирования')).toBeInTheDocument();
    });

    it('disables button with text "Мест нет" for sold out event', async () => {
      renderTopEventsPage({
        success: true,
        httpStatus: 200,
        event: [mockTopEvent2],
      });

      const soldOutBtn = await screen.findByRole('button', { name: 'Мест нет' });
      expect(soldOutBtn).toBeDisabled();
    });

    it('renders empty when res.success is false or not present', async () => {
      const { container } = renderTopEventsPage(null);
      await waitFor(() => {
        expect(container).toBeEmptyDOMElement();
      });
    });
  });

  describe('TopEventsForm Component Isolated Tests', () => {
    it('renders placeholder when events array is empty or undefined', () => {
      const { rerender } = render(<TopEventsForm events={[]} />);
      expect(screen.getByText('В данный момент нет популярных событий.')).toBeInTheDocument();

      rerender(<TopEventsForm events={null as unknown as EventResponse[]} />);
      expect(screen.getByText('В данный момент нет популярных событий.')).toBeInTheDocument();
    });

    it('renders fallbacks for missing title, description, and dates', () => {
      const fallbackEvent: EventResponse = {
        id: 'top-fallback',
        title: '',
        description: null,
        startAt: null as unknown as Date,
        endAt: null as unknown as Date,
        totalSeats: 10,
        availableSeats: 5,
      };

      render(<TopEventsForm events={[fallbackEvent]} />);

      expect(screen.getByText('Без названия')).toBeInTheDocument();
      expect(screen.getByText('Описание отсутствует')).toBeInTheDocument();
      const notSpecified = screen.getAllByText('Не указано');
      expect(notSpecified.length).toBeGreaterThanOrEqual(2);
    });

    it('satisfies WCAG accessibility standards', async () => {
      const { container } = render(<TopEventsForm events={[mockTopEvent1]} />);
      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
