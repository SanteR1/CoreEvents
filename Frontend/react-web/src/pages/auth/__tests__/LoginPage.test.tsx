import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { action, LoginPage } from '../LoginPage';
import { loginUser } from '@/features/auth/api/authApi';
import { setUser } from '@/shared/lib/auth';

vi.mock('@/features/auth/api/authApi', () => ({
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

function renderLoginPage(initialUrl = '/login') {
  const router = createMemoryRouter(
    [
      {
        path: '/login',
        element: <LoginPage />,
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
  url = 'http://localhost/login',
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
  id: 'u-alex',
  userName: 'alex',
  role: 'User' as const,
};

describe('LoginPage Component & Action', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseActionData.mockReturnValue(undefined);
  });

  describe('Component Rendering', () => {
    it('renders page heading and login form container', () => {
      renderLoginPage('/login');

      expect(
        screen.getByRole('heading', { level: 1, name: 'Вход в личный кабинет' }),
      ).toBeInTheDocument();
      expect(screen.getByLabelText('Имя пользователя')).toBeInTheDocument();
      expect(screen.getByLabelText('Пароль')).toBeInTheDocument();
    });

    it('passes actionData error to the form and displays it', () => {
      mockUseActionData.mockReturnValue({ error: 'Пользователь не найден' });
      renderLoginPage('/login');

      expect(screen.getByText('Пользователь не найден')).toBeInTheDocument();
    });

    it('extracts returnUrl from query params and passes to form action and register link', () => {
      const returnUrl = '/events/123';
      const { container } = renderLoginPage(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);

      const form = container.querySelector('form');
      expect(form).toHaveAttribute('action', `/login?returnUrl=${encodeURIComponent(returnUrl)}`);

      const registerLink = screen.getByRole('link', { name: 'Регистрация' });
      expect(registerLink).toHaveAttribute(
        'href',
        `/register?returnUrl=${encodeURIComponent(returnUrl)}`,
      );
    });

    it('satisfies a11y accessibility standards', async () => {
      const { container } = renderLoginPage('/login');

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

    it('returns custom error message when login fails', async () => {
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 401,
        error: { detail: 'Неверный пароль' },
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: 'wrong' });
      const result = await action(args);

      expect(result).toEqual({ error: 'Неверный пароль' });
    });

    it('returns default error message when login fails without specific error message', async () => {
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 400,
        error: {},
        data: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: 'wrong' });
      const result = await action(args);

      expect(result).toEqual({ error: 'Неверный логин или пароль' });
    });

    it('stores user and redirects to default "/" on successful login', async () => {
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 200,
        data: mockUser,
        error: undefined,
      });

      const args = createActionArgs({ username: 'alex', password: 'correct' });
      const result = await action(args);

      expect(setUser).toHaveBeenCalledWith(mockUser);
      expect(result).toBeInstanceOf(Response);
      const res = result as Response;
      expect(res.status).toBe(302);
      expect(res.headers.get('Location')).toBe('/');
    });

    it('stores user and redirects to safe returnUrl from search params', async () => {
      vi.mocked(loginUser).mockResolvedValueOnce({
        status: 200,
        data: mockUser,
        error: undefined,
      });

      const args = createActionArgs(
        { username: 'alex', password: 'correct' },
        'http://localhost/login?returnUrl=%2Fevents%2Fcurrent',
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
      vi.mocked(loginUser).mockRejectedValueOnce(new Error('Auth service network error'));

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(consoleSpy).toHaveBeenCalledWith('Login action error:', expect.any(Error));
      expect(result).toEqual({ error: 'Auth service network error' });
      consoleSpy.mockRestore();
    });

    it('catches non-Error exceptions and returns default service unavailable message', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(vi.fn());
      vi.mocked(loginUser).mockRejectedValueOnce('raw string crash');

      const args = createActionArgs({ username: 'alex', password: '123' });
      const result = await action(args);

      expect(consoleSpy).toHaveBeenCalledWith('Login action error:', 'raw string crash');
      expect(result).toEqual({
        error: 'Сервис временно недоступен. Проверьте интернет-соединение или попробуйте позже.',
      });
      consoleSpy.mockRestore();
    });
  });
});
