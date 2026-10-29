using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq.Expressions;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Configuración de las tablas de los módulos de catálogo, inventario,
    /// producción, compras, finanzas, logística y devoluciones.
    ///
    /// Las reglas generales (longitudes y precisión decimal) se definen por
    /// convención sobre las clases de estos módulos; lo particular de cada
    /// tabla (nombre, índices y relaciones padre-hijo) se declara abajo.
    /// </summary>
    public static class ModeloNegocioConfiguration
    {
        private static readonly string[] PrefijosEspacios =
        {
            "Edelstahl.Domain.Catalogo",
            "Edelstahl.Domain.Produccion",
            "Edelstahl.Domain.Compras",
            "Edelstahl.Domain.Finanzas",
            "Edelstahl.Domain.Logistica"
        };

        private static bool EsDelModelo(Type tipo)
        {
            if (tipo == null || tipo.Namespace == null)
            {
                return false;
            }

            foreach (string prefijo in PrefijosEspacios)
            {
                if (tipo.Namespace == prefijo)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Configurar(DbModelBuilder mb)
        {
            ConfigurarConvenciones(mb);

            ConfigurarCatalogo(mb);
            ConfigurarProduccion(mb);
            ConfigurarCompras(mb);
            ConfigurarFinanzas(mb);
            ConfigurarLogistica(mb);
        }

        private static void ConfigurarConvenciones(DbModelBuilder mb)
        {
            mb.Properties<Guid>()
                .Where(p => p.Name == "Id" && EsDelModelo(p.DeclaringType))
                .Configure(p => p.HasDatabaseGeneratedOption(DatabaseGeneratedOption.None));

            // Las reglas se aplican en orden: la última que coincide prevalece.
            ReglaTexto(mb, nombre => true, 250);
            ReglaTexto(mb, nombre => nombre == "Especificaciones", 1000);
            ReglaTexto(mb, nombre => nombre.StartsWith("Observaciones")
                || nombre.StartsWith("Descripcion") || nombre.StartsWith("Motivo")
                || nombre.StartsWith("Direccion") || nombre == "Medida"
                || nombre == "CondicionMaterial" || nombre == "CondicionPago", 500);
            ReglaTexto(mb, nombre => nombre.StartsWith("Ruta") || nombre.EndsWith("Uri"), 255);
            ReglaTexto(mb, nombre => nombre.StartsWith("Numero") || nombre.StartsWith("Codigo")
                || nombre == "Cuit" || nombre == "Patente" || nombre == "Documento"
                || nombre == "Telefono" || nombre == "UnidadMedida" || nombre == "Estado"
                || nombre == "Origen" || nombre == "Lote", 50);

            ReglaDecimal(mb, nombre => true, 18, 2);
            ReglaDecimal(mb, EsCantidad, 18, 3);
            ReglaDecimal(mb, nombre => nombre.StartsWith("Porcentaje"), 5, 2);
            ReglaDecimal(mb, nombre => nombre == "TipoCambio", 18, 4);

            mb.Properties<DateTime>()
                .Where(p => EsDelModelo(p.DeclaringType))
                .Configure(p => p.HasColumnType("datetime2"));

            mb.Properties<DateTime?>()
                .Where(p => EsDelModelo(p.DeclaringType))
                .Configure(p => p.HasColumnType("datetime2"));
        }

        private static void ReglaTexto(DbModelBuilder mb, Func<string, bool> aplica, int longitud)
        {
            mb.Properties<string>()
                .Where(p => EsDelModelo(p.DeclaringType) && aplica(p.Name))
                .Configure(p => p.HasMaxLength(longitud));
        }

        private static void ReglaDecimal(
            DbModelBuilder mb, Func<string, bool> aplica, byte precision, byte escala)
        {
            mb.Properties<decimal>()
                .Where(p => EsDelModelo(p.DeclaringType) && aplica(p.Name))
                .Configure(p => p.HasPrecision(precision, escala));

            mb.Properties<decimal?>()
                .Where(p => EsDelModelo(p.DeclaringType) && aplica(p.Name))
                .Configure(p => p.HasPrecision(precision, escala));
        }

        private static bool EsCantidad(string nombre)
        {
            string[] claves =
            {
                "Cantidad", "Stock", "Reposicion", "Peso", "Espesor", "Ancho",
                "Largo", "Diametro", "Merma", "SaldoFisico", "SaldoReservado",
                "Capacidad", "Kilometros", "Pendiente"
            };

            foreach (string clave in claves)
            {
                if (nombre.Contains(clave))
                {
                    return true;
                }
            }

            return false;
        }

        // Los índices únicos y las referencias entre módulos se crean en
        // EsquemaNegocio, que es la única fuente de esa definición.
        private static void Unico<T>(
            EntityTypeConfiguration<T> c,
            Expression<Func<T, string>> propiedad,
            string nombre) where T : class
        {
            c.Property(propiedad).IsRequired();
        }

        // ---------------------------------------------------------------
        // Catálogo e inventario
        // ---------------------------------------------------------------

        private static void ConfigurarCatalogo(DbModelBuilder mb)
        {
            EntityTypeConfiguration<CategoriaProducto> categoria = mb.Entity<CategoriaProducto>();
            categoria.ToTable("CategoriasProducto");
            Unico(categoria, x => x.Codigo, "UQ_CategoriasProducto_Codigo");
            categoria.Property(x => x.Nombre).IsRequired();

            EntityTypeConfiguration<Producto> producto = mb.Entity<Producto>();
            producto.ToTable("Productos");
            Unico(producto, x => x.CodigoSku, "UQ_Productos_Sku");
            producto.Property(x => x.Nombre).IsRequired();
            producto.Property(x => x.CodigoSku).HasMaxLength(50);

            EntityTypeConfiguration<Deposito> deposito = mb.Entity<Deposito>();
            deposito.ToTable("Depositos");
            Unico(deposito, x => x.Codigo, "UQ_Depositos_Codigo");
            deposito.Property(x => x.Nombre).IsRequired();
            deposito.HasMany(x => x.Ubicaciones)
                .WithRequired()
                .HasForeignKey(x => x.DepositoId)
                .WillCascadeOnDelete(true);

            EntityTypeConfiguration<UbicacionDeposito> ubicacion = mb.Entity<UbicacionDeposito>();
            ubicacion.ToTable("UbicacionesDeposito");
            ubicacion.Property(x => x.Codigo).IsRequired();

            EntityTypeConfiguration<StockProducto> stock = mb.Entity<StockProducto>();
            stock.ToTable("StockProductos");

            EntityTypeConfiguration<MovimientoStock> movimiento = mb.Entity<MovimientoStock>();
            movimiento.ToTable("MovimientosStock");

            EntityTypeConfiguration<ReservaStock> reserva = mb.Entity<ReservaStock>();
            reserva.ToTable("ReservasStock");
        }

        // ---------------------------------------------------------------
        // Producción
        // ---------------------------------------------------------------

        private static void ConfigurarProduccion(DbModelBuilder mb)
        {
            EntityTypeConfiguration<OrdenFabricacion> orden = mb.Entity<OrdenFabricacion>();
            orden.ToTable("OrdenesFabricacion");
            Unico(orden, x => x.Numero, "UQ_OrdenesFabricacion_Numero");
            orden.HasMany(x => x.Materiales).WithRequired().HasForeignKey(x => x.OrdenFabricacionId).WillCascadeOnDelete(true);
            orden.HasMany(x => x.Avances).WithRequired().HasForeignKey(x => x.OrdenFabricacionId).WillCascadeOnDelete(true);
            orden.HasMany(x => x.Controles).WithRequired().HasForeignKey(x => x.OrdenFabricacionId).WillCascadeOnDelete(true);

            mb.Entity<ConsumoMaterial>().ToTable("ConsumosMaterial");
            mb.Entity<AvanceProduccion>().ToTable("AvancesProduccion");
            mb.Entity<ControlCalidad>().ToTable("ControlesCalidad");
        }

        // ---------------------------------------------------------------
        // Compras
        // ---------------------------------------------------------------

        private static void ConfigurarCompras(DbModelBuilder mb)
        {
            EntityTypeConfiguration<Proveedor> proveedor = mb.Entity<Proveedor>();
            proveedor.ToTable("Proveedores");
            Unico(proveedor, x => x.Cuit, "UQ_Proveedores_Cuit");
            proveedor.Property(x => x.RazonSocial).IsRequired();

            EntityTypeConfiguration<OrdenCompra> orden = mb.Entity<OrdenCompra>();
            orden.ToTable("OrdenesCompra");
            Unico(orden, x => x.Numero, "UQ_OrdenesCompra_Numero");
            orden.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.OrdenCompraId).WillCascadeOnDelete(true);

            mb.Entity<DetalleOrdenCompra>().ToTable("DetallesOrdenCompra");

            EntityTypeConfiguration<RecepcionCompra> recepcion = mb.Entity<RecepcionCompra>();
            recepcion.ToTable("RecepcionesCompra");
            Unico(recepcion, x => x.Numero, "UQ_RecepcionesCompra_Numero");
            recepcion.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.RecepcionId).WillCascadeOnDelete(true);

            mb.Entity<DetalleRecepcionCompra>().ToTable("DetallesRecepcionCompra");
        }

        // ---------------------------------------------------------------
        // Finanzas
        // ---------------------------------------------------------------

        private static void ConfigurarFinanzas(DbModelBuilder mb)
        {
            EntityTypeConfiguration<Factura> factura = mb.Entity<Factura>();
            factura.ToTable("Facturas");
            Unico(factura, x => x.Numero, "UQ_Facturas_Numero");
            factura.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.FacturaId).WillCascadeOnDelete(true);

            mb.Entity<DetalleFactura>().ToTable("DetallesFactura");

            EntityTypeConfiguration<MedioPago> medio = mb.Entity<MedioPago>();
            medio.ToTable("MediosPago");
            Unico(medio, x => x.Codigo, "UQ_MediosPago_Codigo");

            EntityTypeConfiguration<CobroCliente> cobro = mb.Entity<CobroCliente>();
            cobro.ToTable("CobrosCliente");
            Unico(cobro, x => x.Numero, "UQ_CobrosCliente_Numero");

            EntityTypeConfiguration<PagoProveedor> pago = mb.Entity<PagoProveedor>();
            pago.ToTable("PagosProveedor");
            Unico(pago, x => x.Numero, "UQ_PagosProveedor_Numero");

            EntityTypeConfiguration<MovimientoCuentaCliente> cuentaCliente = mb.Entity<MovimientoCuentaCliente>();
            cuentaCliente.ToTable("MovimientosCuentaCliente");

            EntityTypeConfiguration<MovimientoCuentaProveedor> cuentaProveedor = mb.Entity<MovimientoCuentaProveedor>();
            cuentaProveedor.ToTable("MovimientosCuentaProveedor");

            EntityTypeConfiguration<NotaCredito> nota = mb.Entity<NotaCredito>();
            nota.ToTable("NotasCredito");
            Unico(nota, x => x.Numero, "UQ_NotasCredito_Numero");
        }

        // ---------------------------------------------------------------
        // Logística y devoluciones
        // ---------------------------------------------------------------

        private static void ConfigurarLogistica(DbModelBuilder mb)
        {
            EntityTypeConfiguration<Vehiculo> vehiculo = mb.Entity<Vehiculo>();
            vehiculo.ToTable("Vehiculos");
            Unico(vehiculo, x => x.Patente, "UQ_Vehiculos_Patente");

            EntityTypeConfiguration<Chofer> chofer = mb.Entity<Chofer>();
            chofer.ToTable("Choferes");
            Unico(chofer, x => x.Documento, "UQ_Choferes_Documento");
            Unico(chofer, x => x.LicenciaConducir, "UQ_Choferes_Licencia");

            EntityTypeConfiguration<RemitoEntrega> remito = mb.Entity<RemitoEntrega>();
            remito.ToTable("Remitos");
            Unico(remito, x => x.Numero, "UQ_Remitos_Numero");
            remito.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.RemitoId).WillCascadeOnDelete(true);

            mb.Entity<DetalleRemito>().ToTable("DetallesRemito");

            EntityTypeConfiguration<HojaRuta> hoja = mb.Entity<HojaRuta>();
            hoja.ToTable("HojasRuta");
            Unico(hoja, x => x.Numero, "UQ_HojasRuta_Numero");
            hoja.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.HojaRutaId).WillCascadeOnDelete(true);

            EntityTypeConfiguration<DetalleHojaRuta> detalleHoja = mb.Entity<DetalleHojaRuta>();
            detalleHoja.ToTable("DetallesHojaRuta");

            EntityTypeConfiguration<ComprobanteEntrega> comprobante = mb.Entity<ComprobanteEntrega>();
            comprobante.ToTable("ComprobantesEntrega");

            EntityTypeConfiguration<Devolucion> devolucion = mb.Entity<Devolucion>();
            devolucion.ToTable("Devoluciones");
            Unico(devolucion, x => x.Numero, "UQ_Devoluciones_Numero");
            devolucion.HasMany(x => x.Detalles).WithRequired().HasForeignKey(x => x.DevolucionId).WillCascadeOnDelete(true);

            EntityTypeConfiguration<DetalleDevolucion> detalleDevolucion = mb.Entity<DetalleDevolucion>();
            detalleDevolucion.ToTable("DetallesDevolucion");
            detalleDevolucion.HasMany(x => x.Inspecciones).WithRequired().HasForeignKey(x => x.DetalleDevolucionId).WillCascadeOnDelete(true);

            mb.Entity<InspeccionDevolucion>().ToTable("InspeccionesDevolucion");
        }
    }
}
