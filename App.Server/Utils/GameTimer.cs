// Utilities/GameTimer.cs
using System.Diagnostics;

public class GameTimer
{
    private Stopwatch _stopwatch = new();

    public void Start() => _stopwatch.Start();
    public void Stop() => _stopwatch.Stop();
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;
}