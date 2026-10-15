import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export function resolveApiTarget(value = process.env.TITAN_API_URL): string {
  return value ?? 'http://localhost:5080';
}

const apiTarget = resolveApiTarget();

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
  },
});
