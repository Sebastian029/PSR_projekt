// CheckersGame.AILogic.cs - rozszerzona wersja
namespace App.Server
{
    public partial class CheckersGame
    {
        public (int fromRow, int fromCol, int toRow, int toCol) GetAIMove()
        {
            return checkersAi.CalculateOptimalMove(board, isWhiteTurn);
        }

        // NOWA METODA: Pobierz alternatywny ruch AI z wykluczeniem zablokowanych
        public (int fromRow, int fromCol, int toRow, int toCol) GetAlternativeAIMove(HashSet<(int, int, int, int)> blockedMoves)
        {
            return checkersAi.CalculateOptimalMoveWithExclusions(board, isWhiteTurn, blockedMoves);
        }

        // NOWA METODA: Sprawdź czy ruch byłby zablokowany bez jego wykonania
        public bool WouldMoveBeBlocked(int fromRow, int fromCol, int toRow, int toCol)
        {
            // Symuluj ruch na kopii planszy
            var tempBoard = board.Clone();
            tempBoard.MovePiece(fromRow, fromCol, toRow, toCol);
            string newPosition = SerializeBoardPosition(tempBoard);
            
            return positionCount.ContainsKey(newPosition) && positionCount[newPosition] >= MAX_POSITION_REPEATS;
        }

        // NOWA METODA: Pobierz wszystkie dostępne ruchy z filtrowaniem zablokowanych
        public List<(int fromRow, int fromCol, int toRow, int toCol)> GetValidMovesFiltered()
        {
            var allMoves = new List<(int, int, int, int)>();
            
            // Sprawdź bicia
            var captures = GetAllPossibleCaptures();
            if (captures.Count > 0)
            {
                foreach (var kvp in captures)
                {
                    foreach (var target in kvp.Value)
                    {
                        var move = (kvp.Key.row, kvp.Key.col, target.row, target.col);
                        if (!WouldMoveBeBlocked(move.Item1, move.Item2, move.Item3, move.Item4))
                        {
                            allMoves.Add(move);
                        }
                    }
                }
            }
            else
            {
                // Zwykłe ruchy
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        if (!board.IsDarkSquare(row, col)) continue;
                        
                        PieceType piece = board.GetPiece(row, col);
                        bool isCurrentPlayerPiece = (isWhiteTurn && (piece == PieceType.WhitePawn || piece == PieceType.WhiteKing)) ||
                                                  (!isWhiteTurn && (piece == PieceType.BlackPawn || piece == PieceType.BlackKing));
                        
                        if (isCurrentPlayerPiece)
                        {
                            var moves = board.GetValidMoves(row, col);
                            foreach (var move in moves)
                            {
                                if (!WouldMoveBeBlocked(row, col, move.row, move.col))
                                {
                                    allMoves.Add((row, col, move.row, move.col));
                                }
                            }
                        }
                    }
                }
            }
            
            return allMoves;
        }
    }
}
