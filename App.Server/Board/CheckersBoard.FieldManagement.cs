using System;

namespace App.Server
{
    public partial class CheckersBoard
    {
        public byte GetField(int index)
        {
            int bitPosition = index * 3;
            int arrayIndex = bitPosition / 32;
            int bitOffset = bitPosition % 32;

            // Check if the 3 bits cross the boundary of the uint array
            if (bitOffset > 29) { // 32 - 3 = 29
                // Handle the case where bits cross the boundary
                uint lowerBits = (board[arrayIndex] >> bitOffset) & 0x7; // Get bits from current uint
            
                // Make sure we don't go out of bounds
                if (arrayIndex + 1 < board.Length) {
                    uint upperBits = (board[arrayIndex + 1] & ((1u << (bitOffset + 3 - 32)) - 1)) << (32 - bitOffset);
                    return (byte)(lowerBits | upperBits);
                }
                return (byte)lowerBits;
            } else {
                // Original code for when bits don't cross the boundary
                uint mask = (uint)(0b111 << bitOffset);
                return (byte)((board[arrayIndex] & mask) >> bitOffset);
            }
        }

        public void SetField(int index, byte value)
        {
            int bitPosition = index * 3;
            int arrayIndex = bitPosition / 32;
            int bitOffset = bitPosition % 32;

            // Check if the 3 bits cross the boundary of the uint array
            if (bitOffset > 29) { // 32 - 3 = 29
                // Handle the case where bits cross the boundary
                int spillOver = bitOffset + 3 - 32;
            
                // Clear and set bits in the current uint
                uint lowerMask = (uint)(0x7 << bitOffset);
                board[arrayIndex] &= ~lowerMask;
                board[arrayIndex] |= (uint)((value & ((1 << (3 - spillOver)) - 1)) << bitOffset);
            
                // Make sure we don't go out of bounds
                if (arrayIndex + 1 < board.Length) {
                    // Clear and set bits in the next uint
                    uint upperMask = (uint)((1 << spillOver) - 1);
                    board[arrayIndex + 1] &= ~upperMask;
                    board[arrayIndex + 1] |= (uint)(value >> (3 - spillOver));
                }
            } else {
                // Original code for when bits don't cross the boundary
                uint mask = (uint)(0b111 << bitOffset);
                board[arrayIndex] &= ~mask;
                board[arrayIndex] |= (uint)(value << bitOffset);
            }
        }
    }
}