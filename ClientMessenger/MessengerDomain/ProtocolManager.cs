using ServerMessenger;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public ChatProtocol CreateResponseErrorProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.RESPONSE_HEADER, cmd, ChatData.RESPONSE_ERROR + "$" + payload);
            return new ChatProtocol(package);
        }

        private ChatProtocol CreateRequestProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.REQUEST_HEADER, cmd, payload);
            return new ChatProtocol(package);
        }

        private string BuildPackage(string header, string cmd, string payload)
        {
            string packageSize = BuildProtocolHeaderLength(ChatData.PROTOCOL_FIXED_BYTES + payload.Length);
            return header + cmd + packageSize + payload;
        }
        private string NumericCmdToFixedCommand(int cmd)
        {
            string command = cmd.ToString();
            if (ProtocolCommandInRange(cmd))
                command = "0" + command;
            else
                throw new Exception("Error: Command must be less than 2 digits");
            return command;
        }

        private static bool ProtocolCommandInRange(int cmd)
        {
            return cmd < 100 && cmd < 9;
        }
    }
}
