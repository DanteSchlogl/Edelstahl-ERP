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
    /// Guarda y recupera usuarios y su rol desde SQL Server.
    /// </summary>
    internal class UsuarioRepositorySqlServer : IUsuarioRepository
    {
        private const string ColumnasUsuario =
            @"u.Id, u.RolId, u.NombreUsuario, u.NombreCompleto, u.Email,
              u.PasswordHash, u.PasswordSalt, u.Iteraciones, u.Activo,
              u.DebeCambiarPassword, u.IntentosFallidos, u.BloqueadoHasta,
              u.FechaAlta, u.UltimoAcceso,
              u.PreguntaSeguridad, u.RespuestaHash, u.RespuestaSalt,
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
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("A user with the entered name already exists."));
            }

            if (ExistsByEmail(entity.Email))
            {
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("A user with the entered email already exists."));
            }

            const string sql =
                @"INSERT INTO dbo.Usuarios
                  (Id, RolId, NombreUsuario, NombreCompleto, Email,
                   PasswordHash, PasswordSalt, Iteraciones, Activo,
                   DebeCambiarPassword, IntentosFallidos, BloqueadoHasta,
                   FechaAlta, UltimoAcceso,
                   PreguntaSeguridad, RespuestaHash, RespuestaSalt)
                  VALUES
                  (@Id, @RolId, @NombreUsuario, @NombreCompleto, @Email,
                   @PasswordHash, @PasswordSalt, @Iteraciones, @Activo,
                   @DebeCambiarPassword, @IntentosFallidos, @BloqueadoHasta,
                   @FechaAlta, @UltimoAcceso,
                   @PreguntaSeguridad, @RespuestaHash, @RespuestaSalt)";

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));
        }

        public void Update(Usuario entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The user identifier is not valid."));
            }

            Usuario mismoNombre = GetByNombreUsuario(entity.NombreUsuario);
            if (mismoNombre != null && mismoNombre.Id != entity.Id)
            {
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("Another user with that name already exists."));
            }

            Usuario mismoEmail = GetByEmail(entity.Email);
            if (mismoEmail != null && mismoEmail.Id != entity.Id)
            {
                throw new RegistroDuplicadoException(
                    LanguageService.Translate("Another user with that email already exists."));
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
                      UltimoAcceso = @UltimoAcceso,
                      PreguntaSeguridad = @PreguntaSeguridad,
                      RespuestaHash = @RespuestaHash,
                      RespuestaSalt = @RespuestaSalt
                  WHERE Id = @Id";

            int filas = SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, sql, Parametros(entity));

            if (filas == 0)
            {
                throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The user to modify was not found."));
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The user identifier is not valid."));
            }

            int filas = SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Servicios,
                "UPDATE dbo.Usuarios SET Activo = 0 WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));

            if (filas == 0)
            {
                throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The indicated user was not found."));
            }
        }

        public Usuario GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + ColumnasUsuario + DesdeUsuarios + " WHERE u.Id = @Id",
                Mapear,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public List<Usuario> GetAll()
        {
            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + ColumnasUsuario + DesdeUsuarios + " ORDER BY u.NombreUsuario",
                Mapear);
        }

        public Usuario GetByNombreUsuario(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + ColumnasUsuario + DesdeUsuarios + " WHERE u.NombreUsuario = @NombreUsuario",
                Mapear,
                SqlHelper.Parametro("@NombreUsuario", SqlDbType.NVarChar, 100,
                    nombreUsuario.Trim().ToLowerInvariant()));
        }

        public Usuario GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return SqlHelper.ConsultarUno(
                BaseDeDatos.Servicios,
                "SELECT " + ColumnasUsuario + DesdeUsuarios + " WHERE u.Email = @Email",
                Mapear,
                SqlHelper.Parametro("@Email", SqlDbType.NVarChar, 250,
                    email.Trim().ToLowerInvariant()));
        }

        public List<Usuario> GetByRolId(Guid rolId)
        {
            if (rolId == Guid.Empty)
            {
                return new List<Usuario>();
            }

            return SqlHelper.Consultar(
                BaseDeDatos.Servicios,
                "SELECT " + ColumnasUsuario + DesdeUsuarios + " WHERE u.RolId = @RolId ORDER BY u.NombreUsuario",
                Mapear,
                SqlHelper.Parametro("@RolId", SqlDbType.UniqueIdentifier, rolId));
        }

        public bool ExistsByNombreUsuario(string nombreUsuario)
        {
            return Existe("NombreUsuario", nombreUsuario, 100);
        }

        public bool ExistsByEmail(string email)
        {
            return Existe("Email", email, 250);
        }

        private static bool Existe(string columna, string valor, int longitud)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            return SqlHelper.EjecutarEscalar<int>(
                BaseDeDatos.Servicios,
                "SELECT COUNT(1) FROM dbo.Usuarios WHERE " + columna + " = @Valor",
                SqlHelper.Parametro("@Valor", SqlDbType.NVarChar, longitud,
                    valor.Trim().ToLowerInvariant())) > 0;
        }

        private static SqlParameter[] Parametros(Usuario usuario)
        {
            return new[]
            {
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, usuario.Id),
                SqlHelper.Parametro("@RolId", SqlDbType.UniqueIdentifier, usuario.RolId),
                SqlHelper.Parametro("@NombreUsuario", SqlDbType.NVarChar, 100,
                    usuario.NombreUsuario.Trim().ToLowerInvariant()),
                SqlHelper.Parametro("@NombreCompleto", SqlDbType.NVarChar, 250,
                    usuario.NombreCompleto.Trim()),
                SqlHelper.Parametro("@Email", SqlDbType.NVarChar, 250,
                    usuario.Email.Trim().ToLowerInvariant()),
                SqlHelper.Parametro("@PasswordHash", SqlDbType.NVarChar, 500, usuario.PasswordHash),
                SqlHelper.Parametro("@PasswordSalt", SqlDbType.NVarChar, 500, usuario.PasswordSalt),
                SqlHelper.Parametro("@Iteraciones", SqlDbType.Int, usuario.Iteraciones),
                SqlHelper.Parametro("@Activo", SqlDbType.Bit, usuario.Activo),
                SqlHelper.Parametro("@DebeCambiarPassword", SqlDbType.Bit, usuario.DebeCambiarPassword),
                SqlHelper.Parametro("@IntentosFallidos", SqlDbType.Int, usuario.IntentosFallidos),
                SqlHelper.Parametro("@BloqueadoHasta", SqlDbType.DateTime2,
                    SqlHelper.ONulo(usuario.BloqueadoHasta)),
                SqlHelper.Parametro("@FechaAlta", SqlDbType.DateTime2, usuario.FechaAlta),
                SqlHelper.Parametro("@UltimoAcceso", SqlDbType.DateTime2,
                    SqlHelper.ONulo(usuario.UltimoAcceso)),
                SqlHelper.Parametro("@PreguntaSeguridad", SqlDbType.NVarChar, 250,
                    usuario.PreguntaSeguridad ?? string.Empty),
                SqlHelper.Parametro("@RespuestaHash", SqlDbType.NVarChar, 500,
                    usuario.RespuestaHash ?? string.Empty),
                SqlHelper.Parametro("@RespuestaSalt", SqlDbType.NVarChar, 500,
                    usuario.RespuestaSalt ?? string.Empty)
            };
        }

        private static Usuario Mapear(IDataRecord fila)
        {
            Rol rol = new Rol
            {
                Id = (Guid)fila["Rol_Id"],
                Nombre = fila["Rol_Nombre"].ToString(),
                Descripcion = SqlHelper.Texto(fila, "Rol_Descripcion"),
                Activo = Convert.ToBoolean(fila["Rol_Activo"]),
                FechaAlta = Convert.ToDateTime(fila["Rol_FechaAlta"])
            };

            return new Usuario
            {
                Id = (Guid)fila["Id"],
                RolId = (Guid)fila["RolId"],
                NombreUsuario = fila["NombreUsuario"].ToString(),
                NombreCompleto = fila["NombreCompleto"].ToString(),
                Email = fila["Email"].ToString(),
                PasswordHash = fila["PasswordHash"].ToString(),
                PasswordSalt = fila["PasswordSalt"].ToString(),
                Iteraciones = Convert.ToInt32(fila["Iteraciones"]),
                Activo = Convert.ToBoolean(fila["Activo"]),
                DebeCambiarPassword = Convert.ToBoolean(fila["DebeCambiarPassword"]),
                IntentosFallidos = Convert.ToInt32(fila["IntentosFallidos"]),
                BloqueadoHasta = SqlHelper.FechaONula(fila, "BloqueadoHasta"),
                FechaAlta = Convert.ToDateTime(fila["FechaAlta"]),
                UltimoAcceso = SqlHelper.FechaONula(fila, "UltimoAcceso"),
                PreguntaSeguridad = SqlHelper.Texto(fila, "PreguntaSeguridad"),
                RespuestaHash = SqlHelper.Texto(fila, "RespuestaHash"),
                RespuestaSalt = SqlHelper.Texto(fila, "RespuestaSalt"),
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
                throw new DatoInvalidoException(
                    LanguageService.Translate("The role of the user is required."));
            }

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario) ||
                string.IsNullOrWhiteSpace(usuario.NombreCompleto) ||
                string.IsNullOrWhiteSpace(usuario.Email))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The user name, the full name and the email are required."));
            }

            if (string.IsNullOrWhiteSpace(usuario.PasswordHash) ||
                string.IsNullOrWhiteSpace(usuario.PasswordSalt) ||
                usuario.Iteraciones <= 0)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The cryptographic data of the password is not valid."));
            }

            if (usuario.IntentosFallidos < 0)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The failed attempts cannot be negative."));
            }

            if (usuario.FechaAlta == default(DateTime))
            {
                usuario.FechaAlta = DateTime.Now;
            }
        }
    }
}
