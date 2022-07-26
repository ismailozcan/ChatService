using System;
using System.Net;
using System.Net.Sockets;

namespace Common
{
    public static class Helper
    {
        public static Socket GetSocket()
        {
            return new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
        }
    }
}
