using System.Linq;
using System.Windows.Forms;

namespace WSMessengerClient
{
    public static class CommonBehavior
    {

        private static bool validParamLength(string param)
        {
            return param.Length >= 4;
        }
        private static bool validParameter(string param)
        {
            return !param.Contains('#') && !param.Contains('@') && !param.Contains('_') && !param.Contains('$');
        }
        public static bool UserProfileValidInfo(string username, string password)
        {
            bool validInfo = true;
            if (!CommonBehavior.validParamLength(username) || !CommonBehavior.validParamLength(password))
            {
                MessageBox.Show("Error: parameters must be 4 digits long at least");
                validInfo = false;
            }
            else if (!CommonBehavior.validParameter(username) || !CommonBehavior.validParameter(password))
            {
                MessageBox.Show("Error: parameters can't contain #, @, $ or _");
                validInfo = false;
            }
            return validInfo;
        }
    }
}
