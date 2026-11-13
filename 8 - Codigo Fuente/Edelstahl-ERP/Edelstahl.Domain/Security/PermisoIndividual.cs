using System;

namespace Edelstahl.Domain.Security
{
    /// <summary>
    /// Excepción individual a los permisos del rol de un usuario: el administrador le agrega
    /// un permiso que su rol no tiene (Concedido = true) o le quita uno que su rol sí tiene
    /// (Concedido = false). Si no hay registro, el usuario tiene los permisos de su rol.
    /// </summary>
    public class PermisoIndividual
    {
        public Guid Id { get; set; }

        public Guid UsuarioId { get; set; }

        public Permiso Permiso { get; set; }

        /// <summary>true = permiso agregado; false = permiso quitado.</summary>
        public bool Concedido { get; set; }

        public DateTime FechaCambio { get; set; }

        /// <summary>Nombre del administrador que hizo el cambio.</summary>
        public string Administrador { get; set; }

        public PermisoIndividual()
        {
            Id = Guid.NewGuid();
            FechaCambio = DateTime.Now;
            Administrador = string.Empty;
        }
    }
}
