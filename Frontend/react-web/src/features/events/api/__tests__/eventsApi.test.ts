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
} from '../eventsApi';
import { eventsClient } from '@/shared/api';

describe('eventsApi Service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('DTO mappers', () => {
    it('mapEventCreateToDto trims strings and converts dates to ISO', () => {
      const result = mapEventCreateToDto({
        title: '  Концерт симфонической музыки  ',
        description: '  Описание события с пробелами  ',
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
        totalSeats: 150,
      });

      expect(result).toEqual({
        title: 'Концерт симфонической музыки',
        description: 'Описание события с пробелами',
        startAt: '2026-09-20T18:00:00.000Z',
        endAt: '2026-09-20T21:00:00.000Z',
        totalSeats: 150,
      });
    });

    it('mapEventCreateToDto handles null description', () => {
      const result = mapEventCreateToDto({
        title: 'Концерт',
        description: null,
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
        totalSeats: 50,
      });

      expect(result.description).toBeNull();
    });

    it('mapEventUpdateToDto trims strings and converts dates to ISO', () => {
      const result = mapEventUpdateToDto({
        title: '  Обновленный концерт  ',
        description: '  Новое описание  ',
        startAt: new Date('2026-09-21T18:00:00.000Z'),
        endAt: new Date('2026-09-21T21:00:00.000Z'),
      });

      expect(result).toEqual({
        title: 'Обновленный концерт',
        description: 'Новое описание',
        startAt: '2026-09-21T18:00:00.000Z',
        endAt: '2026-09-21T21:00:00.000Z',
      });
    });

    it('mapEventUpdateToDto handles null description', () => {
      const result = mapEventUpdateToDto({
        title: 'Концерт',
        description: null,
        startAt: new Date('2026-09-20T18:00:00.000Z'),
        endAt: new Date('2026-09-20T21:00:00.000Z'),
      });

      expect(result.description).toBeNull();
    });
  });

  describe('createEvent', () => {
    const validDomainEvent = {
      title: 'Новый фестиваль',
      description: 'Фестиваль музыки',
      startAt: new Date('2026-09-25T10:00:00.000Z'),
      endAt: new Date('2026-09-25T22:00:00.000Z'),
      totalSeats: 500,
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
      };

      const postSpy = vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 201 }),
      });

      const result = await createEvent(validDomainEvent);

      expect(postSpy).toHaveBeenCalledWith('/v1/events', {
        body: mapEventCreateToDto(validDomainEvent),
      });

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.statusUrl).toBeNull();
      }
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Validation Error',
        status: 400,
        detail: 'Неверные данные события',
        errors: { title: ['Название обязательно'] },
      };

      vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as never);

      const result = await createEvent(validDomainEvent);

      expect(result).toEqual({
        success: false,
        httpStatus: 400,
        error: {
          message: 'Неверные данные события',
          fieldErrors: { title: ['Название обязательно'] },
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Validation Error',
        errors: { totalSeats: ['Должно быть больше 0'] },
      };

      vi.spyOn(eventsClient, 'POST').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 422 }),
      } as unknown as never);

      const result = await createEvent(validDomainEvent);

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(422);
      expect(result.error).toBeDefined();
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(eventsClient, 'POST').mockRejectedValueOnce(new Error('Network failure'));

      const result = await createEvent(validDomainEvent);

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

  describe('updateEventById', () => {
    const validUpdate = {
      title: 'Обновленный заголовок',
      description: 'Новое описание',
      startAt: new Date('2026-09-25T12:00:00.000Z'),
      endAt: new Date('2026-09-25T15:00:00.000Z'),
    };

    it('updates event successfully and returns status 200', async () => {
      const putSpy = vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 200 }),
      });

      const result = await updateEventById('event_1', validUpdate);

      expect(putSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        body: mapEventUpdateToDto(validUpdate),
        params: { path: { id: 'event_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 200,
      });
    });

    it('updates event with 204 status response', async () => {
      const putSpy = vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await updateEventById('event_1', validUpdate);

      expect(putSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        body: mapEventUpdateToDto(validUpdate),
        params: { path: { id: 'event_1' } },
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
        detail: 'Событие не найдено',
      };

      vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      });

      const result = await updateEventById('unknown_event', validUpdate);

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Событие не найдено',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Bad Request',
        errors: { title: ['Title invalid'] },
      };

      vi.spyOn(eventsClient, 'PUT').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      });

      const result = await updateEventById('event_1', validUpdate);

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(eventsClient, 'PUT').mockRejectedValueOnce(new Error('Update failed'));

      const result = await updateEventById('event_1', validUpdate);

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Update failed',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('getEventById', () => {
    it('returns event response with mapped dates on success', async () => {
      const mockDto = {
        id: 'event_123',
        title: 'Конференция',
        description: 'Описание конференции',
        startAt: '2026-10-01T09:00:00.000Z',
        endAt: '2026-10-01T17:00:00.000Z',
        totalSeats: 300,
        availableSeats: 250,
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
        },
        httpStatus: 200,
      });
    });

    it('falls back to default values for missing or null DTO fields', async () => {
      const mockDto = {
        id: undefined,
        title: undefined,
        description: undefined,
        startAt: undefined,
        endAt: undefined,
        totalSeats: undefined,
        availableSeats: undefined,
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      } as unknown as never);

      const result = await getEventById('event_empty');

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.event.id).toBe('');
        expect(result.event.title).toBe('');
        expect(result.event.description).toBeNull();
        expect(result.event.startAt).toBeInstanceOf(Date);
        expect(result.event.endAt).toBeInstanceOf(Date);
        expect(result.event.totalSeats).toBe(0);
        expect(result.event.availableSeats).toBe(0);
      }
    });

    it('calls GET /v1/events/{id} with correct path param', async () => {
      const getSpy = vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: {
          id: 'event_1',
          title: 'E1',
          description: null,
          startAt: '2026-09-25T10:00:00.000Z',
          endAt: '2026-09-25T12:00:00.000Z',
          totalSeats: 10,
          availableSeats: 10,
        },
        error: undefined,
        response: new Response(JSON.stringify({ id: 'event_1' }), { status: 200 }),
      });

      await getEventById('event_1');

      expect(getSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        params: { path: { id: 'event_1' } },
      });
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Not Found',
        status: 404,
        detail: 'Событие не найдено',
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      } as unknown as never);

      const result = await getEventById('unknown');

      expect(result).toEqual({
        success: false,
        httpStatus: 404,
        error: {
          message: 'Событие не найдено',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Bad Request',
        errors: { id: ['Invalid UUID format'] },
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as never);

      const result = await getEventById('bad_uuid');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(eventsClient, 'GET').mockRejectedValueOnce(new Error('Event lookup failed'));

      const result = await getEventById('e1');

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Event lookup failed',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('getTopEvents', () => {
    it('returns array of mapped top events on success', async () => {
      const mockList = [
        {
          id: 'top_1',
          title: 'Популярное событие 1',
          description: 'Описание 1',
          startAt: '2026-09-30T15:00:00.000Z',
          endAt: '2026-09-30T18:00:00.000Z',
          totalSeats: 100,
          availableSeats: 5,
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
            title: 'Популярное событие 1',
            description: 'Описание 1',
            startAt: new Date('2026-09-30T15:00:00.000Z'),
            endAt: new Date('2026-09-30T18:00:00.000Z'),
            totalSeats: 100,
            availableSeats: 5,
          },
        ],
        httpStatus: 200,
      });
    });

    it('handles non-array response by returning error with response status', async () => {
      const nonArrayData = { message: 'Unexpected payload structure' };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: nonArrayData,
        error: undefined,
        response: new Response(JSON.stringify(nonArrayData), { status: 200 }),
      } as unknown as Awaited<ReturnType<typeof eventsClient.GET>>);

      const result = await getTopEvents();

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(200);
      expect(result.error).toBeDefined();
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Error',
        status: 500,
        detail: 'Не удалось загрузить топ событий',
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 500 }),
      } as unknown as never);

      const result = await getTopEvents();

      expect(result).toEqual({
        success: false,
        httpStatus: 500,
        error: {
          message: 'Не удалось загрузить топ событий',
          fieldErrors: undefined,
        },
      });
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(eventsClient, 'GET').mockRejectedValueOnce(new Error('Top events query failed'));

      const result = await getTopEvents();

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Top events query failed',
          fieldErrors: undefined,
        },
      });
    });
  });

  describe('getAllEvents', () => {
    it('passes query parameters and returns paginated result with mapped events', async () => {
      const mockPaginatedDto = {
        totalCount: 45,
        currentPage: 2,
        pageSize: 10,
        totalPages: 5,
        items: [
          {
            id: 'event_paginated_1',
            title: 'Событие 1',
            description: null,
            startAt: '2026-10-05T10:00:00.000Z',
            endAt: '2026-10-05T12:00:00.000Z',
            totalSeats: 50,
            availableSeats: 25,
          },
        ],
      };

      const getSpy = vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockPaginatedDto,
        error: undefined,
        response: new Response(JSON.stringify(mockPaginatedDto), { status: 200 }),
      });

      const result = await getAllEvents('Музыка', '2026-10-01', '2026-10-10', 2, 10);

      expect(getSpy).toHaveBeenCalledWith('/v1/events', {
        params: {
          query: {
            Title: 'Музыка',
            From: '2026-10-01',
            To: '2026-10-10',
            Page: 2,
            PageSize: 10,
          },
        },
      });

      expect(result).toEqual({
        success: true,
        event: {
          totalCount: 45,
          currentPage: 2,
          pageSize: 10,
          totalPages: 5,
          items: {
            event: [
              {
                id: 'event_paginated_1',
                title: 'Событие 1',
                description: null,
                startAt: new Date('2026-10-05T10:00:00.000Z'),
                endAt: new Date('2026-10-05T12:00:00.000Z'),
                totalSeats: 50,
                availableSeats: 25,
              },
            ],
          },
        },
        httpStatus: 200,
      });
    });

    it('handles null items in paginated DTO and falls back to default numbers', async () => {
      const mockDto = {
        totalCount: undefined,
        currentPage: undefined,
        pageSize: undefined,
        totalPages: undefined,
        items: null,
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: mockDto,
        error: undefined,
        response: new Response(JSON.stringify(mockDto), { status: 200 }),
      } as unknown as Awaited<ReturnType<typeof eventsClient.GET>>);

      const result = await getAllEvents();

      expect(result).toEqual({
        success: true,
        event: {
          totalCount: 0,
          currentPage: 0,
          pageSize: 0,
          totalPages: 0,
          items: {
            event: null,
          },
        },
        httpStatus: 200,
      });
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Bad Request',
        status: 400,
        detail: 'Неверные параметры пагинации',
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as never);

      const result = await getAllEvents(undefined, undefined, undefined, -1, 10);

      expect(result).toEqual({
        success: false,
        httpStatus: 400,
        error: {
          message: 'Неверные параметры пагинации',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Bad Request',
        errors: { Page: ['Страница должна быть положительной'] },
      };

      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 400 }),
      } as unknown as never);

      const result = await getAllEvents(undefined, undefined, undefined, 0, 10);

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(400);
      expect(result.error).toBeDefined();
    });

    it('handles generic client error response', async () => {
      vi.spyOn(eventsClient, 'GET').mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Server down' },
        response: new Response(null, { status: 503 }),
      } as unknown as never);

      const result = await getAllEvents();

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(503);
    });
  });

  describe('deleteEventById', () => {
    it('deletes event successfully and returns status 204', async () => {
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

    it('deletes event and handles ok response with status 200', async () => {
      const deleteSpy = vi.spyOn(eventsClient, 'DELETE').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 200 }),
      });

      const result = await deleteEventById('event_del_1');

      expect(deleteSpy).toHaveBeenCalledWith('/v1/events/{id}', {
        params: { path: { id: 'event_del_1' } },
      });
      expect(result).toEqual({
        success: true,
        httpStatus: 200,
      });
    });

    it('handles ProblemDetails response with status', async () => {
      const problem = {
        title: 'Forbidden',
        status: 403,
        detail: 'Вы не являетесь автором события',
      };

      vi.spyOn(eventsClient, 'DELETE').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 403 }),
      });

      const result = await deleteEventById('event_forbidden');

      expect(result).toEqual({
        success: false,
        httpStatus: 403,
        error: {
          message: 'Вы не являетесь автором события',
          fieldErrors: undefined,
        },
      });
    });

    it('handles ProblemDetails response without status using response.status', async () => {
      const problem = {
        title: 'Error',
        errors: { id: ['Cannot delete event with active bookings'] },
      };

      vi.spyOn(eventsClient, 'DELETE').mockResolvedValueOnce({
        data: problem,
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 409 }),
      });

      const result = await deleteEventById('event_booked');

      expect(result.success).toBe(false);
      expect(result.httpStatus).toBe(409);
      expect(result.error).toBeDefined();
    });

    it('catches generic errors and returns httpStatus: 0', async () => {
      vi.spyOn(eventsClient, 'DELETE').mockRejectedValueOnce(new Error('Delete request failed'));

      const result = await deleteEventById('event_1');

      expect(result).toEqual({
        success: false,
        httpStatus: 0,
        error: {
          message: 'Delete request failed',
          fieldErrors: undefined,
        },
      });
    });
  });
});
