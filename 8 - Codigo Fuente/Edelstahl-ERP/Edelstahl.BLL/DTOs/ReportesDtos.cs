using System;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.BLL.DTOs
{
    public class PedidoSeguimientoDto
    {
        public Guid PedidoId { get; set; }
        public string Numero { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public EstadoPedido EstadoClave { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public string Produccion { get; set; }
        public string Logistica { get; set; }
        public string Facturacion { get; set; }
    }

    /// <summary>Un paso del historial de una orden de fabricacion o de un envio, con quien lo hizo.</summary>
    public class EventoHistorialDto
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Evento { get; set; }
        public string Detalle { get; set; }
    }

    public class EventoSeguimientoDto
    {
        public DateTime Fecha { get; set; }
        public string Etapa { get; set; }
        public string Detalle { get; set; }
    }

    public class IndicadoresDto
    {
        public decimal VentasDelMes { get; set; }
        public int PedidosActivos { get; set; }
        public int OrdenesEnProceso { get; set; }
        public int FacturasPendientes { get; set; }
        public decimal CuentasPorCobrar { get; set; }
        public decimal CuentasPorPagar { get; set; }
        public int ProductosEnAlerta { get; set; }
        public int EntregasDelMes { get; set; }
        public int OrdenesDeCompraAbiertas { get; set; }
    }

    public class VentaDto
    {
        public DateTime Fecha { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public string Estado { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public decimal TotalEnPesos { get; set; }
    }

    public class VentaClienteDto
    {
        public string Cliente { get; set; }
        public int Pedidos { get; set; }
        public decimal TotalEnPesos { get; set; }
    }

    public class VentaProductoDto
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal ImporteEnPesos { get; set; }
    }

    public class StockValorizadoDto
    {
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }
        public decimal Fisica { get; set; }
        public decimal Costo { get; set; }
        public decimal Valor { get; set; }
    }

    public class CuentaPorCobrarDto
    {
        public string Cliente { get; set; }
        public string Factura { get; set; }
        public DateTime Emision { get; set; }
        public DateTime? Vencimiento { get; set; }
        public int DiasVencida { get; set; }
        public decimal Total { get; set; }
        public decimal Saldo { get; set; }
    }

    public class CuentaPorPagarDto
    {
        public string Proveedor { get; set; }
        public string OrdenCompra { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public decimal Saldo { get; set; }
    }
}
