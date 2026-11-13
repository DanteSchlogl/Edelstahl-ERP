using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Backup
{
    /// <summary>
    /// Crea, verifica y restaura respaldos completos de SQL Server.
    /// Es una operación administrativa y se ejecuta sobre la base master.
    /// </summary>
    internal class RespaldoDatabaseService : IRespaldoDatabase
    {
        /// <summary>
        /// Error 3201 de SQL Server: el servicio no pudo abrir el archivo de
        /// respaldo (no existe o su cuenta no tiene permiso sobre la carpeta).
        /// </summary>
        private const int ErrorDispositivoNoAbierto = 3201;

        /// <summary>
        /// Nombre de la base de negocio, la que se restaura ante una alteración.
        /// </summary>
        public string BaseNegocio
        {
            get { return SqlServerConnection.NombreBaseNegocio; }
        }

        public string BaseServicios
        {
            get { return SqlServerConnection.NombreBaseServicios; }
        }

        public string ObtenerCarpetaPredeterminada()
        {
            string carpeta = SqlHelper.EjecutarEscalar<string>(
                BaseDeDatos.Master,
                @"SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000))");

            if (string.IsNullOrWhiteSpace(carpeta))
            {
                throw new RespaldoException(
                    LanguageService.Translate("SQL Server did not report its default backup folder."));
            }

            return carpeta.Trim();
        }

        /// <summary>
        /// Hace un respaldo completo (copia solamente: no altera la cadena de
        /// respaldos de SQL Server) con suma de verificación y comprueba el
        /// archivo resultante.
        /// </summary>
        public ResultadoRespaldo CrearRespaldo(
            string baseDatos,
            string carpetaDestino,
            DateTime fechaHora)
        {
            ValidarNombreBase(baseDatos);

            if (string.IsNullOrWhiteSpace(carpetaDestino))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The backup folder is required."));
            }

            // Con los milisegundos, dos respaldos seguidos nunca comparten archivo (INIT lo pisaria).
            string archivo = baseDatos + "_" + fechaHora.ToString("yyyyMMdd_HHmmss_fff") + ".bak";
            string ruta = CombinarRuta(carpetaDestino, archivo);

            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Master,
                0,
                @"BACKUP DATABASE @BaseDatos
                  TO DISK = @Ruta
                  WITH COPY_ONLY, CHECKSUM, INIT, STATS = 10",
                SqlHelper.Parametro("@BaseDatos", SqlDbType.NVarChar, 128, baseDatos),
                SqlHelper.Parametro("@Ruta", SqlDbType.NVarChar, 4000, ruta));

            // SQL Server mismo lee el archivo: así se comprueba también cuando la
            // carpeta no se puede ver desde el usuario de Windows.
            VerificarArchivo(ruta);

            return new ResultadoRespaldo
            {
                BaseDatos = baseDatos,
                RutaArchivo = ruta,
                FechaHora = fechaHora,
                TamanioBytes = ObtenerTamanio(baseDatos, ruta),
                Correcto = true,
                Mensaje = LanguageService.Translate("Backup created correctly.")
            };
        }

        /// <summary>
        /// Comprueba que el archivo sea un respaldo completo y legible
        /// (RESTORE VERIFYONLY con suma de verificación).
        /// </summary>
        public void VerificarArchivo(string ruta)
        {
            ValidarRuta(ruta);

            try
            {
                SqlHelper.EjecutarNoQuery(
                    BaseDeDatos.Master,
                    0,
                    "RESTORE VERIFYONLY FROM DISK = @Ruta WITH CHECKSUM",
                    SqlHelper.Parametro("@Ruta", SqlDbType.NVarChar, 4000, ruta));
            }
            catch (AccesoADatosException ex)
            {
                throw new RespaldoException(
                    LanguageService.Format(
                        "The backup file is not valid or cannot be read by SQL Server: {0}",
                        ex.InnerException != null ? ex.InnerException.Message : ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// Nombre de la base de la que se hizo el respaldo (para no restaurar
        /// por error el archivo de otra base).
        /// </summary>
        public string ObtenerBaseDelArchivo(string ruta)
        {
            ValidarRuta(ruta);

            List<string> bases = SqlHelper.Consultar(
                BaseDeDatos.Master,
                "RESTORE HEADERONLY FROM DISK = @Ruta",
                fila => Convert.ToString(fila["DatabaseName"]),
                SqlHelper.Parametro("@Ruta", SqlDbType.NVarChar, 4000, ruta));

            return bases.Count == 0 ? string.Empty : bases[0];
        }

        /// <summary>
        /// Reemplaza la base indicada por el contenido del respaldo. La base
        /// pasa a un solo usuario mientras dura la restauración (se cierran las
        /// conexiones abiertas) y vuelve a multiusuario al terminar, aunque
        /// falle.
        /// </summary>
        public void Restaurar(string baseDatos, string ruta)
        {
            ValidarNombreBase(baseDatos);
            ValidarRuta(ruta);

            string seguro = baseDatos.Replace("]", "]]");
            string movimientos = ObtenerMovimientosDeArchivos(baseDatos, ruta);

            string sql =
                "ALTER DATABASE [" + seguro + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\n" +
                "BEGIN TRY\n" +
                "  RESTORE DATABASE [" + seguro + "] FROM DISK = @Ruta WITH REPLACE, CHECKSUM" + movimientos + ";\n" +
                "END TRY\n" +
                "BEGIN CATCH\n" +
                "  ALTER DATABASE [" + seguro + "] SET MULTI_USER;\n" +
                "  THROW;\n" +
                "END CATCH;\n" +
                "ALTER DATABASE [" + seguro + "] SET MULTI_USER;";

            try
            {
                SqlHelper.EjecutarNoQuery(
                    BaseDeDatos.Master,
                    0,
                    sql,
                    SqlHelper.Parametro("@Ruta", SqlDbType.NVarChar, 4000, ruta));
            }
            finally
            {
                // Las conexiones que quedaron en el grupo apuntan a la base
                // anterior: se descartan para que las nuevas vean la restaurada.
                SqlConnection.ClearAllPools();
            }
        }

        /// <summary>
        /// Indica si el error se debe a que SQL Server no pudo abrir el
        /// archivo (permisos de la carpeta o ruta inexistente para el servicio).
        /// </summary>
        public bool EsErrorDeArchivo(Exception ex)
        {
            SqlException sql = ex as SqlException;

            if (sql == null && ex != null)
            {
                sql = ex.InnerException as SqlException;
            }

            if (sql == null && ex != null && ex.InnerException != null)
            {
                sql = ex.InnerException.InnerException as SqlException;
            }

            return sql != null && sql.Number == ErrorDispositivoNoAbierto;
        }

        /// <summary>
        /// Copia un archivo de respaldo con los permisos del usuario de Windows
        /// (se usa cuando SQL Server no puede escribir o leer en la carpeta
        /// elegida).
        /// </summary>
        public string CopiarArchivo(string rutaOrigen, string carpetaDestino)
        {
            ValidarRuta(rutaOrigen);

            if (string.IsNullOrWhiteSpace(carpetaDestino))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The backup folder is required."));
            }

            try
            {
                Directory.CreateDirectory(carpetaDestino);
                string destino = CombinarRuta(carpetaDestino, Path.GetFileName(rutaOrigen));
                File.Copy(rutaOrigen, destino, true);
                return destino;
            }
            catch (Exception ex) when (ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException)
            {
                throw new RespaldoException(
                    LanguageService.Format(
                        "The backup file could not be copied to '{0}': {1}",
                        carpetaDestino,
                        ex.Message),
                    ex);
            }
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Cuando el respaldo viene de otro equipo, los archivos de la base
        /// pueden estar en otras carpetas: se indica dónde dejar cada uno
        /// según los de la base actual.
        /// </summary>
        private static string ObtenerMovimientosDeArchivos(string baseDatos, string ruta)
        {
            List<string[]> enRespaldo = SqlHelper.Consultar(
                BaseDeDatos.Master,
                "RESTORE FILELISTONLY FROM DISK = @Ruta",
                fila => new[] { Convert.ToString(fila["LogicalName"]), Convert.ToString(fila["Type"]) },
                SqlHelper.Parametro("@Ruta", SqlDbType.NVarChar, 4000, ruta));

            List<string[]> actuales = SqlHelper.Consultar(
                BaseDeDatos.Master,
                "SELECT physical_name, type FROM sys.master_files WHERE database_id = DB_ID(@Base) ORDER BY file_id",
                fila => new[] { Convert.ToString(fila["physical_name"]), Convert.ToString(fila["type"]) },
                SqlHelper.Parametro("@Base", SqlDbType.NVarChar, 128, baseDatos));

            string datosActual = null;
            string logActual = null;
            string logicoDatos = null;
            string logicoLog = null;
            int cantidadDatos = 0;
            int cantidadLogs = 0;

            foreach (string[] archivo in actuales)
            {
                if (archivo[1] == "0" && datosActual == null) { datosActual = archivo[0]; }
                if (archivo[1] == "1" && logActual == null) { logActual = archivo[0]; }
            }

            foreach (string[] archivo in enRespaldo)
            {
                if (archivo[1] == "D") { cantidadDatos++; logicoDatos = logicoDatos ?? archivo[0]; }
                if (archivo[1] == "L") { cantidadLogs++; logicoLog = logicoLog ?? archivo[0]; }
            }

            // Solo se reubican respaldos con un archivo de datos y uno de registro,
            // que es como se crean las bases de Edelstahl.
            if (cantidadDatos != 1 || cantidadLogs != 1 || datosActual == null || logActual == null)
            {
                return string.Empty;
            }

            return ", MOVE N'" + logicoDatos.Replace("'", "''") + "' TO N'" + datosActual.Replace("'", "''") + "'" +
                   ", MOVE N'" + logicoLog.Replace("'", "''") + "' TO N'" + logActual.Replace("'", "''") + "'";
        }

        private static long ObtenerTamanio(string baseDatos, string ruta)
        {
            try
            {
                FileInfo informacion = new FileInfo(ruta);

                if (informacion.Exists)
                {
                    return informacion.Length;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // La carpeta de SQL Server puede no ser legible para el usuario.
            }

            // SQL Server guarda el tamaño de cada respaldo en msdb.
            return SqlHelper.EjecutarEscalar<long>(
                BaseDeDatos.Master,
                @"SELECT TOP 1 CAST(backup_size AS BIGINT)
                  FROM msdb.dbo.backupset
                  WHERE database_name = @Base
                  ORDER BY backup_finish_date DESC",
                SqlHelper.Parametro("@Base", SqlDbType.NVarChar, 128, baseDatos));
        }

        private static string CombinarRuta(string carpeta, string archivo)
        {
            return Path.Combine(carpeta.Trim(), archivo);
        }

        private static void ValidarRuta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The backup file is required."));
            }
        }

        private static void ValidarNombreBase(string baseDatos)
        {
            if (!string.Equals(baseDatos, SqlServerConnection.NombreBaseNegocio, StringComparison.Ordinal) &&
                !string.Equals(baseDatos, SqlServerConnection.NombreBaseServicios, StringComparison.Ordinal))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The requested database is not enabled for backup."));
            }
        }
    }
}
