import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { RegisterPage } from '../RegisterPage';

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

describe('RegisterPage Component', () => {
  beforeEach(() => {
    mockUseActionData.mockReturnValue(undefined);
  });

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
    expect(form).toHaveAttribute('action', `/register?returnUrl=${encodeURIComponent(returnUrl)}`);

    const loginLink = screen.getByRole('link', { name: 'Войти' });
    expect(loginLink).toHaveAttribute('href', `/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = renderRegisterPage('/register');

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
