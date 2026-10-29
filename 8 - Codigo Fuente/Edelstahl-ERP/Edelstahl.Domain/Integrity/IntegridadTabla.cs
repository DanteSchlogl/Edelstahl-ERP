using System;

namespace Edelstahl.Domain.Integrity
{
    /// <summary>
    /// Almacena el Dígito Verificador Vertical
    /// correspondiente a una tabla controlada.
    /// </summary>
    public class IntegridadTabla
    {
        /// <summary>
        /// Nombre lógico de la tabla protegida.
        /// Funciona como clave primaria.
        /// </summary>
        public string NombreTabla
        {
            get;
            set;
        }

        /// <summary>
        /// Hash SHA-256 calculado a partir de los
        /// DVH de los registros de la tabla.
        /// </summary>
        public string DVV
        {
            get;
            set;
        }

        /// <summary>
        /// Fecha de la última actualización válida.
        /// </summary>
        public DateTime FechaActualizacion
        {
            get;
            set;
        }

        public IntegridadTabla()
        {
            NombreTabla =
                string.Empty;

            DVV =
                string.Empty;

            FechaActualizacion =
                DateTime.Now;
        }
    }
}
