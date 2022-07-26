using System;
using System.Net;
using System.Net.Sockets;
using Common;
using Common.Enums;
using Common.Models;

namespace Client
{
    internal class Program
    {
        static string UserName;
        static Socket Socket = Helper.GetSocket();
        static byte[] byteData = new byte[1024];
        static bool status = true;
        static void Main()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Console.WriteLine("Enter your name : ");
                UserName = Console.ReadLine();
            }

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            //Server is listening on port 1000
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 1000);

            //Connect to the server
            Socket.BeginConnect(ipEndPoint, OnConnect, null);

            Socket.BeginReceive(byteData,
                0,
                byteData.Length,
                SocketFlags.None,
                OnReceive,
                null);

            Console.WriteLine($"Client Started {DateTime.Now}");


            while (status)
            {
                try
                {
                    Console.Write($"{UserName} Says: ");
                    string message = Console.ReadLine();

                    if (message != null)
                    {
                        MessageData msgToSend = new MessageData
                        {
                            UserName = UserName,
                            Message = message.Replace("Client Says: ", ""),
                            Command = Commands.Message
                        };

                        byteData = msgToSend.ToByte();
                    }

                    //Send it to the server
                    Socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, OnSend, null);
                }
                catch (ObjectDisposedException)
                { }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client Error {ex}");
                    status = false;
                }
            }

            Console.ReadLine();
        }

        static void OnConnect(IAsyncResult ar)
        {
            try
            {
                Socket.EndConnect(ar);

                //We are connected so we login into the server
                MessageData msgToSend = new MessageData
                {
                    Command = Commands.Login,
                    UserName = UserName,
                    Message = null
                };

                byte[] b = msgToSend.ToByte();

                //Send the message to the server
                Socket.BeginSend(b, 0, b.Length, SocketFlags.None, OnSend, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client Error {ex}");
            }
        }

        static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine($"Client Error {ex}");
            }
        }

        static void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket.EndReceive(ar);

                MessageData msgReceived = new MessageData(byteData);
                //Accordingly process the message received
                switch (msgReceived.Command)
                {
                    case Commands.Login:
                        Console.WriteLine("<<<" + msgReceived.UserName + " has joined the room>>>");
                        break;

                    case Commands.Logout:
                        Console.WriteLine("<<<" + msgReceived.UserName + " has logout the room>>>");
                        
                        MessageData banMessage = new MessageData
                        {
                            Command = Commands.Ban,
                            UserName = UserName,
                            Message = "1 saniyede 1 den fazla mesaj gönderdiğiniz için banlandınız."
                        };
                        var banMessageBytes = banMessage.ToByte();
                        Socket.BeginSend(banMessageBytes, 0, banMessageBytes.Length, SocketFlags.None, OnSend,
                            Socket);
                        Socket.Close();
                        status = false;
                        break;
                    case Commands.Ban:
                        Console.WriteLine(msgReceived.Message);
                        Console.Write($"{UserName} Says: ");
                        break;
                    case Commands.Message:
                        if (msgReceived.UserName != UserName)
                        {
                            Console.WriteLine($"{msgReceived.Message}");
                            Console.Write($"{UserName} Says: ");
                        }
                        break;
                }

                byteData = new byte[1024];

                Socket.BeginReceive(byteData,
                    0,
                    byteData.Length,
                    SocketFlags.None,
                    OnReceive,
                    null);

            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine($"Client Error {ex}");
            }
        }
    }
}
