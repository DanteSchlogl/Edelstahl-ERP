using System;

namespace Edelstahl.Domain.Security
{
    public class ResultadoRespaldo
    {
        public string BaseDatos { get; set; }
        public string RutaArchivo { get; set; }
        public DateTime FechaHora { get; set; }
        public long TamanioBytes { get; set; }
        public bool Correcto { get; set; }
        public string Mensaje { get; set; }

        public ResultadoRespaldo()
        {
            BaseDatos = string.Empty;
            RutaArchivo = string.Empty;
            FechaHora = DateTime.Now;
            Mensaje = string.Empty;
        }
    }
}
