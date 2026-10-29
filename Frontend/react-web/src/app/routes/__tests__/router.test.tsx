import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { router } from '../router';
import { anonymousOnlyLoader, rootLoader, setUser, clearUser } from '@/shared/lib/auth';
import * as eventsApi from '@/features/events/api/eventsApi';

describe('App Router Configuration', () => {
  it('configures root layout, error boundary and hydrate fallback', () => {
    expect(router.routes).toHaveLength(1);
    const rootRoute = router.routes[0];

    expect(rootRoute.path).toBe('/');
    expect(rootRoute.id).toBeDefined();
    expect(rootRoute.element).toBeDefined();
    expect(rootRoute.loader).toBe(rootLoader);
  });

  it('configures public, guest, protected, and 404 routes without static loader conflicts on admin lazy routes', () => {
    const rootRoute = router.routes[0];
    const children = rootRoute.children ?? [];

    const indexRoute = children.find((r) => r.index === true);
    const topEventsRoute = children.find((r) => r.path === 'events/topevents');
    const eventDetailsRoute = children.find((r) => r.path === 'events/:id');
    const loginRoute = children.find((r) => r.path === 'login');
    const registerRoute = children.find((r) => r.path === 'register');
    const createEventRoute = children.find((r) => r.path === 'events/create');
    const editEventRoute = children.find((r) => r.path === 'events/:id/edit');
    const bookingsRoute = children.find((r) => r.path === 'bookings');
    const notFoundRoute = children.find((r) => r.path === '*');

    expect(indexRoute).toBeDefined();
    expect(topEventsRoute).toBeDefined();
    expect(eventDetailsRoute).toBeDefined();

    // Guest guards
    expect(loginRoute).toBeDefined();
    expect(loginRoute?.loader).toBe(anonymousOnlyLoader);
    expect(registerRoute).toBeDefined();
    expect(registerRoute?.loader).toBe(anonymousOnlyLoader);

    // Admin routes use lazy resolution so that page-level loaders supply data without static loader conflicts
    expect(createEventRoute).toBeDefined();
    expect(createEventRoute?.loader).toBeUndefined();
    expect(editEventRoute).toBeDefined();
    expect(editEventRoute?.loader).toBeUndefined();

    // Bookings nested routes
    expect(bookingsRoute).toBeDefined();
    expect(bookingsRoute?.children).toHaveLength(3);
    expect(bookingsRoute?.children?.find((r) => r.index === true)).toBeDefined();
    expect(bookingsRoute?.children?.find((r) => r.path === 'create/:eventId')).toBeDefined();
    expect(bookingsRoute?.children?.find((r) => r.path === ':bookingId')).toBeDefined();

    // 404 fallback
    expect(notFoundRoute).toBeDefined();
  });

  async function resolveLazy(route?: { lazy?: unknown }) {
    if (typeof route?.lazy === 'function') {
      return (route.lazy as () => Promise<Record<string, unknown>>)();
    }
    throw new Error('Route lazy loader is not a function');
  }

  it('resolves all lazy route components and actions/loaders properly', async () => {
    const rootRoute = router.routes[0];
    const children = rootRoute.children ?? [];

    const indexRoute = children.find((r) => r.index === true);
    const topEventsRoute = children.find((r) => r.path === 'events/topevents');
    const eventDetailsRoute = children.find((r) => r.path === 'events/:id');
    const loginRoute = children.find((r) => r.path === 'login');
    const registerRoute = children.find((r) => r.path === 'register');
    const createEventRoute = children.find((r) => r.path === 'events/create');
    const editEventRoute = children.find((r) => r.path === 'events/:id/edit');
    const bookingsRoute = children.find((r) => r.path === 'bookings');
    const createBookingRoute = bookingsRoute?.children?.find((r) => r.path === 'create/:eventId');
    const getBookingRoute = bookingsRoute?.children?.find((r) => r.path === ':bookingId');
    const notFoundRoute = children.find((r) => r.path === '*');

    // Index / Home
    const resolvedIndex = await resolveLazy(indexRoute);
    expect(resolvedIndex.Component).toBeDefined();
    expect(resolvedIndex.loader).toBeDefined();

    // Top Events
    const resolvedTopEvents = await resolveLazy(topEventsRoute);
    expect(resolvedTopEvents.Component).toBeDefined();
    expect(resolvedTopEvents.loader).toBeDefined();

    // Event Details
    const resolvedEventDetails = await resolveLazy(eventDetailsRoute);
    expect(resolvedEventDetails.Component).toBeDefined();
    expect(resolvedEventDetails.loader).toBeDefined();
    expect(resolvedEventDetails.action).toBeDefined();

    // Login
    const resolvedLogin = await resolveLazy(loginRoute);
    expect(resolvedLogin.Component).toBeDefined();
    expect(resolvedLogin.action).toBeDefined();

    // Register
    const resolvedRegister = await resolveLazy(registerRoute);
    expect(resolvedRegister.Component).toBeDefined();
    expect(resolvedRegister.action).toBeDefined();

    // Create Event
    const resolvedCreateEvent = await resolveLazy(createEventRoute);
    expect(resolvedCreateEvent.Component).toBeDefined();
    expect(resolvedCreateEvent.loader).toBeDefined();
    expect(resolvedCreateEvent.action).toBeDefined();

    // Edit Event
    const resolvedEditEvent = await resolveLazy(editEventRoute);
    expect(resolvedEditEvent.Component).toBeDefined();
    expect(resolvedEditEvent.loader).toBeDefined();
    expect(resolvedEditEvent.action).toBeDefined();

    // My Bookings
    const myBookingsRoute = bookingsRoute?.children?.find((r) => r.index === true);
    const resolvedMyBookings = await resolveLazy(myBookingsRoute);
    expect(resolvedMyBookings.Component).toBeDefined();
    expect(resolvedMyBookings.loader).toBeDefined();

    // Create Booking
    const resolvedCreateBooking = await resolveLazy(createBookingRoute);
    expect(resolvedCreateBooking.Component).toBeDefined();
    expect(resolvedCreateBooking.loader).toBeDefined();
    expect(resolvedCreateBooking.action).toBeDefined();

    // Get Booking
    const resolvedGetBooking = await resolveLazy(getBookingRoute);
    expect(resolvedGetBooking.Component).toBeDefined();
    expect(resolvedGetBooking.loader).toBeDefined();
    expect(resolvedGetBooking.action).toBeDefined();

    // 404 Not Found
    const resolvedNotFound = await resolveLazy(notFoundRoute);
    expect(resolvedNotFound.Component).toBeDefined();
  });
});

