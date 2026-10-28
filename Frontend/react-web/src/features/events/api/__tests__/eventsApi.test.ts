import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  mapEventCreateToDto,
  mapEventUpdateToDto,
  createEvent,
  updateEventById,
  getEventById,
  getTopEvents,
  getAllEvents,
  deleteEventById,
  cancelEventById,
  getEventReservations,
} from '../eventsApi';
import { eventsClient } from '@/shared/api';

describe('eventsApi Service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('DTO mappers', () => {
    it('mapEventCreateToDto trims strings and converts dates to ISO with price & currency', () => {
      const result = mapEventCreateToDto({
        title: '  Концерт симфонической музыки  ',
        description: '  Описание события с пробелами  ',
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
        totalSeats: 150,
        price: 2500,
        currency: 'KZT',
      });

      expect(result).toEqual({
        title: 'Концерт симфонической музыки',
        description: 'Описание события с пробелами',
        startAt: '2026-09-20T18:00:00.000Z',
        endAt: '2026-09-20T21:00:00.000Z',
        totalSeats: 150,
        price: 2500,
        currency: 'KZT',
      });
    });

    it('mapEventCreateToDto handles null description and defaults price to 0', () => {
      const result = mapEventCreateToDto({
        title: 'Концерт',
        description: null,
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
        totalSeats: 50,
      });

      expect(result.description).toBeNull();
      expect(result.price).toBe(0);
      expect(result.currency).toBe('KZT');
    });

    it('mapEventUpdateToDto trims strings and converts dates to ISO with price & currency', () => {
      const result = mapEventUpdateToDto({
        title: '  Обновленный концерт  ',
        description: '  Новое описание  ',
        startAt: new Date('2026-09-21T18:00:00.000Z'),
        endAt: new Date('2026-09-21T21:00:00.000Z'),
        price: 3000,
        currency: 'USD',
      });

      expect(result).toEqual({
        title: 'Обновленный концерт',
        description: 'Новое описание',
        startAt: '2026-09-21T18:00:00.000Z',
        endAt: '2026-09-21T21:00:00.000Z',
        price: 3000,
        currency: 'USD',
      });
    });

    it('mapEventUpdateToDto handles null description and omitted price/currency', () => {
      const result = mapEventUpdateToDto({
        title: 'Концерт',
        description: null,
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
      });

      expect(result.description).toBeNull();
      expect(result.price).toBeUndefined();
      expect(result.currency).toBeUndefined();
    });
  });

  describe('createEvent', () => {
    const validDomainEvent = {
      title: 'Новый фестиваль',
      description: 'Фестиваль музыки',
      startAt: new Date('2026-09-25T10:00:00.000Z'),
      endAt: new Date('2026-09-25T22:00:00.000Z'),
      totalSeats: 500,
      price: 1500,
      currency: 'KZT',
    };

    it('creates event successfully with statusUrl from Location header', async () => {
      const mockDto = {
        id: 'event_1',
        title: 'Новый фестиваль',
        description: 'Фестиваль музыки',
        startAt: '2026-09-25T10:00:00.000Z',
        endAt: '2026-09-25T22:00:00.000Z',
        totalSeats: 500,
        availableSeats: 500,
        price: 1500,
        currency: 'KZT',
        isActive: true,
      };

      const response = new Response(JSON.stringify(mockDto), {
        status: 201,
        headers: { Location: '/v1/events/event_1' },
      });

      const postSpy = vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response,
      });

      const result = await createEvent(validDomainEvent);

      expect(postSpy).toHaveBeenCalledWith('/v1/events', {
        body: mapEventCreateToDto(validDomainEvent),
      });

      expect(result).toEqual({
        success: true,
        event: {
          id: 'event_1',
          title: 'Новый фестиваль',
          description: 'Фестиваль музыки',
          startAt: new Date('2026-09-25T10:00:00.000Z'),
          endAt: new Date('2026-09-25T22:00:00.000Z'),
          totalSeats: 500,
          availableSeats: 500,
          price: 1500,
          currency: 'KZT',
          isActive: true,
        },
        httpStatus: 201,
        statusUrl: '/v1/events/event_1',
      });
    });

    it('handles missing Location header on creation', async () => {
      const mockDto = {
        id: 'event_2',
        title: 'Событие 2',
        description: null,
        startAt: '2026-09-25T10:00:00.000Z',
        endAt: '2026-09-25T22:00:00.000Z',
        totalSeats: 100,
        availableSeats: 100,
        price: 0,
        currency: 'KZT',
        isActive: true,
      };

      vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 201 }),
      });

      const result = await createEvent(validDomainEvent);

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.statusUrl).toBeNull();
      }
    });

    it('handles problem details on creation failure', async () => {
      const problem = {
        title: 'Validation Error',
        status: 400,
        detail: 'Title is invalid',
        errors: { title: ['Title is required'] },
      };

      vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      });

      const result = await createEvent(validDomainEvent);

      expect(result).toEqual({
        success: false,
        httpStatus: 400,
        error: {
          message: 'Title is invalid',
          fieldErrors: { title: ['Title is required'] },
        },
      });
    });

    it('handles exception in createEvent', async () => {
      vi.spyOn(eventsClient, 'POST').mockRejectedValueOnce(new Error('Network error'));

      const result = await createEvent(validDomainEvent);

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

  describe('updateEventById', () => {
    const validUpdate = {
      title: 'Обновленное событие',
      description: 'Новое описание',
      startAt: new Date('2026-10-01T10:00:00.000Z'),
      endAt: new Date('2026-10-01T18:00:00.000Z'),
      price: 2000,
      currency: 'KZT',
    };

    it('returns success: true and mapped event on 200 OK', async () => {
      const mockDto = {
        id: 'event_1',
        title: 'Обновленное событие',
        description: 'Новое описание',
        startAt: '2026-10-01T10:00:00.000Z',
        endAt: '2026-10-01T18:00:00.000Z',
        totalSeats: 100,
        availableSeats: 80,
        price: 2000,
        currency: 'KZT',
        isActive: true,
      };

      const putSpy = vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      });

      const result = await updateEventById('event_1', validUpdate);

      expect(putSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        body: mapEventUpdateToDto(validUpdate),
        params: { path: { id: 'event_1' } },
      });

      expect(result).toEqual({
        success: true,
        httpStatus: 200,
        event: {
          id: 'event_1',
          title: 'Обновленное событие',
          description: 'Новое описание',
          startAt: new Date('2026-10-01T10:00:00.000Z'),
          endAt: new Date('2026-10-01T18:00:00.000Z'),
          totalSeats: 100,
          availableSeats: 80,
          price: 2000,
          currency: 'KZT',
          isActive: true,
        },
      });
    });

    it('handles problem details on update failure', async () => {
      const problem = {
        title: 'Not Found',
        status: 404,
        detail: 'Event not found',
      };

      vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      } as unknown as never);

      const result = await updateEventById('event_not_found', validUpdate);

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Event not found',
          fieldErrors: undefined,
        },
      });
    });

    it('handles network error in updateEventById', async () => {
      vi.spyOn(eventsClient, 'PUT').mockRejectedValueOnce(new Error('Network failure'));

      const result = await updateEventById('event_1', validUpdate);

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Network failure',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('getEventById', () => {
    it('returns event response with mapped dates, price, currency, isActive on success', async () => {
      const mockDto = {
        id: 'event_123',
        title: 'Конференция',
        description: 'Описание конференции',
        startAt: '2026-10-01T09:00:00.000Z',
        endAt: '2026-10-01T17:00:00.000Z',
        totalSeats: 300,
        availableSeats: 250,
        price: 5000,
        currency: 'KZT',
        isActive: true,
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      });

      const result = await getEventById('event_123');

      expect(result).toEqual({
        success: true,
        event: {
          id: 'event_123',
          title: 'Конференция',
          description: 'Описание конференции',
          startAt: new Date('2026-10-01T09:00:00.000Z'),
          endAt: new Date('2026-10-01T17:00:00.000Z'),
          totalSeats: 300,
          availableSeats: 250,
          price: 5000,
          currency: 'KZT',
          isActive: true,
        },
        httpStatus: 200,
      });
    });

    it('returns error on not found (404)', async () => {
      const problem = { title: 'Not Found', status: 404, detail: 'Event 999 not found' };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      } as unknown as never);

      const result = await getEventById('999');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(404);
    });
  });

  describe('getTopEvents', () => {
    it('returns array of mapped events on success', async () => {
      const mockList = [
        {
          id: 'top_1',
          title: 'Топ 1',
          description: 'Описание 1',
          startAt: '2026-10-01T10:00:00.000Z',
          endAt: '2026-10-01T12:00:00.000Z',
          totalSeats: 100,
          availableSeats: 10,
          price: 1500,
          currency: 'KZT',
          isActive: true,
        },
      ];

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockList,
        error: undefined,
        response: new Response(JSON.stringify(mockList), { status: 200 }),
      });

      const result = await getTopEvents();

      expect(result).toEqual({
        success: true,
        event: [
          {
            id: 'top_1',
            title: 'Топ 1',
            description: 'Описание 1',
            startAt: new Date('2026-10-01T10:00:00.000Z'),
            endAt: new Date('2026-10-01T12:00:00.000Z'),
            totalSeats: 100,
            availableSeats: 10,
            price: 1500,
            currency: 'KZT',
            isActive: true,
          },
        ],
        httpStatus: 200,
      });
    });
  });

  describe('getAllEvents', () => {
    it('passes query parameters including MinPrice, MaxPrice, Currency, IsActive', async () => {
      const mockPaginatedDto = {
        totalCount: 1,
        currentPage: 1,
        pageSize: 6,
        totalPages: 1,
        items: [
          {
            id: 'ev_1',
            title: 'Событие с ценой',
            description: null,
            startAt: '2026-10-01T10:00:00.000Z',
            endAt: '2026-10-01T12:00:00.000Z',
            totalSeats: 50,
            availableSeats: 25,
            price: 2000,
            currency: 'KZT',
            isActive: true,
          },
        ],
      };

      const getSpy = vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockPaginatedDto,
        error: undefined,
        response: new Response(JSON.stringify(mockPaginatedDto), { status: 200 }),
      });

      const result = await getAllEvents(
        'Событие',
        '2026-10-01T00:00:00.000Z',
        '2026-10-02T00:00:00.000Z',
        1,
        6,
        1000,
        5000,
        'KZT',
        true,
      );

      expect(getSpy).toHaveBeenCalledWith('/v1/events', {
        params: {
          query: {
            Title: 'Событие',
            From: '2026-10-01T00:00:00.000Z',
            To: '2026-10-02T00:00:00.000Z',
            Page: 1,
            PageSize: 6,
            MinPrice: 1000,
            MaxPrice: 5000,
            Currency: 'KZT',
            IsActive: true,
          },
        },
      });

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.event.items.event?.[0].price).toBe(2000);
        expect(result.event.items.event?.[0].currency).toBe('KZT');
        expect(result.event.items.event?.[0].isActive).toBe(true);
      }
    });
  });

  describe('deleteEventById', () => {
    it('deletes event successfully on 200/204', async () => {
      const deleteSpy = vi.spyOn(eventsClient, 'DELETE').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await deleteEventById('event_del_1');

      expect(deleteSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        params: { path: { id: 'event_del_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 204,
      });
    });
  });

  describe('cancelEventById', () => {
    it('cancels event successfully via POST /v1/events/{id}/cancel', async () => {
      const postSpy = vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await cancelEventById('event_cancel_1');

      expect(postSpy).toHaveBeenCalledWith('/v1/events/{id}/cancel', {
        params: { path: { id: 'event_cancel_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 204,
      });
    });

    it('handles error on cancelEventById', async () => {
      const problem = { title: 'Forbidden', status: 403, detail: 'Not allowed' };

      vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 403 }),
      });

      const result = await cancelEventById('event_cancel_fail');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(403);
    });
  });

  describe('getEventReservations', () => {
    it('returns mapped seat reservations on success', async () => {
      const mockReservations = [
        {
          bookingId: 'book_1',
          eventId: 'event_1',
          seats: 3,
          unitPrice: 1500,
          totalPrice: 4500,
          currency: 'KZT',
          discountAmount: 0,
          priceVersion: 1,
          status: 'Reserved',
          rejectionReason: null,
          createdAt: '2026-10-01T12:00:00.000Z',
          expiresAt: '2026-10-01T12:15:00.000Z',
        },
      ];

      const getSpy = vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockReservations,
        error: undefined,
        response: new Response(JSON.stringify(mockReservations), { status: 200 }),
      } as unknown as never);

      const result = await getEventReservations('event_1');

      expect(getSpy).toHaveBeenCalledWith('/v1/events/{id}/reservations', {
        params: { path: { id: 'event_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 200,
        reservations: [
          {
            bookingId: 'book_1',
            eventId: 'event_1',
            seats: 3,
            unitPrice: 1500,
            totalPrice: 4500,
            currency: 'KZT',
            discountAmount: 0,
            priceVersion: 1,
            status: 'Reserved',
            rejectionReason: null,
            createdAt: new Date('2026-10-01T12:00:00.000Z'),
            expiresAt: new Date('2026-10-01T12:15:00.000Z'),
          },
        ],
      });
    });

    it('handles error on getEventReservations', async () => {
      vi.spyOn(eventsClient, 'GET').mockRejectedValueOnce(new Error('Fetch failed'));

      const result = await getEventReservations('event_1');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(0);
    });
  });
});
