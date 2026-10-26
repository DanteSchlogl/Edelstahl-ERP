using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Representa un renglón o posición dentro de un presupuesto.
    /// </summary>
    public class DetallePresupuesto : Entity
    {
        public Guid PresupuestoId { get; set; }

        public string Codigo { get; set; }

        public string Descripcion { get; set; }

        public string DescripcionTecnica { get; set; }

        public TipoItemPresupuesto TipoItem { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal PorcentajeDescuento { get; set; }

        public DetallePresupuesto()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
            DescripcionTecnica = string.Empty;
            TipoItem = TipoItemPresupuesto.Producto;
            Cantidad = 1m;
            PrecioUnitario = 0m;
            PorcentajeDescuento = 0m;
        }

        public decimal CalcularImporteBruto()
        {
            return Cantidad * PrecioUnitario;
        }

        public decimal CalcularImporteDescuento()
        {
            return CalcularImporteBruto()
                * PorcentajeDescuento
                / 100m;
        }

        public decimal CalcularSubtotal()
        {
            return CalcularImporteBruto()
                - CalcularImporteDescuento();
        }
    }
}