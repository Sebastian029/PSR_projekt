using Checkers.Core.Ai;
using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Tests;

public class SearchTests
{
    [Theory]
    [InlineData(11, 0)]
    [InlineData(12, 6)]
    [InlineData(13, 12)]
    [InlineData(14, 20)]
    [InlineData(15, 30)]
    [InlineData(16, 41)]
    public void AlphaBetaMatchesPlainMinimax(int seed, int plies)
    {
        var (board, side) = TestSupport.RandomPosition(seed, plies);

        for (var depth = 0; depth <= 4; depth++)
        {
            var pruned = MinimaxSearcher.Search(board, side, depth).Score;
            var reference = MinimaxSearcher.PlainMinimax(board, side, depth);

            Assert.True(reference == pruned, $"seed {seed}, plies {plies}, depth {depth}: {pruned} != {reference}");
        }
    }

    [Theory]
    [InlineData(21, 8)]
    [InlineData(22, 14)]
    [InlineData(23, 22)]
    public void ScoreIsSymmetricUnderColourSwap(int seed, int plies)
    {
        var (board, side) = TestSupport.RandomPosition(seed, plies);
        var mirrored = TestSupport.Mirror(board);

        for (var depth = 1; depth <= 5; depth++)
        {
            var original = MinimaxSearcher.Search(board, side, depth).Score;
            var swapped = MinimaxSearcher.Search(mirrored, side.Opposite(), depth).Score;

            Assert.Equal(-original, swapped);
        }
    }

    [Fact]
    public void SideWithoutMovesLosesAndFasterWinsScoreHigher()
    {
        var blocked = Board.Parse(
            "........", "........", "........", "........", "........", "........", ".b......", "w.w.....");

        var whiteWinsNow = MinimaxSearcher.Search(blocked, Side.Black, 3).Score;

        Assert.True(whiteWinsNow > MinimaxSearcher.WinScore);
    }

    [Fact]
    public void AlphaBetaVisitsFewerNodesThanFullSearch()
    {
        var (board, side) = TestSupport.RandomPosition(7, 10);

        var pruned = MinimaxSearcher.Search(board, side, 5);

        long full = 0;
        Count(board, side, 5, ref full);
        Assert.True(pruned.Nodes < full, $"{pruned.Nodes} !< {full}");

        static void Count(Board b, Side s, int depth, ref long counter)
        {
            counter++;
            if (depth == 0)
            {
                return;
            }

            foreach (var move in MoveGenerator.Generate(b, s))
            {
                Count(MoveGenerator.Apply(b, move), s.Opposite(), depth - 1, ref counter);
            }
        }
    }

    [Fact]
    public void SearchHonoursCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => MinimaxSearcher.Search(Board.CreateInitial(), Side.White, 9, cts.Token));
    }
}
