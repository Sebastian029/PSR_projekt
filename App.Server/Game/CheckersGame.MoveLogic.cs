// CheckersGame.MoveLogic.cs
using System;

namespace App.Server
{
    public partial class CheckersGame
    {
        public bool PlayMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Console.WriteLine($"PlayMove called: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
    
            // NOWE: Sprawdź warunki remisu PRZED próbą ruchu
            if (movesWithoutCapture >= MAX_MOVES_WITHOUT_CAPTURE)
            {
                Console.WriteLine("50 moves without capture - game should end in draw");
                EndGameWithDraw("50 moves without capture");
                return false;
            }

            if (IsDrawByRepetition())
            {
                Console.WriteLine("Draw by position repetition");
                EndGameWithDraw("Position repeated too many times");
                return false;
            }

            // Walidacja współrzędnych
            if (fromRow < 0 || fromRow >= 8 || fromCol < 0 || fromCol >= 8 ||
                toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
            {
                Console.WriteLine($"Invalid coordinates: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
                return false;
            }
            
            // Walidacja współrzędnych
            if (fromRow < 0 || fromRow >= 8 || fromCol < 0 || fromCol >= 8 ||
                toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
            {
                Console.WriteLine($"Invalid coordinates: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
                return false;
            }

            // NOWE: Sprawdź czy to nie jest ruch wsteczny
            if (IsBackwardMove(fromRow, fromCol, toRow, toCol))
            {
                Console.WriteLine("Backward move detected - blocking to prevent loops");
                return false;
            }

            // Sprawdź czy pola są ciemne
            if (!IsDarkSquare(fromRow, fromCol) || !IsDarkSquare(toRow, toCol))
            {
                Console.WriteLine($"Move not on dark squares: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
                return false;
            }

            PieceType piece = board.GetPiece(fromRow, fromCol);
            if (piece == PieceType.Empty)
            {
                Console.WriteLine($"No piece at ({fromRow}, {fromCol})");
                return false;
            }

            // Sprawdź czy gracz próbuje ruszyć swoją figurą
            if (isWhiteTurn && (piece == PieceType.BlackPawn || piece == PieceType.BlackKing))
            {
                Console.WriteLine("White player trying to move black piece");
                return false;
            }
            if (!isWhiteTurn && (piece == PieceType.WhitePawn || piece == PieceType.WhiteKing))
            {
                Console.WriteLine("Black player trying to move white piece");
                return false;
            }

           

            // Sprawdź czy ruch jest po przekątnej
            int rowDiff = Math.Abs(toRow - fromRow);
            int colDiff = Math.Abs(toCol - fromCol);
            
            // Sprawdź najpierw czy są wymagane bicia
            var allCaptures = GetAllPossibleCaptures();
            if (allCaptures.Count > 0)
            {
                Console.WriteLine($"Captures are mandatory. Checking if move ({fromRow},{fromCol}) to ({toRow},{toCol}) is a valid capture.");
                
                if (!allCaptures.ContainsKey((fromRow, fromCol)))
                {
                    Console.WriteLine($"No captures available from ({fromRow},{fromCol})");
                    return false;
                }
                
                if (!allCaptures[(fromRow, fromCol)].Contains((toRow, toCol)))
                {
                    Console.WriteLine($"Move to ({toRow},{toCol}) is not a valid capture from ({fromRow},{fromCol})");
                    return false;
                }
                
                // To jest bicie - resetuj licznik ruchów bez bicia
                movesWithoutCapture = 0;
                bool captureResult = ExecuteCapture(fromRow, fromCol, toRow, toCol);
                
                if (captureResult)
                {
                    RecordBoardPosition();
                }
                
                return captureResult;
            }

            // Dla zwykłych ruchów - sprawdź czy jest przekątny
            if (rowDiff != colDiff)
            {
                Console.WriteLine("Regular move must be diagonal");
                return false;
            }

            try
            {
                // Zwykły ruch
                if ((piece == PieceType.WhitePawn || piece == PieceType.BlackPawn) && 
                    (rowDiff != 1 || colDiff != 1))
                {
                    Console.WriteLine("Pawn regular move must be one square diagonally");
                    return false;
                }

                if (piece == PieceType.WhiteKing || piece == PieceType.BlackKing)
                {
                    if (!IsPathClear(fromRow, fromCol, toRow, toCol))
                    {
                        Console.WriteLine("King path is blocked");
                        return false;
                    }
                }

                if (board.GetPiece(toRow, toCol) != PieceType.Empty)
                {
                    Console.WriteLine("Target square is not empty");
                    return false;
                }

                // NOWE: Sprawdź czy pozycja po ruchu nie będzie powtórzona
                var tempBoard = board.Clone();
                tempBoard.MovePiece(fromRow, fromCol, toRow, toCol);
                string newPosition = SerializeBoardPosition(tempBoard);
                
                if (positionCount.ContainsKey(newPosition) && positionCount[newPosition] >= MAX_POSITION_REPEATS)
                {
                    Console.WriteLine("Position would repeat too many times - blocking move");
                    return false;
                }

                // NOWE: Sprawdź regułę 50 ruchów bez bicia
                movesWithoutCapture++;
                if (movesWithoutCapture >= MAX_MOVES_WITHOUT_CAPTURE)
                {
                    Console.WriteLine("50 moves without capture - game should end in draw");
                    return false;
                }

                board.MovePiece(fromRow, fromCol, toRow, toCol);
                isWhiteTurn = !isWhiteTurn;
                
                // NOWE: Zapisz pozycję
                RecordBoardPosition();
                
                Console.WriteLine($"Move successful, now {(isWhiteTurn ? "White" : "Black")}'s turn");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlayMove: {ex.Message}");
                return false;
            }
        }
        // private void EndGameWithDraw(string reason)
        // {
        //     Console.WriteLine($"Game ended in draw: {reason}");
        //     gameOver = true;
        //     winner = "draw";
        //     drawReason = reason;
        // }
        // NOWE METODY dla detekcji zapętlenia
        private bool IsBackwardMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (boardHistory.Count < 2) return false;
            
            // Sprawdź czy ostatnie 2 ruchy to ten sam ruch tam i z powrotem
            string currentMove = $"{fromRow},{fromCol}->{toRow},{toCol}";
            string reverseMove = $"{toRow},{toCol}->{fromRow},{fromCol}";
            
            // Sprawdź ostatnie ruchy w historii
            for (int i = Math.Max(0, boardHistory.Count - 4); i < boardHistory.Count; i++)
            {
                if (boardHistory[i].Contains(reverseMove))
                {
                    Console.WriteLine($"Detected potential backward move: {currentMove} vs {reverseMove}");
                    return true;
                }
            }
            
            return false;
        }

        private void RecordBoardPosition()
        {
            string position = SerializeBoardPosition(board);
            
            // Dodaj do historii z informacją o ruchu
            string moveInfo = $"Turn:{(isWhiteTurn ? "White" : "Black")},Pos:{position}";
            boardHistory.Add(moveInfo);
            
            // Zlicz powtórzenia pozycji
            if (positionCount.ContainsKey(position))
            {
                positionCount[position]++;
            }
            else
            {
                positionCount[position] = 1;
            }
            
            // Ogranicz historię do ostatnich 20 ruchów
            if (boardHistory.Count > 20)
            {
                string oldestMove = boardHistory[0];
                boardHistory.RemoveAt(0);
                
                // Usuń stare pozycje z licznika
                string oldPosition = oldestMove.Split(',').Last();
                if (positionCount.ContainsKey(oldPosition))
                {
                    positionCount[oldPosition]--;
                    if (positionCount[oldPosition] <= 0)
                    {
                        positionCount.Remove(oldPosition);
                    }
                }
            }
            
            Console.WriteLine($"Position recorded. Repeats: {positionCount.GetValueOrDefault(position, 0)}");
        }

        private string SerializeBoardPosition(CheckersBoard board)
        {
            var positions = new List<string>();
            
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (board.IsDarkSquare(row, col))
                    {
                        PieceType piece = board.GetPiece(row, col);
                        if (piece != PieceType.Empty)
                        {
                            positions.Add($"{row},{col}:{piece}");
                        }
                    }
                }
            }
            
            return string.Join("|", positions);
        }

        // Metoda do resetowania historii (np. po nowej grze)
        public void ResetMoveHistory()
        {
            boardHistory.Clear();
            positionCount.Clear();
            movesWithoutCapture = 0;
        }

        // Reszta metod bez zmian...
        public bool PlayMove(int from, int to)
        {
            var (fromRow, fromCol) = ConvertFromIndex32(from);
            var (toRow, toCol) = ConvertFromIndex32(to);
            
            return PlayMove(fromRow, fromCol, toRow, toCol);
        }

        public bool PlayAIMove()
        {
            var (fromRow, fromCol, toRow, toCol) = GetAIMove();
            return PlayMove(fromRow, fromCol, toRow, toCol);
        }

        private (int row, int col) ConvertFromIndex32(int index32)
        {
            int row = index32 / 4;
            int col = (index32 % 4) * 2 + (row % 2 == 0 ? 1 : 0);
            return (row, col);
        }

        private int ConvertToIndex32(int row, int col)
        {
            if ((row + col) % 2 == 0) return -1;
            return (row * 4) + (col / 2);
        }

        private bool IsPathClear(int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowStep = (toRow - fromRow) > 0 ? 1 : -1;
            int colStep = (toCol - fromCol) > 0 ? 1 : -1;
            
            int currentRow = fromRow + rowStep;
            int currentCol = fromCol + colStep;
            
            while (currentRow != toRow && currentCol != toCol)
            {
                if (board.GetPiece(currentRow, currentCol) != PieceType.Empty)
                {
                    return false;
                }
                currentRow += rowStep;
                currentCol += colStep;
            }
            
            return true;
        }

        private bool IsDarkSquare(int row, int col)
        {
            return (row + col) % 2 == 1;
        }
    }
}
