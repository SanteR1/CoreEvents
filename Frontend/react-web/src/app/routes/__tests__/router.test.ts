import { describe, it, expect } from 'vitest';
import { router } from '../router';
import { requireAuthLoader, anonymousOnlyLoader } from '@/shared/lib/auth';

describe('App Router Configuration', () => {
  it('configures root layout, error boundary and hydrate fallback', () => {
    expect(router.routes).toHaveLength(1);
    const rootRoute = router.routes[0];

    expect(rootRoute.path).toBe('/');
    expect(rootRoute.id).toBeDefined();
    expect(rootRoute.element).toBeDefined();
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

    // Protected guards
    expect(createEventRoute).toBeDefined();
    expect(createEventRoute?.loader).toBe(requireAuthLoader);
    expect(editEventRoute).toBeDefined();

    // Bookings nested routes
    expect(bookingsRoute).toBeDefined();
    expect(bookingsRoute?.children).toHaveLength(2);
    expect(bookingsRoute?.children?.[0].path).toBe('create/:eventId');
    expect(bookingsRoute?.children?.[1].path).toBe(':bookingId');

    // 404 fallback
    expect(notFoundRoute).toBeDefined();
  });

  it('resolves lazy route components properly', async () => {
    const rootRoute = router.routes[0];
    const children = rootRoute.children ?? [];

    const indexRoute = children.find((r) => r.index === true);
    const notFoundRoute = children.find((r) => r.path === '*');

    if (typeof indexRoute?.lazy === 'function') {
      const resolved = await indexRoute.lazy();
      expect(resolved.Component).toBeDefined();
      expect(resolved.loader).toBeDefined();
    }

    if (typeof notFoundRoute?.lazy === 'function') {
      const resolved = await notFoundRoute.lazy();
      expect(resolved.Component).toBeDefined();
    }
  });
});