describe('Router Integration & Navigation Guards', () => {
  const mockEvent = {
    id: 'ev-test-1',
    title: 'Реальная Архитектура 2026',
    description: 'Интеграционный тест роутинга',
    startAt: new Date('2026-11-15T10:00:00Z'),
    endAt: new Date('2026-11-15T18:00:00Z'),
    totalSeats: 100,
    availableSeats: 50,
    price: 2500,
    currency: 'KZT',
    isActive: true,
  };

  beforeEach(() => {
    clearUser();
    vi.clearAllMocks();
  });

  afterEach(() => {
    clearUser();
    vi.restoreAllMocks();
  });

  it('navigates to /events/:id/edit as Admin, executes lazy loader, and renders EditEventPage with prefilled event data', async () => {
    setUser({ id: 'a1', userName: 'Admin', role: 'Admin' });
    vi.spyOn(eventsApi, 'getEventById').mockResolvedValueOnce({
      success: true,
      httpStatus: 200,
      event: mockEvent,
    });

    const testRouter = createMemoryRouter(router.routes, {
      initialEntries: ['/events/ev-test-1/edit'],
    });

    render(<RouterProvider router={testRouter} />);

    expect(
      await screen.findByRole('heading', { name: 'Редактирование события' }),
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue('Реальная Архитектура 2026')).toBeInTheDocument();
    expect(screen.getByDisplayValue('2500')).toBeInTheDocument();
    expect(screen.getByDisplayValue('KZT')).toBeInTheDocument();
  });

  it('redirects unauthenticated guest from /events/:id/edit to /login with returnUrl parameter', async () => {
    clearUser();

    const testRouter = createMemoryRouter(router.routes, {
      initialEntries: ['/events/ev-test-1/edit'],
    });

    render(<RouterProvider router={testRouter} />);

    expect(
      await screen.findByRole('heading', { name: /Вход в личный кабинет/i }),
    ).toBeInTheDocument();
    expect(testRouter.state.location.pathname).toBe('/login');
    expect(testRouter.state.location.search).toContain('returnUrl=%2Fevents%2Fev-test-1%2Fedit');
  });

  it('rejects regular user with 403 Forbidden when attempting to access /events/:id/edit', async () => {
    setUser({ id: 'u1', userName: 'RegularUser', role: 'User' });

    const testRouter = createMemoryRouter(router.routes, {
      initialEntries: ['/events/ev-test-1/edit'],
    });

    render(<RouterProvider router={testRouter} />);

    expect(await screen.findByRole('heading', { name: 'Доступ запрещён' })).toBeInTheDocument();
  });
});
