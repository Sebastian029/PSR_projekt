// CheckersGame.CaptureLogic.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersGame
    {
        
        private bool ExecuteCapture(int fromRow, int fromCol, int toRow, int toCol)
{
    Console.WriteLine($"ExecuteCapture: Attempting capture from ({fromRow},{fromCol}) to ({toRow},{toCol})");
    
    PieceType piece = board.GetPiece(fromRow, fromCol);
    Console.WriteLine($"ExecuteCapture: Piece type is {piece}");
    
    // Sprawdź czy to wielokrotne bicie z GetMultipleCaptures
    var multipleCaptures = board.GetMultipleCaptures(fromRow, fromCol);
    foreach (var sequence in multipleCaptures)
    {
        if (sequence.Count > 0 && sequence.Last() == (toRow, toCol))
        {
            Console.WriteLine($"ExecuteCapture: Found in multiple captures sequence: {string.Join(" → ", sequence.Select(s => $"({s.row},{s.col})"))}");
            
            // POPRAWKA: Wykonaj tylko pierwszy krok sekwencji
            if (sequence.Count > 0)
            {
                var firstStep = sequence[0];
                Console.WriteLine($"ExecuteCapture: Executing first step to ({firstStep.row},{firstStep.col})");
                
                // Sprawdź czy pierwszy krok to pojedyncze bicie
                var validCaptures = board.GetValidCaptures(fromRow, fromCol);
                var targetCapture = validCaptures.FirstOrDefault(c => c.toRow == firstStep.row && c.toCol == firstStep.col);
                
                if (targetCapture != default)
                {
                    // Wykonaj pierwszy krok
                    board.SetPiece(fromRow, fromCol, PieceType.Empty);
                    board.SetPiece(targetCapture.capturedRow, targetCapture.capturedCol, PieceType.Empty);
                    board.SetPiece(firstStep.row, firstStep.col, piece);
                    
                    Console.WriteLine($"ExecuteCapture: Captured piece at ({targetCapture.capturedRow},{targetCapture.capturedCol})");
                    
                    // Sprawdź promocję
                    if (CheckAndPromotePiece(piece, firstStep.row, firstStep.col))
                    {
                        mustCaptureFrom = null;
                        captureSequence.Clear();
                        isWhiteTurn = !isWhiteTurn;
                        return true;
                    }
                    
                    // Ustaw wymagane dalsze bicie
                    mustCaptureFrom = (firstStep.row, firstStep.col);
                    captureSequence.Add((firstStep.row, firstStep.col));
                    Console.WriteLine($"ExecuteCapture: Must continue capturing from ({firstStep.row},{firstStep.col})");
                    return true;
                }
            }
        }
    }
    
    // Sprawdź czy to wielokrotne bicie (ruch o więcej niż 2 pola)
    int rowDiff = Math.Abs(toRow - fromRow);
    int colDiff = Math.Abs(toCol - fromCol);
    
    if (rowDiff > 2 && colDiff > 2 && rowDiff == colDiff)
    {
        Console.WriteLine($"ExecuteCapture: Direct multiple capture detected - {rowDiff} squares");
        return ExecuteMultipleCapture(fromRow, fromCol, toRow, toCol);
    }
    
    // Pojedyncze bicie
    var validSingleCaptures = board.GetValidCaptures(fromRow, fromCol);
    var targetSingleCapture = validSingleCaptures.FirstOrDefault(c => c.toRow == toRow && c.toCol == toCol);
    
    if (targetSingleCapture == default) 
    {
        Console.WriteLine($"ExecuteCapture: No valid single capture from ({fromRow},{fromCol}) to ({toRow},{toCol})");
        Console.WriteLine("Available single captures:");
        foreach (var capture in validSingleCaptures)
        {
            Console.WriteLine($"  ({capture.toRow}, {capture.toCol}) capturing ({capture.capturedRow}, {capture.capturedCol})");
        }
        return false;
    }

    Console.WriteLine($"ExecuteCapture: Single capture - capturing piece at ({targetSingleCapture.capturedRow},{targetSingleCapture.capturedCol})");

    // Wykonaj pojedyncze bicie
    board.SetPiece(fromRow, fromCol, PieceType.Empty);
    board.SetPiece(targetSingleCapture.capturedRow, targetSingleCapture.capturedCol, PieceType.Empty);
    board.SetPiece(toRow, toCol, piece);

    // Sprawdź promocję
    if (CheckAndPromotePiece(piece, toRow, toCol))
    {
        mustCaptureFrom = null;
        captureSequence.Clear();
        isWhiteTurn = !isWhiteTurn;
        return true;
    }

    // Sprawdź dalsze bicia
    var furtherCaptures = board.GetValidCaptures(toRow, toCol);
    if (furtherCaptures.Count > 0)
    {
        mustCaptureFrom = (toRow, toCol);
        captureSequence.Add((toRow, toCol));
        Console.WriteLine($"Further captures available from ({toRow},{toCol})");
        return true;
    }

    // Koniec sekwencji bić
    mustCaptureFrom = null;
    captureSequence.Clear();
    isWhiteTurn = !isWhiteTurn;
    Console.WriteLine("Capture sequence completed");
    return true;
}



        // Nowa metoda dla wielokrotnych bić
        private bool ExecuteMultipleCapture(int fromRow, int fromCol, int toRow, int toCol)
        {
            Console.WriteLine($"ExecuteMultipleCapture: From ({fromRow},{fromCol}) to ({toRow},{toCol})");
            
            PieceType piece = board.GetPiece(fromRow, fromCol);
            
            // Sprawdź czy to wielokrotne bicie przez pionka (sekwencja skoków po 2 pola)
            if (piece == PieceType.WhitePawn || piece == PieceType.BlackPawn)
            {
                Console.WriteLine("ExecuteMultipleCapture: Pawn multiple capture - executing as sequence");
                return ExecutePawnMultipleCapture(fromRow, fromCol, toRow, toCol);
            }
            
            // Dla damek - długie bicie w jednym ruchu
            if (piece != PieceType.WhiteKing && piece != PieceType.BlackKing)
            {
                Console.WriteLine("ExecuteMultipleCapture: Invalid piece type for multiple capture");
                return false;
            }
            
            return ExecuteKingMultipleCapture(fromRow, fromCol, toRow, toCol);
        }

        // Nowa metoda dla wielokrotnych bić pionków
        // Metoda dla wielokrotnych bić pionków
private bool ExecutePawnMultipleCapture(int fromRow, int fromCol, int toRow, int toCol)
{
    Console.WriteLine($"ExecutePawnMultipleCapture: Pawn from ({fromRow},{fromCol}) to ({toRow},{toCol})");
    
    PieceType piece = board.GetPiece(fromRow, fromCol);
    
    // ZMIANA: Zmień nazwę zmiennej lokalnej
    List<(int fromR, int fromC, int toR, int toC, int capturedR, int capturedC)> pawnCaptureSequence = 
        FindPawnCaptureSequence(fromRow, fromCol, toRow, toCol);
    
    if (pawnCaptureSequence.Count == 0)
    {
        Console.WriteLine("ExecutePawnMultipleCapture: No valid capture sequence found");
        return false;
    }
    
    Console.WriteLine($"ExecutePawnMultipleCapture: Found sequence with {pawnCaptureSequence.Count} captures");
    
    // Wykonaj sekwencję bić
    int currentRow = fromRow;
    int currentCol = fromCol;
    PieceType currentPiece = piece;
    
    foreach (var (stepFromR, stepFromC, stepToR, stepToC, capturedR, capturedC) in pawnCaptureSequence)
    {
        Console.WriteLine($"ExecutePawnMultipleCapture: Step from ({stepFromR},{stepFromC}) to ({stepToR},{stepToC}) capturing ({capturedR},{capturedC})");
        
        // Sprawdź czy krok jest prawidłowy
        if (stepFromR != currentRow || stepFromC != currentCol)
        {
            Console.WriteLine($"ExecutePawnMultipleCapture: Invalid step sequence at ({stepFromR},{stepFromC})");
            return false;
        }
        
        // Wykonaj pojedynczy krok bicia
        board.SetPiece(currentRow, currentCol, PieceType.Empty);
        board.SetPiece(capturedR, capturedC, PieceType.Empty);
        board.SetPiece(stepToR, stepToC, currentPiece);
        
        Console.WriteLine($"ExecutePawnMultipleCapture: Captured piece at ({capturedR},{capturedC})");
        
        // Sprawdź promocję po każdym kroku
        if (currentPiece == PieceType.WhitePawn && stepToR == 0)
        {
            board.SetPiece(stepToR, stepToC, PieceType.WhiteKing);
            currentPiece = PieceType.WhiteKing;
            Console.WriteLine($"White pawn promoted to king at ({stepToR},{stepToC}) during multiple capture");
        }
        else if (currentPiece == PieceType.BlackPawn && stepToR == 7)
        {
            board.SetPiece(stepToR, stepToC, PieceType.BlackKing);
            currentPiece = PieceType.BlackKing;
            Console.WriteLine($"Black pawn promoted to king at ({stepToR},{stepToC}) during multiple capture");
        }
        
        currentRow = stepToR;
        currentCol = stepToC;
    }
    
    // Sprawdź czy są dalsze bicia możliwe
    var furtherCaptures = board.GetValidCaptures(currentRow, currentCol);
    if (furtherCaptures.Count > 0)
    {
        mustCaptureFrom = (currentRow, currentCol);
        // POPRAWKA: Używaj pola klasy captureSequence (typu List<(int row, int col)>)
        captureSequence.Add((currentRow, currentCol));
        Console.WriteLine($"ExecutePawnMultipleCapture: Further captures available from ({currentRow},{currentCol})");
        return true;
    }
    
    // Koniec sekwencji bić
    mustCaptureFrom = null;
    captureSequence.Clear();
    isWhiteTurn = !isWhiteTurn;
    Console.WriteLine("ExecutePawnMultipleCapture: Sequence completed");
    return true;
}


        // Metoda do znajdowania sekwencji bić dla pionka
        private List<(int fromR, int fromC, int toR, int toC, int capturedR, int capturedC)> FindPawnCaptureSequence(int startRow, int startCol, int endRow, int endCol)
        {
            var sequence = new List<(int, int, int, int, int, int)>();
            
            PieceType piece = board.GetPiece(startRow, startCol);
            int direction = (piece == PieceType.WhitePawn) ? -1 : 1;
            
            int currentRow = startRow;
            int currentCol = startCol;
            
            // Symuluj planszę dla znajdowania ścieżki
            var tempBoard = board.Clone();
            
            while (currentRow != endRow || currentCol != endCol)
            {
                bool foundCapture = false;
                
                // Sprawdź możliwe bicia w obu kierunkach
                int[] colOffsets = { -1, 1 };
                
                foreach (int colOffset in colOffsets)
                {
                    int capturedRow = currentRow + direction;
                    int capturedCol = currentCol + colOffset;
                    int landingRow = currentRow + (2 * direction);
                    int landingCol = currentCol + (2 * colOffset);
                    
                    // Sprawdź czy to prowadzi w kierunku celu
                    int remainingRowDiff = Math.Abs(endRow - landingRow);
                    int remainingColDiff = Math.Abs(endCol - landingCol);
                    int currentRowDiff = Math.Abs(endRow - currentRow);
                    int currentColDiff = Math.Abs(endCol - currentCol);
                    
                    if (remainingRowDiff + remainingColDiff >= currentRowDiff + currentColDiff)
                        continue; // Ten ruch nie przybliża do celu
                    
                    if (tempBoard.IsValidPosition(capturedRow, capturedCol) && 
                        tempBoard.IsValidPosition(landingRow, landingCol) &&
                        tempBoard.IsDarkSquare(capturedRow, capturedCol) && 
                        tempBoard.IsDarkSquare(landingRow, landingCol))
                    {
                        PieceType capturedPiece = tempBoard.GetPiece(capturedRow, capturedCol);
                        
                        if (capturedPiece != PieceType.Empty && 
                            !IsSameColor(piece, capturedPiece) && 
                            tempBoard.IsEmpty(landingRow, landingCol))
                        {
                            // Znaleziono prawidłowe bicie
                            sequence.Add((currentRow, currentCol, landingRow, landingCol, capturedRow, capturedCol));
                            
                            // Aktualizuj symulowaną planszę
                            tempBoard.SetPiece(currentRow, currentCol, PieceType.Empty);
                            tempBoard.SetPiece(capturedRow, capturedCol, PieceType.Empty);
                            tempBoard.SetPiece(landingRow, landingCol, piece);
                            
                            currentRow = landingRow;
                            currentCol = landingCol;
                            foundCapture = true;
                            break;
                        }
                    }
                }
                
                if (!foundCapture)
                {
                    Console.WriteLine($"FindPawnCaptureSequence: No valid capture found from ({currentRow},{currentCol})");
                    return new List<(int, int, int, int, int, int)>(); // Zwróć pustą listę
                }
            }
            
            return sequence;
        }

        // Metoda dla wielokrotnych bić damek
private bool ExecuteKingMultipleCapture(int fromRow, int fromCol, int toRow, int toCol)
{
    Console.WriteLine($"ExecuteKingMultipleCapture: From ({fromRow},{fromCol}) to ({toRow},{toCol})");
    
    PieceType piece = board.GetPiece(fromRow, fromCol);
    
    // POPRAWKA: Sprawdź czy ruch jest przekątny
    int rowDiff = Math.Abs(toRow - fromRow);
    int colDiff = Math.Abs(toCol - fromCol);
    
    if (rowDiff != colDiff)
    {
        Console.WriteLine($"ExecuteKingMultipleCapture: Move is not diagonal - rowDiff={rowDiff}, colDiff={colDiff}");
        return false;
    }
    
    // Sprawdź ścieżkę i znajdź wszystkie figury do zbicia
    List<(int row, int col)> capturedPieces = new List<(int, int)>();
    
    int rowStep = (toRow - fromRow) > 0 ? 1 : -1;
    int colStep = (toCol - fromCol) > 0 ? 1 : -1;
    
    Console.WriteLine($"ExecuteKingMultipleCapture: Direction steps - row: {rowStep}, col: {colStep}");
    Console.WriteLine($"ExecuteKingMultipleCapture: Expected path length: {rowDiff} squares");
    
    int currentRow = fromRow + rowStep;
    int currentCol = fromCol + colStep;
    int stepCount = 0;
    
    while (currentRow != toRow && currentCol != toCol)
    {
        stepCount++;
        Console.WriteLine($"ExecuteKingMultipleCapture: Step {stepCount} - Checking position ({currentRow},{currentCol})");
        
        if (currentRow < 0 || currentRow >= 8 || currentCol < 0 || currentCol >= 8)
        {
            Console.WriteLine($"ExecuteKingMultipleCapture: Out of bounds at ({currentRow},{currentCol})");
            return false;
        }
        
        if (!board.IsDarkSquare(currentRow, currentCol))
        {
            Console.WriteLine($"ExecuteKingMultipleCapture: Not dark square at ({currentRow},{currentCol})");
            return false;
        }
        
        PieceType currentPiece = board.GetPiece(currentRow, currentCol);
        Console.WriteLine($"ExecuteKingMultipleCapture: Found piece {currentPiece} at ({currentRow},{currentCol})");
        
        if (currentPiece != PieceType.Empty)
        {
            if (IsSameColor(piece, currentPiece))
            {
                Console.WriteLine($"ExecuteKingMultipleCapture: Same color piece at ({currentRow},{currentCol})");
                return false;
            }
            
            capturedPieces.Add((currentRow, currentCol));
            Console.WriteLine($"ExecuteKingMultipleCapture: Will capture {currentPiece} at ({currentRow},{currentCol})");
        }
        
        currentRow += rowStep;
        currentCol += colStep;
        
        // ZABEZPIECZENIE: Zapobiegaj nieskończonej pętli
        if (stepCount > 10)
        {
            Console.WriteLine($"ExecuteKingMultipleCapture: Too many steps, breaking");
            return false;
        }
    }
    
    Console.WriteLine($"ExecuteKingMultipleCapture: Final position reached: ({currentRow},{currentCol}), expected: ({toRow},{toCol})");
    
    // POPRAWKA: Sprawdź czy dotarliśmy do właściwego miejsca
    if (currentRow != toRow || currentCol != toCol)
    {
        Console.WriteLine($"ExecuteKingMultipleCapture: Path doesn't lead to target position");
        return false;
    }
    
    Console.WriteLine($"ExecuteKingMultipleCapture: Target position ({toRow},{toCol}) piece: {board.GetPiece(toRow, toCol)}");
    
    if (board.GetPiece(toRow, toCol) != PieceType.Empty)
    {
        Console.WriteLine($"ExecuteKingMultipleCapture: Target square ({toRow},{toCol}) is not empty");
        return false;
    }
    
    if (capturedPieces.Count == 0)
    {
        Console.WriteLine("ExecuteKingMultipleCapture: No pieces to capture");
        return false;
    }
    
    Console.WriteLine($"ExecuteKingMultipleCapture: Capturing {capturedPieces.Count} pieces");
    
    // Wykonaj wielokrotne bicie
    board.SetPiece(fromRow, fromCol, PieceType.Empty);
    
    foreach (var (capturedRow, capturedCol) in capturedPieces)
    {
        board.SetPiece(capturedRow, capturedCol, PieceType.Empty);
        Console.WriteLine($"ExecuteKingMultipleCapture: Captured piece at ({capturedRow},{capturedCol})");
    }
    
    board.SetPiece(toRow, toCol, piece);
    
    // Sprawdź dalsze bicia
    var furtherCaptures = board.GetValidCaptures(toRow, toCol);
    if (furtherCaptures.Count > 0)
    {
        mustCaptureFrom = (toRow, toCol);
        captureSequence.Add((toRow, toCol));
        Console.WriteLine($"ExecuteKingMultipleCapture: Further captures available from ({toRow},{toCol})");
        return true;
    }
    
    mustCaptureFrom = null;
    captureSequence.Clear();
    isWhiteTurn = !isWhiteTurn;
    Console.WriteLine("ExecuteKingMultipleCapture: Sequence completed");
    return true;
}



        // Metoda pomocnicza do promocji
        private bool CheckAndPromotePiece(PieceType piece, int row, int col)
        {
            if (piece == PieceType.WhitePawn && row == 0)
            {
                board.SetPiece(row, col, PieceType.WhiteKing);
                Console.WriteLine($"White pawn promoted to king at ({row},{col})");
                return true;
            }
            else if (piece == PieceType.BlackPawn && row == 7)
            {
                board.SetPiece(row, col, PieceType.BlackKing);
                Console.WriteLine($"Black pawn promoted to king at ({row},{col})");
                return true;
            }
            return false;
        }

        // Metoda pomocnicza do sprawdzania koloru
        private bool IsSameColor(PieceType piece1, PieceType piece2)
        {
            return ((piece1 == PieceType.WhitePawn || piece1 == PieceType.WhiteKing) &&
                    (piece2 == PieceType.WhitePawn || piece2 == PieceType.WhiteKing)) ||
                   ((piece1 == PieceType.BlackPawn || piece1 == PieceType.BlackKing) &&
                    (piece2 == PieceType.BlackPawn || piece2 == PieceType.BlackKing));
        }

    // Dodaj do GetAllPossibleCaptures w CheckersGame.CaptureLogic.cs
public Dictionary<(int row, int col), List<(int row, int col)>> GetAllPossibleCaptures()
{
    var result = new Dictionary<(int, int), List<(int, int)>>();
    
    Console.WriteLine($"GetAllPossibleCaptures: Checking for {(isWhiteTurn ? "White" : "Black")} player");
    
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
                var captures = board.GetValidCaptures(row, col);
                var multipleCaptures = board.GetMultipleCaptures(row, col);
                
                var allCaptures = new List<(int, int)>();
                allCaptures.AddRange(captures.Select(c => (c.toRow, c.toCol)));
                
                Console.WriteLine($"Single captures from ({row},{col}):");
                foreach (var capture in captures)
                {
                    Console.WriteLine($"  to ({capture.toRow},{capture.toCol}) capturing ({capture.capturedRow},{capture.capturedCol})");
                }
                
                Console.WriteLine($"Multiple capture sequences from ({row},{col}):");
                foreach (var sequence in multipleCaptures)
                {
                    if (sequence.Count > 0)
                    {
                        Console.WriteLine($"  Sequence: {string.Join(" → ", sequence.Select(s => $"({s.row},{s.col})"))}");
                        allCaptures.Add(sequence.Last());
                    }
                }
                
                if (allCaptures.Count > 0)
                {
                    result[(row, col)] = allCaptures.Distinct().ToList();
                    Console.WriteLine($"Total captures from ({row},{col}): {allCaptures.Count}");
                }
            }
        }
    }
    
    return result;
}


    }
}
