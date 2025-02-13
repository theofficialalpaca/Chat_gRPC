﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient
{
    class Program
    {
        public static readonly GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
        public static readonly Chat.ChatClient client = new Chat.ChatClient(channel);
        public static readonly ChatMessagesStreaming.ChatMessagesStreamingClient streamingClient = new ChatMessagesStreaming.ChatMessagesStreamingClient(channel);
        public static string username;

        static async void Login()
        {
            Console.Write("What's your name? ");
            while (String.IsNullOrWhiteSpace(username))
            {
                username = Console.ReadLine();
            }
            try
            {
                var reply = await client.LoginAsync(new UserRequest { User = username });
            }
            catch
            {
                ServerDownAlert();
            }
            Console.Clear();
            Console.WriteLine("You are now connected! Say something...");
        }

        static async void Logout()
        {
            var reply = await client.LogoutAsync(new UserRequest { User = username });
            System.Environment.Exit(1);
        }

        static async void SendMessage(string message)
        {
            var reply = await client.SendMessageAsync(new MessageInput { User = username, Message = message });
        }

        static void ServerDownAlert()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Server is down...");
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Login();

            #region GETTING MESSAGES
            new Thread(async () =>
            {
                var dataStream = streamingClient.ChatMessagesStreaming(new Empty());
                try
                {
                    await foreach (var messageData in dataStream.ResponseStream.ReadAllAsync())
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        if (messageData.User.Equals("SERVER"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        Console.WriteLine($"[{DateTime.Now}]{messageData.User}: {messageData.Message}");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                }
                catch
                {
                    ServerDownAlert();
                }
            }).Start();
            #endregion

            #region SENDING A MESSAGE
            while (true)
            {
                String message = Console.ReadLine();
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                if (message.ToUpper().Equals("/EXIT"))
                {
                    Logout();
                }
                else if (String.IsNullOrWhiteSpace(message))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid text");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else
                {
                    SendMessage(message);
                }
            }
            #endregion
        }
    }
}
