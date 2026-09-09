import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev proxy: '/api' -> backend. The default `dotnet run` (http profile) serves on port 5231.
// Change the target if your backend runs on a different port.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5231',
        changeOrigin: true,
      },
    },
  },
})
