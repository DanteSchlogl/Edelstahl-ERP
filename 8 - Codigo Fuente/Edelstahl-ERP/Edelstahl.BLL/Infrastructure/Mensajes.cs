using System;
using Edelstahl.Domain.Exceptions;
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

        public static CUITInvalidoException CuitInvalido(string cuit)
        {
            return new CUITInvalidoException(
                cuit,
                T("The CUIT entered is not valid. It must contain 11 digits."));
        }

        public static ClienteDuplicadoException ClienteDuplicado(string cuit)
        {
            return new ClienteDuplicadoException(
                cuit,
                T("There is already a customer registered with the CUIT {0}.", cuit));
        }
    }
}
