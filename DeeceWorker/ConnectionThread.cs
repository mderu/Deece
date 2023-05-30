using DeeceApi.Client.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
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
            var messageHeaderBytes = new ArraySegment<byte>(new byte[SentObjectHeader.GetSize()]);
            EndPoint endPoint = null;
            await socket.ReceiveFromAsync(messageHeaderBytes, SocketFlags.None, endPoint);

            var messageHeader = FromBytes<SentObjectHeader>(messageHeaderBytes.ToArray());

            ModelId modelId = (ModelId)messageHeader.ModelId;
            switch (modelId)
            {
                case ModelId.JobRequest:
                    {
                        await HandleJobRequest(messageHeader);
                        break;
                    }
                case ModelId.FileResponse:
                    {
                        await HandleFileResponse(messageHeader);
                        break;
                    }
                default:
                    {
                        // TODO: Don't kill the server. Gracefully tell the client
                        // that the message is not recognized.
                        throw new Exception($"modelId {modelId} is not handled by this endpoint.");
                    }
            }
        }

        // TODO: Move into a util or something.
        private static T FromBytes<T>(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }

        private async Task HandleJobRequest(SentObjectHeader header)
        {
            var jobRequestBytes = new ArraySegment<byte>(new byte[header.SizeInBytes]);
            int bytesRead = await socket.ReceiveAsync(jobRequestBytes, SocketFlags.None);

            if (bytesRead != header.SizeInBytes)
            {
                throw new Exception($"Received the wrong number of bytes for handling {nameof(JobRequest)}.");
            }

            JobRequest jobRequest = FromBytes<JobRequest>(jobRequestBytes.ToArray());

            // TODO:
            // 1. Begin job execution.
            // 2. Blocking read against traffic coming in from the IPC channel with the new pid.
            // 3. Implement an IPC channel message when the injected process completes.
            // 4. Handle all IPC channel messages, calling client-worker APIs as necessary.
            // 5. Send job result object back over the wire.
        }

        private async Task HandleFileResponse(SentObjectHeader header)
        {
            var fileResponseBytes = new ArraySegment<byte>(new byte[header.SizeInBytes]);
            int bytesRead = await socket.ReceiveAsync(fileResponseBytes, SocketFlags.None);

            if (bytesRead != header.SizeInBytes)
            {
                throw new Exception($"Received the wrong number of bytes for handling {nameof(FileResponse)}.");
            }

            FileResponse fileResponse = FromBytes<FileResponse>(fileResponseBytes.ToArray());

            // TODO: notify the process that the file is ready. Load the new path.
        }
    }
}
