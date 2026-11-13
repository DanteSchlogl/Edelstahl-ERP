using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.DTOs
{
    /// <summary>
    /// Estado de un permiso para un usuario: si lo trae su rol, si lo tiene realmente y
    /// si el administrador lo cambió respecto del rol.
    /// </summary>
    public class PermisoDeUsuarioDto
    {
        public Permiso Permiso { get; set; }

        public bool DelRol { get; set; }

        public bool Efectivo { get; set; }

        /// <summary>Administrar usuarios, roles y respaldos: no se pueden dar a un usuario puntual.</summary>
        public bool SoloAdministrador { get; set; }

        /// <summary>true = agregado por el administrador; false = quitado; null = el del rol.</summary>
        public bool? Individual { get; set; }
    }
}
