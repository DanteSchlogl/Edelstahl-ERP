using System;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Mantiene los datos del usuario autenticado durante la ejecución.
    /// </summary>
    public static class SesionActual
    {
        public static Usuario Usuario { get; private set; }

        public static bool EstaIniciada
        {
            get { return Usuario != null; }
        }

        /// <summary>
        /// Nombre del usuario para registrar quién realizó una operación.
        /// </summary>
        public static string NombreUsuario
        {
            get { return EstaIniciada ? Usuario.NombreUsuario : "sistema"; }
        }

        public static string NombreRol
        {
            get
            {
                return EstaIniciada && Usuario.Rol != null
                    ? Usuario.Rol.Nombre
                    : string.Empty;
            }
        }

        public static void Iniciar(Usuario usuario)
        {
            Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        }

        public static void Cerrar()
        {
            Usuario = null;
        }

        public static bool TieneRol(string nombreRol)
        {
            return EstaIniciada &&
                   Usuario.Rol != null &&
                   string.Equals(
                       Usuario.Rol.Nombre,
                       nombreRol,
                       StringComparison.OrdinalIgnoreCase);
        }

        public static bool Tiene(Permiso permiso)
        {
            return EstaIniciada && MatrizPermisos.Tiene(NombreRol, permiso);
        }
    }
}
