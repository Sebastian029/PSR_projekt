using App.Server;
using System.Collections.Generic;
using System.Text.Json;

public class CheckersBoard
{
    private uint[] board;

    public CheckersBoard()
    {
        board = new uint[3];
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        for (int i = 0; i < 12; i++)
            SetField(i, (byte)PieceType.BlackPawn);
        for (int i = 12; i < 20; i++)
            SetField(i, (byte)PieceType.Empty);
        for (int i = 20; i < 32; i++)
            SetField(i, (byte)PieceType.WhiteKing);
    }

    public void ResetBoard()
    {
        for (int i = 0; i < 12; i++)
            SetField(i, (byte)PieceType.BlackPawn);
        for (int i = 12; i < 20; i++)
            SetField(i, (byte)PieceType.Empty);
        for (int i = 20; i < 32; i++)
            SetField(i, (byte)PieceType.WhiteKing);
    }

    public byte GetField(int index)
    {
        int bitPosition = index * 3;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b111 << bitOffset);
        return (byte)((board[arrayIndex] & mask) >> bitOffset);
    }

    public void SetField(int index, byte value)
    {
        int bitPosition = index * 3;
        int arrayIndex = bitPosition / 32;
        int bitOffset = bitPosition % 32;

        uint mask = (uint)(0b111 << bitOffset);
        board[arrayIndex] &= ~mask;
        board[arrayIndex] |= (uint)(value << bitOffset);
    }
    
    public void RemovePiece(int index)
    {
        SetField(index, (byte)PieceType.Empty);
    }

   public List<int> GetValidMoves(int index)
{
    List<int> moves = new List<int>();
    byte piece = GetField(index);
    if (piece == (byte)PieceType.Empty) return moves;

    int row = index / 4;
    bool isEvenRow = (row % 2 == 0);

    // For pawns (white and black)
    if (piece == (byte)PieceType.WhitePawn || piece == (byte)PieceType.BlackPawn)
    {
        List<int> offsets = new List<int>();
        if (piece == (byte)PieceType.WhitePawn)
        {
            // White pawns can only move up
            offsets.Add(isEvenRow ? -4 : -5); // Up-left
            offsets.Add(isEvenRow ? -3 : -4); // Up-right
        }
        else // BlackPawn
        {
            // Black pawns can only move down
            offsets.Add(isEvenRow ? 4 : 3);  // Down-left
            offsets.Add(isEvenRow ? 5 : 4);  // Down-right
        }

        foreach (int offset in offsets)
        {
            int targetIndex = index + offset;
            if (targetIndex >= 0 && targetIndex < 32 && GetField(targetIndex) == (byte)PieceType.Empty)
            {
                int targetRow = targetIndex / 4;
                if (Math.Abs(targetRow - row) == 1)
                {
                    // Check if the move doesn't go off the board (for edge columns)
                    int col = index % 4;
                    if ((col == 0 && offset == -5) || (col == 3 && offset == -3)) continue;
                    if ((col == 0 && offset == 3) || (col == 3 && offset == 5)) continue;

                    moves.Add(targetIndex);
                }
            }
        }
    }
     else // For kings
    {
        List<int> offsets = new List<int>();
        
        offsets.Add(isEvenRow ? -4 : -5); // Up-left
        offsets.Add(isEvenRow ? -3 : -4); // Up-right
        offsets.Add(isEvenRow ? 4 : 3);  // Down-left
        offsets.Add(isEvenRow ? 5 : 4);  // Down-right


        foreach (int offset in offsets)
        {
            if ((isEvenRow & offset == -4) || (!isEvenRow & offset == -5)) 
            {
                int tmp = index;
                while (tmp + offset >= 0)
                {
                    if(GetField(tmp + offset) != (byte)PieceType.Empty)
                    {
                        break;
                    }
                    bool parity = (tmp / 4) % 2 == 0 ? true : false;
                    if (parity & Math.Abs(tmp % 4 - (tmp - 4) % 4) <= 1)
                    {
                        moves.Add(tmp - 4);
                        tmp -= 4;
                    }
                    else if (!parity & Math.Abs(tmp % 4 - (tmp - 5) % 4) <= 1)
                    {
                        moves.Add(tmp - 5);
                        tmp -= 5;
                    }
                    else break;


                }
            }

            if ((isEvenRow & offset == -3) || (!isEvenRow & offset == -4)) 
            {
                int tmp = index;
                while (tmp + offset >= 0)
                {
                    if(GetField(tmp + offset) != (byte)PieceType.Empty)
                    {
                        break;
                    }
                    bool parity = (tmp / 4) % 2 == 0 ? true : false;
                    if (parity & Math.Abs(tmp % 4 - (tmp - 3) % 4) <= 1)
                    {
                        moves.Add(tmp - 3);
                        tmp -= 3;
                    }
                    else if (!parity & Math.Abs(tmp % 4 - (tmp - 4) % 4) <= 1)
                    {
                        moves.Add(tmp - 4);
                        tmp -= 4;

                    }
                    else break;


                }
            }

            if ((isEvenRow & offset == 4) || (!isEvenRow & offset == 3)) 
            {
                int tmp = index;
                while (tmp + offset < 32) 
                {
                    if(GetField(tmp + offset) != (byte)PieceType.Empty)
                    {
                        break;
                    }
                    bool parity = (tmp / 4) % 2 == 0 ? true : false;
                    if (parity & Math.Abs(tmp % 4 - (tmp + 4) % 4) <= 1)
                    {
                        moves.Add(tmp + 4);
                        tmp += 4;
                    }
                    else if (!parity & Math.Abs(tmp % 4 - (tmp + 3) % 4) <= 1)
                    {
                        moves.Add(tmp + 3);
                        tmp += 3;
                    }
                    else break;
                }
            }

            if ((isEvenRow & offset == 5) || (!isEvenRow & offset == 4)) 
            {
                int tmp = index;
                while (tmp + offset < 32) 
                {
                    if(GetField(tmp + offset) != (byte)PieceType.Empty)
                    {
                        break;
                    }
                    bool parity = (tmp / 4) % 2 == 0 ? true : false;
                    if (parity & Math.Abs(tmp % 4 - (tmp + 5) % 4) <= 1)
                    {
                        moves.Add(tmp + 5);
                        tmp += 5;
                    }
                    else if (!parity & Math.Abs(tmp % 4 - (tmp + 4) % 4) <= 1)
                    {
                        moves.Add(tmp + 4);
                        tmp += 4;
                    }
                    else break;
                }




            }
        }

    }
    
    foreach (int move in moves)
    {
    //    Console.WriteLine(move);
    }

    return moves;
}

    public List<(int, int)> GetValidCaptures(int index)
    {
        List<(int, int)> captures = new List<(int, int)>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return captures;

        int row = index / 4;
        bool isEvenRow = (row % 2 == 0);

        // Dla wszystkich pionków (białych, czarnych i dam)
        int[] captureDirections = isEvenRow
            ? new int[] { -7, -9, 7, 9 }
            : new int[] { -9, -7, 9, 7 };
        if (GetField(index) == (byte)PieceType.BlackPawn || index == (byte)PieceType.WhitePawn)
        {

            foreach (var dir in captureDirections)
            {
                int targetIndex = index + dir;
                if (targetIndex >= 0 && targetIndex < 32)
                {
                    // Sprawdź czy cel jest pusty
                    if (GetField(targetIndex) == (byte)PieceType.Empty)
                    {
                        int middleIndex = GetMiddleIndex(index, targetIndex);
                        if (middleIndex >= 0 && middleIndex < 32)
                        {
                            byte capturedPiece = GetField(middleIndex);
                            if (capturedPiece != (byte)PieceType.Empty && !IsSameColor(piece, capturedPiece))
                            {
                                // Sprawdź czy ruch nie wychodzi poza planszę
                                int col = index % 4;
                                if ((col == 0 && (dir == -9 || dir == 7)) || (col == 3 && (dir == -7 || dir == 9)))
                                    continue;

                                // Usunięto ograniczenie kierunku dla pionków - mogą bić w obie strony
                                captures.Add((targetIndex, middleIndex));
                            }
                        }
                    }
                }
            }
        }
       else // For kings
{
    // Define the four diagonal directions based on row parity
    int[] upLeftOffset = isEvenRow ? new int[] { -4 } : new int[] { -5 };
    int[] upRightOffset = isEvenRow ? new int[] { -3 } : new int[] { -4 };
    int[] downLeftOffset = isEvenRow ? new int[] { 4 } : new int[] { 3 };
    int[] downRightOffset = isEvenRow ? new int[] { 5 } : new int[] { 4 };
    
    // Pairs of direction and corresponding offset
    var directions = new[] 
    {
        (Direction: "UpLeft", Offsets: upLeftOffset, NextOffsets: isEvenRow ? -5 : -4),
        (Direction: "UpRight", Offsets: upRightOffset, NextOffsets: isEvenRow ? -4 : -3),
        (Direction: "DownLeft", Offsets: downLeftOffset, NextOffsets: isEvenRow ? 3 : 4),
        (Direction: "DownRight", Offsets: downRightOffset, NextOffsets: isEvenRow ? 4 : 5)
    };

    List<int> moves = new List<int>();

    // Process each direction
    foreach (var direction in directions)
    {
        int offset = direction.Offsets[0];
        int nextOffset = direction.NextOffsets;
        int currentIndex = index;
        int found = -1;

        // Continue in this direction until we hit a boundary or a piece
        while (true)
        {
            // Calculate row parity for the current position
            bool currentParity = (currentIndex / 4) % 2 == 0;
            int nextIndex;
            
            // Determine next index based on parity and direction
            if (direction.Direction == "UpLeft")
            {
                if (currentParity && Math.Abs(currentIndex % 4 - (currentIndex - 4) % 4) <= 1)
                {
                    nextIndex = currentIndex - 4;
                    nextOffset = -5;
                }
                else if (!currentParity && Math.Abs(currentIndex % 4 - (currentIndex - 5) % 4) <= 1)
                {
                    nextIndex = currentIndex - 5;
                    nextOffset = -4;
                }
                else break;
            }
            else if (direction.Direction == "UpRight")
            {
                if (currentParity && Math.Abs(currentIndex % 4 - (currentIndex - 3) % 4) <= 1)
                {
                    nextIndex = currentIndex - 3;
                    nextOffset = -4;
                }
                else if (!currentParity && Math.Abs(currentIndex % 4 - (currentIndex - 4) % 4) <= 1)
                {
                    nextIndex = currentIndex - 4;
                    nextOffset = -3;
                }
                else break;
            }
            else if (direction.Direction == "DownLeft")

            {
                if (currentParity && Math.Abs(currentIndex % 4 - (currentIndex + 4) % 4) <= 1)
                {
                    nextIndex = currentIndex + 4;
                    nextOffset = 3;
                }
                else if (!currentParity && Math.Abs(currentIndex % 4 - (currentIndex + 3) % 4) <= 1)
                {
                    nextIndex = currentIndex + 3;
                    nextOffset = 4;
                }
                else break;
            }
            else // DownRight
            {
                if (currentParity && Math.Abs(currentIndex % 4 - (currentIndex + 5) % 4) <= 1)
                {
                    nextIndex = currentIndex + 5;
                    nextOffset = 4;
                }
                else if (!currentParity && Math.Abs(currentIndex % 4 - (currentIndex + 4) % 4) <= 1)
                {
                    nextIndex = currentIndex + 4;
                    nextOffset = 5;
                }
                else break;
            }
            
            // Check bounds
            if (nextIndex < 0 || nextIndex >= 32)
                break;
                
            // Check if square is occupied
            byte pieceAtNextIndex = GetField(nextIndex);
            
            if (pieceAtNextIndex == (byte)PieceType.Empty)
            {
                // Empty square - add to normal moves
                moves.Add(nextIndex);
                currentIndex = nextIndex;
                continue;
            }
            
            // Not empty - check for capture opportunity
            if (!IsSameColor(piece, pieceAtNextIndex) && found < 0)
            {
                // Calculate the landing square after the capture
                bool nextParity = (nextIndex / 4) % 2 == 0;
                int captureIndex = -1;
                
                if (direction.Direction == "UpLeft")
                {
                    captureIndex = nextParity ? nextIndex - 4 : nextIndex - 5;
                }
                else if (direction.Direction == "UpRight")
                {
                    captureIndex = nextParity ? nextIndex - 3 : nextIndex - 4;
                }
                else if (direction.Direction == "DownLeft")
                {
                    captureIndex = nextParity ? nextIndex + 4 : nextIndex + 3;
                }
                else // DownRight
                {
                    captureIndex = nextParity ? nextIndex + 5 : nextIndex + 4;
                }
                
                // Make sure the capture landing spot is valid
                if (captureIndex >= 0 && captureIndex < 32 && GetField(captureIndex) == (byte)PieceType.Empty)
                {
                    // Check if the landing square is in the correct direction (column check)
                    int nextCol = nextIndex % 4;
                    int captureCol = captureIndex % 4;
                    
                    if (Math.Abs(nextCol - captureCol) <= 1)
                    {
                        found = captureIndex;
                        Console.WriteLine("EMPTY " + captureIndex);
                        Console.WriteLine("PIECE " + nextIndex);
                        Console.WriteLine("BY " + index);
                        Console.WriteLine("----");
                        captures.Add((captureIndex, nextIndex)); // Add capture: (target, captured piece)
                    }
                }
            }
            
            // We found a piece, so stop exploring this direction
            break;
        }
    }
}

        return captures;
    }

    public List<int> GetMultipleCaptures(int index, List<int> previousCaptures = null)
    {
        List<int> multipleCaptures = new List<int>();
        byte piece = GetField(index);
        if (piece == (byte)PieceType.Empty) return multipleCaptures;

        previousCaptures = previousCaptures ?? new List<int>();

        var initialCaptures = GetValidCaptures(index);

        foreach (var (capture, pieceIndex) in initialCaptures)
        {
            // Symuluj ruch
            byte originalPiece = GetField(index);
            SetField(index, (byte)PieceType.Empty);
            byte capturedPiece = GetField(pieceIndex);
            SetField(pieceIndex, (byte)PieceType.Empty);
            SetField(capture, originalPiece);

            // Sprawdź czy pionek powinien zostać damką
            byte currentPiece = originalPiece;
            if (originalPiece == (byte)PieceType.WhitePawn && capture < 4)
            {
                SetField(capture, (byte)PieceType.WhiteKing);
                currentPiece = (byte)PieceType.WhiteKing;
            }
            else if (originalPiece == (byte)PieceType.BlackPawn && capture >= 28)
            {
                SetField(capture, (byte)PieceType.BlackKing);
                currentPiece = (byte)PieceType.BlackKing;
            }

            // Sprawdź kolejne bicia
            var furtherCaptures = GetValidCaptures(capture);
            if (furtherCaptures.Count > 0)
            {
                foreach (var (furtherCapture, tmp) in furtherCaptures)
                {
                    List<int> sequence = new List<int>(previousCaptures) { capture, furtherCapture };
                    multipleCaptures.AddRange(sequence);

                    // Rekurencyjnie sprawdź dalsze bicia
                    var deeperCaptures = GetMultipleCaptures(furtherCapture, sequence);
                    multipleCaptures.AddRange(deeperCaptures);
                }
            }
            else
            {
                multipleCaptures.Add(capture);
            }

            // Cofnij symulację
            SetField(capture, (byte)PieceType.Empty);
            SetField(pieceIndex, capturedPiece);
            SetField(index, originalPiece);
        }

        return multipleCaptures.Distinct().ToList();
    }

    public void MovePiece(int from, int to)
    {
        byte piece = GetField(from);
        SetField(from, (byte)PieceType.Empty);
        SetField(to, piece);

        // Sprawdź czy wykonano bicie
        if (Math.Abs(to - from) > 5) // Bicie (przeskok o więcej niż 5 pól)
        {
            int middleIndex = GetMiddleIndex(from, to);

            SetField(middleIndex, (byte)PieceType.Empty);
        }

        // Awans na damkę
        if (piece == (byte)PieceType.WhitePawn && to < 4)
        {
            SetField(to, (byte)PieceType.WhiteKing);
        }
        else if (piece == (byte)PieceType.BlackPawn && to >= 28)
        {
            SetField(to, (byte)PieceType.BlackKing);
        }
    }

    private bool IsSameColor(byte piece1, byte piece2)
    {
        return ((piece1 == (byte)PieceType.WhitePawn || piece1 == (byte)PieceType.WhiteKing) &&
                (piece2 == (byte)PieceType.WhitePawn || piece2 == (byte)PieceType.WhiteKing)) ||
               ((piece1 == (byte)PieceType.BlackPawn || piece1 == (byte)PieceType.BlackKing) &&
                (piece2 == (byte)PieceType.BlackPawn || piece2 == (byte)PieceType.BlackKing));
    }

    public int GetMiddleIndex(int from, int to)
    {
        int row = from / 4;
        int check = row % 2;

        int middleIndex = check == 0
            ? (int)Math.Floor((double)(to + from + 1) / 2)
            : (int)Math.Floor((double)(to + from) / 2);

        return middleIndex;
    }

    public string SerializeBoard()
    {
        var boardState = new object[32];

        for (int i = 0; i < 32; i++)
        {
            PieceType pieceType = (PieceType)GetField(i);

            switch (pieceType)
            {
                case PieceType.Empty:
                    boardState[i] = "empty";
                    break;
                case PieceType.WhitePawn:
                    boardState[i] = "white";
                    break;
                case PieceType.WhiteKing:
                    boardState[i] = "whiteKing";
                    break;
                case PieceType.BlackPawn:
                    boardState[i] = "black";
                    break;
                case PieceType.BlackKing:
                    boardState[i] = "blackKing";
                    break;
                default:
                    boardState[i] = "empty";
                    break;
            }
        }

        return JsonSerializer.Serialize(boardState);
    }

    
    public CheckersBoard Clone()
    {
        CheckersBoard clonedBoard = new CheckersBoard();
    
        clonedBoard.board = new uint[3];
        for (int i = 0; i < board.Length; i++)
        {
            clonedBoard.board[i] = board[i];
        }

        return clonedBoard;
    }

    public void PrintBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 0)
                {
                    Console.Write(" . ");
                }
                else
                {
                    int index32 = (row / 2) * 4 + (col / 2);
                    byte piece = GetField(index32);
                    char symbol = piece == (byte)PieceType.WhitePawn ? 'w' :
                                  piece == (byte)PieceType.BlackPawn ? 'b' :
                                  piece == (byte)PieceType.WhiteKing ? 'W' :
                                  piece == (byte)PieceType.BlackKing ? 'B' : '-';
                    Console.Write($" {symbol} ");
                }
            }
            Console.WriteLine();
        }
    }

}


