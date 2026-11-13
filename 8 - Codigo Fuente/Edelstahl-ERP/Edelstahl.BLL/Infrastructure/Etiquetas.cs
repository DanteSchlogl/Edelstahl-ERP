using System;
using System.Collections.Generic;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Texto legible (y traducible) de los estados y tipos del sistema.
    /// La clave de traducción es el texto en inglés.
    /// </summary>
    public static class Etiquetas
    {
        private static readonly Dictionary<string, string> Textos = new Dictionary<string, string>
        {
            { "TipoProducto.MateriaPrima", "Raw material" },
            { "TipoProducto.InsumoConsumible", "Consumable supply" },
            { "TipoProducto.Componente", "Component" },
            { "TipoProducto.Semielaborado", "Semi-finished" },
            { "TipoProducto.ProductoTerminado", "Finished product" },

            { "TipoMovimientoStock.RecepcionCompra", "Purchase receipt" },
            { "TipoMovimientoStock.ProductoTerminado", "Finished product in" },
            { "TipoMovimientoStock.DevolucionCliente", "Customer return" },
            { "TipoMovimientoStock.AjustePositivo", "Positive adjustment" },
            { "TipoMovimientoStock.TransferenciaEntrada", "Transfer in" },
            { "TipoMovimientoStock.ConsumoProduccion", "Production consumption" },
            { "TipoMovimientoStock.MermaProduccion", "Production waste" },
            { "TipoMovimientoStock.Despacho", "Dispatch" },
            { "TipoMovimientoStock.AjusteNegativo", "Negative adjustment" },
            { "TipoMovimientoStock.TransferenciaSalida", "Transfer out" },
            { "TipoMovimientoStock.ReservaPedido", "Order reservation" },
            { "TipoMovimientoStock.LiberacionReserva", "Reservation release" },

            { "EstadoReserva.Activa", "Active" },
            { "EstadoReserva.Consumida", "Consumed" },
            { "EstadoReserva.Liberada", "Released" },
            { "EstadoReserva.Cancelada", "Cancelled" },

            { "EstadoProduccion.Pendiente", "Pending" },
            { "EstadoProduccion.EnProceso", "In process" },
            { "EstadoProduccion.ControlCalidad", "Quality control" },
            { "EstadoProduccion.Finalizada", "Finished" },
            { "EstadoProduccion.Cancelada", "Cancelled" },

            { "PrioridadOrden.Normal", "Normal" },
            { "PrioridadOrden.Alta", "High" },
            { "PrioridadOrden.Urgente", "Urgent" },

            { "ResultadoCalidad.Aprobado", "Approved" },
            { "ResultadoCalidad.Rechazado", "Rejected" },
            { "ResultadoCalidad.Reproceso", "Rework" },

            { "EstadoOrdenCompra.Borrador", "Draft" },
            { "EstadoOrdenCompra.Emitida", "Issued" },
            { "EstadoOrdenCompra.ParcialmenteRecibida", "Partially received" },
            { "EstadoOrdenCompra.Recibida", "Received" },
            { "EstadoOrdenCompra.Cancelada", "Cancelled" },

            { "TipoComprobante.FacturaA", "Invoice A" },
            { "TipoComprobante.FacturaB", "Invoice B" },

            { "EstadoPago.Pendiente", "Pending" },
            { "EstadoPago.Parcial", "Partial" },
            { "EstadoPago.Pagada", "Paid" },
            { "EstadoPago.Vencida", "Overdue" },
            { "EstadoPago.Anulada", "Void" },

            { "TipoMovimientoCuenta.Factura", "Invoice" },
            { "TipoMovimientoCuenta.Cobro", "Collection" },
            { "TipoMovimientoCuenta.NotaCredito", "Credit note" },
            { "TipoMovimientoCuenta.NotaDebito", "Debit note" },
            { "TipoMovimientoCuenta.Ajuste", "Adjustment" },
            { "TipoMovimientoCuenta.OrdenCompra", "Purchase order" },
            { "TipoMovimientoCuenta.Pago", "Payment" },

            { "EstadoRemito.Pendiente", "Pending" },
            { "EstadoRemito.Asignado", "Assigned" },
            { "EstadoRemito.EnTransito", "In transit" },
            { "EstadoRemito.Entregado", "Delivered" },
            { "EstadoRemito.Rechazado", "Rejected" },
            { "EstadoRemito.Reprogramado", "Rescheduled" },
            { "EstadoRemito.Cancelado", "Cancelled" },

            { "EstadoHojaRuta.Planificada", "Planned" },
            { "EstadoHojaRuta.EnCurso", "In progress" },
            { "EstadoHojaRuta.Finalizada", "Finished" },
            { "EstadoHojaRuta.Cancelada", "Cancelled" },

            { "ResultadoEntrega.EntregadoConforme", "Delivered in good order" },
            { "ResultadoEntrega.EntregaParcial", "Partial delivery" },
            { "ResultadoEntrega.Rechazado", "Rejected" },
            { "ResultadoEntrega.Ausente", "Recipient absent" },
            { "ResultadoEntrega.DomicilioIncorrecto", "Wrong address" },

            { "EstadoDevolucion.Solicitada", "Requested" },
            { "EstadoDevolucion.EnTransito", "In transit" },
            { "EstadoDevolucion.Recibida", "Received" },
            { "EstadoDevolucion.EnInspeccion", "Under inspection" },
            { "EstadoDevolucion.Resuelta", "Resolved" },
            { "EstadoDevolucion.Rechazada", "Rejected" },
            { "EstadoDevolucion.Cancelada", "Cancelled" },

            { "AccionResuelta.SinAccion", "No action" },
            { "AccionResuelta.ReingresoStock", "Return to stock" },
            { "AccionResuelta.Reproceso", "Rework" },
            { "AccionResuelta.Reemplazo", "Replacement" },
            { "AccionResuelta.Descarte", "Scrap" },

            { "EstadoPedido.Pendiente", "Pending" },
            { "EstadoPedido.Confirmado", "Confirmed" },
            { "EstadoPedido.EnPreparacion", "In preparation" },
            { "EstadoPedido.EnProduccion", "In production" },
            { "EstadoPedido.ListoParaEntrega", "Ready for delivery" },
            { "EstadoPedido.EnDistribucion", "In distribution" },
            { "EstadoPedido.Entregado", "Delivered" },
            { "EstadoPedido.Cancelado", "Cancelled" },

            { "EstadoPresupuesto.Borrador", "Draft" },
            { "EstadoPresupuesto.Enviado", "Sent" },
            { "EstadoPresupuesto.Aceptado", "Accepted" },
            { "EstadoPresupuesto.Vencido", "Expired" },
            { "EstadoPresupuesto.Rechazado", "Rejected" },
            { "EstadoPresupuesto.ConvertidoEnPedido", "Converted to order" },

            { "Moneda.PesosArgentinos", "ARS" },
            { "Moneda.DolaresEstadounidenses", "USD" },

            { "TipoItemPresupuesto.Producto", "Product" },
            { "TipoItemPresupuesto.Servicio", "Service" },
            { "TipoItemPresupuesto.FabricacionEspecial", "Custom manufacture" }
        };

        private static readonly string[] Buenos = { "Aprobado", "Finalizada", "Recibida", "Entregado", "EntregadoConforme", "Pagada", "Aceptado", "Confirmado", "Resuelta", "ConvertidoEnPedido" };
        private static readonly string[] Malos = { "Cancelada", "Cancelado", "Rechazado", "Rechazada", "Vencida", "Vencido", "Anulada", "Ausente", "DomicilioIncorrecto", "Urgente" };
        private static readonly string[] Atencion = { "Pendiente", "EnProceso", "ControlCalidad", "Reproceso", "Borrador", "Parcial", "Reprogramado", "EnTransito", "EnInspeccion", "Solicitada",
            "EnCurso", "Planificada", "Asignado", "EnPreparacion", "EnProduccion", "ListoParaEntrega", "EnDistribucion", "Emitida", "Enviado", "EntregaParcial", "ParcialmenteRecibida", "Alta" };

        /// <summary>
        /// Texto ya traducido de cada estado -> 1 (bien), 2 (en curso o atencion) o 3 (problema).
        /// Sirve para pintar las celdas de estado en las tablas.
        /// </summary>
        public static Dictionary<string, int> TonosPorTexto()
        {
            Dictionary<string, int> tonos = new Dictionary<string, int>();

            foreach (KeyValuePair<string, string> par in Textos)
            {
                if (!(par.Key.StartsWith("Estado") || par.Key.StartsWith("Resultado") || par.Key.StartsWith("Prioridad")))
                {
                    continue;
                }

                string miembro = par.Key.Substring(par.Key.IndexOf('.') + 1);
                int tono = Array.IndexOf(Malos, miembro) >= 0 ? 3 : Array.IndexOf(Buenos, miembro) >= 0 ? 1 : Array.IndexOf(Atencion, miembro) >= 0 ? 2 : 0;
                string texto = Mensajes.T(par.Value);

                if (tono > 0 && !tonos.ContainsKey(texto))
                {
                    tonos[texto] = tono;
                }
            }

            return tonos;
        }

        public static string De<T>(T valor) where T : struct
        {
            string clave = typeof(T).Name + "." + valor;
            string texto;

            if (!Textos.TryGetValue(clave, out texto))
            {
                texto = valor.ToString();
            }

            return Mensajes.T(texto);
        }

        public static string De<T>(T? valor) where T : struct
        {
            return valor.HasValue ? De(valor.Value) : string.Empty;
        }
    }
}
