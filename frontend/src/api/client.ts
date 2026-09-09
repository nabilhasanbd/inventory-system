import axios from 'axios'

// In dev, '/api' is proxied to the backend by Vite (see vite.config.ts).
// For production builds, set VITE_API_BASE_URL to the backend URL.
const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

export default apiClient
