using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Edelstahl.Services.Localization;

namespace Edelstahl.Services.Export
{
    /// <summary>
    /// Patrón Template Method: el esqueleto de la exportación (encabezado,
    /// filas y escritura del archivo) es único; cada exportador solo define
    /// cómo se arman el encabezado y cada fila.
    /// </summary>
    public abstract class ExportadorBase<T>
    {
        public void Exportar(IEnumerable<T> datos, string ruta)
        {
            if (datos == null)
            {
                throw new ArgumentNullException(nameof(datos));
            }

            string carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));

            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            List<string> lineas = new List<string> { ArmarEncabezado() };

            foreach (T elemento in datos)
            {
                lineas.Add(ArmarFila(elemento));
            }

            File.WriteAllLines(ruta, lineas, new UTF8Encoding(true));
        }

        protected abstract string ArmarEncabezado();

        protected abstract string ArmarFila(T elemento);
    }

    /// <summary>
    /// Exporta a CSV cualquier lista de objetos. Usa reflexión para leer las
    /// propiedades de la clase, por eso sirve para tipos que todavía no existen.
    /// </summary>
    public class ExportadorCsv<T> : ExportadorBase<T>
    {
        private const string Separador = ";";

        private static readonly PropertyInfo[] Propiedades = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && EsSimple(p.PropertyType))
            .ToArray();

        protected override string ArmarEncabezado()
        {
            return string.Join(Separador, Propiedades.Select(p => Escapar(LanguageService.Translate(p.Name))));
        }

        protected override string ArmarFila(T elemento)
        {
            return string.Join(Separador, Propiedades.Select(p => Escapar(Texto(p.GetValue(elemento, null)))));
        }

        private static bool EsSimple(Type tipo)
        {
            Type real = Nullable.GetUnderlyingType(tipo) ?? tipo;

            return real.IsPrimitive || real.IsEnum || real == typeof(string) || real == typeof(decimal)
                || real == typeof(DateTime) || real == typeof(Guid) || real == typeof(TimeSpan);
        }

        private static string Texto(object valor)
        {
            if (valor == null)
            {
                return string.Empty;
            }

            if (valor is DateTime)
            {
                return ((DateTime)valor).ToString("dd/MM/yyyy HH:mm");
            }

            if (valor is decimal)
            {
                return ((decimal)valor).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

            return Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Escapar(string texto)
        {
            if (texto.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + texto.Replace("\"", "\"\"") + "\"";
            }

            return texto;
        }
    }

    /// <summary>
    /// Serialización XML de listas de datos (exportar y volver a leer).
    /// </summary>
    public static class SerializadorXml
    {
        public static void Guardar<T>(IEnumerable<T> datos, string ruta)
        {
            string carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));

            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            XmlSerializer serializador = new XmlSerializer(typeof(List<T>));

            using (StreamWriter escritor = new StreamWriter(ruta, false, new UTF8Encoding(true)))
            {
                serializador.Serialize(escritor, datos.ToList());
            }
        }

        public static List<T> Cargar<T>(string ruta)
        {
            XmlSerializer serializador = new XmlSerializer(typeof(List<T>));

            using (StreamReader lector = new StreamReader(ruta, Encoding.UTF8))
            {
                return (List<T>)serializador.Deserialize(lector);
            }
        }
    }
}
