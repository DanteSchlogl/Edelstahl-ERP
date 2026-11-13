using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Implementations.SqlServer
{
    /// <summary>
    /// Guarda en SQL Server los permisos individuales de los usuarios (tabla PermisosUsuario).
    /// </summary>
    internal class PermisoUsuarioRepositorySqlServer : IPermisoUsuarioRepository
    {
        public List<PermisoIndividual> ObtenerDeUsuario(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
            {
                return new List<PermisoIndividual>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT Id, UsuarioId, Permiso, Concedido, FechaCambio, Administrador FROM dbo.PermisosUsuario WHERE UsuarioId = @UsuarioId ORDER BY Permiso",
                Mapear,
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, usuarioId))
                .Where(p => p != null)
                .ToList();
        }

        public void Guardar(PermisoIndividual permiso)
        {
            const string sql =
                @"IF EXISTS (SELECT 1 FROM dbo.PermisosUsuario WHERE UsuarioId = @UsuarioId AND Permiso = @Permiso)
                    UPDATE dbo.PermisosUsuario
                       SET Concedido = @Concedido, FechaCambio = @FechaCambio, Administrador = @Administrador
                     WHERE UsuarioId = @UsuarioId AND Permiso = @Permiso
                  ELSE
                    INSERT INTO dbo.PermisosUsuario (Id, UsuarioId, Permiso, Concedido, FechaCambio, Administrador)
                    VALUES (@Id, @UsuarioId, @Permiso, @Concedido, @FechaCambio, @Administrador)";

            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                sql,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, permiso.Id == Guid.Empty ? Guid.NewGuid() : permiso.Id),
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, permiso.UsuarioId),
                SqlHelper.Parametro("@Permiso", SqlDbType.NVarChar, 60, permiso.Permiso.ToString()),
                SqlHelper.Parametro("@Concedido", SqlDbType.Bit, permiso.Concedido),
                SqlHelper.Parametro("@FechaCambio", SqlDbType.DateTime2, permiso.FechaCambio),
                SqlHelper.Parametro("@Administrador", SqlDbType.NVarChar, 100, permiso.Administrador ?? string.Empty));
        }

        public void Quitar(Guid usuarioId, Permiso permiso)
        {
            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                "DELETE FROM dbo.PermisosUsuario WHERE UsuarioId = @UsuarioId AND Permiso = @Permiso",
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, usuarioId),
                SqlHelper.Parametro("@Permiso", SqlDbType.NVarChar, 60, permiso.ToString()));
        }

        public int QuitarTodos(Guid usuarioId)
        {
            return SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                "DELETE FROM dbo.PermisosUsuario WHERE UsuarioId = @UsuarioId",
                SqlHelper.Parametro("@UsuarioId", SqlDbType.UniqueIdentifier, usuarioId));
        }

        private static PermisoIndividual Mapear(IDataRecord fila)
        {
            Permiso permiso;

            if (!Enum.TryParse(fila["Permiso"].ToString(), out permiso))
            {
                // Un permiso que ya no existe en el programa se ignora.
                return null;
            }

            return new PermisoIndividual
            {
                Id = (Guid)fila["Id"],
                UsuarioId = (Guid)fila["UsuarioId"],
                Permiso = permiso,
                Concedido = Convert.ToBoolean(fila["Concedido"]),
                FechaCambio = Convert.ToDateTime(fila["FechaCambio"]),
                Administrador = SqlHelper.Texto(fila, "Administrador")
            };
        }
    }
}
