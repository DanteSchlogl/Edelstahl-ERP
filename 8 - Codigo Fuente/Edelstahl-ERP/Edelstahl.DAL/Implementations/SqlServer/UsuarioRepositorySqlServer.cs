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
    /// Guarda y recupera usuarios y su rol desde SQL Server.
    /// </summary>
    public class UsuarioRepositorySqlServer : IUsuarioRepository
    {
        private const string ColumnasUsuario =
            @"u.Id, u.RolId, u.NombreUsuario, u.NombreCompleto, u.Email,
              u.PasswordHash, u.PasswordSalt, u.Iteraciones, u.Activo,
              u.DebeCambiarPassword, u.IntentosFallidos, u.BloqueadoHasta,
              u.FechaAlta, u.UltimoAcceso,
              r.Id AS Rol_Id, r.Nombre AS Rol_Nombre,
              r.Descripcion AS Rol_Descripcion, r.Activo AS Rol_Activo,
              r.FechaAlta AS Rol_FechaAlta";

        private const string DesdeUsuarios =
            @" FROM dbo.Usuarios u
               INNER JOIN dbo.Roles r ON r.Id = u.RolId ";

        public void Add(Usuario entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            if (ExistsByNombreUsuario(entity.NombreUsuario))
            {
                throw new InvalidOperationException(
                    "Ya existe un usuario con el nombre ingresado.");
            }

            if (ExistsByEmail(entity.Email))
            {
                throw new InvalidOperationException(
                    "Ya existe un usuario con el correo ingresado.");
            }

            const string sql =
                @"INSERT INTO dbo.Usuarios
                  (Id, RolId, NombreUsuario, NombreCompleto, Email,
                   PasswordHash, PasswordSalt, Iteraciones, Activo,
                   DebeCambiarPassword, IntentosFallidos, BloqueadoHasta,
                   FechaAlta, UltimoAcceso)
                  VALUES
                  (@Id, @RolId, @NombreUsuario, @NombreCompleto, @Email,
                   @PasswordHash, @PasswordSalt, @Iteraciones, @Activo,
                   @DebeCambiarPassword, @IntentosFallidos, @BloqueadoHasta,
                   @FechaAlta, @UltimoAcceso)";

            EjecutarEscritura(sql, entity, false);
        }

        public void Update(Usuario entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del usuario no es válido.",
                    nameof(entity));
            }

            Usuario mismoNombre = GetByNombreUsuario(entity.NombreUsuario);
            if (mismoNombre != null && mismoNombre.Id != entity.Id)
            {
                throw new InvalidOperationException(
                    "Ya existe otro usuario con ese nombre.");
            }

            Usuario mismoEmail = GetByEmail(entity.Email);
            if (mismoEmail != null && mismoEmail.Id != entity.Id)
            {
                throw new InvalidOperationException(
                    "Ya existe otro usuario con ese correo.");
            }

            const string sql =
                @"UPDATE dbo.Usuarios
                  SET RolId = @RolId,
                      NombreUsuario = @NombreUsuario,
                      NombreCompleto = @NombreCompleto,
                      Email = @Email,
                      PasswordHash = @PasswordHash,
                      PasswordSalt = @PasswordSalt,
                      Iteraciones = @Iteraciones,
                      Activo = @Activo,
                      DebeCambiarPassword = @DebeCambiarPassword,
                      IntentosFallidos = @IntentosFallidos,
                      BloqueadoHasta = @BloqueadoHasta,
                      FechaAlta = @FechaAlta,
                      UltimoAcceso = @UltimoAcceso
                  WHERE Id = @Id";

            EjecutarEscritura(sql, entity, true);
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del usuario no es válido.",
                    nameof(id));
            }

            const string sql =
                @"UPDATE dbo.Usuarios SET Activo = 0 WHERE Id = @Id";

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
                            "No se encontró el usuario indicado.");
                    }
                }
            }
        }

        public Usuario GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            string sql =
                "SELECT " + ColumnasUsuario + DesdeUsuarios +
                " WHERE u.Id = @Id";

            return ObtenerUno(
                sql,
                command => command.Parameters.Add(
                    "@Id", SqlDbType.UniqueIdentifier).Value = id);
        }

        public List<Usuario> GetAll()
        {
            string sql =
                "SELECT " + ColumnasUsuario + DesdeUsuarios +
                " ORDER BY u.NombreUsuario";

            return ObtenerLista(sql, null);
        }

        public Usuario GetByNombreUsuario(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                return null;
            }

            string sql =
                "SELECT " + ColumnasUsuario + DesdeUsuarios +
                " WHERE u.NombreUsuario = @NombreUsuario";

            return ObtenerUno(
                sql,
                command => command.Parameters.Add(
                    "@NombreUsuario", SqlDbType.NVarChar, 100).Value =
                    nombreUsuario.Trim().ToLowerInvariant());
        }

        public Usuario GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            string sql =
                "SELECT " + ColumnasUsuario + DesdeUsuarios +
                " WHERE u.Email = @Email";

            return ObtenerUno(
                sql,
                command => command.Parameters.Add(
                    "@Email", SqlDbType.NVarChar, 250).Value =
                    email.Trim().ToLowerInvariant());
        }

        public List<Usuario> GetByRolId(Guid rolId)
        {
            if (rolId == Guid.Empty)
            {
                return new List<Usuario>();
            }

            string sql =
                "SELECT " + ColumnasUsuario + DesdeUsuarios +
                " WHERE u.RolId = @RolId ORDER BY u.NombreUsuario";

            return ObtenerLista(
                sql,
                command => command.Parameters.Add(
                    "@RolId", SqlDbType.UniqueIdentifier).Value = rolId);
        }

        public bool ExistsByNombreUsuario(string nombreUsuario)
        {
            return Existe(
                "NombreUsuario",
                "@Valor",
                nombreUsuario,
                100);
        }

        public bool ExistsByEmail(string email)
        {
            return Existe(
                "Email",
                "@Valor",
                email,
                250);
        }

        private static bool Existe(
            string columna,
            string parametro,
            string valor,
            int longitud)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            string sql =
                "SELECT COUNT(1) FROM dbo.Usuarios WHERE " +
                columna + " = " + parametro;

            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        parametro, SqlDbType.NVarChar, longitud).Value =
                        valor.Trim().ToLowerInvariant();

                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        private static Usuario ObtenerUno(
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

        private static List<Usuario> ObtenerLista(
            string sql,
            Action<SqlCommand> configurar)
        {
            List<Usuario> usuarios = new List<Usuario>();

            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (configurar != null)
                    {
                        configurar(command);
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(Mapear(reader));
                        }
                    }
                }
            }

            return usuarios;
        }

        private static void EjecutarEscritura(
            string sql,
            Usuario usuario,
            bool exigirFila)
        {
            using (SqlConnection connection =
                SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    AgregarParametros(command, usuario);
                    int filas = command.ExecuteNonQuery();

                    if (exigirFila && filas == 0)
                    {
                        throw new InvalidOperationException(
                            "No se encontró el usuario que se desea modificar.");
                    }
                }
            }
        }

        private static void AgregarParametros(
            SqlCommand command,
            Usuario usuario)
        {
            command.Parameters.Add(
                "@Id", SqlDbType.UniqueIdentifier).Value = usuario.Id;
            command.Parameters.Add(
                "@RolId", SqlDbType.UniqueIdentifier).Value = usuario.RolId;
            command.Parameters.Add(
                "@NombreUsuario", SqlDbType.NVarChar, 100).Value =
                usuario.NombreUsuario.Trim().ToLowerInvariant();
            command.Parameters.Add(
                "@NombreCompleto", SqlDbType.NVarChar, 250).Value =
                usuario.NombreCompleto.Trim();
            command.Parameters.Add(
                "@Email", SqlDbType.NVarChar, 250).Value =
                usuario.Email.Trim().ToLowerInvariant();
            command.Parameters.Add(
                "@PasswordHash", SqlDbType.NVarChar, 500).Value =
                usuario.PasswordHash;
            command.Parameters.Add(
                "@PasswordSalt", SqlDbType.NVarChar, 500).Value =
                usuario.PasswordSalt;
            command.Parameters.Add(
                "@Iteraciones", SqlDbType.Int).Value = usuario.Iteraciones;
            command.Parameters.Add(
                "@Activo", SqlDbType.Bit).Value = usuario.Activo;
            command.Parameters.Add(
                "@DebeCambiarPassword", SqlDbType.Bit).Value =
                usuario.DebeCambiarPassword;
            command.Parameters.Add(
                "@IntentosFallidos", SqlDbType.Int).Value =
                usuario.IntentosFallidos;
            command.Parameters.Add(
                "@BloqueadoHasta", SqlDbType.DateTime2).Value =
                usuario.BloqueadoHasta.HasValue
                    ? (object)usuario.BloqueadoHasta.Value
                    : DBNull.Value;
            command.Parameters.Add(
                "@FechaAlta", SqlDbType.DateTime2).Value = usuario.FechaAlta;
            command.Parameters.Add(
                "@UltimoAcceso", SqlDbType.DateTime2).Value =
                usuario.UltimoAcceso.HasValue
                    ? (object)usuario.UltimoAcceso.Value
                    : DBNull.Value;
        }

        private static Usuario Mapear(SqlDataReader reader)
        {
            Rol rol = new Rol
            {
                Id = reader.GetGuid(reader.GetOrdinal("Rol_Id")),
                Nombre = reader["Rol_Nombre"].ToString(),
                Descripcion = reader["Rol_Descripcion"] == DBNull.Value
                    ? string.Empty
                    : reader["Rol_Descripcion"].ToString(),
                Activo = Convert.ToBoolean(reader["Rol_Activo"]),
                FechaAlta = Convert.ToDateTime(reader["Rol_FechaAlta"])
            };

            return new Usuario
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                RolId = reader.GetGuid(reader.GetOrdinal("RolId")),
                NombreUsuario = reader["NombreUsuario"].ToString(),
                NombreCompleto = reader["NombreCompleto"].ToString(),
                Email = reader["Email"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                PasswordSalt = reader["PasswordSalt"].ToString(),
                Iteraciones = Convert.ToInt32(reader["Iteraciones"]),
                Activo = Convert.ToBoolean(reader["Activo"]),
                DebeCambiarPassword =
                    Convert.ToBoolean(reader["DebeCambiarPassword"]),
                IntentosFallidos = Convert.ToInt32(reader["IntentosFallidos"]),
                BloqueadoHasta = reader["BloqueadoHasta"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["BloqueadoHasta"]),
                FechaAlta = Convert.ToDateTime(reader["FechaAlta"]),
                UltimoAcceso = reader["UltimoAcceso"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["UltimoAcceso"]),
                Rol = rol
            };
        }

        private static void Validar(Usuario usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            if (usuario.RolId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "El rol del usuario es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario) ||
                string.IsNullOrWhiteSpace(usuario.NombreCompleto) ||
                string.IsNullOrWhiteSpace(usuario.Email))
            {
                throw new InvalidOperationException(
                    "El nombre de usuario, el nombre completo y el correo son obligatorios.");
            }

            if (string.IsNullOrWhiteSpace(usuario.PasswordHash) ||
                string.IsNullOrWhiteSpace(usuario.PasswordSalt) ||
                usuario.Iteraciones <= 0)
            {
                throw new InvalidOperationException(
                    "Los datos criptográficos de la contraseña no son válidos.");
            }

            if (usuario.IntentosFallidos < 0)
            {
                throw new InvalidOperationException(
                    "Los intentos fallidos no pueden ser negativos.");
            }

            if (usuario.FechaAlta == default(DateTime))
            {
                usuario.FechaAlta = DateTime.Now;
            }
        }
    }
}
