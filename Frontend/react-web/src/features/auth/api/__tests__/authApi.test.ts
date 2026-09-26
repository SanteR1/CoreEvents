import { describe, it, expect, vi, beforeEach } from 'vitest';
import { registerUser, loginUser, getCurrentUser, refreshSession, logoutUser } from '../authApi';
import { usersClient } from '@/shared/api';
import * as sessionStore from '@/shared/lib/auth/sessionStore';

describe('authApi Service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('registerUser', () => {
    it('returns response data and status on successful registration', async () => {
      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await registerUser({
        userName: 'newuser',
        password: 'Password123!',
      });

      expect(result).toEqual({
        data: undefined,
        error: undefined,
        status: 204,
      });
    });

    it('handles ProblemDetails response with numeric status', async () => {
      const problemDetails = {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { userName: ['User already exists'] },
      };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: problemDetails,
        error: undefined,
        response: new Response(JSON.stringify(problemDetails), { status: 400 }),
      });

      const result = await registerUser({
        userName: 'existinguser',
        password: 'Password123!',
      });

      expect(result).toEqual({
        data: undefined,
        error: problemDetails,
        status: 400,
      });
    });

    it('handles ProblemDetails response without numeric status and defaults to 400', async () => {
      const problemDetails = {
        title: 'Validation failed',
        detail: 'Invalid payload',
        errors: { payload: ['Invalid payload'] },
      };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: problemDetails,
        error: undefined,
        response: new Response(JSON.stringify(problemDetails), { status: 400 }),
      });

      const result = await registerUser({
        userName: 'user',
        password: '123',
      });

      expect(result).toEqual({
        data: undefined,
        error: problemDetails,
        status: 400,
      });
    });

    it('returns error object when server responds with generic error', async () => {
      const genericError = { message: 'Internal Server Error' };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: genericError,
        response: new Response(JSON.stringify(genericError), { status: 500 }),
      });

      const result = await registerUser({
        userName: 'user',
        password: 'password',
      });

      expect(result).toEqual({
        data: undefined,
        error: genericError,
        status: 500,
      });
    });
  });

  describe('loginUser', () => {
    it('returns user data on successful login', async () => {
      const mockUser = {
        id: 'u-123',
        userName: 'testuser',
        role: 'User' as const,
      };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: mockUser,
        error: undefined,
        response: new Response(JSON.stringify(mockUser), { status: 200 }),
      });

      const result = await loginUser({
        userName: 'testuser',
        password: 'password123',
      });

      expect(result).toEqual({
        data: mockUser,
        error: undefined,
        status: 200,
      });
    });

    it('handles ProblemDetails on failed login with status', async () => {
      const problemDetails = {
        title: 'Unauthorized',
        status: 401,
        detail: 'Неверный логин или пароль',
      };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: problemDetails,
        error: undefined,
        response: new Response(JSON.stringify(problemDetails), { status: 401 }),
      });

      const result = await loginUser({
        userName: 'testuser',
        password: 'wrongpassword',
      });

      expect(result).toEqual({
        data: undefined,
        error: problemDetails,
        status: 401,
      });
    });

    it('handles ProblemDetails on failed login without status defaulting to 400', async () => {
      const problemDetails = {
        title: 'Bad Request',
        detail: 'Invalid credentials format',
        errors: { credentials: ['Invalid credentials format'] },
      };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: problemDetails,
        error: undefined,
        response: new Response(JSON.stringify(problemDetails), { status: 400 }),
      });

      const result = await loginUser({
        userName: 'testuser',
        password: '',
      });

      expect(result).toEqual({
        data: undefined,
        error: problemDetails,
        status: 400,
      });
    });

    it('returns generic error when login fails with non-problem error', async () => {
      const genericError = { message: 'Service Unavailable' };

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: genericError,
        response: new Response(JSON.stringify(genericError), { status: 503 }),
      });

      const result = await loginUser({
        userName: 'testuser',
        password: 'password',
      });

      expect(result).toEqual({
        data: undefined,
        error: genericError,
        status: 503,
      });
    });
  });

  describe('getCurrentUser', () => {
    it('returns user data on successful GET /v1/users/me', async () => {
      const mockUserDto = {
        id: 'u-456',
        userName: 'adminuser',
        role: 'Admin' as const,
      };

      vi.spyOn(usersClient, 'GET').mockResolvedValueOnce({
        data: mockUserDto,
        error: undefined,
        response: new Response(JSON.stringify(mockUserDto), { status: 200 }),
      });

      const user = await getCurrentUser();

      expect(user).toEqual({
        id: 'u-456',
        userName: 'adminuser',
        role: 'Admin',
      });
    });

    it('returns null when response is not ok', async () => {
      vi.spyOn(usersClient, 'GET').mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Unauthorized' },
        response: new Response(null, { status: 401 }),
      });

      const user = await getCurrentUser();
      expect(user).toBeNull();
    });

    it('returns null when data is a ProblemDetails object', async () => {
      const problem = { title: 'Not Found', status: 404 };
      vi.spyOn(usersClient, 'GET').mockResolvedValueOnce({
        data: problem as unknown as { id: string; userName: string; role: 'Admin' | 'User' },
        error: undefined,
        response: new Response(JSON.stringify(problem), { status: 404 }),
      });

      const user = await getCurrentUser();
      expect(user).toBeNull();
    });

    it('returns null when GET throws an exception', async () => {
      vi.spyOn(usersClient, 'GET').mockRejectedValueOnce(new Error('Network failure'));

      const user = await getCurrentUser();
      expect(user).toBeNull();
    });
  });

  describe('refreshSession', () => {
    it('returns true when refresh succeeds', async () => {
      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });

      const result = await refreshSession();
      expect(result).toBe(true);
    });

    it('returns false when refresh fails', async () => {
      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 401 }),
      });

      const result = await refreshSession();
      expect(result).toBe(false);
    });

    it('returns false when POST throws an exception', async () => {
      vi.spyOn(usersClient, 'POST').mockRejectedValueOnce(new Error('Network error'));

      const result = await refreshSession();
      expect(result).toBe(false);
    });
  });

  describe('logoutUser', () => {
    it('calls POST /v1/auth/logout and clears user', async () => {
      const postSpy = vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        response: new Response(null, { status: 204 }),
      });
      const clearUserSpy = vi.spyOn(sessionStore, 'clearUser');

      await logoutUser();

      expect(postSpy).toHaveBeenCalledWith('/v1/auth/logout');
      expect(clearUserSpy).toHaveBeenCalled();
    });

    it('clears user even if POST /v1/auth/logout throws', async () => {
      vi.spyOn(usersClient, 'POST').mockRejectedValueOnce(new Error('Network error'));
      const clearUserSpy = vi.spyOn(sessionStore, 'clearUser');

      await logoutUser();

      expect(clearUserSpy).toHaveBeenCalled();
    });
  });
});
