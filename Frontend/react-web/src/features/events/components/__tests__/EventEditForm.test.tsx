import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { EventEditForm } from '../EventEditForm';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

function renderEditForm(props: React.ComponentProps<typeof EventEditForm>) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <EventEditForm {...props} />,
        action: () => null,
      },
    ],
    { initialEntries: ['/'] },
  );

  return render(<RouterProvider router={router} />);
}

describe('EventEditForm', () => {
  const baseEvent: EventResponse = {
    id: 'ev-edit-42',
    title: 'React 19 Patterns',
    description: 'Изучение Server Actions, useActionState и useOptimistic',
    startAt: new Date('2026-10-15T09:30:00Z'),
    endAt: new Date('2026-10-15T17:45:00Z'),
    totalSeats: 100,
    availableSeats: 45,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Form Fields & Pre-population', () => {
    it('pre-populates title, dates, and description from event prop', () => {
      renderEditForm({ event: baseEvent, isSubmitting: false });

      expect(screen.getByLabelText(/название события/i)).toHaveValue(baseEvent.title);
      expect(screen.getByLabelText(/описание события/i)).toHaveValue(baseEvent.description);

      const startInput = screen.getByLabelText<HTMLInputElement>(/дата и время начала/i);
      expect(startInput.value).toContain('2026-10-15T');

      const endInput = screen.getByLabelText<HTMLInputElement>(/дата и время окончания/i);
      expect(endInput.value).toContain('2026-10-15T');
    });

    it('handles null description by defaulting textarea to empty string', () => {
      renderEditForm({
        event: { ...baseEvent, description: null },
        isSubmitting: false,
      });

      expect(screen.getByLabelText(/описание события/i)).toHaveValue('');
    });

    it('handles invalid dates defensively by producing empty date string', () => {
      renderEditForm({
        event: {
          ...baseEvent,
          startAt: new Date('invalid-date'),
          endAt: new Date('invalid-date'),
        },
        isSubmitting: false,
      });

      const startInput = screen.getByLabelText<HTMLInputElement>(/дата и время начала/i);
      expect(startInput.value).toBe('');

      const endInput = screen.getByLabelText<HTMLInputElement>(/дата и время окончания/i);
      expect(endInput.value).toBe('');
    });

    it('renders read-only seats informational block', () => {
      renderEditForm({ event: baseEvent, isSubmitting: false });

      expect(screen.getByText(/Количество мест/i)).toBeInTheDocument();
      expect(screen.getByText('100')).toBeInTheDocument();
      expect(screen.getByText('45')).toBeInTheDocument();
      expect(
        screen.getByText(/задается при создании события и не может быть изменено/i),
      ).toBeInTheDocument();
    });

    it('renders cancel link pointing to event details page', () => {
      renderEditForm({ event: baseEvent, isSubmitting: false });

      const cancelLink = screen.getByRole('link', { name: 'Отмена' });
      expect(cancelLink).toHaveAttribute('href', `/events/${baseEvent.id}`);
    });
  });

  describe('Error Display', () => {
    it('renders global error message and field-specific errors', () => {
      renderEditForm({
        event: baseEvent,
        isSubmitting: false,
        error: {
          message: 'Исправьте некорректные поля',
          fieldErrors: {
            title: ['Название слишком короткое'],
            startAt: ['Некорректная дата начала'],
            endAt: ['Дата окончания должна быть позже даты начала'],
            description: ['Описание не должно превышать 1000 символов'],
          },
        },
      });

      expect(screen.getByText('Исправьте некорректные поля')).toBeInTheDocument();
      expect(screen.getByText('Название слишком короткое')).toBeInTheDocument();
      expect(screen.getByText('Некорректная дата начала')).toBeInTheDocument();
      expect(screen.getByText('Дата окончания должна быть позже даты начала')).toBeInTheDocument();
      expect(screen.getByText('Описание не должно превышать 1000 символов')).toBeInTheDocument();
    });
  });

  describe('Delete Confirmation and Submission', () => {
    it('triggers window.confirm when delete button is clicked and cancels on negative response', async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

      renderEditForm({ event: baseEvent, isSubmitting: false });

      const deleteBtn = screen.getByRole('button', { name: /удалить событие/i });
      expect(deleteBtn).toHaveAttribute('name', 'intent');
      expect(deleteBtn).toHaveAttribute('value', 'delete');

      await user.click(deleteBtn);

      expect(confirmSpy).toHaveBeenCalledWith(
        'Вы действительно хотите удалить это событие? Это действие необратимо.',
      );
    });

    it('proceeds with deletion when user accepts window.confirm', async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

      renderEditForm({ event: baseEvent, isSubmitting: false });

      const deleteBtn = screen.getByRole('button', { name: /удалить событие/i });
      await user.click(deleteBtn);

      expect(confirmSpy).toHaveBeenCalled();
    });
  });

  describe('Submitting State', () => {
    it('disables buttons and shows spinner with "Сохранение..." during submission', () => {
      renderEditForm({ event: baseEvent, isSubmitting: true });

      const submitBtn = screen.getByRole('button', { name: /сохранение\.\.\./i });
      expect(submitBtn).toBeDisabled();

      const deleteBtn = screen.getByRole('button', { name: /удалить событие/i });
      expect(deleteBtn).toBeDisabled();
    });
  });

  describe('Accessibility', () => {
    it('satisfies WCAG accessibility standards', async () => {
      const { container } = renderEditForm({ event: baseEvent, isSubmitting: false });
      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
