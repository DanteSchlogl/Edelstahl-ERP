using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    /// <summary>
    /// Produccion: lineas pendientes de fabricar, tablero de ordenes y control de calidad.
    /// </summary>
    public class ModuloProduccion : ModuloBase
    {
        private readonly ProduccionService _produccion = new ProduccionService();

        public ModuloProduccion()
        {
            AgregarPestana("Manufacturing orders", CrearTablero(), Permiso.ProduccionVer);
            AgregarPestana("Pending to manufacture", CrearPorAceptar(), Permiso.ProduccionVer);
            AgregarPestana("Lines without order", CrearPendientes(), Permiso.ProduccionVer);
            CargarPestanaActual();
        }

        private Control CrearPorAceptar()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _produccion.ObtenerOrdenesPorAceptar());

            lista.Agregar("Accept and manufacture", fila =>
            {
                string orden = Estilo.Valor<string>(fila, "Orden");

                if (Estilo.Valor<Guid>(fila, "OrdenId") == Guid.Empty)
                {
                    Estilo.Avisar(this, Tr.T("Quotation {0} is accepted but is not an order yet. Go to Sales > Orders > Confirm order and register the deposit; the manufacturing order will then be ready to accept here.",
                        Estilo.Valor<string>(fila, "Presupuesto")));
                    return;
                }

                if (Estilo.Valor<string>(fila, "Situacion") == Tr.T("Waiting for the invoice that records the deposit"))
                {
                    // Primero se factura: la factura es la que deja asentada la sena abonada.
                    if (!Autorizacion.Tiene(Permiso.FacturasEmitir)
                        || !Estilo.Confirmar(this, Tr.T("The deposit is not invoiced yet. Issue the invoice now so the order can go to production?")))
                    {
                        return;
                    }

                    Guid pedidoId = Estilo.Valor<Guid>(fila, "PedidoId");
                    Pedido delPedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);
                    Cliente delCliente = FactoryDataAccess.ClienteRepository.GetById(delPedido.ClienteId);
                    TipoComprobante tipoFactura = delCliente != null && delCliente.Tipo == TipoPersona.Particular ? TipoComprobante.FacturaB : TipoComprobante.FacturaA;
                    Factura emitida = new FacturacionService().EmitirFactura(pedidoId, tipoFactura);
                    Estilo.Informar(this, Tr.T("Invoice {0} issued, showing the deposit paid.", emitida.Numero));
                }

                if (!Estilo.Confirmar(this, Tr.T("Accept order {0} and start manufacturing?", orden)))
                {
                    return;
                }

                _produccion.IniciarProduccion(Estilo.Valor<Guid>(fila, "OrdenId"));
                lista.Recargar();
                Estilo.Informar(this, Tr.T("Order {0} accepted. It is now in process; follow it in Manufacturing orders.", orden));
            }, true, Permiso.ProduccionGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearPendientes()
        {
            PanelLista lista = new PanelLista(() => _produccion.ObtenerPedidosPendientes());

            lista.Agregar("Create manufacturing order", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Create manufacturing order",
                    Campo.Combo("prioridad", "Priority", new List<Opcion>
                    {
                        new Opcion(Etiquetas.De(PrioridadOrden.Normal), PrioridadOrden.Normal),
                        new Opcion(Etiquetas.De(PrioridadOrden.Alta), PrioridadOrden.Alta),
                        new Opcion(Etiquetas.De(PrioridadOrden.Urgente), PrioridadOrden.Urgente)
                    }),
                    Campo.Fecha("fin", "Estimated finish", DateTime.Today.AddDays(7), true),
                    Campo.Texto("plano", "Technical drawing reference", "", false, 200),
                    Campo.Multilinea("obs", "Notes"));

                if (datos == null)
                {
                    return;
                }

                List<MaterialRequerido> materiales;

                using (EditorLineas editor = new EditorLineas(ModoLineas.Materiales, "Materials required",
                    Tr.T("Product to manufacture: {0} - {1}", Estilo.Valor<string>(fila, "Codigo"), Estilo.Valor<string>(fila, "Descripcion"))
                    + Environment.NewLine
                    + Tr.T("Quantity {0} - Customer {1} - Order {2}", Estilo.Valor<decimal>(fila, "Cantidad").ToString("N2"),
                        Estilo.Valor<string>(fila, "Cliente"), Estilo.Valor<string>(fila, "Pedido"))
                    + Environment.NewLine
                    + Tr.T("Choose the materials needed to make it. A suggestion is loaded; adjust it if needed."),
                    Estilo.Valor<decimal>(fila, "Cantidad")))
                {
                    if (editor.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    materiales = editor.Items
                        .Select(i => new MaterialRequerido { ProductoId = i.ProductoId.Value, Cantidad = i.Cantidad })
                        .ToList();
                }

                ResultadoGeneracionOrden resultado = _produccion.GenerarOrden(
                    Estilo.Valor<Guid>(fila, "PedidoId"),
                    Estilo.Valor<Guid>(fila, "DetallePedidoId"),
                    (PrioridadOrden)datos.Seleccion("prioridad"),
                    datos.Fecha("fin"),
                    materiales,
                    datos.Texto("plano"),
                    datos.Texto("obs"));

                string mensaje = Tr.T("Manufacturing order {0} created.", resultado.Orden.Numero);

                if (resultado.Faltantes != null && resultado.Faltantes.Count > 0)
                {
                    mensaje += Environment.NewLine + Tr.T("Missing materials: {0}",
                        string.Join(", ", resultado.Faltantes.Select(f => f.Material + " " + f.Faltante.ToString("N2"))));
                }

                Estilo.Informar(this, mensaje);

                if (Estilo.Confirmar(this, Tr.T("Manufacture order {0} now? Missing materials are added to the warehouse and quality is approved.", resultado.Orden.Numero)))
                {
                    new PreparacionService().FabricarCompleto(resultado.Orden.Id);
                    Estilo.Informar(this, Tr.T("Order {0} was manufactured and the finished product is in stock.", resultado.Orden.Numero));
                }

                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        /// <summary>
        /// Al terminar de fabricar, si el pedido todavia no tiene factura se ofrece emitirla en el momento
        /// para que Logistica pueda generar el remito.
        /// </summary>
        private void OfrecerFactura(Guid ordenId)
        {
            try
            {
                OrdenFabricacion orden = _produccion.ObtenerOrden(ordenId);

                if (orden == null || orden.Estado != EstadoProduccion.Finalizada || !orden.PedidoId.HasValue)
                {
                    return;
                }

                Guid pedidoId = orden.PedidoId.Value;

                if (new LogisticaService().ObtenerPedidosParaDespacho().Any(p => p.PedidoId == pedidoId && p.Facturado))
                {
                    return;
                }

                if (!Autorizacion.Tiene(Permiso.FacturasEmitir)
                    || !Estilo.Confirmar(this, Tr.T("The order was manufactured. Issue the invoice now so Logistics can ship it?")))
                {
                    return;
                }

                Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);
                Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(pedido.ClienteId);
                TipoComprobante tipo = cliente != null && cliente.Tipo == TipoPersona.Particular ? TipoComprobante.FacturaB : TipoComprobante.FacturaA;
                Factura factura = new FacturacionService().EmitirFactura(pedidoId, tipo);
                Estilo.Informar(this, Tr.T("Invoice {0} issued. The order is ready to coordinate the delivery.", factura.Numero));
            }
            catch (ReglaDeNegocioException ex)
            {
                Estilo.Avisar(this, ex.Message);
            }
        }

        private Control CrearTablero()
        {
            PanelLista lista = null;

            lista = new PanelLista(() => _produccion.ObtenerTablero(true));

            lista.ConDetalle(fila => _produccion.ObtenerMateriales(Estilo.IdDe(fila).Value),
                fila => Tr.T("Materials of {0} - {1}", Estilo.Valor<string>(fila, "Numero"), Estilo.Valor<string>(fila, "Producto")));

            Button reservar = lista.Agregar("Reserve materials", fila =>
            {
                List<FaltanteMaterialDto> faltantes = _produccion.ReservarMateriales(Estilo.IdDe(fila).Value);

                Estilo.Informar(this, faltantes.Count == 0
                    ? Tr.T("All materials were reserved.")
                    : Tr.T("Missing materials: {0}", string.Join(", ", faltantes.Select(f => f.Material + " " + f.Faltante.ToString("N2")))));
                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Add missing materials", fila =>
            {
                List<FaltanteMaterialDto> restantes = new PreparacionService().CompletarMateriales(Estilo.IdDe(fila).Value, true);

                Estilo.Informar(this, restantes.Count == 0
                    ? Tr.T("All materials were reserved.")
                    : Tr.T("Missing materials: {0}", string.Join(", ", restantes.Select(f => f.Material + " " + f.Faltante.ToString("N2")))));
                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Manufacture now", fila =>
            {
                if (!Estilo.Confirmar(this, Tr.T("Run the whole manufacturing cycle for this order? Missing materials are added and quality is approved.")))
                {
                    return;
                }

                new PreparacionService().FabricarCompleto(Estilo.IdDe(fila).Value);
                Estilo.Informar(this, Tr.T("The order was manufactured and the finished product is in stock."));
                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Start production", fila =>
            {
                _produccion.IniciarProduccion(Estilo.IdDe(fila).Value);
                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Record progress", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Record progress",
                    Campo.Texto("etapa", "Stage", "", true, 100),
                    Campo.Numero("porcentaje", "Progress %", 0m, 0m, 100m),
                    Campo.Multilinea("obs", "Notes"));

                if (datos != null)
                {
                    _produccion.RegistrarAvance(Estilo.IdDe(fila).Value, datos.Texto("etapa"), datos.Numero("porcentaje"), datos.Texto("obs"));
                    lista.Recargar();
                }
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Send to quality", fila =>
            {
                decimal avance = Estilo.Valor<decimal>(fila, "Avance");

                if (avance < 100m)
                {
                    // La orden no esta terminada: no puede ir a calidad. Se decide si se sigue fabricando o se cancela.
                    Respuesta decision = Dialogo.Pedir(this, "The order is not complete",
                        Tr.T("{0} is at {1}%. A product cannot go to quality control or enter the stock before manufacturing reaches 100%.",
                            Estilo.Valor<string>(fila, "Numero"), avance.ToString("0.##")),
                        Campo.Combo("accion", "What do you want to do?", new List<Opcion>
                        {
                            new Opcion(Tr.T("Continue manufacturing"), "seguir"),
                            new Opcion(Tr.T("Cancel the order (missing materials or products)"), "cancelar")
                        }),
                        Campo.Texto("motivo", "Reason for cancelling", "", false, 200));

                    if (decision == null)
                    {
                        return;
                    }

                    if ((string)decision.Seleccion("accion") == "seguir")
                    {
                        Estilo.Informar(this, Tr.T("The order stays in process. Use Register progress to keep loading the manufacturing progress."));
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(decision.Texto("motivo")))
                    {
                        Estilo.Avisar(this, Tr.T("Write the reason for cancelling the order."));
                        return;
                    }

                    _produccion.CancelarOrden(Estilo.IdDe(fila).Value, decision.Texto("motivo"));
                    Estilo.Informar(this, Tr.T("Order {0} cancelled. The reserved materials were released.", Estilo.Valor<string>(fila, "Numero")));
                    lista.Recargar();
                    return;
                }

                _produccion.EnviarACalidad(Estilo.IdDe(fila).Value);
                lista.Recargar();
            }, true, Permiso.ProduccionGestionar);

            lista.Agregar("Quality result", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Quality result",
                    Campo.Combo("resultado", "Result", new List<Opcion>
                    {
                        new Opcion(Etiquetas.De(ResultadoCalidad.Aprobado), ResultadoCalidad.Aprobado),
                        new Opcion(Etiquetas.De(ResultadoCalidad.Reproceso), ResultadoCalidad.Reproceso),
                        new Opcion(Etiquetas.De(ResultadoCalidad.Rechazado), ResultadoCalidad.Rechazado)
                    }),
                    Campo.Multilinea("obs", "Notes"));

                if (datos != null)
                {
                    Guid ordenId = Estilo.IdDe(fila).Value;
                    _produccion.RegistrarControlCalidad(ordenId, (ResultadoCalidad)datos.Seleccion("resultado"), datos.Texto("obs"));
                    lista.Recargar();

                    if ((ResultadoCalidad)datos.Seleccion("resultado") == ResultadoCalidad.Aprobado)
                    {
                        OfrecerFactura(ordenId);
                    }
                    else if ((ResultadoCalidad)datos.Seleccion("resultado") == ResultadoCalidad.Rechazado)
                    {
                        Estilo.Informar(this, Tr.T("The order was rejected and closed. Its reserved materials were released. Resolve the sales order or ask for a new manufacturing order."));
                    }
                }
            }, true, Permiso.CalidadRegistrar);

            lista.Agregar("History", fila =>
            {
                Guid ordenId = Estilo.IdDe(fila).Value;
                Form ventana = new Form
                {
                    Text = Tr.T("History") + " - " + Estilo.Valor<string>(fila, "Numero") + " / " + Estilo.Valor<string>(fila, "Pedido"),
                    Size = new System.Drawing.Size(980, 480),
                    StartPosition = FormStartPosition.CenterParent,
                    Font = Estilo.Normal
                };

                TabControl pestanas = new TabControl { Dock = DockStyle.Fill };
                TabPage resumen = new TabPage(Tr.T("Summary"));
                PanelLista pr = new PanelLista(() => _produccion.ObtenerResumenOrden(ordenId), false);
                resumen.Controls.Add(pr);
                TabPage avances = new TabPage(Tr.T("Progress"));
                TabPage controles = new TabPage(Tr.T("Quality controls"));
                PanelLista pa = new PanelLista(() => _produccion.ObtenerAvances(ordenId), false);
                PanelLista pc = new PanelLista(() => _produccion.ObtenerControles(ordenId), false);
                avances.Controls.Add(pa);
                controles.Controls.Add(pc);
                pestanas.TabPages.Add(resumen);
                pestanas.TabPages.Add(avances);
                pestanas.TabPages.Add(controles);
                ventana.Controls.Add(pestanas);
                ventana.Shown += (remitente, argumentos) => { pr.Recargar(); pa.Recargar(); pc.Recargar(); };
                ventana.ShowDialog(this);
                ventana.Dispose();
            }, true, Permiso.ProduccionVer);

            lista.Agregar("Cancel order", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Cancel order", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _produccion.CancelarOrden(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.ProduccionGestionar);

            lista.AgregarExportacion();
            return lista;
        }
    }
}
