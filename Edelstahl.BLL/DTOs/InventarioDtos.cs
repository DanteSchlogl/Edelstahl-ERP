using System;

namespace Edelstahl.BLL.DTOs
{
    public class ProductoListaDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public string Tipo { get; set; }
        public string Unidad { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal CostoActual { get; set; }
        public decimal Fisica { get; set; }
        public decimal Disponible { get; set; }
        public decimal StockMinimo { get; set; }
        public string Estado { get; set; }
        public bool Activo { get; set; }
    }

    public class StockDto
    {
        public Guid ProductoId { get; set; }
        public Guid UbicacionId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Deposito { get; set; }
        public string Ubicacion { get; set; }
        public decimal Fisica { get; set; }
        public decimal Reservada { get; set; }
        public decimal Bloqueada { get; set; }
        public decimal Disponible { get; set; }
        public decimal PuntoReposicion { get; set; }
        public string Estado { get; set; }
    }

    public class MovimientoDto
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Ubicacion { get; set; }
        public string Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal SaldoFisico { get; set; }
        public decimal SaldoReservado { get; set; }
        public string Documento { get; set; }
        public string Lote { get; set; }
        public string Usuario { get; set; }
        public string Observaciones { get; set; }
    }

    public class AlertaReposicionDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }
        public decimal Disponible { get; set; }
        public decimal PuntoReposicion { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal Sugerido { get; set; }
        public bool Comprable { get; set; }
    }

    public class ReservaDto
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public string Ubicacion { get; set; }
        public decimal Reservada { get; set; }
        public decimal Consumida { get; set; }
        public decimal Pendiente { get; set; }
        public string Estado { get; set; }
        public string Origen { get; set; }
    }

    public class MovimientoSolicitud
    {
        public Guid ProductoId { get; set; }
        public Guid UbicacionId { get; set; }
        public Edelstahl.Domain.Catalogo.TipoMovimientoStock Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string Documento { get; set; }
        public string Lote { get; set; }
        public string Colada { get; set; }
        public string Observaciones { get; set; }
    }

    public class UbicacionDto
    {
        public Guid Id { get; set; }
        public Guid DepositoId { get; set; }
        public string Deposito { get; set; }
        public string Codigo { get; set; }
        public string Sector { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }

        public override string ToString()
        {
            return Deposito + " / " + Codigo;
        }
    }
}
