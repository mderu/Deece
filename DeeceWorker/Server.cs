using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeeceApi.Client.Models;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeeceWorker
{
    public class Server
    {
        public class StateObject
        {
            public Socket workSocket;
            public SentObjectHeader header;
            public byte[] payloadBytes;
        }

        public const int DefaultPort = 63_378;

        /// <summary>
        /// An event that is raised when an async accept signal has been processed.
        /// </summary>
        private readonly ManualResetEvent acceptFlag = new ManualResetEvent(false);

        private bool isDisposed = false;

        public async Task StartAsync()
        {
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, DefaultPort);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipEndPoint);

            // Listen for up to 8 connections before claiming the server is busy.
            listener.Listen(backlog: 8);

            while (isDisposed)
            {
                // Set the event to nonsignaled state.
                acceptFlag.Reset();

                Console.WriteLine("Waiting for a connection...");

                // TODO: Find a way to gracefully close down listening.
                listener.BeginAccept(
                    callback: new AsyncCallback(BeginAccept),
                    state: listener);

                // Wait until a connection is made before continuing.
                acceptFlag.WaitOne();
            }
        }

        private void BeginAccept(IAsyncResult asyncResult)
        {
            acceptFlag.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            // Create the state object.
            var messageHeader = new SentObjectHeader();
            byte[] bytes = new byte[8];
            handler.BeginReceive(
                buffer: bytes,
                offset: 0,
                socketFlags: SocketFlags.None,
                size: GetHeaderSize(),
                callback: new AsyncCallback(ReceiveHeaderCallback),
                state: messageHeader);
        }

        public static int GetHeaderSize()
        {
            unsafe
            {
                return sizeof(SentObjectHeader);
            }
        }

        public static void ReceiveHeaderCallback(IAsyncResult asyncResult)
        {
            var state = (StateObject)asyncResult.AsyncState;
            Socket handler = state.workSocket;
            SentObjectHeader header = state.header;
            state.payloadBytes = new byte[header.SizeInBytes - GetHeaderSize()];

            ModelId modelId = (ModelId)header.ModelId;
            switch (modelId)
            {
                case ModelId.JobRequest:
                    {
                        handler.BeginReceive(
                            buffer: state.payloadBytes,
                            offset: 0,
                            socketFlags: SocketFlags.None,
                            size: GetHeaderSize(),
                            callback: new AsyncCallback(HandleJobRequest),
                            state: state);
                        break;
                    }
                case ModelId.FileResponse:
                    {
                        handler.BeginReceive(
                            buffer: state.payloadBytes,
                            offset: 0,
                            socketFlags: SocketFlags.None,
                            size: GetHeaderSize(),
                            callback: new AsyncCallback(HandleFileResponse),
                            state: state);
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

        private static T FromBytes<T>(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }

        public static void HandleJobRequest(IAsyncResult asyncResult)
        {
            var jobRequest = FromBytes<JobRequest>(((StateObject)asyncResult.AsyncState).payloadBytes);
        }

        public static void HandleFileResponse(IAsyncResult asyncResult)
        {
            var FileRequest = FromBytes<FileRequest>(((StateObject)asyncResult.AsyncState).payloadBytes);
        }
    }
}
