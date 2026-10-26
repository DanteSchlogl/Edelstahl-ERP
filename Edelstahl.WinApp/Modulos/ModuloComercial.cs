using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Documents;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    /// <summary>
    /// Ventas: clientes, presupuestos y pedidos con su seguimiento.
    /// </summary>
    public class ModuloComercial : ModuloBase
    {
        private readonly ClienteService _clientes = new ClienteService();
        private readonly PresupuestoService _presupuestos = new PresupuestoService();
        private readonly PedidoService _pedidos = new PedidoService();
        private readonly SeguimientoService _seguimiento = new SeguimientoService();
        private readonly FacturacionService _facturacion = new FacturacionService();
        private readonly DocumentosService _documentos = new DocumentosService();

        public ModuloComercial()
        {
            AgregarPestana("Orders", CrearPedidos(), Permiso.PedidosVer);
            AgregarPestana("Quotations", CrearPresupuestos(), Permiso.PresupuestosGestionar);
            AgregarPestana("Customers", CrearClientes(), Permiso.ClientesVer);
            CargarPestanaActual();
        }

        // ---------------------------------------------------------------
        // Clientes
        // ---------------------------------------------------------------

        private Control CrearClientes()
        {
            PanelLista lista = new PanelLista(() => _clientes.ObtenerTodos()
                .OrderBy(c => c.RazonSocial).Select(ClienteFila.De).ToList());

            lista.Agregar("New customer", fila =>
            {
                Respuesta datos = PedirCliente(null);

                if (datos == null)
                {
                    return;
                }

                Cliente cliente = new Cliente { Id = Guid.NewGuid(), Activo = true, FechaAlta = DateTime.Now };
                Aplicar(cliente, datos);
                _clientes.Registrar(cliente);
                lista.Recargar();
            }, false, Permiso.ClientesEditar);

            lista.Agregar("Edit", fila =>
            {
                Cliente cliente = _clientes.ObtenerPorId(Estilo.IdDe(fila).Value);
                Respuesta datos = PedirCliente(cliente);

                if (datos == null)
                {
                    return;
                }

                Aplicar(cliente, datos);
                _clientes.Modificar(cliente);
                lista.Recargar();
            }, true, Permiso.ClientesEditar);

            lista.Agregar("Authorize / revoke credit", fila =>
            {
                Cliente cliente = _clientes.ObtenerPorId(Estilo.IdDe(fila).Value);
                bool autorizar = !cliente.AutorizadoACredito;

                string pregunta = autorizar
                    ? Tr.T("Authorize {0} to receive goods without paying 100% beforehand (credit account)?", cliente.RazonSocial)
                    : Tr.T("Revoke the credit authorization of {0}? From now on they must pay 100% before delivery.", cliente.RazonSocial);

                if (!Estilo.Confirmar(this, pregunta))
                {
                    return;
                }

                cliente.AutorizadoACredito = autorizar;
                _clientes.Modificar(cliente);
                new BitacoraService().RegistrarAuditoria("Ventas", autorizar ? "AutorizarCredito" : "RevocarCredito", "Cliente", cliente.Id, cliente.RazonSocial);
                lista.Recargar();
            }, true, Permiso.ClientesEditar);

            lista.Agregar("Delete", fila =>
            {
                if (Estilo.Confirmar(this, Tr.T("Delete the selected customer?")))
                {
                    _clientes.Eliminar(Estilo.IdDe(fila).Value);
                    lista.Recargar();
                }
            }, true, Permiso.ClientesEditar);

            lista.Agregar("Account statement", fila => MostrarCuenta(Estilo.IdDe(fila).Value, Estilo.Valor<string>(fila, "RazonSocial")),
                true, Permiso.CuentaCorrienteVer);

            lista.AgregarExportacion();
            return lista;
        }

        private Respuesta PedirCliente(Cliente existente)
        {
            bool nuevo = existente == null;

            return Dialogo.Pedir(this, nuevo ? "New customer" : "Edit customer",
                Campo.Combo("tipo", "Customer type", new List<Opcion>
                {
                    new Opcion(Tr.T("Company (CUIT)"), TipoPersona.Empresa),
                    new Opcion(Tr.T("Individual (CUIL)"), TipoPersona.Particular)
                }).Con(nuevo ? (object)TipoPersona.Empresa : existente.Tipo),
                Campo.Texto("cuit", "Tax ID (CUIT / CUIL)", nuevo ? "" : existente.CUIT, true, 13),
                Campo.Texto("razon", "Business name / full name", nuevo ? "" : existente.RazonSocial, true, 150),
                Campo.Texto("dirFact", "Billing address", nuevo ? "" : existente.DireccionFacturacion, true, 200),
                Campo.Texto("dirEnt", "Delivery address", nuevo ? "" : existente.DireccionEntrega, false, 200),
                Campo.Texto("localidad", "City", nuevo ? "" : existente.Localidad, false, 100),
                Campo.Texto("provincia", "Province", nuevo ? "" : existente.Provincia, false, 100),
                Campo.Texto("cp", "Postal code", nuevo ? "" : existente.CodigoPostal, false, 10),
                Campo.Texto("email", "Email", nuevo ? "" : existente.Email, false, 100),
                Campo.Texto("telefono", "Phone", nuevo ? "" : existente.Telefono, false, 40),
                Campo.Numero("limite", "Credit limit", nuevo ? 0m : existente.LimiteCredito));
        }

        private static void Aplicar(Cliente cliente, Respuesta datos)
        {
            cliente.Tipo = (TipoPersona)datos.Seleccion("tipo");
            cliente.CUIT = datos.Texto("cuit");
            cliente.RazonSocial = datos.Texto("razon");
            cliente.DireccionFacturacion = datos.Texto("dirFact");
            cliente.DireccionEntrega = string.IsNullOrWhiteSpace(datos.Texto("dirEnt")) ? datos.Texto("dirFact") : datos.Texto("dirEnt");
            cliente.Localidad = datos.Texto("localidad");
            cliente.Provincia = datos.Texto("provincia");
            cliente.CodigoPostal = datos.Texto("cp");
            cliente.Email = datos.Texto("email");
            cliente.Telefono = datos.Texto("telefono");
            cliente.LimiteCredito = datos.Numero("limite");
        }

        private void MostrarCuenta(Guid clienteId, string nombre)
        {
            FinanzasService finanzas = new FinanzasService();
            ResumenCuentaClienteDto resumen = finanzas.ObtenerResumenCliente(clienteId);

            Form ventana = new Form
            {
                Text = Tr.T("Account statement") + " - " + nombre,
                Size = new System.Drawing.Size(860, 480),
                StartPosition = FormStartPosition.CenterParent,
                Font = Estilo.Normal
            };

            Label cabecera = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(10, 8, 0, 0),
                Font = Estilo.Negrita,
                Text = Tr.T("Credit limit {0}   Debt {1}   Available {2}",
                    resumen.LimiteCredito.ToString("N2"), resumen.DeudaActual.ToString("N2"), resumen.CreditoDisponible.ToString("N2"))
            };

            PanelLista movimientos = new PanelLista(() => finanzas.ObtenerCuentaCliente(clienteId, null, null), false);
            movimientos.AgregarExportacion();
            ventana.Controls.Add(movimientos);
            ventana.Controls.Add(cabecera);
            cabecera.BringToFront();
            movimientos.BringToFront();
            ventana.Shown += (remitente, argumentos) => movimientos.Recargar();
            ventana.ShowDialog(this);
            ventana.Dispose();
        }

        // ---------------------------------------------------------------
        // Presupuestos
        // ---------------------------------------------------------------

        private Control CrearPresupuestos()
        {
            PanelLista lista = new PanelLista(() => _presupuestos.ObtenerResumenes(null, null)
                .OrderByDescending(p => p.Emision).ToList());

            lista.ConDetalle(fila => _presupuestos.ObtenerDetalles(Estilo.IdDe(fila).Value)
                .Select(d => new DetallePresupuestoDto
                {
                    Codigo = d.Codigo,
                    Descripcion = d.Descripcion,
                    Tipo = Etiquetas.De(d.TipoItem),
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.CalcularSubtotal()
                }).ToList());

            lista.Agregar("New quotation", fila =>
            {
                NuevoPresupuesto();
                lista.Recargar();
            }, false, Permiso.PresupuestosGestionar);

            lista.Agregar("Mark as sent", fila => Cambiar(lista, fila, EstadoPresupuesto.Enviado), true, Permiso.PresupuestosGestionar);
            lista.Agregar("Accepted", fila => Cambiar(lista, fila, EstadoPresupuesto.Aceptado), true, Permiso.PresupuestosGestionar);
            lista.Agregar("Rejected", fila => Cambiar(lista, fila, EstadoPresupuesto.Rechazado), true, Permiso.PresupuestosGestionar);

            lista.Agregar("Extend validity", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Extend validity", Campo.Entero("dias", "Additional days", 7, 1, 60));

                if (datos != null)
                {
                    _presupuestos.ExtenderVigencia(Estilo.IdDe(fila).Value, datos.Entero("dias"));
                    lista.Recargar();
                }
            }, true, Permiso.PresupuestosGestionar);

            lista.Agregar("Print quotation", fila =>
            {
                Presupuesto presupuesto = _presupuestos.ObtenerPorId(Estilo.IdDe(fila).Value);
                Cliente cliente = _clientes.ObtenerPorId(presupuesto.ClienteId);
                Estilo.AbrirArchivo(this, new PresupuestoPdfService().Generar(presupuesto, cliente));
            }, true, Permiso.PresupuestosGestionar);

            lista.Agregar("Update expired", fila =>
            {
                int cantidad = _presupuestos.ActualizarVencidos();
                Estilo.Informar(this, Tr.T("{0} quotation(s) marked as expired.", cantidad));
                lista.Recargar();
            }, false, Permiso.PresupuestosGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private void Cambiar(PanelLista lista, object fila, EstadoPresupuesto estado)
        {
            _presupuestos.CambiarEstado(Estilo.IdDe(fila).Value, estado);
            lista.Recargar();

            if (estado == EstadoPresupuesto.Aceptado)
            {
                Estilo.Informar(this, Tr.T("Quotation accepted. You can now confirm the order from Orders > Confirm order."));
            }
        }

        private void NuevoPresupuesto()
        {
            List<Opcion> clientes = _clientes.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.RazonSocial)
                .Select(c => new Opcion(c.RazonSocial + " (" + c.EtiquetaDocumento + " " + c.CUIT + ")", c.Id)).ToList();

            List<Opcion> monedas = new List<Opcion>
            {
                new Opcion(Etiquetas.De(Moneda.PesosArgentinos), Moneda.PesosArgentinos),
                new Opcion(Etiquetas.De(Moneda.DolaresEstadounidenses), Moneda.DolaresEstadounidenses)
            };

            Respuesta datos = Dialogo.Pedir(this, "New quotation",
                Campo.Combo("cliente", "Customer", clientes),
                Campo.Combo("moneda", "Currency", monedas),
                Campo.Numero("cambio", "Exchange rate", 1m, 0.01m, 100000m),
                Campo.Numero("iva", "VAT %", 21m, 0m, 100m),
                Campo.Numero("descuento", "General discount %", 0m, 0m, 100m),
                Campo.Numero("anticipo", "Deposit %", 0m, 0m, 100m),
                Campo.Entero("vigencia", "Validity (days)", 7, 1, 60),
                Campo.Texto("pago", "Payment terms", Tr.T("Cash on delivery"), false, 100),
                Campo.Texto("plazo", "Delivery time", Tr.T("7 days"), false, 100),
                Campo.Check("entrega", "Delivery included", true),
                Campo.Multilinea("obs", "Notes"),
                Campo.Check("enviar", "Mark as sent", false));

            if (datos == null)
            {
                return;
            }

            // Si algo falla al guardar (por ejemplo la sena), se vuelve al editor con las lineas ya cargadas.
            List<ItemLinea> previos = new List<ItemLinea>();
            decimal anticipo = datos.Numero("anticipo");
            Presupuesto creado = null;

            while (creado == null)
            {
                ItemLinea[] items;

                using (EditorLineas editor = new EditorLineas(ModoLineas.Presupuesto, "Quotation lines", null, 0m, clientes.Where(o => o.Valor.Equals(datos.GuidDe("cliente"))).Select(o => o.Texto).FirstOrDefault()))
                {
                    editor.PonerItems(previos);

                    if (editor.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    items = editor.Items.ToArray();
                }

                previos = items.ToList();

                // La fabricacion especial exige una sena minima: se pide aca, sin perder lo cargado.
                if (items.Any(i => i.Tipo == (int)TipoItemPresupuesto.FabricacionEspecial) && anticipo < 30m)
                {
                    Respuesta sena = Dialogo.Pedir(this, "Deposit",
                        "This quotation includes custom manufacturing, which requires a deposit of at least 30%.",
                        Campo.Numero("anticipo", "Deposit %", 30m, 30m, 100m));

                    if (sena == null)
                    {
                        continue;
                    }

                    anticipo = sena.Numero("anticipo");
                }

                PresupuestoSolicitud solicitud = new PresupuestoSolicitud
                {
                    ClienteId = datos.GuidDe("cliente"),
                    Moneda = (Moneda)datos.Seleccion("moneda"),
                    TipoCambio = datos.Numero("cambio"),
                    PorcentajeIva = datos.Numero("iva"),
                    PorcentajeDescuentoGeneral = datos.Numero("descuento"),
                    PorcentajeAnticipo = anticipo,
                    VigenciaDias = datos.Entero("vigencia"),
                    CondicionPago = datos.Texto("pago"),
                    PlazoEntrega = datos.Texto("plazo"),
                    EntregaIncluida = datos.Check("entrega"),
                    Observaciones = datos.Texto("obs"),
                    Enviar = datos.Check("enviar"),
                    Lineas = items.Select(i => new LineaPresupuestoSolicitud
                    {
                        ProductoId = i.ProductoId,
                        Codigo = i.Codigo,
                        Descripcion = i.Descripcion,
                        DescripcionTecnica = i.Tipo == (int)TipoItemPresupuesto.FabricacionEspecial ? i.Descripcion : string.Empty,
                        Tipo = (TipoItemPresupuesto)i.Tipo,
                        Cantidad = i.Cantidad,
                        PrecioUnitario = i.Precio,
                        Descuento = i.Descuento
                    }).ToList()
                };

                try
                {
                    creado = _presupuestos.CrearPresupuesto(solicitud);
                }
                catch (ReglaDeNegocioException ex)
                {
                    Estilo.Avisar(this, ex.Message);
                }
            }

            Estilo.Informar(this, Tr.T("Quotation {0} created.", creado.Numero));
        }

        // ---------------------------------------------------------------
        // Pedidos y seguimiento
        // ---------------------------------------------------------------

        private Control CrearPedidos()
        {
            PanelLista lista = new PanelLista(() => _seguimiento.ObtenerSeguimiento(string.Empty, null));

            lista.ConDetalle(fila => _seguimiento.ObtenerLineaDeTiempo(Estilo.IdDe(new IdPedido(fila)).Value));

            lista.Agregar("Confirm order", fila =>
            {
                ConfirmarPedido();
                lista.Recargar();
            }, false, Permiso.PedidosConfirmar);

            lista.Agregar("Issue invoice", fila =>
            {
                Guid pedidoId = Estilo.Valor<Guid>(fila, "PedidoId");
                Respuesta datos = Dialogo.Pedir(this, "Issue invoice",
                    "Choose the invoice type. Type A is for VAT registered customers; type B for final consumers.",
                    Campo.Combo("tipo", "Invoice type", new List<Opcion>
                    {
                        new Opcion(Tr.T("Invoice A"), TipoComprobante.FacturaA),
                        new Opcion(Tr.T("Invoice B"), TipoComprobante.FacturaB)
                    }));

                if (datos == null)
                {
                    return;
                }

                Factura factura = _facturacion.EmitirFactura(pedidoId, (TipoComprobante)datos.Seleccion("tipo"));
                string ruta = _documentos.GenerarFactura(factura.Id);
                lista.Recargar();
                Estilo.AbrirArchivo(this, ruta);
            }, true, Permiso.FacturasEmitir);

            lista.Agregar("Print invoice", fila =>
            {
                Factura factura = _facturacion.ObtenerFacturaDePedido(Estilo.Valor<Guid>(fila, "PedidoId"));

                if (factura == null)
                {
                    Estilo.Avisar(this, Tr.T("This order has no invoice yet."));
                    return;
                }

                Estilo.AbrirArchivo(this, _documentos.GenerarFactura(factura.Id));
            }, true, Permiso.PedidosVer);

            lista.Agregar("Order voucher PDF", fila =>
            {
                Pedido pedido = _pedidos.ObtenerPorId(Estilo.Valor<Guid>(fila, "PedidoId"));
                Cliente cliente = _clientes.ObtenerPorId(pedido.ClienteId);
                Estilo.AbrirArchivo(this, new ComprobantePedidoPdfService().Generar(pedido, cliente));
            }, true, Permiso.PedidosVer);

            lista.Agregar("Cancel order", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Cancel order", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _pedidos.Cancelar(Estilo.Valor<Guid>(fila, "PedidoId"), datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.PedidosConfirmar);

            lista.AgregarExportacion();
            return lista;
        }

        private void ConfirmarPedido()
        {
            List<Opcion> clientes = _clientes.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.RazonSocial)
                .Select(c => new Opcion(c.RazonSocial + " (" + c.EtiquetaDocumento + " " + c.CUIT + ")", c.Id)).ToList();

            Respuesta paso1 = Dialogo.Pedir(this, "Confirm order - customer",
                "Select the customer. Only accepted quotations that are still valid can be confirmed.",
                Campo.Combo("cliente", "Customer", clientes));

            if (paso1 == null)
            {
                return;
            }

            Guid clienteId = paso1.GuidDe("cliente");

            List<PresupuestoResumenDto> disponibles = _presupuestos.ObtenerResumenes(clienteId, null)
                .Where(p => p.EstadoClave == EstadoPresupuesto.Aceptado)
                .Where(p => p.Vencimiento.Date >= DateTime.Today)
                .OrderByDescending(p => p.Emision).ToList();

            if (disponibles.Count == 0)
            {
                string vencidos = string.Join(", ", _presupuestos.ObtenerResumenes(clienteId, null)
                    .Where(p => p.EstadoClave == EstadoPresupuesto.Vencido).OrderByDescending(p => p.Emision).Take(5).Select(p => p.Numero));

                Estilo.Avisar(this, Tr.T("This customer has no accepted quotations to confirm. Mark the quotation as Accepted first.")
                    + (vencidos.Length == 0 ? string.Empty : "\n\n" + Tr.T("Expired quotations: {0}. Use \"Extend validity\" to renew them.", vencidos)));
                return;
            }

            List<Opcion> opcionesPresupuesto = disponibles
                .Select(p => new Opcion(p.Numero + "  " + p.Total.ToString("N2") + "  (" + Tr.T("deposit") + " " + p.Anticipo.ToString("N2") + ")", p))
                .ToList();

            List<Opcion> medios = new FinanzasService().ObtenerMediosPago(true)
                .Select(m => new Opcion(m.Nombre, m.Nombre)).ToList();

            Respuesta paso2 = Dialogo.Pedir(this, "Confirm order - deposit",
                "Enter the deposit the customer actually paid. It cannot be below the minimum shown next to the quotation.",
                Campo.Combo("presupuesto", "Quotation", opcionesPresupuesto),
                Campo.Numero("anticipo", "Deposit received", 0m),
                Campo.Combo("medio", "Payment method", medios, false),
                Campo.Texto("comprobante", "Receipt reference", "REC-" + DateTime.Now.ToString("yyyyMMdd-HHmm"), false, 60),
                Campo.Fecha("fecha", "Deposit date", DateTime.Today));

            if (paso2 == null)
            {
                return;
            }

            PresupuestoResumenDto elegido = (PresupuestoResumenDto)paso2.Seleccion("presupuesto");

            // Si falta mercaderia se muestra la lista en rojo y se puede ingresar ahi mismo.
            List<FaltanteDto> faltantes = new FaltantesService().DePresupuesto(elegido.Id);

            if (faltantes.Count > 0)
            {
                using (FrmFaltantes ventana = new FrmFaltantes("Missing products",
                    "Some products are not in stock. Add the quantities below to confirm the order.",
                    faltantes, Autorizacion.Tiene(Permiso.InventarioMover)))
                {
                    if (ventana.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    PreparacionService preparacion = new PreparacionService();

                    foreach (KeyValuePair<Guid, decimal> ingreso in ventana.Ingresos)
                    {
                        preparacion.IngresarStock(ingreso.Key, ingreso.Value, Tr.T("Stock entry for quotation {0}", elegido.Numero));
                    }
                }
            }

            Pedido pedido = _pedidos.ConfirmarDesdePresupuesto(clienteId, elegido.Id, paso2.Numero("anticipo"),
                Convert.ToString(paso2.Seleccion("medio")), paso2.Texto("comprobante"), paso2.Fecha("fecha"));

            string ordenes = string.Empty;

            try
            {
                List<string> creadas = new ProduccionService().GenerarOrdenesDelPedido(pedido.Id);

                if (creadas.Count > 0)
                {
                    ordenes = Environment.NewLine + Tr.T("Manufacturing orders created: {0}", string.Join(", ", creadas));
                }
            }
            catch (Exception)
            {
                // Sin permiso de produccion o sin materiales: las ordenes se generan luego desde Produccion.
            }

            Estilo.Informar(this, Tr.T("Order {0} confirmed.", pedido.Numero) + ordenes);
        }

        // Adaptador para reutilizar IdDe con filas que exponen PedidoId.
        private sealed class IdPedido
        {
            public IdPedido(object fila)
            {
                Id = Estilo.Valor<Guid>(fila, "PedidoId");
            }

            public Guid Id { get; private set; }
        }
    }
}
