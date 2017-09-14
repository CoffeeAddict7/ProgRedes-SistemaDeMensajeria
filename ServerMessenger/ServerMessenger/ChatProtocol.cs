using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger
{
    public class ChatProtocol
    {
        private static int HEADER_INDEX = 0;
        private static int CMD_INDEX = 3;
        private static int PAYLOAD_INDEX = 9;        

        public string Header { get; set; }
        public int Command { get; set; }
        public string Payload { get; set; }

        public ChatProtocol(string package, int payloadLength)
        {
            var packageChars = package.ToCharArray();
            string header = new String(packageChars, HEADER_INDEX, 3);
            string cmd = new String(packageChars, CMD_INDEX, 2);
            string payload = new String(packageChars, PAYLOAD_INDEX, payloadLength);
            ValidateProtocolFields(header, cmd);
            SetProtocolProperties(header, cmd, payload);
        }

        private void SetProtocolProperties(string header, string cmd, string payload)
        {
            int command;
            int.TryParse(cmd, out command);
            this.Header = header;
            this.Command = command;
            this.Payload = payload;
        }

        private void ValidateProtocolFields(string header, string cmd)
        {
            if (!ValidHeader(header) || !ValidCommand(cmd))
            {
                throw new Exception("Error: invalid protocol header or wrong command");
            }
        }

        private static bool ValidCommand(string cmd)
        {
            return cmd.All(char.IsDigit);
        }

        private bool ValidHeader(string header)
        {
            ICollection<string> acceptedHeaders = new List<String> { "REQ", "RES" };
            return acceptedHeaders.Any(h => h.Equals(header.ToUpper()));
        }
    }
}
