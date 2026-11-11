import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, MyBookingsPage } from '../MyBookingsPage';
import { getMyBookings, type BookingResponse } from '@/features/bookings/api/bookingsApi';
import { setToken, clearToken } from '@/shared/lib/auth';
import { checkA11y } from '@/shared/lib/test/axe';

vi.mock('@/features/bookings/api/bookingsApi', () => ({
  getMyBookings: vi.fn(),
}));

function createMockJwt(expSecondsFromNow = 3600): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({
      nameid: 'user_123',
      unique_name: 'testuser',
      role: 'User',
      exp: Math.floor(Date.now() / 1000) + expSecondsFromNow,
    }),
  );
  return `${header}.${payload}.mockSignature`;
}

type LoaderArgs = Parameters<typeof loader>[0];

function createLoaderArgs(url = 'http://localhost/bookings'): LoaderArgs {
  return {
    request: new Request(url),
    params: {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

describe('MyBookingsPage', () => {
  const mockBooking1: BookingResponse = {
    id: 'bk-11111111-aaaa',
    eventId: 'ev-1',
    userId: 'u-1',
    seats: 2,
    unitPrice: 1500,
    totalPrice: 3000,
    discountAmount: 0,
    currency: 'KZT',
    status: 'Confirmed',
    createdAt: new Date('2026-06-01T12:00:00Z'),
    processedAt: null,
    cancellationRequestedAt: null,
    rejectionReason: null,
    cancellationReason: null,
  };

  const mockBooking2: BookingResponse = {
    id: 'bk-22222222-bbbb',
    eventId: 'ev-2',
    userId: 'u-1',
    seats: 1,
    unitPrice: 5000,
    totalPrice: 5000,
    discountAmount: 0,
    currency: 'KZT',
    status: 'Pending',
    createdAt: new Date('2026-06-02T15:30:00Z'),
    processedAt: null,
    cancellationRequestedAt: null,
    rejectionReason: null,
    cancellationReason: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    clearToken();
  });

  afterEach(() => {
    clearToken();
    vi.restoreAllMocks();
  });

  describe('loader', () => {
    it('redirects unauthenticated users to login with returnUrl', async () => {
      clearToken();
      const args = createLoaderArgs('http://localhost/bookings?status=Confirmed');
      const result = await loader(args);

      expect(result).toBeInstanceOf(Response);
      const res = result as Response;
      expect(res.status).toBe(302);
      expect(res.headers.get('Location')).toBe('/login?returnUrl=%2Fbookings%3Fstatus%3DConfirmed');
    });

    it('calls getMyBookings with default params when no filters passed', async () => {
      setToken(createMockJwt());
      vi.mocked(getMyBookings).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        result: {
          items: [mockBooking1],
          totalCount: 1,
          currentPage: 1,
          pageSize: 10,
          totalPages: 1,
        },
      });

      const args = createLoaderArgs('http://localhost/bookings');
      const data = await loader(args);

      expect(getMyBookings).toHaveBeenCalledWith({
        status: undefined,
        page: 1,
        pageSize: 10,
      });
      expect(data).toMatchObject({
        currentStatus: '',
        page: 1,
      });
    });

    it('passes valid status and parsed page parameter to getMyBookings', async () => {
      setToken(createMockJwt());
      vi.mocked(getMyBookings).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        result: {
          items: [],
          totalCount: 0,
          currentPage: 2,
          pageSize: 10,
          totalPages: 0,
        },
      });

      const args = createLoaderArgs('http://localhost/bookings?status=Confirmed&page=2');
      const data = await loader(args);

      expect(getMyBookings).toHaveBeenCalledWith({
        status: 'Confirmed',
        page: 2,
        pageSize: 10,
      });
      expect(data).toMatchObject({
        currentStatus: 'Confirmed',
        page: 2,
      });
    });

    it('ignores invalid status string and sanitizes invalid page', async () => {
      setToken(createMockJwt());
      vi.mocked(getMyBookings).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        result: {
          items: [],
          totalCount: 0,
          currentPage: 1,
          pageSize: 10,
          totalPages: 0,
        },
      });

      const args = createLoaderArgs('http://localhost/bookings?status=InvalidStatus&page=-5');
      await loader(args);

      expect(getMyBookings).toHaveBeenCalledWith({
        status: undefined,
        page: 1,
        pageSize: 10,
      });
    });
  });

  describe('Component rendering & UI interactions', () => {
    function renderMyBookingsPage(
      loaderData: {
        res: Awaited<ReturnType<typeof getMyBookings>>;
        currentStatus: string;
        page: number;
      } = {
        res: {
          success: true,
          httpStatus: 200,
          result: {
            items: [mockBooking1, mockBooking2],
            totalCount: 2,
            currentPage: 1,
            pageSize: 10,
            totalPages: 1,
          },
        },
        currentStatus: '',
        page: 1,
      },
    ) {
      const router = createMemoryRouter(
        [
          {
            path: '/bookings',
            element: <MyBookingsPage />,
            loader: () => loaderData,
          },
          {
            path: '/bookings/:id',
            element: <div>Страница деталей бронирования</div>,
          },
          {
            path: '/',
            element: <div>Главная афиша</div>,
          },
        ],
        { initialEntries: ['/bookings'] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders heading, description and filter tabs', async () => {
      renderMyBookingsPage();

      expect(await screen.findByRole('heading', { name: 'Мои бронирования' })).toBeInTheDocument();
      expect(
        screen.getByText('История оформленных билетов и управление текущими бронями'),
      ).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Все' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'В обработке' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Подтвержденные' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Отмененные' })).toBeInTheDocument();
    });

    it('renders error alert when res.success is false', async () => {
      renderMyBookingsPage({
        res: {
          success: false,
          httpStatus: 500,
          error: { message: 'Не удалось получить список бронирований' },
        } as unknown as Awaited<ReturnType<typeof getMyBookings>>,
        currentStatus: '',
        page: 1,
      });

      expect(await screen.findByRole('alert')).toHaveTextContent(
        'Не удалось получить список бронирований',
      );
    });

    it('renders empty state when items list is empty', async () => {
      renderMyBookingsPage({
        res: {
          success: true,
          httpStatus: 200,
          result: {
            items: [],
            totalCount: 0,
            currentPage: 1,
            pageSize: 10,
            totalPages: 1,
          },
        },
        currentStatus: '',
        page: 1,
      });

      expect(await screen.findByText('Бронирований не найдено')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Перейти к афише событий' })).toHaveAttribute(
        'href',
        '/',
      );
    });

    it('renders table with bookings, prices, status badges and links', async () => {
      renderMyBookingsPage();

      expect(await screen.findByRole('table')).toBeInTheDocument();
      expect(screen.getByText('3 000 ₸')).toBeInTheDocument();
      expect(screen.getByText('5 000 ₸')).toBeInTheDocument();
      expect(screen.getByText('Подтверждено')).toBeInTheDocument();
      expect(screen.getAllByText('В обработке')).toHaveLength(2);

      const detailLinks = screen.getAllByRole('link', { name: /подробнее/i });
      expect(detailLinks).toHaveLength(2);
      expect(detailLinks[0]).toHaveAttribute('href', `/bookings/${mockBooking1.id}`);
    });

    it('handles status filter tab clicks', async () => {
      renderMyBookingsPage();
      const pendingTab = await screen.findByRole('button', { name: 'В обработке' });
      fireEvent.click(pendingTab);

      const allTab = screen.getByRole('button', { name: 'Все' });
      fireEvent.click(allTab);
    });

    it('renders all booking status badges correctly including CancellationPending, Cancelled, Rejected, and fallback', async () => {
      const mockVariousBookings: BookingResponse[] = [
        {
          ...mockBooking1,
          id: 'bk-cancelling',
          status: 'CancellationPending',
        },
        {
          ...mockBooking1,
          id: 'bk-cancelled',
          status: 'Cancelled',
        },
        {
          ...mockBooking1,
          id: 'bk-rejected',
          status: 'Rejected',
        },
        {
          ...mockBooking1,
          id: 'bk-other',
          status: 'CustomStatus' as unknown as BookingResponse['status'],
        },
      ];

      renderMyBookingsPage({
        res: {
          success: true,
          httpStatus: 200,
          result: {
            items: mockVariousBookings,
            totalCount: 4,
            currentPage: 1,
            pageSize: 10,
            totalPages: 1,
          },
        },
        currentStatus: '',
        page: 1,
      });

      expect(await screen.findByText('Отмена...')).toBeInTheDocument();
      expect(screen.getByText('Отменено')).toBeInTheDocument();
      expect(screen.getByText('Отклонено')).toBeInTheDocument();
      expect(screen.getByText('CustomStatus')).toBeInTheDocument();
    });

    it('renders pagination when totalPages > 1 and handles previous page navigation', async () => {
      renderMyBookingsPage({
        res: {
          success: true,
          httpStatus: 200,
          result: {
            items: [mockBooking1],
            totalCount: 15,
            currentPage: 2,
            pageSize: 10,
            totalPages: 2,
          },
        },
        currentStatus: '',
        page: 2,
      });

      expect(await screen.findByText(/страница 2 из 2/i)).toBeInTheDocument();
      const prevBtn = screen.getByRole('button', { name: /← назад/i });
      const nextBtn = screen.getByRole('button', { name: /вперед →/i });
      expect(prevBtn).toBeEnabled();
      expect(nextBtn).toBeDisabled();

      fireEvent.click(prevBtn);
    });

    it('satisfies WCAG accessibility rules', async () => {
      const { container } = renderMyBookingsPage();
      await screen.findByRole('heading', { name: 'Мои бронирования' });

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
