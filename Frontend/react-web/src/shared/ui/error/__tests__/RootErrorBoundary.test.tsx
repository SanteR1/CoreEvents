import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { RootErrorBoundary } from '../RootErrorBoundary';
import { checkA11y } from '@/shared/lib/test/axe';

function renderBoundaryWithError(errorToThrow: unknown, initialUrl = '/events/current') {
  const router = createMemoryRouter(
    [
      {
        path: '/events/current',
        loader: () => {
          throw errorToThrow;
        },
        element: <div>Protected Content</div>,
        ErrorBoundary: RootErrorBoundary,
        HydrateFallback: () => null,
      },
      {
        path: '/login',
        element: <div>Login Page</div>,
      },
      {
        path: '/',
        element: <div>Home Catalog</div>,
      },
    ],
    { initialEntries: [initialUrl] },
  );

  return render(<RouterProvider router={router} />);
}

describe('RootErrorBoundary', () => {
  const originalConsoleError = console.error;
  beforeEach(() => {
    console.error = vi.fn();
  });
  afterEach(() => {
    console.error = originalConsoleError;
    vi.restoreAllMocks();
  });

  describe('401 Unauthorized', () => {
    it('renders 401 state with login link including returnUrl and satisfies a11y', async () => {
      const errorResponse = new Response(null, {
        status: 401,
        statusText: 'Unauthorized',
      });

      const { container } = renderBoundaryWithError(errorResponse, '/events/current?view=details');

      expect(await screen.findByText(/код ошибки: 401/i)).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /требуется авторизация/i })).toBeInTheDocument();
      expect(screen.getByText('🔒')).toBeInTheDocument();

      const loginLink = screen.getByRole('link', { name: /войти в систему/i });
      expect(loginLink).toBeInTheDocument();
      expect(loginLink).toHaveAttribute(
        'href',
        '/login?returnUrl=%2Fevents%2Fcurrent%3Fview%3Ddetails',
      );

      // "Повторить попытку" button should NOT be rendered for 401
      expect(screen.queryByRole('button', { name: /повторить попытку/i })).not.toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('403 Forbidden', () => {
    it('renders 403 state with catalog link and satisfies a11y', async () => {
      const errorResponse = new Response(null, {
        status: 403,
        statusText: 'Forbidden',
      });

      const { container } = renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 403/i)).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /доступ запрещён/i })).toBeInTheDocument();
      expect(screen.getByText('🚫')).toBeInTheDocument();

      const catalogLink = screen.getByRole('link', { name: /в каталог событий/i });
      expect(catalogLink).toBeInTheDocument();
      expect(catalogLink).toHaveAttribute('href', '/');

      expect(screen.queryByRole('button', { name: /повторить попытку/i })).not.toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('404 Not Found', () => {
    it('renders 404 state with catalog link and satisfies a11y', async () => {
      const errorResponse = new Response('Запрашиваемое событие удалено', {
        status: 404,
        statusText: 'Not Found',
      });

      const { container } = renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 404/i)).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /ресурс не найден/i })).toBeInTheDocument();
      expect(screen.getByText('🔍')).toBeInTheDocument();
      expect(screen.getByText('Запрашиваемое событие удалено')).toBeInTheDocument();

      expect(screen.queryByRole('button', { name: /повторить попытку/i })).not.toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('500 Server Error', () => {
    it('renders 500 error with reload button and satisfies a11y', async () => {
      const errorResponse = new Response(null, {
        status: 500,
        statusText: 'Internal Server Error',
      });

      const user = userEvent.setup();
      const { container } = renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 500/i)).toBeInTheDocument();
      expect(
        screen.getByRole('heading', { name: /сервис временно недоступен/i }),
      ).toBeInTheDocument();

      const retryBtn = screen.getByRole('button', { name: /повторить попытку/i });
      expect(retryBtn).toBeInTheDocument();
      await user.click(retryBtn);

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('Network Errors (Failed to fetch)', () => {
    it('identifies network TypeError and renders network error icon and message', async () => {
      const networkError = new TypeError('Failed to fetch');

      const { container } = renderBoundaryWithError(networkError);

      expect(await screen.findByText('📡')).toBeInTheDocument();
      expect(
        screen.getByRole('heading', { name: /ошибка сетевого соединения/i }),
      ).toBeInTheDocument();
      // statusCode 0 should not render status code badge
      expect(screen.queryByText(/код ошибки:/i)).not.toBeInTheDocument();

      // Technical details section should be rendered for Error instances
      const details = screen.getByText(/техническая информация об ошибке/i);
      expect(details).toBeInTheDocument();

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('Unexpected Runtime Errors', () => {
    it('renders error message and technical stack details for unexpected errors', async () => {
      const runtimeError = new Error('Unexpected database syntax error');

      const { container } = renderBoundaryWithError(runtimeError);

      expect(
        await screen.findByRole('heading', { name: /произошла непредвиденная ошибка/i }),
      ).toBeInTheDocument();
      expect(screen.getByText('Unexpected database syntax error')).toBeInTheDocument();

      const pre = container.querySelector('pre');
      expect(pre).toBeInTheDocument();
      expect(pre?.textContent).toContain('Unexpected database syntax error');

      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });

  describe('Navigation', () => {
    it('navigates back when "Назад" button is clicked', async () => {
      const user = userEvent.setup();
      const runtimeError = new Error('Generic error');

      renderBoundaryWithError(runtimeError);

      const backBtn = await screen.findByRole('button', { name: /^назад$/i });
      expect(backBtn).toBeInTheDocument();
      await user.click(backBtn);
    });
  });

  describe('Custom and Object Error Responses', () => {
    it('extracts message from error.data object with message property', async () => {
      const errorResponse = new Response(
        JSON.stringify({ message: 'Ошибка валидации полезной нагрузки' }),
        {
          status: 422,
          statusText: 'Unprocessable Entity',
          headers: { 'Content-Type': 'application/json' },
        },
      );

      renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 422/i)).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /ошибка 422/i })).toBeInTheDocument();
      expect(screen.getByText('Ошибка валидации полезной нагрузки')).toBeInTheDocument();
    });

    it('falls back to default 404 message when message matches statusText', async () => {
      const errorResponse = new Response(null, {
        status: 404,
        statusText: 'Not Found',
      });

      renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 404/i)).toBeInTheDocument();
      expect(
        screen.getByText(
          /запрошенная страница, событие или бронирование не существуют либо были удалены/i,
        ),
      ).toBeInTheDocument();
    });

    it('handles statusText fallback when error.data is empty and status is arbitrary', async () => {
      const errorResponse = new Response(null, {
        status: 418,
        statusText: "I'm a Teapot",
      });

      renderBoundaryWithError(errorResponse);

      expect(await screen.findByText(/код ошибки: 418/i)).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /ошибка 418/i })).toBeInTheDocument();
      expect(screen.getByText("I'm a Teapot")).toBeInTheDocument();
    });
  });
});
