using System.Net.Sockets;

namespace Common.Models
{
    public struct ClientInfo
    {
        public Socket Socket;   //Socket of the client
        public string UserName;  //Name by which the user logged into the chat room
    }
}
