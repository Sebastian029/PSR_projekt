import React, { useState, useEffect } from "react";
import WebSocketClient from "../components/WebSocketClient"

const wsClient = new WebSocketClient("ws://localhost:5000/ws");

const GameBoard = () => {
    const [board, setBoard] = useState([
        // Initialize an 8x8 empty board
        [" ", "W", " ", "W", " ", "W", " ", "W"],
        ["W", " ", "W", " ", "W", " ", "W", " "],
        [" ", "W", " ", "W", " ", "W", " ", "W"],
        [".", " ", ".", " ", ".", " ", ".", " "],
        [" ", ".", " ", ".", " ", ".", " ", "."],
        ["B", " ", "B", " ", "B", " ", "B", " "],
        [" ", "B", " ", "B", " ", "B", " ", "B"],
        ["B", " ", "B", " ", "B", " ", "B", " "],
    ]);

    useEffect(() => {
        wsClient.socket.onmessage = (event) => {
            console.log("Server response:", event.data);
        };
    }, []);

    const handleMove = (fromX, fromY, toX, toY) => {
        const move = { fromX, fromY, toX, toY };
        wsClient.sendMove(move);
    };

    return (
        <div>
            <h2>Checkers Game</h2>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(8, 50px)" }}>
                {board.flatMap((row, rowIndex) =>
                    row.map((cell, colIndex) => (
                        <div
                            key={`${rowIndex}-${colIndex}`}
                            style={{
                                width: 50,
                                height: 50,
                                backgroundColor: (rowIndex + colIndex) % 2 === 1 ? "gray ": "white",
                                color: cell === "W" ? "white" : cell === "B" ? "black" : "",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                fontSize: 24,
                                cursor: cell !== "." ? "pointer" : "default",
                            }}
                            onClick={() => handleMove(rowIndex, colIndex, rowIndex + 1, colIndex + 1)}
                        >
                            {cell}
                        </div>
                    ))
                )}
            </div>
        </div>
    );
};

export default GameBoard;
