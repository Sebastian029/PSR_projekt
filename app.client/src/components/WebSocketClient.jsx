class WebSocketClient {
    constructor(url) {
        this.socket = new WebSocket(url);

        // Log when the WebSocket connection is opened
        this.socket.onopen = () => {
            console.log("Connected to WebSocket server");
        };

        // Log messages received from the WebSocket server
        //this.socket.onmessage = (event) => {
        //    console.log("Received:", event.data);
        //};

        //// Handle WebSocket closure
        //this.socket.onclose = (event) => {
        //    console.log("WebSocket disconnected", event);
        //};

        //// Handle WebSocket errors
        //this.socket.onerror = (error) => {
        //    console.error("WebSocket error:", error);
        //};

        //// Log connection status on state change
        //this.socket.onstatechange = () => {
        //    console.log("WebSocket state changed:", this.socket.readyState);
        //};
    }

    // Send a move to the WebSocket server
    sendMove(move) {
        if (this.socket.readyState === WebSocket.OPEN) {
            console.log("Sending move:", JSON.stringify(move));
            this.socket.send(JSON.stringify(move));
        } else {
            console.error("WebSocket is not open. Ready state:", this.socket.readyState);
        }
    }
}

export default WebSocketClient;
