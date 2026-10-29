using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Implementations.SqlServer
{
    /// <summary>
    /// Guarda y recupera roles desde SQL Server.
    /// </summary>
    public class RolRepositorySqlServer : IRolRepository
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
                throw new InvalidOperationException(
                    "Ya existe un rol con el nombre ingresado.");
            }

            const string sql =
                @"INSERT INTO dbo.Roles
                  (Id, Nombre, Descripcion, Activo, FechaAlta)
                  VALUES
                  (@Id, @Nombre, @Descripcion, @Activo, @FechaAlta)";

            EjecutarEscritura(sql, entity, false);
        }

        public void Update(Rol entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del rol no es válido.",
                    nameof(entity));
            }

            Rol existente = GetByNombre(entity.Nombre);

            if (existente != null && existente.Id != entity.Id)
            {
                throw new InvalidOperationException(
                    "Ya existe otro rol con el nombre ingresado.");
            }

            const string sql =
                @"UPDATE dbo.Roles
                  SET Nombre = @Nombre,
                      Descripcion = @Descripcion,
                      Activo = @Activo,
                      FechaAlta = @FechaAlta
                  WHERE Id = @Id";

            EjecutarEscritura(sql, entity, true);
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del rol no es válido.",
                    nameof(id));
            }

            const string sql =
                @"UPDATE dbo.Roles SET Activo = 0 WHERE Id = @Id";

            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException(
                            "No se encontró el rol indicado.");
                    }
                }
            }
        }

        public Rol GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            string sql =
                "SELECT " + Columnas +
                " FROM dbo.Roles WHERE Id = @Id";

            return ObtenerUno(
                sql,
                command => command.Parameters.Add(
                    "@Id", SqlDbType.UniqueIdentifier).Value = id);
        }

        public List<Rol> GetAll()
        {
            List<Rol> roles = new List<Rol>();
            string sql =
                "SELECT " + Columnas +
                " FROM dbo.Roles ORDER BY Nombre";

            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(Mapear(reader));
                    }
                }
            }

            return roles;
        }

        public Rol GetByNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return null;
            }

            string sql =
                "SELECT " + Columnas +
                " FROM dbo.Roles WHERE Nombre = @Nombre";

            return ObtenerUno(
                sql,
                command => command.Parameters.Add(
                    "@Nombre", SqlDbType.NVarChar, 100).Value =
                    nombre.Trim());
        }

        public bool ExistsByNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return false;
            }

            const string sql =
                @"SELECT COUNT(1) FROM dbo.Roles WHERE Nombre = @Nombre";

            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@Nombre", SqlDbType.NVarChar, 100).Value =
                        nombre.Trim();

                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        private static Rol ObtenerUno(
            string sql,
            Action<SqlCommand> configurar)
        {
            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    configurar(command);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read() ? Mapear(reader) : null;
                    }
                }
            }
        }

        private static void EjecutarEscritura(
            string sql,
            Rol rol,
            bool exigirFila)
        {
            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    AgregarParametros(command, rol);
                    int filas = command.ExecuteNonQuery();

                    if (exigirFila && filas == 0)
                    {
                        throw new InvalidOperationException(
                            "No se encontró el rol que se desea modificar.");
                    }
                }
            }
        }

        private static void AgregarParametros(SqlCommand command, Rol rol)
        {
            command.Parameters.Add(
                "@Id", SqlDbType.UniqueIdentifier).Value = rol.Id;
            command.Parameters.Add(
                "@Nombre", SqlDbType.NVarChar, 100).Value = rol.Nombre.Trim();
            command.Parameters.Add(
                "@Descripcion", SqlDbType.NVarChar, 300).Value =
                string.IsNullOrWhiteSpace(rol.Descripcion)
                    ? string.Empty
                    : rol.Descripcion.Trim();
            command.Parameters.Add(
                "@Activo", SqlDbType.Bit).Value = rol.Activo;
            command.Parameters.Add(
                "@FechaAlta", SqlDbType.DateTime2).Value = rol.FechaAlta;
        }

        private static Rol Mapear(SqlDataReader reader)
        {
            return new Rol
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"] == DBNull.Value
                    ? string.Empty
                    : reader["Descripcion"].ToString(),
                Activo = Convert.ToBoolean(reader["Activo"]),
                FechaAlta = Convert.ToDateTime(reader["FechaAlta"])
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
                throw new InvalidOperationException(
                    "El nombre del rol es obligatorio.");
            }

            if (rol.FechaAlta == default(DateTime))
            {
                rol.FechaAlta = DateTime.Now;
            }
        }
    }
}
