import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import axe from 'axe-core';
import App from '../App';

const mockUseNavigation = vi.fn(() => ({ state: 'idle' }));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigation: () => mockUseNavigation(),
  };
});

vi.mock('@/shared/lib/auth', () => ({
  useIsAuthenticated: () => false,
  clearToken: vi.fn(),
}));

function renderApp() {
  const router = createMemoryRouter([
    {
      path: '/',
      element: <App />,
      children: [
        {
          index: true,
          element: <div data-testid="outlet-content">Welcome to the App</div>,
        },
      ],
    },
  ]);

  return render(<RouterProvider router={router} />);
}

describe('App Layout Component', () => {
  beforeEach(() => {
    mockUseNavigation.mockReturnValue({ state: 'idle' });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders Header, main container and outlet child content', () => {
    renderApp();

    expect(screen.getByRole('banner')).toBeInTheDocument(); // <header> tag
    expect(screen.getByRole('main')).toBeInTheDocument(); // <main> tag
    expect(screen.getByTestId('outlet-content')).toHaveTextContent('Welcome to the App');
  });

  it('applies opacity: 1 to main content when navigation state is idle', () => {
    mockUseNavigation.mockReturnValue({ state: 'idle' });
    renderApp();

    const main = screen.getByRole('main');
    expect(main).toHaveStyle({ opacity: '1' });
  });

  it('applies opacity: 0.6 to main content during navigation loading state', () => {
    mockUseNavigation.mockReturnValue({ state: 'loading' });
    renderApp();

    const main = screen.getByRole('main');
    expect(main).toHaveStyle({ opacity: '0.6' });
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = renderApp();

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
