namespace App.Grpc;

public enum PieceType : byte
{
    Empty = 0,       // Puste pole
    WhitePawn = 1,   // Biały pionek
    WhiteKing = 2,   // Biała damka
    BlackPawn = 3,   // Czarny pionek
    BlackKing = 4    // Czarna damka
}
