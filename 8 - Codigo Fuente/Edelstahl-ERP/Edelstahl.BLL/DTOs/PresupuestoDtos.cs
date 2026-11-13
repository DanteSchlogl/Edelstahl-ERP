using System;
using System.Collections.Generic;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.BLL.DTOs
{
    public class LineaPresupuestoSolicitud
    {
        public Guid? ProductoId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string DescripcionTecnica { get; set; }
        public TipoItemPresupuesto Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
    }

    public class PresupuestoSolicitud
    {
        public Guid ClienteId { get; set; }
        public Moneda Moneda { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal PorcentajeIva { get; set; }
        public decimal PorcentajeRecargo { get; set; }
        public decimal PorcentajeDescuentoGeneral { get; set; }
        public decimal PorcentajeAnticipo { get; set; }
        public string CondicionPago { get; set; }
        public string PlazoEntrega { get; set; }
        public bool EntregaIncluida { get; set; }
        public string Observaciones { get; set; }
        public int VigenciaDias { get; set; }
        public bool Enviar { get; set; }
        public List<LineaPresupuestoSolicitud> Lineas { get; set; }

        public PresupuestoSolicitud()
        {
            Moneda = Moneda.DolaresEstadounidenses;
            TipoCambio = 1m;
            PorcentajeIva = 21m;
            VigenciaDias = 7;
            Lineas = new List<LineaPresupuestoSolicitud>();
        }
    }

    public class PresupuestoResumenDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public Guid ClienteId { get; set; }
        public string Cliente { get; set; }
        public DateTime Emision { get; set; }
        public DateTime Vencimiento { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public decimal Anticipo { get; set; }
        public string Estado { get; set; }
        public EstadoPresupuesto EstadoClave { get; set; }
        public int Items { get; set; }
    }
}
