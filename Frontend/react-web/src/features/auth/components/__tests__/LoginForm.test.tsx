import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { LoginForm } from '../LoginForm';

const mockUseNavigation = vi.fn<() => { state: 'idle' | 'loading' | 'submitting' }>(() => ({
  state: 'idle',
}));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigation: () => mockUseNavigation(),
  };
});

function renderLoginForm(props: { error?: string; returnUrl?: string | null } = {}) {
  const router = createMemoryRouter(
    [
      {
        path: '/login',
        element: <LoginForm {...props} />,
        action: () => null,
      },
    ],
    {
      initialEntries: ['/login'],
    },
  );
  return render(<RouterProvider router={router} />);
}

describe('LoginForm Component', () => {
  beforeEach(() => {
    mockUseNavigation.mockReturnValue({ state: 'idle' });
  });

  it('renders username and password input fields with proper labels and attributes', () => {
    renderLoginForm();

    const usernameInput = screen.getByLabelText<HTMLInputElement>('Имя пользователя');
    const passwordInput = screen.getByLabelText<HTMLInputElement>('Пароль');
    const submitBtn = screen.getByRole('button', { name: 'Войти' });

    expect(usernameInput).toBeInTheDocument();
    expect(usernameInput).toHaveAttribute('type', 'text');
    expect(usernameInput).toHaveAttribute('name', 'username');
    expect(usernameInput).toBeRequired();
    expect(usernameInput).toHaveAttribute('autocomplete', 'username');

    expect(passwordInput).toBeInTheDocument();
    expect(passwordInput).toHaveAttribute('type', 'password');
    expect(passwordInput).toHaveAttribute('name', 'password');
    expect(passwordInput).toBeRequired();
    expect(passwordInput).toHaveAttribute('autocomplete', 'current-password');

    expect(submitBtn).toBeInTheDocument();
    expect(submitBtn).not.toBeDisabled();
  });

  it('sets default form action and register link when returnUrl is not provided', () => {
    const { container } = renderLoginForm();

    const form = container.querySelector('form');
    expect(form).toHaveAttribute('action', '/login');

    const registerLink = screen.getByRole('link', { name: 'Регистрация' });
    expect(registerLink).toHaveAttribute('href', '/register');
  });

  it('encodes returnUrl in form action and register link when returnUrl is provided', () => {
    const returnUrl = '/events/top?category=music';
    const { container } = renderLoginForm({ returnUrl });

    const form = container.querySelector('form');
    expect(form).toHaveAttribute('action', `/login?returnUrl=${encodeURIComponent(returnUrl)}`);

    const registerLink = screen.getByRole('link', { name: 'Регистрация' });
    expect(registerLink).toHaveAttribute(
      'href',
      `/register?returnUrl=${encodeURIComponent(returnUrl)}`,
    );
  });

  it('renders error message banner when error prop is provided', () => {
    renderLoginForm({ error: 'Неверный логин или пароль' });

    const errorBanner = screen.getByText('Неверный логин или пароль');
    expect(errorBanner).toBeInTheDocument();
    expect(errorBanner.className).toContain('text-red-600');
  });

  it('renders recovery link pointing to /recovery', () => {
    renderLoginForm();

    const recoveryLink = screen.getByRole('link', { name: 'Забыли пароль?' });
    expect(recoveryLink).toBeInTheDocument();
    expect(recoveryLink).toHaveAttribute('href', '/recovery');
  });

  it('disables submit button and shows "Вход..." when submitting', () => {
    mockUseNavigation.mockReturnValue({ state: 'submitting' });
    renderLoginForm();

    const submitBtn = screen.getByRole('button', { name: 'Вход...' });
    expect(submitBtn).toBeInTheDocument();
    expect(submitBtn).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Войти' })).not.toBeInTheDocument();
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = renderLoginForm({ error: 'Проверка ошибки' });

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
