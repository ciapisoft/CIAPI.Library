using System.Collections.Generic;

namespace CIAPI.Models
{
    public class ParametroBusquedaPDF
    {
        public string Nombre { get; set; }

        public string Valor { get; set; }


        public bool MantenerPrimerValor { get; set; }

        public int MantenerValorNro { get; set; } = 999999;

        public int ValorNro { get; set; }

        //Usar siempre minusculas
        public List<string> PalabrasClavesTitulo { get; set; }
        public bool UsarLikeParaTitulos;
        public List<string> PalabrasClavesValor { get; set; }
        public bool UsarLikeParaValores;
        //---------------------
        public List<string> RegExs { get; set; }

        public int DesdeLongFija { get; set; }
        public int HastaLongFija { get; set; }

        public TipoDatoConocido Tipo { get; set; }
    }
    public enum TipoDatoConocido { Libre = 0, Numero = 1, Fecha = 2, Importe = 3, ImporteCon2Decimales = 4, CUITL = 5 }
}
