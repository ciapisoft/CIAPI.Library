using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIAPI.ViewModels
{
    
    public class DataListViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Detalle { get; set; }

        public int Id2 { get; set; }
    }
    public class DataListIdStringViewModel
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
    }
}
