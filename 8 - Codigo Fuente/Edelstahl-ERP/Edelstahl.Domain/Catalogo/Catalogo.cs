using System;
using System.Collections.Generic;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Catalogo
{
    /// <summary>
    /// Clasificación funcional del producto: determina si puede
    /// comprarse, fabricarse o venderse.
    /// </summary>
    public enum TipoProducto
    {
        MateriaPrima = 1,
        InsumoConsumible = 2,
        Componente = 3,
        Semielaborado = 4,
        ProductoTerminado = 5
    }

    public static class UnidadesMedida
    {
        public const string Unidad = "UNIDAD";
        public const string Kilogramo = "KILOGRAMO";
        public const string Metro = "METRO";
        public const string MetroCuadrado = "METRO_CUADRADO";
        public const string Litro = "LITRO";

        public static readonly string[] Todas =
        {
            Unidad, Kilogramo, Metro, MetroCuadrado, Litro
        };
    }

    /// <summary>
    /// Categoría del catálogo. Admite jerarquía mediante el
    /// identificador de la categoría padre.
    /// </summary>
    public class CategoriaProducto : Entity
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public Guid? CategoriaPadreId { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public CategoriaProducto()
        {
            Codigo = string.Empty;
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Activo = true;
            FechaAlta = DateTime.Now;
        }
    }

    public class Producto : Entity
    {
        public string CodigoSku { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public Guid? CategoriaId { get; set; }
        public TipoProducto Tipo { get; set; }
        public string UnidadMedida { get; set; }
        public string CalidadAisi { get; set; }
        public decimal? EspesorMm { get; set; }
        public string Acabado { get; set; }
        public decimal? AnchoMm { get; set; }
        public decimal? LargoMm { get; set; }
        public decimal? DiametroMm { get; set; }
        public decimal? PesoUnitarioKg { get; set; }
        public string Medida { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal CostoActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal PuntoReposicion { get; set; }
        public bool EsVendible { get; set; }
        public bool EsComprable { get; set; }
        public bool EsFabricable { get; set; }
        public bool Activo { get; set; }
        public string RutaImagen { get; set; }
        public string RutaPlanoTecnico { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public Producto()
        {
            CodigoSku = string.Empty;
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Tipo = TipoProducto.ProductoTerminado;
            UnidadMedida = UnidadesMedida.Unidad;
            CalidadAisi = string.Empty;
            Acabado = string.Empty;
            Medida = string.Empty;
            RutaImagen = string.Empty;
            RutaPlanoTecnico = string.Empty;
            EsVendible = true;
            Activo = true;
            FechaAlta = DateTime.Now;
        }

        /// <summary>
        /// Aplica los valores por omisión que corresponden al tipo.
        /// </summary>
        public void AjustarSegunTipo()
        {
            EsVendible = Tipo == TipoProducto.ProductoTerminado
                || Tipo == TipoProducto.Componente;
            EsComprable = Tipo == TipoProducto.MateriaPrima
                || Tipo == TipoProducto.InsumoConsumible
                || Tipo == TipoProducto.Componente;
            EsFabricable = Tipo == TipoProducto.ProductoTerminado
                || Tipo == TipoProducto.Semielaborado;
        }
    }

    public class Deposito : Entity
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Localidad { get; set; }
        public string Descripcion { get; set; }
        public string Responsable { get; set; }
        public bool Activo { get; set; }
        public List<UbicacionDeposito> Ubicaciones { get; set; }

        public Deposito()
        {
            Codigo = string.Empty;
            Nombre = string.Empty;
            Direccion = string.Empty;
            Localidad = string.Empty;
            Descripcion = string.Empty;
            Responsable = string.Empty;
            Activo = true;
            Ubicaciones = new List<UbicacionDeposito>();
        }
    }

    public class UbicacionDeposito : Entity
    {
        public Guid DepositoId { get; set; }
        public string Codigo { get; set; }
        public string Sector { get; set; }
        public string Pasillo { get; set; }
        public string Estanteria { get; set; }
        public string Nivel { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }

        public UbicacionDeposito()
        {
            Codigo = string.Empty;
            Sector = string.Empty;
            Pasillo = string.Empty;
            Estanteria = string.Empty;
            Nivel = string.Empty;
            Descripcion = string.Empty;
            Activo = true;
        }
    }

    public class StockProducto : Entity
    {
        public Guid ProductoId { get; set; }
        public Guid UbicacionId { get; set; }
        public decimal CantidadFisica { get; set; }
        public decimal CantidadReservada { get; set; }
        public decimal CantidadBloqueada { get; set; }
        public decimal CostoPromedio { get; set; }
        public DateTime FechaActualizacion { get; set; }

        public StockProducto()
        {
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Existencia libre: física menos reservada y bloqueada.
        /// </summary>
        public decimal CantidadDisponible
        {
            get { return CantidadFisica - CantidadReservada - CantidadBloqueada; }
        }
    }

    public enum TipoMovimientoStock
    {
        RecepcionCompra = 1,
        ProductoTerminado = 2,
        DevolucionCliente = 3,
        AjustePositivo = 4,
        TransferenciaEntrada = 5,
        ConsumoProduccion = 10,
        MermaProduccion = 11,
        Despacho = 12,
        AjusteNegativo = 13,
        TransferenciaSalida = 14,
        ReservaPedido = 20,
        LiberacionReserva = 21
    }

    public class MovimientoStock : Entity
    {
        public Guid ProductoId { get; set; }
        public Guid UbicacionId { get; set; }
        public TipoMovimientoStock Tipo { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public Guid? ReferenciaId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string DocumentoOrigen { get; set; }
        public string Lote { get; set; }
        public string CertificadoColada { get; set; }
        public decimal SaldoFisicoResultante { get; set; }
        public decimal SaldoReservadoResultante { get; set; }
        public string Observaciones { get; set; }

        public MovimientoStock()
        {
            Usuario = string.Empty;
            FechaMovimiento = DateTime.Now;
            DocumentoOrigen = string.Empty;
            Lote = string.Empty;
            CertificadoColada = string.Empty;
            Observaciones = string.Empty;
        }

        /// <summary>
        /// Indica si el movimiento incrementa la existencia física.
        /// </summary>
        public bool Incrementa
        {
            get
            {
                return Tipo == TipoMovimientoStock.RecepcionCompra
                    || Tipo == TipoMovimientoStock.ProductoTerminado
                    || Tipo == TipoMovimientoStock.DevolucionCliente
                    || Tipo == TipoMovimientoStock.AjustePositivo
                    || Tipo == TipoMovimientoStock.TransferenciaEntrada;
            }
        }
    }

    public enum EstadoReserva
    {
        Activa = 1,
        Consumida = 2,
        Liberada = 3,
        Cancelada = 4
    }

    public class ReservaStock : Entity
    {
        public Guid ProductoId { get; set; }
        public Guid UbicacionId { get; set; }
        public Guid? DetallePedidoId { get; set; }
        public Guid? OrdenFabricacionId { get; set; }
        public DateTime FechaReserva { get; set; }
        public DateTime? FechaLiberacion { get; set; }
        public decimal CantidadReservada { get; set; }
        public decimal CantidadConsumida { get; set; }
        public EstadoReserva Estado { get; set; }
        public string MotivoLiberacion { get; set; }
        public string Observaciones { get; set; }

        public ReservaStock()
        {
            FechaReserva = DateTime.Now;
            Estado = EstadoReserva.Activa;
            MotivoLiberacion = string.Empty;
            Observaciones = string.Empty;
        }

        public decimal CantidadPendiente
        {
            get { return CantidadReservada - CantidadConsumida; }
        }
    }
}
