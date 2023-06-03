using DeeceApi;
using DeeceApi.Client.Models;
using DeeceApi.InternalWorker;
using EasyHook;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DeeceWorker
{
    public class ConnectionThread
    {
        private readonly Socket socket;
        public ConnectionThread(Socket socket)
        {
            this.socket = socket;
        }

        public async Task HandleConnection()
        {
            SentObjectHeader messageHeader = await ReadFileHeader();

            ModelId modelId = (ModelId)messageHeader.ModelId;
            switch (modelId)
            {
                case ModelId.JobRequest:
                    {
                        await ReceiveJobRequest(messageHeader);
                        break;
                    }
                default:
                    {
                        // TODO: Don't kill the server. Gracefully tell the client
                        // that the message is not recognized.
                        throw new Exception($"modelId {modelId} is not handled by this endpoint at this time.");
                    }
            }
        }

        private async Task ReceiveJobRequest(SentObjectHeader header)
        {
            var jobRequestBytes = new ArraySegment<byte>(new byte[header.SizeInBytes]);
            int bytesRead = await socket.ReceiveAsync(jobRequestBytes, SocketFlags.None);

            if (bytesRead != header.SizeInBytes)
            {
                throw new Exception($"Received the wrong number of bytes for handling {nameof(JobRequest)}.");
            }

            JobRequest jobRequest = Utils.FromBytes<JobRequest>(jobRequestBytes.ToArray());

            await StartProcess(jobRequest);
        }

        private async Task<FileResponse> ReceiveFileResponse(SentObjectHeader header)
        {
            var fileResponseBytes = new ArraySegment<byte>(new byte[header.SizeInBytes]);
            int bytesRead = await socket.ReceiveAsync(fileResponseBytes, SocketFlags.None);

            Console.WriteLine($"Header Type: {header.ModelId}, Size: {header.SizeInBytes}");
            if (bytesRead != header.SizeInBytes)
            {
                throw new Exception($"Received the wrong number of bytes for handling {nameof(FileResponse)}.");
            }

            return Utils.FromBytes<FileResponse>(fileResponseBytes.ToArray());
        }

        private async Task SendFileRequest(string originalFilename)
        {
            FileRequest fileRequest = new FileRequest()
            {
                MessageId = 0,
                OriginalFilePath = originalFilename,
            };
            var fileRequestBytes = new ArraySegment<byte>(Utils.ToBytes(fileRequest));

            SentObjectHeader header = new SentObjectHeader()
            {
                ModelId = (int)ModelId.FileRequest,
                SizeInBytes = fileRequestBytes.Count,
            };
            var dataToSend = new ArraySegment<byte>(Utils.ToBytes(header).Concat(fileRequestBytes).ToArray());

            await socket.SendAsync(dataToSend, SocketFlags.None);
            DoNothing();
        }

        private void DoNothing() { }

        private async Task<SentObjectHeader> ReadFileHeader()
        {
            // TODO: Cache this byte array?
            var messageHeaderBytes = new ArraySegment<byte>(Utils.ToBytes(new SentObjectHeader()));
            await socket.ReceiveAsync(messageHeaderBytes, SocketFlags.None);

            var messageHeader = Utils.FromBytes<SentObjectHeader>(messageHeaderBytes.ToArray());
            return messageHeader;
        }

        private async Task<JobResponse> StartProcess(JobRequest jobRequest)
        {
            string injectionLibrary = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "ProcessInjection.dll");

            int childPid = 0;

            try
            {
                // TODO: Redirect stdout/stderr to a file.
                // TODO: Set working directory and environment variables.
                RemoteHooking.CreateAndInject(
                    InEXEPath: jobRequest.OriginalExecutablePath,
                    InCommandLine: jobRequest.Commandline,
                    InProcessCreationFlags: 0,
                    InOptions: InjectionOptions.DoNotRequireStrongName,
                    InLibraryPath_x86: injectionLibrary,
                    InLibraryPath_x64: injectionLibrary,
                    OutProcessId: out childPid,
                    // singleton is null, pls fix
                    InternalWorkerCommunication.Instance.Channel);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ResetColor();
            }

            CancellationTokenSource src = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(async (s) =>
            {
                await Process.GetProcessById(childPid).WaitForExitAsync();
                src.Cancel(false);
            });

            try
            {
                while (InternalWorkerCommunication.Instance.ReadFileRequest(childPid, out int tid, out string originalFilename, src.Token))
                {
                    await SendFileRequest(originalFilename);
                    SentObjectHeader messageHeader = await ReadFileHeader();
                    FileResponse response = await ReceiveFileResponse(messageHeader);

                    string newFilePath = Path.GetFullPath(Path.Combine(GetCasPath(), response.Hash));
                    if (!File.Exists(newFilePath) && response.Hash != FileResponse.FileDoesNotExist)
                    {
                        File.WriteAllBytes(newFilePath, response.Contents);
                    }

                    long ptid = (((long)childPid) << 32) + tid;
                    InternalWorkerCommunication.Instance.WriteFileResponse(ptid, newFilePath);
                }
            }
            catch (OperationCanceledException e)
            {

            }

            return null;
        }

        private string GetCasPath()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), ".deece", "cas");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
