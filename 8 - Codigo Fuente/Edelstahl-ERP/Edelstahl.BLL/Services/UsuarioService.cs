using System;
using System.Collections.Generic;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Security;
using Edelstahl.BLL.Infrastructure;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina el registro, autenticacion y administracion
    /// de los usuarios de Edelstahl ERP.
    /// </summary>
    public class UsuarioService
    {
        private const int IntentosMaximos = 5;

        private static readonly TimeSpan DuracionBloqueo =
            TimeSpan.FromMinutes(15);

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRolRepository _rolRepository;
        private readonly BitacoraService _bitacoraService;

        public UsuarioService()
        {
            _usuarioRepository =
                FactoryDataAccess.UsuarioRepository;

            _rolRepository =
                FactoryDataAccess.RolRepository;

            _bitacoraService =
                new BitacoraService();
        }

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IRolRepository rolRepository)
        {
            _usuarioRepository = usuarioRepository
                ?? throw new ArgumentNullException(
                    nameof(usuarioRepository));

            _rolRepository = rolRepository
                ?? throw new ArgumentNullException(
                    nameof(rolRepository));

            _bitacoraService =
                new BitacoraService();
        }

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IRolRepository rolRepository,
            BitacoraService bitacoraService)
        {
            _usuarioRepository = usuarioRepository
                ?? throw new ArgumentNullException(
                    nameof(usuarioRepository));

            _rolRepository = rolRepository
                ?? throw new ArgumentNullException(
                    nameof(rolRepository));

            _bitacoraService = bitacoraService
                ?? throw new ArgumentNullException(
                    nameof(bitacoraService));
        }

        public Usuario Registrar(
            string nombreUsuario,
            string nombreCompleto,
            string email,
            string password,
            string nombreRol,
            bool debeCambiarPassword = true)
        {
            string usuarioNormalizado =
                NormalizarNombreUsuario(nombreUsuario);

            string emailNormalizado =
                NormalizarEmail(email);

            try
            {
                ValidarDatosRegistro(
                    usuarioNormalizado,
                    nombreCompleto,
                    emailNormalizado,
                    password,
                    nombreRol);

                if (_usuarioRepository.ExistsByNombreUsuario(
                    usuarioNormalizado))
                {
                    throw Mensajes.Regla("There is already a user with that name.");
                }

                if (_usuarioRepository.ExistsByEmail(
                    emailNormalizado))
                {
                    throw Mensajes.Regla("There is already a user with that email.");
                }

                Rol rol = _rolRepository.GetByNombre(
                    nombreRol.Trim());

                if (rol == null)
                {
                    throw Mensajes.Regla("The selected role was not found.");
                }

                if (!rol.Activo)
                {
                    throw Mensajes.Regla("The selected role is not active.");
                }

                ValidarClave(password);

                PasswordHashData datosPassword =
                    PasswordHasher.CrearHash(password);

                Usuario usuario = new Usuario
                {
                    Id = Guid.NewGuid(),
                    RolId = rol.Id,
                    NombreUsuario = usuarioNormalizado,
                    NombreCompleto = nombreCompleto.Trim(),
                    Email = emailNormalizado,
                    PasswordHash = datosPassword.Hash,
                    PasswordSalt = datosPassword.Salt,
                    Iteraciones = datosPassword.Iteraciones,
                    Activo = true,
                    DebeCambiarPassword = debeCambiarPassword,
                    IntentosFallidos = 0,
                    BloqueadoHasta = null,
                    FechaAlta = DateTime.Now,
                    UltimoAcceso = null,
                    Rol = rol
                };

                _usuarioRepository.Add(usuario);

                _bitacoraService.Registrar(
                    "Seguridad",
                    "Alta de usuario",
                    "Usuario",
                    usuario.Id,
                    "Se registro el usuario " +
                    usuario.NombreUsuario +
                    " con el rol " +
                    rol.Nombre +
                    ".",
                    "Correcto",
                    "Auditoria",
                    string.Empty,
                    usuario.NombreUsuario);

                return usuario;
            }
            catch (Exception ex)
            {
                _bitacoraService.Registrar(
                    "Seguridad",
                    "Alta de usuario",
                    "Usuario",
                    null,
                    "No fue posible registrar el usuario.",
                    "Fallido",
                    "Error",
                    ex.ToString(),
                    usuarioNormalizado);

                throw;
            }
        }

        public Usuario Autenticar(
            string nombreUsuario,
            string password)
        {
            string usuarioNormalizado =
                NormalizarNombreUsuario(nombreUsuario);

            if (string.IsNullOrWhiteSpace(usuarioNormalizado) ||
                string.IsNullOrEmpty(password))
            {
                _bitacoraService.RegistrarSeguridad(
                    "Inicio de sesion",
                    "Se intento iniciar sesion sin completar " +
                    "el usuario o la contrasena.",
                    false,
                    usuarioNormalizado);

                throw Mensajes.Regla("Enter the user name and the password.");
            }

            Usuario usuario =
                _usuarioRepository.GetByNombreUsuario(
                    usuarioNormalizado);

            if (usuario == null)
            {
                _bitacoraService.RegistrarSeguridad(
                    "Inicio de sesion",
                    "Intento de acceso con un usuario inexistente.",
                    false,
                    usuarioNormalizado);

                throw Mensajes.Regla("The user name or the password is incorrect.");
            }

            if (!usuario.Activo)
            {
                _bitacoraService.Registrar(
                    "Seguridad",
                    "Inicio de sesion",
                    "Usuario",
                    usuario.Id,
                    "Intento de acceso con un usuario inactivo.",
                    "Fallido",
                    "Seguridad",
                    string.Empty,
                    usuario.NombreUsuario);

                throw Mensajes.Regla("The user is inactive.");
            }

            if (usuario.EstaBloqueado())
            {
                _bitacoraService.Registrar(
                    "Seguridad",
                    "Inicio de sesion",
                    "Usuario",
                    usuario.Id,
                    "Intento de acceso con un usuario bloqueado hasta " +
                    usuario.BloqueadoHasta.Value.ToString(
                        "dd/MM/yyyy HH:mm") +
                    ".",
                    "Fallido",
                    "Seguridad",
                    string.Empty,
                    usuario.NombreUsuario);

                throw Mensajes.Regla("The user is locked until {0}.",
                    usuario.BloqueadoHasta.Value.ToString("dd/MM/yyyy HH:mm"));
            }

            bool passwordValido = PasswordHasher.Verificar(
                password,
                usuario.PasswordHash,
                usuario.PasswordSalt,
                usuario.Iteraciones);

            if (!passwordValido)
            {
                usuario.RegistrarAccesoFallido(
                    IntentosMaximos,
                    DuracionBloqueo);

                _usuarioRepository.Update(usuario);

                if (usuario.EstaBloqueado())
                {
                    _bitacoraService.Registrar(
                        "Seguridad",
                        "Bloqueo de usuario",
                        "Usuario",
                        usuario.Id,
                        "El usuario fue bloqueado durante 15 minutos " +
                        "al alcanzar el maximo de intentos fallidos.",
                        "Advertencia",
                        "Seguridad",
                        string.Empty,
                        usuario.NombreUsuario);

                    throw Mensajes.Regla("The maximum number of attempts was reached. The user was locked for 15 minutes.");
                }

                int intentosRestantes =
                    IntentosMaximos - usuario.IntentosFallidos;

                _bitacoraService.Registrar(
                    "Seguridad",
                    "Inicio de sesion",
                    "Usuario",
                    usuario.Id,
                    "Contrasena incorrecta. Intentos restantes: " +
                    intentosRestantes +
                    ".",
                    "Fallido",
                    "Seguridad",
                    string.Empty,
                    usuario.NombreUsuario);

                throw Mensajes.Regla("The user name or the password is incorrect. Remaining attempts: {0}.", intentosRestantes);
            }

            usuario.RegistrarAccesoCorrecto();
            _usuarioRepository.Update(usuario);

            _bitacoraService.Registrar(
                "Seguridad",
                "Inicio de sesion",
                "Usuario",
                usuario.Id,
                "Inicio de sesion correcto.",
                "Correcto",
                "Seguridad",
                string.Empty,
                usuario.NombreUsuario);

            return usuario;
        }

        public void CambiarPassword(
            Guid usuarioId,
            string passwordActual,
            string passwordNueva)
        {
            Usuario usuario = ObtenerPorId(usuarioId);

            try
            {
                if (!PasswordHasher.Verificar(
                    passwordActual,
                    usuario.PasswordHash,
                    usuario.PasswordSalt,
                    usuario.Iteraciones))
                {
                    _bitacoraService.Registrar(
                        "Seguridad",
                        "Cambio de contrasena",
                        "Usuario",
                        usuario.Id,
                        "La contrasena actual ingresada es incorrecta.",
                        "Fallido",
                        "Seguridad",
                        string.Empty,
                        usuario.NombreUsuario);

                    throw Mensajes.Regla("The current password is incorrect.");
                }

                if (string.Equals(
                    passwordActual,
                    passwordNueva,
                    StringComparison.Ordinal))
                {
                    throw Mensajes.Regla("The new password must be different from the current one.");
                }

                PasswordHashData datosPassword =
                    PasswordHasher.CrearHash(ClaveValida(passwordNueva));

                usuario.PasswordHash = datosPassword.Hash;
                usuario.PasswordSalt = datosPassword.Salt;
                usuario.Iteraciones = datosPassword.Iteraciones;
                usuario.DebeCambiarPassword = false;
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;

                _usuarioRepository.Update(usuario);

                _bitacoraService.Registrar(
                    "Seguridad",
                    "Cambio de contrasena",
                    "Usuario",
                    usuario.Id,
                    "La contrasena fue modificada correctamente.",
                    "Correcto",
                    "Auditoria",
                    string.Empty,
                    usuario.NombreUsuario);
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException) ||
                    ex.Message != "La contrasena actual es incorrecta.")
                {
                    _bitacoraService.Registrar(
                        "Seguridad",
                        "Cambio de contrasena",
                        "Usuario",
                        usuario.Id,
                        "No fue posible modificar la contrasena.",
                        "Fallido",
                        "Error",
                        ex.ToString(),
                        usuario.NombreUsuario);
                }

                throw;
            }
        }

        public Usuario ObtenerPorId(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
            {
                throw Mensajes.Regla("The user identifier is not valid.");
            }

            Usuario usuario =
                _usuarioRepository.GetById(usuarioId);

            if (usuario == null)
            {
                throw Mensajes.Regla("The requested user was not found.");
            }

            return usuario;
        }

        public List<Usuario> ObtenerTodos()
        {
            return _usuarioRepository.GetAll();
        }

        public List<Rol> ObtenerRoles()
        {
            return _rolRepository.GetAll();
        }

        public bool ExisteAdministrador()
        {
            Rol rolAdministrador =
                _rolRepository.GetByNombre("Administrador");

            if (rolAdministrador == null)
            {
                return false;
            }

            List<Usuario> administradores =
                _usuarioRepository.GetByRolId(
                    rolAdministrador.Id);

            foreach (Usuario usuario in administradores)
            {
                if (usuario.Activo)
                {
                    return true;
                }
            }

            return false;
        }

        public Usuario CrearAdministradorInicial(
            string nombreUsuario,
            string nombreCompleto,
            string email,
            string password)
        {
            if (ExisteAdministrador())
            {
                throw Mensajes.Regla("There is already an active administrator.");
            }

            return Registrar(
                nombreUsuario,
                nombreCompleto,
                email,
                password,
                "Administrador",
                false);
        }

        public void Desbloquear(Guid usuarioId)
        {
            Usuario usuario = ObtenerPorId(usuarioId);

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;

            _usuarioRepository.Update(usuario);

            _bitacoraService.RegistrarAuditoria(
                "Seguridad",
                "Desbloqueo de usuario",
                "Usuario",
                usuario.Id,
                "El usuario " +
                usuario.NombreUsuario +
                " fue desbloqueado.");
        }

        public void CambiarEstado(
            Guid usuarioId,
            bool activo)
        {
            Usuario usuario = ObtenerPorId(usuarioId);
            usuario.Activo = activo;
            _usuarioRepository.Update(usuario);

            _bitacoraService.RegistrarAuditoria(
                "Seguridad",
                activo
                    ? "Activacion de usuario"
                    : "Desactivacion de usuario",
                "Usuario",
                usuario.Id,
                "El usuario " +
                usuario.NombreUsuario +
                (activo
                    ? " fue activado."
                    : " fue desactivado."));
        }

        private static void ValidarDatosRegistro(
            string nombreUsuario,
            string nombreCompleto,
            string email,
            string password,
            string nombreRol)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                throw Mensajes.Regla("The user name is required.");
            }

            if (nombreUsuario.Length < 4)
            {
                throw Mensajes.Regla("The user name must have at least 4 characters.");
            }

            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                throw Mensajes.Regla("The full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email) ||
                !EmailPareceValido(email))
            {
                throw Mensajes.Regla("The email address is not valid.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw Mensajes.Regla("The password is required.");
            }

            if (string.IsNullOrWhiteSpace(nombreRol))
            {
                throw Mensajes.Regla("The role is required.");
            }
        }

        private static string NormalizarNombreUsuario(
            string nombreUsuario)
        {
            return string.IsNullOrWhiteSpace(nombreUsuario)
                ? string.Empty
                : nombreUsuario.Trim().ToLowerInvariant();
        }

        private static string NormalizarEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static bool EmailPareceValido(string email)
        {
            int arroba = email.IndexOf('@');
            int punto = email.LastIndexOf('.');

            return arroba > 0 &&
                   punto > arroba + 1 &&
                   punto < email.Length - 1;
        }

        /// <summary>La clave se valida como regla de negocio para mostrar un aviso claro y no un error inesperado.</summary>
        public static void ValidarClave(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw Mensajes.Regla("The password cannot be empty.");
            }

            if (password.Length < 8)
            {
                throw Mensajes.Regla("The password must have at least 8 characters.");
            }
        }

        public static string ClaveValida(string password)
        {
            ValidarClave(password);
            return password;
        }
    }
}
