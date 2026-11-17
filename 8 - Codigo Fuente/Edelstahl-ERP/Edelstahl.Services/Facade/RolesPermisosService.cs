using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Logic;

namespace Edelstahl.Services.Facade
{
    /// <summary>
    /// Los roles del sistema como Familias y sus permisos como Patentes: guardarlos en la base,
    /// asignarlos a los usuarios y comprobar que coincidan con la matriz de permisos.
    /// </summary>
    public static class RolesPermisosService
    {
        /// <summary>Guarda patentes, familias y relaciones de todos los roles. Se puede repetir.</summary>
        public static ResultadoSiembra Sembrar()
        {
            return RolesPermisosLogic.Sembrar();
        }

        /// <summary>Asigna a cada usuario existente la familia con el nombre de su rol. Devuelve cuántas asignó.</summary>
        public static int AsignarRolesAUsuarios()
        {
            return RolesPermisosLogic.AsignarRolesAUsuarios();
        }

        /// <summary>Diferencias entre el Composite guardado y MatrizPermisos (vacía si coinciden).</summary>
        public static List<string> Verificar()
        {
            return RolesPermisosLogic.Verificar();
        }

        /// <summary>Árbol de cada rol tal como se define en código (sin leer la base).</summary>
        public static Dictionary<string, Familia> DefinirRoles()
        {
            return RolesPermisosLogic.DefinirRoles();
        }
    }
}
