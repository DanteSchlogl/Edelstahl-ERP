using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;

namespace Edelstahl.Services.Localization
{
    /// <summary>
    /// Administra el idioma activo y las traducciones de Edelstahl ERP.
    ///
    /// Los textos están en archivos de recursos (.resx) incluidos en este
    /// ensamblado: Textos.resx (inglés, idioma neutro) y un Textos.[cultura].resx
    /// por cada idioma, que el compilador convierte en un ensamblado satélite
    /// por cultura. La clave de cada texto es el texto en inglés; si el idioma
    /// activo no tiene la traducción se muestra el inglés.
    /// </summary>
    public static class LanguageService
    {
        private const string IdiomaPredeterminado =
            "en-US";

        private const string NombreArchivoConfiguracion =
            "idioma.config";

        /// <summary>
        /// Variable de entorno que cambia la carpeta de preferencias del usuario.
        /// </summary>
        public const string VariableCarpetaDatos =
            "EDELSTAHL_DATOS_LOCALES";

        private static readonly string[] Idiomas =
        {
            "es-AR",
            "es-ES",
            "en-US",
            "en-GB",
            "pt-BR",
            "de-DE",
            "ru-RU"
        };

        private static readonly ResourceManager Recursos =
            new ResourceManager(
                "Edelstahl.Services.Localization.Textos",
                typeof(LanguageService).Assembly);

        /// <summary>
        /// Textos ya leídos de cada idioma, para no recorrer los recursos en
        /// cada traducción.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache =
            new ConcurrentDictionary<string, Dictionary<string, string>>(
                StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string> _traducciones =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            _traducciones = LeerRecursos(_idiomaActual);
        }

        /// <summary>
        /// Aplica el idioma elegido por el usuario en una sesion anterior.
        /// Se llama despues de iniciar sesion.
        /// </summary>
        public static void AplicarIdiomaPreferido()
        {
            string guardado = LeerIdiomaGuardado();

            if (ExisteIdioma(guardado) &&
                !string.Equals(IdiomaActual, guardado, StringComparison.OrdinalIgnoreCase))
            {
                _idiomaActual = guardado;
                _traducciones = LeerRecursos(_idiomaActual);

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

            if (!ExisteIdioma(
                codigoNormalizado))
            {
                throw new ArgumentException(
                    "The selected language is not available.",
                    nameof(languageCode));
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

            _traducciones =
                LeerRecursos(
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

            if (_traducciones.TryGetValue(
                key.Trim(),
                out traduccion))
            {
                return traduccion;
            }

            return key.Trim();
        }

        /// <summary>
        /// Traduce la clave al idioma activo y reemplaza {0}, {1}... por los valores.
        /// </summary>
        public static string Format(
            string key,
            params object[] valores)
        {
            string texto = Translate(key);

            if (valores == null || valores.Length == 0)
            {
                return texto;
            }

            try
            {
                return string.Format(texto, valores);
            }
            catch (FormatException)
            {
                return texto;
            }
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
                    LeerRecursos(
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

            return Array.Exists(
                Idiomas,
                idioma => string.Equals(
                    idioma,
                    languageCode.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public static string[] ObtenerIdiomasDisponibles()
        {
            return (string[])Idiomas.Clone();
        }

        /// <summary>
        /// Lee los textos de un idioma desde los recursos: primero el neutro
        /// (inglés), luego el idioma general (por ejemplo "es") y al final el
        /// específico (por ejemplo "es-AR"), de modo que lo más específico
        /// reemplaza a lo general.
        /// </summary>
        private static Dictionary<string, string> LeerRecursos(
            string languageCode)
        {
            return Cache.GetOrAdd(
                languageCode,
                codigo =>
                {
                    Dictionary<string, string> resultado =
                        new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase);

                    CultureInfo cultura;

                    try
                    {
                        cultura = new CultureInfo(codigo);
                    }
                    catch (CultureNotFoundException)
                    {
                        return resultado;
                    }

                    List<CultureInfo> cadena = new List<CultureInfo>();

                    for (CultureInfo actual = cultura;
                        !actual.Equals(CultureInfo.InvariantCulture);
                        actual = actual.Parent)
                    {
                        cadena.Add(actual);
                    }

                    cadena.Add(CultureInfo.InvariantCulture);
                    cadena.Reverse();

                    foreach (CultureInfo cadaCultura in cadena)
                    {
                        ResourceSet conjunto;

                        try
                        {
                            conjunto = Recursos.GetResourceSet(
                                cadaCultura,
                                true,
                                false);
                        }
                        catch (MissingManifestResourceException)
                        {
                            continue;
                        }

                        if (conjunto == null)
                        {
                            continue;
                        }

                        foreach (DictionaryEntry entrada in conjunto)
                        {
                            string clave = entrada.Key as string;
                            string valor = entrada.Value as string;

                            if (!string.IsNullOrWhiteSpace(clave) && valor != null)
                            {
                                resultado[clave] = valor;
                            }
                        }
                    }

                    return resultado;
                });
        }

        /// <summary>
        /// La preferencia se guarda en la carpeta de datos del usuario de Windows
        /// (la carpeta de instalacion, en Program Files, no admite escritura).
        /// </summary>
        private static string ObtenerRutaConfiguracion()
        {
            // La variable la usan las pruebas automaticas para no tocar las preferencias reales.
            string carpeta = Environment.GetEnvironmentVariable(VariableCarpetaDatos);

            if (string.IsNullOrWhiteSpace(carpeta))
            {
                carpeta = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "Edelstahl ERP");
            }

            return Path.Combine(carpeta, NombreArchivoConfiguracion);
        }

        /// <summary>Ubicacion usada por las versiones anteriores, junto al programa.</summary>
        private static string ObtenerRutaConfiguracionAnterior()
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
                    rutaConfiguracion =
                        ObtenerRutaConfiguracionAnterior();
                }

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
                string ruta =
                    ObtenerRutaConfiguracion();

                Directory.CreateDirectory(
                    Path.GetDirectoryName(ruta));

                File.WriteAllText(
                    ruta,
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
