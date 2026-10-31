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

        // ------------------------------------------------------------------
        // Pregunta de seguridad y recuperacion de la contrasena
        // ------------------------------------------------------------------

        /// <summary>Preguntas que el usuario puede elegir (texto en ingles; la pantalla las traduce).</summary>
        public static readonly string[] PreguntasSeguridad =
        {
            "What was the name of your first pet?",
            "What is the name of the street where you grew up?",
            "What is your mother's maiden name?",
            "What was the name of your first school?",
            "In which city were you born?",
            "What was the make of your first car or motorcycle?"
        };

        /// <summary>
        /// Deja la respuesta en minusculas, sin tildes y sin espacios repetidos,
        /// para que "Rex", "rex " y "REX" cuenten como la misma respuesta.
        /// </summary>
        public static string NormalizarRespuesta(string respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta))
            {
                return string.Empty;
            }

            string descompuesta = respuesta.Trim().ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD);

            System.Text.StringBuilder limpia = new System.Text.StringBuilder();

            foreach (char c in descompuesta)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                    System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    limpia.Append(c);
                }
            }

            return string.Join(" ", limpia.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // El hash de la respuesta se calcula sobre este prefijo mas la respuesta: el generador de hashes
        // pide al menos 8 caracteres (regla de las contrasenas) y una respuesta como "Rex" es mas corta.
        private const string PrefijoRespuesta = "respuesta|";

        private static void AplicarPregunta(Usuario usuario, string pregunta, string respuesta)
        {
            if (Array.IndexOf(PreguntasSeguridad, pregunta) < 0)
            {
                throw Mensajes.Regla("Choose one of the security questions in the list.");
            }

            string normalizada = NormalizarRespuesta(respuesta);

            if (normalizada.Length < 3)
            {
                throw Mensajes.Regla("The answer must have at least 3 characters.");
            }

            PasswordHashData datos = PasswordHasher.CrearHash(PrefijoRespuesta + normalizada);

            usuario.PreguntaSeguridad = pregunta;
            usuario.RespuestaHash = datos.Hash;
            usuario.RespuestaSalt = datos.Salt;
        }

        /// <summary>
        /// Guarda la pregunta sin pedir la contrasena. Lo usa el administrador
        /// (ver AdministracionService.DefinirPreguntaSeguridad).
        /// </summary>
        public void DefinirPreguntaSeguridad(Guid usuarioId, string pregunta, string respuesta)
        {
            Usuario usuario = ObtenerPorId(usuarioId);

            AplicarPregunta(usuario, pregunta, respuesta);
            _usuarioRepository.Update(usuario);

            _bitacoraService.Registrar(
                "Seguridad", "Definicion de pregunta de seguridad", "Usuario", usuario.Id,
                "Se definio la pregunta de seguridad de " + usuario.NombreUsuario + ".",
                "Correcto", "Auditoria", string.Empty, SesionActual.NombreUsuario);
        }

        /// <summary>El usuario cambia su propia pregunta; se pide la contrasena para confirmar que es el.</summary>
        public void CambiarPreguntaSeguridad(Guid usuarioId, string passwordActual, string pregunta, string respuesta)
        {
            Usuario usuario = ObtenerPorId(usuarioId);

            if (!PasswordHasher.Verificar(passwordActual, usuario.PasswordHash, usuario.PasswordSalt, usuario.Iteraciones))
            {
                _bitacoraService.Registrar(
                    "Seguridad", "Cambio de pregunta de seguridad", "Usuario", usuario.Id,
                    "La contrasena actual ingresada es incorrecta.",
                    "Fallido", "Seguridad", string.Empty, usuario.NombreUsuario);

                throw Mensajes.Regla("The current password is incorrect.");
            }

            AplicarPregunta(usuario, pregunta, respuesta);
            _usuarioRepository.Update(usuario);

            _bitacoraService.Registrar(
                "Seguridad", "Cambio de pregunta de seguridad", "Usuario", usuario.Id,
                "El usuario cambio su pregunta de seguridad.",
                "Correcto", "Auditoria", string.Empty, usuario.NombreUsuario);
        }

        /// <summary>
        /// Devuelve la pregunta del usuario para mostrarla en la pantalla de recuperacion.
        /// Si el usuario no existe o no tiene pregunta se da el mismo aviso, para no revelar cuales usuarios existen.
        /// </summary>
        public string ObtenerPreguntaSeguridad(string nombreUsuario)
        {
            Usuario usuario = _usuarioRepository.GetByNombreUsuario(NormalizarNombreUsuario(nombreUsuario));

            if (usuario == null || !usuario.Activo || !usuario.TienePreguntaSeguridad())
            {
                throw Mensajes.Regla("This user has no security question available. Contact the administrator.");
            }

            return usuario.PreguntaSeguridad;
        }

        /// <summary>
        /// Cambia la contrasena si la respuesta a la pregunta de seguridad es correcta.
        /// Las respuestas incorrectas cuentan como intentos fallidos: a los 5 el usuario queda bloqueado 15 minutos.
        /// </summary>
        public void RecuperarConPregunta(string nombreUsuario, string respuesta, string nuevaPassword)
        {
            string usuarioNormalizado = NormalizarNombreUsuario(nombreUsuario);
            Usuario usuario = _usuarioRepository.GetByNombreUsuario(usuarioNormalizado);

            if (usuario == null || !usuario.Activo || !usuario.TienePreguntaSeguridad())
            {
                _bitacoraService.RegistrarSeguridad(
                    "Recuperacion de contrasena",
                    "Intento de recuperacion de un usuario inexistente, inactivo o sin pregunta de seguridad.",
                    false, usuarioNormalizado);

                throw Mensajes.Regla("This user has no security question available. Contact the administrator.");
            }

            if (usuario.EstaBloqueado())
            {
                throw Mensajes.Regla("The user is locked until {0}.",
                    usuario.BloqueadoHasta.Value.ToString("dd/MM/yyyy HH:mm"));
            }

            bool correcta = PasswordHasher.Verificar(
                PrefijoRespuesta + NormalizarRespuesta(respuesta),
                usuario.RespuestaHash,
                usuario.RespuestaSalt,
                PasswordHasher.IteracionesPredeterminadas);

            if (!correcta)
            {
                usuario.RegistrarAccesoFallido(IntentosMaximos, DuracionBloqueo);
                _usuarioRepository.Update(usuario);

                _bitacoraService.Registrar(
                    "Seguridad", "Recuperacion de contrasena", "Usuario", usuario.Id,
                    "Respuesta incorrecta a la pregunta de seguridad.",
                    "Fallido", "Seguridad", string.Empty, usuario.NombreUsuario);

                if (usuario.EstaBloqueado())
                {
                    throw Mensajes.Regla("The maximum number of attempts was reached. The user was locked for 15 minutes.");
                }

                throw Mensajes.Regla("The answer is incorrect. Remaining attempts: {0}.",
                    IntentosMaximos - usuario.IntentosFallidos);
            }

            PasswordHashData datos = PasswordHasher.CrearHash(ClaveValida(nuevaPassword));

            usuario.PasswordHash = datos.Hash;
            usuario.PasswordSalt = datos.Salt;
            usuario.Iteraciones = datos.Iteraciones;
            usuario.DebeCambiarPassword = false;
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            _usuarioRepository.Update(usuario);

            _bitacoraService.Registrar(
                "Seguridad", "Recuperacion de contrasena", "Usuario", usuario.Id,
                "La contrasena se restablecio con la pregunta de seguridad.",
                "Correcto", "Auditoria", string.Empty, usuario.NombreUsuario);
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
