using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Edelstahl.BLL.DTOs
{
    public class ClienteSeleccionDto
    {
        public Guid Id { get; set; }

        public string CUIT { get; set; }

        public string RazonSocial { get; set; }

        public string Localidad { get; set; }

        public decimal CreditoDisponible { get; set; }

        public bool Activo { get; set; }
    }
}