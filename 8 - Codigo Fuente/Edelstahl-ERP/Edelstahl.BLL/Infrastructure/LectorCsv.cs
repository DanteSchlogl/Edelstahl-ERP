using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Lector de archivos CSV con separador configurable. Admite campos entre
    /// comillas, comillas duplicadas y saltos de línea dentro de un campo.
    /// Detecta si el archivo está en UTF-8 o en Windows-1252.
    /// </summary>
    public static class LectorCsv
    {
        public static List<string[]> Leer(string ruta, char separador)
        {
            if (!File.Exists(ruta))
            {
                throw Mensajes.Regla("The file {0} does not exist.", ruta);
            }

            return Analizar(LeerTexto(ruta), separador);
        }

        public static string LeerTexto(string ruta)
        {
            byte[] bytes = File.ReadAllBytes(ruta);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }

            try
            {
                UTF8Encoding estricto = new UTF8Encoding(false, true);
                return estricto.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
        }

        public static List<string[]> Analizar(string texto, char separador)
        {
            List<string[]> filas = new List<string[]>();
            List<string> fila = new List<string>();
            StringBuilder campo = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];

                if (enComillas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < texto.Length && texto[i + 1] == '"')
                        {
                            campo.Append('"');
                            i++;
                        }
                        else
                        {
                            enComillas = false;
                        }
                    }
                    else
                    {
                        campo.Append(c);
                    }
                }
                else if (c == '"')
                {
                    enComillas = true;
                }
                else if (c == separador)
                {
                    fila.Add(campo.ToString());
                    campo.Clear();
                }
                else if (c == '\n')
                {
                    fila.Add(campo.ToString().TrimEnd('\r'));
                    campo.Clear();
                    AgregarFila(filas, fila);
                    fila = new List<string>();
                }
                else
                {
                    campo.Append(c);
                }
            }

            if (campo.Length > 0 || fila.Count > 0)
            {
                fila.Add(campo.ToString().TrimEnd('\r'));
                AgregarFila(filas, fila);
            }

            return filas;
        }

        private static void AgregarFila(List<string[]> filas, List<string> fila)
        {
            bool vacia = fila.Count == 1 && string.IsNullOrWhiteSpace(fila[0]);

            if (!vacia)
            {
                filas.Add(fila.ToArray());
            }
        }

        /// <summary>
        /// Devuelve el índice de la primera columna cuyo encabezado coincide,
        /// sin distinguir mayúsculas ni acentos.
        /// </summary>
        public static int Columna(string[] encabezado, params string[] nombres)
        {
            for (int i = 0; i < encabezado.Length; i++)
            {
                string actual = Normalizar(encabezado[i]);

                foreach (string nombre in nombres)
                {
                    if (actual == Normalizar(nombre))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static string Valor(string[] fila, int indice)
        {
            return indice >= 0 && indice < fila.Length ? fila[indice].Trim() : string.Empty;
        }

        public static string Normalizar(string texto)
        {
            string descompuesto = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder();

            foreach (char c in descompuesto)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(c);
                }
            }

            return resultado.ToString();
        }
    }
}
