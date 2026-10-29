using System;

namespace Edelstahl.BLL.DTOs
{
    public class OrdenCompraDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public Guid ProveedorId { get; set; }
        public string Proveedor { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? EntregaEstimada { get; set; }
        public string Estado { get; set; }
        public decimal Neto { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoPendiente { get; set; }
    }

    public class DetalleOrdenCompraDto
    {
        public Guid DetalleId { get; set; }
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }
        public decimal Solicitada { get; set; }
        public decimal Recibida { get; set; }
        public decimal Pendiente { get; set; }
        public decimal Costo { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ItemCompra
    {
        public Guid ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Costo { get; set; }
        public decimal Descuento { get; set; }
    }

    public class RecepcionItem
    {
        public Guid DetalleOrdenCompraId { get; set; }
        public decimal Recibida { get; set; }
        public decimal Aceptada { get; set; }
        public decimal Rechazada { get; set; }
        public string MotivoRechazo { get; set; }
        public string Lote { get; set; }
        public string Colada { get; set; }
        public DateTime? Vencimiento { get; set; }
    }

    public class RecepcionDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string OrdenCompra { get; set; }
        public string Proveedor { get; set; }
        public DateTime Fecha { get; set; }
        public string RemitoProveedor { get; set; }
        public decimal Aceptado { get; set; }
        public decimal Rechazado { get; set; }
        public string RecibidoPor { get; set; }
    }

    public class SugerenciaCompraDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }
        public decimal Disponible { get; set; }
        public decimal PuntoReposicion { get; set; }
        public decimal Sugerido { get; set; }
        public decimal CostoActual { get; set; }
    }

    /// <summary>
    /// Producto que se pidio en presupuestos y no alcanza en existencias.
    /// </summary>
    public class FaltanteDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }
        public decimal Requerido { get; set; }
        public decimal Disponible { get; set; }
        public decimal Faltante { get; set; }
        public decimal CostoActual { get; set; }
        public bool Comprable { get; set; }
        public string Presupuestos { get; set; }
    }
}
