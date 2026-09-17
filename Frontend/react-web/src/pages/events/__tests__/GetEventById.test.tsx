import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { loader, action, GetEventByIdPage } from '../GetEventById';
import { getEventById, deleteEventById } from '@/features/events/api/eventsApi';
import { setToken, clearToken } from '@/shared/lib/auth';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  getEventById: vi.fn(),
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

function createLoaderArgs(id?: string, url = 'http://localhost/events/ev-1'): LoaderArgs {
  return {
    request: new Request(url),
    params: id ? { id } : {},
    context: {} as LoaderArgs['context'],
  } as unknown as LoaderArgs;
}

function createActionArgs(
  id?: string,
  formDataRecord: Record<string, string> = { intent: 'delete' },
  url = 'http://localhost/events/ev-1',
): ActionArgs {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  const request = new Request(url, {
    method: 'POST',
    body: formData,
  });
  return {
    request,
    params: id ? { id } : {},
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

describe('GetEventById', () => {
  const mockEvent: EventResponse = {
    id: 'ev-test-1',
    title: 'Архитектурный митап',
    description: 'Обсуждение микросервисной архитектуры и EDA',
    startAt: new Date('2026-10-15T10:00:00Z'),
    endAt: new Date('2026-10-15T13:00:00Z'),
    totalSeats: 50,
    availableSeats: 25,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    clearToken();
  });

  describe('loader function', () => {
    it('throws 404 Response when params.id is missing', async () => {
      const args = createLoaderArgs(undefined);

      await expect(loader(args)).rejects.toSatisfy((err: Response) => {
        expect(err).toBeInstanceOf(Response);
        expect(err.status).toBe(404);
        expect(err.statusText).toBe('Not Found');
        return true;
      });
    });

    it('throws 404 Response when API returns 404 Not Found', async () => {
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

    it('throws 403 Response when API returns 403 Forbidden', async () => {
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 403,
        error: { message: 'Доступ ограничен' },
      });

      const args = createLoaderArgs('ev-restricted');

      await expect(loader(args)).rejects.toSatisfy((err: Response) => {
        expect(err).toBeInstanceOf(Response);
        expect(err.status).toBe(403);
        expect(err.statusText).toBe('Forbidden');
        return true;
      });
    });

    it('throws generic 500 Response when API returns other failure status', async () => {
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 500,
        error: { message: 'Внутренняя ошибка сервера' },
      });

      const args = createLoaderArgs('ev-crash');

      await expect(loader(args)).rejects.toSatisfy((err: Response) => {
        expect(err).toBeInstanceOf(Response);
        expect(err.status).toBe(500);
        return true;
      });
    });

    it('throws 500 Response with fallback message when error.message is missing and status is 0', async () => {
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
      expect(await res.text()).toBe('Не удалось загрузить событие');
    });

    it('returns event data on successful API response', async () => {
      vi.mocked(getEventById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
        event: mockEvent,
      });

      const res = await loader(createLoaderArgs('ev-test-1'));
      expect(res).toEqual({ event: mockEvent });
    });
  });

  describe('action function', () => {
    it('redirects unauthenticated user to /login?returnUrl=...', async () => {
      clearToken();
      const args = createActionArgs(
        'ev-test-1',
        { intent: 'delete' },
        'http://localhost/events/ev-test-1',
      );

      const res = await action(args);
      expect(res).toBeInstanceOf(Response);
      const response = res as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/login?returnUrl=%2Fevents%2Fev-test-1');
    });

    it('returns form error when id is missing from params', async () => {
      setToken(createMockJwt());
      const args = createActionArgs(undefined, { intent: 'delete' });

      const res = await action(args);
      expect(res).toMatchObject({
        error: {
          message: 'Идентификатор события не указан',
        },
      });
    });

    it('returns null when intent is not "delete"', async () => {
      setToken(createMockJwt());
      const args = createActionArgs('ev-test-1', { intent: 'unknown' });

      const res = await action(args);
      expect(res).toBeNull();
    });

    it('calls deleteEventById and returns error when API deletion fails', async () => {
      setToken(createMockJwt());
      vi.mocked(deleteEventById).mockResolvedValueOnce({
        success: false,
        httpStatus: 400,
        error: { message: 'Нельзя удалить событие с активными бронями' },
      });

      const args = createActionArgs('ev-test-1', { intent: 'delete' });
      const res = await action(args);

      expect(deleteEventById).toHaveBeenCalledWith('ev-test-1');
      expect(res).toMatchObject({
        error: {
          message: 'Нельзя удалить событие с активными бронями',
        },
      });
    });

    it('calls deleteEventById and redirects to "/" on success', async () => {
      setToken(createMockJwt());
      vi.mocked(deleteEventById).mockResolvedValueOnce({
        success: true,
        httpStatus: 200,
      });

      const args = createActionArgs('ev-test-1', { intent: 'delete' });
      const res = await action(args);

      expect(deleteEventById).toHaveBeenCalledWith('ev-test-1');
      expect(res).toBeInstanceOf(Response);
      const response = res as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
    });
  });

  describe('Component Rendering & Interactions', () => {
    function renderGetEventByIdPage(event = mockEvent, actionData: unknown = null) {
      const router = createMemoryRouter(
        [
          {
            path: '/events/:id',
            element: <GetEventByIdPage />,
            loader: () => ({ event }),
            action: () => actionData,
            HydrateFallback: () => null,
          },
          {
            path: '/',
            element: <div>Главная афиша</div>,
          },
          {
            path: '/events/:id/edit',
            element: <div>Редактирование события</div>,
          },
          {
            path: '/bookings/create/:id',
            element: <div>Бронирование билетов</div>,
          },
        ],
        { initialEntries: [`/events/${event.id}`] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders event details, ID, back link, edit link, and booking link', async () => {
      renderGetEventByIdPage();

      expect(await screen.findByRole('heading', { name: mockEvent.title })).toBeInTheDocument();
      expect(screen.getByText(`ID: ${mockEvent.id}`)).toBeInTheDocument();
      expect(screen.getByText(mockEvent.description!)).toBeInTheDocument();

      expect(screen.getByText('Свободно мест: 25 из 50')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: '← Назад к афише' })).toHaveAttribute('href', '/');
      expect(screen.getByRole('link', { name: '✏️ Редактировать' })).toHaveAttribute(
        'href',
        `/events/${mockEvent.id}/edit`,
      );
      expect(screen.getByRole('link', { name: 'Забронировать места' })).toHaveAttribute(
        'href',
        `/bookings/create/${mockEvent.id}`,
      );
    });

    it('renders fallback when description is missing', async () => {
      renderGetEventByIdPage({ ...mockEvent, description: null });

      expect(await screen.findByText('Описание не указано')).toBeInTheDocument();
    });

    it('renders "Мест нет" and disabled "Все места распроданы" button when availableSeats = 0', async () => {
      renderGetEventByIdPage({ ...mockEvent, availableSeats: 0 });

      expect(await screen.findByText('Мест нет')).toBeInTheDocument();
      const soldOutBtn = screen.getByRole('button', { name: 'Все места распроданы' });
      expect(soldOutBtn).toBeDisabled();
      expect(screen.queryByRole('link', { name: 'Забронировать места' })).not.toBeInTheDocument();
    });

    it('renders "Осталось мест: 3" badge when availableSeats = 3', async () => {
      renderGetEventByIdPage({ ...mockEvent, availableSeats: 3 });

      expect(await screen.findByText('Осталось мест: 3')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Забронировать места' })).toBeInTheDocument();
    });

    it('displays error banner when actionData contains error after delete submission', async () => {
      const user = userEvent.setup();
      vi.spyOn(window, 'confirm').mockReturnValue(true);

      renderGetEventByIdPage(mockEvent, {
        error: { message: 'Ошибка при удалении: событие содержит активные брони' },
      });

      const deleteBtn = await screen.findByRole('button', { name: /удалить/i });
      await user.click(deleteBtn);

      expect(
        await screen.findByText('Ошибка при удалении: событие содержит активные брони'),
      ).toBeInTheDocument();
    });

    it('cancels deletion when user clicks Cancel in window.confirm', async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

      renderGetEventByIdPage();

      const deleteBtn = await screen.findByRole('button', { name: /удалить/i });
      await user.click(deleteBtn);

      expect(confirmSpy).toHaveBeenCalledWith(
        'Вы уверены, что хотите удалить это событие? Это действие необратимо.',
      );
      // Remained on page, heading still present
      expect(screen.getByRole('heading', { name: mockEvent.title })).toBeInTheDocument();
    });

    it('satisfies WCAG accessibility rules', async () => {
      const { container } = renderGetEventByIdPage();
      await screen.findByRole('heading', { name: mockEvent.title });

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
