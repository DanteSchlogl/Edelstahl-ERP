using System;
using System.Collections.Generic;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Finanzas
{
    /// <summary>
    /// Tipo de comprobante fiscal emitido. La factura A discrimina
    /// el IVA; la B lo incluye dentro del precio final.
    /// </summary>
    public enum TipoComprobante
    {
        FacturaA = 1,
        FacturaB = 2
    }

    public enum EstadoPago
    {
        Pendiente = 1,
        Parcial = 2,
        Pagada = 3,
        Vencida = 4,
        Anulada = 5
    }

    public class Factura : Entity
    {
        public Guid PedidoId { get; set; }
        public Guid ClienteId { get; set; }
        public int PuntoVenta { get; set; }
        public string Numero { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public TipoComprobante Tipo { get; set; }
        public int Moneda { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal MontoNeto { get; set; }
        public decimal Iva { get; set; }
        public decimal OtrosImpuestos { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal SaldoPendiente { get; set; }
        public EstadoPago EstadoPago { get; set; }
        public string CondicionPago { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleFactura> Detalles { get; set; }

        public Factura()
        {
            Numero = string.Empty;
            PuntoVenta = 1;
            FechaEmision = DateTime.Now;
            Moneda = 1;
            TipoCambio = 1m;
            EstadoPago = EstadoPago.Pendiente;
            CondicionPago = string.Empty;
            Observaciones = string.Empty;
            Detalles = new List<DetalleFactura>();
        }
    }

    public class DetalleFactura : Entity
    {
        public Guid FacturaId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal Subtotal { get; set; }

        public DetalleFactura()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
        }
    }

    public class MedioPago : Entity
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool RequiereReferencia { get; set; }
        public bool RequiereEntidadFinanciera { get; set; }
        public bool Activo { get; set; }

        public MedioPago()
        {
            Codigo = string.Empty;
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Activo = true;
        }
    }

    public class CobroCliente : Entity
    {
        public string Numero { get; set; }
        public Guid ClienteId { get; set; }
        public Guid? FacturaId { get; set; }
        public Guid MedioPagoId { get; set; }
        public DateTime FechaCobro { get; set; }
        public DateTime? FechaAcreditacion { get; set; }
        public decimal Importe { get; set; }
        public string Referencia { get; set; }
        public string EntidadFinanciera { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }

        public CobroCliente()
        {
            Numero = string.Empty;
            FechaCobro = DateTime.Now;
            Referencia = string.Empty;
            EntidadFinanciera = string.Empty;
            Estado = "CONFIRMADO";
            Observaciones = string.Empty;
        }
    }

    public class PagoProveedor : Entity
    {
        public string Numero { get; set; }
        public Guid ProveedorId { get; set; }
        public Guid? OrdenCompraId { get; set; }
        public Guid MedioPagoId { get; set; }
        public DateTime FechaPago { get; set; }
        public DateTime? FechaDebito { get; set; }
        public decimal Importe { get; set; }
        public string Referencia { get; set; }
        public string EntidadFinanciera { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }

        public PagoProveedor()
        {
            Numero = string.Empty;
            FechaPago = DateTime.Now;
            Referencia = string.Empty;
            EntidadFinanciera = string.Empty;
            Estado = "CONFIRMADO";
            Observaciones = string.Empty;
        }
    }

    public enum TipoMovimientoCuenta
    {
        Factura = 1,
        Cobro = 2,
        NotaCredito = 3,
        NotaDebito = 4,
        Ajuste = 5,
        OrdenCompra = 6,
        Pago = 7
    }

    public class MovimientoCuentaCliente : Entity
    {
        public Guid ClienteId { get; set; }
        public Guid? FacturaId { get; set; }
        public Guid? CobroId { get; set; }
        public Guid? NotaCreditoId { get; set; }
        public DateTime Fecha { get; set; }
        public TipoMovimientoCuenta Tipo { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal SaldoResultante { get; set; }
        public string Referencia { get; set; }
        public string Observaciones { get; set; }

        public MovimientoCuentaCliente()
        {
            Fecha = DateTime.Now;
            Referencia = string.Empty;
            Observaciones = string.Empty;
        }
    }

    public class MovimientoCuentaProveedor : Entity
    {
        public Guid ProveedorId { get; set; }
        public Guid? OrdenCompraId { get; set; }
        public Guid? PagoId { get; set; }
        public DateTime Fecha { get; set; }
        public TipoMovimientoCuenta Tipo { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal SaldoResultante { get; set; }
        public string Referencia { get; set; }
        public string Observaciones { get; set; }

        public MovimientoCuentaProveedor()
        {
            Fecha = DateTime.Now;
            Referencia = string.Empty;
            Observaciones = string.Empty;
        }
    }

    public class NotaCredito : Entity
    {
        public string Numero { get; set; }
        public Guid FacturaId { get; set; }
        public Guid ClienteId { get; set; }
        public Guid? DevolucionId { get; set; }
        public TipoComprobante Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public decimal MontoNeto { get; set; }
        public decimal Iva { get; set; }
        public decimal MontoTotal { get; set; }
        public string Estado { get; set; }

        public NotaCredito()
        {
            Numero = string.Empty;
            Fecha = DateTime.Now;
            Motivo = string.Empty;
            Estado = "EMITIDA";
        }
    }
}
