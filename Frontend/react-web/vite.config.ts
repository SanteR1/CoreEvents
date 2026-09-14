import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react({ compiler: true }), tailwindcss()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
  },
  build: {
    sourcemap: true,
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules')) {
            if (id.includes('react') || id.includes('react-dom') || id.includes('react-router')) {
              return 'vendor-react';
            }
          }
        },
      },
    },
  },
  resolve: {
    // Vite 8: нативное чтение "paths" из tsconfig.app.json.
    // Единственный источник правды для алиасов — сам tsconfig.
    tsconfigPaths: true,
  },
});
