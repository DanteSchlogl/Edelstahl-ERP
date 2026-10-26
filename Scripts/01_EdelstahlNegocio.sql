-- Base de negocio de Edelstahl ERP
-- Crear antes la base (CREATE DATABASE EdelstahlNegocio;) y ejecutar este script sobre ella.
-- El programa tambien la crea solo al iniciar en modo SQL Server.
USE EdelstahlNegocio;
GO
-- Edelstahl ERP - base de negocio
-- Script generado a partir del modelo de datos.

create table [dbo].[AvancesProduccion] (
    [Id] [uniqueidentifier] not null,
    [OrdenFabricacionId] [uniqueidentifier] not null,
    [Fecha] [datetime2](7) not null,
    [Usuario] [nvarchar](250) null,
    [Etapa] [nvarchar](250) null,
    [PorcentajeAvance] [decimal](5, 2) not null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[CategoriasProducto] (
    [Id] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) not null,
    [Nombre] [nvarchar](250) not null,
    [Descripcion] [nvarchar](500) null,
    [CategoriaPadreId] [uniqueidentifier] null,
    [Activo] [bit] not null,
    [FechaAlta] [datetime2](7) not null,
    primary key ([Id])
);
GO
create table [dbo].[Choferes] (
    [Id] [uniqueidentifier] not null,
    [Documento] [nvarchar](50) not null,
    [Apellido] [nvarchar](250) null,
    [Nombre] [nvarchar](250) null,
    [LicenciaConducir] [nvarchar](250) not null,
    [CategoriaLicencia] [nvarchar](250) null,
    [VencimientoLicencia] [datetime2](7) not null,
    [Telefono] [nvarchar](50) null,
    [Activo] [bit] not null,
    primary key ([Id])
);
GO
create table [dbo].[Clientes] (
    [Id] [uniqueidentifier] not null,
    [CUIT] [nvarchar](300) not null,
    [Tipo] [int] not null,
    [AutorizadoACredito] [bit] not null,
    [RazonSocial] [nvarchar](250) not null,
    [DireccionFacturacion] [nvarchar](500) not null,
    [DireccionEntrega] [nvarchar](500) not null,
    [Localidad] [nvarchar](150) not null,
    [Provincia] [nvarchar](150) not null,
    [CodigoPostal] [nvarchar](20) not null,
    [Email] [nvarchar](250) not null,
    [Telefono] [nvarchar](100) not null,
    [LimiteCredito] [decimal](18, 2) not null,
    [DeudaActual] [decimal](18, 2) not null,
    [Activo] [bit] not null,
    [FechaAlta] [datetime2](7) not null,
    [DVH] [nvarchar](64) not null,
    primary key ([Id])
);
GO
create table [dbo].[CobrosCliente] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [ClienteId] [uniqueidentifier] not null,
    [FacturaId] [uniqueidentifier] null,
    [MedioPagoId] [uniqueidentifier] not null,
    [FechaCobro] [datetime2](7) not null,
    [FechaAcreditacion] [datetime2](7) null,
    [Importe] [decimal](18, 2) not null,
    [Referencia] [nvarchar](250) null,
    [EntidadFinanciera] [nvarchar](250) null,
    [Estado] [nvarchar](50) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[ComprobantesEntrega] (
    [Id] [uniqueidentifier] not null,
    [DetalleHojaRutaId] [uniqueidentifier] not null,
    [FechaRegistro] [datetime2](7) not null,
    [Resultado] [int] not null,
    [NombreReceptor] [nvarchar](250) null,
    [DocumentoReceptor] [nvarchar](250) null,
    [MotivoRechazo] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[ConsumosMaterial] (
    [Id] [uniqueidentifier] not null,
    [OrdenFabricacionId] [uniqueidentifier] not null,
    [ProductoMaterialId] [uniqueidentifier] not null,
    [UbicacionId] [uniqueidentifier] null,
    [FechaReserva] [datetime2](7) null,
    [FechaConsumo] [datetime2](7) null,
    [CantidadRequerida] [decimal](18, 3) not null,
    [CantidadReservada] [decimal](18, 3) not null,
    [CantidadConsumida] [decimal](18, 3) not null,
    [CantidadMerma] [decimal](18, 3) not null,
    [CostoUnitario] [decimal](18, 2) not null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[ControlesCalidad] (
    [Id] [uniqueidentifier] not null,
    [OrdenFabricacionId] [uniqueidentifier] not null,
    [UsuarioControl] [nvarchar](250) null,
    [FechaControl] [datetime2](7) not null,
    [Resultado] [int] not null,
    [RequiereReproceso] [bit] not null,
    [NumeroReproceso] [int] not null,
    [FechaReproceso] [datetime2](7) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[Depositos] (
    [Id] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) not null,
    [Nombre] [nvarchar](250) not null,
    [Direccion] [nvarchar](500) null,
    [Localidad] [nvarchar](250) null,
    [Descripcion] [nvarchar](500) null,
    [Responsable] [nvarchar](250) null,
    [Activo] [bit] not null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesDevolucion] (
    [Id] [uniqueidentifier] not null,
    [DevolucionId] [uniqueidentifier] not null,
    [DetallePedidoId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) null,
    [Descripcion] [nvarchar](500) null,
    [CantidadDevuelta] [decimal](18, 3) not null,
    [Motivo] [nvarchar](500) null,
    [Accion] [int] not null,
    [CantidadReingresada] [decimal](18, 3) not null,
    [CantidadDescartada] [decimal](18, 3) not null,
    [Estado] [nvarchar](50) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesFactura] (
    [Id] [uniqueidentifier] not null,
    [FacturaId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) null,
    [Descripcion] [nvarchar](500) null,
    [Cantidad] [decimal](18, 3) not null,
    [PrecioUnitario] [decimal](18, 2) not null,
    [PorcentajeDescuento] [decimal](5, 2) not null,
    [Subtotal] [decimal](18, 2) not null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesHojaRuta] (
    [Id] [uniqueidentifier] not null,
    [HojaRutaId] [uniqueidentifier] not null,
    [RemitoId] [uniqueidentifier] not null,
    [OrdenEntrega] [int] not null,
    [HoraEstimada] [time](7) null,
    [HoraLlegada] [time](7) null,
    [EstadoEntrega] [nvarchar](250) null,
    [Resultado] [int] null,
    [MotivoNoEntrega] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesOrdenCompra] (
    [Id] [uniqueidentifier] not null,
    [OrdenCompraId] [uniqueidentifier] not null,
    [ProductoId] [uniqueidentifier] not null,
    [CantidadSolicitada] [decimal](18, 3) not null,
    [CantidadRecibida] [decimal](18, 3) not null,
    [UnidadMedida] [nvarchar](50) null,
    [CostoUnitario] [decimal](18, 2) not null,
    [PorcentajeDescuento] [decimal](5, 2) not null,
    [Estado] [nvarchar](50) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesPedido] (
    [Id] [uniqueidentifier] not null,
    [PedidoId] [uniqueidentifier] not null,
    [DetallePresupuestoId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](100) not null,
    [Descripcion] [nvarchar](500) not null,
    [DescripcionTecnica] [nvarchar](1000) not null,
    [TipoItem] [int] not null,
    [Cantidad] [decimal](18, 4) not null,
    [PrecioUnitario] [decimal](18, 4) not null,
    [PorcentajeDescuento] [decimal](18, 4) not null,
    [RequiereFabricacion] [bit] not null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesPresupuesto] (
    [Id] [uniqueidentifier] not null,
    [PresupuestoId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](100) not null,
    [Descripcion] [nvarchar](500) not null,
    [DescripcionTecnica] [nvarchar](1000) not null,
    [TipoItem] [int] not null,
    [Cantidad] [decimal](18, 4) not null,
    [PrecioUnitario] [decimal](18, 4) not null,
    [PorcentajeDescuento] [decimal](18, 4) not null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesRecepcionCompra] (
    [Id] [uniqueidentifier] not null,
    [RecepcionId] [uniqueidentifier] not null,
    [DetalleOrdenCompraId] [uniqueidentifier] not null,
    [ProductoId] [uniqueidentifier] not null,
    [FechaVencimiento] [datetime2](7) null,
    [CantidadRecibida] [decimal](18, 3) not null,
    [CantidadAceptada] [decimal](18, 3) not null,
    [CantidadRechazada] [decimal](18, 3) not null,
    [MotivoRechazo] [nvarchar](500) null,
    [EstadoMaterial] [nvarchar](250) null,
    [NumeroLote] [nvarchar](50) null,
    [CertificadoColada] [nvarchar](250) null,
    primary key ([Id])
);
GO
create table [dbo].[DetallesRemito] (
    [Id] [uniqueidentifier] not null,
    [RemitoId] [uniqueidentifier] not null,
    [DetallePedidoId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) null,
    [Descripcion] [nvarchar](500) null,
    [Cantidad] [decimal](18, 3) not null,
    primary key ([Id])
);
GO
create table [dbo].[Devoluciones] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [RemitoId] [uniqueidentifier] not null,
    [PedidoId] [uniqueidentifier] not null,
    [ClienteId] [uniqueidentifier] not null,
    [FechaSolicitud] [datetime2](7) not null,
    [FechaRecepcion] [datetime2](7) null,
    [Estado] [int] not null,
    [EsTotal] [bit] not null,
    [MotivoGeneral] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[Facturas] (
    [Id] [uniqueidentifier] not null,
    [PedidoId] [uniqueidentifier] not null,
    [ClienteId] [uniqueidentifier] not null,
    [PuntoVenta] [int] not null,
    [Numero] [nvarchar](50) not null,
    [FechaEmision] [datetime2](7) not null,
    [FechaVencimiento] [datetime2](7) null,
    [Tipo] [int] not null,
    [Moneda] [int] not null,
    [TipoCambio] [decimal](18, 4) not null,
    [MontoNeto] [decimal](18, 2) not null,
    [Iva] [decimal](18, 2) not null,
    [OtrosImpuestos] [decimal](18, 2) not null,
    [MontoTotal] [decimal](18, 2) not null,
    [SaldoPendiente] [decimal](18, 3) not null,
    [EstadoPago] [int] not null,
    [CondicionPago] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[HojasRuta] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [VehiculoId] [uniqueidentifier] not null,
    [ChoferId] [uniqueidentifier] not null,
    [FechaPlanificada] [datetime2](7) not null,
    [FechaInicio] [datetime2](7) null,
    [FechaFinalizacion] [datetime2](7) null,
    [Estado] [int] not null,
    [KilometrosIniciales] [decimal](18, 3) null,
    [KilometrosFinales] [decimal](18, 3) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[InspeccionesDevolucion] (
    [Id] [uniqueidentifier] not null,
    [DetalleDevolucionId] [uniqueidentifier] not null,
    [UsuarioInspector] [nvarchar](250) null,
    [FechaInspeccion] [datetime2](7) not null,
    [RequiereReproceso] [bit] not null,
    [RequiereReemplazo] [bit] not null,
    [AptoReingresoStock] [bit] not null,
    [Resultado] [nvarchar](250) null,
    [CondicionMaterial] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[IntegridadTablas] (
    [NombreTabla] [nvarchar](128) not null,
    [DVV] [nvarchar](64) not null,
    [FechaActualizacion] [datetime2](7) not null,
    primary key ([NombreTabla])
);
GO
create table [dbo].[MediosPago] (
    [Id] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) not null,
    [Nombre] [nvarchar](250) null,
    [Descripcion] [nvarchar](500) null,
    [RequiereReferencia] [bit] not null,
    [RequiereEntidadFinanciera] [bit] not null,
    [Activo] [bit] not null,
    primary key ([Id])
);
GO
create table [dbo].[MovimientosCuentaCliente] (
    [Id] [uniqueidentifier] not null,
    [ClienteId] [uniqueidentifier] not null,
    [FacturaId] [uniqueidentifier] null,
    [CobroId] [uniqueidentifier] null,
    [NotaCreditoId] [uniqueidentifier] null,
    [Fecha] [datetime2](7) not null,
    [Tipo] [int] not null,
    [Debe] [decimal](18, 2) not null,
    [Haber] [decimal](18, 2) not null,
    [SaldoResultante] [decimal](18, 2) not null,
    [Referencia] [nvarchar](250) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[MovimientosCuentaProveedor] (
    [Id] [uniqueidentifier] not null,
    [ProveedorId] [uniqueidentifier] not null,
    [OrdenCompraId] [uniqueidentifier] null,
    [PagoId] [uniqueidentifier] null,
    [Fecha] [datetime2](7) not null,
    [Tipo] [int] not null,
    [Debe] [decimal](18, 2) not null,
    [Haber] [decimal](18, 2) not null,
    [SaldoResultante] [decimal](18, 2) not null,
    [Referencia] [nvarchar](250) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[MovimientosStock] (
    [Id] [uniqueidentifier] not null,
    [ProductoId] [uniqueidentifier] not null,
    [UbicacionId] [uniqueidentifier] not null,
    [Tipo] [int] not null,
    [Usuario] [nvarchar](250) null,
    [FechaMovimiento] [datetime2](7) not null,
    [ReferenciaId] [uniqueidentifier] null,
    [Cantidad] [decimal](18, 3) not null,
    [CostoUnitario] [decimal](18, 2) null,
    [DocumentoOrigen] [nvarchar](250) null,
    [Lote] [nvarchar](50) null,
    [CertificadoColada] [nvarchar](250) null,
    [SaldoFisicoResultante] [decimal](18, 3) not null,
    [SaldoReservadoResultante] [decimal](18, 3) not null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[NotasCredito] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [FacturaId] [uniqueidentifier] not null,
    [ClienteId] [uniqueidentifier] not null,
    [DevolucionId] [uniqueidentifier] null,
    [Tipo] [int] not null,
    [Fecha] [datetime2](7) not null,
    [Motivo] [nvarchar](500) null,
    [MontoNeto] [decimal](18, 2) not null,
    [Iva] [decimal](18, 2) not null,
    [MontoTotal] [decimal](18, 2) not null,
    [Estado] [nvarchar](50) null,
    primary key ([Id])
);
GO
create table [dbo].[OrdenesCompra] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [ProveedorId] [uniqueidentifier] not null,
    [FechaEmision] [datetime2](7) not null,
    [FechaEntregaEstimada] [datetime2](7) null,
    [Estado] [int] not null,
    [CondicionPago] [nvarchar](500) null,
    [MontoNeto] [decimal](18, 2) not null,
    [MontoDescuento] [decimal](18, 2) not null,
    [Impuestos] [decimal](18, 2) not null,
    [MontoTotal] [decimal](18, 2) not null,
    [SaldoPendiente] [decimal](18, 3) not null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[OrdenesFabricacion] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [PedidoId] [uniqueidentifier] null,
    [DetallePedidoId] [uniqueidentifier] null,
    [ProductoId] [uniqueidentifier] not null,
    [Origen] [nvarchar](50) null,
    [FechaCreacion] [datetime2](7) not null,
    [FechaInicio] [datetime2](7) null,
    [FechaFinEstimada] [datetime2](7) null,
    [FechaFinReal] [datetime2](7) null,
    [Estado] [int] not null,
    [Prioridad] [int] not null,
    [CantidadFabricar] [decimal](18, 3) not null,
    [PlanoTecnicoUri] [nvarchar](255) null,
    [Especificaciones] [nvarchar](1000) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[PagosProveedor] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [ProveedorId] [uniqueidentifier] not null,
    [OrdenCompraId] [uniqueidentifier] null,
    [MedioPagoId] [uniqueidentifier] not null,
    [FechaPago] [datetime2](7) not null,
    [FechaDebito] [datetime2](7) null,
    [Importe] [decimal](18, 2) not null,
    [Referencia] [nvarchar](250) null,
    [EntidadFinanciera] [nvarchar](250) null,
    [Estado] [nvarchar](50) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[Pedidos] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [ClienteId] [uniqueidentifier] not null,
    [PresupuestoId] [uniqueidentifier] not null,
    [FechaPedido] [datetime2](7) not null,
    [Estado] [int] not null,
    [Moneda] [int] not null,
    [TipoCambio] [decimal](18, 4) not null,
    [PorcentajeIVA] [decimal](18, 4) not null,
    [PorcentajeRecargo] [decimal](18, 4) not null,
    [PorcentajeDescuentoGeneral] [decimal](18, 4) not null,
    [PorcentajeAnticipo] [decimal](18, 4) not null,
    [ImporteAnticipo] [decimal](18, 4) not null,
    [MedioPagoAnticipo] [nvarchar](100) not null,
    [ComprobanteAnticipo] [nvarchar](100) not null,
    [FechaAnticipo] [datetime2](7) null,
    [CondicionPago] [nvarchar](250) not null,
    [PlazoEntrega] [nvarchar](250) not null,
    [EntregaIncluida] [bit] not null,
    [Observaciones] [nvarchar](1000) not null,
    primary key ([Id])
);
GO
create table [dbo].[Presupuestos] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [ClienteId] [uniqueidentifier] not null,
    [FechaEmision] [datetime2](7) not null,
    [FechaVencimiento] [datetime2](7) not null,
    [Moneda] [int] not null,
    [TipoCambio] [decimal](18, 4) not null,
    [PorcentajeIVA] [decimal](18, 4) not null,
    [PorcentajeRecargo] [decimal](18, 4) not null,
    [PorcentajeDescuentoGeneral] [decimal](18, 4) not null,
    [PorcentajeAnticipo] [decimal](18, 4) not null,
    [CondicionPago] [nvarchar](250) not null,
    [PlazoEntrega] [nvarchar](250) not null,
    [EntregaIncluida] [bit] not null,
    [Estado] [int] not null,
    [Observaciones] [nvarchar](1000) not null,
    primary key ([Id])
);
GO
create table [dbo].[Productos] (
    [Id] [uniqueidentifier] not null,
    [CodigoSku] [nvarchar](50) not null,
    [Nombre] [nvarchar](250) not null,
    [Descripcion] [nvarchar](500) null,
    [CategoriaId] [uniqueidentifier] null,
    [Tipo] [int] not null,
    [UnidadMedida] [nvarchar](50) null,
    [CalidadAisi] [nvarchar](250) null,
    [EspesorMm] [decimal](18, 3) null,
    [Acabado] [nvarchar](250) null,
    [AnchoMm] [decimal](18, 3) null,
    [LargoMm] [decimal](18, 3) null,
    [DiametroMm] [decimal](18, 3) null,
    [PesoUnitarioKg] [decimal](18, 3) null,
    [Medida] [nvarchar](500) null,
    [PrecioBase] [decimal](18, 2) not null,
    [CostoActual] [decimal](18, 2) not null,
    [StockMinimo] [decimal](18, 3) not null,
    [PuntoReposicion] [decimal](18, 3) not null,
    [EsVendible] [bit] not null,
    [EsComprable] [bit] not null,
    [EsFabricable] [bit] not null,
    [Activo] [bit] not null,
    [RutaImagen] [nvarchar](255) null,
    [RutaPlanoTecnico] [nvarchar](255) null,
    [FechaAlta] [datetime2](7) not null,
    [FechaModificacion] [datetime2](7) null,
    primary key ([Id])
);
GO
create table [dbo].[Proveedores] (
    [Id] [uniqueidentifier] not null,
    [Cuit] [nvarchar](50) not null,
    [RazonSocial] [nvarchar](250) not null,
    [NombreFantasia] [nvarchar](250) null,
    [Direccion] [nvarchar](500) null,
    [Localidad] [nvarchar](250) null,
    [Provincia] [nvarchar](250) null,
    [CodigoPostal] [nvarchar](50) null,
    [Email] [nvarchar](250) null,
    [Telefono] [nvarchar](50) null,
    [ContactoComercial] [nvarchar](250) null,
    [LimiteCredito] [decimal](18, 2) not null,
    [SaldoCuenta] [decimal](18, 2) not null,
    [PlazoEntregaDias] [int] not null,
    [Activo] [bit] not null,
    [FechaAlta] [datetime2](7) not null,
    primary key ([Id])
);
GO
create table [dbo].[RecepcionesCompra] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [OrdenCompraId] [uniqueidentifier] not null,
    [FechaRecepcion] [datetime2](7) not null,
    [NumeroRemitoProveedor] [nvarchar](50) null,
    [Estado] [nvarchar](50) null,
    [RecibidoPor] [nvarchar](250) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[Remitos] (
    [Id] [uniqueidentifier] not null,
    [Numero] [nvarchar](50) not null,
    [PedidoId] [uniqueidentifier] not null,
    [ClienteId] [uniqueidentifier] not null,
    [FechaEmision] [datetime2](7) not null,
    [FechaDespacho] [datetime2](7) null,
    [FechaEntrega] [datetime2](7) null,
    [Estado] [int] not null,
    [DireccionEntrega] [nvarchar](500) null,
    [ReceptorNombre] [nvarchar](250) null,
    [ReceptorDocumento] [nvarchar](250) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[ReservasStock] (
    [Id] [uniqueidentifier] not null,
    [ProductoId] [uniqueidentifier] not null,
    [UbicacionId] [uniqueidentifier] not null,
    [DetallePedidoId] [uniqueidentifier] null,
    [OrdenFabricacionId] [uniqueidentifier] null,
    [FechaReserva] [datetime2](7) not null,
    [FechaLiberacion] [datetime2](7) null,
    [CantidadReservada] [decimal](18, 3) not null,
    [CantidadConsumida] [decimal](18, 3) not null,
    [Estado] [int] not null,
    [MotivoLiberacion] [nvarchar](500) null,
    [Observaciones] [nvarchar](500) null,
    primary key ([Id])
);
GO
create table [dbo].[StockProductos] (
    [Id] [uniqueidentifier] not null,
    [ProductoId] [uniqueidentifier] not null,
    [UbicacionId] [uniqueidentifier] not null,
    [CantidadFisica] [decimal](18, 3) not null,
    [CantidadReservada] [decimal](18, 3) not null,
    [CantidadBloqueada] [decimal](18, 3) not null,
    [CostoPromedio] [decimal](18, 2) not null,
    [FechaActualizacion] [datetime2](7) not null,
    primary key ([Id])
);
GO
create table [dbo].[UbicacionesDeposito] (
    [Id] [uniqueidentifier] not null,
    [DepositoId] [uniqueidentifier] not null,
    [Codigo] [nvarchar](50) not null,
    [Sector] [nvarchar](250) null,
    [Pasillo] [nvarchar](250) null,
    [Estanteria] [nvarchar](250) null,
    [Nivel] [nvarchar](250) null,
    [Descripcion] [nvarchar](500) null,
    [Activo] [bit] not null,
    primary key ([Id])
);
GO
create table [dbo].[Vehiculos] (
    [Id] [uniqueidentifier] not null,
    [Patente] [nvarchar](50) not null,
    [Marca] [nvarchar](250) null,
    [Modelo] [nvarchar](250) null,
    [Anio] [int] not null,
    [CapacidadKg] [decimal](18, 3) not null,
    [CapacidadM3] [decimal](18, 3) null,
    [VencimientoSeguro] [datetime2](7) null,
    [VencimientoVtv] [datetime2](7) null,
    [Estado] [nvarchar](50) null,
    [Activo] [bit] not null,
    primary key ([Id])
);
GO
alter table [dbo].[UbicacionesDeposito] add constraint [Deposito_Ubicaciones] foreign key ([DepositoId]) references [dbo].[Depositos]([Id]) on delete cascade;
GO
alter table [dbo].[InspeccionesDevolucion] add constraint [DetalleDevolucion_Inspecciones] foreign key ([DetalleDevolucionId]) references [dbo].[DetallesDevolucion]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesDevolucion] add constraint [Devolucion_Detalles] foreign key ([DevolucionId]) references [dbo].[Devoluciones]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesFactura] add constraint [Factura_Detalles] foreign key ([FacturaId]) references [dbo].[Facturas]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesHojaRuta] add constraint [HojaRuta_Detalles] foreign key ([HojaRutaId]) references [dbo].[HojasRuta]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesOrdenCompra] add constraint [OrdenCompra_Detalles] foreign key ([OrdenCompraId]) references [dbo].[OrdenesCompra]([Id]) on delete cascade;
GO
alter table [dbo].[AvancesProduccion] add constraint [OrdenFabricacion_Avances] foreign key ([OrdenFabricacionId]) references [dbo].[OrdenesFabricacion]([Id]) on delete cascade;
GO
alter table [dbo].[ControlesCalidad] add constraint [OrdenFabricacion_Controles] foreign key ([OrdenFabricacionId]) references [dbo].[OrdenesFabricacion]([Id]) on delete cascade;
GO
alter table [dbo].[ConsumosMaterial] add constraint [OrdenFabricacion_Materiales] foreign key ([OrdenFabricacionId]) references [dbo].[OrdenesFabricacion]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesPedido] add constraint [Pedido_Detalles] foreign key ([PedidoId]) references [dbo].[Pedidos]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesPresupuesto] add constraint [Presupuesto_Detalles] foreign key ([PresupuestoId]) references [dbo].[Presupuestos]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesRecepcionCompra] add constraint [RecepcionCompra_Detalles] foreign key ([RecepcionId]) references [dbo].[RecepcionesCompra]([Id]) on delete cascade;
GO
alter table [dbo].[DetallesRemito] add constraint [RemitoEntrega_Detalles] foreign key ([RemitoId]) references [dbo].[Remitos]([Id]) on delete cascade;
GO
ALTER TABLE [dbo].[Productos] WITH CHECK ADD CONSTRAINT [FK_Productos_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [dbo].[CategoriasProducto]([Id]);
GO
ALTER TABLE [dbo].[StockProductos] WITH CHECK ADD CONSTRAINT [FK_StockProductos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[StockProductos] WITH CHECK ADD CONSTRAINT [FK_StockProductos_UbicacionId] FOREIGN KEY ([UbicacionId]) REFERENCES [dbo].[UbicacionesDeposito]([Id]);
GO
ALTER TABLE [dbo].[MovimientosStock] WITH CHECK ADD CONSTRAINT [FK_MovimientosStock_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[MovimientosStock] WITH CHECK ADD CONSTRAINT [FK_MovimientosStock_UbicacionId] FOREIGN KEY ([UbicacionId]) REFERENCES [dbo].[UbicacionesDeposito]([Id]);
GO
ALTER TABLE [dbo].[ReservasStock] WITH CHECK ADD CONSTRAINT [FK_ReservasStock_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[ReservasStock] WITH CHECK ADD CONSTRAINT [FK_ReservasStock_UbicacionId] FOREIGN KEY ([UbicacionId]) REFERENCES [dbo].[UbicacionesDeposito]([Id]);
GO
ALTER TABLE [dbo].[ReservasStock] WITH CHECK ADD CONSTRAINT [FK_ReservasStock_OrdenFabricacionId] FOREIGN KEY ([OrdenFabricacionId]) REFERENCES [dbo].[OrdenesFabricacion]([Id]);
GO
ALTER TABLE [dbo].[OrdenesFabricacion] WITH CHECK ADD CONSTRAINT [FK_OrdenesFabricacion_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[OrdenesFabricacion] WITH CHECK ADD CONSTRAINT [FK_OrdenesFabricacion_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [dbo].[Pedidos]([Id]);
GO
ALTER TABLE [dbo].[ConsumosMaterial] WITH CHECK ADD CONSTRAINT [FK_ConsumosMaterial_ProductoMaterialId] FOREIGN KEY ([ProductoMaterialId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[OrdenesCompra] WITH CHECK ADD CONSTRAINT [FK_OrdenesCompra_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [dbo].[Proveedores]([Id]);
GO
ALTER TABLE [dbo].[DetallesOrdenCompra] WITH CHECK ADD CONSTRAINT [FK_DetallesOrdenCompra_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[RecepcionesCompra] WITH CHECK ADD CONSTRAINT [FK_RecepcionesCompra_OrdenCompraId] FOREIGN KEY ([OrdenCompraId]) REFERENCES [dbo].[OrdenesCompra]([Id]);
GO
ALTER TABLE [dbo].[DetallesRecepcionCompra] WITH CHECK ADD CONSTRAINT [FK_DetallesRecepcionCompra_DetalleOrdenCompraId] FOREIGN KEY ([DetalleOrdenCompraId]) REFERENCES [dbo].[DetallesOrdenCompra]([Id]);
GO
ALTER TABLE [dbo].[DetallesRecepcionCompra] WITH CHECK ADD CONSTRAINT [FK_DetallesRecepcionCompra_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos]([Id]);
GO
ALTER TABLE [dbo].[Facturas] WITH CHECK ADD CONSTRAINT [FK_Facturas_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [dbo].[Pedidos]([Id]);
GO
ALTER TABLE [dbo].[Facturas] WITH CHECK ADD CONSTRAINT [FK_Facturas_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
ALTER TABLE [dbo].[CobrosCliente] WITH CHECK ADD CONSTRAINT [FK_CobrosCliente_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
ALTER TABLE [dbo].[CobrosCliente] WITH CHECK ADD CONSTRAINT [FK_CobrosCliente_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [dbo].[Facturas]([Id]);
GO
ALTER TABLE [dbo].[CobrosCliente] WITH CHECK ADD CONSTRAINT [FK_CobrosCliente_MedioPagoId] FOREIGN KEY ([MedioPagoId]) REFERENCES [dbo].[MediosPago]([Id]);
GO
ALTER TABLE [dbo].[PagosProveedor] WITH CHECK ADD CONSTRAINT [FK_PagosProveedor_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [dbo].[Proveedores]([Id]);
GO
ALTER TABLE [dbo].[PagosProveedor] WITH CHECK ADD CONSTRAINT [FK_PagosProveedor_OrdenCompraId] FOREIGN KEY ([OrdenCompraId]) REFERENCES [dbo].[OrdenesCompra]([Id]);
GO
ALTER TABLE [dbo].[PagosProveedor] WITH CHECK ADD CONSTRAINT [FK_PagosProveedor_MedioPagoId] FOREIGN KEY ([MedioPagoId]) REFERENCES [dbo].[MediosPago]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaCliente] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaCliente_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaCliente] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaCliente_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [dbo].[Facturas]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaCliente] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaCliente_CobroId] FOREIGN KEY ([CobroId]) REFERENCES [dbo].[CobrosCliente]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaCliente] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaCliente_NotaCreditoId] FOREIGN KEY ([NotaCreditoId]) REFERENCES [dbo].[NotasCredito]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaProveedor] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaProveedor_ProveedorId] FOREIGN KEY ([ProveedorId]) REFERENCES [dbo].[Proveedores]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaProveedor] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaProveedor_OrdenCompraId] FOREIGN KEY ([OrdenCompraId]) REFERENCES [dbo].[OrdenesCompra]([Id]);
GO
ALTER TABLE [dbo].[MovimientosCuentaProveedor] WITH CHECK ADD CONSTRAINT [FK_MovimientosCuentaProveedor_PagoId] FOREIGN KEY ([PagoId]) REFERENCES [dbo].[PagosProveedor]([Id]);
GO
ALTER TABLE [dbo].[NotasCredito] WITH CHECK ADD CONSTRAINT [FK_NotasCredito_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [dbo].[Facturas]([Id]);
GO
ALTER TABLE [dbo].[NotasCredito] WITH CHECK ADD CONSTRAINT [FK_NotasCredito_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
ALTER TABLE [dbo].[NotasCredito] WITH CHECK ADD CONSTRAINT [FK_NotasCredito_DevolucionId] FOREIGN KEY ([DevolucionId]) REFERENCES [dbo].[Devoluciones]([Id]);
GO
ALTER TABLE [dbo].[Remitos] WITH CHECK ADD CONSTRAINT [FK_Remitos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [dbo].[Pedidos]([Id]);
GO
ALTER TABLE [dbo].[Remitos] WITH CHECK ADD CONSTRAINT [FK_Remitos_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
ALTER TABLE [dbo].[HojasRuta] WITH CHECK ADD CONSTRAINT [FK_HojasRuta_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [dbo].[Vehiculos]([Id]);
GO
ALTER TABLE [dbo].[HojasRuta] WITH CHECK ADD CONSTRAINT [FK_HojasRuta_ChoferId] FOREIGN KEY ([ChoferId]) REFERENCES [dbo].[Choferes]([Id]);
GO
ALTER TABLE [dbo].[DetallesHojaRuta] WITH CHECK ADD CONSTRAINT [FK_DetallesHojaRuta_RemitoId] FOREIGN KEY ([RemitoId]) REFERENCES [dbo].[Remitos]([Id]);
GO
ALTER TABLE [dbo].[ComprobantesEntrega] WITH CHECK ADD CONSTRAINT [FK_ComprobantesEntrega_DetalleHojaRutaId] FOREIGN KEY ([DetalleHojaRutaId]) REFERENCES [dbo].[DetallesHojaRuta]([Id]);
GO
ALTER TABLE [dbo].[Devoluciones] WITH CHECK ADD CONSTRAINT [FK_Devoluciones_RemitoId] FOREIGN KEY ([RemitoId]) REFERENCES [dbo].[Remitos]([Id]);
GO
ALTER TABLE [dbo].[Devoluciones] WITH CHECK ADD CONSTRAINT [FK_Devoluciones_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [dbo].[Pedidos]([Id]);
GO
ALTER TABLE [dbo].[Devoluciones] WITH CHECK ADD CONSTRAINT [FK_Devoluciones_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Clientes]([Id]);
GO
CREATE UNIQUE INDEX [UQ_Productos_CodigoSku] ON [dbo].[Productos] ([CodigoSku]);
GO
CREATE UNIQUE INDEX [UQ_CategoriasProducto_Codigo] ON [dbo].[CategoriasProducto] ([Codigo]);
GO
CREATE UNIQUE INDEX [UQ_Depositos_Codigo] ON [dbo].[Depositos] ([Codigo]);
GO
CREATE UNIQUE INDEX [UQ_UbicacionesDeposito_DepositoId_Codigo] ON [dbo].[UbicacionesDeposito] ([DepositoId], [Codigo]);
GO
CREATE UNIQUE INDEX [UQ_StockProductos_ProductoId_UbicacionId] ON [dbo].[StockProductos] ([ProductoId], [UbicacionId]);
GO
CREATE UNIQUE INDEX [UQ_OrdenesFabricacion_Numero] ON [dbo].[OrdenesFabricacion] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_Proveedores_Cuit] ON [dbo].[Proveedores] ([Cuit]);
GO
CREATE UNIQUE INDEX [UQ_OrdenesCompra_Numero] ON [dbo].[OrdenesCompra] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_RecepcionesCompra_Numero] ON [dbo].[RecepcionesCompra] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_Facturas_Numero] ON [dbo].[Facturas] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_MediosPago_Codigo] ON [dbo].[MediosPago] ([Codigo]);
GO
CREATE UNIQUE INDEX [UQ_CobrosCliente_Numero] ON [dbo].[CobrosCliente] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_PagosProveedor_Numero] ON [dbo].[PagosProveedor] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_NotasCredito_Numero] ON [dbo].[NotasCredito] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_Vehiculos_Patente] ON [dbo].[Vehiculos] ([Patente]);
GO
CREATE UNIQUE INDEX [UQ_Choferes_Documento] ON [dbo].[Choferes] ([Documento]);
GO
CREATE UNIQUE INDEX [UQ_Choferes_LicenciaConducir] ON [dbo].[Choferes] ([LicenciaConducir]);
GO
CREATE UNIQUE INDEX [UQ_Remitos_Numero] ON [dbo].[Remitos] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_HojasRuta_Numero] ON [dbo].[HojasRuta] ([Numero]);
GO
CREATE UNIQUE INDEX [UQ_Devoluciones_Numero] ON [dbo].[Devoluciones] ([Numero]);
GO
CREATE INDEX [IX_MovimientosStock_ProductoId_FechaMovimiento] ON [dbo].[MovimientosStock] ([ProductoId], [FechaMovimiento]);
GO
CREATE INDEX [IX_ReservasStock_ProductoId] ON [dbo].[ReservasStock] ([ProductoId]);
GO
CREATE INDEX [IX_ReservasStock_DetallePedidoId] ON [dbo].[ReservasStock] ([DetallePedidoId]);
GO
CREATE INDEX [IX_OrdenesFabricacion_DetallePedidoId] ON [dbo].[OrdenesFabricacion] ([DetallePedidoId]);
GO
CREATE INDEX [IX_OrdenesCompra_ProveedorId] ON [dbo].[OrdenesCompra] ([ProveedorId]);
GO
CREATE INDEX [IX_RecepcionesCompra_OrdenCompraId] ON [dbo].[RecepcionesCompra] ([OrdenCompraId]);
GO
CREATE INDEX [IX_Facturas_PedidoId] ON [dbo].[Facturas] ([PedidoId]);
GO
CREATE INDEX [IX_Facturas_ClienteId] ON [dbo].[Facturas] ([ClienteId]);
GO
CREATE INDEX [IX_CobrosCliente_ClienteId] ON [dbo].[CobrosCliente] ([ClienteId]);
GO
CREATE INDEX [IX_PagosProveedor_ProveedorId] ON [dbo].[PagosProveedor] ([ProveedorId]);
GO
CREATE INDEX [IX_MovimientosCuentaCliente_ClienteId_Fecha] ON [dbo].[MovimientosCuentaCliente] ([ClienteId], [Fecha]);
GO
CREATE INDEX [IX_MovimientosCuentaProveedor_ProveedorId_Fecha] ON [dbo].[MovimientosCuentaProveedor] ([ProveedorId], [Fecha]);
GO
CREATE INDEX [IX_NotasCredito_FacturaId] ON [dbo].[NotasCredito] ([FacturaId]);
GO
CREATE INDEX [IX_Remitos_PedidoId] ON [dbo].[Remitos] ([PedidoId]);
GO
CREATE INDEX [IX_DetallesRemito_DetallePedidoId] ON [dbo].[DetallesRemito] ([DetallePedidoId]);
GO
CREATE INDEX [IX_DetallesHojaRuta_RemitoId] ON [dbo].[DetallesHojaRuta] ([RemitoId]);
GO
CREATE INDEX [IX_ComprobantesEntrega_DetalleHojaRutaId] ON [dbo].[ComprobantesEntrega] ([DetalleHojaRutaId]);
GO
CREATE INDEX [IX_Devoluciones_RemitoId] ON [dbo].[Devoluciones] ([RemitoId]);
GO
CREATE INDEX [IX_DetallesDevolucion_DetallePedidoId] ON [dbo].[DetallesDevolucion] ([DetallePedidoId]);
GO
