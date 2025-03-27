import React, { useState, useEffect } from "react";
import WebSocketClient from "../components/WebSocketClient";

const wsClient = new WebSocketClient("ws://localhost:5162/ws");

const GameBoard = () => {
    const initialBoard = Array(8).fill(null).map(() => Array(8).fill("."));
    const [board, setBoard] = useState(initialBoard);
    const [selectedPiece, setSelectedPiece] = useState(null);

    useEffect(() => {
        wsClient.socket.onmessage = (event) => {
            try {
                const data = JSON.parse(event.data);
                console.log("Parsed response:", data);

                if (data.Board) {
                    let boardArray;

                    if (typeof data.Board === 'string') {
                        try {
                            boardArray = JSON.parse(data.Board);
                        } catch (error) {
                            console.error("Error parsing board data string:", error);
                            return;
                        }
                    } else if (Array.isArray(data.Board)) {
                        boardArray = data.Board;
                    } else {
                        console.error("Invalid board format:", data.Board);
                        return;
                    }

                    updateBoardFromServer(boardArray);
                }
            } catch (error) {
                console.error("Error parsing message:", error);
            }
        };
    }, []);

    const updateBoardFromServer = (boardState) => {
        const newBoard = Array(8).fill(null).map(() => Array(8).fill("."));
        const playablePositions = [];

        for (let row = 0; row < 8; row++) {
            for (let col = 0; col < 8; col++) {
                if ((row + col) % 2 === 1) {
                    playablePositions.push({ row, col });
                }
            }
        }

        boardState.forEach((square, index) => {
            if (index < playablePositions.length) {
                const { row, col } = playablePositions[index];
                if (square === "empty") {
                    newBoard[row][col] = ".";
                } else if (square === "black") {
                    newBoard[row][col] = "B";
                } else if (square === "white") {
                    newBoard[row][col] = "W";
                } else if (square === "blackKing") {
                    newBoard[row][col] = "BK";  // Czarna damka
                } else if (square === "whiteKing") {
                    newBoard[row][col] = "WK";  // Bia³a damka
                }
            }
        });

        setBoard(newBoard);
    };

    const handleCellClick = (rowIndex, colIndex) => {
        if ((rowIndex + colIndex) % 2 !== 1) return;

        if (selectedPiece) {
            const { row, col } = selectedPiece;
            sendMove(col, row, colIndex, rowIndex);
            setSelectedPiece(null);
        } else if (board[rowIndex][colIndex] !== ".") {
            setSelectedPiece({ row: rowIndex, col: colIndex });
        }
    };

    const sendMove = (fromX, fromY, toX, toY) => {
        const move = { fromX, fromY, toX, toY };
        wsClient.sendMove(move);
    };

    const sendReset = () => {
        wsClient.sendReset();
    };

    return (
        <div className="checkers-game">
            <h2>Checkers Game</h2>
            <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(8, 50px)",
                border: "2px solid #333",
                width: "fit-content"
            }}>
                {board.flatMap((row, rowIndex) =>
                    row.map((cell, colIndex) => {
                        const isPlayable = (rowIndex + colIndex) % 2 === 1;
                        const isSelected = selectedPiece &&
                            selectedPiece.row === rowIndex &&
                            selectedPiece.col === colIndex;

                        let pieceColor = null;
                        if (cell === "W") pieceColor = "#fff";
                        else if (cell === "B") pieceColor = "#000";
                        else if (cell === "WK" || cell === "BK") pieceColor = "#0f0"; // Zielony dla damki

                        return (
                            <div
                                key={`${rowIndex}-${colIndex}`}
                                style={{
                                    width: 50,
                                    height: 50,
                                    backgroundColor: isSelected
                                        ? "#4a90e2"
                                        : isPlayable ? "#666" : "#eee",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    cursor: isPlayable ? "pointer" : "default",
                                    position: "relative",
                                }}
                                onClick={() => handleCellClick(rowIndex, colIndex)}
                            >
                                {pieceColor && (
                                    <div style={{
                                        width: 40,
                                        height: 40,
                                        borderRadius: "50%",
                                        backgroundColor: pieceColor,
                                        border: "2px solid #333",
                                        boxShadow: "0 2px 4px rgba(0,0,0,0.2)"
                                    }} />
                                )}
                            </div>
                        );
                    })
                )}
            </div>
            <div style={{ marginTop: 10 }}>
                {selectedPiece ? (
                    <p>Piece selected! Click on a valid square to move.</p>
                ) : (
                    <p>Select a piece to move</p>
                )}
            </div>
            <button onClick={() => sendMove(0, 1, 0, 1)}>INIT</button>
            <button onClick={() => sendReset()}>Reset Board</button>
        </div>
    );
};

export default GameBoard;
