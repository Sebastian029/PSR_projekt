using App.Server;
using Grpc.Core;
using GrpcServer;
using System;
using System.Threading.Tasks;

namespace GrpcService
{
    public class CheckersServiceImpl : CheckersService.CheckersServiceBase
    {
        private readonly WorkerCoordinator _coordinator;
        
        public CheckersServiceImpl(WorkerCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public override Task<RegistrationResponse> RegisterWorker(
            WorkerRegistration request,
            ServerCallContext context)
        {
            _coordinator.RegisterWorker(request.WorkerId, request.MaxDepth);
            return Task.FromResult(new RegistrationResponse { Success = true });
        }

        public override async Task<ScoreResponse> EvaluatePosition(
            BoardStateRequest request,
            ServerCallContext context)
        {
            try
            {
                var startTime = DateTime.UtcNow.Ticks;
                var result = await _coordinator.DistributeScoreCalculationAsync(request);
                result.WorkerStartTicks = startTime;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EvaluatePosition: {ex.Message}");
                return new ScoreResponse
                {
                    Success = false,
                    Score = request.IsWhiteTurn ? int.MinValue : int.MaxValue,
                    WorkerStartTicks = DateTime.UtcNow.Ticks,
                    WorkerEndTicks = DateTime.UtcNow.Ticks
                };
            }
        }

        public override Task<CalculationTask> GetTask(
            TaskRequest request,
            ServerCallContext context)
        {
            if (_coordinator.TryGetNextTask(request.WorkerId, out var task))
            {
                return Task.FromResult(task);
            }
            
            return Task.FromResult(new CalculationTask());
        }

        public override Task<ResultAck> SubmitResult(
            CalculationResult result,
            ServerCallContext context)
        {
            _coordinator.SubmitResult(result.TaskId, result.Result);
            return Task.FromResult(new ResultAck { Success = true });
        }
    }
}
