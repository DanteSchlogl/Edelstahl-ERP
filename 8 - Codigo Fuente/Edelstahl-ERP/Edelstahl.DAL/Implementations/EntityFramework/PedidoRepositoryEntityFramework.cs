using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Implementations.EntityFramework
{
    /// <summary>
    /// Persistencia de pedidos y detalles mediante Entity Framework 6.
    /// </summary>
    internal class PedidoRepositoryEntityFramework
        : IPedidoRepository
    {
        public void Add(Pedido entity)
        {
            ValidarPedido(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            PrepararDetalles(entity);

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                try
                {
                    if (context.Pedidos.Any(
                        pedido => pedido.Numero == entity.Numero))
                    {
                        throw new RegistroDuplicadoException(
                    LanguageService.Translate("An order with the entered number already exists."));
                    }

                    if (context.Pedidos.Any(
                        pedido =>
                            pedido.PresupuestoId == entity.PresupuestoId))
                    {
                        throw new RegistroDuplicadoException(
                    LanguageService.Translate("The quote was already converted into an order."));
                    }

                    context.Pedidos.Add(entity);
                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void Update(Pedido entity)
        {
            ValidarPedido(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The order identifier is not valid."));
            }

            PrepararDetalles(entity);

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                try
                {
                    bool numeroDuplicado = context.Pedidos.Any(
                        pedido =>
                            pedido.Numero == entity.Numero &&
                            pedido.Id != entity.Id);

                    bool presupuestoDuplicado = context.Pedidos.Any(
                        pedido =>
                            pedido.PresupuestoId == entity.PresupuestoId &&
                            pedido.Id != entity.Id);

                    if (numeroDuplicado || presupuestoDuplicado)
                    {
                        throw new RegistroDuplicadoException(
                    LanguageService.Translate("The order number or its quote is already in use."));
                    }

                    Pedido existente = context.Pedidos
                        .Include(pedido => pedido.Detalles)
                        .FirstOrDefault(pedido => pedido.Id == entity.Id);

                    if (existente == null)
                    {
                        throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The order to modify was not found."));
                    }

                    context.DetallesPedido.RemoveRange(existente.Detalles);
                    CopiarDatos(entity, existente);
                    existente.Detalles = new List<DetallePedido>();

                    foreach (DetallePedido detalle in entity.Detalles)
                    {
                        detalle.PedidoId = existente.Id;
                        existente.Detalles.Add(detalle);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void ActualizarEstado(Guid pedidoId, EstadoPedido estado)
        {
            if (pedidoId == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The order identifier is not valid."));
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                Pedido existente = context.Pedidos.Find(pedidoId);

                if (existente == null)
                {
                    throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The indicated order was not found."));
                }

                existente.Estado = estado;
                context.SaveChanges();
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The order identifier is not valid."));
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                Pedido existente = context.Pedidos.Find(id);

                if (existente == null)
                {
                    throw new RegistroNoEncontradoException(
                    LanguageService.Translate("The order to delete was not found."));
                }

                existente.Estado = EstadoPedido.Cancelado;
                context.SaveChanges();
            }
        }

        public Pedido GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos
                    .AsNoTracking()
                    .Include(pedido => pedido.Detalles)
                    .FirstOrDefault(pedido => pedido.Id == id);
            }
        }

        public List<Pedido> GetAll()
        {
            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos
                    .AsNoTracking()
                    .Include(pedido => pedido.Detalles)
                    .OrderByDescending(pedido => pedido.FechaPedido)
                    .ToList();
            }
        }

        public Pedido GetByNumero(string numero)
        {
            string normalizado = NormalizarNumero(numero);

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos
                    .AsNoTracking()
                    .Include(pedido => pedido.Detalles)
                    .FirstOrDefault(
                        pedido => pedido.Numero == normalizado);
            }
        }

        public Pedido GetByPresupuestoId(Guid presupuestoId)
        {
            if (presupuestoId == Guid.Empty)
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos
                    .AsNoTracking()
                    .Include(pedido => pedido.Detalles)
                    .FirstOrDefault(
                        pedido => pedido.PresupuestoId == presupuestoId);
            }
        }

        public List<Pedido> GetByClienteId(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                return new List<Pedido>();
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos
                    .AsNoTracking()
                    .Include(pedido => pedido.Detalles)
                    .Where(pedido => pedido.ClienteId == clienteId)
                    .OrderByDescending(pedido => pedido.FechaPedido)
                    .ToList();
            }
        }

        public bool ExistsByNumero(string numero)
        {
            string normalizado = NormalizarNumero(numero);

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                return false;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos.Any(
                    pedido => pedido.Numero == normalizado);
            }
        }

        public bool ExistsByPresupuestoId(Guid presupuestoId)
        {
            if (presupuestoId == Guid.Empty)
            {
                return false;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Pedidos.Any(
                    pedido => pedido.PresupuestoId == presupuestoId);
            }
        }

        private static void PrepararDetalles(Pedido pedido)
        {
            pedido.Numero = NormalizarNumero(pedido.Numero);

            if (pedido.Detalles == null)
            {
                pedido.Detalles = new List<DetallePedido>();
            }

            foreach (DetallePedido detalle in pedido.Detalles)
            {
                if (detalle.Id == Guid.Empty)
                {
                    detalle.Id = Guid.NewGuid();
                }

                detalle.PedidoId = pedido.Id;
                detalle.Codigo = Limpiar(detalle.Codigo);
                detalle.Descripcion = Limpiar(detalle.Descripcion);
                detalle.DescripcionTecnica = Limpiar(
                    detalle.DescripcionTecnica);
            }
        }

        private static void CopiarDatos(Pedido origen, Pedido destino)
        {
            destino.Numero = origen.Numero;
            destino.ClienteId = origen.ClienteId;
            destino.PresupuestoId = origen.PresupuestoId;
            destino.FechaPedido = origen.FechaPedido;
            destino.Estado = origen.Estado;
            destino.Moneda = origen.Moneda;
            destino.TipoCambio = origen.TipoCambio;
            destino.PorcentajeIVA = origen.PorcentajeIVA;
            destino.PorcentajeRecargo = origen.PorcentajeRecargo;
            destino.PorcentajeDescuentoGeneral =
                origen.PorcentajeDescuentoGeneral;
            destino.PorcentajeAnticipo = origen.PorcentajeAnticipo;
            destino.ImporteAnticipo = origen.ImporteAnticipo;
            destino.MedioPagoAnticipo = Limpiar(origen.MedioPagoAnticipo);
            destino.ComprobanteAnticipo = Limpiar(
                origen.ComprobanteAnticipo);
            destino.FechaAnticipo = origen.FechaAnticipo;
            destino.CondicionPago = Limpiar(origen.CondicionPago);
            destino.PlazoEntrega = Limpiar(origen.PlazoEntrega);
            destino.EntregaIncluida = origen.EntregaIncluida;
            destino.Observaciones = Limpiar(origen.Observaciones);
        }

        private static void ValidarPedido(Pedido pedido)
        {
            if (pedido == null)
            {
                throw new ArgumentNullException(nameof(pedido));
            }

            if (string.IsNullOrWhiteSpace(pedido.Numero))
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The order number is required."));
            }

            if (pedido.ClienteId == Guid.Empty ||
                pedido.PresupuestoId == Guid.Empty)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The customer and the quote of the order are required."));
            }

            if (pedido.Detalles == null || pedido.Detalles.Count == 0)
            {
                throw new DatoInvalidoException(
                    LanguageService.Translate("The order must contain at least one line."));
            }
        }

        private static string NormalizarNumero(string numero)
        {
            return string.IsNullOrWhiteSpace(numero)
                ? string.Empty
                : numero.Trim().ToUpperInvariant();
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim();
        }
    }
}
