import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { action, RegisterPage } from '../RegisterPage';
import { registerUser, loginUser } from '@/features/auth/api/authApi';
import { setUser } from '@/shared/lib/auth';

vi.mock('@/features/auth/api/authApi', () => ({
  registerUser: vi.fn(),
  loginUser: vi.fn(),
}));

vi.mock('@/shared/lib/auth', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/lib/auth')>();
  return {
    ...actual,
    setUser: vi.fn(),
  };
});

const mockUseActionData = vi.fn<() => { error?: string } | undefined>(() => undefined);

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useActionData: () => mockUseActionData(),
  };
});

function renderRegisterPage(initialUrl = '/register') {
  const router = createMemoryRouter(
    [
      {
        path: '/register',
        element: <RegisterPage />,
        action: () => null,
      },
    ],
    {
      initialEntries: [initialUrl],
    },
  );

  return render(<RouterProvider router={router} />);
}

function createActionArgs(
  formDataRecord: Record<string, string>,
  url = 'http://localhost/register',
): Parameters<typeof action>[0] {
  const formData = new FormData();
  Object.entries(formDataRecord).forEach(([k, v]) => formData.append(k, v));
  return {
    request: new Request(url, { method: 'POST', body: formData }),
    params: {},
    context: {},
  } as unknown as Parameters<typeof action>[0];
}

const mockUser = {
  id: 'u-reg-1',
  userName: 'alex',
  role: 'User' as const,
};

describe('RegisterPage Component & Action', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseActionData.mockReturnValue(undefined);
  });

  describe('Component Rendering', () => {
    it('renders page heading and register form container', () => {
      renderRegisterPage('/register');

      expect(screen.getByRole('heading', { level: 1, name: 'Регистрация' })).toBeInTheDocument();
      expect(screen.getByLabelText('Имя пользователя')).toBeInTheDocument();
      expect(screen.getByLabelText('Пароль')).toBeInTheDocument();
    });

    it('passes actionData error to the form and displays it', () => {
      mockUseActionData.mockReturnValue({ error: 'Пользователь уже существует' });
      renderRegisterPage('/register');

      expect(screen.getByText('Пользователь уже существует')).toBeInTheDocument();
    });

    it('extracts returnUrl from query params and passes to form action and login link', () => {
      const returnUrl = '/events/456';
      const { container } = renderRegisterPage(
        `/register?returnUrl=${encodeURIComponent(returnUrl)}`,
      );

      const form = container.querySelector('form');
      expect(form).toHaveAttribute(
        'action',
        `/register?returnUrl=${encodeURIComponent(returnUrl)}`,
      );

      const loginLink = screen.getByRole('link', { name: 'Войти' });
      expect(loginLink).toHaveAttribute(
        'href',
        `/login?returnUrl=${encodeURIComponent(returnUrl)}`,
      );
    });

    it('satisfies a11y accessibility standards', async () => {
      const { container } = renderRegisterPage('/register');

      const results = await axe.run(container, {
        rules: { 'color-contrast': { enabled: false } },
      });
      expect(results.violations).toEqual([]);
    });
  });

  describe('action function', () => {
    it('returns validation error when username or password is missing', async () => {
      const missingUser = createActionArgs({ username: '', password: '123' });
      const res1 = await action(missingUser);
      expect(res1).toEqual({ error: 'Заполните имя пользователя и пароль' });

      const missingPass = createActionArgs({ username: 'alex', password: '' });
      const res2 = await action(missingPass);
      expect(res2).toEqual({ error: 'Заполните имя пользователя и пароль' });
    });

    it('returns detail error when registration fails with detail field', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 400,
        error: { detail: 'Пароль слишком простой' },
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(result).toEqual({ error: 'Пароль слишком простой' });
    });

    it('returns title error when registration fails with title but no detail', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 400,
        error: { title: 'Ошибка валидации полей' },
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(result).toEqual({ error: 'Ошибка валидации полей' });
    });

    it('returns message error when registration fails with message but no detail/title', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 400,
        error: { message: 'Пользователь уже существует' } as unknown as undefined,
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(result).toEqual({ error: 'Пользователь уже существует' });
    });

    it('returns default fallback error when registration fails without error fields', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 500,
        error: {},
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(result).toEqual({
        error: 'Ошибка регистрации. Возможно, пользователь с таким именем уже существует.',
      });
    });

    it('redirects to login when registration succeeds but auto-login fails', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 201,
        error: undefined,
        data: undefined,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 401,
        error: { detail: 'Invalid credentials' },
        data: undefined,
      });

      const args = createActionArgs(
        { username: 'alex', password: 'Password123!' },
        'http://localhost/register?returnUrl=%2Fevents%2Fcurrent',
      );
      const result = await action(args);

      expect(result).toBeInstanceOf(Response);
      const res = result as Response;
      expect(res.status).toBe(302);
      expect(res.headers.get('Location')).toContain('/login?returnUrl=');
    });

    it('redirects to /login without query param when auto-login fails and returnUrl is default /', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 201,
        error: undefined,
        data: undefined,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 401,
        error: { detail: 'Invalid credentials' },
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: 'Password123!' });
      const result = await action(args);

      expect(result).toBeInstanceOf(Response);
      const res = result as Response;
      expect(res.status).toBe(302);
      expect(res.headers.get('Location')).toBe('/login');
    });

    it('stores user and redirects to target returnUrl on complete success', async () => {
      vi.mocked(registerUser).mockResolvedValueOnce({
        status: 204,
        error: undefined,
        data: undefined,
      });
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 200,
        data: mockUser,
        error: undefined,
      });

      const args = createActionArgs(
        { username: 'alex', password: 'Password123!' },
        'http://localhost/register?returnUrl=%2Fevents%2Fcurrent',
      );
      const result = await action(args);

      expect(setUser).toHaveBeenCalledWith(mockUser);
      expect(result).toBeInstanceOf(Response);
      const res = result as Response;
      expect(res.status).toBe(302);
      expect(res.headers.get('Location')).toBe('/events/current');
    });

    it('catches Error exceptions and returns error message', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(vi.fn());
      vi.mocked(registerUser).mockRejectedValueOnce(new Error('Database unavailable'));

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(consoleSpy).toHaveBeenCalledWith('Register action error:', expect.any(Error));
      expect(result).toEqual({ error: 'Database unavailable' });
      consoleSpy.mockRestore();
    });

    it('catches non-Error exceptions and returns default service unavailable message', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(vi.fn());
      vi.mocked(registerUser).mockRejectedValueOnce('raw string crash');

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(consoleSpy).toHaveBeenCalledWith('Register action error:', 'raw string crash');
      expect(result).toEqual({
        error: 'Сервис временно недоступен. Проверьте интернет-соединение или попробуйте позже.',
      });
      consoleSpy.mockRestore();
    });
  });
});
