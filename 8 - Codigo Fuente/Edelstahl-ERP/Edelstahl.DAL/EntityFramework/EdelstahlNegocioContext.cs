using System.Data.Entity;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Contexto de Entity Framework 6 para la base EdelstahlNegocio.
    /// </summary>
    public class EdelstahlNegocioContext : DbContext
    {
        static EdelstahlNegocioContext()
        {
            // El esquema lo administra EsquemaNegocio; EF no debe crear ni migrar nada.
            Database.SetInitializer<EdelstahlNegocioContext>(null);
        }

        public EdelstahlNegocioContext()
            : base(Edelstahl.DAL.Tools.SqlServerConnection.CadenaNegocio)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = true;
        }

        // Ventas
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<DetallePresupuesto> DetallesPresupuesto { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<IntegridadTabla> IntegridadTablas { get; set; }

        // Catálogo e inventario
        public DbSet<CategoriaProducto> CategoriasProducto { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Deposito> Depositos { get; set; }
        public DbSet<UbicacionDeposito> UbicacionesDeposito { get; set; }
        public DbSet<StockProducto> StockProductos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<ReservaStock> ReservasStock { get; set; }

        // Producción
        public DbSet<OrdenFabricacion> OrdenesFabricacion { get; set; }
        public DbSet<ConsumoMaterial> ConsumosMaterial { get; set; }
        public DbSet<AvanceProduccion> AvancesProduccion { get; set; }
        public DbSet<ControlCalidad> ControlesCalidad { get; set; }

        // Compras
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; }
        public DbSet<RecepcionCompra> RecepcionesCompra { get; set; }
        public DbSet<DetalleRecepcionCompra> DetallesRecepcionCompra { get; set; }

        // Finanzas
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetallesFactura { get; set; }
        public DbSet<MedioPago> MediosPago { get; set; }
        public DbSet<CobroCliente> CobrosCliente { get; set; }
        public DbSet<PagoProveedor> PagosProveedor { get; set; }
        public DbSet<MovimientoCuentaCliente> MovimientosCuentaCliente { get; set; }
        public DbSet<MovimientoCuentaProveedor> MovimientosCuentaProveedor { get; set; }
        public DbSet<NotaCredito> NotasCredito { get; set; }

        // Logística y devoluciones
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Chofer> Choferes { get; set; }
        public DbSet<RemitoEntrega> Remitos { get; set; }
        public DbSet<DetalleRemito> DetallesRemito { get; set; }
        public DbSet<HojaRuta> HojasRuta { get; set; }
        public DbSet<DetalleHojaRuta> DetallesHojaRuta { get; set; }
        public DbSet<ComprobanteEntrega> ComprobantesEntrega { get; set; }
        public DbSet<Devolucion> Devoluciones { get; set; }
        public DbSet<DetalleDevolucion> DetallesDevolucion { get; set; }
        public DbSet<InspeccionDevolucion> InspeccionesDevolucion { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new ClienteConfiguration());
            modelBuilder.Configurations.Add(new PresupuestoConfiguration());
            modelBuilder.Configurations.Add(new DetallePresupuestoConfiguration());
            modelBuilder.Configurations.Add(new PedidoConfiguration());
            modelBuilder.Configurations.Add(new DetallePedidoConfiguration());
            modelBuilder.Configurations.Add(new IntegridadTablaConfiguration());

            ModeloNegocioConfiguration.Configurar(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}
