import react from '@vitejs/plugin-react';
// import babel from "@rolldown/plugin-babel";
import { defineConfig } from 'vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react({ compiler: true }),
    // babel({ presets: [reactCompilerPreset()] })
  ],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
  },
  build: {
    sourcemap: true,
  },
  resolve: {
    // Vite 8: нативное чтение "paths" из tsconfig.app.json.
    // Единственный источник правды для алиасов — сам tsconfig.
    tsconfigPaths: true,
  },
});
