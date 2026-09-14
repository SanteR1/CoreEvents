import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'json-summary'],
      reportsDirectory: './coverage',
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'src/shared/api/generated/**',
        'src/app/main.tsx',
        'src/shared/lib/test/**',
        'src/**/index.ts',
        'src/**/*.d.ts',
        'src/**/types/**',
        'test/**',
        '**/*.test.{ts,tsx}',
      ],
      thresholds: {
        'src/shared/lib/auth/**': {
          lines: 80,
          functions: 80,
          branches: 75,
          statements: 80,
        },
        'src/shared/api/errors.ts': {
          lines: 80,
          branches: 70,
        },
      },
    },
  },
});
