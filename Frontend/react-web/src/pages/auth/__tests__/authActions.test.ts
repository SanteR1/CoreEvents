import { describe, it, expect, vi, beforeEach } from 'vitest';
import { action as loginAction } from '../LoginPage';
import { action as registerAction } from '../RegisterPage';
import { loginUser, registerUser } from '@/features/auth/api/authApi';
import { getToken, clearToken } from '@/shared/lib/auth';

vi.mock('@/features/auth/api/authApi', () => ({
  loginUser: vi.fn(),
  registerUser: vi.fn(),
}));

type ActionArgs = Parameters<typeof loginAction>[0];

function createActionRequest(url: string, body: Record<string, string>): ActionArgs {
  const formData = new FormData();
  Object.entries(body).forEach(([key, value]) => {
    formData.append(key, value);
  });
  const request = new Request(url, {
    method: 'POST',
    body: formData,
  });
  return {
    request,
    params: {},
    context: {} as ActionArgs['context'],
  } as unknown as ActionArgs;
}

function createMockJwt(expSecondsFromNow = 3600): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({ sub: 'user_123', exp: Math.floor(Date.now() / 1000) + expSecondsFromNow }),
  );
  return `${header}.${payload}.signature`;
}

describe('Auth Pages Actions', () => {
  const originalConsoleError = console.error;

  beforeEach(() => {
    clearToken();
    vi.clearAllMocks();
    console.error = vi.fn();
  });

  afterEach(() => {
    console.error = originalConsoleError;
  });

  describe('LoginPage action', () => {
    it('returns error if username is missing', async () => {
      const args = createActionRequest('http://localhost/login', {
        password: 'password123',
      });
      const result = await loginAction(args);
      expect(result).toEqual({ error: 'Заполните имя пользователя и пароль' });
    });

    it('returns error if password is missing', async () => {
      const args = createActionRequest('http://localhost/login', {
        username: 'alice',
      });
      const result = await loginAction(args);
      expect(result).toEqual({ error: 'Заполните имя пользователя и пароль' });
    });

    it('returns error message from API if login fails', async () => {
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Неверный логин или пароль' },
        status: 400,
      });

      const args = createActionRequest('http://localhost/login', {
        username: 'alice',
        password: 'wrongpassword',
      });
      const result = await loginAction(args);
      expect(result).toEqual({ error: 'Неверный логин или пароль' });
    });

    it('saves token and redirects to default "/" on successful login', async () => {
      const mockToken = createMockJwt();
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        status: 200,
      });

      const args = createActionRequest('http://localhost/login', {
        username: 'alice',
        password: 'secretpassword',
      });
      const result = await loginAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
      expect(getToken()).toBe(mockToken);
    });

    it('redirects to safe returnUrl on successful login', async () => {
      const mockToken = createMockJwt();
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        status: 200,
      });

      const args = createActionRequest('http://localhost/login?returnUrl=%2Fevents%2F42', {
        username: 'alice',
        password: 'secretpassword',
      });
      const result = await loginAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/events/42');
      expect(getToken()).toBe(mockToken);
    });

    it('sanitizes malicious open redirect returnUrl to "/"', async () => {
      const mockToken = createMockJwt();
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        status: 200,
      });

      const args = createActionRequest('http://localhost/login?returnUrl=%2F%2Fevil.com%2Fphish', {
        username: 'alice',
        password: 'secretpassword',
      });
      const result = await loginAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
    });

    it('catches unexpected exceptions and returns user-friendly error', async () => {
      vi.mocked(loginUser).mockRejectedValueOnce(new Error('Network offline'));

      const args = createActionRequest('http://localhost/login', {
        username: 'alice',
        password: 'secretpassword',
      });
      const result = await loginAction(args);
      expect(result).toEqual({ error: 'Network offline' });
    });
  });

  describe('RegisterPage action', () => {
    it('returns error if username or password is missing', async () => {
      const args = createActionRequest('http://localhost/register', {
        username: '',
        password: 'password123',
      });
      const result = await registerAction(args);
      expect(result).toEqual({ error: 'Заполните имя пользователя и пароль' });
    });

    it('returns API error if registration fails (e.g. username taken)', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        data: undefined,
        error: { detail: 'Пользователь с таким именем уже существует' },
        status: 409,
      });

      const args = createActionRequest('http://localhost/register', {
        username: 'existing_user',
        password: 'password123',
      });
      const result = await registerAction(args);
      expect(result).toEqual({ error: 'Пользователь с таким именем уже существует' });
    });

    it('redirects to /login if registration succeeds but auto-login fails', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        status: 201,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: undefined,
        error: { message: 'Временная ошибка входа' },
        status: 500,
      });

      const args = createActionRequest('http://localhost/register?returnUrl=%2Fevents%2F1', {
        username: 'new_user',
        password: 'password123',
      });
      const result = await registerAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/login?returnUrl=%2Fevents%2F1');
    });

    it('automatically logs in and redirects to safe returnUrl on successful registration', async () => {
      const mockToken = createMockJwt();
      vi.mocked(registerUser).mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        status: 201,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        status: 200,
      });

      const args = createActionRequest(
        'http://localhost/register?returnUrl=%2Fbookings%2Fcreate%2F5',
        {
          username: 'new_user',
          password: 'password123',
        },
      );
      const result = await registerAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/bookings/create/5');
      expect(getToken()).toBe(mockToken);
    });

    it('sanitizes malicious open redirect returnUrl to "/" during registration', async () => {
      const mockToken = createMockJwt();
      vi.mocked(registerUser).mockResolvedValueOnce({
        data: undefined,
        error: undefined,
        status: 201,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        data: mockToken,
        error: undefined,
        status: 200,
      });

      const args = createActionRequest(
        'http://localhost/register?returnUrl=https%3A%2F%2Fevil.com',
        {
          username: 'new_user',
          password: 'password123',
        },
      );
      const result = await registerAction(args);

      expect(result).toBeInstanceOf(Response);
      const response = result as Response;
      expect(response.status).toBe(302);
      expect(response.headers.get('Location')).toBe('/');
    });

    it('catches unexpected exceptions during registration', async () => {
      vi.mocked(registerUser).mockRejectedValueOnce(new Error('Database unavailable'));

      const args = createActionRequest('http://localhost/register', {
        username: 'new_user',
        password: 'password123',
      });
      const result = await registerAction(args);
      expect(result).toEqual({ error: 'Database unavailable' });
    });
  });
});
