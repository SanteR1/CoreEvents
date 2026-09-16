import {
  ThemeContext as BaseThemeContext,
  useTheme as baseUseTheme,
  type ThemePreference,
  type ThemeContextType,
} from '@/shared/lib/theme';

export const ThemeContext = BaseThemeContext;
export const useTheme = baseUseTheme;
export type { ThemePreference, ThemeContextType };
