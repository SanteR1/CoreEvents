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
    env: {
      VITE_USERS_API_URL: 'http://localhost:5003',
      VITE_EVENTS_API_URL: 'http://localhost:5004',
      VITE_BOOKINGS_API_URL: 'http://localhost:5005',
    },
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
        'src/shared/lib/auth/user.ts',
        'test/**',
        '**/*.test.{ts,tsx}',
      ],
      thresholds: {
        lines: 98,
        functions: 98,
        branches: 95,
        statements: 98,
      },
    },
  },
});
