using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIAPI.Helpers
{

    public static class CUITL
    {
        public static bool Verifica(string cCUIT)
        {
            string cAux;
            int nResultado=0;
            int nFactor;
            int nValor;
            int i;
            cCUIT= cCUIT.Trim();
            if (cCUIT.Length == 13)
                cAux = cCUIT.Substring(0, 2) + cCUIT.Substring(3, 8) + cCUIT.Substring(12, 1);
            else
                cAux = cCUIT;


            if (cAux.Length == 11 && int.Parse(cAux.Substring(0, 2)) >= 10)
            {
                    nFactor = 2;
                    for (i = 1; i <= 10; i++)
                    {

                    nValor = int.Parse(cAux.Substring(10 - i, 1));
                        nResultado = nResultado + nValor * nFactor;
                        if (nFactor == 7)
                            nFactor = 1;
                        nFactor = nFactor + 1;
                    }
                    nResultado = nResultado % 11;
                    nResultado = nResultado == 0 ? 0 : 11 - nResultado;
                    
                    return (cAux.Substring(10,1) == nResultado.ToString());
                }
                else
                    return false;

        }

        public static string Calcula(string cSexo, string cDocumento)
        {
            int nResultado=0;
            int nResto;
            int nDigito;
            int i;
            string cClave;
            int nValor;
            int nFactor;
            //int nSuma;
            ///string cAux;
            cDocumento = cDocumento.PadLeft(8, '0'); // String.P(15, cDocumento) Varios.CerosIzq(Conversion.Val(cDocumento), 8);
            
            switch (cSexo.ToUpper())
            {
                case "F":
                    {
                        cClave = "27" + cDocumento;
                        break;
                    }

                case "M":
                    {
                        cClave = "20" + cDocumento;
                        break;
                    }

                case "S":
                    {
                        cClave = "30" + cDocumento;
                        break;
                    }

                case "MFR" // Masculino o Femenino que el resto dio 1
         :
                    {
                        cClave = "23" + cDocumento;
                        break;
                    }

                case "SR":
                    {
                        cClave = "33" + cDocumento;
                        break;
                    }

                default:
                    {
                        return "";
                    }
            }
            nFactor = 2;
            for (i = 1; i <= 10; i++)
            {
                nValor = int.Parse(cClave.Substring(11 - i, 1));
                
                nResultado = nResultado + nValor * nFactor;
                if (nFactor == 7)
                    nFactor = 1;
                nFactor = nFactor + 1;
            }
            nResto = nResultado % 11;
            if (nResto == 1)
                return Calcula((cSexo == "S"? "SR": "MFR"), cDocumento);
            else
            {
                nDigito = (nResto == 0 ? 0 : 11 - nResto);
                return cClave.Substring(0, 2) + "-" + cClave.Substring(3, 8) + "-" + nDigito.ToString().Trim();
                
            }
        }
    }
}
