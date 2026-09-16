import { describe, it, expect, vi, beforeEach } from 'vitest';
import { registerUser, loginUser } from '../authApi';
import { usersClient } from '@/shared/api';

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
    it('returns JWT token on successful login', async () => {
      const mockToken = 'mock_jwt_token_xyz';

      vi.spyOn(usersClient, 'POST').mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        response: new Response(JSON.stringify(mockToken), { status: 200 }),
      });

      const result = await loginUser({
        userName: 'testuser',
        password: 'password123',
      });

      expect(result).toEqual({
        data: mockToken,
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
});
