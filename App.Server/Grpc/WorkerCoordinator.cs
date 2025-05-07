using System.Collections.Concurrent;
using GrpcServer;

namespace GrpcService
{
    public class WorkerCoordinator
    {
        private readonly ConcurrentDictionary<string, WorkerInfo> _workers = new();
        private readonly ConcurrentQueue<GrpcServer.CalculationTask> _pendingTasks = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<BestValueResponse>> _pendingResults = new();

        public void RegisterWorker(string workerId, int maxDepth)
        {
            _workers[workerId] = new WorkerInfo(maxDepth);
            Console.WriteLine($"Worker registered: {workerId}");
        }

        public async Task<BestValueResponse> DistributeCalculationAsync(BoardStateRequest request)
        {
            var taskId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<BestValueResponse>();

            var task = new GrpcServer.CalculationTask
            {
                TaskId = taskId,
                Request = request
            };

            _pendingResults[taskId] = tcs;
            _pendingTasks.Enqueue(task);

            Console.WriteLine($"Task {taskId} queued for processing");
            return await tcs.Task;
        }

        public bool TryGetNextTask(out GrpcServer.CalculationTask task)
        {
            return _pendingTasks.TryDequeue(out task);
        }

        public void SubmitResult(string taskId, BestValueResponse result)
        {
            if (_pendingResults.TryRemove(taskId, out var tcs))
            {
                tcs.SetResult(result);
                Console.WriteLine($"Result received for task {taskId}");
            }
        }
    }

    public record WorkerInfo(int MaxDepth);
}