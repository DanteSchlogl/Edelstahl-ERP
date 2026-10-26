using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Representa un producto, servicio o fabricación especial
    /// incluido en un pedido confirmado.
    /// </summary>
    public class DetallePedido : Entity
    {
        public Guid PedidoId { get; set; }

        public Guid DetallePresupuestoId { get; set; }

        public string Codigo { get; set; }

        public string Descripcion { get; set; }

        public string DescripcionTecnica { get; set; }

        public TipoItemPresupuesto TipoItem { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal PorcentajeDescuento { get; set; }

        public bool RequiereFabricacion { get; set; }

        public DetallePedido()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
            DescripcionTecnica = string.Empty;

            Cantidad = 0m;
            PrecioUnitario = 0m;
            PorcentajeDescuento = 0m;

            RequiereFabricacion = false;
        }

        public decimal CalcularImporteBruto()
        {
            return Cantidad * PrecioUnitario;
        }

        public decimal CalcularDescuento()
        {
            return CalcularImporteBruto()
                * PorcentajeDescuento
                / 100m;
        }

        public decimal CalcularSubtotal()
        {
            return CalcularImporteBruto()
                - CalcularDescuento();
        }
    }
}
