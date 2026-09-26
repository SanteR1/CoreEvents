import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { eventsClient } from '../eventsClient';
import { bookingsClient } from '../bookingsClient';
import { usersClient } from '../usersClient';
import {
  authRefreshMiddleware,
  refreshAuthSession,
  getRefreshPromise,
  resetRefreshPromise,
} from '../authRefreshMiddleware';
import * as sessionStore from '@/shared/lib/auth/sessionStore';

describe('OpenAPI Clients & authRefreshMiddleware', () => {
  const mockFetch = vi.fn();

  beforeEach(() => {
    localStorage.clear();
    sessionStore.clearUser();
    resetRefreshPromise();
    mockFetch.mockReset();
    mockFetch.mockImplementation(() =>
      Promise.resolve(
        new Response(JSON.stringify({}), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    );
  });

  afterEach(() => {
    localStorage.clear();
    sessionStore.clearUser();
    resetRefreshPromise();
    vi.restoreAllMocks();
  });

  describe('Client requests configuration', () => {
    it('eventsClient makes requests with /v1/events path', async () => {
      await eventsClient.GET('/v1/events', {
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.url).toContain('/v1/events');
    });

    it('bookingsClient makes requests with /v1/bookings/{id} path', async () => {
      await bookingsClient.GET('/v1/bookings/{id}', {
        params: { path: { id: 'booking_1' } },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.url).toContain('/v1/bookings/booking_1');
    });

    it('usersClient makes requests with /v1/auth/login path', async () => {
      await usersClient.POST('/v1/auth/login', {
        body: { userName: 'testuser', password: 'password123' },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.url).toContain('/v1/auth/login');
    });

    it('attaches Authorization header when legacy token is set', async () => {
      sessionStore.setToken('test-bearer-token');
      await eventsClient.GET('/v1/events', { fetch: mockFetch });
      await bookingsClient.GET('/v1/bookings/{id}', {
        params: { path: { id: 'booking_1' } },
        fetch: mockFetch,
      });
      await usersClient.POST('/v1/auth/login', {
        body: { userName: 'testuser', password: 'password123' },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(3);
      for (const call of mockFetch.mock.calls) {
        const req = call[0] as Request;
        expect(req.headers.get('Authorization')).toBe('Bearer test-bearer-token');
      }
    });

    it('falls back to default URL when env variables are missing', async () => {
      vi.resetModules();
      vi.stubEnv('VITE_API_URL', undefined);
      vi.stubEnv('VITE_API_GATEWAY_URL', undefined);
      vi.stubEnv('VITE_USERS_API_URL', undefined);
      vi.stubEnv('VITE_EVENTS_API_URL', undefined);
      vi.stubEnv('VITE_BOOKINGS_API_URL', undefined);

      const { usersClient: isolatedUsers } = await import('../usersClient');
      const { eventsClient: isolatedEvents } = await import('../eventsClient');
      const { bookingsClient: isolatedBookings } = await import('../bookingsClient');

      expect(isolatedUsers).toBeDefined();
      expect(isolatedEvents).toBeDefined();
      expect(isolatedBookings).toBeDefined();
      vi.unstubAllEnvs();
      vi.resetModules();
    });

    it('uses VITE_API_URL as the gateway base URL', async () => {
      vi.resetModules();
      vi.stubEnv('VITE_API_URL', 'http://custom-gateway:5000');
      vi.stubEnv('VITE_API_GATEWAY_URL', undefined);
      vi.stubEnv('VITE_USERS_API_URL', undefined);
      vi.stubEnv('VITE_EVENTS_API_URL', undefined);
      vi.stubEnv('VITE_BOOKINGS_API_URL', undefined);

      const { eventsClient: isolatedEvents } = await import('../eventsClient');

      await isolatedEvents.GET('/v1/events', { fetch: mockFetch });
      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.url).toBe('http://custom-gateway:5000/v1/events');

      vi.unstubAllEnvs();
      vi.resetModules();
    });

    it('uses service-specific env variables when provided', async () => {
      vi.resetModules();
      vi.stubEnv('VITE_API_URL', undefined);
      vi.stubEnv('VITE_API_GATEWAY_URL', undefined);
      vi.stubEnv('VITE_EVENTS_API_URL', 'http://events-service:5004');
      vi.stubEnv('VITE_BOOKINGS_API_URL', 'http://bookings-service:5005');
      vi.stubEnv('VITE_USERS_API_URL', 'http://users-service:5003');

      const { eventsClient: isolatedEvents } = await import('../eventsClient');
      const { bookingsClient: isolatedBookings } = await import('../bookingsClient');
      const { usersClient: isolatedUsers } = await import('../usersClient');

      await isolatedEvents.GET('/v1/events', { fetch: mockFetch });
      expect((mockFetch.mock.calls[0][0] as Request).url).toBe(
        'http://events-service:5004/v1/events',
      );

      await isolatedBookings.GET('/v1/bookings/{id}', {
        params: { path: { id: '1' } },
        fetch: mockFetch,
      });
      expect((mockFetch.mock.calls[1][0] as Request).url).toBe(
        'http://bookings-service:5005/v1/bookings/1',
      );

      await isolatedUsers.POST('/v1/auth/login', {
        body: { userName: 'u', password: 'p' },
        fetch: mockFetch,
      });
      expect((mockFetch.mock.calls[2][0] as Request).url).toBe(
        'http://users-service:5003/v1/auth/login',
      );

      vi.unstubAllEnvs();
      vi.resetModules();
    });
  });

  describe('authRefreshMiddleware', () => {
    it('passes through when status is not 401', async () => {
      const request = new Request('http://localhost:5000/v1/events');
      const response = new Response(JSON.stringify([]), { status: 200 });

      const result = await authRefreshMiddleware.onResponse({ request, response });

      expect(result).toBe(response);
    });

    it('does not refresh when 401 happens on an auth endpoint', async () => {
      const loginRequest = new Request('http://localhost:5000/v1/auth/login', { method: 'POST' });
      const loginResponse = new Response(null, { status: 401 });

      const result = await authRefreshMiddleware.onResponse({
        request: loginRequest,
        response: loginResponse,
      });

      expect(result).toBe(loginResponse);
    });

    it('does not refresh when 401 happens on refresh endpoint', async () => {
      const refreshReq = new Request('http://localhost:5000/v1/auth/refresh', { method: 'POST' });
      const refreshRes = new Response(null, { status: 401 });

      const result = await authRefreshMiddleware.onResponse({
        request: refreshReq,
        response: refreshRes,
      });

      expect(result).toBe(refreshRes);
    });

    it('does not refresh when 401 happens on register endpoint', async () => {
      const regReq = new Request('http://localhost:5000/v1/auth/register', { method: 'POST' });
      const regRes = new Response(null, { status: 401 });

      const result = await authRefreshMiddleware.onResponse({
        request: regReq,
        response: regRes,
      });

      expect(result).toBe(regRes);
    });

    it('intercepts 401, refreshes session successfully and retries request', async () => {
      const originalRequest = new Request('http://localhost:5000/v1/events', {
        method: 'GET',
        headers: { 'X-Custom': 'test' },
      });
      const response401 = new Response(null, { status: 401 });

      const replayedResponse = new Response(JSON.stringify([{ id: 'ev1' }]), { status: 200 });
      const globalFetchSpy = vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
        const url = typeof input === 'string' ? input : (input as Request).url;
        if (url.includes('/v1/auth/refresh')) {
          return Promise.resolve(new Response(null, { status: 200 }));
        }
        return Promise.resolve(replayedResponse);
      });

      const result = await authRefreshMiddleware.onResponse({
        request: originalRequest,
        response: response401,
      });

      expect(result).toBe(replayedResponse);
      expect(globalFetchSpy).toHaveBeenCalledTimes(2);
    });

    it('intercepts 401, clears user when refresh fails and returns original response', async () => {
      const clearUserSpy = vi.spyOn(sessionStore, 'clearUser');
      const originalRequest = new Request('http://localhost:5000/v1/events');
      const response401 = new Response(null, { status: 401 });

      vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(new Response(null, { status: 401 }));

      const result = await authRefreshMiddleware.onResponse({
        request: originalRequest,
        response: response401,
      });

      expect(clearUserSpy).toHaveBeenCalled();
      expect(result).toBe(response401);
    });

    it('deduplicates concurrent refresh calls via refreshPromise', async () => {
      let resolveRefresh: ((res: Response) => void) | undefined;
      const refreshFetchPromise = new Promise<Response>((resolve) => {
        resolveRefresh = resolve;
      });

      vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
        const url = typeof input === 'string' ? input : (input as Request).url;
        if (url.includes('/v1/auth/refresh')) {
          return refreshFetchPromise;
        }
        return new Response(null, { status: 200 });
      });

      const req1 = new Request('http://localhost:5000/v1/events');
      const req2 = new Request('http://localhost:5000/v1/bookings/b1');
      const res401 = new Response(null, { status: 401 });

      const promise1 = authRefreshMiddleware.onResponse({ request: req1, response: res401 });
      const promise2 = authRefreshMiddleware.onResponse({ request: req2, response: res401 });

      expect(getRefreshPromise()).not.toBeNull();

      resolveRefresh!(new Response(null, { status: 200 }));

      const [res1, res2] = await Promise.all([promise1, promise2]);
      expect(res1?.status).toBe(200);
      expect(res2?.status).toBe(200);
    });

    it('handles network error in refreshAuthSession gracefully', async () => {
      vi.spyOn(globalThis, 'fetch').mockRejectedValueOnce(new Error('Network error'));

      const success = await refreshAuthSession();
      expect(success).toBe(false);
    });

    it('uses fallback URL in refreshAuthSession when env variables are not defined', async () => {
      vi.stubEnv('VITE_API_URL', undefined);
      vi.stubEnv('VITE_API_GATEWAY_URL', undefined);
      vi.stubEnv('VITE_USERS_API_URL', undefined);

      const fetchSpy = vi
        .spyOn(globalThis, 'fetch')
        .mockResolvedValueOnce(new Response(null, { status: 200 }));

      await refreshAuthSession();

      expect(fetchSpy).toHaveBeenCalledWith('http://localhost:5000/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      });

      vi.unstubAllEnvs();
    });
  });
});
