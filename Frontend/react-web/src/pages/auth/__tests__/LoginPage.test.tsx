import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { LoginPage } from '../LoginPage';

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

describe('LoginPage Component', () => {
  beforeEach(() => {
    mockUseActionData.mockReturnValue(undefined);
  });

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
