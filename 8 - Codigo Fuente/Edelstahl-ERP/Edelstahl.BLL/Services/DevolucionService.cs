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
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Logística inversa: devoluciones de mercadería, inspección, reingreso
    /// al inventario y base para la nota de crédito.
    /// </summary>
    public class DevolucionService
    {
        private readonly INegocioStore _almacen;

        public DevolucionService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public DevolucionService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        public Devolucion RegistrarDevolucion(Guid remitoId, string motivoGeneral, List<LineaDevolucionSolicitud> lineas)
        {
            Autorizacion.Exigir(Permiso.DevolucionesGestionar);

            if (lineas == null || lineas.Count == 0 || lineas.All(l => l.Cantidad <= 0m))
            {
                throw Mensajes.Regla("Select at least one item to return.");
            }

            if (string.IsNullOrWhiteSpace(motivoGeneral))
            {
                throw Mensajes.Regla("The reason for the return is required.");
            }

            Devolucion devolucion = _almacen.Ejecutar(unidad =>
            {
                RemitoEntrega remito = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles")
                    .FirstOrDefault(r => r.Id == remitoId);

                if (remito == null)
                {
                    throw Mensajes.Regla("The delivery note does not exist.");
                }

                if (remito.Estado != EstadoRemito.Entregado && remito.Estado != EstadoRemito.Rechazado)
                {
                    throw Mensajes.Regla("Only delivered or rejected delivery notes can originate a return.");
                }

                Dictionary<Guid, decimal> devueltas = CantidadesDevueltas(unidad, remitoId);
                List<string> numeros = unidad.Conjunto<Devolucion>().Select(d => d.Numero).ToList();

                Pedido origen = FactoryDataAccess.PedidoRepository.GetById(remito.PedidoId);

                Devolucion nueva = new Devolucion
                {
                    Numero = Numeracion.Siguiente("DEV", numeros),
                    RemitoId = remito.Id,
                    PedidoId = remito.PedidoId,
                    ClienteId = remito.ClienteId,
                    MotivoGeneral = motivoGeneral.Trim()
                };

                bool total = true;

                foreach (LineaDevolucionSolicitud solicitud in lineas.Where(l => l.Cantidad > 0m))
                {
                    DetalleRemito linea = remito.Detalles.FirstOrDefault(d => d.Id == solicitud.DetalleRemitoId);

                    if (linea == null)
                    {
                        throw Mensajes.Regla("The item does not belong to the delivery note.");
                    }

                    // Lo fabricado a medida no admite devolucion.
                    DetallePedido detallePedido = origen == null ? null : origen.Detalles.FirstOrDefault(d => d.Id == linea.DetallePedidoId);

                    if (detallePedido != null && (detallePedido.RequiereFabricacion || detallePedido.TipoItem == TipoItemPresupuesto.FabricacionEspecial))
                    {
                        throw Mensajes.Regla("{0} was made to measure and cannot be returned.", linea.Codigo);
                    }

                    decimal yaDevuelta;
                    devueltas.TryGetValue(linea.DetallePedidoId, out yaDevuelta);

                    if (solicitud.Cantidad + yaDevuelta > linea.Cantidad)
                    {
                        throw Mensajes.Regla(
                            "The quantity of {0} exceeds what was delivered ({1}).",
                            linea.Codigo, (linea.Cantidad - yaDevuelta).ToString("0.###"));
                    }

                    if (solicitud.Cantidad + yaDevuelta < linea.Cantidad)
                    {
                        total = false;
                    }

                    nueva.Detalles.Add(new DetalleDevolucion
                    {
                        DevolucionId = nueva.Id,
                        DetallePedidoId = linea.DetallePedidoId,
                        Codigo = linea.Codigo,
                        Descripcion = linea.Descripcion,
                        CantidadDevuelta = solicitud.Cantidad,
                        Motivo = string.IsNullOrWhiteSpace(solicitud.Motivo) ? motivoGeneral.Trim() : solicitud.Motivo.Trim()
                    });
                }

                if (remito.Detalles.Count != nueva.Detalles.Count)
                {
                    total = false;
                }

                nueva.EsTotal = total;
                unidad.Conjunto<Devolucion>().Agregar(nueva);

                return nueva;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "RegistrarDevolucion", "Devolucion", devolucion.Id, devolucion.Numero);

            return devolucion;
        }

        private static Dictionary<Guid, decimal> CantidadesDevueltas(IUnidadDeTrabajo unidad, Guid remitoId)
        {
            return unidad.Conjunto<Devolucion>().Incluir("Detalles").ToList()
                .Where(d => d.RemitoId == remitoId
                    && d.Estado != EstadoDevolucion.Cancelada
                    && d.Estado != EstadoDevolucion.Rechazada)
                .SelectMany(d => d.Detalles)
                .GroupBy(d => d.DetallePedidoId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.CantidadDevuelta));
        }

        public void RecibirDevolucion(Guid devolucionId)
        {
            Autorizacion.Exigir(Permiso.DevolucionesGestionar);

            Devolucion devolucion = _almacen.Ejecutar(unidad =>
            {
                Devolucion existente = unidad.Conjunto<Devolucion>().FirstOrDefault(d => d.Id == devolucionId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The return does not exist.");
                }

                if (existente.Estado != EstadoDevolucion.Solicitada && existente.Estado != EstadoDevolucion.EnTransito)
                {
                    throw Mensajes.Regla("The return was already received or closed.");
                }

                existente.Estado = EstadoDevolucion.Recibida;
                existente.FechaRecepcion = DateTime.Now;
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "RecibirDevolucion", "Devolucion", devolucion.Id, devolucion.Numero);
        }

        public void CancelarDevolucion(Guid devolucionId, string motivo)
        {
            Autorizacion.Exigir(Permiso.DevolucionesGestionar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for cancelling is required.");
            }

            Devolucion devolucion = _almacen.Ejecutar(unidad =>
            {
                Devolucion existente = unidad.Conjunto<Devolucion>().FirstOrDefault(d => d.Id == devolucionId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The return does not exist.");
                }

                if (existente.Estado != EstadoDevolucion.Solicitada && existente.Estado != EstadoDevolucion.EnTransito)
                {
                    throw Mensajes.Regla("The return cannot be cancelled in its current state.");
                }

                existente.Estado = EstadoDevolucion.Cancelada;
                existente.Observaciones = motivo.Trim();
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "CancelarDevolucion", "Devolucion", devolucion.Id, devolucion.Numero + ": " + motivo);
        }

        /// <summary>
        /// Inspecciona un ítem devuelto y ejecuta su resolución: reingreso al
        /// stock, reproceso, reemplazo o descarte.
        /// </summary>
        public void ResolverDetalle(ResolucionDevolucion resolucion)
        {
            Autorizacion.Exigir(Permiso.DevolucionesGestionar);

            if (resolucion == null)
            {
                throw new ArgumentNullException(nameof(resolucion));
            }

            if (resolucion.CantidadReingresada < 0m || resolucion.CantidadDescartada < 0m)
            {
                throw Mensajes.Regla("Quantities cannot be negative.");
            }

            Devolucion devolucion = _almacen.Ejecutar(unidad =>
            {
                Devolucion existente = unidad.Conjunto<Devolucion>().Incluir("Detalles").ToList()
                    .FirstOrDefault(d => d.Detalles.Any(x => x.Id == resolucion.DetalleDevolucionId));

                if (existente == null)
                {
                    throw Mensajes.Regla("The returned item does not exist.");
                }

                if (existente.Estado != EstadoDevolucion.Recibida && existente.Estado != EstadoDevolucion.EnInspeccion)
                {
                    throw Mensajes.Regla("The return must be received before it can be inspected.");
                }

                DetalleDevolucion detalle = existente.Detalles.First(x => x.Id == resolucion.DetalleDevolucionId);

                if (detalle.Estado == "RESUELTO")
                {
                    throw Mensajes.Regla("This item was already resolved.");
                }

                AplicarResolucion(unidad, existente, detalle, resolucion);

                existente.Estado = existente.Detalles.All(x => x.Estado == "RESUELTO")
                    ? EstadoDevolucion.Resuelta
                    : EstadoDevolucion.EnInspeccion;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "ResolverDevolucion", "Devolucion", devolucion.Id,
                devolucion.Numero + ": " + resolucion.Accion);
        }

        private static void AplicarResolucion(
            IUnidadDeTrabajo unidad,
            Devolucion devolucion,
            DetalleDevolucion detalle,
            ResolucionDevolucion r)
        {
            decimal devuelta = detalle.CantidadDevuelta;

            if (r.CantidadReingresada + r.CantidadDescartada > devuelta)
            {
                throw Mensajes.Regla("The quantity returned to stock plus the quantity scrapped cannot exceed the quantity returned.");
            }

            switch (r.Accion)
            {
                case AccionResuelta.ReingresoStock:
                    if (!r.AptoReingreso || r.CantidadReingresada <= 0m)
                    {
                        throw Mensajes.Regla("Only items approved by the inspection can return to stock.");
                    }

                    break;

                case AccionResuelta.Descarte:
                    r.CantidadDescartada = devuelta;
                    r.CantidadReingresada = 0m;
                    break;

                case AccionResuelta.Reproceso:
                case AccionResuelta.Reemplazo:
                case AccionResuelta.SinAccion:
                    break;
            }

            Producto producto = null;
            string sku = detalle.Codigo;

            if (r.CantidadReingresada > 0m || r.Accion == AccionResuelta.Reproceso)
            {
                producto = unidad.Conjunto<Producto>().FirstOrDefault(p => p.CodigoSku == sku);

                if (producto == null)
                {
                    throw Mensajes.Regla("The product {0} is not in the catalog.", sku);
                }
            }

            if (r.CantidadReingresada > 0m)
            {
                UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, producto);

                InventarioMotor.Mover(
                    unidad, producto.Id, ubicacion.Id, TipoMovimientoStock.DevolucionCliente,
                    r.CantidadReingresada, null, devolucion.Numero, devolucion.Id,
                    string.Empty, string.Empty, r.Observaciones, false);
            }

            if (r.Accion == AccionResuelta.Reproceso)
            {
                decimal aReprocesar = devuelta - r.CantidadReingresada - r.CantidadDescartada;

                if (aReprocesar <= 0m)
                {
                    throw Mensajes.Regla("There is nothing left to rework.");
                }

                ProduccionService.GenerarOrdenReproceso(unidad, producto.Id, aReprocesar, devolucion.PedidoId,
                        "Reproceso de la devolución " + devolucion.Numero);
            }

            detalle.Accion = r.Accion;
            detalle.CantidadReingresada = r.CantidadReingresada;
            detalle.CantidadDescartada = r.CantidadDescartada;
            detalle.Estado = "RESUELTO";
            detalle.Observaciones = r.Observaciones ?? string.Empty;

            detalle.Inspecciones.Add(new InspeccionDevolucion
            {
                DetalleDevolucionId = detalle.Id,
                UsuarioInspector = SesionActual.NombreUsuario,
                RequiereReproceso = r.Accion == AccionResuelta.Reproceso,
                RequiereReemplazo = r.Accion == AccionResuelta.Reemplazo,
                AptoReingresoStock = r.AptoReingreso,
                Resultado = r.AptoReingreso ? "APROBADA" : "NO_RECUPERABLE",
                CondicionMaterial = r.Condicion ?? string.Empty,
                Observaciones = r.Observaciones ?? string.Empty
            });
        }

        // ---------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------

        public List<DevolucionDto> ObtenerDevoluciones(EstadoDevolucion? estado)
        {
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, RemitoEntrega> remitos = unidad.Conjunto<RemitoEntrega>().ToList().ToDictionary(r => r.Id);

                return unidad.Conjunto<Devolucion>().Incluir("Detalles").ToList()
                    .Where(d => !estado.HasValue || d.Estado == estado.Value)
                    .OrderByDescending(d => d.FechaSolicitud)
                    .Select(d =>
                    {
                        RemitoEntrega remito;
                        Pedido pedido;
                        remitos.TryGetValue(d.RemitoId, out remito);
                        pedidos.TryGetValue(d.PedidoId, out pedido);

                        return new DevolucionDto
                        {
                            Id = d.Id,
                            Numero = d.Numero,
                            Remito = remito == null ? string.Empty : remito.Numero,
                            Pedido = pedido == null ? string.Empty : pedido.Numero,
                            Cliente = Referencias.Nombre(clientes, d.ClienteId),
                            Fecha = d.FechaSolicitud,
                            Estado = Etiquetas.De(d.Estado),
                            EstadoClave = d.Estado,
                            Tipo = d.EsTotal ? Mensajes.T("Total") : Mensajes.T("Partial"),
                            Items = d.Detalles.Count
                        };
                    }).ToList();
            });
        }

        public List<DetalleDevolucionDto> ObtenerDetalles(Guid devolucionId)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Devolucion devolucion = unidad.Conjunto<Devolucion>().Incluir("Detalles")
                    .FirstOrDefault(d => d.Id == devolucionId);

                if (devolucion == null)
                {
                    return new List<DetalleDevolucionDto>();
                }

                return devolucion.Detalles.Select(d => new DetalleDevolucionDto
                {
                    Id = d.Id,
                    Codigo = d.Codigo,
                    Descripcion = d.Descripcion,
                    Devuelta = d.CantidadDevuelta,
                    Reingresada = d.CantidadReingresada,
                    Descartada = d.CantidadDescartada,
                    Accion = Etiquetas.De(d.Accion),
                    Estado = d.Estado == "RESUELTO" ? Mensajes.T("Resolved") : Mensajes.T("Pending"),
                    Motivo = d.Motivo
                }).ToList();
            });
        }

        /// <summary>
        /// Calcula el importe neto de la nota de crédito que corresponde a una
        /// devolución resuelta, valuando cada ítem al precio facturado.
        /// </summary>
        public NotaCreditoSugerida CalcularNotaCreditoSugerida(Guid devolucionId)
        {
            Devolucion devolucion = _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Devolucion>().Incluir("Detalles").FirstOrDefault(d => d.Id == devolucionId));

            if (devolucion == null)
            {
                throw Mensajes.Regla("The return does not exist.");
            }

            if (devolucion.Estado != EstadoDevolucion.Resuelta)
            {
                throw Mensajes.Regla("A credit note requires an approved resolution of the return.");
            }

            Factura factura = new FacturacionService(_almacen).ObtenerFacturaDePedido(devolucion.PedidoId);

            if (factura == null)
            {
                throw Mensajes.Regla("The order has no invoice to credit.");
            }

            decimal neto = 0m;

            foreach (DetalleDevolucion detalle in devolucion.Detalles.Where(d => d.Accion != AccionResuelta.Reemplazo))
            {
                DetalleFactura linea = factura.Detalles.FirstOrDefault(l => l.Codigo == detalle.Codigo);

                if (linea == null)
                {
                    continue;
                }

                decimal unitario = linea.PrecioUnitario * (1m - linea.PorcentajeDescuento / 100m);
                neto += unitario * detalle.CantidadDevuelta;
            }

            return new NotaCreditoSugerida
            {
                FacturaId = factura.Id,
                Factura = factura.Numero,
                MontoNeto = Math.Round(neto, 2)
            };
        }
    }
}
