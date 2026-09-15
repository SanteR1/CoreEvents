import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import { Header } from '../Header';
import { ThemeProvider } from '@/shared/lib/theme';

const mockUseNavigation = vi.fn<() => { state: 'idle' | 'loading' | 'submitting' }>(() => ({
  state: 'idle',
}));
const mockUseIsAuthenticated = vi.fn<() => boolean>(() => false);
const mockClearToken = vi.fn<() => void>();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigation: () => mockUseNavigation(),
  };
});

vi.mock('@/shared/lib/auth', () => ({
  useIsAuthenticated: () => mockUseIsAuthenticated(),
  clearToken: () => {
    mockClearToken();
  },
}));

function renderHeaderWithRouter(initialEntry = '/') {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: (
          <ThemeProvider>
            <Header />
            <div data-testid="page-content">Home Content</div>
          </ThemeProvider>
        ),
      },
      {
        path: '/events/topevents',
        element: (
          <ThemeProvider>
            <Header />
            <div data-testid="page-content">Top Events Content</div>
          </ThemeProvider>
        ),
      },
      {
        path: '/events/create',
        element: (
          <ThemeProvider>
            <Header />
            <div data-testid="page-content">Create Event Content</div>
          </ThemeProvider>
        ),
      },
      {
        path: '/login',
        element: (
          <ThemeProvider>
            <Header />
            <div data-testid="page-content">Login Page Content</div>
          </ThemeProvider>
        ),
      },
    ],
    {
      initialEntries: [initialEntry],
    },
  );

  const result = render(<RouterProvider router={router} />);
  return { ...result, router };
}

describe('Header Component', () => {
  beforeEach(() => {
    mockUseIsAuthenticated.mockReturnValue(false);
    mockUseNavigation.mockReturnValue({ state: 'idle' });
    mockClearToken.mockClear();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders public navigation links and highlights active route', () => {
    renderHeaderWithRouter('/');

    const homeLink = screen.getByRole('link', { name: 'Главная' });
    const topEventsLink = screen.getByRole('link', { name: 'Топовые события' });

    expect(homeLink).toBeInTheDocument();
    expect(topEventsLink).toBeInTheDocument();

    // Home is active on '/'
    expect(homeLink.className).toContain('font-semibold');
    expect(homeLink.className).toContain('text-(--accent)');
    expect(topEventsLink.className).not.toContain('font-semibold');
  });

  it('highlights "Топовые события" when on /events/topevents', () => {
    renderHeaderWithRouter('/events/topevents');

    const homeLink = screen.getByRole('link', { name: 'Главная' });
    const topEventsLink = screen.getByRole('link', { name: 'Топовые события' });

    expect(topEventsLink.className).toContain('font-semibold');
    expect(topEventsLink.className).toContain('text-(--accent)');
    expect(homeLink.className).not.toContain('font-semibold');
  });

  it('renders in guest mode: displays "Вход", hides "Создать событие" and "Выйти"', () => {
    renderHeaderWithRouter('/');

    expect(screen.getByRole('link', { name: 'Вход' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Создать событие' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Выйти' })).not.toBeInTheDocument();
  });

  it('renders in authenticated mode: displays "Создать событие" and "Выйти", hides "Вход"', () => {
    mockUseIsAuthenticated.mockReturnValue(true);
    renderHeaderWithRouter('/');

    expect(screen.getByRole('link', { name: 'Создать событие' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Выйти' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Вход' })).not.toBeInTheDocument();
  });

  it('clears token and navigates to /login when clicking "Выйти"', async () => {
    const user = userEvent.setup();
    mockUseIsAuthenticated.mockReturnValue(true);

    const { router } = renderHeaderWithRouter('/');

    const logoutBtn = screen.getByRole('button', { name: 'Выйти' });
    await user.click(logoutBtn);

    expect(mockClearToken).toHaveBeenCalledTimes(1);
    expect(router.state.location.pathname).toBe('/login');
    expect(screen.getByTestId('page-content')).toHaveTextContent('Login Page Content');
  });

  it('displays loading indicator when navigation state is "loading"', () => {
    mockUseNavigation.mockReturnValue({ state: 'loading' });
    renderHeaderWithRouter('/');

    expect(screen.getByText('Загрузка...')).toBeInTheDocument();
  });

  it('hides loading indicator when navigation state is "idle"', () => {
    mockUseNavigation.mockReturnValue({ state: 'idle' });
    renderHeaderWithRouter('/');

    expect(screen.queryByText('Загрузка...')).not.toBeInTheDocument();
  });

  it('renders ThemeToggle buttons inside Header', () => {
    renderHeaderWithRouter('/');

    expect(screen.getByRole('button', { name: /Светлая/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Система/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Тёмная/i })).toBeInTheDocument();
  });

  it('satisfies a11y accessibility audit in guest and authenticated states', async () => {
    const { container, unmount } = renderHeaderWithRouter('/');
    let results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);

    unmount();

    mockUseIsAuthenticated.mockReturnValue(true);
    const { container: authContainer } = renderHeaderWithRouter('/');
    results = await axe.run(authContainer, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
