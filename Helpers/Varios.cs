using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CIAPI.Models;
using Org.BouncyCastle.Ocsp;

namespace CIAPI.Helpers
{
    public class PeriodoStruct
    {
        public int Anio { get; set; }
        public int Mes { get; set; }

        public string Periodo
        {

            get
            {
                return String.Format("{0}/{1}", Anio.ToString("0000"), Mes.ToString("00"));
            }
            set
            {
                if (value == null)
                {
                    Anio = 0;
                    Mes = 0;
                }
                else
                    if (Varios.ValidPeriodo(value))
                {
                    Anio = int.Parse(value.Substring(0, 4));
                    Mes = int.Parse(value.Substring(5, 2));
                }

            }
        }

        public DateTime Fecha1erdia
        {
            get
            {
                return new DateTime(Anio, Mes, 1);
            }
        }
        public DateTime UltimodiaMes
        {
            get
            {
                PeriodoStruct periodosig = new PeriodoStruct(Varios.SumaPeriodo(this.Periodo, 1));
                return new DateTime(periodosig.Anio, periodosig.Mes, 1).AddDays(-1);
            }
        }
        public DateTime UltimodiaMesSiguiente
        {
            get
            {
                PeriodoStruct periodosig = new PeriodoStruct(Varios.SumaPeriodo(this.Periodo, 2));
                return new DateTime(periodosig.Anio, periodosig.Mes, 1).AddDays(-1);
            }
        }
        public DateTime UltimodiaMesAnterior
        {
            get
            {
                return new DateTime(Anio, Mes, 1).AddDays(-1);
            }
        }
        public PeriodoStruct(string periodo)
        {
            this.Periodo = periodo;
        }
        public PeriodoStruct(int anio, int mes)
        {
            this.Anio = anio;
            this.Mes = mes;
        }
    }

