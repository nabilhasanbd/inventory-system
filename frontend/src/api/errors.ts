import axios from 'axios'

// Extracts a human-readable message from an axios error.
// The backend's exception middleware returns { status, detail }; the [ApiController]
// validation responses return { errors: { field: [msg, ...] } }.
export function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const resp = err.response
    const data = resp?.data
    if (data && typeof data === 'object' && 'detail' in data && data.detail) {
      return String(data.detail)
    }
    if (data && typeof data === 'object' && 'errors' in data && data.errors && typeof data.errors === 'object') {
      const msgs = Object.values(data.errors as Record<string, unknown>).flat() as string[]
      if (msgs.length) return msgs.join('; ')
    }
    if (typeof data === 'string' && data) return data
    if (resp) return `Request failed with status ${resp.status}`
  }
  if (err instanceof Error) return err.message
  return 'An unexpected error occurred'
}
