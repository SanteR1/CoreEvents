import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider, useTheme } from '../index';
import { ThemeProvider as AppThemeProvider } from '@/app/providers/ThemeProvider';
import { useTheme as useAppTheme } from '@/app/providers/themeContext';

function TestConsumer() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  return (
    <div>
      <div data-testid="theme">{theme}</div>
      <div data-testid="resolved">{resolvedTheme}</div>
      <button onClick={() => setTheme('light')}>Set Light</button>
      <button onClick={() => setTheme('dark')}>Set Dark</button>
      <button onClick={() => setTheme('system')}>Set System</button>
    </div>
  );
}

describe('ThemeProvider & useTheme', () => {
  type Listener = (e: MediaQueryListEvent) => void;
  let listeners: Set<Listener>;
  let systemPrefersDark: boolean;
  let addEventListenerSpy: ReturnType<typeof vi.fn>;
  let removeEventListenerSpy: ReturnType<typeof vi.fn>;

  const triggerMediaChange = (matches: boolean) => {
    systemPrefersDark = matches;
    const event = {
      matches,
      media: '(prefers-color-scheme: dark)',
    } as MediaQueryListEvent;
    act(() => {
      listeners.forEach((listener) => listener(event));
    });
  };

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
    listeners = new Set();
    systemPrefersDark = false;

    addEventListenerSpy = vi.fn((event: string, cb: EventListenerOrEventListenerObject) => {
      if (event === 'change') {
        listeners.add(cb as Listener);
      }
    });

    removeEventListenerSpy = vi.fn((event: string, cb: EventListenerOrEventListenerObject) => {
      if (event === 'change') {
        listeners.delete(cb as Listener);
      }
    });

    vi.spyOn(window, 'matchMedia').mockImplementation(
      (query: string) =>
        ({
          matches: systemPrefersDark,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: addEventListenerSpy,
          removeEventListener: removeEventListenerSpy,
          dispatchEvent: vi.fn(),
        }) as unknown as MediaQueryList,
    );
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
    vi.restoreAllMocks();
  });

  it('initializes with "system" and resolved "light" when localStorage is empty and system prefers light', () => {
    systemPrefersDark = false;
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('theme')).toHaveTextContent('system');
    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('initializes with "system" and resolved "dark" when localStorage is empty and system prefers dark', () => {
    systemPrefersDark = true;
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('theme')).toHaveTextContent('system');
    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
  });

  it('initializes with "dark" if stored in localStorage', () => {
    localStorage.setItem('theme', 'dark');
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
  });

  it('initializes with "light" if stored in localStorage', () => {
    localStorage.setItem('theme', 'light');
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('theme')).toHaveTextContent('light');
    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
  });

  it('switches to dark mode via setTheme("dark")', async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Set Dark' }));

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('switches to light mode via setTheme("light")', async () => {
    const user = userEvent.setup();
    localStorage.setItem('theme', 'dark');
    document.documentElement.classList.add('dark');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Set Light' }));

    expect(screen.getByTestId('theme')).toHaveTextContent('light');
    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(localStorage.getItem('theme')).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('switches to system mode via setTheme("system"), removing localStorage key', async () => {
    const user = userEvent.setup();
    localStorage.setItem('theme', 'dark');
    systemPrefersDark = true;

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Set System' }));

    expect(screen.getByTestId('theme')).toHaveTextContent('system');
    expect(localStorage.getItem('theme')).toBeNull();
    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('reacts to system color scheme changes when theme is "system"', () => {
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);

    triggerMediaChange(true);

    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);

    triggerMediaChange(false);

    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('ignores system color scheme changes when theme is explicitly set to "dark"', async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Set Dark' }));
    expect(screen.getByTestId('theme')).toHaveTextContent('dark');

    // Listener should not be registered or active in explicit mode
    triggerMediaChange(false);

    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('unsubscribes from mediaQuery listener when unmounted', () => {
    const { unmount } = render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(listeners.size).toBe(1);

    unmount();

    expect(removeEventListenerSpy).toHaveBeenCalled();
    expect(listeners.size).toBe(0);
  });

  it('throws descriptive error when useTheme is called outside ThemeProvider', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(vi.fn());

    expect(() => render(<TestConsumer />)).toThrow('useTheme must be used within a ThemeProvider');

    spy.mockRestore();
  });

  it('supports app provider re-exports', () => {
    function AppConsumer() {
      const { theme } = useAppTheme();
      return <div data-testid="app-theme">{theme}</div>;
    }

    render(
      <AppThemeProvider>
        <AppConsumer />
      </AppThemeProvider>,
    );

    expect(screen.getByTestId('app-theme')).toHaveTextContent('system');
  });
});
