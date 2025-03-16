import React, { useState } from 'react';
import './Board.css';

const Board = () => {
    const [board, setBoard] = useState(initializeBoard());

    function initializeBoard() {
        const initialBoard = Array(8).fill(null).map(() => Array(8).fill(null));
        // Initialize pieces for player 1 and player 2
        for (let i = 0; i < 3; i++) {
            for (let j = (i % 2); j < 8; j += 2) {
                initialBoard[i][j] = 'P1';
                initialBoard[7 - i][j] = 'P2';
            }
        }
        return initialBoard;
    }

    function renderSquare(row, col) {
        const piece = board[row][col];
        return (
            <div key={`${row}-${col}`} className={`square ${piece ? 'occupied' : ''}`}>
                {piece && <div className={`piece ${piece}`}></div>}
            </div>
        );
    }

    return (
        <div className="board">
            {board.map((row, rowIndex) => (
                <div key={rowIndex} className="row">
                    {row.map((_, colIndex) => renderSquare(rowIndex, colIndex))}
                </div>
            ))}
        </div>
    );
};

export default Board;