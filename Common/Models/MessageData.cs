using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums;

namespace Common.Models
{
    public class MessageData
    {
        public string UserName { get; set; }
        public string Message { get; set; }
        public Commands Command { get; set; }

        public MessageData()
        {
            UserName = null;
            Message=null;
            Command = Commands.Null;
        }

        public MessageData(byte[] messageData)
        {
            //The first four bytes are for the Command
            Command = (Commands)BitConverter.ToInt32(messageData, 0);

            //The next four store the length of the name
            int userNameLen = BitConverter.ToInt32(messageData, 4);

            //The next four store the length of the message
            int messageLen = BitConverter.ToInt32(messageData, 8);

            //This check makes sure that userNameLen has been passed in the array of bytes
            UserName = userNameLen > 0 ? Encoding.UTF8.GetString(messageData, 12, userNameLen) : null;

            //This checks for a null message field
            Message = messageLen > 0 ? Encoding.UTF8.GetString(messageData, 12 + userNameLen, messageLen) : null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)Command));

            //Add the length of the name
            result.AddRange(UserName != null ? BitConverter.GetBytes(UserName.Length) : BitConverter.GetBytes(0));

            //Length of the message
            result.AddRange(Message != null ? BitConverter.GetBytes(Message.Length) : BitConverter.GetBytes(0));

            //Add the name
            if (UserName != null)
                result.AddRange(Encoding.UTF8.GetBytes(UserName));

            //And, lastly we add the message text to our array of bytes
            if (Message != null)
                result.AddRange(Encoding.UTF8.GetBytes(Message));

            return result.ToArray();
        }
    }
}
