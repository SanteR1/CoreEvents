import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { action, CreateEventPage } from '../CreateEventPage';
import { createEvent } from '@/features/events/api/eventsApi';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

vi.mock('@/features/events/api/eventsApi', () => ({
  createEvent: vi.fn(),
}));

type ActionArgs = Parameters<typeof action>[0];

function createActionArgs(formDataRecord: Record<string, string>): ActionArgs {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  const request = new Request('http://localhost/events/create', {
    method: 'POST',
    body: formData,
  });
  return {
    request,
    params: {},
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

describe('CreateEventPage', () => {
  const mockCreatedEvent: EventResponse = {
    id: 'ev-created-99',
    title: 'Kubernetes Deep Dive',
    description: 'Изучение CNI, CSI и операторов',
    startAt: new Date('2026-10-20T10:00:00Z'),
    endAt: new Date('2026-10-20T16:00:00Z'),
    totalSeats: 40,
    availableSeats: 40,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('action function - Form Validation & Boundaries', () => {
    it('returns field errors when all required fields are empty', async () => {
      const args = createActionArgs({});
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Пожалуйста, исправьте ошибки в форме',
          fieldErrors: {
            title: ['Название события обязательно для заполнения'],
            startAt: ['Укажите дату начала'],
            endAt: ['Укажите дату окончания'],
            totalSeats: ['Количество мест должно быть больше 0'],
          },
        },
      });
      expect(createEvent).not.toHaveBeenCalled();
    });

    it('returns error when title consists only of whitespace', async () => {
      const args = createActionArgs({
        title: '    ',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '30',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          fieldErrors: {
            title: ['Название события обязательно для заполнения'],
          },
        },
      });
    });

    it('validates invalid date strings', async () => {
      const args = createActionArgs({
        title: 'Valid Title',
        startAt: 'not-a-date',
        endAt: 'also-not-a-date',
        totalSeats: '30',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          fieldErrors: {
            startAt: ['Некорректный формат даты начала'],
            endAt: ['Некорректный формат даты окончания'],
          },
        },
      });
    });

    it('validates date boundaries: endAt < startAt', async () => {
      const args = createActionArgs({
        title: 'Valid Title',
        startAt: '2026-10-20T14:00',
        endAt: '2026-10-20T10:00',
        totalSeats: '30',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          fieldErrors: {
            endAt: ['Дата окончания должна быть позже даты начала'],
          },
        },
      });
    });

    it('validates date boundary: endAt === startAt (must be strictly later)', async () => {
      const args = createActionArgs({
        title: 'Valid Title',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T10:00',
        totalSeats: '30',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          fieldErrors: {
            endAt: ['Дата окончания должна быть позже даты начала'],
          },
        },
      });
    });

    it('validates totalSeats boundary: 0, negative and non-numeric numbers', async () => {
      const argsZero = createActionArgs({
        title: 'Valid Title',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '0',
      });
      const resZero = await action(argsZero);
      expect(resZero).toMatchObject({
        error: { fieldErrors: { totalSeats: ['Количество мест должно быть больше 0'] } },
      });

      const argsNeg = createActionArgs({
        title: 'Valid Title',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '-5',
      });
      const resNeg = await action(argsNeg);
      expect(resNeg).toMatchObject({
        error: { fieldErrors: { totalSeats: ['Количество мест должно быть больше 0'] } },
      });

      const argsNan = createActionArgs({
        title: 'Valid Title',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: 'abc',
      });
      const resNan = await action(argsNan);
      expect(resNan).toMatchObject({
        error: { fieldErrors: { totalSeats: ['Количество мест должно быть больше 0'] } },
      });
    });

    it('accepts totalSeats = 1 as valid lower boundary', async () => {
      vi.mocked(createEvent).mockResolvedValueOnce({
        success: true,
        httpStatus: 201,
        statusUrl: null,
        event: { ...mockCreatedEvent, totalSeats: 1, availableSeats: 1 },
      });

      const args = createActionArgs({
        title: 'Exclusive Mentorship',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T11:00',
        totalSeats: '1',
      });
      const result = await action(args);

      expect(createEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Exclusive Mentorship',
          totalSeats: 1,
        }),
      );
      expect(result).toEqual({
        event: { ...mockCreatedEvent, totalSeats: 1, availableSeats: 1 },
      });
    });

    it('creates event successfully when description is omitted', async () => {
      vi.mocked(createEvent).mockResolvedValueOnce({
        success: true,
        httpStatus: 201,
        statusUrl: null,
        event: { ...mockCreatedEvent, description: null },
      });

      const args = createActionArgs({
        title: 'Event without description',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '20',
      });

      const result = await action(args);
      expect(result).toEqual({
        event: { ...mockCreatedEvent, description: null },
      });
      expect(createEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Event without description',
          description: '',
          totalSeats: 20,
        }),
      );
    });

    it('creates event with trimmed description when description is provided', async () => {
      vi.mocked(createEvent).mockResolvedValueOnce({
        success: true,
        httpStatus: 201,
        statusUrl: null,
        event: { ...mockCreatedEvent, description: 'Подробное описание' },
      });

      const args = createActionArgs({
        title: 'Event with description',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '25',
        description: '   Подробное описание   ',
      });
      const result = await action(args);

      expect(result).toEqual({
        event: { ...mockCreatedEvent, description: 'Подробное описание' },
      });
      expect(createEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Event with description',
          description: 'Подробное описание',
          totalSeats: 25,
        }),
      );
    });

    it('returns API error when createEvent returns success: false', async () => {
      vi.mocked(createEvent).mockResolvedValueOnce({
        success: false,
        httpStatus: 400,
        error: {
          message: 'Ошибка валидации на сервере',
          fieldErrors: { title: ['Событие с таким названием уже существует'] },
        },
      });

      const args = createActionArgs({
        title: 'Duplicate Event',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '50',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Ошибка валидации на сервере',
          fieldErrors: { title: ['Событие с таким названием уже существует'] },
        },
      });
    });

    it('catches unexpected JS exceptions and converts to form error', async () => {
      vi.mocked(createEvent).mockRejectedValueOnce(new Error('Network offline'));

      const args = createActionArgs({
        title: 'Offline Event',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '50',
      });
      const result = await action(args);

      expect(result).toMatchObject({
        error: {
          message: 'Network offline',
        },
      });
    });

    it('re-throws when caught error is an instance of Response', async () => {
      const responseError = new Response('Unauthorized', { status: 401 });
      vi.mocked(createEvent).mockRejectedValueOnce(responseError);

      const args = createActionArgs({
        title: 'Auth Event',
        startAt: '2026-10-20T10:00',
        endAt: '2026-10-20T12:00',
        totalSeats: '50',
      });

      await expect(action(args)).rejects.toBe(responseError);
    });
  });

  describe('Component Rendering & Interactions', () => {
    function renderCreateEventPage(actionResult: unknown = null) {
      const router = createMemoryRouter(
        [
          {
            path: '/events/create',
            element: <CreateEventPage />,
            action: () => actionResult,
            HydrateFallback: () => null,
          },
        ],
        { initialEntries: ['/events/create'] },
      );

      return render(<RouterProvider router={router} />);
    }

    it('renders initial creation form with heading and inputs', async () => {
      renderCreateEventPage();

      expect(await screen.findByRole('heading', { name: 'Создание события' })).toBeInTheDocument();
      expect(screen.getByLabelText('Название события')).toBeInTheDocument();
      expect(screen.getByLabelText('Начало')).toBeInTheDocument();
      expect(screen.getByLabelText('Окончание')).toBeInTheDocument();
      expect(screen.getByLabelText('Количество мест')).toHaveValue(50);
      expect(screen.getByLabelText('Описание')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Создать событие' })).toBeEnabled();
    });

    it('displays validation errors under inputs when submission fails', async () => {
      renderCreateEventPage({
        error: {
          message: 'Пожалуйста, исправьте ошибки в форме',
          fieldErrors: {
            title: ['Название события обязательно для заполнения'],
            endAt: ['Дата окончания должна быть позже даты начала'],
          },
        },
      });

      const submitBtn = await screen.findByRole('button', { name: 'Создать событие' });
      fireEvent.submit(submitBtn.closest('form')!);

      expect(await screen.findByText('Пожалуйста, исправьте ошибки в форме')).toBeInTheDocument();
      expect(screen.getByText('Название события обязательно для заполнения')).toBeInTheDocument();
      expect(screen.getByText('Дата окончания должна быть позже даты начала')).toBeInTheDocument();
    });

    it('renders success confirmation screen when actionData contains event', async () => {
      renderCreateEventPage({
        event: mockCreatedEvent,
      });

      const submitBtn = await screen.findByRole('button', { name: 'Создать событие' });
      fireEvent.submit(submitBtn.closest('form')!);

      expect(await screen.findByRole('heading', { name: 'Событие создано!' })).toBeInTheDocument();
      expect(screen.getByText('Событие успешно опубликовано')).toBeInTheDocument();
      expect(screen.getByText('Событие успешно создано')).toBeInTheDocument();
      expect(screen.getByText(`ID: ${mockCreatedEvent.id}`)).toBeInTheDocument();
      expect(screen.getByText(mockCreatedEvent.title)).toBeInTheDocument();
      expect(screen.getByText(mockCreatedEvent.description!)).toBeInTheDocument();
      expect(screen.getByText('Всего мест:')).toBeInTheDocument();
    });

    it('satisfies WCAG accessibility rules for form and confirmation view', async () => {
      const { container } = renderCreateEventPage();
      await screen.findByRole('heading', { name: 'Создание события' });
      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
