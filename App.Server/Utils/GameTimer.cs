using System;
using System.Diagnostics;

public class GameTimer
{
    private Stopwatch _stopwatch;
    
    public GameTimer()
    {
        _stopwatch = new Stopwatch();
    }
    
    public void Start()
    {
        _stopwatch.Start();
    }
    
    public void Stop()
    {
        _stopwatch.Stop();
    }
    
    public void Reset()
    {
        _stopwatch.Reset();
    }
    
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    
    public double ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    
    public double ElapsedSeconds => _stopwatch.ElapsedMilliseconds / 1000.0;
}