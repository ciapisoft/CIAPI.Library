using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIAPI.Helpers
{
    public static class BasicTokenManager
    {
        public static bool isValid(string token, DateTime fecha, string apikey)
        {
            var ret = false;
            if(token.Length>10)
            {
                var tokenplain = EasyEncrypt.Decrypt(token);
                var listToken = tokenplain.Split('|');
                if (listToken.Count()==2)
                {
                    var fectoken = DateTime.Parse(listToken[0]);
                    var apikeytoken = listToken[1];
                    if(Math.Abs(fectoken.Subtract(fecha).TotalMinutes) < 1)
                    {
                        if(Math.Abs(fecha.Subtract(DateTime.Now).TotalHours) <= 12)
                        { 
                            ret = (apikey == apikeytoken);
                        }
                     
                    }
                }
            }
            return ret;
        }

        public static string Generate(DateTime fecha, string apikey)
        {
            var ret = fecha.ToString() +"|"+apikey ;
            ret = EasyEncrypt.Encrypt(ret);
            //var aux = EasyEncrypt.Decrypt(ret);
            return ret;

        }

    }
}
