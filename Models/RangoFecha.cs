using System;

namespace CIAPI.Models
{
    public class RangoFecha
    {
        public DateTime FechaDesde { get; set; } = new DateTime(1900, 1, 1);
        public DateTime FechaHasta { get; set; } = DateTime.Now;
    }
}
