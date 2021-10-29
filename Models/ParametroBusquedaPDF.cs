using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIAPI.Models
{
    public class ParametroBusquedaPDF
    {
        public string Nombre { get; set; }

        public string Valor { get; set; }

        public bool MantenerPrimerValor { get; set; }

        //Usar siempre minusculas
        public List<string> PalabrasClavesTitulo { get; set; }
        public bool UsarLikeParaTitulos;
        public List<string> PalabrasClavesValor { get; set; }
        public bool UsarLikeParaValores;
        //---------------------
        public List<string> RegExs { get; set; }

        public int DesdeLongFija {get;set;}
        public int HastaLongFija { get; set; }

        public TipoDatoConocido Tipo { get; set; }
    }
    public enum TipoDatoConocido { Libre=0, Numero=1, Fecha=2, Importe=3, ImporteCon2Decimales=4, CUITL=5}
}
