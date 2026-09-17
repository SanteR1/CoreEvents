import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { BookingCreateForm } from '../BookingCreateForm';
import { checkA11y } from '@/shared/lib/test/axe';

function renderForm(props: React.ComponentProps<typeof BookingCreateForm>) {
  const router = createMemoryRouter([
    {
      path: '/',
      element: <BookingCreateForm {...props} />,
    },
  ]);
  return render(<RouterProvider router={router} />);
}

describe('BookingCreateForm', () => {
  describe('availableSeats = 0 (Sold out state)', () => {
    it('renders "Все места распроданы" banner, link to events, and satisfies a11y', async () => {
      const { container } = renderForm({
        eventId: 'event-1',
        availableSeats: 0,
        isSubmitting: false,
      });

      expect(screen.getByRole('heading', { name: /все места распроданы/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /посмотреть другие события/i })).toBeInTheDocument();
      expect(screen.queryByRole('spinbutton')).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /забронировать/i })).not.toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('availableSeats = 1 (Single seat boundary)', () => {
    it('disables both decrement and increment buttons at availableSeats = 1', async () => {
      const user = userEvent.setup();
      const { container } = renderForm({
        eventId: 'event-1',
        availableSeats: 1,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });
      const decrementBtn = screen.getByRole('button', { name: /уменьшить количество мест/i });
      const incrementBtn = screen.getByRole('button', { name: /увеличить количество мест/i });
      const submitBtn = screen.getByRole('button', { name: /забронировать \(1\)/i });

      expect(input).toHaveValue(1);
      expect(decrementBtn).toBeDisabled();
      expect(incrementBtn).toBeDisabled();
      expect(submitBtn).toBeEnabled();

      // Attempts to click disabled buttons do not change value
      await user.click(incrementBtn);
      expect(input).toHaveValue(1);

      await user.click(decrementBtn);
      expect(input).toHaveValue(1);

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('availableSeats = N (Multi-seat boundaries and stepping)', () => {
    it('increments up to upper boundary N and decrements down to lower boundary 1', async () => {
      const user = userEvent.setup();
      const { container } = renderForm({
        eventId: 'event-1',
        availableSeats: 3,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });
      const decrementBtn = screen.getByRole('button', { name: /уменьшить количество мест/i });
      const incrementBtn = screen.getByRole('button', { name: /увеличить количество мест/i });

      // Initial state (1 / 3)
      expect(input).toHaveValue(1);
      expect(decrementBtn).toBeDisabled();
      expect(incrementBtn).toBeEnabled();
      expect(screen.getByRole('button', { name: /забронировать \(1\)/i })).toBeInTheDocument();

      // Step 1: increment to 2
      await user.click(incrementBtn);
      expect(input).toHaveValue(2);
      expect(decrementBtn).toBeEnabled();
      expect(incrementBtn).toBeEnabled();
      expect(screen.getByRole('button', { name: /забронировать \(2\)/i })).toBeInTheDocument();

      // Step 2: increment to 3 (upper boundary)
      await user.click(incrementBtn);
      expect(input).toHaveValue(3);
      expect(decrementBtn).toBeEnabled();
      expect(incrementBtn).toBeDisabled();
      expect(screen.getByRole('button', { name: /забронировать \(3\)/i })).toBeInTheDocument();

      // Extra increment attempt at upper boundary has no effect
      await user.click(incrementBtn);
      expect(input).toHaveValue(3);

      // Step 3: decrement back to 2
      await user.click(decrementBtn);
      expect(input).toHaveValue(2);
      expect(decrementBtn).toBeEnabled();
      expect(incrementBtn).toBeEnabled();

      // Step 4: decrement back to 1 (lower boundary)
      await user.click(decrementBtn);
      expect(input).toHaveValue(1);
      expect(decrementBtn).toBeDisabled();
      expect(incrementBtn).toBeEnabled();

      // Extra decrement attempt at lower boundary has no effect
      await user.click(decrementBtn);
      expect(input).toHaveValue(1);

      await expect(checkA11y(container)).resolves.toEqual([]);
    });

    it('clamps manually typed input to range [1, availableSeats]', async () => {
      const user = userEvent.setup();
      renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });

      // Typing numbers beyond availableSeats clamps to availableSeats (5)
      await user.clear(input);
      await user.type(input, '99');
      expect(input).toHaveValue(5);

      // Typing 0 clamps to 1
      await user.clear(input);
      await user.type(input, '0');
      expect(input).toHaveValue(1);
    });

    it('resets empty input value back to 1 on blur', async () => {
      const user = userEvent.setup();
      renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });

      await user.clear(input);
      expect(input).toHaveValue(null);

      // Moving focus away resets to 1
      await user.tab();
      expect(input).toHaveValue(1);

      // Blurring when input is not empty preserves value
      await user.click(input);
      await user.tab();
      expect(input).toHaveValue(1);
    });

    it('handles increment and decrement when input is currently empty string', async () => {
      const user = userEvent.setup();
      renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });
      const incrementBtn = screen.getByRole('button', { name: /увеличить количество мест/i });
      const decrementBtn = screen.getByRole('button', { name: /уменьшить количество мест/i });

      // Clear input without triggering blur
      await user.clear(input);
      expect(input).toHaveValue(null);

      // Increment from empty defaults to 1 + 1 = 2
      await user.click(incrementBtn);
      expect(input).toHaveValue(2);

      // Clear input again
      await user.clear(input);
      expect(input).toHaveValue(null);

      // Decrement button is disabled when currentSeats is 1
      expect(decrementBtn).toBeDisabled();
    });

    it('ignores input change when parsed value is NaN', () => {
      renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: false,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });
      input.setAttribute('type', 'text');
      fireEvent.change(input, { target: { value: 'invalid-number' } });
      input.setAttribute('type', 'number');

      // State is not updated with NaN, button still displays (1)
      expect(screen.getByRole('button', { name: /забронировать \(1\)/i })).toBeInTheDocument();
    });
  });

  describe('Submitting state', () => {
    it('disables input, stepper buttons, and submit button during submission', () => {
      renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: true,
      });

      const input = screen.getByRole('spinbutton', { name: /^количество мест$/i });
      const decrementBtn = screen.getByRole('button', { name: /уменьшить количество мест/i });
      const incrementBtn = screen.getByRole('button', { name: /увеличить количество мест/i });
      const submitBtn = screen.getByRole('button', { name: /отправка заявки/i });

      expect(input).toBeDisabled();
      expect(decrementBtn).toBeDisabled();
      expect(incrementBtn).toBeDisabled();
      expect(submitBtn).toBeDisabled();
    });
  });

  describe('Validation & Error state', () => {
    it('displays alert banner when error prop is passed and satisfies a11y', async () => {
      const { container } = renderForm({
        eventId: 'event-1',
        availableSeats: 5,
        isSubmitting: false,
        error: { message: 'Недостаточно свободных мест для оформления брони' },
      });

      const alert = screen.getByRole('alert');
      expect(alert).toBeInTheDocument();
      expect(alert).toHaveTextContent('Недостаточно свободных мест для оформления брони');

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
