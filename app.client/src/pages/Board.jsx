"use client"

import { useState, useEffect } from "react"
import { useLocation, Link } from "react-router-dom"
import WebSocketClient from "../components/WebSocketClient"

const wsClient = new WebSocketClient("ws://localhost:5162/ws")

const GameBoard = () => {
    const initialBoard = Array(8)
        .fill(null)
        .map(() => Array(8).fill("."))
    const [board, setBoard] = useState(initialBoard)
    const [gameState, setGameState] = useState({
        isWhiteTurn: true,
        gameOver: false,
        winner: null,
        currentPlayer: "human"
    })
    const location = useLocation()
    const { depth, granulation, isPerformanceTest, isPlayerMode } = location.state || {}
    const [selectedPiece, setSelectedPiece] = useState(null)

    // Styles object to keep all styling in one place
    const styles = {
        pageWrapper: {
            width: "100vw",
            height: "100vh",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            backgroundColor: "#f0e9e2",
            position: "relative",
        },
        homeButton: {
            position: "absolute",
            top: "20px",
            left: "20px",
            padding: "10px 15px",
            backgroundColor: "#5D4037",
            color: "white",
            border: "none",
            borderRadius: "4px",
            cursor: "pointer",
            fontWeight: "bold",
            textDecoration: "none",
            display: "flex",
            alignItems: "center",
            gap: "5px",
            boxShadow: "0 2px 4px rgba(0,0,0,0.2)",
            transition: "all 0.2s ease",
            zIndex: 100,
        },
        homeButtonHover: {
            backgroundColor: "#3E2723",
            boxShadow: "0 3px 6px rgba(0,0,0,0.3)",
        },
        container: {
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            padding: "20px",
            fontFamily: "Arial, sans-serif",
            backgroundColor: "#f5f5f5",
            borderRadius: "8px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.1)",
            maxWidth: "500px",
            margin: "0 auto",
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
        },
        title: {
            color: "#333",
            marginBottom: "20px",
            fontSize: "24px",
        },
        board: {
            display: "grid",
            gridTemplateColumns: "repeat(8, 50px)",
            border: "3px solid #5D4037",
            width: "fit-content",
            boxShadow: "0 6px 12px rgba(0, 0, 0, 0.15)",
            backgroundColor: "#8D6E63",
        },
        cell: (isPlayable, isSelected) => ({
            width: 50,
            height: 50,
            backgroundColor: isSelected ? "#4a90e2" : isPlayable ? "#8D6E63" : "#D7CCC8",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            cursor: isPlayable ? "pointer" : "default",
            position: "relative",
            transition: "background-color 0.2s",
        }),
        piece: (pieceType) => {
            const style = {
                width: 40,
                height: 40,
                borderRadius: "50%",
                border: "2px solid #333",
                boxShadow: "0 3px 5px rgba(0,0,0,0.3)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                position: "relative",
            }

            if (pieceType === "W") {
                return {
                    ...style,
                    backgroundColor: "#FFFDE7",
                    border: "2px solid #BFA094",
                }
            } else if (pieceType === "B") {
                return {
                    ...style,
                    backgroundColor: "#3E2723",
                    border: "2px solid #1A120B",
                }
            } else if (pieceType === "WK") {
                return {
                    ...style,
                    backgroundColor: "#FFFDE7",
                    border: "2px solid #BFA094",
                }
            } else if (pieceType === "BK") {
                return {
                    ...style,
                    backgroundColor: "#3E2723",
                    border: "2px solid #1A120B",
                }
            }
            return style
        },
        kingSymbol: (isWhite) => ({
            fontSize: "24px",
            fontWeight: "bold",
            color: isWhite ? "#3E2723" : "#FFFDE7",
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
        }),
        cellNumber: {
            position: "absolute",
            bottom: "2px",
            right: "2px",
            fontSize: "10px",
            color: "rgba(0, 0, 0, 0.5)",
            pointerEvents: "none",
        },
        statusMessage: {
            marginTop: "15px",
            padding: "10px",
            backgroundColor: "#EFEBE9",
            borderRadius: "4px",
            color: "#5D4037",
            fontWeight: "bold",
            width: "100%",
            textAlign: "center",
        },
        buttonContainer: {
            display: "flex",
            gap: "10px",
            marginTop: "15px",
        },
        button: {
            padding: "8px 16px",
            backgroundColor: "#795548",
            color: "white",
            border: "none",
            borderRadius: "4px",
            cursor: "pointer",
            fontWeight: "bold",
            transition: "background-color 0.2s",
        },
    }

    // State for home button hover effect
    const [isHomeButtonHovered, setIsHomeButtonHovered] = useState(false)

    useEffect(() => {
        const handleMessage = (event) => {
            try {
                const data = JSON.parse(event.data)
                console.log("Parsed response:", data)

                // Handle board data
                if (data.Board) {
                    let boardArray

                    if (typeof data.Board === "string") {
                        try {
                            boardArray = JSON.parse(data.Board)
                        } catch (error) {
                            console.error("Error parsing board data string:", error)
                            return
                        }
                    } else if (Array.isArray(data.Board)) {
                        boardArray = data.Board
                    } else {
                        console.error("Invalid board format:", data.Board)
                        return
                    }

                    updateBoardFromServer(boardArray)
                }

                // Update game state
                setGameState({
                    isWhiteTurn: data.IsWhiteTurn !== undefined ? data.IsWhiteTurn : true,
                    gameOver: data.GameOver !== undefined ? data.GameOver : false,
                    winner: data.Winner || null,
                    currentPlayer: data.CurrentPlayer || "human"
                })

                // Clear selection if it's not player's turn
                if (data.CurrentPlayer === "computer" || !data.IsWhiteTurn) {
                    setSelectedPiece(null)
                }

                // Handle error messages
                if (data.error) {
                    console.error("Server error:", data.error)
                }

                // Handle settings confirmation
                if (data.type === "settings_confirmation") {
                    console.log("Server confirmed settings:", data)
                }
            } catch (error) {
                console.error("Error parsing message:", error)
            }
        }

        wsClient.socket.onmessage = handleMessage

        // Send settings if available
        if (depth && granulation) {
            console.log("Sending settings to server:", { depth, granulation, isPlayerMode })
            wsClient.sendSettings({
                depth: Number.parseInt(depth),
                granulation: Number.parseInt(granulation),
                isPerformanceTest: isPerformanceTest !== undefined ? Boolean(isPerformanceTest) : false,
                isPlayerMode: isPlayerMode !== undefined ? Boolean(isPlayerMode) : false,
            })
        }

        return () => {
            wsClient.socket.onmessage = null
        }
    }, [depth, granulation])

    const updateBoardFromServer = (boardState) => {
        const newBoard = Array(8)
            .fill(null)
            .map(() => Array(8).fill("."))

        // Iterate through the 64-element array (8x8 board)
        for (let i = 0; i < boardState.length && i < 64; i++) {
            const row = Math.floor(i / 8)
            const col = i % 8
            const square = boardState[i]

            if (square === "invalid") {
                // Light squares - keep as "."
                newBoard[row][col] = "."
            } else if (square === "empty") {
                newBoard[row][col] = "."
            } else if (square === "black") {
                newBoard[row][col] = "B"
            } else if (square === "white") {
                newBoard[row][col] = "W"
            } else if (square === "blackKing") {
                newBoard[row][col] = "BK"
            } else if (square === "whiteKing") {
                newBoard[row][col] = "WK"
            }
        }

        setBoard(newBoard)
    }

    const handleCellClick = (rowIndex, colIndex) => {
        // Only allow moves on dark squares
        if ((rowIndex + colIndex) % 2 !== 1) return

        // Don't allow moves if game is over or it's computer's turn
        if (gameState.gameOver || gameState.currentPlayer === "computer") return

        if (selectedPiece) {
            const { row, col } = selectedPiece
            sendMove(row, col, rowIndex, colIndex)
            setSelectedPiece(null)
        } else if (board[rowIndex][colIndex] !== ".") {
            // Only allow selecting pieces of the current player
            const piece = board[rowIndex][colIndex]
            const isWhitePiece = piece === "W" || piece === "WK"

            if (gameState.isWhiteTurn === isWhitePiece) {
                setSelectedPiece({ row: rowIndex, col: colIndex })
            }
        }
    }

    const sendMove = (fromRow, fromCol, toRow, toCol) => {
        const move = {
            type: "move",
            FromRow: fromRow,
            FromCol: fromCol,
            ToRow: toRow,
            ToCol: toCol
        }
        console.log("Sending move:", move)
        wsClient.sendMove(move)
    }

    const sendReset = () => {
        wsClient.sendReset()
        setSelectedPiece(null)
    }

    const getStatusMessage = () => {
        if (gameState.gameOver) {
            return `Game Over! ${gameState.winner === "white" ? "White" : "Black"} wins!`
        }

        if (selectedPiece) {
            return "Piece selected! Click on a valid square to move."
        }

        if (gameState.currentPlayer === "computer") {
            return "Computer is thinking..."
        }

        return `${gameState.isWhiteTurn ? "White" : "Black"}'s turn`
    }

    return (
        <div style={styles.pageWrapper}>
            {/* Home Navigation Button */}
            <Link
                to="/"
                style={{
                    ...styles.homeButton,
                    ...(isHomeButtonHovered ? styles.homeButtonHover : {}),
                }}
                onMouseEnter={() => setIsHomeButtonHovered(true)}
                onMouseLeave={() => setIsHomeButtonHovered(false)}
            >
                <span>← Home</span>
            </Link>

            <div style={styles.container}>
                <h2 style={styles.title}>Checkers Game</h2>
                <div style={styles.board}>
                    {board.flatMap((row, rowIndex) =>
                        row.map((cell, colIndex) => {
                            const isPlayable = (rowIndex + colIndex) % 2 === 1
                            const isSelected = selectedPiece && selectedPiece.row === rowIndex && selectedPiece.col === colIndex

                            const cellPosition = Math.floor(rowIndex) * 4 + Math.floor(colIndex / 2)
                            const isKing = cell === "WK" || cell === "BK"

                            return (
                                <div
                                    key={`${rowIndex}-${colIndex}`}
                                    style={styles.cell(isPlayable, isSelected)}
                                    onClick={() => handleCellClick(rowIndex, colIndex)}
                                >
                                    {cell !== "." && (
                                        <div style={styles.piece(cell)}>
                                            {isKing && <span style={styles.kingSymbol(cell === "WK")}>♛</span>}
                                        </div>
                                    )}
                                    <span style={styles.cellNumber}>{isPlayable ? cellPosition : ""}</span>
                                </div>
                            )
                        }),
                    )}
                </div>
                <div style={styles.statusMessage}>
                    {getStatusMessage()}
                </div>
                <div style={styles.buttonContainer}>
                    <button style={styles.button} onClick={() => sendReset()}>
                        Reset Board
                    </button>
                </div>
            </div>
        </div>
    )
}

export default GameBoard
