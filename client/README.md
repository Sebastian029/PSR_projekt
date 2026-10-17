# Checkers web client

React 19 + Vite UI for the checkers game server (see the repository README).

```bash
npm install
npm run dev      # http://localhost:5173
npm run lint
npm run build
```

The client opens one WebSocket per game to `ws://localhost:5162/ws`. Override with `VITE_WS_URL`
(copy `.env.example` to `.env.local`).
