using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;



namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Implementación en memoria del repositorio de pedidos.
    /// </summary>
    public class PedidoRepositoryMemory
        : IPedidoRepository
    {
        private static readonly List<Pedido> _pedidos =
            new List<Pedido>();

        public void Add(Pedido entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(
                    nameof(entity),
                    "El pedido no puede ser nulo.");
            }

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            if (ExistsByNumero(entity.Numero))
            {
                throw new InvalidOperationException(
                    "Ya existe un pedido con el número '" +
                    entity.Numero +
                    "'.");
            }

            if (ExistsByPresupuestoId(
                entity.PresupuestoId))
            {
                throw new InvalidOperationException(
                    "El presupuesto ya fue convertido en pedido.");
            }

            if (entity.Detalles == null)
            {
                entity.Detalles =
                    new List<DetallePedido>();
            }

            foreach (DetallePedido detalle
                in entity.Detalles)
            {
                if (detalle.Id == Guid.Empty)
                {
                    detalle.Id = Guid.NewGuid();
                }

                detalle.PedidoId = entity.Id;
            }

            _pedidos.Add(entity);
        }

        public void Update(Pedido entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(
                    nameof(entity),
                    "El pedido no puede ser nulo.");
            }

            Pedido pedidoExistente =
                GetById(entity.Id);

            if (pedidoExistente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el pedido que se desea modificar.");
            }

            Pedido pedidoConMismoNumero =
                GetByNumero(entity.Numero);

            if (pedidoConMismoNumero != null &&
                pedidoConMismoNumero.Id != entity.Id)
            {
                throw new InvalidOperationException(
                    "Ya existe otro pedido con el número '" +
                    entity.Numero +
                    "'.");
            }

            pedidoExistente.Numero =
                entity.Numero;

            pedidoExistente.ClienteId =
                entity.ClienteId;

            pedidoExistente.PresupuestoId =
                entity.PresupuestoId;

            pedidoExistente.FechaPedido =
                entity.FechaPedido;

            pedidoExistente.Estado =
                entity.Estado;

            pedidoExistente.Moneda =
                entity.Moneda;

            pedidoExistente.TipoCambio =
                entity.TipoCambio;

            pedidoExistente.PorcentajeIVA =
                entity.PorcentajeIVA;

            pedidoExistente.PorcentajeRecargo =
                entity.PorcentajeRecargo;

            pedidoExistente.PorcentajeDescuentoGeneral =
                entity.PorcentajeDescuentoGeneral;

            pedidoExistente.PorcentajeAnticipo =
                entity.PorcentajeAnticipo;

            pedidoExistente.ImporteAnticipo =
                entity.ImporteAnticipo;

            pedidoExistente.MedioPagoAnticipo =
                entity.MedioPagoAnticipo;

            pedidoExistente.ComprobanteAnticipo =
                entity.ComprobanteAnticipo;

            pedidoExistente.FechaAnticipo =
                entity.FechaAnticipo;

            pedidoExistente.CondicionPago =
                entity.CondicionPago;

            pedidoExistente.PlazoEntrega =
                entity.PlazoEntrega;

            pedidoExistente.EntregaIncluida =
                entity.EntregaIncluida;

            pedidoExistente.Observaciones =
                entity.Observaciones;

            pedidoExistente.Detalles =
                entity.Detalles
                ?? new List<DetallePedido>();

            foreach (DetallePedido detalle
                in pedidoExistente.Detalles)
            {
                if (detalle.Id == Guid.Empty)
                {
                    detalle.Id = Guid.NewGuid();
                }

                detalle.PedidoId =
                    pedidoExistente.Id;
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del pedido no es válido.",
                    nameof(id));
            }

            Pedido pedido =
                GetById(id);

            if (pedido != null)
            {
                _pedidos.Remove(pedido);
            }
        }

        public void ActualizarEstado(Guid pedidoId, EstadoPedido estado)
        {
            Pedido existente = _pedidos.FirstOrDefault(
                pedido => pedido.Id == pedidoId);

            if (existente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el pedido indicado.");
            }

            existente.Estado = estado;
        }

        public Pedido GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return _pedidos.FirstOrDefault(
                pedido => pedido.Id == id);
        }

        public List<Pedido> GetAll()
        {
            return _pedidos
                .OrderByDescending(
                    pedido => pedido.FechaPedido)
                .ToList();
        }

        public Pedido GetByNumero(
            string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
            {
                return null;
            }

            string numeroNormalizado =
                numero.Trim().ToUpperInvariant();

            return _pedidos.FirstOrDefault(
                pedido =>
                    string.Equals(
                        pedido.Numero,
                        numeroNormalizado,
                        StringComparison.OrdinalIgnoreCase));
        }

        public Pedido GetByPresupuestoId(
            Guid presupuestoId)
        {
            if (presupuestoId == Guid.Empty)
            {
                return null;
            }

            return _pedidos.FirstOrDefault(
                pedido =>
                    pedido.PresupuestoId ==
                    presupuestoId);
        }

        public List<Pedido> GetByClienteId(
            Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                return new List<Pedido>();
            }

            return _pedidos
                .Where(
                    pedido =>
                        pedido.ClienteId ==
                        clienteId)
                .OrderByDescending(
                    pedido => pedido.FechaPedido)
                .ToList();
        }

        public bool ExistsByNumero(
            string numero)
        {
            return GetByNumero(numero) != null;
        }

        public bool ExistsByPresupuestoId(
            Guid presupuestoId)
        {
            return GetByPresupuestoId(
                presupuestoId) != null;
        }
    }
}