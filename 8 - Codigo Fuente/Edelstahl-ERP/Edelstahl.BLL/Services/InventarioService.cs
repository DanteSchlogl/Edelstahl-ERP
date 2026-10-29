using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Consulta de existencias, movimientos, reservas y alertas de reposición.
    /// </summary>
    public class InventarioService
    {
        private readonly INegocioStore _almacen;

        public InventarioService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public InventarioService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Depósitos y ubicaciones
        // ---------------------------------------------------------------

        public List<Deposito> ObtenerDepositos()
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Deposito>().ToList().OrderBy(d => d.Nombre).ToList());
        }

        public List<UbicacionDto> ObtenerUbicaciones(bool soloActivas)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Deposito> depositos = unidad.Conjunto<Deposito>().ToList()
                    .ToDictionary(d => d.Id);

                return unidad.Conjunto<UbicacionDeposito>().ToList()
                    .Where(u => !soloActivas || u.Activo)
                    .Select(u => new UbicacionDto
                    {
                        Id = u.Id,
                        DepositoId = u.DepositoId,
                        Deposito = depositos.ContainsKey(u.DepositoId) ? depositos[u.DepositoId].Nombre : string.Empty,
                        Codigo = u.Codigo,
                        Sector = u.Sector,
                        Descripcion = u.Descripcion,
                        Activo = u.Activo
                    })
                    .OrderBy(u => u.Deposito).ThenBy(u => u.Codigo)
                    .ToList();
            });
        }

        public Deposito GuardarDeposito(Deposito deposito)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (deposito == null || string.IsNullOrWhiteSpace(deposito.Codigo)
                || string.IsNullOrWhiteSpace(deposito.Nombre))
            {
                throw Mensajes.Regla("The warehouse code and name are required.");
            }

            string codigo = deposito.Codigo.Trim().ToUpperInvariant();

            Deposito resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<Deposito> conjunto = unidad.Conjunto<Deposito>();

                if (conjunto.Any(d => d.Codigo == codigo && d.Id != deposito.Id))
                {
                    throw Mensajes.Regla("There is already a warehouse with the code {0}.", codigo);
                }

                Deposito existente = conjunto.FirstOrDefault(d => d.Id == deposito.Id);

                if (existente == null)
                {
                    existente = new Deposito { Id = deposito.Id };
                    conjunto.Agregar(existente);
                }

                existente.Codigo = codigo;
                existente.Nombre = deposito.Nombre.Trim();
                existente.Direccion = deposito.Direccion ?? string.Empty;
                existente.Localidad = deposito.Localidad ?? string.Empty;
                existente.Descripcion = deposito.Descripcion ?? string.Empty;
                existente.Responsable = deposito.Responsable ?? string.Empty;
                existente.Activo = deposito.Activo;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Inventario", "GuardarDeposito", "Deposito", resultado.Id, resultado.Nombre);

            return resultado;
        }

        public UbicacionDeposito GuardarUbicacion(UbicacionDeposito ubicacion)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (ubicacion == null || string.IsNullOrWhiteSpace(ubicacion.Codigo) || ubicacion.DepositoId == Guid.Empty)
            {
                throw Mensajes.Regla("The warehouse and the location code are required.");
            }

            string codigo = ubicacion.Codigo.Trim().ToUpperInvariant();

            UbicacionDeposito resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<UbicacionDeposito> conjunto = unidad.Conjunto<UbicacionDeposito>();

                if (conjunto.Any(u => u.DepositoId == ubicacion.DepositoId && u.Codigo == codigo && u.Id != ubicacion.Id))
                {
                    throw Mensajes.Regla("There is already a location with the code {0} in this warehouse.", codigo);
                }

                UbicacionDeposito existente = conjunto.FirstOrDefault(u => u.Id == ubicacion.Id);

                if (existente == null)
                {
                    existente = new UbicacionDeposito { Id = ubicacion.Id, DepositoId = ubicacion.DepositoId };
                    conjunto.Agregar(existente);
                }

                existente.Codigo = codigo;
                existente.Sector = ubicacion.Sector ?? string.Empty;
                existente.Pasillo = ubicacion.Pasillo ?? string.Empty;
                existente.Estanteria = ubicacion.Estanteria ?? string.Empty;
                existente.Nivel = ubicacion.Nivel ?? string.Empty;
                existente.Descripcion = ubicacion.Descripcion ?? string.Empty;
                existente.Activo = ubicacion.Activo;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Inventario", "GuardarUbicacion", "UbicacionDeposito", resultado.Id, resultado.Codigo);

            return resultado;
        }

        // ---------------------------------------------------------------
        // Existencias
        // ---------------------------------------------------------------

        public List<StockDto> ConsultarStock(string texto, Guid? depositoId, bool soloAlertas)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);
                Dictionary<Guid, UbicacionDeposito> ubicaciones = unidad.Conjunto<UbicacionDeposito>().ToList().ToDictionary(u => u.Id);
                Dictionary<Guid, Deposito> depositos = unidad.Conjunto<Deposito>().ToList().ToDictionary(d => d.Id);

                string buscado = (texto ?? string.Empty).Trim().ToLowerInvariant();

                List<StockDto> filas = new List<StockDto>();

                foreach (StockProducto stock in unidad.Conjunto<StockProducto>().ToList())
                {
                    Producto producto;
                    UbicacionDeposito ubicacion;

                    if (!productos.TryGetValue(stock.ProductoId, out producto)
                        || !ubicaciones.TryGetValue(stock.UbicacionId, out ubicacion))
                    {
                        continue;
                    }

                    if (depositoId.HasValue && ubicacion.DepositoId != depositoId.Value)
                    {
                        continue;
                    }

                    if (buscado.Length > 0
                        && !producto.CodigoSku.ToLowerInvariant().Contains(buscado)
                        && !producto.Nombre.ToLowerInvariant().Contains(buscado))
                    {
                        continue;
                    }

                    string estado = CatalogoService.EstadoStock(stock.CantidadDisponible, producto.PuntoReposicion);

                    if (soloAlertas && stock.CantidadDisponible > producto.PuntoReposicion)
                    {
                        continue;
                    }

                    Deposito deposito;
                    depositos.TryGetValue(ubicacion.DepositoId, out deposito);

                    filas.Add(new StockDto
                    {
                        ProductoId = producto.Id,
                        UbicacionId = ubicacion.Id,
                        Sku = producto.CodigoSku,
                        Producto = producto.Nombre,
                        Deposito = deposito == null ? string.Empty : deposito.Nombre,
                        Ubicacion = ubicacion.Codigo,
                        Fisica = stock.CantidadFisica,
                        Reservada = stock.CantidadReservada,
                        Bloqueada = stock.CantidadBloqueada,
                        Disponible = stock.CantidadDisponible,
                        PuntoReposicion = producto.PuntoReposicion,
                        Estado = estado
                    });
                }

                return filas.OrderBy(f => f.Producto).ThenBy(f => f.Ubicacion).ToList();
            });
        }

        public List<MovimientoDto> ObtenerMovimientos(Guid? productoId, DateTime? desde, DateTime? hasta, int maximo)
        {
            return _almacen.Ejecutar(unidad =>
            {
                IQueryable<MovimientoStock> consulta = unidad.Conjunto<MovimientoStock>();

                if (productoId.HasValue)
                {
                    Guid id = productoId.Value;
                    consulta = consulta.Where(m => m.ProductoId == id);
                }

                if (desde.HasValue)
                {
                    DateTime inicio = desde.Value.Date;
                    consulta = consulta.Where(m => m.FechaMovimiento >= inicio);
                }

                if (hasta.HasValue)
                {
                    DateTime fin = hasta.Value.Date.AddDays(1);
                    consulta = consulta.Where(m => m.FechaMovimiento < fin);
                }

                List<MovimientoStock> movimientos = consulta
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(Math.Max(1, maximo))
                    .ToList();

                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);
                Dictionary<Guid, UbicacionDeposito> ubicaciones = unidad.Conjunto<UbicacionDeposito>().ToList().ToDictionary(u => u.Id);

                return movimientos.Select(m =>
                {
                    Producto producto;
                    UbicacionDeposito ubicacion;
                    productos.TryGetValue(m.ProductoId, out producto);
                    ubicaciones.TryGetValue(m.UbicacionId, out ubicacion);

                    return new MovimientoDto
                    {
                        Id = m.Id,
                        Fecha = m.FechaMovimiento,
                        Sku = producto == null ? string.Empty : producto.CodigoSku,
                        Producto = producto == null ? string.Empty : producto.Nombre,
                        Ubicacion = ubicacion == null ? string.Empty : ubicacion.Codigo,
                        Tipo = Etiquetas.De(m.Tipo),
                        Cantidad = m.Cantidad,
                        SaldoFisico = m.SaldoFisicoResultante,
                        SaldoReservado = m.SaldoReservadoResultante,
                        Documento = m.DocumentoOrigen,
                        Lote = m.Lote,
                        Usuario = m.Usuario,
                        Observaciones = m.Observaciones
                    };
                }).ToList();
            });
        }

        // ---------------------------------------------------------------
        // Movimientos manuales
        // ---------------------------------------------------------------

        public void RegistrarMovimiento(MovimientoSolicitud solicitud)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (solicitud == null)
            {
                throw new ArgumentNullException(nameof(solicitud));
            }

            TipoMovimientoStock[] permitidos =
            {
                TipoMovimientoStock.AjustePositivo,
                TipoMovimientoStock.AjusteNegativo,
                TipoMovimientoStock.RecepcionCompra,
                TipoMovimientoStock.DevolucionCliente
            };

            if (Array.IndexOf(permitidos, solicitud.Tipo) < 0)
            {
                throw Mensajes.Regla("The movement type is not valid for this operation.");
            }

            bool esAjuste = solicitud.Tipo == TipoMovimientoStock.AjustePositivo
                || solicitud.Tipo == TipoMovimientoStock.AjusteNegativo;

            if (esAjuste && string.IsNullOrWhiteSpace(solicitud.Observaciones))
            {
                throw Mensajes.Regla("An adjustment must state the reason.");
            }

            MovimientoStock movimiento = _almacen.Ejecutar(unidad =>
                InventarioMotor.Mover(
                    unidad,
                    solicitud.ProductoId,
                    solicitud.UbicacionId,
                    solicitud.Tipo,
                    solicitud.Cantidad,
                    solicitud.CostoUnitario,
                    solicitud.Documento,
                    null,
                    solicitud.Lote,
                    solicitud.Colada,
                    solicitud.Observaciones,
                    false));

            new BitacoraService().RegistrarAuditoria(
                "Inventario", "RegistrarMovimiento", "MovimientoStock", movimiento.Id,
                solicitud.Tipo + " x " + solicitud.Cantidad.ToString("0.###"));
        }

        public void Transferir(Guid productoId, Guid ubicacionOrigenId, Guid ubicacionDestinoId, decimal cantidad, string observaciones)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (ubicacionOrigenId == ubicacionDestinoId)
            {
                throw Mensajes.Regla("The origin and destination locations must be different.");
            }

            _almacen.Ejecutar(unidad =>
            {
                string documento = "TRANSFERENCIA";

                InventarioMotor.Mover(unidad, productoId, ubicacionOrigenId,
                    TipoMovimientoStock.TransferenciaSalida, cantidad, null, documento, null,
                    string.Empty, string.Empty, observaciones, false);

                InventarioMotor.Mover(unidad, productoId, ubicacionDestinoId,
                    TipoMovimientoStock.TransferenciaEntrada, cantidad, null, documento, null,
                    string.Empty, string.Empty, observaciones, false);
            });

            new BitacoraService().RegistrarAuditoria(
                "Inventario", "Transferir", "Producto", productoId,
                "Transferencia de " + cantidad.ToString("0.###"));
        }

        // ---------------------------------------------------------------
        // Reservas y alertas
        // ---------------------------------------------------------------

        public List<ReservaDto> ObtenerReservas(bool soloActivas)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);
                Dictionary<Guid, UbicacionDeposito> ubicaciones = unidad.Conjunto<UbicacionDeposito>().ToList().ToDictionary(u => u.Id);

                return unidad.Conjunto<ReservaStock>().ToList()
                    .Where(r => !soloActivas || r.Estado == EstadoReserva.Activa)
                    .OrderByDescending(r => r.FechaReserva)
                    .Select(r =>
                    {
                        Producto producto;
                        UbicacionDeposito ubicacion;
                        productos.TryGetValue(r.ProductoId, out producto);
                        ubicaciones.TryGetValue(r.UbicacionId, out ubicacion);

                        return new ReservaDto
                        {
                            Id = r.Id,
                            Fecha = r.FechaReserva,
                            Sku = producto == null ? string.Empty : producto.CodigoSku,
                            Producto = producto == null ? string.Empty : producto.Nombre,
                            Ubicacion = ubicacion == null ? string.Empty : ubicacion.Codigo,
                            Reservada = r.CantidadReservada,
                            Consumida = r.CantidadConsumida,
                            Pendiente = r.CantidadPendiente,
                            Estado = Etiquetas.De(r.Estado),
                            Origen = r.OrdenFabricacionId.HasValue
                                ? Mensajes.T("Manufacturing order")
                                : Mensajes.T("Sales order")
                        };
                    }).ToList();
            });
        }

        public void LiberarReserva(Guid reservaId, string motivo)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for releasing the reservation is required.");
            }

            _almacen.Ejecutar(unidad =>
            {
                ReservaStock reserva = unidad.Conjunto<ReservaStock>().FirstOrDefault(r => r.Id == reservaId);

                if (reserva == null)
                {
                    throw Mensajes.Regla("The reservation does not exist.");
                }

                InventarioMotor.LiberarReserva(unidad, reserva, motivo);
            });

            new BitacoraService().RegistrarAuditoria(
                "Inventario", "LiberarReserva", "ReservaStock", reservaId, motivo);
        }

        /// <summary>
        /// Productos cuya existencia disponible llegó al punto de reposición.
        /// </summary>
        public List<AlertaReposicionDto> ObtenerAlertas()
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, decimal> disponibles = unidad.Conjunto<StockProducto>().ToList()
                    .GroupBy(s => s.ProductoId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.CantidadDisponible));

                List<AlertaReposicionDto> alertas = new List<AlertaReposicionDto>();

                foreach (Producto producto in unidad.Conjunto<Producto>().Where(p => p.Activo).ToList())
                {
                    if (producto.PuntoReposicion <= 0m)
                    {
                        continue;
                    }

                    decimal disponible;
                    disponibles.TryGetValue(producto.Id, out disponible);

                    if (disponible > producto.PuntoReposicion)
                    {
                        continue;
                    }

                    decimal objetivo = Math.Max(producto.PuntoReposicion * 2m, producto.StockMinimo);

                    alertas.Add(new AlertaReposicionDto
                    {
                        ProductoId = producto.Id,
                        Sku = producto.CodigoSku,
                        Producto = producto.Nombre,
                        Unidad = producto.UnidadMedida,
                        Disponible = disponible,
                        PuntoReposicion = producto.PuntoReposicion,
                        StockMinimo = producto.StockMinimo,
                        Sugerido = Math.Max(1m, objetivo - disponible),
                        Comprable = producto.EsComprable
                    });
                }

                return alertas.OrderBy(a => a.Disponible).ThenBy(a => a.Producto).ToList();
            });
        }
    }
}
