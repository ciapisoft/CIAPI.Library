using System;
using System.Text;

namespace CIAPI.Helpers
{
    public class TokenForTimeModel
    {

        public double time { get; set; }

        public double modulus { get; set; }

        public double timeFix { get; set; }


        public int Token { get; set; }
        public int secondsLeft { get; set; }
    }
    public class TokenForTime
    {
        const int K_DIGITS = 3;
        const int K_DEFAULT_WAIT = 60;  //Seconds


        public static TokenForTimeModel GetToken(int numberPass, int secondsWait = K_DEFAULT_WAIT, DateTime? ahora = null)
        {
            var ret = new TokenForTimeModel();

            DateTime ah = ahora == null ? DateTime.Now : ahora.Value;
            //ret.time = Math.Truncate(DateTime.UtcNow.Subtract( DateTime.MinValue ).TotalSeconds);
            ret.time = Math.Truncate(ah.ToUniversalTime().Subtract(DateTime.MinValue).TotalSeconds);

            ret.modulus = ret.time % secondsWait;

            ret.secondsLeft = (int)(secondsWait - ret.modulus);

            ret.timeFix = ret.time + ret.secondsLeft;

            var str = ret.time.ToString();

            var calc = (ret.timeFix / numberPass).ToString();


            var textBytes = Encoding.UTF8.GetBytes(calc);

            var base64String = Convert.ToBase64String(textBytes);
            var calc2 = base64String.ToString();

            byte[] newBytes = Convert.FromBase64String(calc2);
            Int32 calc3 = 0;
            foreach (byte b in newBytes)
            {
                calc3 += b * (b % 2 == 1 ? 111 : 133);
            }
            var calc4 = calc3.ToString();


            ret.Token = int.Parse(calc4.Substring(calc4.Length - K_DIGITS - 1, K_DIGITS));


            return ret;
        }

        public static bool IsValidToken(int verifyToken, int numberPass, int secondsWait = K_DEFAULT_WAIT, DateTime? ahora = null)
        {
            var tokenOK = GetToken(numberPass, secondsWait, ahora);

            return tokenOK.Token == verifyToken;
        }
    }
}
