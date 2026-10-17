import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The browser talks to the game server directly over WebSocket (see VITE_WS_URL in .env.example),
// so no dev-server proxy is needed.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
  },
})
