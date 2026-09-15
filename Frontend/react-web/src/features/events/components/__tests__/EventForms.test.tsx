import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
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
        },
      },
    });

    expect(screen.getByText('Не удалось сохранить форму')).toBeInTheDocument();
    expect(screen.getByText('Название обязательно')).toBeInTheDocument();
    expect(screen.getByText('Некорректная дата начала')).toBeInTheDocument();
    expect(screen.getByText('Дата окончания должна быть позже даты начала')).toBeInTheDocument();
    expect(screen.getByText('Мест должно быть не менее 1')).toBeInTheDocument();
    expect(screen.getByText('Описание слишком длинное')).toBeInTheDocument();
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
  };

  it('renders event details, ID badge, and formatted dates', () => {
    render(<EventResponseForm event={baseEvent} />);

    expect(screen.getByRole('heading', { name: 'Событие успешно создано' })).toBeInTheDocument();
    expect(screen.getByText(`ID: ${baseEvent.id}`)).toBeInTheDocument();
    expect(screen.getByText(baseEvent.title)).toBeInTheDocument();
    expect(screen.getByText(baseEvent.description!)).toBeInTheDocument();
    expect(screen.getByText('Всего мест:')).toBeInTheDocument();
    expect(screen.getByText('150', { selector: 'span' })).toBeInTheDocument();
  });

  it('handles optional description and dates when empty or null', () => {
    const eventWithoutDesc: EventResponse = {
      ...baseEvent,
      description: null,
      startAt: null as unknown as Date,
      endAt: null as unknown as Date,
    };

    render(<EventResponseForm event={eventWithoutDesc} />);

    expect(screen.queryByText(baseEvent.description!)).not.toBeInTheDocument();
    const dashes = screen.getAllByText('—');
    expect(dashes.length).toBeGreaterThanOrEqual(2);
  });

  it('satisfies WCAG accessibility standards', async () => {
    const { container } = render(<EventResponseForm event={baseEvent} />);
    await expect(checkA11y(container)).resolves.toEqual([]);
  });
});
