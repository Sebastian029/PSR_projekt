using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public List<(int, int)> GetValidCaptures(int index)
        {
            List<(int, int)> captures = new List<(int, int)>();
            byte piece = GetField(index);
            if (piece == (byte)PieceType.Empty) return captures;

            int row = index / 4;
            bool isEvenRow = (row % 2 == 0);

            // For all pieces (white, black and kings)
            int[] captureDirections = isEvenRow
                ? new int[] { -7, -9, 7, 9 }
                : new int[] { -9, -7, 9, 7 };
                
            if (piece == (byte)PieceType.BlackPawn || piece == (byte)PieceType.WhitePawn)
            {
                foreach (var dir in captureDirections)
                {
                    int targetIndex = index + dir;
                    if (targetIndex >= 0 && targetIndex < 32)
                    {
                        // Check if target is empty
                        if (GetField(targetIndex) == (byte)PieceType.Empty)
                        {
                            int middleIndex = GetMiddleIndex(index, targetIndex);
                            if (middleIndex >= 0 && middleIndex < 32)
                            {
                                byte capturedPiece = GetField(middleIndex);
                                if (capturedPiece != (byte)PieceType.Empty && !IsSameColor(piece, capturedPiece))
                                {
                                    // Check if the move stays on the board
                                    int col = index % 4;
                                    if ((col == 0 && (dir == -9 || dir == 7)) || (col == 3 && (dir == -7 || dir == 9)))
                                        continue;

                                    // Pawns can capture in both directions
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
                // Simulate move
                byte originalPiece = GetField(index);
                SetField(index, (byte)PieceType.Empty);
                byte capturedPiece = GetField(pieceIndex);
                SetField(pieceIndex, (byte)PieceType.Empty);
                SetField(capture, originalPiece);

                // Check if piece should be promoted to king
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

                // Check for further captures
                var furtherCaptures = GetValidCaptures(capture);
                if (furtherCaptures.Count > 0)
                {
                    foreach (var (furtherCapture, tmp) in furtherCaptures)
                    {
                        List<int> sequence = new List<int>(previousCaptures) { capture, furtherCapture };
                        multipleCaptures.AddRange(sequence);

                        // Recursively check for deeper captures
                        var deeperCaptures = GetMultipleCaptures(furtherCapture, sequence);
                        multipleCaptures.AddRange(deeperCaptures);
                    }
                }
                else
                {
                    multipleCaptures.Add(capture);
                }

                // Undo simulation
                SetField(capture, (byte)PieceType.Empty);
                SetField(pieceIndex, capturedPiece);
                SetField(index, originalPiece);
            }

            return multipleCaptures.Distinct().ToList();
        }
    }
}