using System;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.Services.Localization;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Mensajes de negocio traducibles. La clave es el texto en inglés;
    /// si el idioma activo no tiene traducción se muestra ese mismo texto.
    /// </summary>
    public static class Mensajes
    {
        public static string T(string clave, params object[] argumentos)
        {
            string texto = LanguageService.Translate(clave);

            if (argumentos == null || argumentos.Length == 0)
            {
                return texto;
            }

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
        /// Crea una excepción de regla de negocio con el mensaje traducido.
        /// </summary>
        public static ReglaDeNegocioException Regla(string clave, params object[] argumentos)
        {
            return new ReglaDeNegocioException(T(clave, argumentos));
        }
    }
}
