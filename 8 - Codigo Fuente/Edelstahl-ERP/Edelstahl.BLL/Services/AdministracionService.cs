using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
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
            string nombreUsuario, string nombreCompleto, string email, string password, string nombreRol)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);
            return _usuarios.Registrar(nombreUsuario, nombreCompleto, email, password, nombreRol, false);
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

        public static IEnumerable<Permiso> PermisosDeRol(string nombreRol)
        {
            return MatrizPermisos.De(nombreRol).OrderBy(p => p.ToString()).ToList();
        }
    }
}
