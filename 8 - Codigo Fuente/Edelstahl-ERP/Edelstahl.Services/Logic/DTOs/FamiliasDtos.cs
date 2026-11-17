using System;
using System.Collections.Generic;

namespace Edelstahl.Services.Logic.DTOs
{
    /// <summary>Una familia (rol o grupo de permisos) con cuántos permisos otorga en total.</summary>
    public class FamiliaDto
    {
        public Guid Id { get; set; }

        public string Nombre { get; set; }

        public int Permisos { get; set; }
    }

    /// <summary>
    /// Nodo del árbol de permisos de un usuario: una familia (con hijos) o un permiso suelto.
    /// Clave es el DataKey del permiso (vacío en las familias).
    /// </summary>
    public class NodoPermisoDto
    {
        public string Nombre { get; set; }

        public string Clave { get; set; }

        public bool EsFamilia { get; set; }

        public List<NodoPermisoDto> Hijos { get; set; }

        public NodoPermisoDto()
        {
            Nombre = string.Empty;
            Clave = string.Empty;
            Hijos = new List<NodoPermisoDto>();
        }
    }
}
