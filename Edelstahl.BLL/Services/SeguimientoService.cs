using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Consulta del estado de un pedido en todas sus etapas: comercial,
    /// productiva, de facturación y logística.
    /// </summary>
    public class SeguimientoService
    {
        private readonly INegocioStore _almacen;

        public SeguimientoService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public SeguimientoService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        public List<PedidoSeguimientoDto> ObtenerSeguimiento(string texto, EstadoPedido? estado)
        {
            string buscado = (texto ?? string.Empty).Trim().ToLowerInvariant();
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            List<Pedido> pedidos = FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => (!estado.HasValue || p.Estado == estado.Value)
                    && (buscado.Length == 0
                        || p.Numero.ToLowerInvariant().Contains(buscado)
                        || Referencias.Nombre(clientes, p.ClienteId).ToLowerInvariant().Contains(buscado)))
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return _almacen.Ejecutar(unidad =>
            {
                List<OrdenFabricacion> ordenes = unidad.Conjunto<OrdenFabricacion>().ToList();
                List<Factura> facturas = unidad.Conjunto<Factura>().ToList();
                List<RemitoEntrega> remitos = unidad.Conjunto<RemitoEntrega>().ToList();

                return pedidos.Select(p => new PedidoSeguimientoDto
                {
                    PedidoId = p.Id,
                    Numero = p.Numero,
                    Cliente = Referencias.Nombre(clientes, p.ClienteId),
                    Fecha = p.FechaPedido,
                    Estado = Etiquetas.De(p.Estado),
                    EstadoClave = p.Estado,
                    Moneda = Etiquetas.De(p.Moneda),
                    Total = p.CalcularTotal(),
                    Produccion = ResumenProduccion(p, ordenes),
                    Facturacion = ResumenFacturacion(p, facturas),
                    Logistica = ResumenLogistica(p, remitos)
                }).ToList();
            });
        }

        private static string ResumenProduccion(Pedido pedido, List<OrdenFabricacion> ordenes)
        {
            int lineas = pedido.Detalles.Count(d => d.RequiereFabricacion);

            if (lineas == 0)
            {
                return Mensajes.T("Not required");
            }

            List<OrdenFabricacion> propias = ordenes
                .Where(o => o.PedidoId == pedido.Id && o.Estado != EstadoProduccion.Cancelada)
                .ToList();

            int terminadas = propias.Count(o => o.Estado == EstadoProduccion.Finalizada);

            if (propias.Count == 0)
            {
                return Mensajes.T("Pending: {0} line(s) without a manufacturing order", lineas);
            }

            return Mensajes.T("{0} of {1} finished", terminadas, lineas);
        }

        private static string ResumenFacturacion(Pedido pedido, List<Factura> facturas)
        {
            Factura factura = facturas.FirstOrDefault(
                f => f.PedidoId == pedido.Id && f.EstadoPago != EstadoPago.Anulada);

            if (factura == null)
            {
                return Mensajes.T("Not invoiced");
            }

            return factura.Numero + " - " + Etiquetas.De(FacturacionService.EstadoVisible(factura));
        }

        private static string ResumenLogistica(Pedido pedido, List<RemitoEntrega> remitos)
        {
            List<RemitoEntrega> propios = remitos
                .Where(r => r.PedidoId == pedido.Id && r.Estado != EstadoRemito.Cancelado)
                .ToList();

            if (propios.Count == 0)
            {
                return Mensajes.T("No delivery notes yet");
            }

            int entregados = propios.Count(r => r.Estado == EstadoRemito.Entregado);

            return Mensajes.T("{0} delivery note(s), {1} delivered", propios.Count, entregados);
        }

        /// <summary>
        /// Reúne en orden cronológico todo lo que le pasó al pedido.
        /// </summary>
        public List<EventoSeguimientoDto> ObtenerLineaDeTiempo(Guid pedidoId)
        {
            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                return new List<EventoSeguimientoDto>();
            }

            List<EventoSeguimientoDto> eventos = new List<EventoSeguimientoDto>();

            Presupuesto origen = FactoryDataAccess.PresupuestoRepository.GetById(pedido.PresupuestoId);

            if (origen != null)
            {
                Agregar(eventos, origen.FechaEmision.AddSeconds(-2), "Quotation", Mensajes.T("Quotation {0} issued", origen.Numero));
                Agregar(eventos, pedido.FechaPedido.AddSeconds(-1), "Quotation", Mensajes.T("Quotation {0} accepted by the customer", origen.Numero));
            }

            Agregar(eventos, pedido.FechaPedido, "Order", Mensajes.T("Order {0} confirmed", pedido.Numero));

            if (pedido.FechaAnticipo.HasValue && pedido.ImporteAnticipo > 0m)
            {
                Agregar(eventos, SinHoraDespuesDe(pedido.FechaAnticipo.Value, pedido.FechaPedido, 1), "Order",
                    Mensajes.T("Deposit of {0} registered", pedido.ImporteAnticipo.ToString("N2")));
            }

            _almacen.Ejecutar(unidad =>
            {
                foreach (OrdenFabricacion orden in unidad.Conjunto<OrdenFabricacion>()
                    .Incluir("Controles").ToList().Where(o => o.PedidoId == pedidoId))
                {
                    Agregar(eventos, orden.FechaCreacion, "Production",
                        Mensajes.T("Manufacturing order {0} created", orden.Numero));

                    foreach (AvanceProduccion avance in unidad.Conjunto<AvanceProduccion>().ToList().Where(a => a.OrdenFabricacionId == orden.Id))
                    {
                        Agregar(eventos, avance.Fecha, "Production",
                            Mensajes.T("{0}: progress {1}% ({2})", orden.Numero, avance.PorcentajeAvance.ToString("0.##"), avance.Etapa));
                    }

                    if (orden.FechaInicio.HasValue)
                    {
                        Agregar(eventos, orden.FechaInicio.Value, "Production", Mensajes.T("{0} started", orden.Numero));
                    }

                    foreach (ControlCalidad control in orden.Controles)
                    {
                        Agregar(eventos, control.FechaControl, "Production",
                            Mensajes.T("{0}: quality control {1}", orden.Numero, Etiquetas.De(control.Resultado)));
                    }

                    if (orden.FechaFinReal.HasValue)
                    {
                        Agregar(eventos, orden.FechaFinReal.Value, "Production", Mensajes.T("{0} finished", orden.Numero));
                    }
                }

                HashSet<Guid> lineas = new HashSet<Guid>(pedido.Detalles.Select(d => d.Id));

                foreach (ReservaStock reserva in unidad.Conjunto<ReservaStock>().ToList()
                    .Where(r => r.DetallePedidoId.HasValue && lineas.Contains(r.DetallePedidoId.Value)))
                {
                    Producto producto = unidad.Conjunto<Producto>().ToList().FirstOrDefault(p => p.Id == reserva.ProductoId);
                    Agregar(eventos, reserva.FechaReserva, "Warehouse",
                        Mensajes.T("Stock reserved: {0} x {1}", producto == null ? string.Empty : producto.CodigoSku, reserva.CantidadReservada.ToString("0.##")));
                }

                foreach (Factura factura in unidad.Conjunto<Factura>().ToList().Where(f => f.PedidoId == pedidoId))
                {
                    Agregar(eventos, factura.FechaEmision, "Invoicing",
                        Mensajes.T("Invoice {0} issued for {1}", factura.Numero, factura.MontoTotal.ToString("N2")));

                    foreach (CobroCliente cobro in unidad.Conjunto<CobroCliente>().ToList().Where(c => c.FacturaId == factura.Id))
                    {
                        Agregar(eventos, SinHoraDespuesDe(cobro.FechaCobro, factura.FechaEmision, 1), "Invoicing",
                            Mensajes.T("Collection {0} of {1}", cobro.Numero, cobro.Importe.ToString("N2")));
                    }
                }

                foreach (RemitoEntrega remito in unidad.Conjunto<RemitoEntrega>().ToList().Where(r => r.PedidoId == pedidoId))
                {
                    Agregar(eventos, remito.FechaEmision, "Logistics", Mensajes.T("Delivery note {0} issued", remito.Numero));

                    foreach (DetalleHojaRuta parada in unidad.Conjunto<DetalleHojaRuta>().ToList().Where(d => d.RemitoId == remito.Id))
                    {
                        HojaRuta hoja = unidad.Conjunto<HojaRuta>().ToList().FirstOrDefault(h => h.Id == parada.HojaRutaId);

                        if (hoja != null)
                        {
                            Agregar(eventos, remito.FechaEmision.AddSeconds(1), "Logistics",
                                Mensajes.T("{0} assigned to route sheet {1}", remito.Numero, hoja.Numero));

                            if (hoja.FechaInicio.HasValue)
                            {
                                Agregar(eventos, hoja.FechaInicio.Value, "Logistics", Mensajes.T("Route sheet {0} started", hoja.Numero));
                            }
                        }
                    }

                    if (remito.FechaDespacho.HasValue)
                    {
                        Agregar(eventos, remito.FechaDespacho.Value, "Logistics", Mensajes.T("{0} dispatched", remito.Numero));
                    }

                    if (remito.FechaEntrega.HasValue)
                    {
                        Agregar(eventos, remito.FechaEntrega.Value, "Logistics",
                            Mensajes.T("{0} delivered to {1}", remito.Numero, remito.ReceptorNombre));
                    }
                }

                foreach (Devolucion devolucion in unidad.Conjunto<Devolucion>().ToList().Where(d => d.PedidoId == pedidoId))
                {
                    Agregar(eventos, devolucion.FechaSolicitud, "Returns", Mensajes.T("Return {0} requested", devolucion.Numero));

                    if (devolucion.FechaRecepcion.HasValue)
                    {
                        Agregar(eventos, devolucion.FechaRecepcion.Value, "Returns", Mensajes.T("{0} received", devolucion.Numero));
                    }
                }

                foreach (NotaCredito nota in unidad.Conjunto<NotaCredito>().ToList()
                    .Where(n => unidad.Conjunto<Factura>().Any(f => f.Id == n.FacturaId && f.PedidoId == pedidoId)))
                {
                    Agregar(eventos, nota.Fecha, "Invoicing",
                        Mensajes.T("Credit note {0} for {1}", nota.Numero, nota.MontoTotal.ToString("N2")));
                }
            });

            return eventos.OrderBy(e => e.Fecha).ToList();
        }

        /// <summary>
        /// Las fechas que se cargan sin hora (0:00) se ubican justo despues del hecho que las origina,
        /// para que el historial siga el orden real de los pasos.
        /// </summary>
        private static DateTime SinHoraDespuesDe(DateTime fecha, DateTime referencia, int segundos)
        {
            if (fecha.TimeOfDay != TimeSpan.Zero)
            {
                return fecha;
            }

            return fecha.Date <= referencia.Date ? referencia.AddSeconds(segundos) : fecha.AddHours(12);
        }

        private static void Agregar(List<EventoSeguimientoDto> eventos, DateTime fecha, string etapa, string detalle)
        {
            eventos.Add(new EventoSeguimientoDto { Fecha = fecha, Etapa = Mensajes.T(etapa), Detalle = detalle });
        }
    }
}
