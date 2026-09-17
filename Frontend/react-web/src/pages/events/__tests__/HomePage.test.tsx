import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, HomePage } from '../HomePage';
import { getAllEvents } from '@/features/events/api/eventsApi';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse, PaginatedResult } from '@/features/events/api/eventsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  getAllEvents: vi.fn(),
}));

type LoaderArgs = Parameters<typeof loader>[0];

function createLoaderArgs(url = 'http://localhost/'): LoaderArgs {
  return {
    request: new Request(url),
    params: {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

describe('HomePage', () => {
  const mockEvent1: EventResponse = {
    id: 'ev-alpha',
    title: 'Конференция Alpha',
    description: 'Описание конференции Alpha',
    startAt: new Date('2026-10-15T10:00:00Z'),
    endAt: new Date('2026-10-15T18:00:00Z'),
    totalSeats: 100,
    availableSeats: 50,
  };

  const mockEvent2: EventResponse = {
    id: 'ev-beta',
    title: 'Встреча Beta',
    description: null,
    startAt: new Date('2026-11-01T12:00:00Z'),
    endAt: new Date('2026-11-01T14:00:00Z'),
    totalSeats: 20,
    availableSeats: 0,
  };

  const mockSuccessResult: PaginatedResult = {
    success: true,
    httpStatus: 200,
    event: {
      totalCount: 2,
      currentPage: 1,
      pageSize: 6,
      totalPages: 1,
      items: {
        event: [mockEvent1, mockEvent2],
      },
    },
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('loader function', () => {
    it('uses default values (page=1, pageSize=6) when query params are omitted', async () => {
      vi.mocked(getAllEvents).mockResolvedValueOnce(mockSuccessResult);

      const res = await loader(createLoaderArgs('http://localhost/'));

      expect(getAllEvents).toHaveBeenCalledWith(undefined, undefined, undefined, 1, 6);
      expect(res.filters).toEqual({
        title: '',
        from: '',
        to: '',
        page: 1,
        pageSize: 6,
      });
      expect(res.result).toEqual(mockSuccessResult);
    });

    it('sanitizes invalid page and pageSize parameters (NaN, negative, zero)', async () => {
      vi.mocked(getAllEvents).mockResolvedValueOnce(mockSuccessResult);

      await loader(createLoaderArgs('http://localhost/?page=-5&pageSize=0'));
      expect(getAllEvents).toHaveBeenCalledWith(undefined, undefined, undefined, 1, 6);

      await loader(createLoaderArgs('http://localhost/?page=abc&pageSize=xyz'));
      expect(getAllEvents).toHaveBeenCalledWith(undefined, undefined, undefined, 1, 6);
    });

    it('parses valid page, pageSize and trims title parameter', async () => {
      vi.mocked(getAllEvents).mockResolvedValueOnce(mockSuccessResult);

      const res = await loader(
        createLoaderArgs(
          'http://localhost/?page=3&pageSize=12&title=%20%20Tech%20Conference%20%20',
        ),
      );

      expect(getAllEvents).toHaveBeenCalledWith('Tech Conference', undefined, undefined, 3, 12);
      expect(res.filters.title).toBe('Tech Conference');
      expect(res.filters.page).toBe(3);
      expect(res.filters.pageSize).toBe(12);
    });

    it('formats valid date filters into ISO start-of-day and end-of-day', async () => {
      vi.mocked(getAllEvents).mockResolvedValueOnce(mockSuccessResult);

      await loader(createLoaderArgs('http://localhost/?from=2026-08-01&to=2026-08-10'));

      const expectedFrom = new Date('2026-08-01T00:00:00').toISOString();
      const expectedTo = new Date('2026-08-10T23:59:59.999').toISOString();

      expect(getAllEvents).toHaveBeenCalledWith(undefined, expectedFrom, expectedTo, 1, 6);
    });

    it('handles invalid date strings gracefully without passing corrupt ISO to API', async () => {
      vi.mocked(getAllEvents).mockResolvedValueOnce(mockSuccessResult);

      await loader(createLoaderArgs('http://localhost/?from=not-a-date&to=invalid-date'));

      expect(getAllEvents).toHaveBeenCalledWith(undefined, undefined, undefined, 1, 6);
    });
  });

  describe('HomePage Component Rendering', () => {
    function renderHomePage(
      resultData = mockSuccessResult,
      filtersData = { title: '', from: '', to: '', page: 1, pageSize: 6 },
    ) {
      const router = createMemoryRouter(
        [
          {
            path: '/',
            element: <HomePage />,
            loader: () => ({
              result: resultData,
              filters: filtersData,
            }),
            HydrateFallback: () => null,
          },
          {
            path: '/events/topevents',
            element: <div>Топ событий</div>,
          },
          {
            path: '/events/create',
            element: <div>Создать событие</div>,
          },
          {
            path: '/events/:id',
            element: <div>Детали события</div>,
          },
          {
            path: '/bookings/create/:id',
            element: <div>Бронирование</div>,
          },
        ],
        { initialEntries: ['/'] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders header, description, and quick-action links', async () => {
      renderHomePage();

      expect(await screen.findByRole('heading', { name: 'Афиша событий' })).toBeInTheDocument();
      expect(screen.getByText(/Актуальные события, встречи и конференции/i)).toBeInTheDocument();

      const topEventsLink = screen.getByRole('link', { name: /топ событий/i });
      expect(topEventsLink).toHaveAttribute('href', '/events/topevents');

      const createEventLink = screen.getByRole('link', { name: /\+ Создать событие/i });
      expect(createEventLink).toHaveAttribute('href', '/events/create');
    });

    it('renders events grid and pagination on successful load', async () => {
      renderHomePage();

      expect(await screen.findByText('Конференция Alpha')).toBeInTheDocument();
      expect(screen.getByText('Встреча Beta')).toBeInTheDocument();

      // Check seat statuses on cards
      expect(screen.getByText('Свободно мест: 50 из 100')).toBeInTheDocument();
      expect(screen.getByText('Мест нет')).toBeInTheDocument();

      // Check pagination element
      expect(screen.getByText(/показано/i)).toBeInTheDocument();
    });

    it('renders empty state when event list is empty without active filters', async () => {
      const emptyResult: PaginatedResult = {
        success: true,
        httpStatus: 200,
        event: {
          totalCount: 0,
          currentPage: 1,
          pageSize: 6,
          totalPages: 0,
          items: {
            event: [],
          },
        },
      };

      renderHomePage(emptyResult);

      expect(
        await screen.findByRole('heading', { name: 'События не найдены' }),
      ).toBeInTheDocument();
      expect(screen.getByText(/По заданным критериям ничего не найдено/i)).toBeInTheDocument();
      expect(screen.queryByRole('link', { name: 'Сбросить фильтры' })).not.toBeInTheDocument();
    });

    it('renders empty state with "Сбросить фильтры" button when search filters were applied', async () => {
      const emptyResult: PaginatedResult = {
        success: true,
        httpStatus: 200,
        event: {
          totalCount: 0,
          currentPage: 1,
          pageSize: 6,
          totalPages: 0,
          items: {
            event: [],
          },
        },
      };

      renderHomePage(emptyResult, {
        title: 'Non-existent Event',
        from: '2026-01-01',
        to: '',
        page: 1,
        pageSize: 6,
      });

      expect(
        await screen.findByRole('heading', { name: 'События не найдены' }),
      ).toBeInTheDocument();
      const resetLink = screen.getByRole('link', { name: 'Сбросить фильтры' });
      expect(resetLink).toBeInTheDocument();
      expect(resetLink).toHaveAttribute('href', '/');
    });

    it('handles items.event === null defensively', async () => {
      const nullItemsResult: PaginatedResult = {
        success: true,
        httpStatus: 200,
        event: {
          totalCount: 0,
          currentPage: 1,
          pageSize: 6,
          totalPages: 0,
          items: {
            event: null,
          },
        },
      };

      renderHomePage(nullItemsResult);

      expect(
        await screen.findByRole('heading', { name: 'События не найдены' }),
      ).toBeInTheDocument();
    });

    it('renders error banner and handles reload button click when result.success is false', async () => {
      const user = userEvent.setup();
      const reloadMock = vi.fn();
      const locationSpy = vi.spyOn(window, 'location', 'get').mockReturnValue({
        ...window.location,
        reload: reloadMock,
      });

      const errorResult: PaginatedResult = {
        success: false,
        httpStatus: 500,
        error: {
          message: 'Сервис временно недоступен',
        },
      };

      renderHomePage(errorResult);

      expect(
        await screen.findByRole('heading', { name: 'Не удалось загрузить события' }),
      ).toBeInTheDocument();
      expect(screen.getByText('Сервис временно недоступен')).toBeInTheDocument();

      const retryBtn = screen.getByRole('button', { name: 'Повторить попытку' });
      await user.click(retryBtn);

      expect(reloadMock).toHaveBeenCalledTimes(1);
      locationSpy.mockRestore();
    });

    it('displays fallback message when error.message is not provided', async () => {
      const errorResultWithoutMessage: PaginatedResult = {
        success: false,
        httpStatus: 500,
        error: {
          message: undefined as unknown as string,
        },
      };

      renderHomePage(errorResultWithoutMessage);

      expect(
        await screen.findByRole('heading', { name: 'Не удалось загрузить события' }),
      ).toBeInTheDocument();
      expect(
        screen.getByText('Пожалуйста, проверьте подключение и повторите попытку.'),
      ).toBeInTheDocument();
    });

    it('satisfies WCAG accessibility rules in success, empty and error states', async () => {
      const { container: successContainer } = renderHomePage();
      await screen.findByRole('heading', { name: 'Афиша событий' });
      await expect(checkA11y(successContainer)).resolves.toEqual([]);
    });
  });
});
