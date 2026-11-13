using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Security
{
    /// <summary>
    /// Representa un perfil de acceso dentro de Edelstahl ERP.
    /// </summary>
    public class Rol : Entity
    {
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaAlta { get; set; }

        public Rol()
        {
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Activo = true;
            FechaAlta = DateTime.Now;
        }
    }
}

