using System;
using System.Text;
using Edelstahl.Services.Localization;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Acceso corto a las traducciones. La clave es el texto en ingles.
    /// </summary>
    public static class Tr
    {
        public static string T(string clave)
        {
            return LanguageService.Translate(clave);
        }

        public static string T(string clave, params object[] argumentos)
        {
            string texto = LanguageService.Translate(clave);

            try
            {
                return string.Format(texto, argumentos);
            }
            catch (FormatException)
            {
                return texto;
            }
        }

        /// <summary>
        /// Convierte un nombre de propiedad en un titulo de columna
        /// (por ejemplo "FechaEmision" pasa a "Fecha Emision" y luego se traduce).
        /// </summary>
        public static string Columna(string nombrePropiedad)
        {
            if (string.IsNullOrEmpty(nombrePropiedad))
            {
                return string.Empty;
            }

            StringBuilder texto = new StringBuilder();

            for (int i = 0; i < nombrePropiedad.Length; i++)
            {
                char c = nombrePropiedad[i];

                if (i > 0 && char.IsUpper(c) && !char.IsUpper(nombrePropiedad[i - 1]))
                {
                    texto.Append(' ');
                }

                texto.Append(c);
            }

            return T(Nombres.Columna(texto.ToString()));
        }
    }
}
