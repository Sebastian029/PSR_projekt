using Grpc.Core;
using GrpcService;

namespace GrpcService.Services
{
    public class GreeterService : GrpcServer.Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<GrpcServer.HelloReply> SayHello(GrpcServer.HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GrpcServer.HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
