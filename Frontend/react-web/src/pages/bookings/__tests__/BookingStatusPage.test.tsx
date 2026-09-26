import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, action, GetBookingPage } from '../BookingStatusPage';
import { getBookingById, deleteBookingById } from '@/features/bookings/api/bookingsApi';
import { setToken, clearToken } from '@/shared/lib/auth';
import { checkA11y } from '@/shared/lib/test/axe';
import type { BookingResponse } from '@/features/bookings/api/bookingsApi';

vi.mock('@/features/bookings/api/bookingsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/features/bookings/api/bookingsApi')>();
  return {
    ...actual,
    getBookingById: vi.fn(),
    deleteBookingById: vi.fn(),
  };
});

function createMockJwt(expSecondsFromNow = 3600): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({ sub: 'user_123', exp: Math.floor(Date.now() / 1000) + expSecondsFromNow }),
  );
  return `${header}.${payload}.signature`;
}

type LoaderArgs = Parameters<typeof loader>[0];
type ActionArgs = Parameters<typeof action>[0];

function createLoaderArgs(bookingId?: string, signal?: AbortSignal): LoaderArgs {
  return {
    request: new Request('http://localhost/bookings/bk-1', { signal }),
    params: bookingId !== undefined ? { bookingId } : {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

function createActionArgs(
  bookingId?: string,
  formDataRecord: Record<string, string> = {},
): ActionArgs {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  return {
    request: new Request('http://localhost/bookings/bk-1', { method: 'POST', body: formData }),
    params: bookingId !== undefined ? { bookingId } : {},
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

describe('BookingStatusPage', () => {
  const mockBooking: BookingResponse = {
    id: 'bk-123',
    eventId: 'ev-456',
    status: 'Pending',
    createdAt: new Date('2026-08-01T12:00:00Z'),
    processedAt: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    clearToken();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  describe('loader', () => {
    it('redirects unauthenticated users to /login', async () => {
      clearToken();
      const args = createLoaderArgs('bk-123');
      const result = await loader(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toContain('/login');
    });

    it('throws 404 Response when bookingId param is missing', async () => {
      setToken(createMockJwt());
      const args = createLoaderArgs(undefined);

      await expect(loader(args)).rejects.toMatchObject({
        status: 404,
        statusText: 'Not Found',
      });
    });

    it('throws 404 Response when getBookingById returns 404', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockResolvedValueOnce({
        success: false,
        httpStatus: 404,
        error: { message: 'Бронирование не найдено' },
      });

      const args = createLoaderArgs('bk-not-found');
      await expect(loader(args)).rejects.toMatchObject({
        status: 404,
        statusText: 'Not Found',
      });
    });

    it('throws 403 Response when getBookingById returns 403 (forbidden)', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockResolvedValueOnce({
        success: false,
        httpStatus: 403,
        error: { message: 'У вас нет доступа' },
      });

      const args = createLoaderArgs('bk-other-user');
      await expect(loader(args)).rejects.toMatchObject({
        status: 403,
        statusText: 'Forbidden',
      });
    });

    it('re-throws DOMException AbortError when signal is aborted', async () => {
      setToken(createMockJwt());
      const abortError = new DOMException('The user aborted a request.', 'AbortError');
      vi.mocked(getBookingById).mockRejectedValueOnce(abortError);

      const controller = new AbortController();
      controller.abort();
      const args = createLoaderArgs('bk-123', controller.signal);

      await expect(loader(args)).rejects.toThrow('The user aborted a request.');
    });

    it('throws 500 Response when an unexpected network error occurs', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockRejectedValueOnce(new TypeError('Failed to fetch'));

      const args = createLoaderArgs('bk-123');
      await expect(loader(args)).rejects.toMatchObject({
        status: 500,
        statusText: 'Internal Server Error',
      });
    });

    it('throws 500 Response with fallback message when res.error?.message is missing and status is 0', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockResolvedValueOnce({
        success: false,
        httpStatus: 0,
        error: undefined as unknown as { message: string },
      });

      const args = createLoaderArgs('bk-crash');
      let caught: unknown;
      try {
        await loader(args);
      } catch (err) {
        caught = err;
      }
      expect(caught).toBeInstanceOf(Response);
      const res = caught as Response;
      expect(res.status).toBe(500);
      expect(await res.text()).toBe('Не удалось загрузить бронирование');
    });

    it('throws Response with res.httpStatus and custom message when httpStatus >= 400', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockResolvedValueOnce({
        success: false,
        httpStatus: 502,
        error: { message: 'Шлюз недоступен' },
      });

      const args = createLoaderArgs('bk-bad-gateway');
      let caught: unknown;
      try {
        await loader(args);
      } catch (err) {
        caught = err;
      }
      expect(caught).toBeInstanceOf(Response);
      const res = caught as Response;
      expect(res.status).toBe(502);
      expect(await res.text()).toBe('Шлюз недоступен');
    });

    it('returns booking data on successful load', async () => {
      setToken(createMockJwt());
      vi.mocked(getBookingById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        booking: mockBooking,
      });

      const args = createLoaderArgs('bk-123');
      const result = await loader(args);
      expect(result).toEqual({ booking: mockBooking });
    });
  });

  describe('action', () => {
    it('redirects unauthenticated users to /login', async () => {
      clearToken();
      const args = createActionArgs('bk-123', { intent: 'cancel' });
      const result = await action(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
    });

    it('returns error when bookingId is missing', async () => {
      setToken(createMockJwt());
      const args = createActionArgs(undefined, { intent: 'cancel' });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Идентификатор бронирования не указан',
        },
      });
    });

    it('handles API error when deleteBookingById fails', async () => {
      setToken(createMockJwt());
      vi.mocked(deleteBookingById).mockResolvedValueOnce({
        success: false,
        httpStatus: 400,
        error: { message: 'Нельзя отменить уже завершенное бронирование' },
      });

      const args = createActionArgs('bk-123', { intent: 'cancel' });
      const result = await action(args);

      expect(result).toEqual({
        error: { message: 'Нельзя отменить уже завершенное бронирование' },
      });
      expect(deleteBookingById).toHaveBeenCalledWith('bk-123');
    });

    it('returns success message when deleteBookingById succeeds', async () => {
      setToken(createMockJwt());
      vi.mocked(deleteBookingById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
      });

      const args = createActionArgs('bk-123', { intent: 'cancel' });
      const result = await action(args);

      expect(result).toEqual({
        success: true,
        message: 'Заявка на отмену бронирования принята',
      });
      expect(deleteBookingById).toHaveBeenCalledWith('bk-123');
    });

    it('returns null for unhandled intent', async () => {
      setToken(createMockJwt());
      const args = createActionArgs('bk-123', { intent: 'unknown_action' });
      const result = await action(args);
      expect(result).toBeNull();
    });
  });

  describe('Component rendering, state machine & sessionStorage', () => {
    it('renders booking details and active polling badge for Pending booking', async () => {
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: mockBooking }),
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      const { container } = render(<RouterProvider router={router} />);

      expect(
        await screen.findByRole('heading', { name: /статус бронирования/i }),
      ).toBeInTheDocument();
      expect(screen.getByText(/фоновое отслеживание/i)).toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });

    it('restores cancelling state from sessionStorage across page reload (F5)', async () => {
      sessionStorage.setItem('cancelling_booking_bk-123', 'true');

      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: { ...mockBooking, status: 'Confirmed' } }),
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      expect(
        await screen.findByText(
          /заявка на отмену бронирования принята и обрабатывается в фоновом режиме/i,
        ),
      ).toBeInTheDocument();
    });

    it('clears cancelling key from sessionStorage when status becomes Cancelled', async () => {
      sessionStorage.setItem('cancelling_booking_bk-123', 'true');

      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: { ...mockBooking, status: 'Cancelled' } }),
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      await screen.findByRole('heading', { name: /статус бронирования/i });
      await waitFor(() => {
        expect(sessionStorage.getItem('cancelling_booking_bk-123')).toBeNull();
      });
    });

    it('renders error alert banner when actionData contains error', async () => {
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: mockBooking }),
            action: () => ({ error: { message: 'Сбой отмены' } }),
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      await screen.findByRole('heading', { name: /статус бронирования/i });
    });

    it('submits cancellation and displays cancelling banner when action succeeds', async () => {
      setToken(createMockJwt());
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      vi.mocked(deleteBookingById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
      });

      const user = userEvent.setup();
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: { ...mockBooking, status: 'Confirmed' } }),
            action,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      const cancelBtn = await screen.findByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(await screen.findByText('Заявка на отмену бронирования принята')).toBeInTheDocument();
      expect(screen.getByText('Отмена обрабатывается сервером...')).toBeInTheDocument();
    });

    it('executes polling loop and handles revalidation correctly', async () => {
      vi.useFakeTimers();
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: mockBooking }),
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      // Flush router initial loader & mount GetBookingPage
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });
      expect(screen.getByRole('heading', { name: /статус бронирования/i })).toBeInTheDocument();

      // Advance timer by 2000ms to trigger poll()
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });
      // Advance timer by another 2000ms to test recurring loop
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      vi.useRealTimers();
    });

    it('displays actionData error when cancellation fails', async () => {
      setToken(createMockJwt());
      const user = userEvent.setup();
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: mockBooking }),
            action: () => ({ error: { message: 'Ошибка при отмене' } }),
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);
      const cancelBtn = await screen.findByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(await screen.findByText('Ошибка при отмене')).toBeInTheDocument();
    });

    it('displays actionData success message when cancellation is completed without inProgress', async () => {
      setToken(createMockJwt());
      const user = userEvent.setup();
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      let callCount = 0;
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => {
              callCount++;
              if (callCount > 1) {
                return { booking: { ...mockBooking, status: 'Cancelled' } };
              }
              return { booking: mockBooking };
            },
            action: () => ({ success: true, message: 'Бронирование успешно отменено' }),
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);
      const cancelBtn = await screen.findByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(await screen.findByText('Бронирование успешно отменено')).toBeInTheDocument();
    });

    it('catches and ignores network errors in polling loop', async () => {
      vi.useFakeTimers();
      let callCount = 0;
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => {
              callCount++;
              if (callCount > 1) {
                throw new Error('Network error during poll');
              }
              return { booking: mockBooking };
            },
            action: () => null,
            HydrateFallback: () => null,
            ErrorBoundary: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });
      expect(screen.getByRole('heading', { name: /статус бронирования/i })).toBeInTheDocument();

      // Trigger poll which throws error in revalidation
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      vi.useRealTimers();
    });

    it('displays fallback cancelling message when action returns inProgress without message', async () => {
      setToken(createMockJwt());
      const user = userEvent.setup();
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => ({ booking: mockBooking }),
            action: () => ({ success: true }),
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);
      const cancelBtn = await screen.findByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(
        await screen.findByText(
          'Заявка на отмену бронирования принята и обрабатывается в фоновом режиме...',
        ),
      ).toBeInTheDocument();
    });

    it('shows loading indicator when revalidator is in loading state', async () => {
      vi.useFakeTimers();
      let callCount = 0;
      let resolveLoader!: (val: unknown) => void;

      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => {
              callCount++;
              if (callCount > 1) {
                return new Promise((res) => {
                  resolveLoader = res;
                });
              }
              return { booking: mockBooking };
            },
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      render(<RouterProvider router={router} />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });

      expect(screen.getByText('Фоновое отслеживание...')).toBeInTheDocument();

      // Trigger poll() which starts revalidating
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      // While loader is in-flight, revalidator.state is 'loading'
      expect(screen.getByText('Обновление данных...')).toBeInTheDocument();

      // Resolve pending loader with Confirmed status
      await act(async () => {
        resolveLoader({ booking: { ...mockBooking, status: 'Confirmed' } });
        await vi.advanceTimersByTimeAsync(0);
      });

      expect(screen.queryByText('Обновление данных...')).not.toBeInTheDocument();
      expect(screen.queryByText('Фоновое отслеживание...')).not.toBeInTheDocument();

      vi.useRealTimers();
    });

    it('stops polling and cleans up when component unmounts while revalidating', async () => {
      vi.useFakeTimers();
      let resolveReval!: () => void;
      let callCount = 0;
      const router = createMemoryRouter(
        [
          {
            path: '/bookings/:bookingId',
            element: <GetBookingPage />,
            loader: () => {
              callCount++;
              if (callCount > 1) {
                return new Promise((res) => {
                  resolveReval = () => res({ booking: mockBooking });
                });
              }
              return { booking: mockBooking };
            },
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/bk-123'] },
      );

      const { unmount } = render(<RouterProvider router={router} />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });

      // Trigger poll() which starts revalidating (callCount > 1)
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      // Unmount while revalidation is in-flight!
      unmount();

      // Now resolve the in-flight revalidation
      await act(async () => {
        if (resolveReval) resolveReval();
        await vi.advanceTimersByTimeAsync(0);
      });

      vi.useRealTimers();
    });
  });
});
