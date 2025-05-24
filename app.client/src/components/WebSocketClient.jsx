class WebSocketClient {
    constructor(url) {
        this.socket = new WebSocket(url)
        this.socket.onopen = () => console.log("WebSocket connected")
        this.socket.onclose = () => console.log("WebSocket disconnected")
        this.socket.onerror = (error) => console.error("WebSocket error:", error)
    }

    sendSettings(settings) {
        const message = {
            type: "settings",
            depth: settings.depth,
            granulation: settings.granulation,
            isPerformanceTest: settings.isPerformanceTest,
            isPlayerMode: settings.isPlayerMode
        }
        console.log("Sending settings:", message)
        this.socket.send(JSON.stringify(message))
    }

    sendMove(move) {
        const moveMessage = {
            type: "move",
            FromRow: move.FromRow,
            FromCol: move.FromCol,
            ToRow: move.ToRow,
            ToCol: move.ToCol
        }
        console.log("Sending move message:", moveMessage)
        this.socket.send(JSON.stringify(moveMessage))
    }

    sendReset() {
        const message = { type: "reset" }
        console.log("Sending reset message")
        this.socket.send(JSON.stringify(message))
    }
}

export default WebSocketClient
