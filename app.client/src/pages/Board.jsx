import React, { useState, useEffect } from "react";
import WebSocketClient from "../components/WebSocketClient";

const wsClient = new WebSocketClient("ws://localhost:5162/ws"); 

const GameBoard = () => {
    const [board, setBoard] = useState(Array(8).fill(null).map(() => Array(8).fill(".")));
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

        return () => {
           // wsClient.socket.close();
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
                } else if (square.includes("black")) {
                    newBoard[row][col] = "B";
                } else if (square.includes("white")) {
                    newBoard[row][col] = "W";
                }
            }
        });

        setBoard(newBoard);
    };
    const handleCellClick = (rowIndex, colIndex) => {
        if ((rowIndex + colIndex) % 2 !== 1) {
            return;
        }

        if (selectedPiece) {
            const { row, col } = selectedPiece;
            sendMove(col, row, colIndex, rowIndex);
            setSelectedPiece(null);
        } else if (board[rowIndex][colIndex] === "W" || board[rowIndex][colIndex] === "B") {
            setSelectedPiece({ row: rowIndex, col: colIndex });
        }
    };

    const sendMove = (fromX, fromY, toX, toY) => {
        const move = { fromX, fromY, toX, toY };
        wsClient.sendMove(move);
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
                                
                                {cell !== "." && (
                                    <div style={{
                                        width: 40,
                                        height: 40,
                                        borderRadius: "50%",
                                        backgroundColor: cell === "W" ? "#fff" : "#000",
                                        border: "2px solid #333",
                                        boxShadow: "0 2px 4px rgba(0,0,0,0.2)",
                                        textAlign: "center",
                                        color:"#eee"
                                    }} />
                                    
                                )}
                                {Math.floor(rowIndex) * 4 + Math.floor( colIndex/ 2)}
                                
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
            <button onClick={()=>sendMove(0,1,0,1)}>INIT</button>
        </div>
    );
};

export default GameBoard;