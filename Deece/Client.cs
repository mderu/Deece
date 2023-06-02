using DeeceApi;
using DeeceApi.Client.Models;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Deece
{
    public class Client
    {
        public string IpAddress { get; }
        public int Port { get; }
        public Client(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public async Task StartAsync()
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(IPAddress.Parse(IpAddress), Port);

            string fileToRead = "C:/Windows/System32/WindowsPowerShell/v1.0/en-US/default.help.txt";
            JobRequest jobRequest = new JobRequest()
            {
                MessageId = 1,
                WorkingDirectory = "C:/",
                OriginalExecutablePath = "C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe",
                // Multi-process commandline is broken because the Worker doesn't pick up the child pids.
                //Commandline = $"Powershell Powershell Get-Content \"{fileToRead}\"",
                Commandline = $"Get-Content \"{fileToRead}\"",
                DirectTransfer = true,
            };

            var bytes = Utils.ToBytes(jobRequest);
            SentObjectHeader header = new SentObjectHeader()
            {
                SizeInBytes = bytes.Length,
                ModelId = (int)Enum.Parse<ModelId>(nameof(JobRequest)),
            };
            var headerBytes = Utils.ToBytes(header);
            await socket.SendAsync(headerBytes);
            await socket.SendAsync(bytes);

            
            while (true)
            {
                await socket.ReceiveAsync(headerBytes);
                header = Utils.FromBytes<SentObjectHeader>(headerBytes);
                if (header.ModelId != (int)ModelId.FileRequest)
                {
                    break;
                }
                byte[] fileRequestBytes = new byte[header.SizeInBytes];
                await socket.ReceiveAsync(fileRequestBytes);
                await SendFileResponse(socket, Utils.FromBytes<FileRequest>(fileRequestBytes));
            }

            Console.WriteLine("Received Job finish message");
            // TODO: Handle job result.
        }

        private async Task SendFileResponse(Socket socket, FileRequest request)
        {
            Console.WriteLine($"Reading requested file {request.OriginalFilePath}");
            
            byte[] contentBytes;
            string hash;

            if (File.Exists(request.OriginalFilePath))
            {
                contentBytes = await File.ReadAllBytesAsync(request.OriginalFilePath);
                hash = string.Concat(SHA1.HashData(contentBytes).Select(x => x.ToString("X2")));
            }
            else
            {
                contentBytes = new byte[0];
                hash = FileResponse.FileDoesNotExist;
            }

            FileResponse fileResponse = new FileResponse()
            {
                MessageId = request.MessageId,
                FileName = request.OriginalFilePath,
                Contents = contentBytes,
                Hash = hash,
            };
            byte[] fileResponseBytes = Utils.ToBytes(fileResponse);
            SentObjectHeader header = new SentObjectHeader()
            {
                SizeInBytes = fileResponseBytes.Length,
                ModelId = (int)Enum.Parse<ModelId>(nameof(FileResponse)),
            };
            byte[] headerBytes = Utils.ToBytes(header);

            await socket.SendAsync(headerBytes);
            await socket.SendAsync(fileResponseBytes);
        }
    }
}
