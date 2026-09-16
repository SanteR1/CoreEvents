import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, action, CreateBookingPage } from '../CreateBookingPage';
import { getEventById } from '@/features/events/api/eventsApi';
import { createBooking } from '@/features/bookings/api/bookingsApi';
import { setToken, clearToken } from '@/shared/lib/auth';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';
import type { CreateBookingResult } from '@/features/bookings/api/bookingsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  getEventById: vi.fn(),
}));

vi.mock('@/features/bookings/api/bookingsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/features/bookings/api/bookingsApi')>();
  return {
    ...actual,
    createBooking: vi.fn(),
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

function createLoaderArgs(
  eventId?: string,
  url = 'http://localhost/bookings/create/ev-1',
): LoaderArgs {
  return {
    request: new Request(url),
    params: eventId ? { eventId } : {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

function createActionArgs(formDataRecord: Record<string, string>): ActionArgs {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  const request = new Request('http://localhost/bookings/create/ev-1', {
    method: 'POST',
    body: formData,
  });
  return {
    request,
    params: { eventId: formDataRecord.eventId ?? 'ev-1' },
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

describe('CreateBookingPage', () => {
  const mockEvent: EventResponse = {
    id: 'ev-1',
    title: 'Вечер джаза',
    description: 'Живая музыка',
    startAt: new Date('2026-07-20T19:00:00Z'),
    endAt: new Date('2026-07-20T22:00:00Z'),
    totalSeats: 50,
    availableSeats: 10,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    clearToken();
  });

  describe('loader', () => {
    it('redirects unauthenticated users to login with returnUrl', async () => {
      clearToken();
      const args = createLoaderArgs('ev-1', 'http://localhost/bookings/create/ev-1?view=full');
      const result = await loader(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe(
        '/login?returnUrl=%2Fbookings%2Fcreate%2Fev-1%3Fview%3Dfull',
      );
    });

    it('throws 404 Response if eventId param is missing', async () => {
      setToken(createMockJwt());
      const args = createLoaderArgs(undefined);

      await expect(loader(args)).rejects.toMatchObject({
        status: 404,
        statusText: 'Not Found',
      });
    });

    it('throws 404 Response if getEventById returns 404', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 404,
        error: { message: 'Событие не найдено' },
      });

      const args = createLoaderArgs('ev-not-found');
      await expect(loader(args)).rejects.toMatchObject({
        status: 404,
        statusText: 'Not Found',
      });
    });

    it('throws 403 Response if getEventById returns 403', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 403,
        error: { message: 'Доступ закрыт' },
      });

      const args = createLoaderArgs('ev-private');
      await expect(loader(args)).rejects.toMatchObject({
        status: 403,
        statusText: 'Forbidden',
      });
    });

    it('throws Response with server error status if getEventById fails with 500', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 500,
        error: { message: 'Сервер недоступен' },
      });

      const args = createLoaderArgs('ev-1');
      await expect(loader(args)).rejects.toMatchObject({
        status: 500,
      });
    });

    it('throws 500 Response with fallback message when res.error?.message is missing and status is 0', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 0,
        error: undefined as unknown as { message: string },
      });

      const args = createLoaderArgs('ev-crash');
      let caught: unknown;
      try {
        await loader(args);
      } catch (err) {
        caught = err;
      }
      expect(caught).toBeInstanceOf(Response);
      const res = caught as Response;
      expect(res.status).toBe(500);
      expect(await res.text()).toBe('Не удалось загрузить данные события');
    });

    it('returns event data on successful load', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        event: mockEvent,
      });

      const args = createLoaderArgs('ev-1');
      const result = await loader(args);
      expect(result).toEqual({ event: mockEvent });
    });
  });

  describe('action', () => {
    it('returns error if eventId is missing in formData', async () => {
      const args = createActionArgs({ seats: '2' });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Идентификатор события не найден',
        },
      });
    });

    it('handles API failure during booking creation', async () => {
      vi.mocked(createBooking).mockResolvedValueOnce({
        success: false,
        httpStatus: 409,
        error: { message: 'Все места уже забронированы' },
      });

      const args = createActionArgs({ eventId: 'ev-1', seats: '3' });
      const result = await action(args);

      expect(result).toEqual({
        error: { message: 'Все места уже забронированы' },
      });
      expect(createBooking).toHaveBeenCalledWith('ev-1', '3');
    });

    it('defaults seats to "1" if not provided', async () => {
      vi.mocked(createBooking).mockResolvedValueOnce({
        success: false,
        httpStatus: 400,
        error: { message: 'Ошибка' },
      });

      const args = createActionArgs({ eventId: 'ev-1' });
      await action(args);

      expect(createBooking).toHaveBeenCalledWith('ev-1', '1');
    });

    it('redirects to /bookings/:id when booking.id is returned', async () => {
      vi.mocked(createBooking).mockResolvedValueOnce({
        success: true,
        httpStatus: 201,
        // @ts-expect-error partial booking response
        booking: { id: 'bk-new-123' },
        statusUrl: null,
      });

      const args = createActionArgs({ eventId: 'ev-1', seats: '2' });
      const result = await action(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/bookings/bk-new-123');
    });

    it('extracts ID from statusUrl and redirects when booking.id is missing', async () => {
      vi.mocked(createBooking).mockResolvedValueOnce({
        success: true,
        httpStatus: 202,
        booking: undefined,
        statusUrl: '/api/v1/bookings/status/bk-async-777',
      } as unknown as CreateBookingResult);

      const args = createActionArgs({ eventId: 'ev-1', seats: '2' });
      const result = await action(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/bookings/bk-async-777');
    });

    it('returns error if neither booking.id nor statusUrl are returned on success', async () => {
      vi.mocked(createBooking).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        booking: undefined,
        statusUrl: null,
      } as unknown as CreateBookingResult);

      const args = createActionArgs({ eventId: 'ev-1', seats: '2' });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Заявка принята, но не удалось получить номер бронирования',
        },
      });
    });

    it('catches unexpected JS errors and returns user-friendly message', async () => {
      vi.mocked(createBooking).mockRejectedValueOnce(new Error('Syntax crash in parser'));

      const args = createActionArgs({ eventId: 'ev-1', seats: '2' });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Syntax crash in parser',
        },
      });
    });

    it('re-throws thrown Response instances without catching', async () => {
      const responseToThrow = new Response(null, {
        status: 302,
        headers: { Location: '/somewhere' },
      });
      vi.mocked(createBooking).mockImplementationOnce(() => {
        throw responseToThrow;
      });

      const args = createActionArgs({ eventId: 'ev-1' });
      await expect(action(args)).rejects.toBe(responseToThrow);
    });
  });

  describe('Component rendering & A11y', () => {
    it('renders heading and BookingCreateForm, satisfying a11y', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        event: mockEvent,
      });

      const router = createMemoryRouter(
        [
          {
            path: '/bookings/create/:eventId',
            element: <CreateBookingPage />,
            loader: () => ({ event: mockEvent }),
            action: () => null,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/bookings/create/ev-1'] },
      );

      const { container } = render(<RouterProvider router={router} />);

      expect(
        await screen.findByRole('heading', { name: /бронирование билетов/i }),
      ).toBeInTheDocument();
      expect(screen.getByRole('spinbutton', { name: /^количество мест$/i })).toHaveValue(1);

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
