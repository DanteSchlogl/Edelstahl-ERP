using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Backup
{
    /// <summary>
    /// Ejecuta respaldos completos de SQL Server.
    /// Esta operacion administrativa utiliza ADO.NET.
    /// </summary>
    public class RespaldoDatabaseService
    {
        public string ObtenerCarpetaPredeterminada()
        {
            const string sql =
                @"SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath')
                         AS NVARCHAR(4000))";

            using (SqlConnection connection =
                SqlServerConnection.CreateMasterConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                object resultado = command.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "SQL Server no informó su carpeta predeterminada de respaldos.");
                }

                string carpeta = resultado.ToString().Trim();

                if (string.IsNullOrWhiteSpace(carpeta))
                {
                    throw new InvalidOperationException(
                        "La carpeta predeterminada de respaldos está vacía.");
                }

                return carpeta;
            }
        }

        public ResultadoRespaldo CrearRespaldo(
            string baseDatos,
            string carpetaDestino,
            DateTime fechaHora)
        {
            ValidarNombreBase(baseDatos);

            if (string.IsNullOrWhiteSpace(carpetaDestino))
            {
                throw new ArgumentException(
                    "La carpeta de respaldo es obligatoria.",
                    nameof(carpetaDestino));
            }

            string marcaTiempo = fechaHora.ToString("yyyyMMdd_HHmmss");
            string archivo = baseDatos + "_" + marcaTiempo + ".bak";
            string ruta = Path.Combine(carpetaDestino, archivo);

            const string sql =
                 @"BACKUP DATABASE @BaseDatos
                TO DISK = @Ruta
                WITH COPY_ONLY,
                CHECKSUM,
                 STATS = 10";

            using (SqlConnection connection =
                SqlServerConnection.CreateMasterConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 0;

                command.Parameters.Add(
                    "@BaseDatos",
                    SqlDbType.NVarChar,
                    128).Value = baseDatos;

                command.Parameters.Add(
                    "@Ruta",
                    SqlDbType.NVarChar,
                    4000).Value = ruta;

                connection.Open();
                command.ExecuteNonQuery();
            }

            if (!File.Exists(ruta))
            {
                throw new IOException(
                    "SQL Server informó que el respaldo terminó, " +
                    "pero el archivo no fue encontrado: " + ruta);
            }

            FileInfo informacion = new FileInfo(ruta);

            if (informacion.Length <= 0)
            {
                throw new IOException(
                    "El archivo de respaldo fue creado sin contenido.");
            }

            return new ResultadoRespaldo
            {
                BaseDatos = baseDatos,
                RutaArchivo = ruta,
                FechaHora = fechaHora,
                TamanioBytes = informacion.Length,
                Correcto = true,
                Mensaje = "Respaldo creado correctamente."
            };
        }

        private static void ValidarNombreBase(string baseDatos)
        {
            if (!string.Equals(
                    baseDatos,
                    "EdelstahlNegocio",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    baseDatos,
                    "EdelstahlServicios",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "La base solicitada no está habilitada para respaldo.",
                    nameof(baseDatos));
            }
        }
    }
}
