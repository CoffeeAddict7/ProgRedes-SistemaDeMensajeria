using ServerMessenger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerDomain
{
    public class ProtocolManager
    {
        private string BuildProtocolHeaderLength(int size)
        {
            var sizeInStr = size.ToString();
            while (sizeInStr.Length != 4)
                sizeInStr = "0" + sizeInStr;

            if (sizeInStr.Length > 4)
                throw new Exception("Error: Request message too big");
            return sizeInStr;
        }

        public ChatProtocol CreateRequestProtocolFromInput(String message)
        {
            int cmd;
            string command = "", payload = "";
            ValidateProtocolPayloadInput(message);

            command = new String(message.Take(2).ToArray());
            payload = new String(message.Skip(2).Take(message.Length - 2).ToArray());
            if (!Int32.TryParse(command, out cmd))
                throw new Exception("Error: Server request must be preceded by a numeric command");
            if (cmd < 0)
                throw new Exception("Error: Command cant be negative");

            return CreateRequestProtocol(command, payload);
        }

        private static void ValidateProtocolPayloadInput(string message)
        {
            if (message.Equals(String.Empty))
                throw new Exception("Error: Can't send empty text");
            if (message.Length < 2)
                throw new Exception("Error: Invalid command");
        }
  
        public ChatProtocol CreateResponseOkProtocol(string cmd)
        {
            return CreateResponseOkProtocol(cmd, String.Empty);
        }
        public ChatProtocol CreateResponseOkProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.RESPONSE_HEADER, cmd, ChatData.RESPONSE_OK + "$" + payload);
            return new ChatProtocol(package);
        }
        public ChatProtocol CreateUnseenMessagesResponseProtocol(string type, string payload)
        {
            string package = BuildPackage(ChatData.REQUEST_HEADER, ChatData.CMD_UNSEEN_MSGS, ChatData.RESPONSE_OK + "$" + type + "#" + payload);
            return new ChatProtocol(package);
        }

        public ChatProtocol CreateLiveChatResponseProtocol(string profile, string chatState, string message)
        {
            return CreateLiveChatProtocol(ChatData.RESPONSE_HEADER, ChatData.RESPONSE_OK + "$" + profile + "#" + chatState + "#" + message);
        }
        public ChatProtocol CreateLiveChatRequestProtocol(string profile, string chatState, string message)
        {
            return CreateLiveChatProtocol(ChatData.REQUEST_HEADER, profile + "#" + chatState + "#" + message);
        }
        private ChatProtocol CreateLiveChatProtocol(string type, string payload)
        {
            string package = BuildPackage(type, ChatData.CMD_LIVECHAT, payload);
            return new ChatProtocol(package);
        }

        public ChatProtocol CreateResponseErrorProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.RESPONSE_HEADER, cmd, ChatData.RESPONSE_ERROR + "$" + payload);
            return new ChatProtocol(package);
        }

        public ChatProtocol CreateRequestProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.REQUEST_HEADER, cmd, payload);
            return new ChatProtocol(package);
        }
        public ChatProtocol CreateFileRequestProtocol(string cmd, string name, byte[] content)
        {
            string package = null;
            return new ChatProtocol(package);
        }

        private string BuildPackage(string header, string cmd, string payload)
        {
            string packageSize = BuildProtocolHeaderLength(ChatData.PROTOCOL_FIXED_BYTES + payload.Length);
            return header + cmd + packageSize + payload;
        }

        public int ReadFixedBytesFromPackage(Socket client, StreamReader reader, ref StringBuilder sb)
        {
            var buffer = new char[10000];
            int received = 0, localReceived = 0, bytesLeftToRead = 0, packageLength = ChatData.PROTOCOL_FIXED_BYTES;
            while (received != ChatData.PROTOCOL_FIXED_BYTES)
            {
                bytesLeftToRead = ChatData.PROTOCOL_FIXED_BYTES - received;
                localReceived = reader.Read(buffer, received, bytesLeftToRead);
                received += localReceived;

                if (localReceived > 0)
                {
                    AppendBufferToStringBuilder(ref sb, buffer, received);
                    Int32.TryParse(sb.ToString().Substring(5), out packageLength);
                }
                else
                {
                    EndConnection(client);
                }
            }
            return packageLength;
        }

        public void ReadPayloadBytesFromPackage(Socket client, StreamReader reader, ref StringBuilder sb, int packageLength)
        {
            var payloadBuffer = new char[packageLength + 1];
            int received = 0, localReceived = 0, bytesLeftToRead = 0;
            int payloadLength = packageLength - ChatData.PROTOCOL_FIXED_BYTES;
            while (received != payloadLength)
            {
                bytesLeftToRead = payloadLength - received;
                localReceived = reader.Read(payloadBuffer, received, bytesLeftToRead);
                received += localReceived;

                if (localReceived > 0)
                {
                    AppendBufferToStringBuilder(ref sb, payloadBuffer, payloadLength);
                }
                else
                {
                    EndConnection(client);
                }
            }
        }
        public char[] ReadBytesFromFile(Socket client, StreamReader reader, int fileLength, string name)
        {
            var payloadBuffer = new char[fileLength + 1];
            int received = 0, localReceived = 0, bytesLeftToRead = 0;
            int payloadLength = fileLength;
            while (received != payloadLength)
            {
                bytesLeftToRead = payloadLength - received;                
                localReceived = reader.Read(payloadBuffer, received, bytesLeftToRead);
                received += localReceived;
                if (localReceived == 0)
                    EndConnection(client);
            }
            return payloadBuffer;
        }

        public void AppendBufferToStringBuilder(ref StringBuilder sb, char[] payloadBuffer, int payloadLength)
        {
            var bufferCopy = new char[payloadLength];
            Array.Copy(payloadBuffer, bufferCopy, payloadLength);
            sb.Append(bufferCopy);
        }

        public void EndConnection(Socket client)
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception) { }
        }
    }
}
