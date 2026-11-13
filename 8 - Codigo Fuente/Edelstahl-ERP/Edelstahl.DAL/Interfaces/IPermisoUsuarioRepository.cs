using System;
using System.Collections.Generic;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Persistencia de los permisos individuales: lo que el administrador agregó o quitó
    /// a un usuario respecto de los permisos de su rol.
    /// </summary>
    public interface IPermisoUsuarioRepository
    {
        List<PermisoIndividual> ObtenerDeUsuario(Guid usuarioId);

        /// <summary>Crea la excepción o, si el usuario ya tenía una para ese permiso, la reemplaza.</summary>
        void Guardar(PermisoIndividual permiso);

        /// <summary>Vuelve el permiso al valor de su rol (borra la excepción).</summary>
        void Quitar(Guid usuarioId, Permiso permiso);

        /// <summary>Borra todas las excepciones del usuario: queda con los permisos de su rol.</summary>
        int QuitarTodos(Guid usuarioId);
    }
}
