import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import axe from 'axe-core';
import { ThemeProvider } from '@/shared/lib/theme';
import { ThemeToggle } from '../ThemeToggle';

describe('ThemeToggle Component', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
  });

  it('renders all three theme options with labels and titles', () => {
    render(
      <ThemeProvider>
        <ThemeToggle />
      </ThemeProvider>,
    );

    const lightBtn = screen.getByRole('button', { name: /Светлая/i });
    const systemBtn = screen.getByRole('button', { name: /Система/i });
    const darkBtn = screen.getByRole('button', { name: /Тёмная/i });

    expect(lightBtn).toBeInTheDocument();
    expect(lightBtn).toHaveAttribute('title', 'Светлая');

    expect(systemBtn).toBeInTheDocument();
    expect(systemBtn).toHaveAttribute('title', 'Система');

    expect(darkBtn).toBeInTheDocument();
    expect(darkBtn).toHaveAttribute('title', 'Тёмная');
  });

  it('highlights the active button based on current theme preference', () => {
    localStorage.setItem('theme', 'dark');

    render(
      <ThemeProvider>
        <ThemeToggle />
      </ThemeProvider>,
    );

    const darkBtn = screen.getByRole('button', { name: /Тёмная/i });
    const lightBtn = screen.getByRole('button', { name: /Светлая/i });

    expect(darkBtn.className).toContain('shadow-sm');
    expect(lightBtn.className).not.toContain('shadow-sm');
  });

  it('switches theme to light and dark when respective buttons are clicked', async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <ThemeToggle />
      </ThemeProvider>,
    );

    const lightBtn = screen.getByRole('button', { name: /Светлая/i });
    const darkBtn = screen.getByRole('button', { name: /Тёмная/i });
    const systemBtn = screen.getByRole('button', { name: /Система/i });

    // Click Light
    await user.click(lightBtn);
    expect(localStorage.getItem('theme')).toBe('light');
    expect(lightBtn.className).toContain('shadow-sm');
    expect(darkBtn.className).not.toContain('shadow-sm');

    // Click Dark
    await user.click(darkBtn);
    expect(localStorage.getItem('theme')).toBe('dark');
    expect(darkBtn.className).toContain('shadow-sm');
    expect(lightBtn.className).not.toContain('shadow-sm');

    // Click System
    await user.click(systemBtn);
    expect(localStorage.getItem('theme')).toBeNull();
    expect(systemBtn.className).toContain('shadow-sm');
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = render(
      <ThemeProvider>
        <ThemeToggle />
      </ThemeProvider>,
    );

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
