using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Implementations.SqlServer
{
    /// <summary>
    /// Guarda el historial de respaldos en la tabla dbo.Respaldos de la base
    /// de servicios.
    /// </summary>
    internal class RespaldoRepositorySqlServer : IRespaldoRepository
    {
        private const string Columnas =
            @"Id, FechaHora, BaseDatos, RutaSql, RutaCopia, TamanioBytes,
              Origen, Sano, Usuario, Observaciones";

        public void Add(RegistroRespaldo entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            const string sql =
                @"INSERT INTO dbo.Respaldos
                  (Id, FechaHora, BaseDatos, RutaSql, RutaCopia, TamanioBytes,
                   Origen, Sano, Usuario, Observaciones)
                  VALUES
                  (@Id, @FechaHora, @BaseDatos, @RutaSql, @RutaCopia, @TamanioBytes,
                   @Origen, @Sano, @Usuario, @Observaciones)";

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));
        }

        public void Update(RegistroRespaldo entity)
        {
            Validar(entity);

            const string sql =
                @"UPDATE dbo.Respaldos
                  SET FechaHora = @FechaHora, BaseDatos = @BaseDatos,
                      RutaSql = @RutaSql, RutaCopia = @RutaCopia,
                      TamanioBytes = @TamanioBytes, Origen = @Origen,
                      Sano = @Sano, Usuario = @Usuario,
                      Observaciones = @Observaciones
                  WHERE Id = @Id";

            if (SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity)) == 0)
            {
                throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The backup was not found."));
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The backup identifier is not valid."));
            }

            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                "DELETE FROM dbo.Respaldos WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public RegistroRespaldo GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Respaldos WHERE Id = @Id",
                Mapear,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public List<RegistroRespaldo> GetAll()
        {
            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Respaldos ORDER BY FechaHora DESC",
                Mapear);
        }

        public List<RegistroRespaldo> GetByBaseDatos(string baseDatos)
        {
            if (string.IsNullOrWhiteSpace(baseDatos))
            {
                return new List<RegistroRespaldo>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Respaldos WHERE BaseDatos = @BaseDatos ORDER BY FechaHora DESC",
                Mapear,
                SqlHelper.Parametro("@BaseDatos", SqlDbType.NVarChar, 100, baseDatos.Trim()));
        }

        private static SqlParameter[] Parametros(RegistroRespaldo r)
        {
            return new[]
            {
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, r.Id),
                SqlHelper.Parametro("@FechaHora", SqlDbType.DateTime2, r.FechaHora),
                SqlHelper.Parametro("@BaseDatos", SqlDbType.NVarChar, 100, r.BaseDatos.Trim()),
                SqlHelper.Parametro("@RutaSql", SqlDbType.NVarChar, 500, r.RutaSql ?? string.Empty),
                SqlHelper.Parametro("@RutaCopia", SqlDbType.NVarChar, 500, r.RutaCopia ?? string.Empty),
                SqlHelper.Parametro("@TamanioBytes", SqlDbType.BigInt, r.TamanioBytes),
                SqlHelper.Parametro("@Origen", SqlDbType.NVarChar, 30, r.Origen),
                SqlHelper.Parametro("@Sano", SqlDbType.Bit, r.Sano),
                SqlHelper.Parametro("@Usuario", SqlDbType.NVarChar, 100, r.Usuario ?? string.Empty),
                SqlHelper.Parametro("@Observaciones", SqlDbType.NVarChar, 500, r.Observaciones ?? string.Empty)
            };
        }

        private static RegistroRespaldo Mapear(IDataRecord fila)
        {
            return new RegistroRespaldo
            {
                Id = (Guid)fila["Id"],
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                BaseDatos = SqlHelper.Texto(fila, "BaseDatos"),
                RutaSql = SqlHelper.Texto(fila, "RutaSql"),
                RutaCopia = SqlHelper.Texto(fila, "RutaCopia"),
                TamanioBytes = Convert.ToInt64(fila["TamanioBytes"]),
                Origen = SqlHelper.Texto(fila, "Origen"),
                Sano = Convert.ToBoolean(fila["Sano"]),
                Usuario = SqlHelper.Texto(fila, "Usuario"),
                Observaciones = SqlHelper.Texto(fila, "Observaciones")
            };
        }

        private static void Validar(RegistroRespaldo respaldo)
        {
            if (respaldo == null)
            {
                throw new ArgumentNullException(nameof(respaldo));
            }

            if (string.IsNullOrWhiteSpace(respaldo.BaseDatos) ||
                string.IsNullOrWhiteSpace(respaldo.RutaSql))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The database and the file of the backup are required."));
            }

            if (string.IsNullOrWhiteSpace(respaldo.Origen))
            {
                respaldo.Origen = RegistroRespaldo.OrigenManual;
            }

            if (respaldo.FechaHora == default(DateTime))
            {
                respaldo.FechaHora = DateTime.Now;
            }
        }
    }
}
