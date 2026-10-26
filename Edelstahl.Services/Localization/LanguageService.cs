using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Edelstahl.Services.Localization
{
    /// <summary>
    /// Administra el idioma activo y las traducciones
    /// de Edelstahl ERP.
    /// </summary>
    public static class LanguageService
    {
        private const string IdiomaPredeterminado =
            "en-US";

        private const string NombreArchivoConfiguracion =
            "idioma.config";

        private static readonly Dictionary<
            string,
            string> Traducciones =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

        private static string _idiomaActual;

        public static event EventHandler IdiomaCambiado;

        public static string IdiomaActual
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                    _idiomaActual))
                {
                    Inicializar();
                }

                return _idiomaActual;
            }
        }

        public static void Inicializar()
        {
            // El programa siempre arranca en ingles (idioma universal).
            _idiomaActual = IdiomaPredeterminado;
            CargarTraducciones(_idiomaActual);
        }

        /// <summary>
        /// Aplica el idioma elegido por el usuario en una sesion anterior.
        /// Se llama despues de iniciar sesion.
        /// </summary>
        public static void AplicarIdiomaPreferido()
        {
            string guardado = LeerIdiomaGuardado();

            if (ExisteArchivoIdioma(guardado) &&
                !string.Equals(IdiomaActual, guardado, StringComparison.OrdinalIgnoreCase))
            {
                _idiomaActual = guardado;
                CargarTraducciones(_idiomaActual);

                EventHandler manejador = IdiomaCambiado;

                if (manejador != null)
                {
                    manejador(null, EventArgs.Empty);
                }
            }
        }

        public static void CambiarIdioma(
            string languageCode)
        {
            if (string.IsNullOrWhiteSpace(
                languageCode))
            {
                throw new ArgumentException(
                    "The language code is not valid.",
                    nameof(languageCode));
            }

            string codigoNormalizado =
                languageCode.Trim();

            if (!ExisteArchivoIdioma(
                codigoNormalizado))
            {
                throw new FileNotFoundException(
                    "The selected language file was not found.",
                    ObtenerRutaIdioma(
                        codigoNormalizado));
            }

            if (string.Equals(
                IdiomaActual,
                codigoNormalizado,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _idiomaActual =
                codigoNormalizado;

            CargarTraducciones(
                _idiomaActual);

            GuardarIdioma(
                _idiomaActual);

            EventHandler manejador =
                IdiomaCambiado;

            if (manejador != null)
            {
                manejador(
                    null,
                    EventArgs.Empty);
            }
        }

        public static string Translate(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(
                _idiomaActual))
            {
                Inicializar();
            }

            string traduccion;

            if (Traducciones.TryGetValue(
                key.Trim(),
                out traduccion))
            {
                return traduccion;
            }

            return BuscarEnIdiomaRespaldo(
                key.Trim());
        }

        public static string Translate(
            string key,
            string languageCode)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(
                languageCode))
            {
                return key;
            }

            Dictionary<string, string>
                traduccionesIdioma =
                    LeerArchivoIdioma(
                        languageCode.Trim());

            string traduccion;

            if (traduccionesIdioma.TryGetValue(
                key.Trim(),
                out traduccion))
            {
                return traduccion;
            }

            return key;
        }

        public static bool ExisteIdioma(
            string languageCode)
        {
            if (string.IsNullOrWhiteSpace(
                languageCode))
            {
                return false;
            }

            return ExisteArchivoIdioma(
                languageCode.Trim());
        }

        public static string[] ObtenerIdiomasDisponibles()
        {
            return new[]
            {
                "es-AR",
                "es-ES",
                "en-US",
                "en-GB",
                "pt-BR",
                "de-DE",
                "ru-RU"
            };
        }

        private static void CargarTraducciones(
            string languageCode)
        {
            Traducciones.Clear();

            Dictionary<string, string>
                traduccionesArchivo =
                    LeerArchivoIdioma(
                        languageCode);

            foreach (KeyValuePair<string, string>
                traduccion in traduccionesArchivo)
            {
                Traducciones[traduccion.Key] =
                    traduccion.Value;
            }
        }

        private static Dictionary<string, string>
            LeerArchivoIdioma(
                string languageCode)
        {
            Dictionary<string, string>
                resultado =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

            string rutaArchivo =
                ObtenerRutaIdioma(
                    languageCode);

            if (!File.Exists(rutaArchivo))
            {
                return resultado;
            }

            string[] lineas =
                File.ReadAllLines(
                    rutaArchivo,
                    Encoding.UTF8);

            foreach (string lineaOriginal
                in lineas)
            {
                if (string.IsNullOrWhiteSpace(
                    lineaOriginal))
                {
                    continue;
                }

                string linea =
                    lineaOriginal.Trim();

                if (linea.StartsWith("#") ||
                    linea.StartsWith(";"))
                {
                    continue;
                }

                // Los archivos nuevos separan clave y texto con un tabulador
                // (las claves pueden llevar dos puntos); los antiguos usan ':'.
                int posicionSeparador =
                    linea.IndexOf('\t');

                if (posicionSeparador <= 0)
                {
                    posicionSeparador = linea.IndexOf(':');
                }

                if (posicionSeparador <= 0)
                {
                    continue;
                }

                string clave =
                    linea
                        .Substring(
                            0,
                            posicionSeparador)
                        .Trim();

                string valor =
                    linea
                        .Substring(
                            posicionSeparador + 1)
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                    clave))
                {
                    continue;
                }

                resultado[clave] =
                    valor;
            }

            return resultado;
        }

        private static string BuscarEnIdiomaRespaldo(
            string key)
        {
            if (string.Equals(
                _idiomaActual,
                IdiomaPredeterminado,
                StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }

            Dictionary<string, string>
                traduccionesRespaldo =
                    LeerArchivoIdioma(
                        IdiomaPredeterminado);

            string traduccion;

            if (traduccionesRespaldo.TryGetValue(
                key,
                out traduccion))
            {
                return traduccion;
            }

            return key;
        }

        private static bool ExisteArchivoIdioma(
            string languageCode)
        {
            if (string.Equals(languageCode, IdiomaPredeterminado, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string ruta =
                ObtenerRutaIdioma(
                    languageCode);

            return File.Exists(ruta);
        }

        private static string ObtenerRutaIdioma(
            string languageCode)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Languages",
                "idioma." + languageCode);
        }

        private static string ObtenerRutaConfiguracion()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                NombreArchivoConfiguracion);
        }

        private static string LeerIdiomaGuardado()
        {
            try
            {
                string rutaConfiguracion =
                    ObtenerRutaConfiguracion();

                if (!File.Exists(
                    rutaConfiguracion))
                {
                    return IdiomaPredeterminado;
                }

                string idioma =
                    File.ReadAllText(
                        rutaConfiguracion,
                        Encoding.UTF8)
                        .Trim();

                return string.IsNullOrWhiteSpace(
                    idioma)
                        ? IdiomaPredeterminado
                        : idioma;
            }
            catch
            {
                return IdiomaPredeterminado;
            }
        }

        private static void GuardarIdioma(
            string languageCode)
        {
            try
            {
                File.WriteAllText(
                    ObtenerRutaConfiguracion(),
                    languageCode,
                    Encoding.UTF8);
            }
            catch
            {
                // El cambio de idioma continúa funcionando
                // aunque no pueda guardarse la preferencia.
            }
        }
    }
}