    public static class Varios
    {
        public static DateTime UltimoDiaMesPeriodo(string periodo)
        {
            PeriodoStruct periodosig = new PeriodoStruct(Varios.SumaPeriodo(periodo, 1));
            return new DateTime(periodosig.Anio, periodosig.Mes, 1).AddDays(-1);
        }
        public static DateTime PrimerDiaMesPeriodo(string periodo)
        {
            PeriodoStruct periodosig = new PeriodoStruct(periodo);
            return new DateTime(periodosig.Anio, periodosig.Mes, 1);
        }
        public static bool IsNumeric(this string s)
        {
            float output;
            return float.TryParse(s, out output);
        }
        public static bool ValidPeriodo(string periodo)
        {
            var ret = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(periodo))
                {
                    var parts = periodo.Split("/");
                    if (parts.Count() == 2)
                    {
                        if (IsNumeric(parts[0]) && IsNumeric(parts[1]))
                        {
                            if (int.Parse(parts[0]) >= 1900 && int.Parse(parts[0]) <= 2199)
                            {
                                ret = (int.Parse(parts[1]) >= 1 && int.Parse(parts[1]) <= 12);
                            }
                        }
                    }
                }
            }
            catch
            {
                ret = false;
            }
            return ret;

        }
        public static string PeriodoActual(int sumaMeses=0)
        {
            var fecha = DateTime.Now.AddMonths(sumaMeses);

            return String.Format("{0}/{1}", fecha.Year.ToString("0000"), fecha.Month.ToString("00"));
        }
        public static string AnioMesToPeriodo(int Anio, int Mes)
        {
            return String.Format("{0}/{1}", Anio.ToString("0000"), Mes.ToString("00"));
        }

        /// <summary>
        /// Obtiene la eddad en años
        /// </summary>
        public static int Edad(DateTime fechaNacimiento,DateTime? fechaHoy = null)
        {
            var fechaHoyConf = fechaHoy==null ? DateTime.Now : fechaHoy.GetValueOrDefault();

            int edad = fechaHoyConf.Year - fechaNacimiento.Year;

            if (fechaHoyConf < fechaNacimiento.AddYears(edad))
                --edad;

            return edad;

        }

        public static string GetFechaAbreviada(DateTime fechahora)
        {
            if (fechahora.Date == DateTime.Now.Date && (fechahora.Hour != 0 || fechahora.Minute != 0))
                return fechahora.ToString("H:mm");
            else
                if (fechahora.Date.Year == DateTime.Now.Date.Year)
                return fechahora.ToString("dd/MM");
            else
                return fechahora.ToString("MM/yy");

        }
        public static string SumaPeriodo(string periodo, int meses = 1)
        {
            if (!string.IsNullOrEmpty(periodo))
            {
                var anio = int.Parse(periodo.Substring(0, 4));
                var mes = int.Parse(periodo.Substring(5, 2));
                mes = mes + meses;
                while (mes > 12)
                {
                    mes -= 12;
                    anio++;
                }
                while (mes < 0)
                {
                    mes += 12;
                    anio--;
                }
                return anio.ToString() + "/" + mes.ToString("00");
            }
            else
            {
                return "0000/00";
            }
        }
        public static int DifPeriodos(string periodoInicial, string periodoFinal)
        {
            int ret = 0;
            var periodo = periodoInicial;
            if(Varios.ValidPeriodo(periodoInicial) && Varios.ValidPeriodo(periodoFinal))
            { 
                while(periodo.CompareTo(periodoFinal)<0)
                {
                    periodo = SumaPeriodo(periodo);
                    ret++;
                }
            }
            else
            { 
                ret = -1;
            }
            return ret;
        }
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
                                            param.ValorNro++;
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
                                                    param.ValorNro++;
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
                    //if(i==180 && param.Nombre=="TipoAutorizacionAFIP")
                    //{
                    //    var a = 1;
                    //}
                    if(param.PalabrasClavesValor!=null)
                    { 
                        var cantpalabras = ContarMaxPalabras(param.PalabrasClavesValor);
                        var frase = "";
                        for(var j=0;j<=cantpalabras; j++)
                        {
                            if(vtexto.Count>i+j)
                            { 
                                frase += (frase == "" ? "" : " ") + vtexto[i + j];
                                if (frase.ToLower() == "nota" || frase.ToLower()=="factura")
                                    Debug.Write("aa");
                                var pclaveOK = AnalisisPalabraClave(frase, param);
                            }
                        }
                    }

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
        private static int ContarMaxPalabras(List<string> listaPalabras)
        {
            var maxPalabras = 0;
            if(listaPalabras!=null)
            { 
                foreach(var palabra in listaPalabras)
                {
                    var conteo = palabra.Count(f => f == ' ');
                    if (conteo+1>maxPalabras)
                    {
                        maxPalabras = conteo+1;
                    }
                }
            }
            return maxPalabras;
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

                    //if (dato == "B-00003-00000300" && param.Nombre=="LetraPuntoyNumero") Debugger.Break();

                    regExEncontrada = rgx.IsMatch(dato);

                    if (regExEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || param.MantenerValorNro>param.ValorNro))
                    {
                        //if (param.Nombre == "PuntoVenta") Debugger.Break();
                        //if (vtexto[i] == "102.628,00") Debugger.Break();

                        if (param.DesdeLongFija != 0)
                        {
                            regExEncontrada = (dato.Length >= param.DesdeLongFija && dato.Length <= param.HastaLongFija);
                        }
                        if (regExEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || param.MantenerValorNro > param.ValorNro))
                        {
                            var textoresult = ValidarTipoDato(dato, param.Tipo); 
                            if (!String.IsNullOrEmpty(textoresult))
                            {
                                param.ValorNro++;
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
                        palabraClaveEncontrada = (palabraClave.ToLower() == dato.ToLower());
                    }
                    if (palabraClaveEncontrada && (String.IsNullOrWhiteSpace(param.Valor) || param.MantenerValorNro > param.ValorNro))
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
                                param.ValorNro++;
                                ret = true;
                                param.Valor = textoresult;
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
                        auxFec = auxFec.Replace(".", "");
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
        public static string Espacios(string valor, int largoTotal) 
        { 
            var falta = largoTotal - valor.Length; 
            var ret = valor; 
            for (int i = 1; i <= falta; i++) 
            { 
                ret = ret + " ";
                
            } 
            return ret;
        }
        /// <summary>
        /// Compara 2 cuit con o sin guiones y devuelve true si son iguales
        /// </summary>
        /// <param name="CUITL1"></param>
        /// <param name="CUITL2"></param>
        /// <returns></returns>
        public static bool CompararCUITLs(string CUITL1, string CUITL2)
        {
            var c1 = CUITL1.Replace("-", "").Trim();
            var c2 = CUITL2.Replace("-", "").Trim();
            return c1 == c2;
        }

        /// <summary>
        /// Ofusca poniendo 3 caracteres random uno real
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string OfuscString(string text, int paso=3)
        {
            var ret = "";
            foreach(var c in text)
            {
                ret += RandomString(paso)+c;
            }
            ret += RandomString(paso);
            return ret;
        }
        public static string DesOfuscString(string text, int paso = 3)
        {
            var ret = "";
            int i = 0;
            foreach (var c in text)
            {
                i++;
                if ((i % (paso+1))==0)
                {
                    ret += c;
                }
            }
            return ret;
        }
        private static readonly Random _random = new Random();
        

        // Generates a random number within a range.      
        public static int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }
        // Generates a random string with a given size.    
        public static string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length=26  

            for (var i = 0; i < size; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        public static string InsertarGuionesAlCUITL(string CUITLSinGuiones)
        {
            if (CUITLSinGuiones.Length < 11)
            {
                CUITLSinGuiones = Varios.CerosIzq(CUITLSinGuiones, 11);
                
            }
            return $"{CUITLSinGuiones.Substring(0, 2)}-{CUITLSinGuiones.Substring(2, 8)}-{CUITLSinGuiones.Substring(10, 1)}";
        }
    }
}
