using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Common;
using Common.Enums;
using Common.Models;

namespace Server
{
    internal class Program
    {
        static Socket Socket = Helper.GetSocket();
        static byte[] byteData = new byte[1024];

        static List<ClientInfo> clientList = new List<ClientInfo>();
        static Dictionary<string, BanUser> LogLastMessages = new Dictionary<string, BanUser>();
        static List<string> banClientList = new List<string>();
        static void Main()
        {
            //Assign the any IP of the machine and listen on port number 1000
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

            //Bind and listen on the given address
            Socket.Bind(ipEndPoint);
            Socket.Listen(4);

            //Accept the incoming clients
            Socket.BeginAccept(OnAccept, null);

            Console.WriteLine($"Server started {DateTime.Now}");

            Console.ReadLine();
        }

        private static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = Socket.EndAccept(ar);

                //Start listening for more clients
                Socket.BeginAccept(OnAccept, null);

                //Once the client connects then start receiving the commands from her
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, OnReceive, clientSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server Error : {ex}");
            }
        }

        private static void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = (Socket)ar.AsyncState;
                clientSocket.EndReceive(ar);

                //Transform the array of bytes received from the user into an
                //intelligent form of object Data
                MessageData msgReceived = new MessageData(byteData);

                //We will send this object in response the users request
                MessageData msgToSend = new MessageData
                {
                    //then when send to others the type of the message remains the same
                    //If the message is to login, logout, or simple text message
                    Command = msgReceived.Command,
                    UserName = msgReceived.UserName
                };

                switch (msgReceived.Command)
                {
                    case Commands.Login:

                        //When a user logs in to the server then we add her to our
                        //list of clients

                        ClientInfo clientInfo = new ClientInfo
                        {
                            Socket = clientSocket,
                            UserName = msgReceived.UserName
                        };

                        clientList.Add(clientInfo);

                        //Set the text of the message that we will broadcast to all users
                        msgToSend.Message = "<<<" + msgReceived.UserName + " has joined the room>>>";
                        break;

                    case Commands.Logout:

                        //When a user wants to log out of the server then we search for her 
                        //in the list of clients and close the corresponding connection

                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.Socket == clientSocket)
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }

                            ++nIndex;
                        }

                        clientSocket.Close();

                        msgToSend.Message = "<<<" + msgReceived.UserName + " has left the room>>>";
                        break;
                    case Commands.Ban:

                        //When a user wants to log out of the server then we search for her 
                        //in the list of clients and close the corresponding connection

                        clientList.RemoveAll(_ => _.UserName == msgReceived.UserName);

                        clientSocket.Close();

                        msgToSend.Message = "<<<" + msgReceived.UserName + " has banned the room>>>";
                        break;
                    case Commands.Message:

                        //Set the text of the message that we will broadcast to all users
                        msgToSend.Message = msgReceived.UserName + ": " + msgReceived.Message;
                        if (!LogLastMessages.ContainsKey(msgReceived.UserName))
                        {
                            LogLastMessages.Add(msgReceived.UserName, new BanUser
                            {
                                PenaltyCount = 0,
                                LastMessageTime = DateTime.Now
                            });
                        }
                        else
                        {
                            DateTime now = DateTime.Now;
                            double duringTime = now.Subtract(LogLastMessages[msgReceived.UserName].LastMessageTime)
                                .TotalSeconds;
                            if (duringTime <= 1)
                            {
                                LogLastMessages[msgReceived.UserName].PenaltyCount++;
                                if (LogLastMessages[msgReceived.UserName].IsBanned)
                                {
                                    banClientList.Add(msgReceived.UserName);
                                }
                            }

                            LogLastMessages[msgReceived.UserName].LastMessageTime = now;
                        }

                        break;

                }

                //messages are  broadcast
                var message = msgToSend.ToByte();

                foreach (ClientInfo clientInfo in clientList)
                {
                    if (clientInfo.Socket != clientSocket ||
                        msgToSend.Command != Commands.Login)
                    {
                        //Send the message to all users
                        clientInfo.Socket.BeginSend(message, 0, message.Length, SocketFlags.None, OnSend,
                            clientInfo.Socket);
                    }
                }

                //Warning block

                List<KeyValuePair<string, BanUser>> warningUsers = LogLastMessages
                    .Where(_ => _.Value.PenaltyCount > 0 && !_.Value.PenaltyMessageSended).ToList();
                if (warningUsers.Count > 0)
                {
                    List<ClientInfo> clients =
                        clientList.Where(_ => warningUsers.Select(t => t.Key).Contains(_.UserName)).ToList();
                    foreach (ClientInfo clientInfo in clients)
                    {
                        MessageData banMessage = new MessageData
                        {
                            Command = Commands.Ban,
                            UserName = clientInfo.UserName,
                            Message = "1 saniyede 1 den fazla mesaj gönderemezsiniz.Aksi halde banlanacaksınız."
                        };
                        var banMessageBytes = banMessage.ToByte();
                        clientInfo.Socket.BeginSend(banMessageBytes, 0, banMessageBytes.Length, SocketFlags.None,
                            OnSend,
                            clientInfo.Socket);
                    }

                    warningUsers.ForEach(_ => _.Value.PenaltyMessageSended = true);
                }

                //Ban block
                if (banClientList.Count > 0)
                {
                    List<ClientInfo> clients = clientList.Where(_ => banClientList.Contains(_.UserName)).ToList();
                    foreach (ClientInfo clientInfo in clients)
                    {
                        MessageData banMessage = new MessageData
                        {
                            Command = Commands.Logout,
                            UserName = clientInfo.UserName,
                            Message = "1 saniyede 1 den fazla mesaj gönderdiğiniz için banlandınız."
                        };
                        var banMessageBytes = banMessage.ToByte();
                        clientInfo.Socket.BeginSend(banMessageBytes, 0, banMessageBytes.Length, SocketFlags.None,
                            OnSend,
                            clientInfo.Socket);
                    }

                    banClientList.Clear();
                }

                Console.WriteLine($"{msgToSend.Message} \r\n");

                //If the user is logging out then we need not listen from her
                if (msgReceived.Command != Commands.Logout || msgReceived.Command != Commands.Ban)
                {
                    //Start listening to the message send by the user
                    clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, OnReceive, clientSocket);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.ConnectionAborted && se.SocketErrorCode != SocketError.ConnectionReset)
                {
                    Console.WriteLine($"Server Error {se}");
                }
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine($"Server Error {ex}");
            }
        }

        static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server Error {ex}");
            }
        }
    }
}
