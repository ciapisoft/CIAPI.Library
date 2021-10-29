using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CIAPI.Models;

namespace CIAPI.Helpers
{
    public class Varios
    {

        public static void ExtraerInfoPDF(List<string> vtexto, List<ParametroBusquedaPDF> parambusq)
        {
            for(var i=0;i<vtexto.Count; i++)
            {
                foreach(var param in parambusq)
                {


                    //Revisamos titulos
                    var tituloEncontrado = false;
                    if(param.PalabrasClavesTitulo!=null)
                    { 
                        foreach (var titulo in param.PalabrasClavesTitulo)
                        {
                            if(param.UsarLikeParaTitulos)
                            {
                                tituloEncontrado = vtexto[i].ToLower().Contains(titulo.ToLower());
                            }
                            else
                            {
                                tituloEncontrado = (titulo.ToLower()== vtexto[i].ToLower());
                            }
                            if (tituloEncontrado) //Analizamos lo que viene luego
                                if (i + 1 < vtexto.Count)
                                {
                                    var datoOk = false;
                                    if (param.PalabrasClavesValor == null && param.RegExs == null)
                                    {

                                        var textoresult = ValidarTipoDato(vtexto[i + 1], param.Tipo);
                                        if (!String.IsNullOrEmpty(textoresult))
                                        {
                                            param.Valor = textoresult;
                                            datoOk = true;
                                        }
                                        else
                                        {
                                            //Verificamos Fecha Partida
                                            if(i+3<vtexto.Count && param.Tipo==TipoDatoConocido.Fecha)
                                            {

                                                textoresult = ValidarTipoDato(CerosIzq(vtexto[i + 1], 2) + "/" + CerosIzq(vtexto[i + 2], 2) + "/" + vtexto[i + 3], param.Tipo);
                                                if (!String.IsNullOrEmpty(textoresult))
                                                {
                                                    param.Valor = textoresult;
                                                    datoOk = true;
                                                }
                                            }
                                        }

                                        

                                    }
                                    else
                                    {
                                        var palabraclave = AnalisisPalabraClave(vtexto[i + 1], param);
                                        if (!palabraclave)
                                        {
                                            var regExOK = AnalisisRegExs(vtexto[i + 1], param);
                                            datoOk = regExOK;
                                        }
                                    }
                                    if (datoOk)
                                    {
                                        i++;
                                        continue;
                                    }
                                }
                        }

    
                    }
                    //-------------------


                    //-------------------------
                    //PALABRAS CLAVES ENCONTRADAS
                    var pclaveOK = AnalisisPalabraClave(vtexto[i], param);

                    //var palabraClaveEncontrada = false;
                    //if (param.PalabrasClavesValor != null)
                    //{
                    //    foreach (var palabraClave in param.PalabrasClavesValor)
                    //    {
                    //        if (param.UsarLikeParaValores)
                    //        {
                    //            palabraClaveEncontrada = vtexto[i].ToLower().Contains(palabraClave);
                    //        }
                    //        else
                    //        {
                    //            palabraClaveEncontrada = (palabraClave == vtexto[i].ToLower());
                    //        }
                    //        if (palabraClaveEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || !param.MantenerPrimerValor))
                    //        {
                    //            if(param.DesdeLongFija!=0)
                    //            {
                    //                palabraClaveEncontrada = (vtexto[i].Length>=param.DesdeLongFija && vtexto[i].Length<=param.HastaLongFija);
                    //            }


                    //            if (palabraClaveEncontrada)
                    //            {
                    //                var textoresult = ValidarTipoDato(vtexto[i], param.Tipo);
                    //                if (!String.IsNullOrEmpty(textoresult)) param.Valor = textoresult;
                    //            }
                    //        }
                    //    }
                    //}
                    //-------------------

                    var regExs = AnalisisRegExs(vtexto[i], param);
                    //-------------------------
                    //REGEX ENCONTRADAS
                    //var regExEncontrada = false;
                    //if (param.RegExs != null)
                    //{
                    //    foreach (var regexv in param.RegExs)
                    //    {

                    //        Regex rgx = new Regex(regexv);

                    //        //if (vtexto[i] == "102.628,00" && param.Nombre=="MontoTotal") Debugger.Break();

                    //        regExEncontrada = rgx.IsMatch(vtexto[i]);

                    //        if (regExEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || !param.MantenerPrimerValor))
                    //        {
                    //            //if (param.Nombre == "PuntoVenta") Debugger.Break();
                    //            //if (vtexto[i] == "102.628,00") Debugger.Break();

                    //            if (param.DesdeLongFija != 0)
                    //            {
                    //                regExEncontrada = (vtexto[i].Length >= param.DesdeLongFija && vtexto[i].Length <= param.HastaLongFija);
                    //            }
                    //            if(regExEncontrada)
                    //            {
                    //                var textoresult = ValidarTipoDato(vtexto[i], param.Tipo);
                    //                if (!String.IsNullOrEmpty(textoresult)) param.Valor = textoresult;
                    //            }
                    //        }
                    //    }
                    //}
                    //-------------------



                }
            }
        }
        private static bool AnalisisRegExs(string dato, ParametroBusquedaPDF param)
        {
            var ret = false;
            var regExEncontrada = false;
            if (param.RegExs != null)
            {
                foreach (var regexv in param.RegExs)
                {

                    Regex rgx = new Regex(regexv);

                    //if (vtexto[i] == "102.628,00" && param.Nombre=="MontoTotal") Debugger.Break();

                    regExEncontrada = rgx.IsMatch(dato);

                    if (regExEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || !param.MantenerPrimerValor))
                    {
                        //if (param.Nombre == "PuntoVenta") Debugger.Break();
                        //if (vtexto[i] == "102.628,00") Debugger.Break();

                        if (param.DesdeLongFija != 0)
                        {
                            regExEncontrada = (dato.Length >= param.DesdeLongFija && dato.Length <= param.HastaLongFija);
                        }
                        if (regExEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || !param.MantenerPrimerValor))
                        {
                            var textoresult = ValidarTipoDato(dato, param.Tipo);
                            if (!String.IsNullOrEmpty(textoresult))
                            {
                                param.Valor = textoresult;
                                ret = true;
                            }
                        }
                    }
                }
            }
            return ret;
        }


        private static bool AnalisisPalabraClave(string dato,ParametroBusquedaPDF param)
        {
            var palabraClaveEncontrada = false;
            var ret=false;
            if (param.PalabrasClavesValor != null)
            {
                foreach (var palabraClave in param.PalabrasClavesValor)
                {
                    if (param.UsarLikeParaValores)
                    {
                        palabraClaveEncontrada = dato.ToLower().Contains(palabraClave);
                    }
                    else
                    {
                        palabraClaveEncontrada = (palabraClave == dato.ToLower());
                    }
                    if (palabraClaveEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || !param.MantenerPrimerValor))
                    {
                        if (param.DesdeLongFija != 0)
                        {
                            palabraClaveEncontrada = (dato.Length >= param.DesdeLongFija && dato.Length <= param.HastaLongFija);
                        }


                        if (palabraClaveEncontrada)
                        {
                            var textoresult = ValidarTipoDato(dato, param.Tipo);
                            if (!String.IsNullOrEmpty(textoresult))
                            { 
                                param.Valor = textoresult;
                                ret = true;
                            }
                        }
                    }
                }
            }
            return ret;
        }
        private static string ValidarTipoDato(string dato, TipoDatoConocido tipo)
        {
            var ret = "";
            switch (tipo)
            {
                case TipoDatoConocido.CUITL :
                    {
                        var okCuit = Helpers.CUITL.Verifica(dato);
                        if (okCuit)
                        {
                            if (dato.Length == 11)
                                ret = dato.Substring(0,2) + "-" + dato.Substring(2, 8) + "-" + dato.Substring(10, 1);
                            else
                                ret = dato;
                        }

                    }
                    break;
                case TipoDatoConocido.Fecha:
                    {

                        var auxFec = dato.Replace("/", "");
                        auxFec = auxFec.Replace("-", "");

                        if (auxFec.Length == 8)
                        {
                            var p1 = auxFec.Substring(0, 2);
                            var p2 = auxFec.Substring(2, 2);
                            var p3 = auxFec.Substring(4, 4);
                            int dia=0, mes, anio;
                            if (int.TryParse(p1, out var p1n))
                            {
                                if (p1n >= 1 && p1n <= 31)
                                {
                                    dia = p1n;
                                }

                                if (int.TryParse(p2, out var p2n))
                                {
                                    if (p2n >= 1 && p2n <= 12)
                                    {
                                        mes = p2n;
                                    }
                                    else
                                    {
                                        dia = p2n;
                                        mes = p1n;

   
                                    }
                                    if (int.TryParse(p3, out var p3n))
                                    {
                                        if (p3n >= DateTime.MinValue.Year && p3n <= DateTime.MaxValue.Year)
                                        {
                                            anio = p3n;
                                            var fecharet = new DateTime(anio, mes, dia);
                                            ret = fecharet.ToString("dd/MM/yyyy");
                                        }
                                    }
                                }
                            }
                        }
                        
                    }
                    break;
                case TipoDatoConocido.Importe:
                    {
                        if(Decimal.TryParse(dato, out var result))
                         {

                                ret = result.ToString();
                            
                         }
                     }
                    break;
                case TipoDatoConocido.ImporteCon2Decimales:
                    {
                        if(dato.Length>=4)
                        { 
                            var sepDec = dato.Substring(dato.Length - 3,1);
                            if (sepDec=="." || sepDec=="," )
                            {
                                var num = "";
                                foreach(var c in dato)
                                {
                                    if (!(c == '.' || c == ','))
                                    { 
                                
                                        num += c.ToString();
                                    }
                                }
                                num = num.Substring(0, num.Length - 2) + "," + num.Substring(num.Length - 2, 2);
                                if(float.TryParse(num, out var result2))
                                {
                                    ret = result2.ToString();
                                }
                            }
                        }
                    }
                    break;
                case TipoDatoConocido.Numero:
                
                   if(long.TryParse(dato, out var result3))
                    {
                        ret = dato;
                    }
                    break;

                default: //Libre
                    ret = dato;
                    break;
            }
            return ret;
        }
        public static string CerosIzq(string valor, int largoTotal)
        {
            valor = valor.Trim();
            var falta = largoTotal - valor.Length;
            var ret = valor;
            for (int i = 1; i <= falta; i++)
            {
                ret = "0" + ret;
            }
            return ret;
        }
    }
}
