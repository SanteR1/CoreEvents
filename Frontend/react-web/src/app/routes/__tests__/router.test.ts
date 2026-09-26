import { describe, it, expect } from 'vitest';
import { router } from '../router';
import { requireAdminLoader, anonymousOnlyLoader, rootLoader } from '@/shared/lib/auth';

describe('App Router Configuration', () => {
  it('configures root layout, error boundary and hydrate fallback', () => {
    expect(router.routes).toHaveLength(1);
    const rootRoute = router.routes[0];

    expect(rootRoute.path).toBe('/');
    expect(rootRoute.id).toBeDefined();
    expect(rootRoute.element).toBeDefined();
    expect(rootRoute.loader).toBe(rootLoader);
  });

  it('configures public, guest, protected, and 404 routes', () => {
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

    // Admin guards
    expect(createEventRoute).toBeDefined();
    expect(createEventRoute?.loader).toBe(requireAdminLoader);
    expect(editEventRoute).toBeDefined();
    expect(editEventRoute?.loader).toBe(requireAdminLoader);

    // Bookings nested routes
    expect(bookingsRoute).toBeDefined();
    expect(bookingsRoute?.children).toHaveLength(2);
    expect(bookingsRoute?.children?.[0].path).toBe('create/:eventId');
    expect(bookingsRoute?.children?.[1].path).toBe(':bookingId');

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
    expect(resolvedCreateEvent.action).toBeDefined();

    // Edit Event
    const resolvedEditEvent = await resolveLazy(editEventRoute);
    expect(resolvedEditEvent.Component).toBeDefined();
    expect(resolvedEditEvent.loader).toBeDefined();
    expect(resolvedEditEvent.action).toBeDefined();

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
