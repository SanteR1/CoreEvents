import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import axe from 'axe-core';
import { RootFallback } from '../RootFallback';

describe('RootFallback Component', () => {
  it('renders loading text and spinner element', () => {
    render(<RootFallback />);

    expect(screen.getByText('Загрузка...')).toBeInTheDocument();
  });

  it('satisfies a11y accessibility standards', async () => {
    const { container } = render(<RootFallback />);

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });

  it('re-exports requireAuthLoader and anonymousOnlyLoader correctly', async () => {
    const { requireAuthLoader, anonymousOnlyLoader } = await import('../loaders');
    expect(typeof requireAuthLoader).toBe('function');
    expect(typeof anonymousOnlyLoader).toBe('function');
  });
});
