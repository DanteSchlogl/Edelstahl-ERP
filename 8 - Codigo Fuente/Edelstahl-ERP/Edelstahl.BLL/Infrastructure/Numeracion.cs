using System;
using System.Collections.Generic;
using System.Globalization;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Genera números correlativos con el formato PREFIJO-AAAA-NNNNNN.
    /// </summary>
    public static class Numeracion
    {
        public static string Siguiente(string prefijo, IEnumerable<string> existentes)
        {
            string raiz = prefijo + "-" + DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) + "-";
            int maximo = 0;

            foreach (string numero in existentes)
            {
                if (numero == null || !numero.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int valor;

                if (int.TryParse(numero.Substring(raiz.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out valor) && valor > maximo)
                {
                    maximo = valor;
                }
            }

            return raiz + (maximo + 1).ToString("D6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Numeración fiscal: punto de venta de cuatro cifras y número de ocho.
        /// </summary>
        public static string SiguienteFiscal(int puntoVenta, IEnumerable<string> existentes)
        {
            string raiz = puntoVenta.ToString("D4", CultureInfo.InvariantCulture) + "-";
            int maximo = 0;

            foreach (string numero in existentes)
            {
                if (numero == null || !numero.StartsWith(raiz, StringComparison.Ordinal))
                {
                    continue;
                }

                int valor;

                if (int.TryParse(numero.Substring(raiz.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out valor) && valor > maximo)
                {
                    maximo = valor;
                }
            }

            return raiz + (maximo + 1).ToString("D8", CultureInfo.InvariantCulture);
        }
    }
}
