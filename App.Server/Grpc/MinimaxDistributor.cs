using App.Grpc;
using App.Server;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Client
{
    public class MinimaxDistributor
    {
        private readonly List<string> _serverAddresses;
        private int _currentServerIndex = 0;

        public MinimaxDistributor(List<string> serverAddresses)
        {
            _serverAddresses = serverAddresses;
        }

        public int DistributeMinimaxSearch(CheckersBoard board, int depth, bool isMaximizing)
        {
            // Send the entire board state to the server and let it handle move generation
            // and evaluation of all subtrees in parallel
            return SendBoardForEvaluation(board, depth, isMaximizing);
        }

        private int SendBoardForEvaluation(CheckersBoard board, int depth, bool isMaximizing)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string serverAddress = GetNextServerAddress();
            using var channel = GrpcChannel.ForAddress(serverAddress);
            var client = new CheckersEvaluationService.CheckersEvaluationServiceClient(channel);
    
            var request = new MinimaxRequest
            {
                Depth = depth,
                IsMaximizing = isMaximizing
            };
    
            // Add the board state to the request
            request.Board.Add(board.board[0]);
            request.Board.Add(board.board[1]);
            request.Board.Add(board.board[2]);
            // Send the request and get the response
            var response = client.MinimaxSearch(request);
            stopwatch.Stop();

            Console.WriteLine($"SendBoardForEvaluation execution time: {stopwatch.ElapsedMilliseconds} ms for server {serverAddress}");
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