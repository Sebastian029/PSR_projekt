using System.Globalization;
using Checkers.Server.Configuration;
using Microsoft.Extensions.Options;

namespace Checkers.Server.Distribution;

public sealed record MoveMetrics(
    string GameId,
    string Side,
    int Depth,
    int Granulation,
    int Jobs,
    long Nodes,
    long ElapsedMs,
    int Score,
    bool Forced,
    int ConfiguredWorkers);

public interface IMetricsSink
{
    void Record(MoveMetrics metrics);
}

public sealed class CsvMetricsSink : IMetricsSink
{
    private const string Header = "timestamp_utc,game_id,side,depth,granulation,jobs,nodes,elapsed_ms,score,forced,workers";

    private readonly string? _path;
    private readonly ILogger<CsvMetricsSink> _logger;
    private readonly object _lock = new();
    private bool _headerChecked;

    public CsvMetricsSink(IOptions<ServerOptions> options, ILogger<CsvMetricsSink> logger)
    {
        _path = string.IsNullOrWhiteSpace(options.Value.MetricsCsvPath) ? null : options.Value.MetricsCsvPath;
        _logger = logger;
    }

    public void Record(MoveMetrics m)
    {
        if (_path is null)
        {
            return;
        }

        var row = string.Join(
            ',',
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            m.GameId,
            m.Side,
            m.Depth.ToString(CultureInfo.InvariantCulture),
            m.Granulation.ToString(CultureInfo.InvariantCulture),
            m.Jobs.ToString(CultureInfo.InvariantCulture),
            m.Nodes.ToString(CultureInfo.InvariantCulture),
            m.ElapsedMs.ToString(CultureInfo.InvariantCulture),
            m.Score.ToString(CultureInfo.InvariantCulture),
            m.Forced ? "1" : "0",
            m.ConfiguredWorkers.ToString(CultureInfo.InvariantCulture));

        try
        {
            lock (_lock)
            {
                if (!_headerChecked)
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
                    if (directory is not null)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(_path) || new FileInfo(_path).Length == 0)
                    {
                        File.AppendAllText(_path, Header + Environment.NewLine);
                    }

                    _headerChecked = true;
                }

                File.AppendAllText(_path, row + Environment.NewLine);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not write search metrics to {Path}", _path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not write search metrics to {Path}", _path);
        }
    }
}
