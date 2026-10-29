using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    /// <summary>
    /// Compras y stock: catalogo, existencias, movimientos, reservas, proveedores y ordenes de compra.
    /// </summary>
    public class ModuloInventario : ModuloBase
    {
        private readonly CatalogoService _catalogo = new CatalogoService();
        private readonly InventarioService _inventario = new InventarioService();
        private readonly ComprasService _compras = new ComprasService();
        private readonly DocumentosService _documentos = new DocumentosService();

        public ModuloInventario()
        {
            AgregarPestana("Stock", CrearStock(), Permiso.InventarioVer);
            AgregarPestana("Preparation orders", CrearPreparacion(), Permiso.InventarioVer);
            AgregarPestana("Missing products", CrearFaltantes(), Permiso.InventarioVer);
            AgregarPestana("Catalog", CrearCatalogo(), Permiso.CatalogoVer);
            AgregarPestana("Movements", CrearMovimientos(), Permiso.InventarioVer);
            AgregarPestana("Reservations", CrearReservas(), Permiso.InventarioVer);
            AgregarPestana("Reorder suggestions", CrearSugerencias(), Permiso.ComprasVer);
            AgregarPestana("Purchase orders", CrearOrdenes(), Permiso.ComprasVer);
            AgregarPestana("Receipts", CrearRecepciones(), Permiso.ComprasVer);
            AgregarPestana("Suppliers", CrearProveedores(), Permiso.ComprasVer);
            CargarPestanaActual();
        }

        private List<Opcion> OpcionesProducto(bool soloVendibles)
        {
            return _catalogo.BuscarProductos(string.Empty, null, null, true, soloVendibles)
                .Select(p => new Opcion(p.Sku + "  " + p.Nombre, p.Id)).ToList();
        }

        // ---------------------------------------------------------------
        // Existencias
        // ---------------------------------------------------------------

        private Control CrearStock()
        {
            CheckBox soloAlertas = new CheckBox { Text = Tr.T("Only alerts"), AutoSize = true, Margin = new Padding(8, 7, 8, 0) };
            PanelLista lista = null;
            bool alertas = false;

            lista = new PanelLista(() => _inventario.ConsultarStock(string.Empty, null, alertas));
            soloAlertas.CheckedChanged += (remitente, argumentos) =>
            {
                alertas = soloAlertas.Checked;
                lista.Recargar();
            };
            lista.PonerEnBarra(soloAlertas);

            lista.Agregar("Register movement", fila =>
            {
                List<Opcion> ubicaciones = _inventario.ObtenerUbicaciones(true)
                    .Select(u => new Opcion(u.Deposito + " / " + u.Codigo, u.Id)).ToList();

                List<Opcion> tipos = new List<Opcion>();

                foreach (TipoMovimientoStock tipo in new[]
                {
                    TipoMovimientoStock.AjustePositivo, TipoMovimientoStock.AjusteNegativo,
                    TipoMovimientoStock.RecepcionCompra, TipoMovimientoStock.ProductoTerminado,
                    TipoMovimientoStock.DevolucionCliente
                })
                {
                    tipos.Add(new Opcion(Etiquetas.De(tipo), tipo));
                }

                Respuesta datos = Dialogo.Pedir(this, "Register movement",
                    Campo.Combo("producto", "Product", OpcionesProducto(false)),
                    Campo.Combo("ubicacion", "Location", ubicaciones),
                    Campo.Combo("tipo", "Movement type", tipos),
                    Campo.Numero("cantidad", "Quantity", 1m, 0.01m, 1000000m, 3),
                    Campo.Numero("costo", "Unit cost", 0m),
                    Campo.Texto("documento", "Document", "", false, 60),
                    Campo.Texto("lote", "Lot", "", false, 60),
                    Campo.Texto("colada", "Heat number", "", false, 60),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (datos == null)
                {
                    return;
                }

                _inventario.RegistrarMovimiento(new MovimientoSolicitud
                {
                    ProductoId = datos.GuidDe("producto"),
                    UbicacionId = datos.GuidDe("ubicacion"),
                    Tipo = (TipoMovimientoStock)datos.Seleccion("tipo"),
                    Cantidad = datos.Numero("cantidad"),
                    CostoUnitario = datos.Numero("costo") > 0m ? (decimal?)datos.Numero("costo") : null,
                    Documento = datos.Texto("documento"),
                    Lote = datos.Texto("lote"),
                    Colada = datos.Texto("colada"),
                    Observaciones = datos.Texto("obs")
                });

                lista.Recargar();
            }, false, Permiso.InventarioMover);

            lista.Agregar("Add stock", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Add stock",
                    Campo.Combo("producto", "Product", OpcionesProducto(false)),
                    Campo.Numero("cantidad", "Quantity", 1m, 0.01m, 1000000m, 3),
                    Campo.Texto("motivo", "Reason", Tr.T("Stock entry"), true, 200));

                if (datos == null)
                {
                    return;
                }

                new PreparacionService().IngresarStock(datos.GuidDe("producto"), datos.Numero("cantidad"), datos.Texto("motivo"));
                lista.Recargar();
            }, false, Permiso.InventarioMover);

            lista.Agregar("Transfer", fila =>
            {
                List<Opcion> ubicaciones = _inventario.ObtenerUbicaciones(true)
                    .Select(u => new Opcion(u.Deposito + " / " + u.Codigo, u.Id)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Transfer",
                    Campo.Combo("producto", "Product", OpcionesProducto(false)),
                    Campo.Combo("origen", "From location", ubicaciones),
                    Campo.Combo("destino", "To location", ubicaciones),
                    Campo.Numero("cantidad", "Quantity", 1m, 0.01m, 1000000m, 3),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (datos == null)
                {
                    return;
                }

                _inventario.Transferir(datos.GuidDe("producto"), datos.GuidDe("origen"), datos.GuidDe("destino"),
                    datos.Numero("cantidad"), datos.Texto("obs"));
                lista.Recargar();
            }, false, Permiso.InventarioMover);

            lista.AgregarExportacion();
            return lista;
        }

        // ---------------------------------------------------------------
        // Catalogo
        // ---------------------------------------------------------------

        private Control CrearCatalogo()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _catalogo.BuscarProductos(string.Empty, null, null, false, false));

            lista.Agregar("New product", fila =>
            {
                Producto producto = new Producto { Id = Guid.NewGuid(), Activo = true, FechaAlta = DateTime.Now };

                if (EditarProducto(producto))
                {
                    lista.Recargar();
                }
            }, false, Permiso.CatalogoEditar);

            lista.Agregar("Edit", fila =>
            {
                Producto producto = _catalogo.ObtenerProducto(Estilo.IdDe(fila).Value);

                if (EditarProducto(producto))
                {
                    lista.Recargar();
                }
            }, true, Permiso.CatalogoEditar);

            lista.Agregar("Activate / deactivate", fila =>
            {
                _catalogo.CambiarEstadoProducto(Estilo.IdDe(fila).Value, !Estilo.Valor<bool>(fila, "Activo"));
                lista.Recargar();
            }, true, Permiso.CatalogoEditar);

            lista.Agregar("Mass price update", fila =>
            {
                List<Opcion> categorias = new List<Opcion> { new Opcion(Tr.T("All categories"), Guid.Empty) };
                Dictionary<Guid, string> rutas = _catalogo.ObtenerRutasDeCategoria();
                categorias.AddRange(rutas.OrderBy(r => r.Value).Select(r => new Opcion(r.Value, r.Key)));

                Respuesta datos = Dialogo.Pedir(this, "Mass price update",
                    "Applies a percentage to the base price of every active product in the category.",
                    Campo.Combo("categoria", "Category", categorias),
                    Campo.Numero("porcentaje", "Percentage", 10m, -90m, 500m),
                    Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos == null)
                {
                    return;
                }

                Guid categoria = datos.GuidDe("categoria");
                int cantidad = _catalogo.AumentoMasivoPrecios(categoria == Guid.Empty ? (Guid?)null : categoria,
                    datos.Numero("porcentaje"), datos.Texto("motivo"));

                Estilo.Informar(this, Tr.T("{0} product(s) updated.", cantidad));
                lista.Recargar();
            }, false, Permiso.CatalogoEditar);

            lista.Agregar("New category", fila =>
            {
                List<Opcion> padres = new List<Opcion> { new Opcion(Tr.T("(none)"), Guid.Empty) };
                padres.AddRange(_catalogo.ObtenerRutasDeCategoria().OrderBy(r => r.Value).Select(r => new Opcion(r.Value, r.Key)));

                Respuesta datos = Dialogo.Pedir(this, "New category",
                    Campo.Texto("codigo", "Code", "", true, 30),
                    Campo.Texto("nombre", "Name", "", true, 100),
                    Campo.Combo("padre", "Parent category", padres));

                if (datos == null)
                {
                    return;
                }

                Guid padre = datos.GuidDe("padre");
                _catalogo.GuardarCategoria(new CategoriaProducto
                {
                    Id = Guid.NewGuid(),
                    Codigo = datos.Texto("codigo"),
                    Nombre = datos.Texto("nombre"),
                    CategoriaPadreId = padre == Guid.Empty ? (Guid?)null : padre,
                    Activo = true,
                    FechaAlta = DateTime.Now
                });
            }, false, Permiso.CatalogoEditar);

            lista.AgregarExportacion();
            return lista;
        }

        private bool EditarProducto(Producto producto)
        {
            bool nuevo = producto.CodigoSku == null;
            Dictionary<Guid, string> rutas = _catalogo.ObtenerRutasDeCategoria();

            List<Opcion> categorias = rutas.OrderBy(r => r.Value).Select(r => new Opcion(r.Value, r.Key)).ToList();
            List<Opcion> tipos = Enum.GetValues(typeof(TipoProducto)).Cast<TipoProducto>()
                .Select(t => new Opcion(Etiquetas.De(t), t)).ToList();

            Respuesta datos = Dialogo.Pedir(this, nuevo ? "New product" : "Edit product",
                Campo.Texto("sku", "SKU", producto.CodigoSku ?? "", true, 40),
                Campo.Texto("nombre", "Name", producto.Nombre ?? "", true, 150),
                Campo.Combo("categoria", "Category", categorias, false),
                Campo.Combo("tipo", "Product type", tipos),
                Campo.Texto("unidad", "Unit", producto.UnidadMedida ?? "UN", true, 10),
                Campo.Texto("calidad", "Steel grade", producto.CalidadAisi ?? "", false, 20),
                Campo.Texto("acabado", "Finish", producto.Acabado ?? "", false, 40),
                Campo.Texto("medida", "Size", producto.Medida ?? "", false, 60),
                Campo.Numero("precio", "Base price", producto.PrecioBase),
                Campo.Numero("costo", "Current cost", producto.CostoActual),
                Campo.Numero("minimo", "Minimum stock", producto.StockMinimo, 0m, 1000000m, 3),
                Campo.Numero("reposicion", "Reorder point", producto.PuntoReposicion, 0m, 1000000m, 3),
                Campo.Check("vendible", "Sellable", nuevo ? true : producto.EsVendible),
                Campo.Check("comprable", "Purchasable", nuevo ? true : producto.EsComprable),
                Campo.Check("fabricable", "Manufacturable", producto.EsFabricable));

            if (datos == null)
            {
                return false;
            }

            producto.CodigoSku = datos.Texto("sku");
            producto.Nombre = datos.Texto("nombre");
            producto.CategoriaId = datos.Seleccion("categoria") is Guid ? (Guid?)datos.GuidDe("categoria") : producto.CategoriaId;
            producto.Tipo = (TipoProducto)datos.Seleccion("tipo");
            producto.UnidadMedida = datos.Texto("unidad");
            producto.CalidadAisi = datos.Texto("calidad");
            producto.Acabado = datos.Texto("acabado");
            producto.Medida = datos.Texto("medida");
            producto.PrecioBase = datos.Numero("precio");
            producto.CostoActual = datos.Numero("costo");
            producto.StockMinimo = datos.Numero("minimo");
            producto.PuntoReposicion = datos.Numero("reposicion");
            producto.EsVendible = datos.Check("vendible");
            producto.EsComprable = datos.Check("comprable");
            producto.EsFabricable = datos.Check("fabricable");

            _catalogo.GuardarProducto(producto);
            return true;
        }

        // ---------------------------------------------------------------
        // Movimientos y reservas
        // ---------------------------------------------------------------

        private Control CrearMovimientos()
        {
            PanelLista lista = new PanelLista(() => _inventario.ObtenerMovimientos(null, null, null, 500));
            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearReservas()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _inventario.ObtenerReservas(true));

            lista.Agregar("Release reservation", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Release reservation", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _inventario.LiberarReserva(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.InventarioMover);

            lista.AgregarExportacion();
            return lista;
        }

        // ---------------------------------------------------------------
        // Compras
        // ---------------------------------------------------------------

        // ---------------------------------------------------------------
        // Faltantes: lo que piden los presupuestos y no alcanza en stock
        // ---------------------------------------------------------------

        private Control CrearPreparacion()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => new LogisticaService().ObtenerOrdenesPreparacion());

            lista.Agregar("Prepare order", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Prepare order",
                    "Reserves the stock the order is missing so it can be shipped. Lines that must be manufactured are resolved in Production.",
                    Campo.Check("ingresar", "Add the missing stock to the warehouse", true));

                if (datos == null)
                {
                    return;
                }

                ResultadoPreparacion r = new PreparacionService().PrepararPedido(Estilo.Valor<Guid>(fila, "PedidoId"), datos.Check("ingresar"));
                List<string> partes = new List<string>();

                if (r.Reservados.Count > 0) partes.Add(Tr.T("Reserved: {0}", string.Join(", ", r.Reservados)));
                if (r.Ingresados.Count > 0) partes.Add(Tr.T("Stock added: {0}", string.Join(", ", r.Ingresados)));
                if (r.Faltantes.Count > 0) partes.Add(Tr.T("Still missing: {0}", string.Join(", ", r.Faltantes)));
                if (r.EnProduccion.Count > 0) partes.Add(Tr.T("Waiting for manufacturing: {0}", string.Join(", ", r.EnProduccion)));
                if (partes.Count == 0) partes.Add(Tr.T("The order already has everything reserved."));

                Estilo.Informar(this, string.Join(Environment.NewLine, partes));
                lista.Recargar();
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearFaltantes()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => new FaltantesService().Generales());
            lista.PintarFilas(System.Drawing.Color.FromArgb(255, 238, 236), System.Drawing.Color.FromArgb(120, 20, 20));

            lista.Agregar("Add stock", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Add stock",
                    Campo.Numero("cantidad", "Quantity", Estilo.Valor<decimal>(fila, "Faltante"), 0.01m, 1000000m, 3),
                    Campo.Texto("motivo", "Reason", Tr.T("Stock entry"), true, 200));

                if (datos == null)
                {
                    return;
                }

                new PreparacionService().IngresarStock(Estilo.Valor<Guid>(fila, "ProductoId"), datos.Numero("cantidad"), datos.Texto("motivo"));
                lista.Recargar();
            }, true, Permiso.InventarioMover);

            lista.Agregar("Create purchase order", fila =>
            {
                if (!Estilo.Valor<bool>(fila, "Comprable"))
                {
                    Estilo.Avisar(this, Tr.T("This product is not marked as purchasable."));
                    return;
                }

                List<Opcion> proveedores = _compras.ObtenerProveedores(string.Empty, true)
                    .Select(p => new Opcion(p.RazonSocial, p.Id)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Create purchase order",
                    Campo.Combo("proveedor", "Supplier", proveedores),
                    Campo.Numero("cantidad", "Quantity", Estilo.Valor<decimal>(fila, "Faltante"), 0.01m, 1000000m, 3),
                    Campo.Numero("costo", "Unit cost", Estilo.Valor<decimal>(fila, "CostoActual")),
                    Campo.Check("emitir", "Issue now", true));

                if (datos == null)
                {
                    return;
                }

                OrdenCompra orden = _compras.CrearOrden(datos.GuidDe("proveedor"), DateTime.Today.AddDays(10), Tr.T("30 days"), Tr.T("Missing products"),
                    new List<ItemCompra> { new ItemCompra { ProductoId = Estilo.Valor<Guid>(fila, "ProductoId"), Cantidad = datos.Numero("cantidad"), Costo = datos.Numero("costo") } },
                    datos.Check("emitir"));

                Estilo.Informar(this, Tr.T("Purchase order {0} created.", orden.Numero));
            }, true, Permiso.ComprasGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearSugerencias()
        {
            PanelLista lista = new PanelLista(() => _compras.SugerirCompras());

            lista.Agregar("Create purchase order", fila =>
            {
                Guid productoId = Estilo.Valor<Guid>(fila, "ProductoId");
                decimal sugerido = Estilo.Valor<decimal>(fila, "Sugerido");
                decimal costo = Estilo.Valor<decimal>(fila, "CostoActual");

                List<Opcion> proveedores = _compras.ObtenerProveedores(string.Empty, true)
                    .Select(p => new Opcion(p.RazonSocial, p.Id)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Create purchase order",
                    Campo.Combo("proveedor", "Supplier", proveedores),
                    Campo.Numero("cantidad", "Quantity", sugerido, 0.01m, 1000000m, 3),
                    Campo.Numero("costo", "Unit cost", costo),
                    Campo.Check("emitir", "Issue now", true));

                if (datos == null)
                {
                    return;
                }

                OrdenCompra orden = _compras.CrearOrden(datos.GuidDe("proveedor"), DateTime.Today.AddDays(10), Tr.T("30 days"), Tr.T("Reorder"),
                    new List<ItemCompra> { new ItemCompra { ProductoId = productoId, Cantidad = datos.Numero("cantidad"), Costo = datos.Numero("costo") } },
                    datos.Check("emitir"));

                Estilo.Informar(this, Tr.T("Purchase order {0} created.", orden.Numero));
            }, true, Permiso.ComprasGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearOrdenes()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _compras.ObtenerOrdenes(null, null));
            lista.ConDetalle(fila => _compras.ObtenerDetalles(Estilo.IdDe(fila).Value));

            lista.Agregar("New purchase order", fila =>
            {
                List<Opcion> proveedores = _compras.ObtenerProveedores(string.Empty, true)
                    .Select(p => new Opcion(p.RazonSocial, p.Id)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "New purchase order",
                    Campo.Combo("proveedor", "Supplier", proveedores),
                    Campo.Fecha("entrega", "Estimated delivery", DateTime.Today.AddDays(10), true),
                    Campo.Texto("pago", "Payment terms", Tr.T("30 days"), false, 100),
                    Campo.Multilinea("obs", "Notes"),
                    Campo.Check("emitir", "Issue now", true));

                if (datos == null)
                {
                    return;
                }

                List<ItemLinea> items;

                using (EditorLineas editor = new EditorLineas(ModoLineas.Compra, "Purchase order lines"))
                {
                    if (editor.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    items = editor.Items;
                }

                OrdenCompra orden = _compras.CrearOrden(datos.GuidDe("proveedor"), datos.Fecha("entrega"), datos.Texto("pago"),
                    datos.Texto("obs"),
                    items.Select(i => new ItemCompra { ProductoId = i.ProductoId.Value, Cantidad = i.Cantidad, Costo = i.Precio, Descuento = i.Descuento }).ToList(),
                    datos.Check("emitir"));

                Estilo.Informar(this, Tr.T("Purchase order {0} created.", orden.Numero));
                lista.Recargar();
            }, false, Permiso.ComprasGestionar);

            lista.Agregar("Issue", fila =>
            {
                _compras.EmitirOrden(Estilo.IdDe(fila).Value);
                lista.Recargar();
            }, true, Permiso.ComprasGestionar);

            lista.Agregar("Receive goods", fila =>
            {
                Recibir(Estilo.IdDe(fila).Value);
                lista.Recargar();
            }, true, Permiso.ComprasGestionar);

            lista.Agregar("Purchase order PDF", fila =>
            {
                Estilo.AbrirArchivo(this, _documentos.GenerarOrdenCompra(Estilo.IdDe(fila).Value));
            }, true, Permiso.ComprasVer);

            lista.Agregar("Cancel", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Cancel", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _compras.CancelarOrden(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.ComprasGestionar);

            lista.Agregar("Pay supplier", fila =>
            {
                List<Opcion> medios = new FinanzasService().ObtenerMediosPago(true).Select(m => new Opcion(m.Nombre, m.Id)).ToList();
                decimal saldo = Estilo.Valor<decimal>(fila, "SaldoPendiente");

                Respuesta datos = Dialogo.Pedir(this, "Pay supplier",
                    Campo.Combo("medio", "Payment method", medios),
                    Campo.Numero("importe", "Amount", saldo),
                    Campo.Texto("referencia", "Reference", "", false, 60),
                    Campo.Texto("entidad", "Bank", "", false, 60),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (datos == null)
                {
                    return;
                }

                new FinanzasService().RegistrarPagoProveedor(Estilo.Valor<Guid>(fila, "ProveedorId"), Estilo.IdDe(fila),
                    datos.GuidDe("medio"), datos.Numero("importe"), datos.Texto("referencia"), datos.Texto("entidad"), datos.Texto("obs"));
                lista.Recargar();
            }, true, Permiso.PagosRegistrar);

            lista.AgregarExportacion();
            return lista;
        }

        private void Recibir(Guid ordenId)
        {
            List<DetalleOrdenCompraDto> pendientes = _compras.ObtenerDetalles(ordenId).Where(d => d.Pendiente > 0m).ToList();

            if (pendientes.Count == 0)
            {
                Estilo.Avisar(this, Tr.T("There is nothing pending to receive."));
                return;
            }

            Respuesta cabecera = Dialogo.Pedir(this, "Receive goods",
                "Every pending line will be received in full. Use the rejected quantity to register defective goods.",
                Campo.Texto("remito", "Supplier delivery note", "", true, 60),
                Campo.Texto("lote", "Lot", "", false, 60),
                Campo.Texto("colada", "Heat number", "", false, 60),
                Campo.Numero("rechazo", "Rejected % of each line", 0m, 0m, 100m),
                Campo.Texto("motivo", "Rejection reason", "", false, 200),
                Campo.Multilinea("obs", "Notes"));

            if (cabecera == null)
            {
                return;
            }

            decimal porcentajeRechazo = cabecera.Numero("rechazo");

            List<RecepcionItem> items = pendientes.Select(d =>
            {
                decimal rechazada = Math.Round(d.Pendiente * porcentajeRechazo / 100m, 3);
                return new RecepcionItem
                {
                    DetalleOrdenCompraId = d.DetalleId,
                    Recibida = d.Pendiente,
                    Aceptada = d.Pendiente - rechazada,
                    Rechazada = rechazada,
                    MotivoRechazo = rechazada > 0m ? cabecera.Texto("motivo") : string.Empty,
                    Lote = cabecera.Texto("lote"),
                    Colada = cabecera.Texto("colada")
                };
            }).ToList();

            RecepcionCompra recepcion = _compras.RegistrarRecepcion(ordenId, cabecera.Texto("remito"), items, cabecera.Texto("obs"));
            Estilo.Informar(this, Tr.T("Receipt {0} registered.", recepcion.Numero));
        }

        private Control CrearRecepciones()
        {
            PanelLista lista = new PanelLista(() => _compras.ObtenerRecepciones(null));
            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearProveedores()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _compras.ObtenerProveedores(string.Empty, false).Select(ProveedorFila.De).ToList());

            lista.Agregar("New supplier", fila =>
            {
                Proveedor proveedor = new Proveedor { Id = Guid.NewGuid(), Activo = true, FechaAlta = DateTime.Now };

                if (EditarProveedor(proveedor, true))
                {
                    lista.Recargar();
                }
            }, false, Permiso.ProveedoresGestionar);

            lista.Agregar("Edit", fila =>
            {
                Proveedor proveedor = _compras.ObtenerProveedores(string.Empty, false).First(p => p.Id == Estilo.IdDe(fila).Value);

                if (EditarProveedor(proveedor, false))
                {
                    lista.Recargar();
                }
            }, true, Permiso.ProveedoresGestionar);

            lista.Agregar("Account statement", fila =>
            {
                Guid proveedorId = Estilo.IdDe(fila).Value;
                Form ventana = new Form
                {
                    Text = Tr.T("Account statement") + " - " + Estilo.Valor<string>(fila, "RazonSocial"),
                    Size = new System.Drawing.Size(860, 460),
                    StartPosition = FormStartPosition.CenterParent,
                    Font = Estilo.Normal
                };

                PanelLista cuenta = new PanelLista(() => new FinanzasService().ObtenerCuentaProveedor(proveedorId, null, null), false);
                cuenta.AgregarExportacion();
                ventana.Controls.Add(cuenta);
                ventana.Shown += (remitente, argumentos) => cuenta.Recargar();
                ventana.ShowDialog(this);
                ventana.Dispose();
            }, true, Permiso.ComprasVer);

            lista.AgregarExportacion();
            return lista;
        }

        private bool EditarProveedor(Proveedor proveedor, bool nuevo)
        {
            Respuesta datos = Dialogo.Pedir(this, nuevo ? "New supplier" : "Edit supplier",
                Campo.Texto("cuit", "Tax ID (CUIT)", proveedor.Cuit ?? "", true, 13),
                Campo.Texto("razon", "Company name", proveedor.RazonSocial ?? "", true, 150),
                Campo.Texto("direccion", "Address", proveedor.Direccion ?? "", false, 200),
                Campo.Texto("localidad", "City", proveedor.Localidad ?? "", false, 100),
                Campo.Texto("email", "Email", proveedor.Email ?? "", false, 100),
                Campo.Texto("telefono", "Phone", proveedor.Telefono ?? "", false, 40),
                Campo.Texto("contacto", "Contact", proveedor.ContactoComercial ?? "", false, 100),
                Campo.Entero("plazo", "Lead time (days)", proveedor.PlazoEntregaDias, 0, 365),
                Campo.Numero("limite", "Credit limit", proveedor.LimiteCredito),
                Campo.Check("activo", "Active", nuevo ? true : proveedor.Activo));

            if (datos == null)
            {
                return false;
            }

            proveedor.Cuit = datos.Texto("cuit");
            proveedor.RazonSocial = datos.Texto("razon");
            proveedor.Direccion = datos.Texto("direccion");
            proveedor.Localidad = datos.Texto("localidad");
            proveedor.Email = datos.Texto("email");
            proveedor.Telefono = datos.Texto("telefono");
            proveedor.ContactoComercial = datos.Texto("contacto");
            proveedor.PlazoEntregaDias = datos.Entero("plazo");
            proveedor.LimiteCredito = datos.Numero("limite");
            proveedor.Activo = datos.Check("activo");

            _compras.GuardarProveedor(proveedor);
            return true;
        }
    }
}
