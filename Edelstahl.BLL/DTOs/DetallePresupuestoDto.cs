using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edelstahl.BLL.DTOs
{
    /// <summary>
    /// Contiene la información de un renglón del presupuesto
    /// que se mostrará en la grilla de productos.
    /// </summary>
    public class DetallePresupuestoDto
    {
        public string Codigo { get; set; }

        public string Descripcion { get; set; }

        public string Tipo { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public DetallePresupuestoDto()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
            Tipo = string.Empty;
        }
    }
}
