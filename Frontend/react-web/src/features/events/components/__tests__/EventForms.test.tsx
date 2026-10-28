import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { EventRequestForm } from '../EventRequestForm';
import { EventResponseForm } from '../EventResponseForm';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

function renderRequestForm(props: React.ComponentProps<typeof EventRequestForm>) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <EventRequestForm {...props} />,
        action: () => null,
      },
    ],
    { initialEntries: ['/'] },
  );

  return render(<RouterProvider router={router} />);
}

function renderResponseForm(event: EventResponse, onCreateAnother?: () => void) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <EventResponseForm event={event} onCreateAnother={onCreateAnother} />,
      },
      {
        path: '/events/:id',
        element: <div>Детали события</div>,
      },
    ],
    { initialEntries: ['/'] },
  );

  return render(<RouterProvider router={router} />);
}

describe('EventRequestForm', () => {
  it('renders all default form fields with totalSeats=50', () => {
    renderRequestForm({ isSubmitting: false });

    expect(screen.getByLabelText('Название события')).toHaveValue('');
    expect(screen.getByLabelText('Начало')).toHaveValue('');
    expect(screen.getByLabelText('Окончание')).toHaveValue('');
    expect(screen.getByLabelText('Количество мест')).toHaveValue(50);
    expect(screen.getByLabelText('Описание')).toHaveValue('');

    const submitBtn = screen.getByRole('button', { name: 'Создать событие' });
    expect(submitBtn).toBeEnabled();
  });

  it('renders global error banner and individual field error messages', () => {
    renderRequestForm({
      isSubmitting: false,
      error: {
        message: 'Не удалось сохранить форму',
        fieldErrors: {
          title: ['Название обязательно'],
          startAt: ['Некорректная дата начала'],
          endAt: ['Дата окончания должна быть позже даты начала'],
          totalSeats: ['Мест должно быть не менее 1'],
          description: ['Описание слишком длинное'],
          price: ['Цена не может быть отрицательной'],
          currency: ['Код валюты должен содержать максимум 3 символа'],
        },
      },
    });

    expect(screen.getByText('Не удалось сохранить форму')).toBeInTheDocument();
    expect(screen.getByText('Название обязательно')).toBeInTheDocument();
    expect(screen.getByText('Некорректная дата начала')).toBeInTheDocument();
    expect(screen.getByText('Дата окончания должна быть позже даты начала')).toBeInTheDocument();
    expect(screen.getByText('Мест должно быть не менее 1')).toBeInTheDocument();
    expect(screen.getByText('Описание слишком длинное')).toBeInTheDocument();
    expect(screen.getByText('Цена не может быть отрицательной')).toBeInTheDocument();
    expect(screen.getByText('Код валюты должен содержать максимум 3 символа')).toBeInTheDocument();
  });

  it('disables submit button and displays "Создание..." when isSubmitting is true', () => {
    renderRequestForm({ isSubmitting: true });

    const submitBtn = screen.getByRole('button', { name: 'Создание...' });
    expect(submitBtn).toBeDisabled();
  });

  it('satisfies WCAG accessibility standards', async () => {
    const { container } = renderRequestForm({ isSubmitting: false });
    await expect(checkA11y(container)).resolves.toEqual([]);
  });
});

describe('EventResponseForm', () => {
  const baseEvent: EventResponse = {
    id: 'ev-resp-101',
    title: 'Distributed Systems Summit',
    description: 'Конференция по распределенным базам данных и консенсусу',
    startAt: new Date('2026-11-20T10:00:00Z'),
    endAt: new Date('2026-11-20T18:00:00Z'),
    totalSeats: 150,
    availableSeats: 150,
    price: 1500,
    currency: 'KZT',
    isActive: true,
  };

  it('renders event details, ID badge, formatted price and active status', () => {
    const handleCreateAnother = vi.fn();
    renderResponseForm(baseEvent, handleCreateAnother);

    expect(screen.getByRole('heading', { name: 'Событие успешно создано' })).toBeInTheDocument();
    expect(screen.getByText(`ID: ${baseEvent.id}`)).toBeInTheDocument();
    expect(screen.getByText(baseEvent.title)).toBeInTheDocument();
    expect(screen.getByText(baseEvent.description!)).toBeInTheDocument();
    expect(screen.getByText('Всего мест:')).toBeInTheDocument();
    expect(screen.getByText('Доступно мест:')).toBeInTheDocument();
    expect(screen.getAllByText('150').length).toBe(2);
    expect(screen.getByText('1 500 ₸')).toBeInTheDocument();
    expect(screen.getByText('Активно')).toBeInTheDocument();

    const eventLink = screen.getByRole('link', { name: /Перейти к событию/i });
    expect(eventLink).toHaveAttribute('href', `/events/${baseEvent.id}`);

    const createAnotherBtn = screen.getByRole('button', { name: /\+ Создать ещё одно/i });
    fireEvent.click(createAnotherBtn);
    expect(handleCreateAnother).toHaveBeenCalledTimes(1);
  });

  it('renders free price badge and cancelled badge correctly', () => {
    const freeCancelledEvent: EventResponse = {
      ...baseEvent,
      price: 0,
      currency: 'KZT',
      isActive: false,
    };

    renderResponseForm(freeCancelledEvent);

    expect(screen.getByText('Бесплатно')).toBeInTheDocument();
    expect(screen.getByText('Отменено')).toBeInTheDocument();
  });

  it('handles optional description and dates when empty or null', () => {
    const eventWithoutDesc: EventResponse = {
      ...baseEvent,
      description: null,
      startAt: null as unknown as Date,
      endAt: null as unknown as Date,
    };

    renderResponseForm(eventWithoutDesc);

    expect(screen.queryByText(baseEvent.description!)).not.toBeInTheDocument();
    const dashes = screen.getAllByText('—');
    expect(dashes.length).toBeGreaterThanOrEqual(2);
  });

  it('satisfies WCAG accessibility standards', async () => {
    const { container } = renderResponseForm(baseEvent);
    await expect(checkA11y(container)).resolves.toEqual([]);
  });
});
