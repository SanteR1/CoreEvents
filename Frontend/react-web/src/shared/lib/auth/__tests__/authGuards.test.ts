import { describe, it, expect, beforeEach } from 'vitest';
import { requireAuthLoader, anonymousOnlyLoader } from '../authGuards';
import { setToken, clearToken } from '../sessionStore';

function createMockToken(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600 }));
  return `${header}.${payload}.sig`;
}

describe('authGuards.ts', () => {
  beforeEach(() => {
    localStorage.clear();
    clearToken();
  });

  describe('requireAuthLoader', () => {
    it('returns 302 redirect Response to /login with encoded returnUrl when not authenticated', () => {
      const request = new Request('http://localhost:5173/events/create?category=music');

      const result = requireAuthLoader({ request });

      // Catch-редирект паттерн: проверка нативного объекта Response
      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe(
        '/login?returnUrl=%2Fevents%2Fcreate%3Fcategory%3Dmusic',
      );
    });

    it('returns null and allows navigation when valid token is present', () => {
      setToken(createMockToken());
      const request = new Request('http://localhost:5173/events/create');

      const result = requireAuthLoader({ request });

      expect(result).toBeNull();
    });
  });

  describe('anonymousOnlyLoader', () => {
    it('returns null and allows access to login/register for anonymous guests', () => {
      const request = new Request('http://localhost:5173/login');
      // @ts-expect-error Mocking LoaderFunctionArgs minimal shape
      const result = anonymousOnlyLoader({ request });

      expect(result).toBeNull();
    });

    it('redirects authenticated user to "/" by default if no returnUrl was passed', () => {
      setToken(createMockToken());
      const request = new Request('http://localhost:5173/login');
      // @ts-expect-error Mocking LoaderFunctionArgs minimal shape
      const result = anonymousOnlyLoader({ request });

      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
    });

    it('redirects authenticated user to specified returnUrl query parameter', () => {
      setToken(createMockToken());
      const request = new Request('http://localhost:5173/login?returnUrl=/events/123');
      // @ts-expect-error Mocking LoaderFunctionArgs minimal shape
      const result = anonymousOnlyLoader({ request });

      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/events/123');
    });
  });
});
