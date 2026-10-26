using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Reglas de existencias, reservas y movimientos. Opera siempre dentro
    /// de la unidad de trabajo recibida, de modo que cada servicio decide
    /// qué operaciones se confirman o se revierten juntas.
    /// </summary>
    public static class InventarioMotor
    {
        public static UbicacionDeposito UbicacionPredeterminada(IUnidadDeTrabajo unidad, bool materiales)
        {
            List<UbicacionDeposito> activas = unidad.Conjunto<UbicacionDeposito>()
                .Where(u => u.Activo)
                .OrderBy(u => u.Codigo)
                .ToList();

            if (activas.Count == 0)
            {
                throw Mensajes.Regla("There is no active warehouse location. Create one in the Inventory module.");
            }

            if (materiales)
            {
                UbicacionDeposito deMateriales = activas.FirstOrDefault(
                    u => u.Codigo.StartsWith("MP", StringComparison.OrdinalIgnoreCase));

                if (deMateriales != null)
                {
                    return deMateriales;
                }
            }

            return activas[0];
        }

        public static UbicacionDeposito UbicacionParaProducto(IUnidadDeTrabajo unidad, Producto producto)
        {
            bool materiales = producto.Tipo == TipoProducto.MateriaPrima
                || producto.Tipo == TipoProducto.InsumoConsumible;

            return UbicacionPredeterminada(unidad, materiales);
        }

        public static Producto ObtenerProducto(IUnidadDeTrabajo unidad, Guid productoId)
        {
            Producto producto = unidad.Conjunto<Producto>().FirstOrDefault(p => p.Id == productoId);

            if (producto == null)
            {
                throw Mensajes.Regla("The product does not exist.");
            }

            return producto;
        }

        public static StockProducto ObtenerOCrearStock(
            IUnidadDeTrabajo unidad, Guid productoId, Guid ubicacionId)
        {
            StockProducto stock = unidad.Conjunto<StockProducto>().FirstOrDefault(
                s => s.ProductoId == productoId && s.UbicacionId == ubicacionId);

            if (stock == null)
            {
                stock = new StockProducto { ProductoId = productoId, UbicacionId = ubicacionId };
                unidad.Conjunto<StockProducto>().Agregar(stock);
                unidad.Guardar();
            }

            return stock;
        }

        public static decimal ExistenciaFisicaTotal(IUnidadDeTrabajo unidad, Guid productoId)
        {
            return unidad.Conjunto<StockProducto>()
                .Where(s => s.ProductoId == productoId)
                .Select(s => (decimal?)s.CantidadFisica)
                .Sum() ?? 0m;
        }

        public static decimal DisponibleTotal(IUnidadDeTrabajo unidad, Guid productoId)
        {
            return unidad.Conjunto<StockProducto>()
                .Where(s => s.ProductoId == productoId)
                .ToList()
                .Sum(s => s.CantidadDisponible);
        }

        /// <summary>
        /// Registra un movimiento que cambia la existencia física.
        /// Las reservas y liberaciones usan Reservar y LiberarReserva.
        /// </summary>
        public static MovimientoStock Mover(
            IUnidadDeTrabajo unidad,
            Guid productoId,
            Guid ubicacionId,
            TipoMovimientoStock tipo,
            decimal cantidad,
            decimal? costoUnitario,
            string documento,
            Guid? referenciaId,
            string lote,
            string colada,
            string observaciones,
            bool consumeReserva)
        {
            if (cantidad <= 0m)
            {
                throw Mensajes.Regla("The quantity must be greater than zero.");
            }

            if (tipo == TipoMovimientoStock.ReservaPedido || tipo == TipoMovimientoStock.LiberacionReserva)
            {
                throw Mensajes.Regla("The movement type is not valid for this operation.");
            }

            unidad.Guardar();

            Producto producto = ObtenerProducto(unidad, productoId);

            if (!producto.Activo)
            {
                throw Mensajes.Regla("The product {0} is inactive.", producto.CodigoSku);
            }

            UbicacionDeposito ubicacion = unidad.Conjunto<UbicacionDeposito>()
                .FirstOrDefault(u => u.Id == ubicacionId);

            if (ubicacion == null || !ubicacion.Activo)
            {
                throw Mensajes.Regla("The warehouse location is not available.");
            }

            StockProducto stock = ObtenerOCrearStock(unidad, productoId, ubicacionId);

            MovimientoStock movimiento = new MovimientoStock
            {
                ProductoId = productoId,
                UbicacionId = ubicacionId,
                Tipo = tipo,
                Usuario = SesionActual.NombreUsuario,
                ReferenciaId = referenciaId,
                Cantidad = cantidad,
                CostoUnitario = costoUnitario,
                DocumentoOrigen = documento ?? string.Empty,
                Lote = lote ?? string.Empty,
                CertificadoColada = colada ?? string.Empty,
                Observaciones = observaciones ?? string.Empty
            };

            if (movimiento.Incrementa)
            {
                if (costoUnitario.HasValue && costoUnitario.Value >= 0m)
                {
                    decimal antes = ExistenciaFisicaTotal(unidad, productoId);
                    decimal total = antes + cantidad;
                    decimal costo = total > 0m
                        ? ((antes * producto.CostoActual) + (cantidad * costoUnitario.Value)) / total
                        : costoUnitario.Value;

                    producto.CostoActual = Math.Round(costo, 2);
                    stock.CostoPromedio = producto.CostoActual;
                }

                stock.CantidadFisica += cantidad;
            }
            else if (consumeReserva)
            {
                if (stock.CantidadReservada < cantidad)
                {
                    throw Mensajes.Regla("The reserved quantity of {0} is not enough.", producto.CodigoSku);
                }

                stock.CantidadReservada -= cantidad;
                stock.CantidadFisica -= cantidad;
            }
            else
            {
                if (stock.CantidadDisponible < cantidad)
                {
                    throw Mensajes.Regla(
                        "Insufficient stock of {0}: available {1}, requested {2}.",
                        producto.CodigoSku,
                        stock.CantidadDisponible.ToString("0.###"),
                        cantidad.ToString("0.###"));
                }

                stock.CantidadFisica -= cantidad;
            }

            stock.FechaActualizacion = DateTime.Now;
            movimiento.SaldoFisicoResultante = stock.CantidadFisica;
            movimiento.SaldoReservadoResultante = stock.CantidadReservada;

            unidad.Conjunto<MovimientoStock>().Agregar(movimiento);

            return movimiento;
        }

        /// <summary>
        /// Reserva existencias repartiéndolas entre las ubicaciones que tengan
        /// disponible. Si no alcanza, no reserva nada.
        /// </summary>
        public static List<ReservaStock> Reservar(
            IUnidadDeTrabajo unidad,
            Guid productoId,
            decimal cantidad,
            Guid? detallePedidoId,
            Guid? ordenFabricacionId,
            string documento)
        {
            if (cantidad <= 0m)
            {
                throw Mensajes.Regla("The quantity must be greater than zero.");
            }

            unidad.Guardar();

            Producto producto = ObtenerProducto(unidad, productoId);

            if (!producto.Activo)
            {
                throw Mensajes.Regla("The product {0} is inactive.", producto.CodigoSku);
            }

            List<StockProducto> stocks = unidad.Conjunto<StockProducto>()
                .Where(s => s.ProductoId == productoId)
                .ToList()
                .Where(s => s.CantidadDisponible > 0m)
                .OrderByDescending(s => s.CantidadDisponible)
                .ToList();

            decimal disponible = stocks.Sum(s => s.CantidadDisponible);

            if (disponible < cantidad)
            {
                throw Mensajes.Regla(
                    "Insufficient stock of {0} ({1}): available {2}, required {3}.",
                    producto.CodigoSku,
                    producto.Nombre,
                    disponible.ToString("0.###"),
                    cantidad.ToString("0.###"));
            }

            List<ReservaStock> reservas = new List<ReservaStock>();
            decimal restante = cantidad;

            foreach (StockProducto stock in stocks)
            {
                if (restante <= 0m)
                {
                    break;
                }

                decimal tomar = Math.Min(restante, stock.CantidadDisponible);
                stock.CantidadReservada += tomar;
                stock.FechaActualizacion = DateTime.Now;

                ReservaStock reserva = new ReservaStock
                {
                    ProductoId = productoId,
                    UbicacionId = stock.UbicacionId,
                    DetallePedidoId = detallePedidoId,
                    OrdenFabricacionId = ordenFabricacionId,
                    CantidadReservada = tomar
                };

                unidad.Conjunto<ReservaStock>().Agregar(reserva);

                unidad.Conjunto<MovimientoStock>().Agregar(new MovimientoStock
                {
                    ProductoId = productoId,
                    UbicacionId = stock.UbicacionId,
                    Tipo = TipoMovimientoStock.ReservaPedido,
                    Usuario = SesionActual.NombreUsuario,
                    ReferenciaId = reserva.Id,
                    Cantidad = tomar,
                    DocumentoOrigen = documento ?? string.Empty,
                    SaldoFisicoResultante = stock.CantidadFisica,
                    SaldoReservadoResultante = stock.CantidadReservada
                });

                reservas.Add(reserva);
                restante -= tomar;
            }

            return reservas;
        }

        public static void LiberarReserva(IUnidadDeTrabajo unidad, ReservaStock reserva, string motivo)
        {
            if (reserva.Estado != EstadoReserva.Activa)
            {
                return;
            }

            decimal pendiente = reserva.CantidadPendiente;
            StockProducto stock = ObtenerOCrearStock(unidad, reserva.ProductoId, reserva.UbicacionId);

            stock.CantidadReservada = Math.Max(0m, stock.CantidadReservada - pendiente);
            stock.FechaActualizacion = DateTime.Now;

            reserva.Estado = EstadoReserva.Liberada;
            reserva.FechaLiberacion = DateTime.Now;
            reserva.MotivoLiberacion = motivo ?? string.Empty;

            if (pendiente > 0m)
            {
                unidad.Conjunto<MovimientoStock>().Agregar(new MovimientoStock
                {
                    ProductoId = reserva.ProductoId,
                    UbicacionId = reserva.UbicacionId,
                    Tipo = TipoMovimientoStock.LiberacionReserva,
                    Usuario = SesionActual.NombreUsuario,
                    ReferenciaId = reserva.Id,
                    Cantidad = pendiente,
                    Observaciones = motivo ?? string.Empty,
                    SaldoFisicoResultante = stock.CantidadFisica,
                    SaldoReservadoResultante = stock.CantidadReservada
                });
            }
        }

        /// <summary>
        /// Consume una parte de la reserva: baja la existencia física y la
        /// reservada a la vez.
        /// </summary>
        public static void ConsumirReserva(
            IUnidadDeTrabajo unidad,
            ReservaStock reserva,
            decimal cantidad,
            TipoMovimientoStock tipo,
            string documento,
            Guid? referenciaId)
        {
            if (reserva.Estado != EstadoReserva.Activa)
            {
                throw Mensajes.Regla("The reservation is no longer active.");
            }

            if (cantidad > reserva.CantidadPendiente)
            {
                throw Mensajes.Regla("The quantity exceeds what is pending on the reservation.");
            }

            Mover(unidad, reserva.ProductoId, reserva.UbicacionId, tipo, cantidad, null,
                documento, referenciaId, string.Empty, string.Empty, string.Empty, true);

            reserva.CantidadConsumida += cantidad;

            if (reserva.CantidadPendiente <= 0m)
            {
                reserva.Estado = EstadoReserva.Consumida;
            }
        }

        public static List<ReservaStock> ReservasActivasDeDetalle(IUnidadDeTrabajo unidad, Guid detallePedidoId)
        {
            return unidad.Conjunto<ReservaStock>()
                .Where(r => r.DetallePedidoId == detallePedidoId && r.Estado == EstadoReserva.Activa)
                .ToList();
        }

        public static List<ReservaStock> ReservasActivasDeOrden(IUnidadDeTrabajo unidad, Guid ordenId)
        {
            return unidad.Conjunto<ReservaStock>()
                .Where(r => r.OrdenFabricacionId == ordenId && r.Estado == EstadoReserva.Activa)
                .ToList();
        }
    }
}
