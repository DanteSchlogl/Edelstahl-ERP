using System;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.DataAccess.Implementations;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.Logic
{
    /// <summary>Privilegios de un usuario: familias (roles) y patentes asignadas.</summary>
    internal static class PrivilegiosLogic
    {
        private static readonly UsuarioFamiliaRepository _usuarioFamilia = new UsuarioFamiliaRepository();
        private static readonly UsuarioPatenteRepository _usuarioPatente = new UsuarioPatenteRepository();

        public static void AgregarFamilia(Familia familia, Guid idUsuario)
        {
            Validar(familia, idUsuario);

            UsuarioPrivilegios usuario = new UsuarioPrivilegios { IdUsuario = idUsuario };

            if (!_usuarioFamilia.Existe(familia, usuario))
            {
                _usuarioFamilia.Add(familia, usuario);
            }
        }

        public static void AgregarPatente(Patente patente, Guid idUsuario)
        {
            Validar(patente, idUsuario);

            UsuarioPrivilegios usuario = new UsuarioPrivilegios { IdUsuario = idUsuario };

            if (!_usuarioPatente.Existe(patente, usuario))
            {
                _usuarioPatente.Add(patente, usuario);
            }
        }

        public static void QuitarFamilia(Familia familia, Guid idUsuario)
        {
            Validar(familia, idUsuario);
            _usuarioFamilia.Delete(familia, new UsuarioPrivilegios { IdUsuario = idUsuario });
        }

        public static void QuitarPatente(Patente patente, Guid idUsuario)
        {
            Validar(patente, idUsuario);
            _usuarioPatente.Delete(patente, new UsuarioPrivilegios { IdUsuario = idUsuario });
        }

        /// <summary>Recupera los privilegios del usuario con todo el árbol de familias ya armado.</summary>
        public static UsuarioPrivilegios Obtener(Guid idUsuario, string nombreUsuario)
        {
            UsuarioPrivilegios usuario = new UsuarioPrivilegios
            {
                IdUsuario = idUsuario,
                NombreUsuario = nombreUsuario ?? string.Empty
            };

            usuario.Privilegios.AddRange(_usuarioFamilia.GetByObject(usuario));
            usuario.Privilegios.AddRange(_usuarioPatente.GetByObject(usuario));

            return usuario;
        }

        private static void Validar(Component componente, Guid idUsuario)
        {
            if (componente == null)
            {
                throw new ArgumentNullException("componente");
            }

            if (componente.Id == Guid.Empty || idUsuario == Guid.Empty)
            {
                throw new DatoInvalidoException("The user and the component must be saved before they can be related.");
            }
        }
    }
}
