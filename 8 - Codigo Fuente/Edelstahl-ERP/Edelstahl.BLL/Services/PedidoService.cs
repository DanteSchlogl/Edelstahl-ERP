using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina la creación, validación y persistencia
    /// de los pedidos comerciales.
    /// </summary>
    public class PedidoService
    {
        private readonly IPedidoRepository
            _pedidoRepository;

        private readonly IPresupuestoRepository
            _presupuestoRepository;

        private readonly IClienteRepository
            _clienteRepository;

        private readonly INegocioStore _almacen;

        public PedidoService()
        {
            _pedidoRepository =
                FactoryDataAccess.PedidoRepository;

            _presupuestoRepository =
                FactoryDataAccess.PresupuestoRepository;

            _clienteRepository =
                FactoryDataAccess.ClienteRepository;

            _almacen = FactoryDataAccess.NegocioStore;
        }

        public PedidoService(
            IPedidoRepository pedidoRepository,
            IPresupuestoRepository presupuestoRepository,
            IClienteRepository clienteRepository)
        {
            _pedidoRepository =
                pedidoRepository
                ?? throw new ArgumentNullException(
                    nameof(pedidoRepository));

            _presupuestoRepository =
                presupuestoRepository
                ?? throw new ArgumentNullException(
                    nameof(presupuestoRepository));

            _clienteRepository =
                clienteRepository
                ?? throw new ArgumentNullException(
                    nameof(clienteRepository));

            _almacen = FactoryDataAccess.NegocioStore;
        }

        public Pedido ConfirmarDesdePresupuesto(
            Guid clienteId,
            Guid presupuestoId,
            decimal importeAnticipo = 0m,
            string medioPagoAnticipo = "",
            string comprobanteAnticipo = "",
            DateTime? fechaAnticipo = null)
        {
            Cliente cliente =
                ObtenerClienteValido(
                    clienteId);

            Presupuesto presupuesto =
                ObtenerPresupuestoValido(
                    presupuestoId,
                    cliente.Id);

            ValidarPedidoNoExistente(
                presupuesto.Id);

            if (presupuesto.EntregaIncluida && Direcciones.Falta(cliente.DireccionEntrega))
            {
                throw Mensajes.Regla("The customer has no delivery address. Complete it in Customers before confirming an order with delivery.");
            }

            ValidarAnticipo(
                presupuesto,
                importeAnticipo,
                medioPagoAnticipo,
                comprobanteAnticipo,
                fechaAnticipo);

            ValidarCredito(
                cliente,
                presupuesto,
                importeAnticipo);

            Pedido pedido =
                CrearPedidoDesdePresupuesto(
                    cliente,
                    presupuesto,
                    importeAnticipo,
                    medioPagoAnticipo,
                    comprobanteAnticipo,
                    fechaAnticipo);

            try
            {
                pedido.Confirmar();
            }
            catch (InvalidOperationException)
            {
                throw Mensajes.Regla("The order does not meet the conditions to be confirmed. Check the deposit and the quotation lines.");
            }

            // Primero se reservan las existencias; si algo falla después,
            // las reservas y el estado del presupuesto se deshacen.
            List<Guid> detallesReservados = ReservarExistencias(pedido);

            try
            {
                presupuesto.Estado = EstadoPresupuesto.ConvertidoEnPedido;
                _presupuestoRepository.Update(presupuesto);

                try
                {
                    _pedidoRepository.Add(pedido);
                }
                catch
                {
                    presupuesto.Estado = EstadoPresupuesto.Aceptado;
                    _presupuestoRepository.Update(presupuesto);
                    throw;
                }
            }
            catch
            {
                LiberarReservas(detallesReservados, "No se pudo confirmar el pedido");
                throw;
            }

            return pedido;

        }

        /// <summary>
        /// Reserva las existencias de las líneas de producto del catálogo.
        /// Si alguna no alcanza, no se reserva nada.
        /// </summary>
        private List<Guid> ReservarExistencias(Pedido pedido)
        {
            List<DetallePedido> lineas = pedido.Detalles
                .Where(d => d.TipoItem == TipoItemPresupuesto.Producto)
                .ToList();

            if (lineas.Count == 0)
            {
                return new List<Guid>();
            }

            return _almacen.Ejecutar(unidad =>
            {
                List<Guid> reservados = new List<Guid>();

                foreach (DetallePedido linea in lineas)
                {
                    string sku = linea.Codigo;
                    Producto producto = unidad.Conjunto<Producto>().FirstOrDefault(p => p.CodigoSku == sku);

                    if (producto == null)
                    {
                        throw Mensajes.Regla("The product {0} is not in the catalog.", linea.Codigo);
                    }

                    InventarioMotor.Reservar(unidad, producto.Id, linea.Cantidad, linea.Id, null, pedido.Numero);
                    reservados.Add(linea.Id);
                }

                return reservados;
            });
        }

        private void LiberarReservas(List<Guid> detalleIds, string motivo)
        {
            if (detalleIds == null || detalleIds.Count == 0)
            {
                return;
            }

            _almacen.Ejecutar(unidad =>
            {
                foreach (Guid detalleId in detalleIds)
                {
                    foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeDetalle(unidad, detalleId))
                    {
                        InventarioMotor.LiberarReserva(unidad, reserva, motivo);
                    }
                }
            });
        }

        /// <summary>
        /// Cancela un pedido que todavía no fue despachado ni facturado:
        /// libera sus reservas y cancela las órdenes de fabricación pendientes.
        /// </summary>
        public void Cancelar(Guid pedidoId, string motivo)
        {
            Autorizacion.Exigir(Edelstahl.Domain.Security.Permiso.PedidosConfirmar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for cancelling is required.");
            }

            Pedido pedido = _pedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            if (pedido.Estado == EstadoPedido.Cancelado || pedido.Estado == EstadoPedido.Entregado
                || pedido.Estado == EstadoPedido.EnDistribucion)
            {
                throw Mensajes.Regla("The order cannot be cancelled in its current state.");
            }

            _almacen.Ejecutar(unidad =>
            {
                if (unidad.Conjunto<Edelstahl.Domain.Finanzas.Factura>().Any(
                    f => f.PedidoId == pedidoId && f.EstadoPago != Edelstahl.Domain.Finanzas.EstadoPago.Anulada))
                {
                    throw Mensajes.Regla("The order has an active invoice. Issue a credit note first.");
                }

                if (unidad.Conjunto<Edelstahl.Domain.Logistica.RemitoEntrega>().Any(
                    r => r.PedidoId == pedidoId && r.Estado != Edelstahl.Domain.Logistica.EstadoRemito.Cancelado))
                {
                    throw Mensajes.Regla("The order already has delivery notes.");
                }

                foreach (Edelstahl.Domain.Produccion.OrdenFabricacion orden in unidad
                    .Conjunto<Edelstahl.Domain.Produccion.OrdenFabricacion>()
                    .Where(o => o.PedidoId == pedidoId
                        && o.Estado != Edelstahl.Domain.Produccion.EstadoProduccion.Cancelada
                        && o.Estado != Edelstahl.Domain.Produccion.EstadoProduccion.Finalizada)
                    .ToList())
                {
                    orden.Estado = Edelstahl.Domain.Produccion.EstadoProduccion.Cancelada;

                    foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeOrden(unidad, orden.Id))
                    {
                        InventarioMotor.LiberarReserva(unidad, reserva, "Pedido cancelado");
                    }
                }

                foreach (DetallePedido detalle in pedido.Detalles)
                {
                    foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeDetalle(unidad, detalle.Id))
                    {
                        InventarioMotor.LiberarReserva(unidad, reserva, "Pedido cancelado: " + motivo);
                    }
                }
            });

            _pedidoRepository.ActualizarEstado(pedidoId, EstadoPedido.Cancelado);

            new BitacoraService().RegistrarAuditoria(
                "Ventas", "CancelarPedido", "Pedido", pedidoId, pedido.Numero + ": " + motivo);
        }

        public Pedido ObtenerPorId(
            Guid pedidoId)
        {
            if (pedidoId == Guid.Empty)
            {
                throw Mensajes.Regla("The order identifier is not valid.");
            }

            return _pedidoRepository.GetById(
                pedidoId);
        }

        public Pedido ObtenerPorNumero(
            string numero)
        {
            if (string.IsNullOrWhiteSpace(
                numero))
            {
                return null;
            }

            return _pedidoRepository.GetByNumero(
                numero.Trim().ToUpperInvariant());
        }

        public Pedido ObtenerPorPresupuesto(
            Guid presupuestoId)
        {
            if (presupuestoId == Guid.Empty)
            {
                throw Mensajes.Regla("The quotation identifier is not valid.");
            }

            return _pedidoRepository
                .GetByPresupuestoId(
                    presupuestoId);
        }

        public List<Pedido> ObtenerTodos()
        {
            return _pedidoRepository
                .GetAll()
                .OrderByDescending(
                    pedido =>
                        pedido.FechaPedido)
                .ToList();
        }

        public List<Pedido> ObtenerPorCliente(
            Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            return _pedidoRepository
                .GetByClienteId(
                    clienteId)
                .OrderByDescending(
                    pedido =>
                        pedido.FechaPedido)
                .ToList();
        }

        private Cliente ObtenerClienteValido(
            Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            Cliente cliente =
                _clienteRepository.GetById(
                    clienteId);

            if (cliente == null)
            {
                throw Mensajes.Regla("The selected customer was not found.");
            }

            if (!cliente.Activo)
            {
                throw Mensajes.Regla("The selected customer is not active.");
            }

            return cliente;
        }

        private Presupuesto ObtenerPresupuestoValido(
            Guid presupuestoId,
            Guid clienteId)
        {
            if (presupuestoId == Guid.Empty)
            {
                throw Mensajes.Regla("The quotation identifier is not valid.");
            }

            Presupuesto presupuesto =
                _presupuestoRepository.GetById(
                    presupuestoId);

            if (presupuesto == null)
            {
                throw Mensajes.Regla("The selected quotation was not found.");
            }

            if (presupuesto.ClienteId !=
                clienteId)
            {
                throw Mensajes.Regla("The quotation does not belong to the selected customer.");
            }

            if (!presupuesto.PuedeConfirmarse())
            {
                throw Mensajes.Regla("The quotation cannot be confirmed. It must be valid, accepted and have items.");
            }

            return presupuesto;
        }

        private void ValidarPedidoNoExistente(
            Guid presupuestoId)
        {
            if (_pedidoRepository
                .ExistsByPresupuestoId(
                    presupuestoId))
            {
                throw Mensajes.Regla("The selected quotation was already converted into an order.");
            }
        }

        private static void ValidarAnticipo(
            Presupuesto presupuesto,
            decimal importeAnticipo,
            string medioPagoAnticipo,
            string comprobanteAnticipo,
            DateTime? fechaAnticipo)
        {
            if (importeAnticipo < 0m)
            {
                throw Mensajes.Regla("The deposit amount cannot be negative.");
            }

            decimal anticipoRequerido =
                presupuesto.CalcularAnticipo();

            decimal margenRedondeo =
                0.01m;

            if (!presupuesto.RequiereAnticipo() && importeAnticipo <= 0m)
            {
                return;
            }

            // La sena es lo que el cliente realmente pago: puede ser mas que el minimo, nunca menos ni mas que el total.
            if (presupuesto.RequiereAnticipo() &&
                importeAnticipo <
                anticipoRequerido - margenRedondeo)
            {
                throw Mensajes.Regla("The deposit entered is below the minimum required by the quotation ({0}).", anticipoRequerido.ToString("N2"));
            }

            if (importeAnticipo >
                presupuesto.CalcularTotal() + margenRedondeo)
            {
                throw Mensajes.Regla("The deposit cannot exceed the total of the quotation.");
            }

            if (string.IsNullOrWhiteSpace(
                medioPagoAnticipo))
            {
                throw Mensajes.Regla("The payment method of the deposit is required.");
            }

            if (string.IsNullOrWhiteSpace(
                comprobanteAnticipo))
            {
                throw Mensajes.Regla("The receipt of the deposit is required.");
            }

            if (!fechaAnticipo.HasValue)
            {
                throw Mensajes.Regla("The date of the deposit is required.");
            }

            if (fechaAnticipo.Value.Date >
                DateTime.Today)
            {
                throw Mensajes.Regla("The date of the deposit cannot be in the future.");
            }
        }

        private static void ValidarCredito(
            Cliente cliente,
            Presupuesto presupuesto,
            decimal importeAnticipo)
        {
            decimal creditoDisponible =
                cliente.CalcularCreditoDisponible();

            decimal totalEnPesos =
                presupuesto.CalcularTotalEnPesos();

            decimal anticipoPermitido =
                presupuesto.RequiereAnticipo()
                    ? presupuesto.CalcularAnticipo()
                    : 0m;

            if (importeAnticipo >
                presupuesto.CalcularTotal())
            {
                importeAnticipo =
                    presupuesto.CalcularTotal();
            }

            decimal anticipoEnPesos =
                importeAnticipo;

            if (presupuesto.Moneda ==
                Moneda.DolaresEstadounidenses)
            {
                if (presupuesto.TipoCambio <= 0m)
                {
                    throw Mensajes.Regla("The exchange rate of the quotation is not valid.");
                }

                anticipoEnPesos =
                    importeAnticipo *
                    presupuesto.TipoCambio;
            }

            decimal creditoNecesario =
                totalEnPesos -
                anticipoEnPesos;

            if (creditoNecesario < 0m)
            {
                creditoNecesario =
                    0m;
            }

            if (creditoDisponible <
                creditoNecesario)
            {
                throw new Edelstahl.BLL.Exceptions.Base.ReglaDeNegocioException(
                    Mensajes.T("The available credit of the customer is not enough to confirm the order.") +
                    Environment.NewLine +
                    Environment.NewLine +
                    Mensajes.T("Available credit: {0}", "$" + creditoDisponible.ToString("N2")) +
                    Environment.NewLine +
                    Mensajes.T("Required credit: {0}", "$" + creditoNecesario.ToString("N2")) +
                    Environment.NewLine +
                    Mensajes.T("Allowed deposit: {0}",
                        ObtenerPrefijoMoneda(presupuesto.Moneda) + anticipoPermitido.ToString("N2")));
            }
        }

        private Pedido CrearPedidoDesdePresupuesto(
            Cliente cliente,
            Presupuesto presupuesto,
            decimal importeAnticipo,
            string medioPagoAnticipo,
            string comprobanteAnticipo,
            DateTime? fechaAnticipo)
        {
            Pedido pedido =
                new Pedido
                {
                    Numero =
                        GenerarNumeroPedido(),

                    ClienteId =
                        cliente.Id,

                    PresupuestoId =
                        presupuesto.Id,

                    FechaPedido =
                        DateTime.Now,

                    Estado =
                        EstadoPedido.Pendiente,

                    Moneda =
                        presupuesto.Moneda,

                    TipoCambio =
                        presupuesto.TipoCambio,

                    PorcentajeIVA =
                        presupuesto.PorcentajeIVA,

                    PorcentajeRecargo =
                        presupuesto.PorcentajeRecargo,

                    PorcentajeDescuentoGeneral =
                        presupuesto
                            .PorcentajeDescuentoGeneral,

                    PorcentajeAnticipo =
                        presupuesto.PorcentajeAnticipoEfectivo(),

                    ImporteAnticipo =
                        importeAnticipo,

                    MedioPagoAnticipo =
                        medioPagoAnticipo
                        ?? string.Empty,

                    ComprobanteAnticipo =
                        comprobanteAnticipo
                        ?? string.Empty,

                    FechaAnticipo =
                        importeAnticipo > 0m
                            ? fechaAnticipo
                            : null,

                    CondicionPago =
                        presupuesto.CondicionPago
                        ?? string.Empty,

                    PlazoEntrega =
                        presupuesto.PlazoEntrega
                        ?? string.Empty,

                    EntregaIncluida =
                        presupuesto.EntregaIncluida,

                    Observaciones =
                        presupuesto.Observaciones
                        ?? string.Empty
                };

            foreach (DetallePresupuesto detalle
                in presupuesto.Detalles)
            {
                DetallePedido detallePedido =
                    new DetallePedido
                    {
                        DetallePresupuestoId =
                            detalle.Id,

                        Codigo =
                            detalle.Codigo
                            ?? string.Empty,

                        Descripcion =
                            detalle.Descripcion
                            ?? string.Empty,

                        DescripcionTecnica =
                            detalle.DescripcionTecnica
                            ?? string.Empty,

                        TipoItem =
                            detalle.TipoItem,

                        Cantidad =
                            detalle.Cantidad,

                        PrecioUnitario =
                            detalle.PrecioUnitario,

                        PorcentajeDescuento =
                            detalle.PorcentajeDescuento,

                        RequiereFabricacion =
                            detalle.TipoItem ==
                            TipoItemPresupuesto
                                .FabricacionEspecial
                    };

                pedido.Detalles.Add(
                    detallePedido);
            }

            return pedido;
        }

        private string GenerarNumeroPedido()
        {
            string numero;

            do
            {
                string codigo =
                    Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 6)
                        .ToUpperInvariant();

                numero =
                    string.Format(
                        "PED-{0}-{1}",
                        DateTime.Today.Year,
                        codigo);
            }
            while (_pedidoRepository
                .ExistsByNumero(
                    numero));

            return numero;
        }

        private static string ObtenerPrefijoMoneda(
            Moneda moneda)
        {
            return moneda ==
                Moneda.DolaresEstadounidenses
                    ? "USD "
                    : "$";
        }
    }
}