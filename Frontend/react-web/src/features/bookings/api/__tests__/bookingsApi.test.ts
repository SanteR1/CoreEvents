import { describe, it, expect, vi, beforeEach } from 'vitest';
import { getBookingById, createBooking, deleteBookingById } from '../bookingsApi';
import { bookingsClient } from '@/shared/api';

describe('bookingsApi Service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('getBookingById', () => {
    it('returns booking data with mapped dates on success', async () => {
      const mockDto = {
        id: 'booking_1',
        eventId: 'event_1',
        status: 'Confirmed' as const,
        createdAt: '2026-09-16T10:00:00.000Z',
        processedAt: '2026-09-16T10:05:00.000Z',
        userId: 'user_1',
      };

      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      });

      const result = await getBookingById('booking_1');

      expect(result).toEqual({
        success: true,
        booking: {
          id: 'booking_1',
          eventId: 'event_1',
          status: 'Confirmed',
          createdAt: new Date('2026-09-16T10:00:00.000Z'),
          processedAt: new Date('2026-09-16T10:05:00.000Z'),
          userId: 'user_1',
        },
        httpStatus: 200,
      });
    });

    it('falls back to default values for missing or null DTO fields', async () => {
      const mockDto = {
        id: undefined,
        eventId: undefined,
        status: undefined,
        createdAt: undefined,
        processedAt: null,
      };

      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      } as unknown as never);

      const result = await getBookingById('booking_fallback');

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.booking.id).toBe('');
        expect(result.booking.eventId).toBe('');
        expect(result.booking.status).toBe('Pending');
        expect(result.booking.createdAt).toBeInstanceOf(Date);
        expect(result.booking.processedAt).toBeNull();
      }
    });

    it('passes abort signal and cache headers', async () => {
      const controller = new AbortController();
      const getSpy = vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: { id: 'b1', eventId: 'e1' },
        error: undefined,
        response: new Response(JSON.stringify({}), { status: 200 }),
      } as unknown as never);

      await getBookingById('b1', { signal: controller.signal });

      expect(getSpy).toHaveBeenCalledWith('/v1/bookings/{id}', {
        params: { path: { id: 'b1' } },
        signal: controller.signal,
        cache: 'no-store',
        headers: {
          'Cache-Control': 'no-cache, no-store, must-revalidate',
          Pragma: 'no-cache',
        },
      });
    });

    it('rethrows DOMException when request is aborted', async () => {
      const abortError = new DOMException('Operation aborted', 'AbortError');
      vi.spyOn(bookingsClient, 'GET').mockRejectedValueOnce(abortError);

      await expect(getBookingById('b1')).rejects.toThrow(abortError);
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Not Found',
        status: 404,
        detail: 'Бронирование не найдено',
      };

      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      } as unknown as Awaited<ReturnType<typeof bookingsClient.GET>>);

      const result = await getBookingById('unknown_id');

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Бронирование не найдено',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Error',
        detail: 'Booking error',
        errors: { id: ['Invalid ID'] },
      };

      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as Awaited<ReturnType<typeof bookingsClient.GET>>);

      const result = await getBookingById('b1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('handles client response errors (!response.ok and missing data)', async () => {
      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Failed to fetch' },
        response: new Response(null, { status: 500 }),
      } as unknown as never);

      const result = await getBookingById('b1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(500);
      expect(result.error).toBeDefined();
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(bookingsClient, 'GET').mockRejectedValueOnce(new Error('Network error'));

      const result = await getBookingById('b1');

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Network error',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('createBooking', () => {
    it('returns booking, statusUrl from Location header, and status 201 on success', async () => {
      const mockDto = {
        id: 'booking_new',
        eventId: 'event_1',
        status: 'Pending' as const,
        createdAt: '2026-09-16T12:00:00.000Z',
        processedAt: null,
      };

      const response = new Response(JSON.stringify(mockDto), {
        status: 201,
        headers: { Location: '/v1/bookings/booking_new' },
      });

      const postSpy = vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response,
      });

      const result = await createBooking('event_1');

      expect(postSpy).toHaveBeenCalledWith('/v1/bookings/{id}/book', {
        params: { path: { id: 'event_1' } },
      });

      expect(result).toEqual({
        success: true,
        booking: {
          id: 'booking_new',
          eventId: 'event_1',
          status: 'Pending',
          createdAt: new Date('2026-09-16T12:00:00.000Z'),
          processedAt: null,
        },
        statusUrl: '/v1/bookings/booking_new',
        httpStatus: 201,
      });
    });

    it('handles ProblemDetails response with custom status code', async () => {
      const problem = {
        title: 'Conflict',
        status: 409,
        detail: 'Все места уже забронированы',
      };

      vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 409 }),
      } as unknown as Awaited<ReturnType<typeof bookingsClient.POST>>);

      const result = await createBooking('event_full');

      expect(result).toEqual({
        success: false,
        httpStatus: 409,
        error: {
          message: 'Все места уже забронированы',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails without status using response.status', async () => {
      const problem = {
        title: 'Bad Request',
        errors: { seats: ['Invalid seat count'] },
      };

      vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as Awaited<ReturnType<typeof bookingsClient.POST>>);

      const result = await createBooking('event_1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('returns fallback error message when server responds with empty data and error', async () => {
      vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 500 }),
      } as unknown as Awaited<ReturnType<typeof bookingsClient.POST>>);

      const result = await createBooking('event_1');

      expect(result).toEqual({
        success: false,
        httpStatus: 500,
        error: {
          message: 'Сервер не вернул данные бронирования',
          fieldErrors: undefined,
        },
      });
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(bookingsClient, 'POST').mockRejectedValueOnce(new Error('Connection failed'));

      const result = await createBooking('event_1');

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Connection failed',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('deleteBookingById', () => {
    it('returns success and status 204 on successful cancellation', async () => {
      const deleteSpy = vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await deleteBookingById('booking_1');

      expect(deleteSpy).toHaveBeenCalledWith('/v1/bookings/{id}', {
        params: { path: { id: 'booking_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 204,
      });
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Not Found',
        status: 404,
        detail: 'Бронирование не найдено для отмены',
      };

      vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      });

      const result = await deleteBookingById('booking_unknown');

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Бронирование не найдено для отмены',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Bad Request',
        errors: { id: ['Cannot delete completed booking'] },
      };

      vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      });

      const result = await deleteBookingById('booking_1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('handles generic server error response', async () => {
      vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Internal Server Error' },
        response: new Response(null, { status: 500 }),
      } as unknown as never);

      const result = await deleteBookingById('booking_1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(500);
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(bookingsClient, 'DELETE').mockRejectedValueOnce(new Error('Network error'));

      const result = await deleteBookingById('booking_1');

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Network error',
          fieldErrors: undefined,
        },
      });
    });
  });
});
