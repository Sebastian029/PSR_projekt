using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Client
{
    public class ServerPerformanceTracker
    {
        private class ServerMetrics
        {
            public int ActiveRequests { get; set; }
            public double AverageResponseTime { get; set; }
            public int TotalRequests { get; set; }
            public DateTime LastRequestTime { get; set; }
            public bool IsAvailable { get; set; } = true;
        }

        private readonly Dictionary<string, ServerMetrics> _serverMetrics;
        private readonly object _lockObject = new object();
        private const int MAX_ACTIVE_REQUESTS = 2;
        private const double RESPONSE_TIME_THRESHOLD = 60000; 

        public ServerPerformanceTracker()
        {
            _serverMetrics = new Dictionary<string, ServerMetrics>();
        }

        public void RegisterServer(string address)
        {
            lock (_lockObject)
            {
                if (!_serverMetrics.ContainsKey(address))
                {
                    _serverMetrics[address] = new ServerMetrics();
                }
            }
        }

        public void UpdateMetrics(string address, double responseTime)
        {
            lock (_lockObject)
            {
                if (_serverMetrics.TryGetValue(address, out var metrics))
                {
                    metrics.TotalRequests++;
                    metrics.AverageResponseTime = (metrics.AverageResponseTime * (metrics.TotalRequests - 1) + responseTime) / metrics.TotalRequests;
                    metrics.LastRequestTime = DateTime.Now;
                    metrics.ActiveRequests--;
                    
                    if (metrics.AverageResponseTime > RESPONSE_TIME_THRESHOLD)
                    {
                        metrics.IsAvailable = false;
                    }
                }
            }
        }

        public void StartRequest(string address)
        {
            lock (_lockObject)
            {
                if (_serverMetrics.TryGetValue(address, out var metrics))
                {
                    metrics.ActiveRequests++;
                }
            }
        }

        public string GetBestServer()
        {
            lock (_lockObject)
            {
                var availableServers = _serverMetrics
                    .Where(s => s.Value.IsAvailable && s.Value.ActiveRequests < MAX_ACTIVE_REQUESTS)
                    .OrderBy(s => s.Value.ActiveRequests)
                    .ThenBy(s => s.Value.AverageResponseTime)
                    .ToList();

                if (!availableServers.Any())
                {
                    var notOverloadedServers = _serverMetrics
                        .Where(s => s.Value.ActiveRequests < MAX_ACTIVE_REQUESTS)
                        .OrderBy(s => s.Value.ActiveRequests)
                        .ThenBy(s => s.Value.AverageResponseTime)
                        .ToList();

                    if (notOverloadedServers.Any())
                    {
                        return notOverloadedServers.First().Key;
                    }

                    return _serverMetrics
                        .OrderBy(s => s.Value.ActiveRequests)
                        .ThenBy(s => s.Value.AverageResponseTime)
                        .First().Key;
                }

                return availableServers.First().Key;
            }
        }

        public void MarkServerUnavailable(string address)
        {
            lock (_lockObject)
            {
                if (_serverMetrics.TryGetValue(address, out var metrics))
                {
                    metrics.IsAvailable = false;
                }
            }
        }

        public void MarkServerAvailable(string address)
        {
            lock (_lockObject)
            {
                if (_serverMetrics.TryGetValue(address, out var metrics))
                {
                    metrics.IsAvailable = true;
                }
            }
        }

        public Dictionary<string, (int activeRequests, double avgResponseTime, bool isAvailable)> GetServerStatus()
        {
            lock (_lockObject)
            {
                return _serverMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.ActiveRequests, kvp.Value.AverageResponseTime, kvp.Value.IsAvailable)
                );
            }
        }
    }
} 