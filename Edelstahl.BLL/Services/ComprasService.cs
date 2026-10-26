using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Proveedores, órdenes de compra y recepción de materiales.
    /// </summary>
    public class ComprasService
    {
        public const decimal PorcentajeIvaCompra = 21m;

        private readonly INegocioStore _almacen;

        public ComprasService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public ComprasService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Proveedores
        // ---------------------------------------------------------------

        public List<Proveedor> ObtenerProveedores(string texto, bool soloActivos)
        {
            string buscado = (texto ?? string.Empty).Trim().ToLowerInvariant();

            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Proveedor>().ToList()
                    .Where(p => (!soloActivos || p.Activo)
                        && (buscado.Length == 0
                            || p.RazonSocial.ToLowerInvariant().Contains(buscado)
                            || p.Cuit.Contains(buscado)
                            || (p.NombreFantasia ?? string.Empty).ToLowerInvariant().Contains(buscado)))
                    .OrderBy(p => p.RazonSocial)
                    .ToList());
        }

        public Proveedor GuardarProveedor(Proveedor proveedor)
        {
            Autorizacion.Exigir(Permiso.ProveedoresGestionar);

            if (proveedor == null)
            {
                throw new ArgumentNullException(nameof(proveedor));
            }

            string cuit = SoloDigitos(proveedor.Cuit);

            if (cuit.Length != 11)
            {
                throw Mensajes.Regla("The CUIT must contain 11 digits.");
            }

            string razon = (proveedor.RazonSocial ?? string.Empty).Trim();

            if (razon.Length < 3 || razon.Length > 150)
            {
                throw Mensajes.Regla("The business name must have between 3 and 150 characters.");
            }

            if (proveedor.LimiteCredito < 0m)
            {
                throw Mensajes.Regla("The credit limit cannot be negative.");
            }

            if (proveedor.PlazoEntregaDias < 0)
            {
                throw Mensajes.Regla("The delivery term cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(proveedor.Email))
            {
                try
                {
                    new MailAddress(proveedor.Email.Trim());
                }
                catch (FormatException)
                {
                    throw Mensajes.Regla("The email address is not valid.");
                }
            }

            Proveedor resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<Proveedor> conjunto = unidad.Conjunto<Proveedor>();

                if (conjunto.Any(p => p.Cuit == cuit && p.Id != proveedor.Id))
                {
                    throw Mensajes.Regla("There is already a supplier with the CUIT {0}.", cuit);
                }

                Proveedor existente = conjunto.FirstOrDefault(p => p.Id == proveedor.Id);

                if (existente == null)
                {
                    existente = new Proveedor { Id = proveedor.Id };
                    conjunto.Agregar(existente);
                }

                existente.Cuit = cuit;
                existente.RazonSocial = razon;
                existente.NombreFantasia = proveedor.NombreFantasia ?? string.Empty;
                existente.Direccion = proveedor.Direccion ?? string.Empty;
                existente.Localidad = proveedor.Localidad ?? string.Empty;
                existente.Provincia = proveedor.Provincia ?? string.Empty;
                existente.CodigoPostal = proveedor.CodigoPostal ?? string.Empty;
                existente.Email = (proveedor.Email ?? string.Empty).Trim();
                existente.Telefono = proveedor.Telefono ?? string.Empty;
                existente.ContactoComercial = proveedor.ContactoComercial ?? string.Empty;
                existente.LimiteCredito = proveedor.LimiteCredito;
                existente.PlazoEntregaDias = proveedor.PlazoEntregaDias;
                existente.Activo = proveedor.Activo;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Compras", "GuardarProveedor", "Proveedor", resultado.Id, resultado.RazonSocial);

            return resultado;
        }

        private static string SoloDigitos(string valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        // ---------------------------------------------------------------
        // Órdenes de compra
        // ---------------------------------------------------------------

        public List<OrdenCompraDto> ObtenerOrdenes(Guid? proveedorId, EstadoOrdenCompra? estado)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Proveedor> proveedores = unidad.Conjunto<Proveedor>().ToList().ToDictionary(p => p.Id);

                return unidad.Conjunto<OrdenCompra>().ToList()
                    .Where(o => (!proveedorId.HasValue || o.ProveedorId == proveedorId.Value)
                        && (!estado.HasValue || o.Estado == estado.Value))
                    .OrderByDescending(o => o.FechaEmision)
                    .Select(o => new OrdenCompraDto
                    {
                        Id = o.Id,
                        Numero = o.Numero,
                        ProveedorId = o.ProveedorId,
                        Proveedor = proveedores.ContainsKey(o.ProveedorId) ? proveedores[o.ProveedorId].RazonSocial : string.Empty,
                        Fecha = o.FechaEmision,
                        EntregaEstimada = o.FechaEntregaEstimada,
                        Estado = Etiquetas.De(o.Estado),
                        Neto = o.MontoNeto,
                        Total = o.MontoTotal,
                        SaldoPendiente = o.SaldoPendiente
                    }).ToList();
            });
        }

        public OrdenCompra ObtenerOrden(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<OrdenCompra>().Incluir("Detalles").FirstOrDefault(o => o.Id == ordenId));
        }

        public List<DetalleOrdenCompraDto> ObtenerDetalles(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad =>
            {
                OrdenCompra orden = unidad.Conjunto<OrdenCompra>().Incluir("Detalles")
                    .FirstOrDefault(o => o.Id == ordenId);

                if (orden == null)
                {
                    return new List<DetalleOrdenCompraDto>();
                }

                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);

                return orden.Detalles.Select(d =>
                {
                    Producto producto;
                    productos.TryGetValue(d.ProductoId, out producto);

                    return new DetalleOrdenCompraDto
                    {
                        DetalleId = d.Id,
                        ProductoId = d.ProductoId,
                        Sku = producto == null ? string.Empty : producto.CodigoSku,
                        Producto = producto == null ? string.Empty : producto.Nombre,
                        Unidad = d.UnidadMedida,
                        Solicitada = d.CantidadSolicitada,
                        Recibida = d.CantidadRecibida,
                        Pendiente = d.Pendiente,
                        Costo = d.CostoUnitario,
                        Descuento = d.PorcentajeDescuento,
                        Subtotal = d.Subtotal
                    };
                }).ToList();
            });
        }

        public OrdenCompra CrearOrden(
            Guid proveedorId,
            DateTime? entregaEstimada,
            string condicionPago,
            string observaciones,
            List<ItemCompra> items,
            bool emitir)
        {
            Autorizacion.Exigir(Permiso.ComprasGestionar);

            if (items == null || items.Count == 0)
            {
                throw Mensajes.Regla("The purchase order must have at least one item.");
            }

            if (entregaEstimada.HasValue && entregaEstimada.Value.Date < DateTime.Today)
            {
                throw Mensajes.Regla("The estimated delivery date cannot be earlier than the issue date.");
            }

            OrdenCompra resultado = _almacen.Ejecutar(unidad =>
            {
                Proveedor proveedor = unidad.Conjunto<Proveedor>().FirstOrDefault(p => p.Id == proveedorId);

                if (proveedor == null || !proveedor.Activo)
                {
                    throw Mensajes.Regla("Only active suppliers can receive purchase orders.");
                }

                List<string> numeros = unidad.Conjunto<OrdenCompra>().Select(o => o.Numero).ToList();

                OrdenCompra orden = new OrdenCompra
                {
                    Numero = Numeracion.Siguiente("OC", numeros),
                    ProveedorId = proveedorId,
                    FechaEntregaEstimada = entregaEstimada,
                    CondicionPago = condicionPago ?? string.Empty,
                    Observaciones = observaciones ?? string.Empty,
                    Estado = EstadoOrdenCompra.Borrador
                };

                foreach (ItemCompra item in items)
                {
                    if (item.Cantidad <= 0m)
                    {
                        throw Mensajes.Regla("The quantity must be greater than zero.");
                    }

                    if (item.Costo < 0m || item.Descuento < 0m || item.Descuento > 100m)
                    {
                        throw Mensajes.Regla("The cost or the discount is not valid.");
                    }

                    Producto producto = InventarioMotor.ObtenerProducto(unidad, item.ProductoId);

                    if (!producto.Activo || !producto.EsComprable)
                    {
                        throw Mensajes.Regla("The product {0} cannot be purchased.", producto.CodigoSku);
                    }

                    DetalleOrdenCompra existente = orden.Detalles.FirstOrDefault(d => d.ProductoId == item.ProductoId);

                    if (existente != null)
                    {
                        existente.CantidadSolicitada += item.Cantidad;
                        continue;
                    }

                    orden.Detalles.Add(new DetalleOrdenCompra
                    {
                        OrdenCompraId = orden.Id,
                        ProductoId = item.ProductoId,
                        CantidadSolicitada = item.Cantidad,
                        UnidadMedida = producto.UnidadMedida,
                        CostoUnitario = item.Costo,
                        PorcentajeDescuento = item.Descuento
                    });
                }

                orden.Recalcular(PorcentajeIvaCompra);
                orden.SaldoPendiente = 0m;

                unidad.Conjunto<OrdenCompra>().Agregar(orden);
                unidad.Guardar();

                if (emitir)
                {
                    Emitir(unidad, orden, proveedor);
                }

                return orden;
            });

            new BitacoraService().RegistrarAuditoria(
                "Compras", emitir ? "EmitirOrdenCompra" : "CrearOrdenCompra", "OrdenCompra", resultado.Id,
                resultado.Numero + " por " + resultado.MontoTotal.ToString("N2"));

            return resultado;
        }

        public void EmitirOrden(Guid ordenId)
        {
            Autorizacion.Exigir(Permiso.ComprasGestionar);

            OrdenCompra orden = _almacen.Ejecutar(unidad =>
            {
                OrdenCompra existente = unidad.Conjunto<OrdenCompra>().Incluir("Detalles")
                    .FirstOrDefault(o => o.Id == ordenId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The purchase order does not exist.");
                }

                if (existente.Estado != EstadoOrdenCompra.Borrador)
                {
                    throw Mensajes.Regla("Only draft purchase orders can be issued.");
                }

                Proveedor proveedor = unidad.Conjunto<Proveedor>().First(p => p.Id == existente.ProveedorId);

                if (!proveedor.Activo)
                {
                    throw Mensajes.Regla("Only active suppliers can receive purchase orders.");
                }

                Emitir(unidad, existente, proveedor);
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Compras", "EmitirOrdenCompra", "OrdenCompra", orden.Id, orden.Numero);
        }

        private static void Emitir(IUnidadDeTrabajo unidad, OrdenCompra orden, Proveedor proveedor)
        {
            if (proveedor.LimiteCredito > 0m && proveedor.SaldoCuenta + orden.MontoTotal > proveedor.LimiteCredito)
            {
                throw Mensajes.Regla(
                    "The purchase would exceed the credit limit of the supplier (balance {0}, limit {1}).",
                    proveedor.SaldoCuenta.ToString("N2"),
                    proveedor.LimiteCredito.ToString("N2"));
            }

            orden.Estado = EstadoOrdenCompra.Emitida;
            orden.SaldoPendiente = orden.MontoTotal;
            proveedor.SaldoCuenta += orden.MontoTotal;

            unidad.Conjunto<MovimientoCuentaProveedor>().Agregar(new MovimientoCuentaProveedor
            {
                ProveedorId = proveedor.Id,
                OrdenCompraId = orden.Id,
                Tipo = TipoMovimientoCuenta.OrdenCompra,
                Haber = orden.MontoTotal,
                SaldoResultante = proveedor.SaldoCuenta,
                Referencia = orden.Numero
            });
        }

        public void CancelarOrden(Guid ordenId, string motivo)
        {
            Autorizacion.Exigir(Permiso.ComprasGestionar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for cancelling is required.");
            }

            OrdenCompra orden = _almacen.Ejecutar(unidad =>
            {
                OrdenCompra existente = unidad.Conjunto<OrdenCompra>().Incluir("Detalles")
                    .FirstOrDefault(o => o.Id == ordenId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The purchase order does not exist.");
                }

                if (existente.Estado != EstadoOrdenCompra.Borrador && existente.Estado != EstadoOrdenCompra.Emitida)
                {
                    throw Mensajes.Regla("The purchase order cannot be cancelled in its current state.");
                }

                if (existente.Detalles.Any(d => d.CantidadRecibida > 0m))
                {
                    throw Mensajes.Regla("The purchase order already has received materials.");
                }

                if (existente.Estado == EstadoOrdenCompra.Emitida)
                {
                    if (existente.SaldoPendiente != existente.MontoTotal)
                    {
                        throw Mensajes.Regla("The purchase order already has payments applied.");
                    }

                    Proveedor proveedor = unidad.Conjunto<Proveedor>().First(p => p.Id == existente.ProveedorId);
                    proveedor.SaldoCuenta = Math.Max(0m, proveedor.SaldoCuenta - existente.MontoTotal);

                    unidad.Conjunto<MovimientoCuentaProveedor>().Agregar(new MovimientoCuentaProveedor
                    {
                        ProveedorId = proveedor.Id,
                        OrdenCompraId = existente.Id,
                        Tipo = TipoMovimientoCuenta.Ajuste,
                        Debe = existente.MontoTotal,
                        SaldoResultante = proveedor.SaldoCuenta,
                        Referencia = existente.Numero,
                        Observaciones = motivo
                    });
                }

                existente.Estado = EstadoOrdenCompra.Cancelada;
                existente.SaldoPendiente = 0m;
                existente.Observaciones = (existente.Observaciones + " | " + motivo).Trim(' ', '|');

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Compras", "CancelarOrdenCompra", "OrdenCompra", orden.Id, orden.Numero + ": " + motivo);
        }

        public List<SugerenciaCompraDto> SugerirCompras()
        {
            List<AlertaReposicionDto> alertas = new InventarioService(_almacen).ObtenerAlertas();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);

                return alertas.Where(a => a.Comprable).Select(a => new SugerenciaCompraDto
                {
                    ProductoId = a.ProductoId,
                    Sku = a.Sku,
                    Producto = a.Producto,
                    Unidad = a.Unidad,
                    Disponible = a.Disponible,
                    PuntoReposicion = a.PuntoReposicion,
                    Sugerido = a.Sugerido,
                    CostoActual = productos.ContainsKey(a.ProductoId) ? productos[a.ProductoId].CostoActual : 0m
                }).ToList();
            });
        }

        // ---------------------------------------------------------------
        // Recepción de materiales
        // ---------------------------------------------------------------

        public RecepcionCompra RegistrarRecepcion(
            Guid ordenId,
            string remitoProveedor,
            List<RecepcionItem> items,
            string observaciones)
        {
            Autorizacion.Exigir(Permiso.ComprasGestionar);

            if (items == null || items.Count == 0 || items.All(i => i.Recibida <= 0m))
            {
                throw Mensajes.Regla("There is nothing to receive.");
            }

            RecepcionCompra resultado = _almacen.Ejecutar(unidad =>
            {
                OrdenCompra orden = unidad.Conjunto<OrdenCompra>().Incluir("Detalles")
                    .FirstOrDefault(o => o.Id == ordenId);

                if (orden == null)
                {
                    throw Mensajes.Regla("The purchase order does not exist.");
                }

                if (orden.Estado != EstadoOrdenCompra.Emitida && orden.Estado != EstadoOrdenCompra.ParcialmenteRecibida)
                {
                    throw Mensajes.Regla("Materials can only be received against an issued purchase order.");
                }

                List<string> numeros = unidad.Conjunto<RecepcionCompra>().Select(r => r.Numero).ToList();

                RecepcionCompra recepcion = new RecepcionCompra
                {
                    Numero = Numeracion.Siguiente("REC", numeros),
                    OrdenCompraId = orden.Id,
                    NumeroRemitoProveedor = remitoProveedor ?? string.Empty,
                    RecibidoPor = SesionActual.NombreUsuario,
                    Observaciones = observaciones ?? string.Empty
                };

                foreach (RecepcionItem item in items.Where(i => i.Recibida > 0m || i.Aceptada > 0m || i.Rechazada > 0m))
                {
                    DetalleOrdenCompra detalle = orden.Detalles.FirstOrDefault(d => d.Id == item.DetalleOrdenCompraId);

                    if (detalle == null)
                    {
                        throw Mensajes.Regla("The item does not belong to the purchase order.");
                    }

                    if (item.Recibida < 0m || item.Aceptada < 0m || item.Rechazada < 0m)
                    {
                        throw Mensajes.Regla("Quantities cannot be negative.");
                    }

                    if (item.Recibida != item.Aceptada + item.Rechazada)
                    {
                        throw Mensajes.Regla("The received quantity must equal the accepted plus the rejected quantity.");
                    }

                    if (item.Aceptada > detalle.Pendiente)
                    {
                        throw Mensajes.Regla("The accepted quantity exceeds what is pending on the order.");
                    }

                    if (item.Rechazada > 0m && string.IsNullOrWhiteSpace(item.MotivoRechazo))
                    {
                        throw Mensajes.Regla("The reason for the rejection is required.");
                    }

                    recepcion.Detalles.Add(new DetalleRecepcionCompra
                    {
                        RecepcionId = recepcion.Id,
                        DetalleOrdenCompraId = detalle.Id,
                        ProductoId = detalle.ProductoId,
                        FechaVencimiento = item.Vencimiento,
                        CantidadRecibida = item.Recibida,
                        CantidadAceptada = item.Aceptada,
                        CantidadRechazada = item.Rechazada,
                        MotivoRechazo = item.MotivoRechazo ?? string.Empty,
                        EstadoMaterial = item.Rechazada > 0m ? "CON_RECHAZO" : "CONFORME",
                        NumeroLote = item.Lote ?? string.Empty,
                        CertificadoColada = item.Colada ?? string.Empty
                    });
                }

                unidad.Conjunto<RecepcionCompra>().Agregar(recepcion);
                unidad.Guardar();

                foreach (DetalleRecepcionCompra linea in recepcion.Detalles)
                {
                    if (linea.CantidadAceptada <= 0m)
                    {
                        continue;
                    }

                    DetalleOrdenCompra detalle = orden.Detalles.First(d => d.Id == linea.DetalleOrdenCompraId);
                    Producto producto = InventarioMotor.ObtenerProducto(unidad, linea.ProductoId);
                    UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, producto);

                    decimal costo = detalle.CostoUnitario * (1m - detalle.PorcentajeDescuento / 100m);

                    InventarioMotor.Mover(
                        unidad, producto.Id, ubicacion.Id, TipoMovimientoStock.RecepcionCompra,
                        linea.CantidadAceptada, costo, recepcion.Numero, recepcion.Id,
                        linea.NumeroLote, linea.CertificadoColada, string.Empty, false);

                    detalle.CantidadRecibida += linea.CantidadAceptada;
                    detalle.Estado = detalle.Pendiente <= 0m ? "RECIBIDO" : "PARCIAL";
                }

                orden.Estado = orden.Detalles.All(d => d.Pendiente <= 0m)
                    ? EstadoOrdenCompra.Recibida
                    : EstadoOrdenCompra.ParcialmenteRecibida;

                return recepcion;
            });

            new BitacoraService().RegistrarAuditoria(
                "Compras", "RegistrarRecepcion", "RecepcionCompra", resultado.Id, resultado.Numero);

            return resultado;
        }

        public List<RecepcionDto> ObtenerRecepciones(Guid? ordenId)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, OrdenCompra> ordenes = unidad.Conjunto<OrdenCompra>().ToList().ToDictionary(o => o.Id);
                Dictionary<Guid, Proveedor> proveedores = unidad.Conjunto<Proveedor>().ToList().ToDictionary(p => p.Id);

                return unidad.Conjunto<RecepcionCompra>().Incluir("Detalles").ToList()
                    .Where(r => !ordenId.HasValue || r.OrdenCompraId == ordenId.Value)
                    .OrderByDescending(r => r.FechaRecepcion)
                    .Select(r =>
                    {
                        OrdenCompra orden;
                        ordenes.TryGetValue(r.OrdenCompraId, out orden);
                        Proveedor proveedor = null;

                        if (orden != null)
                        {
                            proveedores.TryGetValue(orden.ProveedorId, out proveedor);
                        }

                        return new RecepcionDto
                        {
                            Id = r.Id,
                            Numero = r.Numero,
                            OrdenCompra = orden == null ? string.Empty : orden.Numero,
                            Proveedor = proveedor == null ? string.Empty : proveedor.RazonSocial,
                            Fecha = r.FechaRecepcion,
                            RemitoProveedor = r.NumeroRemitoProveedor,
                            Aceptado = r.Detalles.Sum(d => d.CantidadAceptada),
                            Rechazado = r.Detalles.Sum(d => d.CantidadRechazada),
                            RecibidoPor = r.RecibidoPor
                        };
                    }).ToList();
            });
        }
    }
}
