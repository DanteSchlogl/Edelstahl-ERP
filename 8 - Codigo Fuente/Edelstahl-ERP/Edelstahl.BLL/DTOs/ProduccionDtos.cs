using System;
using Edelstahl.Domain.Produccion;

namespace Edelstahl.BLL.DTOs
{
    public class PedidoPendienteFabricacionDto
    {
        public Guid PedidoId { get; set; }
        public Guid DetallePedidoId { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Especificaciones { get; set; }
        public decimal Cantidad { get; set; }
        public string PlazoEntrega { get; set; }
    }

    public class MaterialRequerido
    {
        public Guid ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class FaltanteMaterialDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Material { get; set; }
        public decimal Requerido { get; set; }
        public decimal Reservado { get; set; }
        public decimal Faltante { get; set; }
    }

    public class OrdenTableroDto
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public string Pedido { get; set; }
        public string Cliente { get; set; }
        public string Producto { get; set; }
        public decimal Cantidad { get; set; }
        public string Estado { get; set; }
        public EstadoProduccion EstadoClave { get; set; }
        public string Prioridad { get; set; }
        public PrioridadOrden PrioridadClave { get; set; }
        public string Material { get; set; }
        public DateTime Creacion { get; set; }
        public DateTime? FinEstimada { get; set; }
        public string Plano { get; set; }
        public decimal Avance { get; set; }
        public int Reprocesos { get; set; }
        public bool Bloqueada { get; set; }
    }

    public class MaterialOrdenDto
    {
        public Guid Id { get; set; }
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Material { get; set; }
        public decimal Requerida { get; set; }
        public decimal Reservada { get; set; }
        public decimal Consumida { get; set; }
        public decimal Merma { get; set; }
        public decimal PorConsumir { get; set; }
    }

    public class AvanceDto
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Etapa { get; set; }
        public decimal Porcentaje { get; set; }
        public string Observaciones { get; set; }
    }

    public class ControlCalidadDto
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Resultado { get; set; }
        public int Reproceso { get; set; }
        public string Observaciones { get; set; }
    }

    public class ResultadoGeneracionOrden
    {
        public OrdenFabricacion Orden { get; set; }
        public System.Collections.Generic.List<FaltanteMaterialDto> Faltantes { get; set; }
    }

    /// <summary>Orden de fabricacion que espera que Produccion la acepte, con los datos de la venta y del pago.</summary>
    public class OrdenPorAceptarDto
    {
        public Guid OrdenId { get; set; }
        public string Orden { get; set; }
        public Guid PedidoId { get; set; }
        public string Pedido { get; set; }
        public string Presupuesto { get; set; }
        public string Cliente { get; set; }
        public string Documento { get; set; }
        public string Telefono { get; set; }
        public string Producto { get; set; }
        public decimal Cantidad { get; set; }
        public string Especificaciones { get; set; }
        public decimal Total { get; set; }
        public decimal Abonado { get; set; }
        public decimal Saldo { get; set; }
        public string Pago { get; set; }
        public string Situacion { get; set; }
    }
}
