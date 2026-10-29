using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Compras
{
    public class Proveedor : Entity
    {
        public string Cuit { get; set; }
        public string RazonSocial { get; set; }
        public string NombreFantasia { get; set; }
        public string Direccion { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string ContactoComercial { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal SaldoCuenta { get; set; }
        public int PlazoEntregaDias { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public Proveedor()
        {
            Cuit = string.Empty;
            RazonSocial = string.Empty;
            NombreFantasia = string.Empty;
            Direccion = string.Empty;
            Localidad = string.Empty;
            Provincia = string.Empty;
            CodigoPostal = string.Empty;
            Email = string.Empty;
            Telefono = string.Empty;
            ContactoComercial = string.Empty;
            PlazoEntregaDias = 7;
            Activo = true;
            FechaAlta = DateTime.Now;
        }
    }

    public enum EstadoOrdenCompra
    {
        Borrador = 1,
        Emitida = 2,
        ParcialmenteRecibida = 3,
        Recibida = 4,
        Cancelada = 5
    }

    public class OrdenCompra : Entity
    {
        public string Numero { get; set; }
        public Guid ProveedorId { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaEntregaEstimada { get; set; }
        public EstadoOrdenCompra Estado { get; set; }
        public string CondicionPago { get; set; }
        public decimal MontoNeto { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal Impuestos { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleOrdenCompra> Detalles { get; set; }

        public OrdenCompra()
        {
            Numero = string.Empty;
            FechaEmision = DateTime.Now;
            Estado = EstadoOrdenCompra.Borrador;
            CondicionPago = string.Empty;
            Observaciones = string.Empty;
            Detalles = new List<DetalleOrdenCompra>();
        }

        public void Recalcular(decimal porcentajeIva)
        {
            decimal bruto = Detalles.Sum(d => d.CantidadSolicitada * d.CostoUnitario);
            MontoDescuento = Detalles.Sum(
                d => d.CantidadSolicitada * d.CostoUnitario * d.PorcentajeDescuento / 100m);
            MontoNeto = bruto - MontoDescuento;
            Impuestos = Math.Round(MontoNeto * porcentajeIva / 100m, 2);
            MontoTotal = MontoNeto + Impuestos;
        }
    }

    public class DetalleOrdenCompra : Entity
    {
        public Guid OrdenCompraId { get; set; }
        public Guid ProductoId { get; set; }
        public decimal CantidadSolicitada { get; set; }
        public decimal CantidadRecibida { get; set; }
        public string UnidadMedida { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }

        public DetalleOrdenCompra()
        {
            UnidadMedida = string.Empty;
            Estado = "PENDIENTE";
            Observaciones = string.Empty;
        }

        public decimal Subtotal
        {
            get
            {
                decimal bruto = CantidadSolicitada * CostoUnitario;
                return bruto - (bruto * PorcentajeDescuento / 100m);
            }
        }

        public decimal Pendiente
        {
            get { return CantidadSolicitada - CantidadRecibida; }
        }
    }

    public class RecepcionCompra : Entity
    {
        public string Numero { get; set; }
        public Guid OrdenCompraId { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string NumeroRemitoProveedor { get; set; }
        public string Estado { get; set; }
        public string RecibidoPor { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleRecepcionCompra> Detalles { get; set; }

        public RecepcionCompra()
        {
            Numero = string.Empty;
            FechaRecepcion = DateTime.Now;
            NumeroRemitoProveedor = string.Empty;
            Estado = "REGISTRADA";
            RecibidoPor = string.Empty;
            Observaciones = string.Empty;
            Detalles = new List<DetalleRecepcionCompra>();
        }
    }

    public class DetalleRecepcionCompra : Entity
    {
        public Guid RecepcionId { get; set; }
        public Guid DetalleOrdenCompraId { get; set; }
        public Guid ProductoId { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadAceptada { get; set; }
        public decimal CantidadRechazada { get; set; }
        public string MotivoRechazo { get; set; }
        public string EstadoMaterial { get; set; }
        public string NumeroLote { get; set; }
        public string CertificadoColada { get; set; }

        public DetalleRecepcionCompra()
        {
            MotivoRechazo = string.Empty;
            EstadoMaterial = "CONFORME";
            NumeroLote = string.Empty;
            CertificadoColada = string.Empty;
        }
    }
}
