using System;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Security
{
    /// <summary>
    /// Quién creó un respaldo, cuándo, dónde quedó el archivo y si los datos
    /// habían pasado la verificación de integridad en ese momento.
    /// </summary>
    public class RegistroRespaldo : Entity
    {
        public const string OrigenManual = "Manual";
        public const string OrigenAutomatico = "Automatico";
        public const string OrigenAntesDeRestaurar = "AntesDeRestaurar";

        public DateTime FechaHora { get; set; }

        public string BaseDatos { get; set; }

        /// <summary>
        /// Ruta del archivo tal como la escribió SQL Server.
        /// </summary>
        public string RutaSql { get; set; }

        /// <summary>
        /// Ruta de la copia hecha en la carpeta que eligió el usuario
        /// (vacía si no se pidió una carpeta distinta).
        /// </summary>
        public string RutaCopia { get; set; }

        public long TamanioBytes { get; set; }

        public string Origen { get; set; }

        /// <summary>
        /// Verdadero cuando el respaldo se hizo con los dígitos verificadores
        /// correctos. Un respaldo sin esa garantía nunca se ofrece como sano.
        /// </summary>
        public bool Sano { get; set; }

        public string Usuario { get; set; }

        public string Observaciones { get; set; }

        public RegistroRespaldo()
        {
            FechaHora = DateTime.Now;
            BaseDatos = string.Empty;
            RutaSql = string.Empty;
            RutaCopia = string.Empty;
            Origen = OrigenManual;
            Usuario = string.Empty;
            Observaciones = string.Empty;
        }
    }
}
