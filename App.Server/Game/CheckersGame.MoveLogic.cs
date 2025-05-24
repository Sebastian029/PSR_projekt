// CheckersGame.MoveLogic.cs
namespace App.Server;

public partial class CheckersGame
{
    // CheckersGame.MoveLogic.cs
// CheckersGame.MoveLogic.cs
public bool PlayMove(int fromRow, int fromCol, int toRow, int toCol)
{
    //Console.WriteLine($"PlayMove called: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
    
    // Walidacja współrzędnych
    if (fromRow < 0 || fromRow >= 8 || fromCol < 0 || fromCol >= 8 ||
        toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
    {
        //Console.WriteLine($"Invalid coordinates: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
        return false;
    }

    // Sprawdź czy pola są ciemne
    if (!IsDarkSquare(fromRow, fromCol) || !IsDarkSquare(toRow, toCol))
    {
        //Console.WriteLine($"Move not on dark squares: from ({fromRow}, {fromCol}) to ({toRow}, {toCol})");
        return false;
    }

    PieceType piece = board.GetPiece(fromRow, fromCol);
    if (piece == PieceType.Empty)
    {
       // Console.WriteLine($"No piece at ({fromRow}, {fromCol})");
        return false;
    }

    // Sprawdź czy gracz próbuje ruszyć swoją figurą
    if (isWhiteTurn && (piece == PieceType.BlackPawn || piece == PieceType.BlackKing))
    {
        //Console.WriteLine("White player trying to move black piece");
        return false;
    }
    if (!isWhiteTurn && (piece == PieceType.WhitePawn || piece == PieceType.WhiteKing))
    {
       // Console.WriteLine("Black player trying to move white piece");
        return false;
    }

    // Sprawdź czy ruch jest po przekątnej (TYLKO dla zwykłych ruchów)
    int rowDiff = Math.Abs(toRow - fromRow);
    int colDiff = Math.Abs(toCol - fromCol);
    
    // POPRAWKA: Sprawdź najpierw czy są wymagane bicia
    var allCaptures = GetAllPossibleCaptures();
    if (allCaptures.Count > 0)
    {
        //Console.WriteLine($"Captures are mandatory. Checking if move ({fromRow},{fromCol}) to ({toRow},{toCol}) is a valid capture.");
        
        if (!allCaptures.ContainsKey((fromRow, fromCol)))
        {
            //Console.WriteLine($"No captures available from ({fromRow},{fromCol})");
            return false;
        }
        
        if (!allCaptures[(fromRow, fromCol)].Contains((toRow, toCol)))
        {
            //Console.WriteLine($"Move to ({toRow},{toCol}) is not a valid capture from ({fromRow},{fromCol})");
            //Console.WriteLine("Available capture targets:");
            foreach (var target in allCaptures[(fromRow, fromCol)])
            {
                //Console.WriteLine($"  ({target.row}, {target.col})");
            }
            return false;
        }
        
        // To jest prawidłowe bicie - wykonaj je
        return ExecuteCapture(fromRow, fromCol, toRow, toCol);
    }

    // Dla zwykłych ruchów - sprawdź czy jest przekątny
    if (rowDiff != colDiff)
    {
        //Console.WriteLine("Regular move must be diagonal");
        return false;
    }

    // Walidacja kierunku TYLKO dla pionków (nie dla królów)
    if (piece == PieceType.WhitePawn && toRow >= fromRow)
    {
        //Console.WriteLine("White pawn cannot move backwards or sideways");
        return false;
    }
    if (piece == PieceType.BlackPawn && toRow <= fromRow)
    {
        //Console.WriteLine("Black pawn cannot move backwards or sideways");
        return false;
    }

    try
    {
        // Zwykły ruch
        // Dla pionków - tylko ruch o jedno pole
        if ((piece == PieceType.WhitePawn || piece == PieceType.BlackPawn) && 
            (rowDiff != 1 || colDiff != 1))
        {
            //Console.WriteLine("Pawn regular move must be one square diagonally");
            return false;
        }

        // Dla królów - sprawdź czy ścieżka jest wolna
        if (piece == PieceType.WhiteKing || piece == PieceType.BlackKing)
        {
            if (!IsPathClear(fromRow, fromCol, toRow, toCol))
            {
                //Console.WriteLine("King path is blocked");
                return false;
            }
        }

        // Sprawdź czy pole docelowe jest puste
        if (board.GetPiece(toRow, toCol) != PieceType.Empty)
        {
            //Console.WriteLine("Target square is not empty");
            return false;
        }

        board.MovePiece(fromRow, fromCol, toRow, toCol);
        isWhiteTurn = !isWhiteTurn;
       // Console.WriteLine($"Move successful, now {(isWhiteTurn ? "White" : "Black")}'s turn");
        return true;
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Error in PlayMove: {ex.Message}");
        //Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return false;
    }
}


// Dodaj metodę pomocniczą do sprawdzania czy ścieżka jest wolna
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


    // Dodaj metodę pomocniczą
    private bool IsDarkSquare(int row, int col)
    {
        return (row + col) % 2 == 1;
    }


    
    public bool PlayAIMove()
    {
        var (fromRow, fromCol, toRow, toCol) = GetAIMove();
        return PlayMove(fromRow, fromCol, toRow, toCol);
    }

    // Metoda pomocnicza do konwersji z indeksu 32-polowego
    private (int row, int col) ConvertFromIndex32(int index32)
    {
        int row = index32 / 4;
        int col = (index32 % 4) * 2 + (row % 2 == 0 ? 1 : 0);
        return (row, col);
    }

    // Metoda pomocnicza do konwersji na indeks 32-polowy
    private int ConvertToIndex32(int row, int col)
    {
        if ((row + col) % 2 == 0) return -1; // Jasne pole - nieużywane
        return (row * 4) + (col / 2);
    }
}
