using System;
using System.Collections.Generic;

namespace App.Server
{
    public partial class CheckersBoard
    {
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
                offsets.Add(isEvenRow ? 4 : 3);   // Down-left
                offsets.Add(isEvenRow ? 5 : 4);   // Down-right

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
            
            return moves;
        }
    }
}