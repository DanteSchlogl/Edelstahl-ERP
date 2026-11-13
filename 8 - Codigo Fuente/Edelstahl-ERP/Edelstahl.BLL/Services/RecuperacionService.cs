using System;
using System.Collections.Generic;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Pantalla de recuperación: cuando los dígitos verificadores indican que
    /// los datos fueron alterados, el sistema no deja entrar con normalidad.
    /// Quien quiera repararlo debe identificarse con usuario y contraseña y
    /// tener permiso de administrar respaldos; recién entonces puede ver los
    /// respaldos hechos, restaurar uno o, tras revisar los datos, recalcular
    /// los dígitos.
    ///
    /// Esta clase no abre una sesión de trabajo: solo autoriza estas operaciones.
    /// </summary>
    public class RecuperacionService
    {
        private readonly UsuarioService _usuarios;
        private readonly BitacoraService _bitacora;
        private readonly RespaldoService _respaldos;
        private readonly IntegridadService _integridad;

        private Usuario _autorizado;

        public RecuperacionService()
            : this(new UsuarioService(), new BitacoraService(), new RespaldoService(), new IntegridadService())
        {
        }

        public RecuperacionService(
            UsuarioService usuarios,
            BitacoraService bitacora,
            RespaldoService respaldos,
            IntegridadService integridad)
        {
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
            _bitacora = bitacora ?? throw new ArgumentNullException(nameof(bitacora));
            _respaldos = respaldos ?? throw new ArgumentNullException(nameof(respaldos));
            _integridad = integridad ?? throw new ArgumentNullException(nameof(integridad));
        }

        public bool Autorizado
        {
            get { return _autorizado != null; }
        }

        public string NombreUsuario
        {
            get { return _autorizado == null ? string.Empty : _autorizado.NombreUsuario; }
        }

        public RespaldoService Respaldos
        {
            get { return _respaldos; }
        }

        /// <summary>
        /// Comprueba usuario y contraseña y que el rol tenga permiso para
        /// administrar respaldos. Los intentos quedan en la bitácora.
        /// </summary>
        public void Autenticar(string nombreUsuario, string password)
        {
            _autorizado = null;

            // Autenticar ya registra los intentos fallidos y bloquea tras varios.
            Usuario usuario = _usuarios.Autenticar(nombreUsuario, password);

            string rol = usuario.Rol == null ? string.Empty : usuario.Rol.Nombre;

            PermisosEfectivos.Invalidar(usuario.Id);

            if (!PermisosEfectivos.Tiene(usuario, Permiso.RespaldoAdministrar))
            {
                _bitacora.Registrar(
                    "Integridad",
                    "Acceso a la recuperacion denegado",
                    "Usuario",
                    usuario.Id,
                    "El usuario no tiene permiso para administrar respaldos y no puede usar la recuperacion de datos.",
                    "Fallido",
                    "Seguridad",
                    string.Empty,
                    usuario.NombreUsuario);

                throw new AccesoNoAutorizadoException(
                    Mensajes.T("You do not have permission to perform this operation."));
            }

            _autorizado = usuario;

            _bitacora.Registrar(
                "Integridad",
                "Acceso a la recuperacion",
                "Usuario",
                usuario.Id,
                "El usuario se identifico en la pantalla de recuperacion de datos.",
                "Correcto",
                "Seguridad",
                string.Empty,
                usuario.NombreUsuario);
        }

        /// <summary>
        /// Todos los respaldos de la base de negocio, del más nuevo al más antiguo.
        /// </summary>
        public List<RegistroRespaldo> ObtenerRespaldos()
        {
            ExigirAutorizacion();

            return _respaldos.ObtenerRespaldos();
        }

        public ResultadoIntegridad Verificar()
        {
            return _integridad.Verificar();
        }

        public ResultadoIntegridad RestaurarRespaldo(RegistroRespaldo respaldo)
        {
            ExigirAutorizacion();

            return _respaldos.RestaurarRespaldo(respaldo, NombreUsuario);
        }

        public ResultadoIntegridad RestaurarArchivo(string ruta)
        {
            ExigirAutorizacion();

            return _respaldos.RestaurarArchivo(ruta, NombreUsuario);
        }

        /// <summary>
        /// Recalcula los dígitos con los datos actuales y devuelve el resultado
        /// de verificar de nuevo. Lo decide el administrador tras revisar los datos.
        /// </summary>
        public ResultadoIntegridad RecalcularDigitos()
        {
            ExigirAutorizacion();

            _integridad.Recalcular(NombreUsuario);

            return _integridad.Verificar();
        }

        private void ExigirAutorizacion()
        {
            if (_autorizado == null)
            {
                throw new AccesoNoAutorizadoException(
                    Mensajes.T("Sign in with a user that can manage backups to continue."));
            }
        }
    }
}
