using System;
using Edelstahl.Domain.Logistica;

namespace Edelstahl.BLL.DTOs
{
    public class PedidoDespachoDto
    {
        public Guid PedidoId { get; set; }
        public string Pedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string Cliente { get; set; }
        public string Direccion { get; set; }
        public string Estado { get; set; }
        public decimal Pendiente { get; set; }
        public bool Facturado { get; set; }
        public string Factura { get; set; }
        public decimal Saldo { get; set; }
        public bool Habilitado { get; set; }
        public string Motivo { get; set; }
    }

    /// <summary>Pedido facturado que el deposito debe preparar antes de despacharlo.</summary>
    public class OrdenPreparacionDto
    {
        public Guid PedidoId { get; set; }
        public string Pedido { get; set; }
        public string Factura { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Productos { get; set; }
        public decimal Pendiente { get; set; }
        public string Estado { get; set; }
    }

    public class LineaPendienteRemitoDto
    {
        public Guid DetallePedidoId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Pedida { get; set; }
        public decimal Remitida { get; set; }
        public decimal Pendiente { get; set; }
        public decimal ListaParaDespacho { get; set; }
    }

    public class LineaRemitoSolicitud
    {
        public Guid DetallePedidoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class RemitoDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public Guid PedidoId { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public EstadoRemito EstadoClave { get; set; }
        public string Direccion { get; set; }
        public int Items { get; set; }
        public decimal PesoKg { get; set; }
        public bool PesoIncompleto { get; set; }
    }

    /// <summary>Una fila de la bitacora de envios: un remito con su recorrido y resultado.</summary>
    public class EnvioHistorialDto
    {
        public Guid Id { get; set; }
        public string Remito { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public string Estado { get; set; }
        public EstadoRemito EstadoClave { get; set; }
        public DateTime Emision { get; set; }
        public string HojaRuta { get; set; }
        public string Resultado { get; set; }
        public string Motivo { get; set; }
        public string Receptor { get; set; }
        public DateTime? Entrega { get; set; }
    }

    public class HojaRutaDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Vehiculo { get; set; }
        public string Chofer { get; set; }
        public string Estado { get; set; }
        public EstadoHojaRuta EstadoClave { get; set; }
        public int Paradas { get; set; }
        public int Resueltas { get; set; }
    }

    public class ParadaDto
    {
        public Guid DetalleId { get; set; }
        public Guid RemitoId { get; set; }
        public int Orden { get; set; }
        public string Remito { get; set; }
        public string Cliente { get; set; }
        public string Direccion { get; set; }
        public string Estado { get; set; }
        public string Resultado { get; set; }
        public bool Pendiente { get; set; }
    }

    public class DevolucionDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Remito { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public EstadoDevolucion EstadoClave { get; set; }
        public string Tipo { get; set; }
        public int Items { get; set; }
    }

    public class DetalleDevolucionDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Devuelta { get; set; }
        public decimal Reingresada { get; set; }
        public decimal Descartada { get; set; }
        public string Accion { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
    }

    public class LineaDevolucionSolicitud
    {
        public Guid DetalleRemitoId { get; set; }
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; }
    }

    public class ResolucionDevolucion
    {
        public Guid DetalleDevolucionId { get; set; }
        public AccionResuelta Accion { get; set; }
        public bool AptoReingreso { get; set; }
        public decimal CantidadReingresada { get; set; }
        public decimal CantidadDescartada { get; set; }
        public string Condicion { get; set; }
        public string Observaciones { get; set; }
    }

    public class NotaCreditoSugerida
    {
        public Guid FacturaId { get; set; }
        public string Factura { get; set; }
        public decimal MontoNeto { get; set; }
    }

    public class ResultadoPreparacion
    {
        public ResultadoPreparacion()
        {
            Reservados = new System.Collections.Generic.List<string>();
            Ingresados = new System.Collections.Generic.List<string>();
            Faltantes = new System.Collections.Generic.List<string>();
            EnProduccion = new System.Collections.Generic.List<string>();
        }

        public System.Collections.Generic.List<string> Reservados { get; private set; }
        public System.Collections.Generic.List<string> Ingresados { get; private set; }
        public System.Collections.Generic.List<string> Faltantes { get; private set; }
        public System.Collections.Generic.List<string> EnProduccion { get; private set; }
    }
}
