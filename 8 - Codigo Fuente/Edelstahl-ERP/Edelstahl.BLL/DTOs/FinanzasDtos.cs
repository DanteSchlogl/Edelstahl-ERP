using System;
using Edelstahl.Domain.Finanzas;

namespace Edelstahl.BLL.DTOs
{
    public class FacturaDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Tipo { get; set; }
        public Guid PedidoId { get; set; }
        public string Pedido { get; set; }
        public Guid ClienteId { get; set; }
        public string Cliente { get; set; }
        public DateTime Emision { get; set; }
        public DateTime? Vencimiento { get; set; }
        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public decimal Saldo { get; set; }
        public string Estado { get; set; }
        public EstadoPago EstadoClave { get; set; }
    }

    public class NotaCreditoDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Factura { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
    }

    public class CobroDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Cliente { get; set; }
        public string Factura { get; set; }
        public DateTime Fecha { get; set; }
        public string Medio { get; set; }
        public decimal Importe { get; set; }
        public string Referencia { get; set; }
        public string Estado { get; set; }
    }

    public class PagoDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Proveedor { get; set; }
        public string OrdenCompra { get; set; }
        public DateTime Fecha { get; set; }
        public string Medio { get; set; }
        public decimal Importe { get; set; }
        public string Referencia { get; set; }
        public string Estado { get; set; }
    }

    public class MovimientoCuentaDto
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        public string Referencia { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
        public string Observaciones { get; set; }
    }

    public class ResumenCuentaClienteDto
    {
        public Guid ClienteId { get; set; }
        public string Cliente { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal DeudaActual { get; set; }
        public decimal CreditoDisponible { get; set; }
        public int FacturasPendientes { get; set; }
    }

    /// <summary>Factura con saldo por cobrar, con lo abonado y la forma de pago prevista.</summary>
    public class CobroPendienteDto
    {
        public Guid FacturaId { get; set; }
        public Guid ClienteId { get; set; }
        public Guid PedidoId { get; set; }
        public string Cliente { get; set; }
        public string Pedido { get; set; }
        public string Factura { get; set; }
        public decimal Total { get; set; }
        public decimal Abonado { get; set; }
        public decimal Falta { get; set; }
        public string Estado { get; set; }
        public string FormaDePago { get; set; }
        public DateTime? Vencimiento { get; set; }
    }
}
