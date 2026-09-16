import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { eventsClient } from '../eventsClient';
import { bookingsClient } from '../bookingsClient';
import { usersClient } from '../usersClient';
import { setToken, clearToken } from '@/shared/lib/auth';

function createMockJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({ sub: 'user_123', exp: Math.floor(Date.now() / 1000) + 3600 }),
  );
  return `${header}.${payload}.signature`;
}

describe('OpenAPI Clients onRequest interceptors', () => {
  const mockFetch = vi.fn();

  beforeEach(() => {
    localStorage.clear();
    clearToken();
    mockFetch.mockReset();
    mockFetch.mockResolvedValue(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );
  });

  afterEach(() => {
    localStorage.clear();
    clearToken();
    vi.restoreAllMocks();
  });

  describe('eventsClient', () => {
    it('attaches Authorization header when token is present', async () => {
      const jwt = createMockJwt();
      setToken(jwt);

      await eventsClient.GET('/Events', {
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBe(`Bearer ${jwt}`);
    });

    it('does not attach Authorization header when token is missing', async () => {
      clearToken();

      await eventsClient.GET('/Events', {
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBeNull();
    });
  });

  describe('bookingsClient', () => {
    it('attaches Authorization header when token is present', async () => {
      const jwt = createMockJwt();
      setToken(jwt);

      await bookingsClient.GET('/Bookings/{id}', {
        params: { path: { id: 'booking_1' } },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBe(`Bearer ${jwt}`);
    });

    it('does not attach Authorization header when token is missing', async () => {
      clearToken();

      await bookingsClient.GET('/Bookings/{id}', {
        params: { path: { id: 'booking_1' } },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBeNull();
    });
  });

  describe('usersClient', () => {
    it('attaches Authorization header when token is present', async () => {
      const jwt = createMockJwt();
      setToken(jwt);

      await usersClient.POST('/Auth/login', {
        body: { userName: 'testuser', password: 'password123' },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBe(`Bearer ${jwt}`);
    });

    it('does not attach Authorization header when token is missing', async () => {
      clearToken();

      await usersClient.POST('/Auth/login', {
        body: { userName: 'testuser', password: 'password123' },
        fetch: mockFetch,
      });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      const request = mockFetch.mock.calls[0][0] as Request;
      expect(request.headers.get('Authorization')).toBeNull();
    });
  });
});
