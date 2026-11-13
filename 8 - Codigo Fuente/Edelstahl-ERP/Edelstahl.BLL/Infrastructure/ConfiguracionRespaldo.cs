using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Preferencias del respaldo en este equipo: carpeta elegida, si se hace
    /// un respaldo automático, cada cuántas horas y cuántos se conservan.
    /// Se guardan en %LOCALAPPDATA%\Edelstahl ERP\respaldos.config, que el
    /// usuario de Windows puede escribir (la carpeta del programa no).
    /// </summary>
    public class ConfiguracionRespaldo
    {
        public const int ConservarPorDefecto = 10;
        public const int HorasPorDefecto = 24;

        private const string NombreArchivo = "respaldos.config";

        /// <summary>
        /// Variable de entorno que cambia la carpeta de preferencias; la usan
        /// las pruebas para no tocar las del usuario.
        /// </summary>
        public const string VariableCarpeta = "EDELSTAHL_DATOS_LOCALES";

        private readonly string _ruta;

        /// <summary>
        /// Carpeta elegida por el usuario. Vacía: se usa la de SQL Server.
        /// </summary>
        public string Carpeta { get; set; }

        public bool Automatico { get; set; }

        public int HorasEntreRespaldos { get; set; }

        /// <summary>
        /// Cantidad de respaldos automáticos que se conservan por base.
        /// </summary>
        public int Conservar { get; set; }

        public ConfiguracionRespaldo()
            : this(RutaPorDefecto())
        {
        }

        public ConfiguracionRespaldo(string ruta)
        {
            _ruta = ruta;
            Carpeta = string.Empty;
            Automatico = true;
            HorasEntreRespaldos = HorasPorDefecto;
            Conservar = ConservarPorDefecto;
            Cargar();
        }

        public void Guardar()
        {
            StringBuilder texto = new StringBuilder();
            texto.AppendLine("Carpeta=" + (Carpeta ?? string.Empty));
            texto.AppendLine("Automatico=" + (Automatico ? "1" : "0"));
            texto.AppendLine("HorasEntreRespaldos=" + HorasEntreRespaldos);
            texto.AppendLine("Conservar=" + Conservar);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta));
                File.WriteAllText(_ruta, texto.ToString(), Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Si no se puede guardar, la preferencia vale hasta cerrar el programa.
            }
        }

        private void Cargar()
        {
            Dictionary<string, string> valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(_ruta))
                {
                    return;
                }

                foreach (string linea in File.ReadAllLines(_ruta, Encoding.UTF8))
                {
                    int igual = linea.IndexOf('=');

                    if (igual > 0)
                    {
                        valores[linea.Substring(0, igual).Trim()] = linea.Substring(igual + 1).Trim();
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return;
            }

            string valor;
            int numero;

            if (valores.TryGetValue("Carpeta", out valor))
            {
                Carpeta = valor;
            }

            if (valores.TryGetValue("Automatico", out valor))
            {
                Automatico = valor != "0";
            }

            if (valores.TryGetValue("HorasEntreRespaldos", out valor) && int.TryParse(valor, out numero) && numero > 0)
            {
                HorasEntreRespaldos = numero;
            }

            if (valores.TryGetValue("Conservar", out valor) && int.TryParse(valor, out numero) && numero > 0)
            {
                Conservar = numero;
            }
        }

        private static string RutaPorDefecto()
        {
            string carpeta = Environment.GetEnvironmentVariable(VariableCarpeta);

            if (string.IsNullOrWhiteSpace(carpeta))
            {
                carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Edelstahl ERP");
            }

            return Path.Combine(carpeta, NombreArchivo);
        }
    }
}
