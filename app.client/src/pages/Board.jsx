import React, { useState, useEffect } from "react";
import WebSocketClient from "../components/WebSocketClient";

const wsClient = new WebSocketClient("ws://localhost:5162/ws");

const GameBoard = () => {
    const [board, setBoard] = useState([
        [" ", "W", " ", "W", " ", "W", " ", "W"],
        ["W", " ", "W", " ", "W", " ", "W", " "],
        [" ", "W", " ", "W", " ", "W", " ", "W"],
        [".", " ", ".", " ", ".", " ", ".", " "],
        [" ", ".", " ", ".", " ", ".", " ", "."],
        ["B", " ", "B", " ", "B", " ", "B", " "],
        [" ", "B", " ", "B", " ", "B", " ", "B"],
        ["B", " ", "B", " ", "B", " ", "B", " "],
    ]);
    const [selectedPiece, setSelectedPiece] = useState(null);

    useEffect(() => {
        wsClient.socket.onmessage = (event) => {
            console.log("Server response:", event.data);
        };
    }, []);

    const handleCellClick = (rowIndex, colIndex) => {
        if (selectedPiece) {
            const { row, col } = selectedPiece;
            sendMove(col, row, colIndex, rowIndex);
            setSelectedPiece(null);
        } else if (board[rowIndex][colIndex] === "W" || board[rowIndex][colIndex] === "B") {
            setSelectedPiece({ row: rowIndex, col: colIndex });
        }
    };

    const sendMove = (fromX, fromY, toX, toY) => {
        const move = { fromY, fromX, toY, toX };
        wsClient.sendMove(move);
        console.log("Move sent:", move);
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
                                backgroundColor: selectedPiece?.row === rowIndex && selectedPiece?.col === colIndex ? "lightblue" : (rowIndex + colIndex) % 2 === 1 ? "gray" : "white",
                                color: cell === "W" ? "white" : cell === "B" ? "black" : "",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                fontSize: 24,
                                cursor: cell !== "." ? "pointer" : "default"
                            }}
                            onClick={() => handleCellClick(rowIndex, colIndex)}
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
