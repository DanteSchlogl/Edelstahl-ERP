using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edelstahl.BLL.DTOs
{
    /// <summary>
    /// Contiene la información necesaria para mostrar
    /// y seleccionar un presupuesto.
    /// </summary>
    public class PresupuestoSeleccionDto
    {
        public Guid Id { get; set; }

        public string Numero { get; set; }

        public DateTime FechaEmision { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public string Moneda { get; set; }

        public decimal Total { get; set; }

        public decimal Anticipo { get; set; }

        public string Estado { get; set; }

        public bool Vigente { get; set; }

        public bool PuedeConfirmarse { get; set; }
    }
}