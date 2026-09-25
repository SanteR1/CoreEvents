import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  requireAuthLoader,
  requireAdminLoader,
  anonymousOnlyLoader,
  rootLoader,
  ensureSession,
  resetSessionPromise,
} from '../authGuards';
import { setUser, clearUser } from '../sessionStore';
import * as authApi from '@/features/auth/api/authApi';
import type { User } from '../user';

const mockUser: User = {
  id: 'user_123',
  userName: 'regular_user',
  role: 'User',
};

const mockAdmin: User = {
  id: 'admin_789',
  userName: 'super_admin',
  role: 'Admin',
};

describe('authGuards.ts', () => {
  beforeEach(() => {
    localStorage.clear();
    clearUser();
    resetSessionPromise();
    vi.restoreAllMocks();
  });

  describe('ensureSession', () => {
    it('returns in-memory user immediately without network request', async () => {
      setUser(mockUser);
      const getSpy = vi.spyOn(authApi, 'getCurrentUser');

      const user = await ensureSession();

      expect(user).toEqual(mockUser);
      expect(getSpy).not.toHaveBeenCalled();
    });

    it('fetches user from API when store is empty and updates store', async () => {
      const getSpy = vi.spyOn(authApi, 'getCurrentUser').mockResolvedValueOnce(mockAdmin);

      const user = await ensureSession();

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(user).toEqual(mockAdmin);
    });

    it('deduplicates simultaneous concurrent calls into a single network call', async () => {
      let resolveCall!: (u: User | null) => void;
      const pendingPromise = new Promise<User | null>((resolve) => {
        resolveCall = resolve;
      });
      const getSpy = vi.spyOn(authApi, 'getCurrentUser').mockReturnValueOnce(pendingPromise);

      const promise1 = ensureSession();
      const promise2 = ensureSession();
      resolveCall(mockUser);

      const [res1, res2] = await Promise.all([promise1, promise2]);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(res1).toEqual(mockUser);
      expect(res2).toEqual(mockUser);
    });

    it('returns null and saves null when user is not authenticated', async () => {
      vi.spyOn(authApi, 'getCurrentUser').mockResolvedValueOnce(null);

      const user = await ensureSession();

      expect(user).toBeNull();
    });
  });

  describe('rootLoader', () => {
    it('returns current user session object', async () => {
      setUser(mockUser);

      const result = await rootLoader();

      expect(result).toEqual({ user: mockUser });
    });
  });

  describe('requireAuthLoader', () => {
    it('returns 302 redirect Response to /login with encoded returnUrl when not authenticated', async () => {
      const request = new Request('http://localhost:5173/events/create?category=music');

      const result = await requireAuthLoader({ request });

      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe(
        '/login?returnUrl=%2Fevents%2Fcreate%3Fcategory%3Dmusic',
      );
    });

    it('returns null and allows navigation when authenticated user is present', async () => {
      setUser(mockUser);
      const request = new Request('http://localhost:5173/events/create');

      const result = await requireAuthLoader({ request });

      expect(result).toBeNull();
    });
  });

  describe('requireAdminLoader', () => {
    it('returns 302 redirect to /login when user is unauthenticated', async () => {
      const request = new Request('http://localhost:5173/events/create');

      const result = await requireAdminLoader({ request });

      expect(result).toBeInstanceOf(Response);
      expect(result!.status).toBe(302);
      expect(result!.headers.get('Location')).toBe('/login?returnUrl=%2Fevents%2Fcreate');
    });

    it('throws 403 Forbidden Response when user is authenticated but not an Admin', async () => {
      setUser(mockUser);
      const request = new Request('http://localhost:5173/events/create');

      await expect(requireAdminLoader({ request })).rejects.toThrow();

      try {
        await requireAdminLoader({ request });
      } catch (err) {
        expect(err).toBeInstanceOf(Response);
        const errorResponse = err as Response;
        expect(errorResponse.status).toBe(403);
        expect(errorResponse.statusText).toBe('Forbidden');
      }
    });

    it('returns null and allows navigation when user has Admin role', async () => {
      setUser(mockAdmin);
      const request = new Request('http://localhost:5173/events/create');

      const result = await requireAdminLoader({ request });

      expect(result).toBeNull();
    });
  });

  describe('anonymousOnlyLoader', () => {
    it('returns null and allows access to login/register for anonymous guests', async () => {
      const request = new Request('http://localhost:5173/login');
      const result = await anonymousOnlyLoader({ request });

      expect(result).toBeNull();
    });

    it('redirects authenticated user to "/" by default if no returnUrl was passed', async () => {
      setUser(mockUser);
      const request = new Request('http://localhost:5173/login');
      const result = await anonymousOnlyLoader({ request });

      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
    });

    it('redirects authenticated user to specified returnUrl query parameter', async () => {
      setUser(mockUser);
      const request = new Request('http://localhost:5173/login?returnUrl=/events/123');
      const result = await anonymousOnlyLoader({ request });

      expect(result).toBeInstanceOf(Response);
      const response = result!;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/events/123');
    });
  });
});
