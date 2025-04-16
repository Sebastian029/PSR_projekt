class WebSocketClient {
    constructor(url) {
        this.url = url;
        this.connect();
    }

    connect() {
        this.socket = new WebSocket(this.url);

        this.socket.onopen = () => {
            console.log("Connected to WebSocket server");
        };

        this.socket.onmessage = (event) => {
            console.log("Received:", event.data);
        };

        this.socket.onclose = (event) => {
            console.log("WebSocket disconnected", event);
            setTimeout(() => this.connect(), 3000); // Reconnect after 3 seconds
        };

        this.socket.onerror = (error) => {
            console.error("WebSocket error:", error);
        };
    }

    sendMove(move) {
        function getFieldNumber(x, y) {
            return Math.floor(y ) * 4 + Math.floor(x / 2);
        }

        let from = getFieldNumber(move.fromX, move.fromY);
        let to = getFieldNumber(move.toX, move.toY);
        let formattedMove = { from, to, type: "move" };
        
        
        console.log("MOVE:", formattedMove);

        if (this.socket.readyState === WebSocket.OPEN) {
            console.log("Sending move:", JSON.stringify(formattedMove));
            this.socket.send(JSON.stringify(formattedMove));
        } else {
            console.error("WebSocket is not open. Attempting to reconnect...");
            this.connect();
            setTimeout(() => this.sendMove(move), 1000);
        }
    }
    sendReset() {
        let from = -1;
        let to = -1;
        let formattedMove = { from, to, type: "reset" };
        this.socket.send(JSON.stringify(formattedMove))
    }
    sendSettings(settings) {
        if (this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify({
                type: "settings",
                depth: settings.depth,
                granulation: settings.granulation,
                isPerformanceTest: settings.isPerformanceTest !== null ? Boolean(settings.isPerformanceTest) : false,
                isPlayerMode: settings.isPlayerMode !== null ? Boolean(settings.isPlayerMode) : false
            }));
            console.log("Settings sent:", settings);
        } else {
            console.error("WebSocket is not open");
        }
    }

}

export default WebSocketClient;
