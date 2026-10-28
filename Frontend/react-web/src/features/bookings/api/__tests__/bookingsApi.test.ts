import { describe, it, expect, vi, beforeEach } from 'vitest';
import { getBookingById, createBooking, deleteBookingById, getMyBookings } from '../bookingsApi';
import { bookingsClient } from '@/shared/api';

describe('bookingsApi Service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('getBookingById', () => {
    it('returns booking data with mapped dates and financial fields on success', async () => {
      const mockDto = {
        id: 'booking_1',
        eventId: 'event_1',
        status: 'Confirmed' as const,
        createdAt: '2026-09-16T10:00:00.000Z',
        processedAt: '2026-09-16T10:05:00.000Z',
        userId: 'user_1',
        seats: 2,
        unitPrice: 1500,
        totalPrice: 3000,
        discountAmount: 0,
        currency: 'KZT',
        rejectionReason: null,
        cancellationReason: null,
        cancellationRequestedAt: null,
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
          seats: 2,
          unitPrice: 1500,
          totalPrice: 3000,
          discountAmount: 0,
          currency: 'KZT',
          rejectionReason: null,
          cancellationReason: null,
          cancellationRequestedAt: null,
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
        expect(result.booking.seats).toBe(1);
        expect(result.booking.unitPrice).toBe(0);
        expect(result.booking.totalPrice).toBe(0);
        expect(result.booking.currency).toBe('KZT');
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
        detail: 'Booking not found',
      };

      vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      } as unknown as never);

      const result = await getBookingById('b_missing');

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Booking not found',
          fieldErrors: undefined,
        },
      });
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
    it('sends seats in request body and returns booking on success', async () => {
      const mockDto = {
        id: 'booking_new',
        eventId: 'event_1',
        status: 'Pending' as const,
        createdAt: '2026-09-16T12:00:00.000Z',
        processedAt: null,
        seats: 3,
        unitPrice: 1000,
        totalPrice: 3000,
        discountAmount: 0,
        currency: 'KZT',
        rejectionReason: null,
        cancellationReason: null,
        cancellationRequestedAt: null,
      };

      const response = new Response(JSON.stringify(mockDto), {
        status: 202,
        headers: { Location: '/v1/bookings/booking_new' },
      });

      const postSpy = vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response,
      } as unknown as never);

      const result = await createBooking('event_1', 3);

      expect(postSpy).toHaveBeenCalledWith('/v1/bookings/{id}/book', {
        params: { path: { id: 'event_1' } },
        body: { seats: 3 },
      });

      expect(result).toEqual({
        success: true,
        booking: {
          id: 'booking_new',
          eventId: 'event_1',
          status: 'Pending',
          createdAt: new Date('2026-09-16T12:00:00.000Z'),
          processedAt: null,
          userId: undefined,
          seats: 3,
          unitPrice: 1000,
          totalPrice: 3000,
          discountAmount: 0,
          currency: 'KZT',
          rejectionReason: null,
          cancellationReason: null,
          cancellationRequestedAt: null,
        },
        statusUrl: '/v1/bookings/booking_new',
        httpStatus: 202,
      });
    });

    it('defaults to 1 seat when seats is omitted or invalid', async () => {
      const mockDto = {
        id: 'booking_default',
        eventId: 'event_1',
        status: 'Pending' as const,
        createdAt: '2026-09-16T12:00:00.000Z',
        processedAt: null,
        seats: 1,
        unitPrice: 1000,
        totalPrice: 1000,
        discountAmount: 0,
        currency: 'KZT',
      };

      const postSpy = vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 202 }),
      } as unknown as never);

      await createBooking('event_1');

      expect(postSpy).toHaveBeenCalledWith('/v1/bookings/{id}/book', {
        params: { path: { id: 'event_1' } },
        body: { seats: 1 },
      });
    });

    it('handles Conflict 409 error on booking', async () => {
      const problem = {
        title: 'Conflict',
        status: 409,
        detail: 'Недостаточно мест',
      };

      vi.spyOn(bookingsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 409 }),
      } as unknown as never);

      const result = await createBooking('event_1', 10);

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(409);
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
    it('sends reason in body and returns 202 Accepted on successful cancellation', async () => {
      const mockDto = {
        id: 'booking_1',
        eventId: 'event_1',
        status: 'CancellationPending' as const,
        createdAt: '2026-09-16T10:00:00.000Z',
        processedAt: '2026-09-16T10:05:00.000Z',
        seats: 2,
        unitPrice: 1000,
        totalPrice: 2000,
        discountAmount: 0,
        currency: 'KZT',
        rejectionReason: null,
        cancellationReason: 'UserCancelled' as const,
        cancellationRequestedAt: '2026-09-16T11:00:00.000Z',
      };

      const deleteSpy = vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 202 }),
      } as unknown as never);

      const result = await deleteBookingById('booking_1', 'UserCancelled');

      expect(deleteSpy).toHaveBeenCalledWith('/v1/bookings/{id}', {
        params: { path: { id: 'booking_1' } },
        body: { reason: 'UserCancelled' },
      });
      expect(result.success).toBe(true);
      expect(result.httpStatus).toBe(202);
      if (result.success) {
        expect(result.booking?.status).toBe('CancellationPending');
      }
    });

    it('handles 409 Conflict when cancelling Pending booking', async () => {
      const problem = {
        title: 'Conflict',
        status: 409,
        detail: 'Cannot cancel pending booking',
      };

      vi.spyOn(bookingsClient, 'DELETE').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 409 }),
      } as unknown as never);

      const result = await deleteBookingById('booking_pending');

      expect(result).toEqual({
        success: false,
        httpStatus: 409,
        error: {
          message: 'Cannot cancel pending booking',
          fieldErrors: undefined,
        },
      });
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

  describe('getMyBookings', () => {
    it('returns paginated list of bookings on success', async () => {
      const mockPaginatedDto = {
        totalCount: 1,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        items: [
          {
            id: 'bk_1',
            eventId: 'ev_1',
            status: 'Confirmed' as const,
            createdAt: '2026-09-16T10:00:00.000Z',
            processedAt: '2026-09-16T10:05:00.000Z',
            seats: 2,
            unitPrice: 1500,
            totalPrice: 3000,
            discountAmount: 0,
            currency: 'KZT',
            userId: 'user_1',
            rejectionReason: null,
            cancellationReason: null,
            cancellationRequestedAt: null,
          },
        ],
      };

      const getSpy = vi.spyOn(bookingsClient, 'GET').mockResolvedValueOnce({
        data: mockPaginatedDto,
        error: undefined,
        response: new Response(JSON.stringify(mockPaginatedDto), { status: 200 }),
      });

      const result = await getMyBookings({ status: 'Confirmed', page: 1, pageSize: 10 });

      expect(getSpy).toHaveBeenCalledWith('/v1/bookings', {
        params: {
          query: {
            Status: 'Confirmed',
            UserId: undefined,
            Page: 1,
            PageSize: 10,
          },
        },
      });

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.result.totalCount).toBe(1);
        expect(result.result.items[0].seats).toBe(2);
        expect(result.result.items[0].totalPrice).toBe(3000);
      }
    });

    it('handles failure when loading bookings', async () => {
      vi.spyOn(bookingsClient, 'GET').mockRejectedValueOnce(new Error('Load failed'));

      const result = await getMyBookings();

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(0);
    });
  });
});
