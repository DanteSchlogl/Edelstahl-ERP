using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Administración de usuarios y roles. Solo la usa quien tiene permiso de
    /// administración; cada operación queda en la bitácora.
    /// </summary>
    public class AdministracionService
    {
        private readonly UsuarioService _usuarios;

        public AdministracionService()
        {
            _usuarios = new UsuarioService();
        }

        public List<Usuario> ObtenerUsuarios()
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);
            return _usuarios.ObtenerTodos();
        }

        public List<Rol> ObtenerRoles()
        {
            Autorizacion.Exigir(Permiso.RolesAdministrar);
            return _usuarios.ObtenerRoles();
        }

        public Usuario CrearUsuario(
            string nombreUsuario, string nombreCompleto, string email, string password, string nombreRol,
            string preguntaSeguridad = null, string respuestaSeguridad = null)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            Usuario usuario = _usuarios.Registrar(nombreUsuario, nombreCompleto, email, password, nombreRol, false);

            if (!string.IsNullOrWhiteSpace(preguntaSeguridad))
            {
                _usuarios.DefinirPreguntaSeguridad(usuario.Id, preguntaSeguridad, respuestaSeguridad);
            }

            return usuario;
        }

        /// <summary>El administrador define o reemplaza la pregunta de seguridad de un usuario.</summary>
        public void DefinirPreguntaSeguridad(Guid usuarioId, string pregunta, string respuesta)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);
            _usuarios.DefinirPreguntaSeguridad(usuarioId, pregunta, respuesta);
        }

        public void CambiarEstado(Guid usuarioId, bool activo)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            if (!activo && SesionActual.EstaIniciada && SesionActual.Usuario.Id == usuarioId)
            {
                throw Mensajes.Regla("You cannot deactivate the user you are logged in with.");
            }

            _usuarios.CambiarEstado(usuarioId, activo);
        }

        public void Desbloquear(Guid usuarioId)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);
            _usuarios.Desbloquear(usuarioId);
        }

        public void RestablecerPassword(Guid usuarioId, string nuevaPassword)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            Usuario usuario = _usuarios.ObtenerPorId(usuarioId);

            if (usuario == null)
            {
                throw Mensajes.Regla("The requested user was not found.");
            }

            PasswordHashData datos = PasswordHasher.CrearHash(UsuarioService.ClaveValida(nuevaPassword));

            usuario.PasswordHash = datos.Hash;
            usuario.PasswordSalt = datos.Salt;
            usuario.Iteraciones = datos.Iteraciones;
            usuario.DebeCambiarPassword = true;
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;

            FactoryDataAccess.UsuarioRepository.Update(usuario);

            new BitacoraService().RegistrarAuditoria(
                "Seguridad", "Restablecer contrasena", "Usuario", usuario.Id,
                "Se restableció la contraseña de " + usuario.NombreUsuario);
        }

        /// <summary>El administrador corrige el nombre completo y el correo de un usuario.</summary>
        public void ModificarDatos(Guid usuarioId, string nombreCompleto, string email)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            Usuario usuario = _usuarios.ObtenerPorId(usuarioId);

            if (usuario == null)
            {
                throw Mensajes.Regla("The requested user was not found.");
            }

            if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(email))
            {
                throw Mensajes.Regla("The full name and the email are required.");
            }

            try
            {
                new System.Net.Mail.MailAddress(email.Trim());
            }
            catch (FormatException)
            {
                throw Mensajes.Regla("The email address entered is not valid.");
            }

            usuario.NombreCompleto = nombreCompleto.Trim();
            usuario.Email = email.Trim();
            FactoryDataAccess.UsuarioRepository.Update(usuario);

            new BitacoraService().RegistrarAuditoria("Seguridad", "Modificar usuario", "Usuario", usuario.Id,
                "Se modificaron los datos de " + usuario.NombreUsuario);
        }

        public void CambiarRol(Guid usuarioId, string nombreRol)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            Usuario usuario = _usuarios.ObtenerPorId(usuarioId);
            Rol rol = FactoryDataAccess.RolRepository.GetByNombre(nombreRol);

            if (usuario == null)
            {
                throw Mensajes.Regla("The requested user was not found.");
            }

            if (rol == null || !rol.Activo)
            {
                throw Mensajes.Regla("The selected role is not active.");
            }

            usuario.RolId = rol.Id;
            usuario.Rol = rol;
            FactoryDataAccess.UsuarioRepository.Update(usuario);

            new BitacoraService().RegistrarAuditoria(
                "Seguridad", "Cambio de rol", "Usuario", usuario.Id,
                usuario.NombreUsuario + " pasó al rol " + rol.Nombre);

            // Los permisos individuales eran respecto del rol anterior: con el rol nuevo se parte de cero.
            QuitarPermisosIndividuales(usuario);
        }

        public void CambiarEstadoRol(Guid rolId, bool activo)
        {
            Autorizacion.Exigir(Permiso.RolesAdministrar);

            Rol rol = FactoryDataAccess.RolRepository.GetById(rolId);

            if (rol == null)
            {
                throw Mensajes.Regla("The selected role was not found.");
            }

            if (!activo && string.Equals(rol.Nombre, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase))
            {
                throw Mensajes.Regla("The administrator role cannot be deactivated.");
            }

            rol.Activo = activo;
            FactoryDataAccess.RolRepository.Update(rol);

            new BitacoraService().RegistrarAuditoria(
                "Seguridad", activo ? "Activar rol" : "Desactivar rol", "Rol", rol.Id, rol.Nombre);
        }

        // ---------------------------------------------------------------
        // Permisos individuales: solo el administrador los da y los quita
        // ---------------------------------------------------------------

        private static void ExigirAdministrador()
        {
            Autorizacion.Exigir(Permiso.RolesAdministrar);

            if (!SesionActual.TieneRol(NombresRol.Administrador))
            {
                throw new AccesoNoAutorizadoException(Mensajes.T("Only an administrator can change permissions."));
            }
        }

        private string RolDe(Usuario usuario)
        {
            Rol rol = FactoryDataAccess.RolRepository.GetById(usuario.RolId);
            return rol == null ? string.Empty : rol.Nombre;
        }

        private Usuario ObtenerUsuarioParaPermisos(Guid usuarioId)
        {
            Usuario usuario = _usuarios.ObtenerPorId(usuarioId);

            if (usuario == null)
            {
                throw Mensajes.Regla("The requested user was not found.");
            }

            return usuario;
        }

        /// <summary>Cada permiso con lo que trae el rol del usuario y lo que el administrador le agregó o quitó.</summary>
        public List<PermisoDeUsuarioDto> ObtenerPermisosDeUsuario(Guid usuarioId)
        {
            ExigirAdministrador();

            Usuario usuario = ObtenerUsuarioParaPermisos(usuarioId);
            string rol = RolDe(usuario);
            Dictionary<Permiso, bool> excepciones = FactoryDataAccess.PermisoUsuarioRepository.ObtenerDeUsuario(usuarioId)
                .ToDictionary(p => p.Permiso, p => p.Concedido);

            return ((Permiso[])Enum.GetValues(typeof(Permiso))).Select(p =>
            {
                bool delRol = MatrizPermisos.Tiene(rol, p);
                bool individual;
                bool hay = excepciones.TryGetValue(p, out individual);

                return new PermisoDeUsuarioDto
                {
                    Permiso = p,
                    DelRol = delRol,
                    Efectivo = hay ? individual : delRol,
                    SoloAdministrador = MatrizPermisos.EsSoloAdministrador(p),
                    Individual = hay ? (bool?)individual : null
                };
            }).ToList();
        }

        /// <summary>
        /// Deja al usuario con exactamente los permisos pedidos: lo que difiere de su rol se guarda como
        /// permiso agregado o quitado, y lo que coincide con el rol vuelve al valor del rol. Cada cambio
        /// queda en la bitácora.
        /// </summary>
        public int CambiarPermisosDeUsuario(Guid usuarioId, IDictionary<Permiso, bool> deseados)
        {
            ExigirAdministrador();

            if (deseados == null)
            {
                throw new ArgumentNullException(nameof(deseados));
            }

            Usuario usuario = ObtenerUsuarioParaPermisos(usuarioId);
            string rol = RolDe(usuario);

            if (string.Equals(rol, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase))
            {
                throw Mensajes.Regla("The permissions of an administrator cannot be changed.");
            }

            Dictionary<Permiso, bool> actuales = FactoryDataAccess.PermisoUsuarioRepository.ObtenerDeUsuario(usuarioId)
                .ToDictionary(p => p.Permiso, p => p.Concedido);

            BitacoraService bitacora = new BitacoraService();
            int cambios = 0;

            foreach (KeyValuePair<Permiso, bool> par in deseados)
            {
                Permiso permiso = par.Key;
                bool delRol = MatrizPermisos.Tiene(rol, permiso);
                bool individual;
                bool hay = actuales.TryGetValue(permiso, out individual);
                bool efectivoActual = hay ? individual : delRol;

                if (par.Value == efectivoActual)
                {
                    continue;
                }

                if (MatrizPermisos.EsSoloAdministrador(permiso))
                {
                    throw Mensajes.Regla("This permission can only belong to administrators.");
                }

                if (par.Value == delRol)
                {
                    FactoryDataAccess.PermisoUsuarioRepository.Quitar(usuarioId, permiso);
                    bitacora.RegistrarAuditoria("Seguridad", "Permiso restablecido", "Usuario", usuarioId,
                        "Se restableció el permiso " + permiso + " de " + usuario.NombreUsuario + " al valor de su rol");
                }
                else
                {
                    FactoryDataAccess.PermisoUsuarioRepository.Guardar(new PermisoIndividual
                    {
                        UsuarioId = usuarioId,
                        Permiso = permiso,
                        Concedido = par.Value,
                        Administrador = SesionActual.NombreUsuario
                    });

                    bitacora.RegistrarAuditoria("Seguridad", par.Value ? "Permiso agregado" : "Permiso quitado", "Usuario", usuarioId,
                        (par.Value ? "Se agregó el permiso " : "Se quitó el permiso ") + permiso
                        + (par.Value ? " a " : " a ") + usuario.NombreUsuario);
                }

                cambios++;
            }

            PermisosEfectivos.Invalidar(usuarioId);
            return cambios;
        }

        /// <summary>El usuario vuelve a tener exactamente los permisos de su rol.</summary>
        public void RestablecerPermisos(Guid usuarioId)
        {
            ExigirAdministrador();

            Usuario usuario = ObtenerUsuarioParaPermisos(usuarioId);
            QuitarPermisosIndividuales(usuario);
        }

        private static void QuitarPermisosIndividuales(Usuario usuario)
        {
            int quitados = FactoryDataAccess.PermisoUsuarioRepository.QuitarTodos(usuario.Id);
            PermisosEfectivos.Invalidar(usuario.Id);

            if (quitados > 0)
            {
                new BitacoraService().RegistrarAuditoria(
                    "Seguridad", "Permisos restablecidos", "Usuario", usuario.Id,
                    "Se restablecieron los permisos de " + usuario.NombreUsuario + " a los de su rol.");
            }
        }

        public static IEnumerable<Permiso> PermisosDeRol(string nombreRol)
        {
            return MatrizPermisos.De(nombreRol).OrderBy(p => p.ToString()).ToList();
        }
    }
}
