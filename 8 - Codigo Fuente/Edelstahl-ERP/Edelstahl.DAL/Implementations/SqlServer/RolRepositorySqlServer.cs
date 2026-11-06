using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Implementations.SqlServer
{
    /// <summary>
    /// Guarda y recupera roles desde SQL Server.
    /// </summary>
    internal class RolRepositorySqlServer : IRolRepository
    {
        private const string Columnas =
            "Id, Nombre, Descripcion, Activo, FechaAlta";

        public void Add(Rol entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            if (ExistsByNombre(entity.Nombre))
            {
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("A role with the entered name already exists."));
            }

            const string sql =
                @"INSERT INTO dbo.Roles
                  (Id, Nombre, Descripcion, Activo, FechaAlta)
                  VALUES
                  (@Id, @Nombre, @Descripcion, @Activo, @FechaAlta)";

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));
        }

        public void Update(Rol entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The role identifier is not valid."));
            }

            Rol existente = GetByNombre(entity.Nombre);

            if (existente != null && existente.Id != entity.Id)
            {
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("Another role with the entered name already exists."));
            }

            const string sql =
                @"UPDATE dbo.Roles
                  SET Nombre = @Nombre,
                      Descripcion = @Descripcion,
                      Activo = @Activo,
                      FechaAlta = @FechaAlta
                  WHERE Id = @Id";

            int filas = SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));

            if (filas == 0)
            {
                throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The role to modify was not found."));
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The role identifier is not valid."));
            }

            int filas = SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                "UPDATE dbo.Roles SET Activo = 0 WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));

            if (filas == 0)
            {
                throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The indicated role was not found."));
            }
        }

        public Rol GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Roles WHERE Id = @Id",
                Mapear,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public List<Rol> GetAll()
        {
            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Roles ORDER BY Nombre",
                Mapear);
        }

        public Rol GetByNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + Columnas + " FROM dbo.Roles WHERE Nombre = @Nombre",
                Mapear,
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, nombre.Trim()));
        }

        public bool ExistsByNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return false;
            }

            return SqlHelper.EjecutarEscalar<int>(
                BaseDeDatos.Servicios,
                "SELECT COUNT(1) FROM dbo.Roles WHERE Nombre = @Nombre",
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, nombre.Trim())) > 0;
        }

        private static System.Data.SqlClient.SqlParameter[] Parametros(Rol rol)
        {
            return new[]
            {
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, rol.Id),
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, rol.Nombre.Trim()),
                SqlHelper.Parametro("@Descripcion", SqlDbType.NVarChar, 300,
                    string.IsNullOrWhiteSpace(rol.Descripcion) ? string.Empty : rol.Descripcion.Trim()),
                SqlHelper.Parametro("@Activo", SqlDbType.Bit, rol.Activo),
                SqlHelper.Parametro("@FechaAlta", SqlDbType.DateTime2, rol.FechaAlta)
            };
        }

        private static Rol Mapear(IDataRecord fila)
        {
            return new Rol
            {
                Id = (Guid)fila["Id"],
                Nombre = fila["Nombre"].ToString(),
                Descripcion = SqlHelper.Texto(fila, "Descripcion"),
                Activo = Convert.ToBoolean(fila["Activo"]),
                FechaAlta = Convert.ToDateTime(fila["FechaAlta"])
            };
        }

        private static void Validar(Rol rol)
        {
            if (rol == null)
            {
                throw new ArgumentNullException(nameof(rol));
            }

            if (string.IsNullOrWhiteSpace(rol.Nombre))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The role name is required."));
            }

            if (rol.FechaAlta == default(DateTime))
            {
                rol.FechaAlta = DateTime.Now;
            }
        }
    }
}
