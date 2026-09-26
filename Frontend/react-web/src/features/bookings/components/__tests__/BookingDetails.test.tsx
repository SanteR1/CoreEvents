import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { BookingDetails } from '../BookingDetails';
import { checkA11y } from '@/shared/lib/test/axe';
import { setUser, clearUser } from '@/shared/lib/auth';
import type { BookingResponse } from '@/features/bookings/api/bookingsApi';

function renderBookingDetails(
  props: React.ComponentProps<typeof BookingDetails>,
  action: () => unknown = () => null,
) {
  const router = createMemoryRouter(
    [
      {
        path: '/bookings/:bookingId',
        element: <BookingDetails {...props} />,
        action,
      },
      {
        path: '/',
        element: <div>Каталог событий</div>,
      },
      {
        path: '/events/:eventId',
        element: <div>Детали события</div>,
      },
    ],
    { initialEntries: [`/bookings/${props.booking.id}`] },
  );

  return render(<RouterProvider router={router} />);
}

describe('BookingDetails.tsx', () => {
  const baseBooking: BookingResponse = {
    id: 'booking-123',
    eventId: 'event-456',
    userId: 'user_123',
    status: 'Confirmed',
    createdAt: new Date('2026-06-15T10:30:00Z'),
    processedAt: new Date('2026-06-15T10:35:00Z'),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    setUser({ id: 'user_123', userName: 'testuser', role: 'User' });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    clearUser();
  });

  describe('Status Badges', () => {
    it('renders "Подтверждено" badge for Confirmed status', () => {
      renderBookingDetails({ booking: { ...baseBooking, status: 'Confirmed' } });
      const badges = screen.getAllByText('Подтверждено');
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('renders "В обработке (Pending)" badge for Pending status', () => {
      renderBookingDetails({ booking: { ...baseBooking, status: 'Pending' } });
      const badges = screen.getAllByText('В обработке (Pending)');
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('renders "Отменено" badge for Cancelled status', () => {
      renderBookingDetails({ booking: { ...baseBooking, status: 'Cancelled' } });
      const badges = screen.getAllByText('Отменено');
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('renders "Отклонено" badge for Rejected status', () => {
      renderBookingDetails({ booking: { ...baseBooking, status: 'Rejected' } });
      const badges = screen.getAllByText('Отклонено');
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('renders fallback badge for unknown custom status', () => {
      // @ts-expect-error testing unusual status string
      renderBookingDetails({ booking: { ...baseBooking, status: 'CustomWaitlist' } });
      const badges = screen.getAllByText('CustomWaitlist');
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });

    it('renders "Отмена в процессе..." badge when isCancellingInProgress is true', () => {
      renderBookingDetails({
        booking: { ...baseBooking, status: 'Confirmed' },
        isCancellingInProgress: true,
      });
      const badges = screen.getAllByText(/отмена в процессе/i);
      expect(badges.length).toBeGreaterThanOrEqual(1);
    });
  });

  describe('Date formatting', () => {
    it('formats createdAt and processedAt dates when present', () => {
      renderBookingDetails({ booking: baseBooking });
      const createdFormatted = new Date('2026-06-15T10:30:00Z').toLocaleString('ru-RU');
      const processedFormatted = new Date('2026-06-15T10:35:00Z').toLocaleString('ru-RU');

      expect(screen.getByText(createdFormatted)).toBeInTheDocument();
      expect(screen.getByText(processedFormatted)).toBeInTheDocument();
    });

    it('displays fallback when createdAt or processedAt are null or undefined', () => {
      renderBookingDetails({
        // @ts-expect-error testing null createdAt
        booking: { ...baseBooking, createdAt: null, processedAt: null },
      });

      expect(screen.getByText('—')).toBeInTheDocument();
      expect(screen.getByText('Ожидает обработки')).toBeInTheDocument();
    });
  });

  describe('Cancellation Logic & Confirmation Dialog', () => {
    it('displays cancel button for Pending and Confirmed bookings', () => {
      renderBookingDetails({ booking: { ...baseBooking, status: 'Confirmed' } });
      expect(screen.getByRole('button', { name: /отменить бронирование/i })).toBeInTheDocument();
    });

    it('hides cancel button for Cancelled and Rejected bookings', () => {
      const { unmount } = renderBookingDetails({
        booking: { ...baseBooking, status: 'Cancelled' },
      });
      expect(
        screen.queryByRole('button', { name: /отменить бронирование/i }),
      ).not.toBeInTheDocument();
      unmount();

      renderBookingDetails({ booking: { ...baseBooking, status: 'Rejected' } });
      expect(
        screen.queryByRole('button', { name: /отменить бронирование/i }),
      ).not.toBeInTheDocument();
    });

    it('hides cancel button and displays server processing banner when isCancellingInProgress is true', () => {
      renderBookingDetails({
        booking: { ...baseBooking, status: 'Confirmed' },
        isCancellingInProgress: true,
      });

      expect(
        screen.queryByRole('button', { name: /отменить бронирование/i }),
      ).not.toBeInTheDocument();
      expect(screen.getByText(/отмена обрабатывается сервером/i)).toBeInTheDocument();
    });

    it('prompts window.confirm and prevents submission if user rejects confirmation', async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValueOnce(false);

      renderBookingDetails({ booking: { ...baseBooking, status: 'Pending' } });

      const cancelBtn = screen.getByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(confirmSpy).toHaveBeenCalledWith('Вы действительно хотите отменить это бронирование?');
    });

    it('submits form when user confirms cancellation', async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValueOnce(true);

      renderBookingDetails({ booking: { ...baseBooking, status: 'Pending' } });

      const cancelBtn = screen.getByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(confirmSpy).toHaveBeenCalledWith('Вы действительно хотите отменить это бронирование?');
    });

    it('displays cancel button when user is admin even if not booking owner', () => {
      setUser({ id: 'admin_user', userName: 'admin', role: 'Admin' });
      renderBookingDetails({
        booking: { ...baseBooking, userId: 'other_user', status: 'Pending' },
      });
      expect(screen.getByRole('button', { name: /отменить бронирование/i })).toBeInTheDocument();
    });

    it('hides cancel button when user is neither booking owner nor admin', () => {
      setUser({ id: 'stranger_user', userName: 'stranger', role: 'User' });
      renderBookingDetails({
        booking: { ...baseBooking, userId: 'other_user', status: 'Pending' },
      });
      expect(
        screen.queryByRole('button', { name: /отменить бронирование/i }),
      ).not.toBeInTheDocument();
    });

    it('hides cancel button when user is unauthenticated', () => {
      clearUser();
      renderBookingDetails({
        booking: { ...baseBooking, status: 'Pending' },
      });
      expect(
        screen.queryByRole('button', { name: /отменить бронирование/i }),
      ).not.toBeInTheDocument();
    });

    it('displays cancel button for legacy booking without userId when authenticated', () => {
      setUser({ id: 'user_123', userName: 'testuser', role: 'User' });
      renderBookingDetails({
        booking: { ...baseBooking, userId: undefined, status: 'Pending' },
      });
      expect(screen.getByRole('button', { name: /отменить бронирование/i })).toBeInTheDocument();
    });

    it('shows submitting indicator when cancellation is in flight', async () => {
      const user = userEvent.setup();
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      let resolveAction!: (v: unknown) => void;
      const actionPromise = new Promise((resolve) => {
        resolveAction = resolve;
      });

      renderBookingDetails({ booking: { ...baseBooking, status: 'Pending' } }, () => actionPromise);

      const cancelBtn = screen.getByRole('button', { name: /отменить бронирование/i });
      await user.click(cancelBtn);

      expect(screen.getByRole('button', { name: /отмена бронирования/i })).toBeDisabled();

      resolveAction(null);
    });
  });

  describe('Navigation Links', () => {
    it('renders valid navigation links to event and catalog', () => {
      renderBookingDetails({ booking: baseBooking });

      const eventLink = screen.getByRole('link', { name: /открыть страницу события/i });
      expect(eventLink).toHaveAttribute('href', `/events/${baseBooking.eventId}`);

      const catalogLink = screen.getByRole('link', { name: /к афише событий/i });
      expect(catalogLink).toHaveAttribute('href', '/');
    });
  });

  describe('Accessibility (A11y)', () => {
    it('satisfies a11y across various booking statuses', async () => {
      const { container: confirmedContainer, unmount } = renderBookingDetails({
        booking: { ...baseBooking, status: 'Confirmed' },
      });
      await expect(checkA11y(confirmedContainer)).resolves.toEqual([]);
      unmount();

      const { container: inProgressContainer } = renderBookingDetails({
        booking: { ...baseBooking, status: 'Pending' },
        isCancellingInProgress: true,
      });
      await expect(checkA11y(inProgressContainer)).resolves.toEqual([]);
    });
  });
});
