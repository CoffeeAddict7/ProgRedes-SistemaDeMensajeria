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
    }
}
