import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, action, EditEventPage } from '../EditEventPage';
import { getEventById, updateEventById, deleteEventById } from '@/features/events/api/eventsApi';
import { setToken, clearToken } from '@/shared/lib/auth';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  getEventById: vi.fn(),
  updateEventById: vi.fn(),
  deleteEventById: vi.fn(),
}));

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
  url = eventId ? `http://localhost/events/${eventId}/edit` : 'http://localhost/events/edit',
  paramKey: 'eventId' | 'id' = 'eventId',
): LoaderArgs {
  return {
    request: new Request(url),
    params: eventId ? { [paramKey]: eventId } : {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

function createActionArgs(
  eventId?: string,
  formDataRecord: Record<string, string> = {},
  paramKey: 'eventId' | 'id' = 'eventId',
): ActionArgs {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  const request = new Request('http://localhost/events/ev-1/edit', {
    method: 'POST',
    body: formData,
  });
  return {
    request,
    params: eventId ? { [paramKey]: eventId } : {},
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

describe('EditEventPage', () => {
  const mockEvent: EventResponse = {
    id: 'ev-edit-1',
    title: 'TypeScript 6 Workshop',
    description: 'Глубокое погружение в систему типов',
    startAt: new Date('2026-11-10T10:00:00Z'),
    endAt: new Date('2026-11-10T16:00:00Z'),
    totalSeats: 30,
    availableSeats: 15,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    clearToken();
  });

  describe('loader function', () => {
    it('redirects unauthenticated user to /login?returnUrl=...', async () => {
      clearToken();
      const args = createLoaderArgs('ev-edit-1');

      const res = await loader(args);
      expect(res).toBeInstanceOf(Response);
      const response = res as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe(
        '/login?returnUrl=%2Fevents%2Fev-edit-1%2Fedit',
      );
    });

    it('throws 404 Response when eventId is missing from params', async () => {
      setToken(createMockJwt());
      const args = createLoaderArgs(undefined);

      await expect(loader(args)).rejects.toSatisfy((err: Response) => {
        expect(err).toBeInstanceOf(Response);
        expect(err.status).toBe(404);
        return true;
      });
    });

    it('throws 404 Response when getEventById returns success: false', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 404,
        error: { message: 'Событие не найдено' },
      });

      const args = createLoaderArgs('ev-missing');
      await expect(loader(args)).rejects.toSatisfy((err: Response) => {
        expect(err).toBeInstanceOf(Response);
        expect(err.status).toBe(404);
        return true;
      });
    });

    it('returns event on successful fetch using params.eventId or fallback params.id', async () => {
      setToken(createMockJwt());
      vi.mocked(getEventById).mockResolvedValue({
        success: true,
        httpStatus: 200,
        event: mockEvent,
      });

      const resEventId = await loader(createLoaderArgs('ev-edit-1', undefined, 'eventId'));
      expect(resEventId).toEqual({ event: mockEvent });

      const resId = await loader(createLoaderArgs('ev-edit-1', undefined, 'id'));
      expect(resId).toEqual({ event: mockEvent });
    });
  });

  describe('action function', () => {
    beforeEach(() => {
      setToken(createMockJwt());
    });

    it('redirects unauthenticated user to /login?returnUrl=...', async () => {
      clearToken();
      const args = createActionArgs('ev-edit-1', { title: 'New' });
      const res = await action(args);
      expect(res).toBeInstanceOf(Response);
      const response = res as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toContain('/login?returnUrl=');
    });

    it('returns error when eventId is missing from params', async () => {
      const args = createActionArgs(undefined, { title: 'New' });
      const res = await action(args);

      expect(res).toMatchObject({
        error: {
          message: 'Идентификатор события не найден',
        },
      });
    });

    describe('intent="delete"', () => {
      it('calls deleteEventById and returns error on delete failure', async () => {
        vi.mocked(deleteEventById).mockResolvedValueOnce({
          success: false,
          httpStatus: 400,
          error: { message: 'Нельзя удалить активное событие' },
        });

        const args = createActionArgs('ev-edit-1', { intent: 'delete' });
        const res = await action(args);

        expect(deleteEventById).toHaveBeenCalledWith('ev-edit-1');
        expect(res).toMatchObject({
          error: {
            message: 'Нельзя удалить активное событие',
          },
        });
      });

      it('calls deleteEventById and redirects to "/" on success', async () => {
        vi.mocked(deleteEventById).mockResolvedValueOnce({
          success: true,
          httpStatus: 200,
        });

        const args = createActionArgs('ev-edit-1', { intent: 'delete' });
        const res = await action(args);

        expect(deleteEventById).toHaveBeenCalledWith('ev-edit-1');
        expect(res).toBeInstanceOf(Response);
        const response = res as Response;
        expect(response.status).toBe(302);
        expect(response.headers.get('Location')).toBe('/');
      });
    });

    describe('update event validation & API', () => {
      it('validates empty fields and date sequence (endDate <= startDate)', async () => {
        const argsEmpty = createActionArgs('ev-edit-1', {});
        const resEmpty = await action(argsEmpty);
        expect(resEmpty).toMatchObject({
          error: {
            fieldErrors: {
              title: ['Название события обязательно для заполнения'],
              startAt: ['Укажите дату и время начала'],
              endAt: ['Укажите дату и время окончания'],
            },
          },
        });

        const argsInvalidDates = createActionArgs('ev-edit-1', {
          title: 'Title',
          startAt: 'invalid',
          endAt: 'invalid',
        });
        const resInvalid = await action(argsInvalidDates);
        expect(resInvalid).toMatchObject({
          error: {
            fieldErrors: {
              startAt: ['Некорректный формат даты начала'],
              endAt: ['Некорректный формат даты окончания'],
            },
          },
        });

        const argsDateOrder = createActionArgs('ev-edit-1', {
          title: 'Title',
          startAt: '2026-11-10T16:00',
          endAt: '2026-11-10T10:00',
        });
        const resDateOrder = await action(argsDateOrder);
        expect(resDateOrder).toMatchObject({
          error: {
            fieldErrors: {
              endAt: ['Дата окончания должна быть позже даты начала'],
            },
          },
        });
      });

      it('normalizes empty description to null and calls updateEventById', async () => {
        vi.mocked(updateEventById).mockResolvedValueOnce({
          success: true,
          httpStatus: 200,
        });

        const args = createActionArgs('ev-edit-1', {
          title: 'Updated Title',
          startAt: '2026-11-10T10:00',
          endAt: '2026-11-10T12:00',
          description: '   ',
        });
        const res = await action(args);

        expect(updateEventById).toHaveBeenCalledWith(
          'ev-edit-1',
          expect.objectContaining({
            title: 'Updated Title',
            description: null,
          }),
        );
        expect(res).toBeInstanceOf(Response);
        const response = res as Response;
        expect(response.status).toBe(302);
        expect(response.headers.get('Location')).toBe('/events/ev-edit-1');
      });

      it('returns error when updateEventById returns success: false', async () => {
        vi.mocked(updateEventById).mockResolvedValueOnce({
          success: false,
          httpStatus: 400,
          error: { message: 'Название занято' },
        });

        const args = createActionArgs('ev-edit-1', {
          title: 'Updated Title',
          startAt: '2026-11-10T10:00',
          endAt: '2026-11-10T12:00',
          description: 'Valid Desc',
        });
        const res = await action(args);

        expect(res).toMatchObject({
          error: {
            message: 'Название занято',
          },
        });
      });

      it('catches unexpected JS error and converts to form error', async () => {
        vi.mocked(updateEventById).mockRejectedValueOnce(new Error('Update crashed'));

        const args = createActionArgs('ev-edit-1', {
          title: 'Updated Title',
          startAt: '2026-11-10T10:00',
          endAt: '2026-11-10T12:00',
        });
        const res = await action(args);

        expect(res).toMatchObject({
          error: {
            message: 'Update crashed',
          },
        });
      });

      it('re-throws when caught error is an instance of Response', async () => {
        const responseError = new Response('Conflict', { status: 409 });
        vi.mocked(updateEventById).mockRejectedValueOnce(responseError);

        const args = createActionArgs('ev-edit-1', {
          title: 'Updated Title',
          startAt: '2026-11-10T10:00',
          endAt: '2026-11-10T12:00',
        });

        await expect(action(args)).rejects.toBe(responseError);
      });
    });
  });

  describe('Component Rendering & Interactions', () => {
    function renderEditEventPage(event = mockEvent, actionData: unknown = null) {
      const router = createMemoryRouter(
        [
          {
            path: '/events/:id/edit',
            element: <EditEventPage />,
            loader: () => ({ event }),
            action: () => actionData,
            HydrateFallback: () => null,
          },
          {
            path: '/events/:id',
            element: <div>Детали события</div>,
          },
        ],
        { initialEntries: [`/events/${event.id}/edit`] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders heading, back link and pre-populated form fields', async () => {
      renderEditEventPage();

      expect(
        await screen.findByRole('heading', { name: 'Редактирование события' }),
      ).toBeInTheDocument();
      expect(screen.getByRole('link', { name: '← Назад к событию' })).toHaveAttribute(
        'href',
        `/events/${mockEvent.id}`,
      );

      expect(screen.getByLabelText(/название события/i)).toHaveValue(mockEvent.title);
      expect(screen.getByLabelText(/описание события/i)).toHaveValue(mockEvent.description);
      expect(screen.getByRole('button', { name: 'Сохранить изменения' })).toBeEnabled();
      expect(screen.getByRole('button', { name: /удалить событие/i })).toBeEnabled();
    });

    it('displays error messages when actionData contains validation error', async () => {
      renderEditEventPage(mockEvent, {
        error: {
          message: 'Пожалуйста, исправьте ошибки в форме',
          fieldErrors: {
            title: ['Название события обязательно для заполнения'],
          },
        },
      });

      const submitBtn = await screen.findByRole('button', { name: 'Сохранить изменения' });
      fireEvent.submit(submitBtn.closest('form')!);

      expect(await screen.findByText('Пожалуйста, исправьте ошибки в форме')).toBeInTheDocument();
      expect(screen.getByText('Название события обязательно для заполнения')).toBeInTheDocument();
    });

    it('satisfies WCAG accessibility standards', async () => {
      const { container } = renderEditEventPage();
      await screen.findByRole('heading', { name: 'Редактирование события' });

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
