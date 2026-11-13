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
    internal class BitacoraRepositorySqlServer : IBitacoraRepository
    {
        private const string Columnas =
            @"Id, FechaHora, UsuarioId, NombreUsuario, Modulo, Accion,
              Entidad, EntidadId, Descripcion, Resultado, DireccionEquipo,
              NombreEquipo, TipoEvento, DetalleTecnico";

        public void Add(RegistroBitacora entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            const string sql =
                @"INSERT INTO dbo.Bitacora
                  (Id, FechaHora, UsuarioId, NombreUsuario, Modulo, Accion,
                   Entidad, EntidadId, Descripcion, Resultado, DireccionEquipo,
                   NombreEquipo, TipoEvento, DetalleTecnico)
                  VALUES
                  (@Id, @FechaHora, @UsuarioId, @NombreUsuario, @Modulo, @Accion,
                   @Entidad, @EntidadId, @Descripcion, @Resultado, @DireccionEquipo,
                   @NombreEquipo, @TipoEvento, @DetalleTecnico)";

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));
        }

        public void Update(RegistroBitacora entity)
        {
            throw new ReglaDeNegocioException(
                LanguageService.Translate("The audit log records cannot be modified."));
        }

        public void Delete(Guid id)
        {
            throw new ReglaDeNegocioException(
                LanguageService.Translate("The audit log records cannot be deleted."));
        }

        public RegistroBitacora GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Bitacora WHERE Id = @Id",
                Mapear,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public List<RegistroBitacora> GetAll()
        {
            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Bitacora ORDER BY FechaHora DESC",
                Mapear);
        }

        public List<RegistroBitacora> GetByUsuarioId(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
            {
                return new List<RegistroBitacora>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Bitacora WHERE UsuarioId = @UsuarioId ORDER BY FechaHora DESC",
                Mapear,
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, usuarioId));
        }

        public List<RegistroBitacora> GetByFecha(DateTime desde, DateTime hasta)
        {
            if (hasta < desde)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The end date cannot be earlier than the start date."));
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE FechaHora >= @Desde AND FechaHora <= @Hasta ORDER BY FechaHora DESC",
                Mapear,
                SqlHelper.Parametro("@Desde", SqlDbType.DateTime2, desde),
                SqlHelper.Parametro("@Hasta", SqlDbType.DateTime2, hasta));
        }

        public List<RegistroBitacora> GetByModulo(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
            {
                return new List<RegistroBitacora>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Bitacora WHERE Modulo = @Modulo ORDER BY FechaHora DESC",
                Mapear,
                SqlHelper.Parametro("@Modulo", SqlDbType.NVarChar, 100, modulo.Trim()));
        }

        public List<RegistroBitacora> GetByTipoEvento(string tipoEvento)
        {
            if (string.IsNullOrWhiteSpace(tipoEvento))
            {
                return new List<RegistroBitacora>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Bitacora WHERE TipoEvento = @TipoEvento ORDER BY FechaHora DESC",
                Mapear,
                SqlHelper.Parametro("@TipoEvento", SqlDbType.NVarChar, 50, tipoEvento.Trim()));
        }

        private static RegistroBitacora Mapear(IDataRecord fila)
        {
            return new RegistroBitacora
            {
                Id = (Guid)fila["Id"],
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                UsuarioId = SqlHelper.GuidONulo(fila, "UsuarioId"),
                NombreUsuario = SqlHelper.Texto(fila, "NombreUsuario"),
                Modulo = SqlHelper.Texto(fila, "Modulo"),
                Accion = SqlHelper.Texto(fila, "Accion"),
                Entidad = SqlHelper.Texto(fila, "Entidad"),
                EntidadId = SqlHelper.GuidONulo(fila, "EntidadId"),
                Descripcion = SqlHelper.Texto(fila, "Descripcion"),
                Resultado = SqlHelper.Texto(fila, "Resultado"),
                DireccionEquipo = SqlHelper.Texto(fila, "DireccionEquipo"),
                NombreEquipo = SqlHelper.Texto(fila, "NombreEquipo"),
                TipoEvento = SqlHelper.Texto(fila, "TipoEvento"),
                DetalleTecnico = SqlHelper.Texto(fila, "DetalleTecnico")
            };
        }

        private static SqlParameter[] Parametros(RegistroBitacora registro)
        {
            return new[]
            {
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, registro.Id),
                SqlHelper.Parametro("@FechaHora", SqlDbType.DateTime2, registro.FechaHora),
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, SqlHelper.ONulo(registro.UsuarioId)),
                SqlHelper.Parametro("@NombreUsuario", SqlDbType.NVarChar, 100, Limpiar(registro.NombreUsuario)),
                SqlHelper.Parametro("@Modulo", SqlDbType.NVarChar, 100, Limpiar(registro.Modulo)),
                SqlHelper.Parametro("@Accion", SqlDbType.NVarChar, 150, Limpiar(registro.Accion)),
                SqlHelper.Parametro("@Entidad", SqlDbType.NVarChar, 100, Limpiar(registro.Entidad)),
                SqlHelper.Parametro("@EntidadId", SqlDbType.UniqueIdentifier, SqlHelper.ONulo(registro.EntidadId)),
                SqlHelper.Parametro("@Descripcion", SqlDbType.NVarChar, 1000, Limpiar(registro.Descripcion)),
                SqlHelper.Parametro("@Resultado", SqlDbType.NVarChar, 50, Limpiar(registro.Resultado)),
                SqlHelper.Parametro("@DireccionEquipo", SqlDbType.NVarChar, 100, Limpiar(registro.DireccionEquipo)),
                SqlHelper.Parametro("@NombreEquipo", SqlDbType.NVarChar, 150, Limpiar(registro.NombreEquipo)),
                SqlHelper.Parametro("@TipoEvento", SqlDbType.NVarChar, 50, Limpiar(registro.TipoEvento)),
                SqlHelper.Parametro("@DetalleTecnico", SqlDbType.NVarChar, -1, Limpiar(registro.DetalleTecnico))
            };
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Trim();
        }

        private static void Validar(RegistroBitacora registro)
        {
            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            if (registro.FechaHora == default(DateTime))
            {
                registro.FechaHora = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(registro.Modulo) ||
                string.IsNullOrWhiteSpace(registro.Accion))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The module and the action of the audit log are required."));
            }

            string[] resultados = { "Correcto", "Fallido", "Advertencia" };
            string[] tipos = { "Informacion", "Seguridad", "Error", "Auditoria" };

            if (Array.IndexOf(resultados, registro.Resultado) < 0)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The audit log result is not valid."));
            }

            if (Array.IndexOf(tipos, registro.TipoEvento) < 0)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The event type is not valid."));
            }
        }
    }
}
