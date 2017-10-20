using System;
using System.Collections.Generic;
using System.Linq;
using MessengerDomain;

namespace ServerMessenger
{
    public class ChatProtocol
    {
        private static int HEADER = 3;
        private static int CMD = 2;

        public string Header { get; set; }
        public string Command { get; set; }
        public string Payload { get; set; }
        public string Package { get; set; }
        public ChatProtocol(string package)
        {
            string header, cmd, payload;
            ExtractProtocolParameters(package, out header, out cmd, out payload);
            ValidateProtocolFields(header, cmd);
            SetProtocolProperties(package, header, cmd, payload);
        }

        private static void ExtractProtocolParameters(string package, out string header, out string cmd, out string payload)
        {
            try {
                header = new String(package.Take(HEADER).ToArray());
                cmd = new String(package.Skip(HEADER).Take(CMD).ToArray());
                int payloadBytes = package.Length - ChatData.PROTOCOL_FIXED_BYTES;
                payload = new String(package.Skip(ChatData.PROTOCOL_FIXED_BYTES).Take(payloadBytes).ToArray());
            }
            catch (Exception) {
                throw new Exception("Error: Incorrect protocol format");
            }           
        }

        public int GetCommandNumber()
        {
            int intCmd;
            int.TryParse(this.Command, out intCmd);
            return intCmd;
        }

        private void SetProtocolProperties(string package, string header, string command, string payload)
        {
            this.Header = header;
            this.Command = command;
            this.Payload = payload;
            this.Package = package;
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
            ICollection<string> acceptedHeaders = new List<String> { ChatData.REQUEST_HEADER, ChatData.RESPONSE_HEADER };
            return acceptedHeaders.Any(h => h.Equals(header.ToUpper()));
        }

    }
}
