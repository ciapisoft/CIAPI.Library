using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace CIAPI.Helpers
{

    public class ARCAQrGenerator
    {
        public static string GenerarUrlQr(
            int version,
            DateTime fecha,
            long cuit,
            int puntoVenta,
            int tipoComprobante,
            int numeroComprobante,
            decimal importe,
            string moneda,
            decimal cotizacion,
            int tipoDocReceptor,
            long nroDocReceptor,
            string tipoCodAut,
            long codAutorizacion)
        {
            var datos = new
            {
                ver = version,
                fecha = fecha.ToString("yyyy-MM-dd"),
                cuit = cuit,
                ptoVta = puntoVenta,
                tipoCmp = tipoComprobante,
                nroCmp = numeroComprobante,
                importe = Math.Round(importe, 2),
                moneda = moneda,
                ctz = cotizacion,
                tipoDocRec = tipoDocReceptor,
                nroDocRec = nroDocReceptor,
                tipoCodAut = tipoCodAut,
                codAut = codAutorizacion
            };

            string json = JsonSerializer.Serialize(datos);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            string url = $"https://www.arca.gob.ar/fe/qr/?p={base64}";

            return url;
        }
    }
}
