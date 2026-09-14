import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// Автоматическая очистка DOM после каждого теста
afterEach(() => {
  cleanup();
});
