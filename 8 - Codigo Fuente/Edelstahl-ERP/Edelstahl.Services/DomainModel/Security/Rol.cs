using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Edelstahl.Services.DomainModel.Security
{
    /// <summary>
    /// Representa un perfil de acceso dentro de Edelstahl ERP.
    /// </summary>
    public class Rol
    {
        public Guid Id { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaAlta { get; set; }

        public Rol()
        {
            Id = Guid.NewGuid();
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Activo = true;
            FechaAlta = DateTime.Now;
        }
    }
}

