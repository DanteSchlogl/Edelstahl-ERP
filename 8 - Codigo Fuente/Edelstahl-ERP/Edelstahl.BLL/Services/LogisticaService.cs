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
    /// Vehículos, choferes, remitos, hojas de ruta y registro de entregas.
    /// </summary>
    public class LogisticaService
    {
        public const int PuntoVentaRemito = 1;
        private const decimal CapacidadMaximaCategoriaB = 3500m;

        private readonly INegocioStore _almacen;

        public LogisticaService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public LogisticaService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Vehículos y choferes
        // ---------------------------------------------------------------

        public List<Vehiculo> ObtenerVehiculos(bool soloActivos)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Vehiculo>().ToList()
                    .Where(v => !soloActivos || v.Activo)
                    .OrderBy(v => v.Patente).ToList());
        }

        public Vehiculo GuardarVehiculo(Vehiculo vehiculo)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (vehiculo == null)
            {
                throw new ArgumentNullException(nameof(vehiculo));
            }

            string patente = (vehiculo.Patente ?? string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();

            if (patente.Length < 6 || patente.Length > 8)
            {
                throw Mensajes.Regla("The license plate is not valid.");
            }

            if (vehiculo.CapacidadKg <= 0m)
            {
                throw Mensajes.Regla("The capacity must be greater than zero.");
            }

            if (vehiculo.Anio < 1980 || vehiculo.Anio > DateTime.Today.Year + 1)
            {
                throw Mensajes.Regla("The model year is not valid.");
            }

            Vehiculo resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<Vehiculo> conjunto = unidad.Conjunto<Vehiculo>();

                if (conjunto.Any(v => v.Patente == patente && v.Id != vehiculo.Id))
                {
                    throw Mensajes.Regla("There is already a vehicle with the plate {0}.", patente);
                }

                Vehiculo existente = conjunto.FirstOrDefault(v => v.Id == vehiculo.Id);

                if (existente == null)
                {
                    existente = new Vehiculo { Id = vehiculo.Id };
                    conjunto.Agregar(existente);
                }

                existente.Patente = patente;
                existente.Marca = vehiculo.Marca ?? string.Empty;
                existente.Modelo = vehiculo.Modelo ?? string.Empty;
                existente.Anio = vehiculo.Anio;
                existente.CapacidadKg = vehiculo.CapacidadKg;
                existente.CapacidadM3 = vehiculo.CapacidadM3;
                existente.VencimientoSeguro = vehiculo.VencimientoSeguro;
                existente.VencimientoVtv = vehiculo.VencimientoVtv;
                existente.Activo = vehiculo.Activo;

                if (string.IsNullOrWhiteSpace(existente.Estado))
                {
                    existente.Estado = "DISPONIBLE";
                }

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "GuardarVehiculo", "Vehiculo", resultado.Id, resultado.Patente);

            return resultado;
        }

        public List<Chofer> ObtenerChoferes(bool soloActivos)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Chofer>().ToList()
                    .Where(c => !soloActivos || c.Activo)
                    .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToList());
        }

        public Chofer GuardarChofer(Chofer chofer)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (chofer == null)
            {
                throw new ArgumentNullException(nameof(chofer));
            }

            string documento = new string((chofer.Documento ?? string.Empty).Where(char.IsDigit).ToArray());

            if (documento.Length < 7 || documento.Length > 9)
            {
                throw Mensajes.Regla("The document number is not valid.");
            }

            if (string.IsNullOrWhiteSpace(chofer.Apellido) || string.IsNullOrWhiteSpace(chofer.Nombre))
            {
                throw Mensajes.Regla("The first name and last name are required.");
            }

            if (string.IsNullOrWhiteSpace(chofer.LicenciaConducir) || string.IsNullOrWhiteSpace(chofer.CategoriaLicencia))
            {
                throw Mensajes.Regla("The license number and category are required.");
            }

            Chofer resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<Chofer> conjunto = unidad.Conjunto<Chofer>();
                string licencia = chofer.LicenciaConducir.Trim();

                if (conjunto.Any(c => c.Documento == documento && c.Id != chofer.Id))
                {
                    throw Mensajes.Regla("There is already a driver with the document {0}.", documento);
                }

                if (conjunto.Any(c => c.LicenciaConducir == licencia && c.Id != chofer.Id))
                {
                    throw Mensajes.Regla("There is already a driver with that license number.");
                }

                Chofer existente = conjunto.FirstOrDefault(c => c.Id == chofer.Id);

                if (existente == null)
                {
                    existente = new Chofer { Id = chofer.Id };
                    conjunto.Agregar(existente);
                }

                existente.Documento = documento;
                existente.Apellido = chofer.Apellido.Trim();
                existente.Nombre = chofer.Nombre.Trim();
                existente.LicenciaConducir = licencia;
                existente.CategoriaLicencia = chofer.CategoriaLicencia.Trim().ToUpperInvariant();
                existente.VencimientoLicencia = chofer.VencimientoLicencia;
                existente.Telefono = chofer.Telefono ?? string.Empty;
                existente.Activo = chofer.Activo;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "GuardarChofer", "Chofer", resultado.Id, resultado.NombreCompleto);

            return resultado;
        }

        // ---------------------------------------------------------------
        // Pedidos listos para despachar
        // ---------------------------------------------------------------

        private static bool EsLineaFisica(DetallePedido detalle)
        {
            return detalle.TipoItem != TipoItemPresupuesto.Servicio;
        }

        private static Dictionary<Guid, decimal> CantidadesRemitidas(IUnidadDeTrabajo unidad, Guid pedidoId)
        {
            return unidad.Conjunto<RemitoEntrega>().Incluir("Detalles").ToList()
                .Where(r => r.PedidoId == pedidoId && r.Estado != EstadoRemito.Cancelado)
                .SelectMany(r => r.Detalles)
                .GroupBy(d => d.DetallePedidoId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));
        }

        private static decimal ReservadoPendiente(IUnidadDeTrabajo unidad, Guid detallePedidoId)
        {
            return InventarioMotor.ReservasActivasDeDetalle(unidad, detallePedidoId).Sum(r => r.CantidadPendiente);
        }

        /// <summary>
        /// Ordenes de preparacion: pedidos ya facturados con mercaderia pendiente de enviar,
        /// con el detalle de lo que hay que preparar y si ya esta todo reservado.
        /// </summary>
        public List<OrdenPreparacionDto> ObtenerOrdenesPreparacion()
        {
            List<Pedido> pedidos = FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado != EstadoPedido.Pendiente && p.Estado != EstadoPedido.Cancelado && p.Estado != EstadoPedido.Entregado)
                .ToList();

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, string> facturas = new Dictionary<Guid, string>();

                foreach (Factura f in unidad.Conjunto<Factura>().Where(x => x.EstadoPago != EstadoPago.Anulada).ToList())
                {
                    facturas[f.PedidoId] = f.Numero;
                }

                List<OrdenPreparacionDto> resultado = new List<OrdenPreparacionDto>();

                foreach (Pedido pedido in pedidos.OrderBy(p => p.FechaPedido))
                {
                    string factura;

                    if (!facturas.TryGetValue(pedido.Id, out factura))
                    {
                        continue;
                    }

                    Dictionary<Guid, decimal> remitidas = CantidadesRemitidas(unidad, pedido.Id);
                    decimal pendiente = 0m;
                    bool sinReservar = false;
                    bool aFabricar = false;
                    List<string> partes = new List<string>();

                    foreach (DetallePedido d in pedido.Detalles.Where(EsLineaFisica))
                    {
                        decimal remitida;
                        remitidas.TryGetValue(d.Id, out remitida);
                        decimal falta = d.Cantidad - remitida;

                        if (falta <= 0m)
                        {
                            continue;
                        }

                        pendiente += falta;
                        partes.Add(d.Codigo + " x " + falta.ToString("0.##"));

                        if (ReservadoPendiente(unidad, d.Id) < falta)
                        {
                            sinReservar = true;
                            aFabricar = aFabricar || d.RequiereFabricacion;
                        }
                    }

                    if (pendiente <= 0m)
                    {
                        continue;
                    }

                    resultado.Add(new OrdenPreparacionDto
                    {
                        PedidoId = pedido.Id,
                        Pedido = pedido.Numero,
                        Factura = factura,
                        Cliente = Referencias.Nombre(clientes, pedido.ClienteId),
                        FechaPedido = pedido.FechaPedido,
                        Productos = string.Join(", ", partes),
                        Pendiente = pendiente,
                        Estado = !sinReservar ? Mensajes.T("Prepared") : aFabricar ? Mensajes.T("Waiting for production") : Mensajes.T("To prepare")
                    });
                }

                return resultado;
            });
        }

        public List<PedidoDespachoDto> ObtenerPedidosParaDespacho()
        {
            List<Pedido> pedidos = FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado != EstadoPedido.Pendiente
                    && p.Estado != EstadoPedido.Cancelado
                    && p.Estado != EstadoPedido.Entregado)
                .ToList();

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, string> facturados = new Dictionary<Guid, string>();
                Dictionary<Guid, decimal> saldos = new Dictionary<Guid, decimal>();

                foreach (Factura emitida in unidad.Conjunto<Factura>().Where(f => f.EstadoPago != EstadoPago.Anulada).ToList())
                {
                    facturados[emitida.PedidoId] = emitida.Numero;
                    saldos[emitida.PedidoId] = emitida.SaldoPendiente;
                }

                List<PedidoDespachoDto> resultado = new List<PedidoDespachoDto>();

                foreach (Pedido pedido in pedidos.OrderBy(p => p.FechaPedido))
                {
                    Dictionary<Guid, decimal> remitidas = CantidadesRemitidas(unidad, pedido.Id);
                    decimal pendiente = 0m;
                    decimal faltaProducir = 0m;
                    List<string> sinReserva = new List<string>();

                    foreach (DetallePedido detalle in pedido.Detalles.Where(EsLineaFisica))
                    {
                        decimal remitida;
                        remitidas.TryGetValue(detalle.Id, out remitida);
                        decimal falta = detalle.Cantidad - remitida;

                        if (falta <= 0m)
                        {
                            continue;
                        }

                        pendiente += falta;

                        if (ReservadoPendiente(unidad, detalle.Id) < falta)
                        {
                            faltaProducir += falta;
                            sinReserva.Add(detalle.Codigo);
                        }
                    }

                    if (pendiente <= 0m)
                    {
                        continue;
                    }

                    string numeroFactura;
                    bool facturado = facturados.TryGetValue(pedido.Id, out numeroFactura);
                    string motivo = string.Empty;
                    string direccion = DireccionDeEntrega(pedido.ClienteId);
                    bool sinDireccion = pedido.EntregaIncluida && Direcciones.Falta(direccion);

                    if (pendiente <= 0m)
                    {
                        motivo = Mensajes.T("Nothing pending to deliver");
                    }
                    else if (sinDireccion)
                    {
                        motivo = Mensajes.T("The customer has no delivery address");
                    }
                    else if (!facturado)
                    {
                        motivo = Mensajes.T("The order has not been invoiced");
                    }
                    else if (saldos[pedido.Id] > 0.005m && !EsAutorizado(pedido.ClienteId))
                    {
                        motivo = Mensajes.T("Balance due {0}: the customer must pay 100% before shipping", saldos[pedido.Id].ToString("N2"));
                    }
                    else if (faltaProducir >= pendiente)
                    {
                        motivo = Mensajes.T("Waiting for production or stock: {0}", string.Join(", ", sinReserva.Distinct().Take(3)));
                    }

                    resultado.Add(new PedidoDespachoDto
                    {
                        PedidoId = pedido.Id,
                        Pedido = pedido.Numero,
                        FechaPedido = pedido.FechaPedido,
                        FechaEntrega = FechaComprometida(pedido),
                        Cliente = Referencias.Nombre(clientes, pedido.ClienteId),
                        Direccion = Direcciones.Falta(direccion) ? Mensajes.T("No delivery address") : direccion,
                        Factura = facturado ? numeroFactura : string.Empty,
                        Estado = Etiquetas.De(pedido.Estado),
                        Pendiente = pendiente,
                        Facturado = facturado,
                        Saldo = facturado ? saldos[pedido.Id] : 0m,
                        Habilitado = motivo.Length == 0,
                        Motivo = motivo.Length > 0 ? motivo
                            : (saldos.ContainsKey(pedido.Id) && saldos[pedido.Id] > 0.005m
                                ? Mensajes.T("Authorized customer (credit account) - ready to ship and deliver")
                                : Mensajes.T("Customer paid 100% - ready to ship and deliver"))
                    });
                }

                return resultado;
            });
        }

        /// <summary>
        /// Fecha de entrega pactada: fecha del pedido mas los dias del plazo ("7 dias"). Vacia si el plazo no tiene numero.
        /// </summary>
        private static DateTime? FechaComprometida(Pedido pedido)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(pedido.PlazoEntrega ?? string.Empty, @"\d+");
            int dias;

            return m.Success && int.TryParse(m.Value, out dias) && dias < 1000 ? pedido.FechaPedido.Date.AddDays(dias) : (DateTime?)null;
        }

        private static bool EsAutorizado(Guid clienteId)
        {
            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(clienteId);
            return cliente != null && cliente.AutorizadoACredito;
        }

        private static string DireccionDeEntrega(Guid clienteId)
        {
            try
            {
                Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(clienteId);
                return cliente == null ? string.Empty : cliente.DireccionEntrega;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public List<LineaPendienteRemitoDto> ObtenerLineasPendientes(Guid pedidoId)
        {
            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                return new List<LineaPendienteRemitoDto>();
            }

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, decimal> remitidas = CantidadesRemitidas(unidad, pedidoId);

                return pedido.Detalles.Where(EsLineaFisica).Select(d =>
                {
                    decimal remitida;
                    remitidas.TryGetValue(d.Id, out remitida);
                    decimal pendiente = Math.Max(0m, d.Cantidad - remitida);

                    return new LineaPendienteRemitoDto
                    {
                        DetallePedidoId = d.Id,
                        Codigo = d.Codigo,
                        Descripcion = d.Descripcion,
                        Pedida = d.Cantidad,
                        Remitida = remitida,
                        Pendiente = pendiente,
                        ListaParaDespacho = Math.Min(pendiente, ReservadoPendiente(unidad, d.Id))
                    };
                }).ToList();
            });
        }

        // ---------------------------------------------------------------
        // Remitos
        // ---------------------------------------------------------------

        public RemitoEntrega GenerarRemito(Guid pedidoId, List<LineaRemitoSolicitud> lineas, string observaciones)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (lineas == null || lineas.Count == 0 || lineas.All(l => l.Cantidad <= 0m))
            {
                throw Mensajes.Regla("Select at least one line to deliver.");
            }

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            if (pedido.Estado == EstadoPedido.Pendiente || pedido.Estado == EstadoPedido.Cancelado
                || pedido.Estado == EstadoPedido.Entregado)
            {
                throw Mensajes.Regla("The order is not enabled for dispatch.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(pedido.ClienteId);

            if (pedido.EntregaIncluida && (cliente == null || Direcciones.Falta(cliente.DireccionEntrega)))
            {
                throw Mensajes.Regla("The customer has no delivery address. Complete it in Customers.");
            }

            RemitoEntrega remito = _almacen.Ejecutar(unidad =>
            {
                Factura facturaDelPedido = unidad.Conjunto<Factura>().ToList()
                    .FirstOrDefault(f => f.PedidoId == pedidoId && f.EstadoPago != EstadoPago.Anulada);

                if (facturaDelPedido == null)
                {
                    throw Mensajes.Regla("The order must be invoiced before a delivery note can be issued.");
                }

                if (facturaDelPedido.SaldoPendiente > 0.005m && (cliente == null || !cliente.AutorizadoACredito))
                {
                    throw Mensajes.Regla("The customer still owes {0}. The delivery note can be issued once the order is paid in full.", facturaDelPedido.SaldoPendiente.ToString("N2"));
                }

                Dictionary<Guid, decimal> remitidas = CantidadesRemitidas(unidad, pedidoId);
                List<string> numeros = unidad.Conjunto<RemitoEntrega>().Select(r => r.Numero).ToList();

                RemitoEntrega nuevo = new RemitoEntrega
                {
                    Numero = Numeracion.SiguienteFiscal(PuntoVentaRemito, numeros),
                    PedidoId = pedido.Id,
                    ClienteId = pedido.ClienteId,
                    DireccionEntrega = DireccionDeEntrega(cliente),
                    Observaciones = observaciones ?? string.Empty
                };

                foreach (LineaRemitoSolicitud solicitud in lineas.Where(l => l.Cantidad > 0m))
                {
                    DetallePedido detalle = pedido.Detalles.FirstOrDefault(d => d.Id == solicitud.DetallePedidoId);

                    if (detalle == null || !EsLineaFisica(detalle))
                    {
                        throw Mensajes.Regla("The line does not belong to the order or cannot be delivered.");
                    }

                    decimal remitida;
                    remitidas.TryGetValue(detalle.Id, out remitida);
                    decimal pendiente = detalle.Cantidad - remitida;

                    if (solicitud.Cantidad > pendiente)
                    {
                        throw Mensajes.Regla(
                            "The quantity of {0} exceeds what is pending to deliver ({1}).",
                            detalle.Codigo, pendiente.ToString("0.###"));
                    }

                    if (ReservadoPendiente(unidad, detalle.Id) < solicitud.Cantidad)
                    {
                        throw Mensajes.Regla(
                            "The line {0} is not ready: there is no reserved stock for that quantity.",
                            detalle.Codigo);
                    }

                    nuevo.Detalles.Add(new DetalleRemito
                    {
                        RemitoId = nuevo.Id,
                        DetallePedidoId = detalle.Id,
                        Codigo = detalle.Codigo,
                        Descripcion = detalle.Descripcion,
                        Cantidad = solicitud.Cantidad
                    });

                    remitidas[detalle.Id] = remitida + solicitud.Cantidad;
                }

                unidad.Conjunto<RemitoEntrega>().Agregar(nuevo);
                return nuevo;
            });

            if (pedido.Estado == EstadoPedido.Confirmado || pedido.Estado == EstadoPedido.EnPreparacion
                || pedido.Estado == EstadoPedido.EnProduccion)
            {
                FactoryDataAccess.PedidoRepository.ActualizarEstado(pedido.Id, EstadoPedido.ListoParaEntrega);
            }

            new BitacoraService().RegistrarAuditoria(
                "Logística", "GenerarRemito", "RemitoEntrega", remito.Id, remito.Numero + " del pedido " + pedido.Numero);

            return remito;
        }

        private static string DireccionDeEntrega(Cliente cliente)
        {
            if (cliente == null)
            {
                return string.Empty;
            }

            string direccion = string.IsNullOrWhiteSpace(cliente.DireccionEntrega)
                ? cliente.DireccionFacturacion
                : cliente.DireccionEntrega;

            return (direccion + " " + cliente.Localidad + " " + cliente.Provincia).Trim();
        }

        public void CancelarRemito(Guid remitoId, string motivo)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for cancelling is required.");
            }

            RemitoEntrega remito = _almacen.Ejecutar(unidad =>
            {
                RemitoEntrega existente = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles")
                    .FirstOrDefault(r => r.Id == remitoId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The delivery note does not exist.");
                }

                if (existente.Estado != EstadoRemito.Pendiente)
                {
                    throw Mensajes.Regla("Only pending delivery notes can be cancelled.");
                }

                existente.Estado = EstadoRemito.Cancelado;
                existente.Observaciones = (existente.Observaciones + " | " + motivo).Trim(' ', '|');
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "CancelarRemito", "RemitoEntrega", remito.Id, remito.Numero + ": " + motivo);
        }

        /// <summary>Bitacora de envios: todos los remitos (abiertos y cerrados) con su recorrido y resultado.</summary>
        public List<EnvioHistorialDto> ObtenerHistorialEnvios()
        {
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                List<HojaRuta> hojas = unidad.Conjunto<HojaRuta>().Incluir("Detalles").ToList();

                return unidad.Conjunto<RemitoEntrega>().ToList().OrderByDescending(r => r.FechaEmision).Select(r =>
                {
                    Pedido pedido;
                    pedidos.TryGetValue(r.PedidoId, out pedido);

                    DetalleHojaRuta ultima = hojas.SelectMany(h => h.Detalles).Where(d => d.RemitoId == r.Id)
                        .OrderBy(d => hojas.First(h => h.Id == d.HojaRutaId).FechaPlanificada).LastOrDefault();
                    HojaRuta hoja = ultima == null ? null : hojas.First(h => h.Id == ultima.HojaRutaId);

                    return new EnvioHistorialDto
                    {
                        Id = r.Id,
                        Remito = r.Numero,
                        Pedido = pedido == null ? string.Empty : pedido.Numero,
                        Cliente = Referencias.Nombre(clientes, r.ClienteId),
                        Estado = Etiquetas.De(r.Estado),
                        EstadoClave = r.Estado,
                        Emision = r.FechaEmision,
                        HojaRuta = hoja == null ? string.Empty : hoja.Numero,
                        Resultado = ultima == null || !ultima.Resultado.HasValue ? string.Empty : Etiquetas.De(ultima.Resultado.Value),
                        Motivo = ultima == null ? string.Empty : ultima.MotivoNoEntrega,
                        Receptor = string.IsNullOrEmpty(r.ReceptorNombre) ? string.Empty : r.ReceptorNombre + " (" + r.ReceptorDocumento + ")",
                        Entrega = r.FechaEntrega
                    };
                }).ToList();
            });
        }

        /// <summary>Todos los pasos de un envio, con el usuario que los hizo y el motivo si no se entrego o se cancelo.</summary>
        public List<EventoHistorialDto> ObtenerEventosEnvio(Guid remitoId)
        {
            List<EventoHistorialDto> eventos = new List<EventoHistorialDto>();
            List<RegistroBitacora> bitacora = new BitacoraService().ObtenerDeEntidad(remitoId);

            Action<DateTime, string, string, string> add = (f, u, ev, d) =>
                eventos.Add(new EventoHistorialDto { Fecha = f, Usuario = u ?? string.Empty, Evento = Mensajes.T(ev), Detalle = d ?? string.Empty });

            _almacen.Ejecutar(unidad =>
            {
                RemitoEntrega remito = unidad.Conjunto<RemitoEntrega>().FirstOrDefault(r => r.Id == remitoId);

                if (remito == null)
                {
                    return 0;
                }

                RegistroBitacora emision = bitacora.FirstOrDefault(r => r.Accion == "GenerarRemito");
                add(remito.FechaEmision, emision == null ? string.Empty : emision.NombreUsuario, "Delivery note issued", remito.Numero + " - " + remito.DireccionEntrega);

                RegistroBitacora carga = bitacora.FirstOrDefault(r => r.Accion == "VerificarCargaRemito");

                if (carga != null)
                {
                    add(carga.FechaHora, carga.NombreUsuario, "Load verified", carga.Descripcion);
                }

                List<HojaRuta> hojas = unidad.Conjunto<HojaRuta>().Incluir("Detalles").ToList();

                foreach (DetalleHojaRuta parada in hojas.SelectMany(h => h.Detalles).Where(d => d.RemitoId == remitoId))
                {
                    HojaRuta hoja = hojas.First(h => h.Id == parada.HojaRutaId);
                    add(remito.FechaEmision.AddSeconds(1), string.Empty, "Assigned to route sheet", hoja.Numero);

                    if (hoja.FechaInicio.HasValue)
                    {
                        add(hoja.FechaInicio.Value, string.Empty, "Route started", hoja.Numero);
                    }

                    if (parada.Resultado.HasValue)
                    {
                        DateTime cuando = (hoja.FechaInicio ?? hoja.FechaPlanificada).Date + (parada.HoraLlegada ?? TimeSpan.Zero);
                        string detalle = Etiquetas.De(parada.Resultado.Value)
                            + (string.IsNullOrWhiteSpace(parada.MotivoNoEntrega) ? string.Empty : " - " + Mensajes.T("Reason: {0}", parada.MotivoNoEntrega))
                            + (string.IsNullOrWhiteSpace(parada.Observaciones) ? string.Empty : " - " + parada.Observaciones);

                        if (remito.FechaEntrega.HasValue && parada.EstadoEntrega == "COMPLETADA")
                        {
                            cuando = remito.FechaEntrega.Value;
                            detalle += " - " + Mensajes.T("Received by {0} (ID {1})", remito.ReceptorNombre, remito.ReceptorDocumento);
                        }

                        add(cuando, string.Empty, "Delivery result", detalle);
                    }
                }

                return 0;
            });

            foreach (RegistroBitacora r in bitacora.Where(x => x.Accion == "CancelarRemito" || x.Accion == "ReprogramarRemito"))
            {
                int corte = r.Descripcion.IndexOf(": ", StringComparison.Ordinal);
                string motivo = corte >= 0 ? r.Descripcion.Substring(corte + 2) : r.Descripcion;
                add(r.FechaHora, r.NombreUsuario, r.Accion == "CancelarRemito" ? "Cancelled" : "Rescheduled", Mensajes.T("Reason: {0}", motivo));
            }

            return eventos.OrderBy(e => e.Fecha).ToList();
        }

        /// <summary>Un envio rechazado vuelve a quedar disponible para una nueva hoja de ruta, dejando constancia del motivo.</summary>
        public void ReprogramarRemito(Guid remitoId, string motivo)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason for rescheduling is required.");
            }

            RemitoEntrega remito = _almacen.Ejecutar(unidad =>
            {
                RemitoEntrega existente = unidad.Conjunto<RemitoEntrega>().FirstOrDefault(r => r.Id == remitoId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The delivery note does not exist.");
                }

                if (existente.Estado != EstadoRemito.Rechazado && existente.Estado != EstadoRemito.Reprogramado)
                {
                    throw Mensajes.Regla("Only rejected or rescheduled deliveries can be rescheduled.");
                }

                existente.Estado = EstadoRemito.Reprogramado;
                return existente;
            });

            new BitacoraService().RegistrarAuditoria("Logística", "ReprogramarRemito", "RemitoEntrega", remito.Id, remito.Numero + ": " + motivo);
        }

        public List<RemitoDto> ObtenerRemitos(Guid? pedidoId, EstadoRemito? estado)
        {
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<string, decimal?> pesos = unidad.Conjunto<Producto>().ToList()
                    .ToDictionary(p => p.CodigoSku, p => p.PesoUnitarioKg);

                return unidad.Conjunto<RemitoEntrega>().Incluir("Detalles").ToList()
                    .Where(r => (!pedidoId.HasValue || r.PedidoId == pedidoId.Value)
                        && (!estado.HasValue || r.Estado == estado.Value))
                    .OrderByDescending(r => r.FechaEmision)
                    .Select(r =>
                    {
                        Pedido pedido;
                        pedidos.TryGetValue(r.PedidoId, out pedido);

                        return new RemitoDto
                        {
                            Id = r.Id,
                            Numero = r.Numero,
                            PedidoId = r.PedidoId,
                            Pedido = pedido == null ? string.Empty : pedido.Numero,
                            Cliente = Referencias.Nombre(clientes, r.ClienteId),
                            Fecha = r.FechaEmision,
                            Estado = Etiquetas.De(r.Estado),
                            EstadoClave = r.Estado,
                            Direccion = r.DireccionEntrega,
                            Items = r.Detalles.Count,
                            PesoKg = PesoDe(r, pesos),
                            PesoIncompleto = r.Detalles.Any(d => { decimal? p; return !(pesos.TryGetValue(d.Codigo, out p) && p.HasValue && p.Value > 0m); })
                        };
                    }).ToList();
            });
        }

        private static decimal PesoDe(RemitoEntrega remito, Dictionary<string, decimal?> pesos)
        {
            decimal total = 0m;

            foreach (DetalleRemito detalle in remito.Detalles)
            {
                decimal? peso;

                if (pesos.TryGetValue(detalle.Codigo, out peso) && peso.HasValue)
                {
                    total += detalle.Cantidad * peso.Value;
                }
            }

            return total;
        }

        public RemitoEntrega ObtenerRemito(Guid remitoId)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<RemitoEntrega>().Incluir("Detalles").FirstOrDefault(r => r.Id == remitoId));
        }

        public List<RemitoDto> ObtenerRemitosDisponiblesParaRuta()
        {
            return ObtenerRemitos(null, null)
                .Where(r => r.EstadoClave == EstadoRemito.Pendiente || r.EstadoClave == EstadoRemito.Reprogramado)
                .OrderBy(r => r.Direccion)
                .ToList();
        }

        // ---------------------------------------------------------------
        // Hojas de ruta
        // ---------------------------------------------------------------

        public HojaRuta CrearHojaRuta(
            Guid vehiculoId,
            Guid choferId,
            DateTime fechaPlanificada,
            List<Guid> remitosEnOrden,
            string observaciones)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (remitosEnOrden == null || remitosEnOrden.Count == 0)
            {
                throw Mensajes.Regla("Select at least one delivery note for the route.");
            }

            if (remitosEnOrden.Distinct().Count() != remitosEnOrden.Count)
            {
                throw Mensajes.Regla("A delivery note cannot appear twice in the same route.");
            }

            if (fechaPlanificada.Date < DateTime.Today)
            {
                throw Mensajes.Regla("The planned date cannot be in the past.");
            }

            HojaRuta hoja = _almacen.Ejecutar(unidad =>
            {
                Vehiculo vehiculo = unidad.Conjunto<Vehiculo>().FirstOrDefault(v => v.Id == vehiculoId);

                if (vehiculo == null || !vehiculo.Activo || vehiculo.Estado != "DISPONIBLE")
                {
                    throw Mensajes.Regla("The vehicle is not available.");
                }

                if (!vehiculo.DocumentacionVigente(fechaPlanificada))
                {
                    throw Mensajes.Regla("The insurance or the technical inspection of the vehicle has expired.");
                }

                Chofer chofer = unidad.Conjunto<Chofer>().FirstOrDefault(c => c.Id == choferId);

                if (chofer == null || !chofer.Activo)
                {
                    throw Mensajes.Regla("The driver is not available.");
                }

                if (chofer.VencimientoLicencia.Date < fechaPlanificada.Date)
                {
                    throw Mensajes.Regla("The driver's license has expired.");
                }

                if (!CategoriaHabilita(chofer.CategoriaLicencia, vehiculo.CapacidadKg))
                {
                    throw Mensajes.Regla("The license category does not allow driving this vehicle.");
                }

                Dictionary<string, decimal?> pesos = unidad.Conjunto<Producto>().ToList()
                    .ToDictionary(p => p.CodigoSku, p => p.PesoUnitarioKg);

                List<RemitoEntrega> remitos = new List<RemitoEntrega>();

                foreach (Guid remitoId in remitosEnOrden)
                {
                    RemitoEntrega remito = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles")
                        .FirstOrDefault(r => r.Id == remitoId);

                    if (remito == null)
                    {
                        throw Mensajes.Regla("The delivery note does not exist.");
                    }

                    if (remito.Estado != EstadoRemito.Pendiente && remito.Estado != EstadoRemito.Reprogramado)
                    {
                        throw Mensajes.Regla("The delivery note {0} is not pending.", remito.Numero);
                    }

                    remitos.Add(remito);
                }

                decimal carga = remitos.Sum(r => PesoDe(r, pesos));

                if (carga > vehiculo.CapacidadKg)
                {
                    throw Mensajes.Regla(
                        "The load ({0} kg) exceeds the capacity of the vehicle ({1} kg).",
                        carga.ToString("N1"), vehiculo.CapacidadKg.ToString("N1"));
                }

                List<string> numeros = unidad.Conjunto<HojaRuta>().Select(h => h.Numero).ToList();

                HojaRuta nueva = new HojaRuta
                {
                    Numero = Numeracion.Siguiente("HR", numeros),
                    VehiculoId = vehiculoId,
                    ChoferId = choferId,
                    FechaPlanificada = fechaPlanificada.Date,
                    Observaciones = observaciones ?? string.Empty
                };

                int orden = 1;

                foreach (RemitoEntrega remito in remitos)
                {
                    nueva.Detalles.Add(new DetalleHojaRuta
                    {
                        HojaRutaId = nueva.Id,
                        RemitoId = remito.Id,
                        OrdenEntrega = orden++
                    });

                    remito.Estado = EstadoRemito.Asignado;
                }

                unidad.Conjunto<HojaRuta>().Agregar(nueva);
                return nueva;
            });

            new BitacoraService().RegistrarAuditoria(
                "Logística", "CrearHojaRuta", "HojaRuta", hoja.Id,
                hoja.Numero + " con " + remitosEnOrden.Count + " remitos");

            return hoja;
        }

        public static bool CategoriaHabilita(string categoria, decimal capacidadKg)
        {
            string valor = (categoria ?? string.Empty).Trim().ToUpperInvariant();

            if (valor.Length == 0)
            {
                return false;
            }

            if (capacidadKg > CapacidadMaximaCategoriaB)
            {
                return valor.StartsWith("C") || valor.StartsWith("E");
            }

            return valor.StartsWith("B") || valor.StartsWith("C") || valor.StartsWith("E");
        }

        public void IniciarHojaRuta(Guid hojaId, decimal? kilometrosIniciales)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            List<Guid> pedidos = new List<Guid>();

            HojaRuta hoja = _almacen.Ejecutar(unidad =>
            {
                HojaRuta existente = unidad.Conjunto<HojaRuta>().Incluir("Detalles")
                    .FirstOrDefault(h => h.Id == hojaId);

                if (existente == null)
                {
                    throw Mensajes.Regla("The route sheet does not exist.");
                }

                if (existente.Estado != EstadoHojaRuta.Planificada)
                {
                    throw Mensajes.Regla("Only planned route sheets can be started.");
                }

                Vehiculo vehiculo = unidad.Conjunto<Vehiculo>().First(v => v.Id == existente.VehiculoId);

                foreach (DetalleHojaRuta parada in existente.Detalles)
                {
                    RemitoEntrega remito = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles")
                        .First(r => r.Id == parada.RemitoId);

                    if (!remito.FechaDespacho.HasValue)
                    {
                        DespacharStock(unidad, remito);
                        remito.FechaDespacho = DateTime.Now;
                    }

                    remito.Estado = EstadoRemito.EnTransito;

                    if (!pedidos.Contains(remito.PedidoId))
                    {
                        pedidos.Add(remito.PedidoId);
                    }
                }

                existente.Estado = EstadoHojaRuta.EnCurso;
                existente.FechaInicio = DateTime.Now;
                existente.KilometrosIniciales = kilometrosIniciales;
                vehiculo.Estado = "EN_RUTA";

                return existente;
            });

            foreach (Guid pedidoId in pedidos)
            {
                FactoryDataAccess.PedidoRepository.ActualizarEstado(pedidoId, EstadoPedido.EnDistribucion);
            }

            new BitacoraService().RegistrarAuditoria(
                "Logística", "IniciarHojaRuta", "HojaRuta", hoja.Id, hoja.Numero);
        }

        /// <summary>
        /// La mercadería sale del depósito al despachar: se consumen las
        /// reservas de cada línea del remito.
        /// </summary>
        private static void DespacharStock(IUnidadDeTrabajo unidad, RemitoEntrega remito)
        {
            foreach (DetalleRemito linea in remito.Detalles)
            {
                decimal restante = linea.Cantidad;

                foreach (ReservaStock reserva in InventarioMotor.ReservasActivasDeDetalle(unidad, linea.DetallePedidoId))
                {
                    if (restante <= 0m)
                    {
                        break;
                    }

                    decimal tomar = Math.Min(restante, reserva.CantidadPendiente);

                    if (tomar <= 0m)
                    {
                        continue;
                    }

                    InventarioMotor.ConsumirReserva(
                        unidad, reserva, tomar, TipoMovimientoStock.Despacho, remito.Numero, remito.Id);

                    restante -= tomar;
                }

                if (restante > 0m)
                {
                    throw Mensajes.Regla("There is not enough reserved stock to dispatch {0}.", linea.Codigo);
                }
            }
        }

        public void RegistrarEntrega(
            Guid detalleHojaRutaId,
            ResultadoEntrega resultado,
            string receptorNombre,
            string receptorDocumento,
            string motivo,
            string observaciones,
            decimal? kilometrosFinales)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            bool exitosa = resultado == ResultadoEntrega.EntregadoConforme
                || resultado == ResultadoEntrega.EntregaParcial;

            if (exitosa && (string.IsNullOrWhiteSpace(receptorNombre) || string.IsNullOrWhiteSpace(receptorDocumento)))
            {
                throw Mensajes.Regla("The name and the document of the person who received are required.");
            }

            if (!exitosa && string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("A delivery that was not completed must state the reason.");
            }

            Guid pedidoId = Guid.Empty;
            HojaRuta hojaResultado = null;

            _almacen.Ejecutar(unidad =>
            {
                HojaRuta hoja = unidad.Conjunto<HojaRuta>().Incluir("Detalles").ToList()
                    .FirstOrDefault(h => h.Detalles.Any(d => d.Id == detalleHojaRutaId));

                if (hoja == null)
                {
                    throw Mensajes.Regla("The stop does not exist.");
                }

                if (hoja.Estado != EstadoHojaRuta.EnCurso)
                {
                    throw Mensajes.Regla("Deliveries can only be registered on a route that is in progress.");
                }

                DetalleHojaRuta parada = hoja.Detalles.First(d => d.Id == detalleHojaRutaId);

                if (parada.EstadoEntrega != "PENDIENTE")
                {
                    throw Mensajes.Regla("The delivery of this stop was already registered.");
                }

                RemitoEntrega remito = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles")
                    .First(r => r.Id == parada.RemitoId);

                pedidoId = remito.PedidoId;

                if (exitosa)
                {
                    remito.Estado = EstadoRemito.Entregado;
                    remito.FechaEntrega = DateTime.Now;
                    remito.ReceptorNombre = receptorNombre.Trim();
                    remito.ReceptorDocumento = receptorDocumento.Trim();
                }
                else if (resultado == ResultadoEntrega.Rechazado)
                {
                    remito.Estado = EstadoRemito.Rechazado;
                }
                else
                {
                    remito.Estado = EstadoRemito.Reprogramado;
                }

                parada.Resultado = resultado;
                parada.EstadoEntrega = exitosa ? "COMPLETADA" : "NO_ENTREGADA";
                parada.HoraLlegada = DateTime.Now.TimeOfDay;
                parada.MotivoNoEntrega = exitosa ? string.Empty : motivo.Trim();
                parada.Observaciones = observaciones ?? string.Empty;

                unidad.Conjunto<ComprobanteEntrega>().Agregar(new ComprobanteEntrega
                {
                    DetalleHojaRutaId = parada.Id,
                    Resultado = resultado,
                    NombreReceptor = receptorNombre ?? string.Empty,
                    DocumentoReceptor = receptorDocumento ?? string.Empty,
                    MotivoRechazo = exitosa ? string.Empty : motivo.Trim(),
                    Observaciones = observaciones ?? string.Empty
                });

                if (hoja.Detalles.All(d => d.EstadoEntrega != "PENDIENTE"))
                {
                    hoja.Estado = EstadoHojaRuta.Finalizada;
                    hoja.FechaFinalizacion = DateTime.Now;
                    hoja.KilometrosFinales = kilometrosFinales;

                    Vehiculo vehiculo = unidad.Conjunto<Vehiculo>().First(v => v.Id == hoja.VehiculoId);
                    vehiculo.Estado = "DISPONIBLE";
                }

                hojaResultado = hoja;
            });

            ActualizarEstadoDelPedido(pedidoId);

            new BitacoraService().RegistrarAuditoria(
                "Logística", "RegistrarEntrega", "DetalleHojaRuta", detalleHojaRutaId, resultado.ToString());
        }

        private void ActualizarEstadoDelPedido(Guid pedidoId)
        {
            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                return;
            }

            bool todoEntregado = _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, decimal> entregadas = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles").ToList()
                    .Where(r => r.PedidoId == pedidoId && r.Estado == EstadoRemito.Entregado)
                    .SelectMany(r => r.Detalles)
                    .GroupBy(d => d.DetallePedidoId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

                foreach (DetallePedido detalle in pedido.Detalles.Where(EsLineaFisica))
                {
                    decimal entregada;
                    entregadas.TryGetValue(detalle.Id, out entregada);

                    if (entregada < detalle.Cantidad)
                    {
                        return false;
                    }
                }

                return true;
            });

            if (todoEntregado && pedido.Estado != EstadoPedido.Entregado)
            {
                FactoryDataAccess.PedidoRepository.ActualizarEstado(pedidoId, EstadoPedido.Entregado);
            }
        }

        public List<HojaRutaDto> ObtenerHojas(EstadoHojaRuta? estado)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Vehiculo> vehiculos = unidad.Conjunto<Vehiculo>().ToList().ToDictionary(v => v.Id);
                Dictionary<Guid, Chofer> choferes = unidad.Conjunto<Chofer>().ToList().ToDictionary(c => c.Id);

                return unidad.Conjunto<HojaRuta>().Incluir("Detalles").ToList()
                    .Where(h => !estado.HasValue || h.Estado == estado.Value)
                    .OrderByDescending(h => h.FechaPlanificada)
                    .Select(h =>
                    {
                        Vehiculo vehiculo;
                        Chofer chofer;
                        vehiculos.TryGetValue(h.VehiculoId, out vehiculo);
                        choferes.TryGetValue(h.ChoferId, out chofer);

                        return new HojaRutaDto
                        {
                            Id = h.Id,
                            Numero = h.Numero,
                            Fecha = h.FechaPlanificada,
                            Vehiculo = vehiculo == null ? string.Empty : vehiculo.Patente + " " + vehiculo.Marca,
                            Chofer = chofer == null ? string.Empty : chofer.NombreCompleto,
                            Estado = Etiquetas.De(h.Estado),
                            EstadoClave = h.Estado,
                            Paradas = h.Detalles.Count,
                            Resueltas = h.Detalles.Count(d => d.EstadoEntrega != "PENDIENTE")
                        };
                    }).ToList();
            });
        }

        public List<ParadaDto> ObtenerParadas(Guid hojaId)
        {
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                HojaRuta hoja = unidad.Conjunto<HojaRuta>().Incluir("Detalles").FirstOrDefault(h => h.Id == hojaId);

                if (hoja == null)
                {
                    return new List<ParadaDto>();
                }

                Dictionary<Guid, RemitoEntrega> remitos = unidad.Conjunto<RemitoEntrega>().ToList().ToDictionary(r => r.Id);

                return hoja.Detalles.OrderBy(d => d.OrdenEntrega).Select(d =>
                {
                    RemitoEntrega remito;
                    remitos.TryGetValue(d.RemitoId, out remito);

                    return new ParadaDto
                    {
                        DetalleId = d.Id,
                        RemitoId = d.RemitoId,
                        Orden = d.OrdenEntrega,
                        Remito = remito == null ? string.Empty : remito.Numero,
                        Cliente = remito == null ? string.Empty : Referencias.Nombre(clientes, remito.ClienteId),
                        Direccion = remito == null ? string.Empty : remito.DireccionEntrega,
                        Estado = d.EstadoEntrega == "PENDIENTE"
                            ? Mensajes.T("Pending")
                            : (d.EstadoEntrega == "COMPLETADA" ? Mensajes.T("Completed") : Mensajes.T("Not delivered")),
                        Resultado = Etiquetas.De(d.Resultado),
                        Pendiente = d.EstadoEntrega == "PENDIENTE"
                    };
                }).ToList();
            });
        }

        public HojaRuta ObtenerHoja(Guid hojaId)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<HojaRuta>().Incluir("Detalles").FirstOrDefault(h => h.Id == hojaId));
        }
    }
}
