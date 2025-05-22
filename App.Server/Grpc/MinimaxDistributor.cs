using App.Grpc;
using App.Server;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace App.Client
{
    public class MinimaxDistributor
    {
        private readonly List<string> _serverAddresses;
        private readonly Dictionary<string, GrpcChannel> _channels;
        private int _currentServerIndex = 0;

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
            _channels = new Dictionary<string, GrpcChannel>();
            foreach (var address in _serverAddresses)
            {
                _channels[address] = GrpcChannel.ForAddress(address);
            }
        }

        public int DistributeMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            return SendBoardForEvaluation(board, depth, isMaximizing);
        }

        private int SendBoardForEvaluation(CheckersBoard board, int depth, bool isMaximizing)
        {
            string serverAddress = GetNextServerAddress();
            var channel = _channels[serverAddress];
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);

            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing,
                RequestTime = Timestamp.FromDateTimeOffset(DateTimeOffset.Now)
            };
            request.Board.Add(board.board[0]);
            request.Board.Add(board.board[1]);
            request.Board.Add(board.board[2]);

            var response = client.MinimaxSearch(request);
            var currentTime = DateTimeOffset.Now;
            var responseTime = response.ResponseTime.ToDateTimeOffset();
            Console.WriteLine($"Response time: {(currentTime - responseTime).TotalMilliseconds} ms");
            return response.Score;
        }


        private string GetNextServerAddress()
        {
            lock (this)
            {
                string address = _serverAddresses[_currentServerIndex];
                _currentServerIndex = (_currentServerIndex + 1) % _serverAddresses.Count;
                return address;
            }
        }
    }
}