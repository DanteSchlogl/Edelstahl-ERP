using System;
using Edelstahl.Services.Logic.DTOs;
using Edelstahl.Services.Logic;
using Edelstahl.Services.DataAccess.Factory;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Facade;

namespace Edelstahl.Services.Logic.Infrastructure
{
    /// <summary>
    /// Permisos reales de un usuario.
    ///
    /// Base: las patentes que le llegan por el Composite (las familias que tiene asignadas, con sus
    /// familias hijas, más las patentes directas, sin repetidos). Si el usuario todavía no tiene nada
    /// cargado en el Composite, o la base no lo tiene, se usan los permisos de su rol (MatrizPermisos).
    ///
    /// Encima de esa base se aplican las excepciones individuales: lo que el administrador le agregó
    /// y lo que le quitó. Todo se lee una vez por usuario y se guarda en memoria hasta que el
    /// administrador lo cambia o el usuario vuelve a ingresar.
    /// </summary>
    public static class PermisosEfectivos
    {
        private static readonly object Candado = new object();
        private static readonly Dictionary<Guid, Dictionary<Permiso, bool>> Cache = new Dictionary<Guid, Dictionary<Permiso, bool>>();

        // Permisos que le llegan por el Composite; null guardado = sin datos de Composite (usar la matriz).
        private static readonly Dictionary<Guid, HashSet<Permiso>> CacheComposite = new Dictionary<Guid, HashSet<Permiso>>();

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

            return TieneDeBase(usuario, rol, permiso);
        }

        /// <summary>El permiso sin contar las excepciones individuales: Composite o, si no hay, la matriz del rol.</summary>
        private static bool TieneDeBase(Usuario usuario, string rol, Permiso permiso)
        {
            HashSet<Permiso> delComposite = PermisosDelComposite(usuario);

            if (delComposite == null)
            {
                return MatrizPermisos.Tiene(rol, permiso);
            }

            // Administrar usuarios, roles y respaldos no se puede repartir con patentes sueltas: solo vale si lo trae el rol.
            return delComposite.Contains(permiso)
                && (!MatrizPermisos.EsSoloAdministrador(permiso) || MatrizPermisos.Tiene(rol, permiso));
        }

        /// <summary>
        /// Permisos que el Composite le da al usuario (familias con sus hijas y patentes directas), o null
        /// si no hay datos de Composite para él o no se pueden leer.
        /// </summary>
        private static HashSet<Permiso> PermisosDelComposite(Usuario usuario)
        {
            lock (Candado)
            {
                HashSet<Permiso> guardado;

                if (CacheComposite.TryGetValue(usuario.Id, out guardado))
                {
                    return guardado;
                }
            }

            HashSet<Permiso> leido = null;

            // Solo existe la base de servicios en modo SQL Server; en demostración se usa la matriz.
            if (Edelstahl.Services.Configuration.ApplicationMode.UsesSqlServer)
            {
                try
                {
                    UsuarioPrivilegios privilegios = PrivilegiosService.Obtener(usuario.Id, usuario.NombreUsuario);

                    if (privilegios.Privilegios.Count > 0)
                    {
                        leido = new HashSet<Permiso>(
                            privilegios.Patentes
                                .Where(p => p.PermisoAsociado.HasValue)
                                .Select(p => p.PermisoAsociado.Value));
                    }
                }
                catch (Exception)
                {
                    // Si la base todavía no tiene las tablas del Composite, vale la matriz del rol.
                    leido = null;
                }
            }

            lock (Candado)
            {
                CacheComposite[usuario.Id] = leido;
            }

            return leido;
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
                CacheComposite.Remove(usuarioId);
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
                if (FactoryServicios.PermisoUsuarioRepository != null)
                {
                    foreach (PermisoIndividual p in FactoryServicios.PermisoUsuarioRepository.ObtenerDeUsuario(usuarioId))
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
