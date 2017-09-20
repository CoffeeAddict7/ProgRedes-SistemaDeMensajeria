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
        public string BuildProtocolHeaderLength(int size)
        {
            var sizeInStr = size.ToString();
            while (sizeInStr.Length != 4)
                sizeInStr = "0" + sizeInStr;

            if (sizeInStr.Length > 4)
                throw new Exception("Error: Request message too big");
            return sizeInStr;
        }

        public int ExtractProtocolHeaderLength(string size)
        {
            int length = 0;
            if (!Int32.TryParse(size, out length))
                throw new Exception("Error: Protocol header length non numeric");
               
            return length;
        }

        public ChatProtocol CreateResponseProtocol(string cmd, string payload)
        {
            string package = BuildPackage(ChatData.RESPONSE_HEADER, cmd, payload);
            return new ChatProtocol(package);
        }

        private string BuildPackage(string header, string cmd, string payload)
        {
            string packageSize = BuildProtocolHeaderLength(ChatData.PROTOCOL_FIXED_BYTES + payload.Length);
            return ChatData.RESPONSE_HEADER + cmd + packageSize + payload;
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
