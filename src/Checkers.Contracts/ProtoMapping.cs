using Checkers.Core.Ai;
using Checkers.Core.Model;
using Google.Protobuf;

namespace Checkers.Contracts;

public static class ProtoMapping
{
    public static SearchRequest ToRequest(SearchJob job, string taskId) => new()
    {
        Board = ByteString.CopyFrom(job.Board.ToWire()),
        WhiteToMove = job.SideToMove == Side.White,
        Depth = job.Depth,
        TaskId = taskId,
    };

    public static SearchJob ToJob(SearchRequest request)
    {
        if (request.Depth is < 0 or > SearchOptions.MaxDepth)
        {
            throw new ArgumentException($"Depth must be between 0 and {SearchOptions.MaxDepth}.");
        }

        var board = Board.FromWire(request.Board.Span);
        return new SearchJob(board, request.WhiteToMove ? Side.White : Side.Black, request.Depth);
    }

    public static SearchResult ToResult(SearchResponse response) =>
        new(response.Score, response.Nodes, response.ComputeMs);
}
