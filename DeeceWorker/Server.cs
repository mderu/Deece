﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeeceApi.Client.Models;

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
            // TODO: limit the threadpool's number of threads?

            while (isDisposed)
            {
                Console.WriteLine("Waiting for a connection...");
                Socket newSocket = await listener.AcceptAsync();
                var connectionThread = new ConnectionThread(newSocket);

                // TODO: Taskify?
                ThreadPool.QueueUserWorkItem(async delegate { await connectionThread.HandleConnection(); });
            }
        }
    }
}
