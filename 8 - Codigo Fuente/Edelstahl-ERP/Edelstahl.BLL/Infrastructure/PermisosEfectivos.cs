using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Permisos reales de un usuario: los de su rol, más los que el administrador le agregó
    /// y menos los que le quitó. Las excepciones individuales se leen una vez por usuario y se
    /// guardan en memoria hasta que el administrador las cambia o el usuario vuelve a ingresar.
    /// </summary>
    public static class PermisosEfectivos
    {
        private static readonly object Candado = new object();
        private static readonly Dictionary<Guid, Dictionary<Permiso, bool>> Cache = new Dictionary<Guid, Dictionary<Permiso, bool>>();

        public static bool Tiene(Usuario usuario, Permiso permiso)
        {
            if (usuario == null)
            {
                return false;
            }

            string rol = usuario.Rol == null ? string.Empty : usuario.Rol.Nombre;

            // El administrador siempre conserva todo: no se le pueden quitar permisos.
            if (string.Equals(rol, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase))
            {
                return MatrizPermisos.Tiene(rol, permiso);
            }

            bool concedido;

            if (Excepciones(usuario.Id).TryGetValue(permiso, out concedido))
            {
                if (!concedido)
                {
                    return false;
                }

                // Administrar usuarios, roles y respaldos no se puede repartir: solo vale si lo trae el rol.
                return !MatrizPermisos.EsSoloAdministrador(permiso) || MatrizPermisos.Tiene(rol, permiso);
            }

            return MatrizPermisos.Tiene(rol, permiso);
        }

        public static List<Permiso> De(Usuario usuario)
        {
            return ((Permiso[])Enum.GetValues(typeof(Permiso))).Where(p => Tiene(usuario, p)).ToList();
        }

        /// <summary>Descarta lo guardado en memoria para que la próxima consulta lea la base.</summary>
        public static void Invalidar(Guid usuarioId)
        {
            lock (Candado)
            {
                Cache.Remove(usuarioId);
            }
        }

        private static Dictionary<Permiso, bool> Excepciones(Guid usuarioId)
        {
            lock (Candado)
            {
                Dictionary<Permiso, bool> guardado;

                if (Cache.TryGetValue(usuarioId, out guardado))
                {
                    return guardado;
                }
            }

            Dictionary<Permiso, bool> leido = new Dictionary<Permiso, bool>();

            try
            {
                if (FactoryDataAccess.PermisoUsuarioRepository != null)
                {
                    foreach (PermisoIndividual p in FactoryDataAccess.PermisoUsuarioRepository.ObtenerDeUsuario(usuarioId))
                    {
                        leido[p.Permiso] = p.Concedido;
                    }
                }
            }
            catch (Exception)
            {
                // Si la base todavía no tiene la tabla, el usuario queda con los permisos de su rol.
                leido.Clear();
            }

            lock (Candado)
            {
                Cache[usuarioId] = leido;
            }

            return leido;
        }
    }
}
