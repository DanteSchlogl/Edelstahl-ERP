using System;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Reconoce las direcciones que en realidad no existen (vacias o con el texto de relleno de la carga masiva).
    /// </summary>
    public static class Direcciones
    {
        public const string SinDireccion = "Address not provided";

        public static bool Falta(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
            {
                return true;
            }

            string d = direccion.Trim();

            return string.Equals(d, SinDireccion, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d, Mensajes.T(SinDireccion), StringComparison.OrdinalIgnoreCase)
                || string.Equals(d, "Dirección no informada", StringComparison.OrdinalIgnoreCase);
        }
    }
}
