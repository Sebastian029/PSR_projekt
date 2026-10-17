const DEFAULT_URL = "ws://localhost:5162/ws"

const serverUrl = import.meta.env.VITE_WS_URL || DEFAULT_URL

/**
 * Thin wrapper around a WebSocket to the game server.
 * Messages sent before the connection is open are queued and flushed on open.
 * One instance = one connection = one game on the server.
 */
class WebSocketClient {
    constructor(url = serverUrl) {
        this.queue = []
        this.onMessage = null
        this.onStatus = null
        this.closed = false
        this.socket = new WebSocket(url)
        this.socket.onopen = () => {
            this.onStatus?.("connected")
            this.queue.splice(0).forEach((payload) => this.socket.send(payload))
        }
        this.socket.onclose = () => {
            if (!this.closed) this.onStatus?.("disconnected")
        }
        this.socket.onerror = () => this.onStatus?.("error")
        this.socket.onmessage = (event) => this.onMessage?.(event)
    }

    send(message) {
        const payload = JSON.stringify(message)
        if (this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(payload)
        } else if (this.socket.readyState === WebSocket.CONNECTING) {
            this.queue.push(payload)
        }
    }

    sendSettings({ depth, granulation, isPerformanceTest, isPlayerMode }) {
        this.send({ type: "settings", depth, granulation, isPerformanceTest, isPlayerMode })
    }

    sendMove({ FromRow, FromCol, ToRow, ToCol }) {
        this.send({ type: "move", FromRow, FromCol, ToRow, ToCol })
    }

    sendReset() {
        this.send({ type: "reset" })
    }

    close() {
        this.closed = true
        this.socket.close()
    }
}

export default WebSocketClient
