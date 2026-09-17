import { describe, it, expect } from 'vitest';
import { requireAuthLoader, anonymousOnlyLoader } from '@/app/routes/loaders';
import { useToken, useIsAuthenticated } from '@/features/auth/hooks/useToken';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { useTheme, ThemeContext } from '@/app/providers/themeContext';

describe('Application Re-export Modules', () => {
  it('exports route auth loaders properly', () => {
    expect(requireAuthLoader).toBeDefined();
    expect(anonymousOnlyLoader).toBeDefined();
  });

  it('exports auth hooks from features/auth/hooks/useToken properly', () => {
    expect(useToken).toBeDefined();
    expect(useIsAuthenticated).toBeDefined();
  });

  it('exports theme provider and hook from app/providers properly', () => {
    expect(ThemeProvider).toBeDefined();
    expect(useTheme).toBeDefined();
    expect(ThemeContext).toBeDefined();
  });
});
