using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;
using static System.Net.Mime.MediaTypeNames;

namespace CIAPI.Helpers
{
    public static class SecVB6
    {
        public static string Encriptar(string cadena)
        {
            cadena = cadena.Trim();
            int longi= cadena.Length;
            
            int pos = 0;
            var ret = "";
            foreach(char c in cadena)
            {
                pos++;
                var auxcar = (int)c +longi+pos;
                ret = (char)auxcar + ret;

            }


            return ret;
        }

        public static string DesEncriptar(string cadena)
        {
            cadena = cadena.Trim();
            int longi = cadena.Length;            
            int pos = 0;
            var ret = "";
            foreach (char c in cadena)
            {
                pos++;
                var auxcar = (int)c - (longi + (longi - pos)+1);
                ret = (char)auxcar + ret;

            }
            return ret;



  //          cCadena = Trim(cCadena)
  //nLong = Len(cCadena)
  //For nPos = 1 To nLong
  //  cCaracter = Chr(Asc(Mid(cCadena, nPos, 1)) - (nLong + (nLong - nPos) + 1))
  //  DesEncriptar = cCaracter & DesEncriptar
  //Next
        }


        /*
         Public Function Encriptar(ByVal cCadena As String) As String
Dim nLong As Integer, cCaracter As String, nPos As Integer
cCadena = Trim(cCadena)
nLong = Len(cCadena)
For nPos = 1 To nLong
cCaracter = Chr(Asc(Mid(cCadena, nPos, 1)) + (nLong + nPos))
Encriptar = cCaracter & Encriptar
Next
End Function 

            'Pide el string para encriptar y devuelve el string encriptado

'Pide el string Encriptado y devuelve el string desencriptado
Public Function DesEncriptar(ByVal cCadena As String) As String
Dim nLong As Integer, cCaracter As String, nPos As Integer
cCadena = Trim(cCadena)
nLong = Len(cCadena)
For nPos = 1 To nLong
cCaracter = Chr(Asc(Mid(cCadena, nPos, 1)) - (nLong + (nLong - nPos) + 1))
DesEncriptar = cCaracter & DesEncriptar
Next
End Function

         */
    }
}
