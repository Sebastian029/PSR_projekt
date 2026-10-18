using Checkers.Core.Ai;
using Checkers.Core.Model;
using Checkers.Core.Rules;

namespace Checkers.Core.Tests;

public class AiPlayerTests
{
    private readonly AiPlayer _player = new(new LocalSearchExecutor());

    [Theory]
    [InlineData(31, 4, 1)]
    [InlineData(31, 4, 2)]
    [InlineData(31, 4, 3)]
    [InlineData(32, 7, 1)]
    [InlineData(32, 7, 3)]
    [InlineData(33, 11, 2)]
    [InlineData(34, 18, 2)]
    public async Task ChosenMoveHasTheBestMinimaxValueAtEveryDepth(int seed, int plies, int granulation)
    {
        var (board, side) = TestSupport.RandomPosition(seed, plies);

        for (var depth = 1; depth <= 4; depth++)
        {
            var result = await _player.FindBestMoveAsync(board, side, new SearchOptions(depth, granulation));

            Assert.NotNull(result);
            var scores = MoveGenerator.Generate(board, side)
                .Select(move => MinimaxSearcher.PlainMinimax(MoveGenerator.Apply(board, move), side.Opposite(), depth - 1))
                .ToList();
            var best = side == Side.White ? scores.Max() : scores.Min();

            if (!result.WasForced)
            {
                Assert.Equal(best, result.Score);
            }

            var chosen = MoveGenerator.Generate(board, side).IndexOf(result.Move);
            Assert.True(chosen >= 0, "chosen move must be legal");
            Assert.Equal(best, scores[chosen]);
        }
    }

    [Fact]
    public async Task GranulationDoesNotChangeTheDecision()
    {
        var board = Board.CreateInitial();

        var baseline = await _player.FindBestMoveAsync(board, Side.White, new SearchOptions(5, 1));
        var expanded = await _player.FindBestMoveAsync(board, Side.White, new SearchOptions(5, 3));

        Assert.Equal(baseline!.Move, expanded!.Move);
        Assert.Equal(baseline.Score, expanded.Score);
        Assert.True(expanded.Jobs > baseline.Jobs);
    }

    [Fact]
    public async Task TieBreakerVariesTheMoveButNeverTheScore()
    {
        var board = Board.Parse(
            "...b....", "........", "........", "........", "........", "..w.w...", "........", "........");
        var deterministic = await _player.FindBestMoveAsync(board, Side.White, new SearchOptions(1));

        var moves = new HashSet<Move>();
        for (var seed = 0; seed < 30; seed++)
        {
            var player = new AiPlayer(new LocalSearchExecutor(), new Random(seed));
            var result = await player.FindBestMoveAsync(board, Side.White, new SearchOptions(1));
            Assert.Equal(deterministic!.Score, result!.Score);
            moves.Add(result.Move);
        }

        Assert.True(moves.Count > 1, "equally good moves should not always resolve to the same one");
    }

    [Fact]
    public async Task GranulationLargerThanDepthIsClamped()
    {
        var (board, side) = TestSupport.RandomPosition(42, 8);

        var result = await _player.FindBestMoveAsync(board, side, new SearchOptions(2, 9));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ForcedMoveIsReturnedWithoutSearching()
    {
        var board = Board.Parse(
            "........", "........", "........", "........", "...b....", "..w.....", "........", "........");

        var result = await _player.FindBestMoveAsync(board, Side.White, new SearchOptions(6));

        Assert.NotNull(result);
        Assert.True(result.WasForced);
        Assert.Equal(0, result.Jobs);
        Assert.Equal(new Square(3, 4), result.Move.To);
    }

    [Fact]
    public async Task ReturnsNullWhenThereIsNoMove()
    {
        var blocked = Board.Parse(
            "........", "........", "........", "........", "........", "........", ".b......", "w.w.....");

        var result = await _player.FindBestMoveAsync(blocked, Side.Black, new SearchOptions(3));

        Assert.Null(result);
    }

    [Fact]
    public async Task TakesTheWinningLineAtOddAndEvenDepths()
    {
        var board = Board.Parse(
            "........", "........", "........", "..b.....", "........", "....w...", "........", "........");

        foreach (var depth in new[] { 1, 2, 3, 4, 5 })
        {
            var result = await _player.FindBestMoveAsync(board, Side.White, new SearchOptions(depth));
            Assert.NotNull(result);

            var next = MoveGenerator.Apply(board, result.Move);
            var blackReplies = MoveGenerator.Generate(next, Side.Black);
            var losesMan = depth >= 2 && blackReplies.Any(reply => reply.IsCapture);
            Assert.False(losesMan, $"depth {depth}: chose {result.Move} which hangs the man");
        }
    }

    [Fact]
    public async Task InvalidOptionsAreRejected()
    {
        var board = Board.CreateInitial();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _player.FindBestMoveAsync(board, Side.White, new SearchOptions(0)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _player.FindBestMoveAsync(board, Side.White, new SearchOptions(3, 0)));
    }

    [Fact]
    public async Task LocalExecutorMatchesSequentialSearch()
    {
        var jobs = Enumerable.Range(0, 6)
            .Select(i => TestSupport.RandomPosition(100 + i, 6 + i))
            .Select(p => new SearchJob(p.Board, p.SideToMove, 3))
            .ToList();

        var parallel = await new LocalSearchExecutor(4).ExecuteAsync(jobs);

        for (var i = 0; i < jobs.Count; i++)
        {
            Assert.Equal(MinimaxSearcher.Search(jobs[i].Board, jobs[i].SideToMove, 3).Score, parallel[i].Score);
        }
    }
}
