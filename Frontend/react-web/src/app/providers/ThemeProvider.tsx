import { useEffect, useState, type ReactNode } from 'react';
import { ThemeContext, type ThemePreference } from './themeContext';

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [themePreference, setThemePreference] = useState<ThemePreference>(() => {
    const stored = localStorage.getItem('theme');
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    return 'system';
  });

  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() => {
    if (typeof window === 'undefined') return 'light';
    const stored = localStorage.getItem('theme');
    if (stored === 'dark') return 'dark';
    if (stored === 'light') return 'light';
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  });

  useEffect(() => {
    if (themePreference !== 'system') return;

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const listener = (e: MediaQueryListEvent) => {
      document.documentElement.classList.toggle('dark', e.matches);
      setResolvedTheme(e.matches ? 'dark' : 'light');
    };

    mediaQuery.addEventListener('change', listener);
    return () => {
      mediaQuery.removeEventListener('change', listener);
    };
  }, [themePreference]);

  const setTheme = (newTheme: ThemePreference) => {
    if (newTheme === 'system') {
      localStorage.removeItem('theme');
      const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      setResolvedTheme(isDark ? 'dark' : 'light');
      document.documentElement.classList.toggle('dark', isDark);
    } else {
      localStorage.setItem('theme', newTheme);
      setResolvedTheme(newTheme);
      document.documentElement.classList.toggle('dark', newTheme === 'dark');
    }
    setThemePreference(newTheme);
  };

  return (
    <ThemeContext value={{ theme: themePreference, setTheme, resolvedTheme }}>
      {children}
    </ThemeContext>
  );
}
