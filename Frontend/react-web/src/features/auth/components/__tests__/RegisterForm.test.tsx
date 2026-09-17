import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { RegisterForm } from '../RegisterForm';

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

function renderRegisterForm(props: { error?: string; returnUrl?: string | null } = {}) {
  const router = createMemoryRouter(
    [
      {
        path: '/register',
        element: <RegisterForm {...props} />,
        action: () => null,
      },
    ],
    {
      initialEntries: ['/register'],
    },
  );
  return render(<RouterProvider router={router} />);
}

describe('RegisterForm Component', () => {
  beforeEach(() => {
    mockUseNavigation.mockReturnValue({ state: 'idle' });
  });

  it('renders username and password input fields with proper labels and attributes', () => {
    renderRegisterForm();

    const usernameInput = screen.getByLabelText<HTMLInputElement>('Имя пользователя');
    const passwordInput = screen.getByLabelText<HTMLInputElement>('Пароль');
    const submitBtn = screen.getByRole('button', { name: 'Зарегистрироваться' });

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

  it('sets default form action and login link when returnUrl is not provided', () => {
    const { container } = renderRegisterForm();

    const form = container.querySelector('form');
    expect(form).toHaveAttribute('action', '/register');

    const loginLink = screen.getByRole('link', { name: 'Войти' });
    expect(loginLink).toHaveAttribute('href', '/login');
    expect(screen.getByText('Уже есть аккаунт?')).toBeInTheDocument();
  });

  it('encodes returnUrl in form action and login link when returnUrl is provided', () => {
    const returnUrl = '/bookings/123';
    const { container } = renderRegisterForm({ returnUrl });

    const form = container.querySelector('form');
    expect(form).toHaveAttribute('action', `/register?returnUrl=${encodeURIComponent(returnUrl)}`);

    const loginLink = screen.getByRole('link', { name: 'Войти' });
    expect(loginLink).toHaveAttribute('href', `/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  });

  it('renders error message banner when error prop is provided', () => {
    renderRegisterForm({ error: 'Пользователь уже существует' });

    const errorBanner = screen.getByText('Пользователь уже существует');
    expect(errorBanner).toBeInTheDocument();
    expect(errorBanner.className).toContain('text-red-600');
  });

  it('disables submit button when navigation state is "submitting"', () => {
    mockUseNavigation.mockReturnValue({ state: 'submitting' });
    renderRegisterForm();

    const submitBtn = screen.getByRole('button', { name: 'Зарегистрироваться' });
    expect(submitBtn).toBeDisabled();
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = renderRegisterForm({ error: 'Ошибка регистрации' });

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
