using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Produccion;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Órdenes de fabricación: generación, reserva de materiales, avance,
    /// consumos, control de calidad y finalización.
    /// </summary>
    public class ProduccionService
    {
        private readonly INegocioStore _almacen;

        public ProduccionService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public ProduccionService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Pedidos que requieren fabricación
        // ---------------------------------------------------------------

        public List<PedidoPendienteFabricacionDto> ObtenerPedidosPendientes()
        {
            List<Pedido> pedidos = FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado == EstadoPedido.Confirmado
                    || p.Estado == EstadoPedido.EnPreparacion
                    || p.Estado == EstadoPedido.EnProduccion)
                .ToList();

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            HashSet<Guid> conOrden = _almacen.Ejecutar(unidad =>
                new HashSet<Guid>(
                    unidad.Conjunto<OrdenFabricacion>()
                        .Where(o => o.Estado != EstadoProduccion.Cancelada && o.DetallePedidoId != null)
                        .Select(o => o.DetallePedidoId.Value)
                        .ToList()));

            List<PedidoPendienteFabricacionDto> resultado = new List<PedidoPendienteFabricacionDto>();

            foreach (Pedido pedido in pedidos)
            {
                foreach (DetallePedido detalle in pedido.Detalles.Where(d => d.RequiereFabricacion))
                {
                    if (conOrden.Contains(detalle.Id))
                    {
                        continue;
                    }

                    resultado.Add(new PedidoPendienteFabricacionDto
                    {
                        PedidoId = pedido.Id,
                        DetallePedidoId = detalle.Id,
                        Pedido = pedido.Numero,
                        Cliente = Referencias.Nombre(clientes, pedido.ClienteId),
                        Codigo = detalle.Codigo,
                        Descripcion = detalle.Descripcion,
                        Especificaciones = detalle.DescripcionTecnica,
                        Cantidad = detalle.Cantidad,
                        PlazoEntrega = pedido.PlazoEntrega
                    });
                }
            }

            return resultado.OrderBy(r => r.Pedido).ToList();
        }

        // ---------------------------------------------------------------
        // Generación
        // ---------------------------------------------------------------

        /// <summary>
        /// Crea una orden de fabricacion por cada linea a medida del pedido que todavia no tiene una.
        /// Usa como material sugerido la materia prima con mas existencia (Produccion puede ajustarlo despues).
        /// </summary>
        public List<string> GenerarOrdenesDelPedido(Guid pedidoId)
        {
            List<string> numeros = new List<string>();
            List<PedidoPendienteFabricacionDto> pendientes = ObtenerPedidosPendientes().Where(p => p.PedidoId == pedidoId).ToList();

            if (pendientes.Count == 0)
            {
                return numeros;
            }

            List<ProductoListaDto> materiales = new CatalogoService(_almacen)
                .BuscarProductos(string.Empty, null, TipoProducto.MateriaPrima, true, false);

            if (materiales.Count == 0)
            {
                return numeros;
            }

            foreach (PedidoPendienteFabricacionDto linea in pendientes)
            {
                ProductoListaDto material = ElegirMaterial(materiales, linea);
                ResultadoGeneracionOrden r = GenerarOrden(pedidoId, linea.DetallePedidoId, PrioridadOrden.Normal, DateTime.Today.AddDays(7),
                    new List<MaterialRequerido> { new MaterialRequerido { ProductoId = material.Id, Cantidad = linea.Cantidad } },
                    string.Empty, Mensajes.T("Created automatically when the order was confirmed."));
                numeros.Add(r.Orden.Numero);
            }

            return numeros;
        }

        /// <summary>
        /// Sugiere la materia prima que corresponde al producto: misma calidad de acero, misma forma
        /// (chapa o cano) y mismo diametro cuando figura en la descripcion; a igualdad, la de mas existencia.
        /// </summary>
        private static ProductoListaDto ElegirMaterial(List<ProductoListaDto> materiales, PedidoPendienteFabricacionDto linea)
        {
            string texto = ((linea.Descripcion ?? string.Empty) + " " + (linea.Especificaciones ?? string.Empty)).ToLowerInvariant();
            bool esChapa = new[] { "chapa", "puerta", "tapa", "panel", "plancha", "lamina", "bandeja" }.Any(texto.Contains);
            System.Text.RegularExpressions.Match medida = System.Text.RegularExpressions.Regex.Match(texto, @"(\d+(?: \d+/\d+)?)""");

            return materiales.OrderByDescending(m =>
            {
                string nombre = (m.Nombre ?? string.Empty).ToLowerInvariant();
                int puntos = 0;

                if (texto.Contains("316"))
                {
                    puntos += nombre.Contains("316") ? 5 : 0;
                }
                else if (texto.Contains("304"))
                {
                    puntos += nombre.Contains("304") ? 3 : 0;
                }

                puntos += esChapa == nombre.StartsWith("chapa") ? 4 : 0;

                if (!esChapa && medida.Success && nombre.Contains("(" + medida.Groups[1].Value + "\""))
                {
                    puntos += 3;
                }

                return puntos * 1000000m + (m.Disponible > 0m ? 1000m : 0m) + Math.Min(m.Disponible, 999m);
            }).First();
        }

        /// <summary>
        /// Ordenes creadas que todavia no se aceptaron (estado Pendiente), con el presupuesto, el cliente y el estado de pago.
        /// </summary>
        public List<OrdenPorAceptarDto> ObtenerOrdenesPorAceptar()
        {
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Factura> facturas = new Dictionary<Guid, Factura>();

                foreach (Factura f in unidad.Conjunto<Factura>().ToList().Where(x => x.EstadoPago != EstadoPago.Anulada))
                {
                    facturas[f.PedidoId] = f;
                }

                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);
                List<OrdenPorAceptarDto> resultado = new List<OrdenPorAceptarDto>();

                foreach (OrdenFabricacion orden in unidad.Conjunto<OrdenFabricacion>().ToList()
                    .Where(o => o.Estado == EstadoProduccion.Pendiente).OrderBy(o => o.FechaCreacion))
                {
                    Pedido pedido;

                    if (!orden.PedidoId.HasValue || !pedidos.TryGetValue(orden.PedidoId.Value, out pedido))
                    {
                        continue;
                    }

                    Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(pedido.ClienteId);
                    Presupuesto presupuesto = FactoryDataAccess.PresupuestoRepository.GetById(pedido.PresupuestoId);
                    Producto producto;
                    productos.TryGetValue(orden.ProductoId, out producto);

                    Factura factura;
                    decimal total, abonado;

                    if (facturas.TryGetValue(pedido.Id, out factura))
                    {
                        total = factura.MontoTotal;
                        abonado = factura.MontoTotal - factura.SaldoPendiente;
                    }
                    else
                    {
                        decimal factor = pedido.Moneda == Moneda.DolaresEstadounidenses ? pedido.TipoCambio : 1m;
                        total = pedido.CalcularTotalEnPesos();
                        abonado = pedido.ImporteAnticipo * factor;
                    }

                    decimal saldo = Math.Max(total - abonado, 0m);

                    resultado.Add(new OrdenPorAceptarDto
                    {
                        OrdenId = orden.Id,
                        Orden = orden.Numero,
                        PedidoId = pedido.Id,
                        Pedido = pedido.Numero,
                        Presupuesto = presupuesto == null ? string.Empty : presupuesto.Numero,
                        Cliente = cliente == null ? string.Empty : cliente.RazonSocial,
                        Documento = cliente == null ? string.Empty : cliente.EtiquetaDocumento + " " + cliente.CUIT,
                        Telefono = cliente == null ? string.Empty : cliente.Telefono,
                        Producto = producto == null ? string.Empty : producto.CodigoSku + " - " + producto.Nombre,
                        Cantidad = orden.CantidadFabricar,
                        Especificaciones = orden.Especificaciones,
                        Total = total,
                        Abonado = abonado,
                        Saldo = saldo,
                        Pago = saldo <= 0.005m ? Mensajes.T("Paid in full") : abonado > 0m ? Mensajes.T("Deposit paid") : Mensajes.T("Not paid yet"),
                        Situacion = factura != null && abonado + 0.01m >= Math.Round(total * PresupuestoService.SenaMinimaFabricacion / 100m, 2)
                            ? Mensajes.T("Ready to accept")
                            : Mensajes.T("Waiting for the invoice that records the deposit")
                    });
                }

                // Presupuestos aceptados con fabricacion especial que todavia no son pedido: se ven desde ya,
                // pero no se pueden fabricar hasta confirmar el pedido y cobrar la sena.
                List<Presupuesto> aceptados = FactoryDataAccess.PresupuestoRepository.GetAll()
                    .Where(p => p.Estado == EstadoPresupuesto.Aceptado && p.FechaVencimiento.Date >= DateTime.Today).ToList();

                foreach (Presupuesto resumen in aceptados)
                {
                    Presupuesto p = FactoryDataAccess.PresupuestoRepository.GetById(resumen.Id);

                    if (p == null)
                    {
                        continue;
                    }

                    Cliente cli = FactoryDataAccess.ClienteRepository.GetById(p.ClienteId);
                    decimal factor = p.Moneda == Moneda.DolaresEstadounidenses ? p.TipoCambio : 1m;

                    foreach (DetallePresupuesto d in p.Detalles.Where(x => x.TipoItem == TipoItemPresupuesto.FabricacionEspecial))
                    {
                        resultado.Add(new OrdenPorAceptarDto
                        {
                            OrdenId = Guid.Empty,
                            Orden = "-",
                            PedidoId = Guid.Empty,
                            Pedido = "-",
                            Presupuesto = p.Numero,
                            Cliente = cli == null ? string.Empty : cli.RazonSocial,
                            Documento = cli == null ? string.Empty : cli.EtiquetaDocumento + " " + cli.CUIT,
                            Telefono = cli == null ? string.Empty : cli.Telefono,
                            Producto = d.Codigo + " - " + d.Descripcion,
                            Cantidad = d.Cantidad,
                            Especificaciones = d.DescripcionTecnica,
                            Total = p.CalcularTotalEnPesos(),
                            Abonado = 0m,
                            Saldo = p.CalcularTotalEnPesos(),
                            Pago = Mensajes.T("Deposit required: {0}", (p.CalcularAnticipo() * factor).ToString("N2")),
                            Situacion = Mensajes.T("Waiting for the order to be confirmed and the deposit paid")
                        });
                    }
                }

                return resultado;
            });
        }

        /// <summary>
        /// Un producto a medida nunca entra a fabricacion si el cliente no abono la sena minima.
        /// </summary>
        /// <summary>
        /// Un producto a medida solo se manda a producir cuando existe la factura que deja asentada la sena minima abonada.
        /// </summary>
        private static void ExigirFacturaConSena(IUnidadDeTrabajo unidad, Pedido pedido)
        {
            if (pedido == null || !pedido.Detalles.Any(d => d.RequiereFabricacion))
            {
                return;
            }

            Factura factura = unidad.Conjunto<Factura>().ToList()
                .FirstOrDefault(f => f.PedidoId == pedido.Id && f.EstadoPago != EstadoPago.Anulada);

            if (factura == null)
            {
                throw Mensajes.Regla("Production cannot start until the invoice that records the deposit is issued. Issue it from Sales > Orders.");
            }

            decimal requerida = Math.Round(factura.MontoTotal * PresupuestoService.SenaMinimaFabricacion / 100m, 2);

            if (factura.MontoTotal - factura.SaldoPendiente + 0.01m < requerida)
            {
                throw Mensajes.Regla(
                    "The invoice {0} does not show the minimum deposit ({1}%) as paid. Production cannot start.",
                    factura.Numero, PresupuestoService.SenaMinimaFabricacion.ToString("0.##"));
            }
        }

        private static void ExigirSenaPagada(Pedido pedido)
        {
            if (pedido == null || !pedido.Detalles.Any(d => d.RequiereFabricacion))
            {
                return;
            }

            decimal requerida = Math.Round(pedido.CalcularTotal() * PresupuestoService.SenaMinimaFabricacion / 100m, 2);

            if (pedido.ImporteAnticipo + 0.01m < requerida)
            {
                throw Mensajes.Regla(
                    "Manufacturing cannot start until the minimum deposit ({0}%) is paid. Deposit received: {1}. Required: {2}.",
                    PresupuestoService.SenaMinimaFabricacion.ToString("0.##"), pedido.ImporteAnticipo.ToString("N2"), requerida.ToString("N2"));
            }
        }

        public ResultadoGeneracionOrden GenerarOrden(
            Guid pedidoId,
            Guid detallePedidoId,
            PrioridadOrden prioridad,
            DateTime? finEstimada,
            List<MaterialRequerido> materiales,
            string planoTecnico,
            string observaciones)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            if (materiales == null || materiales.Count == 0)
            {
                throw Mensajes.Regla("At least one material is required to manufacture.");
            }

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            if (pedido.Estado == EstadoPedido.Pendiente || pedido.Estado == EstadoPedido.Cancelado
                || pedido.Estado == EstadoPedido.Entregado)
            {
                throw Mensajes.Regla("The order is not in a state that allows manufacturing.");
            }

            DetallePedido detalle = pedido.Detalles.FirstOrDefault(d => d.Id == detallePedidoId);

            if (detalle == null || !detalle.RequiereFabricacion)
            {
                throw Mensajes.Regla("Only lines marked for manufacturing can generate a manufacturing order.");
            }

            ExigirSenaPagada(pedido);

            if (detalle.Cantidad <= 0m)
            {
                throw Mensajes.Regla("The quantity to manufacture must be greater than zero.");
            }

            if (finEstimada.HasValue && finEstimada.Value.Date < DateTime.Today)
            {
                throw Mensajes.Regla("The estimated end date cannot be in the past.");
            }

            ResultadoGeneracionOrden resultado = _almacen.Ejecutar(unidad =>
            {
                if (unidad.Conjunto<OrdenFabricacion>().Any(
                    o => o.DetallePedidoId == detallePedidoId && o.Estado != EstadoProduccion.Cancelada))
                {
                    throw Mensajes.Regla("The order line already has an active manufacturing order.");
                }

                string sku = detalle.Codigo;
                Producto producto = unidad.Conjunto<Producto>().FirstOrDefault(p => p.CodigoSku == sku);

                if (producto == null)
                {
                    throw Mensajes.Regla("The product {0} of the order is not in the catalog.", detalle.Codigo);
                }

                if (!producto.Activo)
                {
                    throw Mensajes.Regla("The product {0} is inactive.", producto.CodigoSku);
                }

                List<string> numeros = unidad.Conjunto<OrdenFabricacion>().Select(o => o.Numero).ToList();

                OrdenFabricacion orden = new OrdenFabricacion
                {
                    Numero = Numeracion.Siguiente("OF", numeros),
                    PedidoId = pedido.Id,
                    DetallePedidoId = detalle.Id,
                    ProductoId = producto.Id,
                    Origen = "PEDIDO",
                    FechaFinEstimada = finEstimada,
                    Prioridad = prioridad,
                    CantidadFabricar = detalle.Cantidad,
                    PlanoTecnicoUri = string.IsNullOrWhiteSpace(planoTecnico) ? producto.RutaPlanoTecnico : planoTecnico,
                    Especificaciones = detalle.DescripcionTecnica ?? string.Empty,
                    Observaciones = observaciones ?? string.Empty
                };

                AgregarMateriales(unidad, orden, materiales, producto.Id);

                unidad.Conjunto<OrdenFabricacion>().Agregar(orden);
                unidad.Guardar();

                List<FaltanteMaterialDto> faltantes = ReservarFaltantes(unidad, orden);

                return new ResultadoGeneracionOrden { Orden = orden, Faltantes = faltantes };
            });

            if (pedido.Estado == EstadoPedido.Confirmado)
            {
                FactoryDataAccess.PedidoRepository.ActualizarEstado(pedido.Id, EstadoPedido.EnPreparacion);
            }

            new BitacoraService().RegistrarAuditoria(
                "Producción", "GenerarOrdenFabricacion", "OrdenFabricacion", resultado.Orden.Id,
                resultado.Orden.Numero + " para el pedido " + pedido.Numero);

            return resultado;
        }

        /// <summary>
        /// Crea una orden de reproceso para un producto devuelto, sin pedido asociado.
        /// </summary>
        public static OrdenFabricacion GenerarOrdenReproceso(
            IUnidadDeTrabajo unidad, Guid productoId, decimal cantidad, Guid? pedidoId, string motivo)
        {
            List<string> numeros = unidad.Conjunto<OrdenFabricacion>().Select(o => o.Numero).ToList();

            OrdenFabricacion orden = new OrdenFabricacion
            {
                Numero = Numeracion.Siguiente("OF", numeros),
                PedidoId = pedidoId,
                ProductoId = productoId,
                Origen = "REPROCESO",
                Prioridad = PrioridadOrden.Alta,
                CantidadFabricar = cantidad,
                Observaciones = motivo ?? string.Empty
            };

            unidad.Conjunto<OrdenFabricacion>().Agregar(orden);
            unidad.Guardar();
            return orden;
        }

        private static void AgregarMateriales(
            IUnidadDeTrabajo unidad, OrdenFabricacion orden, List<MaterialRequerido> materiales, Guid productoFinalId)
        {
            foreach (MaterialRequerido pedido in materiales)
            {
                if (pedido.Cantidad <= 0m)
                {
                    throw Mensajes.Regla("The required quantity of every material must be greater than zero.");
                }

                Producto material = InventarioMotor.ObtenerProducto(unidad, pedido.ProductoId);

                if (!material.Activo)
                {
                    throw Mensajes.Regla("The product {0} is inactive.", material.CodigoSku);
                }

                if (material.Id == productoFinalId)
                {
                    throw Mensajes.Regla("A product cannot be used as a material of itself.");
                }

                if (material.Tipo == TipoProducto.ProductoTerminado)
                {
                    throw Mensajes.Regla("The material {0} is a finished product.", material.CodigoSku);
                }

                ConsumoMaterial existente = orden.Materiales.FirstOrDefault(m => m.ProductoMaterialId == material.Id);

                if (existente != null)
                {
                    existente.CantidadRequerida += pedido.Cantidad;
                    continue;
                }

                orden.Materiales.Add(new ConsumoMaterial
                {
                    OrdenFabricacionId = orden.Id,
                    ProductoMaterialId = material.Id,
                    CantidadRequerida = pedido.Cantidad,
                    CostoUnitario = material.CostoActual
                });
            }
        }

        /// <summary>
        /// Reserva, para cada material, lo que haya disponible. Devuelve lo que falta.
        /// </summary>
        private static List<FaltanteMaterialDto> ReservarFaltantes(IUnidadDeTrabajo unidad, OrdenFabricacion orden)
        {
            List<FaltanteMaterialDto> faltantes = new List<FaltanteMaterialDto>();

            foreach (ConsumoMaterial material in orden.Materiales)
            {
                decimal falta = material.CantidadRequerida - material.CantidadReservada;

                if (falta <= 0m)
                {
                    continue;
                }

                decimal disponible = InventarioMotor.DisponibleTotal(unidad, material.ProductoMaterialId);
                decimal reservar = Math.Min(falta, disponible);

                if (reservar > 0m)
                {
                    List<ReservaStock> reservas = InventarioMotor.Reservar(
                        unidad, material.ProductoMaterialId, reservar, null, orden.Id, orden.Numero);

                    material.CantidadReservada += reservar;
                    material.FechaReserva = DateTime.Now;
                    material.UbicacionId = reservas[0].UbicacionId;
                }

                decimal pendiente = material.CantidadRequerida - material.CantidadReservada;

                if (pendiente > 0m)
                {
                    Producto producto = InventarioMotor.ObtenerProducto(unidad, material.ProductoMaterialId);

                    faltantes.Add(new FaltanteMaterialDto
                    {
                        ProductoId = producto.Id,
                        Sku = producto.CodigoSku,
                        Material = producto.Nombre,
                        Requerido = material.CantidadRequerida,
                        Reservado = material.CantidadReservada,
                        Faltante = pendiente
                    });
                }
            }

            return faltantes;
        }

        public List<FaltanteMaterialDto> ReservarMateriales(Guid ordenId)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            return _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion orden = ObtenerOrdenCompleta(unidad, ordenId);

                if (orden.Estado != EstadoProduccion.Pendiente && orden.Estado != EstadoProduccion.EnProceso)
                {
                    throw Mensajes.Regla("Materials can only be reserved for pending or in-process orders.");
                }

                return ReservarFaltantes(unidad, orden);
            });
        }

        // ---------------------------------------------------------------
        // Ejecución
        // ---------------------------------------------------------------

        public void IniciarProduccion(Guid ordenId)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);

                if (existente.PedidoId.HasValue && existente.Origen == "PEDIDO")
                {
                    Pedido delPedido = FactoryDataAccess.PedidoRepository.GetById(existente.PedidoId.Value);
                    ExigirSenaPagada(delPedido);
                    ExigirFacturaConSena(unidad, delPedido);
                }

                if (existente.Materiales.Count == 0)
                {
                    throw Mensajes.Regla("At least one material is required to manufacture.");
                }

                if (existente.Materiales.Any(m => m.CantidadReservada < m.CantidadRequerida))
                {
                    throw Mensajes.Regla("Manufacturing cannot start without all the materials reserved.");
                }

                EstadosOrden.Para(existente.Estado).Iniciar(existente);
                return existente;
            });

            if (orden.PedidoId.HasValue && orden.Origen == "PEDIDO")
            {
                FactoryDataAccess.PedidoRepository.ActualizarEstado(orden.PedidoId.Value, EstadoPedido.EnProduccion);
            }

            new BitacoraService().RegistrarAuditoria(
                "Producción", "IniciarProduccion", "OrdenFabricacion", orden.Id, orden.Numero);
        }

        public void RegistrarAvance(Guid ordenId, string etapa, decimal porcentaje, string observaciones)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            if (string.IsNullOrWhiteSpace(etapa))
            {
                throw Mensajes.Regla("The production stage is required.");
            }

            if (porcentaje < 0m || porcentaje > 100m)
            {
                throw Mensajes.Regla("The progress must be between 0 and 100.");
            }

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);
                EstadosOrden.Para(existente.Estado).RegistrarAvance(existente);

                decimal ultimo = existente.Avances.Count == 0 ? 0m : existente.Avances.Max(a => a.PorcentajeAvance);

                if (porcentaje < ultimo)
                {
                    throw Mensajes.Regla("The progress cannot be lower than the last one registered ({0}%).", ultimo.ToString("0.##"));
                }

                existente.Avances.Add(new AvanceProduccion
                {
                    OrdenFabricacionId = existente.Id,
                    Usuario = SesionActual.NombreUsuario,
                    Etapa = etapa.Trim(),
                    PorcentajeAvance = porcentaje,
                    Observaciones = observaciones ?? string.Empty
                });

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Producción", "RegistrarAvance", "OrdenFabricacion", orden.Id,
                orden.Numero + ": " + etapa + " " + porcentaje.ToString("0.##") + "%");
        }

        public void RegistrarConsumo(Guid ordenId, Guid consumoMaterialId, decimal consumido, decimal merma, string observaciones)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            if (consumido < 0m || merma < 0m)
            {
                throw Mensajes.Regla("Quantities cannot be negative.");
            }

            if (consumido + merma <= 0m)
            {
                throw Mensajes.Regla("Enter the consumed quantity or the waste.");
            }

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);

                if (!EstadosOrden.Para(existente.Estado).PermiteConsumos)
                {
                    throw Mensajes.Regla("Consumption can only be registered while the order is in process.");
                }

                ConsumoMaterial material = existente.Materiales.FirstOrDefault(m => m.Id == consumoMaterialId);

                if (material == null)
                {
                    throw Mensajes.Regla("The material does not belong to the manufacturing order.");
                }

                decimal disponibleReserva = material.CantidadReservada - material.CantidadConsumida - material.CantidadMerma;

                if (consumido + merma > disponibleReserva)
                {
                    throw Mensajes.Regla("The quantity exceeds what is reserved for this material.");
                }

                List<ReservaStock> reservas = InventarioMotor.ReservasActivasDeOrden(unidad, existente.Id)
                    .Where(r => r.ProductoId == material.ProductoMaterialId)
                    .ToList();

                ConsumirDeReservas(unidad, reservas, consumido, TipoMovimientoStock.ConsumoProduccion, existente);
                ConsumirDeReservas(unidad, reservas, merma, TipoMovimientoStock.MermaProduccion, existente);

                material.CantidadConsumida += consumido;
                material.CantidadMerma += merma;
                material.FechaConsumo = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(observaciones))
                {
                    material.Observaciones = observaciones;
                }

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Producción", "RegistrarConsumo", "OrdenFabricacion", orden.Id,
                orden.Numero + ": consumo " + consumido.ToString("0.###") + ", merma " + merma.ToString("0.###"));
        }

        private static void ConsumirDeReservas(
            IUnidadDeTrabajo unidad,
            List<ReservaStock> reservas,
            decimal cantidad,
            TipoMovimientoStock tipo,
            OrdenFabricacion orden)
        {
            decimal restante = cantidad;

            foreach (ReservaStock reserva in reservas)
            {
                if (restante <= 0m)
                {
                    break;
                }

                if (reserva.Estado != EstadoReserva.Activa || reserva.CantidadPendiente <= 0m)
                {
                    continue;
                }

                decimal tomar = Math.Min(restante, reserva.CantidadPendiente);
                InventarioMotor.ConsumirReserva(unidad, reserva, tomar, tipo, orden.Numero, orden.Id);
                restante -= tomar;
            }

            if (restante > 0m)
            {
                throw Mensajes.Regla("The reservation does not cover the quantity requested.");
            }
        }

        public void EnviarACalidad(Guid ordenId)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            // Al terminar de fabricar se descuenta solo lo reservado de cada material (ya no se carga a mano).
            OrdenFabricacion actual = ObtenerOrden(ordenId);

            // Una orden incompleta no pasa a calidad ni ingresa producto terminado: antes de tocar el stock se exige el 100%.
            if (actual.Estado == EstadoProduccion.EnProceso)
            {
                decimal avance = EstadoEnProceso.AvanceDe(actual);

                if (avance < 100m)
                {
                    throw Mensajes.Regla(
                        "The order is at {0}% progress. It cannot be sent to quality control until manufacturing reaches 100%. Keep manufacturing or cancel the order.",
                        avance.ToString("0.##"));
                }
            }

            foreach (ConsumoMaterial pendiente in actual.Estado == EstadoProduccion.EnProceso ? actual.Materiales : new List<ConsumoMaterial>())
            {
                decimal resto = pendiente.CantidadReservada - pendiente.CantidadConsumida - pendiente.CantidadMerma;

                if (resto > 0m)
                {
                    RegistrarConsumo(ordenId, pendiente.Id, resto, 0m, Mensajes.T("Consumed automatically when sent to quality control."));
                }
            }

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);

                if (existente.Materiales.All(m => m.CantidadConsumida <= 0m))
                {
                    throw Mensajes.Regla("The order has no materials reserved. Reserve the materials before sending to quality control.");
                }

                EstadosOrden.Para(existente.Estado).EnviarACalidad(existente);
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Producción", "EnviarACalidad", "OrdenFabricacion", orden.Id, orden.Numero);
        }

        public void RegistrarControlCalidad(Guid ordenId, ResultadoCalidad resultado, string observaciones)
        {
            Autorizacion.Exigir(Permiso.CalidadRegistrar);

            if (resultado != ResultadoCalidad.Aprobado && string.IsNullOrWhiteSpace(observaciones))
            {
                throw Mensajes.Regla("Explain the reason for a rejection or a rework.");
            }

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);
                IEstadoOrden estado = EstadosOrden.Para(existente.Estado);

                int reprocesos = existente.Controles.Count(c => c.Resultado == ResultadoCalidad.Reproceso);

                ControlCalidad control = new ControlCalidad
                {
                    OrdenFabricacionId = existente.Id,
                    UsuarioControl = SesionActual.NombreUsuario,
                    Resultado = resultado,
                    RequiereReproceso = resultado == ResultadoCalidad.Reproceso,
                    NumeroReproceso = resultado == ResultadoCalidad.Reproceso ? reprocesos + 1 : reprocesos,
                    FechaReproceso = resultado == ResultadoCalidad.Reproceso ? (DateTime?)DateTime.Now : null,
                    Observaciones = observaciones ?? string.Empty
                };

                estado.AplicarControl(existente, resultado);
                existente.Controles.Add(control);

                if (resultado == ResultadoCalidad.Aprobado)
                {
                    FinalizarOrden(unidad, existente);
                }
                else if (resultado == ResultadoCalidad.Rechazado)
                {
                    foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeOrden(unidad, existente.Id))
                    {
                        InventarioMotor.LiberarReserva(unidad, reserva, "Orden rechazada en calidad: " + observaciones);
                    }

                    existente.Observaciones = (existente.Observaciones + " | " + Mensajes.T("Rejected in quality control") + ": " + observaciones).Trim(' ', '|');
                }

                return existente;
            });

            if (resultado == ResultadoCalidad.Aprobado)
            {
                ActualizarEstadoDelPedido(orden);
            }

            new BitacoraService().RegistrarAuditoria(
                "Producción", "ControlCalidad", "OrdenFabricacion", orden.Id,
                orden.Numero + ": " + resultado);
        }

        private static void FinalizarOrden(IUnidadDeTrabajo unidad, OrdenFabricacion orden)
        {
            decimal costoMateriales = orden.Materiales.Sum(m => m.CantidadConsumida * m.CostoUnitario);
            decimal? costoUnitario = orden.CantidadFabricar > 0m
                ? (decimal?)Math.Round(costoMateriales / orden.CantidadFabricar, 2)
                : null;

            // Lo reservado y no consumido vuelve a estar disponible.
            foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeOrden(unidad, orden.Id))
            {
                InventarioMotor.LiberarReserva(unidad, reserva, "Orden finalizada");
            }

            Producto producto = InventarioMotor.ObtenerProducto(unidad, orden.ProductoId);
            UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, producto);

            InventarioMotor.Mover(
                unidad, producto.Id, ubicacion.Id, TipoMovimientoStock.ProductoTerminado,
                orden.CantidadFabricar, costoUnitario, orden.Numero, orden.Id,
                string.Empty, string.Empty, string.Empty, false);

            // El producto terminado queda reservado para la línea del pedido que lo originó.
            if (orden.DetallePedidoId.HasValue)
            {
                InventarioMotor.Reservar(
                    unidad, producto.Id, orden.CantidadFabricar, orden.DetallePedidoId, null, orden.Numero);
            }
        }

        private void ActualizarEstadoDelPedido(OrdenFabricacion orden)
        {
            if (!orden.PedidoId.HasValue)
            {
                return;
            }

            Guid pedidoId = orden.PedidoId.Value;

            bool todasTerminadas = _almacen.Ejecutar(unidad =>
                !unidad.Conjunto<OrdenFabricacion>().Any(
                    o => o.PedidoId == pedidoId
                        && o.Estado != EstadoProduccion.Finalizada
                        && o.Estado != EstadoProduccion.Cancelada));

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (todasTerminadas && pedido != null
                && (pedido.Estado == EstadoPedido.EnProduccion || pedido.Estado == EstadoPedido.EnPreparacion))
            {
                bool quedanSinOrden = pedido.Detalles.Any(d => d.RequiereFabricacion)
                    && ObtenerPedidosPendientes().Any(p => p.PedidoId == pedidoId);

                if (!quedanSinOrden)
                {
                    FactoryDataAccess.PedidoRepository.ActualizarEstado(pedidoId, EstadoPedido.ListoParaEntrega);
                }
            }
        }

        /// <summary>
        /// Toda la vida de una orden: creacion, aceptacion, avances, calidad, cierre o cancelacion (con motivo y usuario).
        /// </summary>
        public List<EventoHistorialDto> ObtenerResumenOrden(Guid ordenId)
        {
            OrdenFabricacion orden = ObtenerOrden(ordenId);
            List<EventoHistorialDto> eventos = new List<EventoHistorialDto>();
            List<RegistroBitacora> bitacora = new BitacoraService().ObtenerDeEntidad(ordenId);

            Action<DateTime, string, string, string> add = (f, u, ev, d) =>
                eventos.Add(new EventoHistorialDto { Fecha = f, Usuario = u ?? string.Empty, Evento = Mensajes.T(ev), Detalle = d ?? string.Empty });

            RegistroBitacora alta = bitacora.FirstOrDefault(r => r.Accion == "GenerarOrden" || r.Accion == "CrearOrden");
            add(orden.FechaCreacion, alta == null ? string.Empty : alta.NombreUsuario, "Order created",
                orden.Numero + " - " + Mensajes.T("{0} units", orden.CantidadFabricar.ToString("0.##")));

            if (orden.FechaInicio.HasValue)
            {
                RegistroBitacora inicio = bitacora.FirstOrDefault(r => r.Accion == "IniciarProduccion");
                add(orden.FechaInicio.Value, inicio == null ? string.Empty : inicio.NombreUsuario, "Accepted and started", string.Empty);
            }

            foreach (AvanceProduccion a in orden.Avances.OrderBy(x => x.Fecha))
            {
                add(a.Fecha, a.Usuario, "Progress", a.PorcentajeAvance.ToString("0.##") + "% - " + a.Etapa
                    + (string.IsNullOrWhiteSpace(a.Observaciones) ? string.Empty : " - " + a.Observaciones));
            }

            foreach (ControlCalidad c in orden.Controles.OrderBy(x => x.FechaControl))
            {
                add(c.FechaControl, c.UsuarioControl, "Quality control", Etiquetas.De(c.Resultado)
                    + (string.IsNullOrWhiteSpace(c.Observaciones) ? string.Empty : " - " + c.Observaciones));
            }

            if (orden.FechaFinReal.HasValue)
            {
                add(orden.FechaFinReal.Value, string.Empty, "Finished", Mensajes.T("Finished product delivered to stock."));
            }

            RegistroBitacora cancelacion = bitacora.LastOrDefault(r => r.Accion == "CancelarOrden");

            if (cancelacion != null || orden.Estado == EstadoProduccion.Cancelada)
            {
                string motivo = cancelacion == null ? orden.Observaciones : cancelacion.Descripcion;
                int corte = motivo == null ? -1 : motivo.IndexOf(": ", StringComparison.Ordinal);
                motivo = corte >= 0 && cancelacion != null ? motivo.Substring(corte + 2) : motivo;
                add(cancelacion == null ? orden.FechaCreacion : cancelacion.FechaHora, cancelacion == null ? string.Empty : cancelacion.NombreUsuario,
                    "Cancelled", Mensajes.T("Reason: {0}", motivo));
            }

            return eventos.OrderBy(e => e.Fecha).ToList();
        }

        public void CancelarOrden(Guid ordenId, string motivo)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for cancelling is required.");
            }

            OrdenFabricacion orden = _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion existente = ObtenerOrdenCompleta(unidad, ordenId);
                EstadosOrden.Para(existente.Estado).Cancelar(existente);

                foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeOrden(unidad, existente.Id))
                {
                    InventarioMotor.LiberarReserva(unidad, reserva, "Orden cancelada: " + motivo);
                }

                existente.Observaciones = (existente.Observaciones + " | " + motivo).Trim(' ', '|');
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Producción", "CancelarOrden", "OrdenFabricacion", orden.Id, orden.Numero + ": " + motivo);
        }

        // ---------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------

        private static OrdenFabricacion ObtenerOrdenCompleta(IUnidadDeTrabajo unidad, Guid ordenId)
        {
            OrdenFabricacion orden = unidad.Conjunto<OrdenFabricacion>()
                .Incluir("Materiales").Incluir("Avances").Incluir("Controles")
                .FirstOrDefault(o => o.Id == ordenId);

            if (orden == null)
            {
                throw Mensajes.Regla("The manufacturing order does not exist.");
            }

            return orden;
        }

        public List<OrdenTableroDto> ObtenerTablero(bool incluirFinalizadas)
        {
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);

                List<OrdenFabricacion> ordenes = unidad.Conjunto<OrdenFabricacion>()
                    .Incluir("Avances").Incluir("Controles").ToList()
                    .Where(o => o.Estado != EstadoProduccion.Cancelada
                        && (incluirFinalizadas || o.Estado != EstadoProduccion.Finalizada))
                    .OrderByDescending(o => o.Prioridad)
                    .ThenBy(o => o.FechaCreacion)
                    .ToList();

                return ordenes.Select(o =>
                {
                    Pedido pedido = null;

                    if (o.PedidoId.HasValue)
                    {
                        pedidos.TryGetValue(o.PedidoId.Value, out pedido);
                    }

                    Producto producto;
                    productos.TryGetValue(o.ProductoId, out producto);
                    ControlCalidad ultimo = o.Controles.OrderBy(c => c.FechaControl).LastOrDefault();

                    return new OrdenTableroDto
                    {
                        Id = o.Id,
                        Numero = o.Numero,
                        Pedido = pedido == null ? Mensajes.T("Rework") : pedido.Numero,
                        Cliente = pedido == null ? string.Empty : Referencias.Nombre(clientes, pedido.ClienteId),
                        Producto = producto == null ? string.Empty : producto.Nombre,
                        Cantidad = o.CantidadFabricar,
                        Estado = Etiquetas.De(o.Estado),
                        EstadoClave = o.Estado,
                        Prioridad = Etiquetas.De(o.Prioridad),
                        PrioridadClave = o.Prioridad,
                        Material = producto == null ? string.Empty : producto.CalidadAisi,
                        Creacion = o.FechaCreacion,
                        FinEstimada = o.FechaFinEstimada,
                        Plano = o.PlanoTecnicoUri,
                        Avance = o.Avances.Count == 0 ? 0m : o.Avances.Max(a => a.PorcentajeAvance),
                        Reprocesos = o.Controles.Count(c => c.Resultado == ResultadoCalidad.Reproceso),
                        Bloqueada = o.Estado == EstadoProduccion.ControlCalidad
                            && ultimo != null && ultimo.Resultado == ResultadoCalidad.Rechazado
                    };
                }).ToList();
            });
        }

        public List<MaterialOrdenDto> ObtenerMateriales(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad =>
            {
                OrdenFabricacion orden = ObtenerOrdenCompleta(unidad, ordenId);
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);

                return orden.Materiales.Select(m =>
                {
                    Producto producto;
                    productos.TryGetValue(m.ProductoMaterialId, out producto);

                    return new MaterialOrdenDto
                    {
                        Id = m.Id,
                        ProductoId = m.ProductoMaterialId,
                        Sku = producto == null ? string.Empty : producto.CodigoSku,
                        Material = producto == null ? string.Empty : producto.Nombre,
                        Requerida = m.CantidadRequerida,
                        Reservada = m.CantidadReservada,
                        Consumida = m.CantidadConsumida,
                        Merma = m.CantidadMerma,
                        PorConsumir = m.CantidadReservada - m.CantidadConsumida - m.CantidadMerma
                    };
                }).ToList();
            });
        }

        public List<AvanceDto> ObtenerAvances(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad =>
                ObtenerOrdenCompleta(unidad, ordenId).Avances
                    .OrderBy(a => a.Fecha)
                    .Select(a => new AvanceDto
                    {
                        Fecha = a.Fecha,
                        Usuario = a.Usuario,
                        Etapa = a.Etapa,
                        Porcentaje = a.PorcentajeAvance,
                        Observaciones = a.Observaciones
                    }).ToList());
        }

        public List<ControlCalidadDto> ObtenerControles(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad =>
                ObtenerOrdenCompleta(unidad, ordenId).Controles
                    .OrderBy(c => c.FechaControl)
                    .Select(c => new ControlCalidadDto
                    {
                        Fecha = c.FechaControl,
                        Usuario = c.UsuarioControl,
                        Resultado = Etiquetas.De(c.Resultado),
                        Reproceso = c.NumeroReproceso,
                        Observaciones = c.Observaciones
                    }).ToList());
        }

        public OrdenFabricacion ObtenerOrden(Guid ordenId)
        {
            return _almacen.Ejecutar(unidad => ObtenerOrdenCompleta(unidad, ordenId));
        }
    }
}
