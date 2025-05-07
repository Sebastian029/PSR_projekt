using System;
using System.Collections.Concurrent;
using System.Linq;
using GrpcServer;

namespace GrpcService
{
    public class WorkerCoordinator
    {
        private readonly ConcurrentDictionary<string, WorkerInfo> _workers = new();
        private readonly ConcurrentQueue<CalculationTask> _pendingTasks = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<BestValueResponse>> _pendingResults = new();
        private readonly ConcurrentDictionary<string, (string workerId, DateTime startTime)> _activeTasks = new();
        private readonly ConcurrentDictionary<string, byte> _processedTasks = new(); // Śledzenie przetworzonych zadań

        public void RegisterWorker(string workerId, int maxDepth)
        {
            _workers[workerId] = new WorkerInfo(maxDepth);
            LogWorkerStatus();
        }

        public async Task<BestValueResponse> DistributeCalculationAsync(BoardStateRequest request)
        {
            var taskId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<BestValueResponse>();

            var task = new CalculationTask
            {
                TaskId = taskId,
                Request = request
            };

            if (!_pendingResults.TryAdd(taskId, tcs))
            {
                throw new InvalidOperationException($"Task {taskId} already exists in pending results");
            }

            _pendingTasks.Enqueue(task);
            LogTaskStatus($"New task created: {taskId}");

            return await tcs.Task;
        }

        public bool TryGetNextTask(string workerId, out CalculationTask task)
        {
            // Worker może mieć tylko jedno aktywne zadanie
            if (_activeTasks.Values.Any(x => x.workerId == workerId))
            {
                task = null;
                return false;
            }

            while (_pendingTasks.TryDequeue(out task))
            {
                // Sprawdź czy zadanie nie było już przetwarzane
                if (_processedTasks.TryAdd(task.TaskId, 0))
                {
                    if (_activeTasks.TryAdd(task.TaskId, (workerId, DateTime.UtcNow)))
                    {
                        LogTaskStatus($"Task {task.TaskId} assigned to {workerId}");
                        return true;
                    }
                }
            }

            task = null;
            return false;
        }

        public void SubmitResult(string taskId, BestValueResponse result)
        {
            if (_pendingResults.TryRemove(taskId, out var tcs))
            {
                if (!_activeTasks.TryRemove(taskId, out var taskInfo))
                {
                    Console.WriteLine($"[WARNING] Task {taskId} not found in active tasks");
                }
                tcs.SetResult(result);
                LogTaskStatus($"Task {taskId} completed by {taskInfo.workerId}");
            }
            else
            {
                Console.WriteLine($"[ERROR] Task {taskId} not found in pending results");
            }
        }

        private void LogTaskStatus(string actionMessage)
        {
            if (_pendingTasks.IsEmpty && _activeTasks.IsEmpty && _pendingResults.IsEmpty)
                return;

            Console.WriteLine($"\n=== Task Status at {DateTime.UtcNow:HH:mm:ss} ===");
            Console.WriteLine($"Action: {actionMessage}");

            if (!_activeTasks.IsEmpty)
            {
                Console.WriteLine("\n[Active Tasks]");
                foreach (var task in _activeTasks)
                {
                    var duration = DateTime.UtcNow - task.Value.startTime;
                    Console.WriteLine($"- {task.Key} (Worker: {task.Value.workerId}, Running: {duration.TotalSeconds:F1}s)");
                }
            }

            if (!_pendingTasks.IsEmpty)
            {
                Console.WriteLine($"\n[Pending Tasks: {_pendingTasks.Count}]");
            }

            if (!_pendingResults.IsEmpty)
            {
                Console.WriteLine($"\n[Waiting Results: {_pendingResults.Count}]");
            }
        }

        private void LogWorkerStatus()
        {
            if (_workers.IsEmpty)
                return;

            Console.WriteLine($"\n=== Workers Status ===");
            Console.WriteLine($"Active workers: {_workers.Count}");
            foreach (var worker in _workers)
            {
                var activeTask = _activeTasks.FirstOrDefault(t => t.Value.workerId == worker.Key);
                var isProcessing = !activeTask.Equals(default(KeyValuePair<string, (string, DateTime)>));
                Console.WriteLine($"- {worker.Key} (MaxDepth: {worker.Value.MaxDepth}) " +
                                $"{(isProcessing ? $"| Processing: {activeTask.Key}" : "| Idle")}");
            }
        }

        // Dodatkowa metoda do czyszczenia zawieszonych zadań
        public void CleanupStaleTasks(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            foreach (var task in _activeTasks)
            {
                if (now - task.Value.startTime > timeout)
                {
                    if (_activeTasks.TryRemove(task.Key, out _))
                    {
                        Console.WriteLine($"[CLEANUP] Removed stale task: {task.Key}");
                    }
                }
            }
        }
    }

    public record WorkerInfo(int MaxDepth);
}