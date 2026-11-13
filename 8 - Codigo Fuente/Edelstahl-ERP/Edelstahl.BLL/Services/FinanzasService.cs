using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Cobros a clientes, pagos a proveedores y consulta de las cuentas
    /// corrientes.
    /// </summary>
    public class FinanzasService
    {
        private readonly INegocioStore _almacen;

        public FinanzasService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public FinanzasService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Utilidades compartidas
        // ---------------------------------------------------------------

        /// <summary>
        /// Traduce el medio de pago escrito al registrar la seña al código
        /// del medio de pago configurado.
        /// </summary>
        public static string CodigoMedioPago(string texto)
        {
            string valor = (texto ?? string.Empty).ToLowerInvariant();

            if (valor.Contains("transf"))
            {
                return "TRANSFERENCIA";
            }

            if (valor.Contains("dep"))
            {
                return "DEPOSITO";
            }

            if (valor.Contains("cheq"))
            {
                return "CHEQUE";
            }

            if (valor.Contains("tarj") || valor.Contains("card"))
            {
                return "TARJETA";
            }

            return "EFECTIVO";
        }

        /// <summary>
        /// Fija la deuda del cliente. Pasa por el repositorio de clientes, que
        /// recalcula los dígitos verificadores de integridad.
        /// </summary>
        public static void ActualizarDeudaCliente(Guid clienteId, decimal deuda)
        {
            if (clienteId == Guid.Empty)
            {
                return;
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(clienteId);

            if (cliente == null)
            {
                return;
            }

            decimal nueva = Math.Max(0m, Math.Round(deuda, 2));

            if (cliente.DeudaActual == nueva)
            {
                return;
            }

            cliente.DeudaActual = nueva;
            FactoryDataAccess.ClienteRepository.Update(cliente);
        }

        public List<MedioPago> ObtenerMediosPago(bool soloActivos)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<MedioPago>().ToList()
                    .Where(m => !soloActivos || m.Activo)
                    .OrderBy(m => m.Nombre)
                    .ToList());
        }

        // ---------------------------------------------------------------
        // Cobros a clientes
        // ---------------------------------------------------------------

        /// <summary>
        /// Facturas con saldo por cobrar: lo abonado (por ejemplo la sena), lo que falta y como se prevee pagar.
        /// </summary>
        public List<CobroPendienteDto> ObtenerCobrosPendientes()
        {
            Autorizacion.Exigir(Permiso.CuentaCorrienteVer);

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();

            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().ToList()
                    .Where(f => f.SaldoPendiente > 0m && f.EstadoPago != EstadoPago.Anulada)
                    .OrderBy(f => f.FechaVencimiento)
                    .Select(f =>
                    {
                        Pedido pedido;
                        pedidos.TryGetValue(f.PedidoId, out pedido);
                        decimal abonado = f.MontoTotal - f.SaldoPendiente;

                        return new CobroPendienteDto
                        {
                            FacturaId = f.Id,
                            ClienteId = f.ClienteId,
                            PedidoId = f.PedidoId,
                            Cliente = Referencias.Nombre(clientes, f.ClienteId),
                            Pedido = pedido == null ? string.Empty : pedido.Numero,
                            Factura = f.Numero,
                            Total = f.MontoTotal,
                            Abonado = abonado,
                            Falta = f.SaldoPendiente,
                            Estado = abonado > 0m ? Mensajes.T("Deposit paid") : Mensajes.T("Not paid yet"),
                            FormaDePago = pedido == null ? string.Empty : pedido.CondicionPago,
                            Vencimiento = f.FechaVencimiento
                        };
                    }).ToList());
        }

        /// <summary>
        /// Anota como piensa pagar el cliente (efectivo, transferencia, cheque...) antes de que abone.
        /// </summary>
        public void ActualizarFormaDePago(Guid pedidoId, string formaDePago)
        {
            Autorizacion.Exigir(Permiso.CobrosRegistrar);

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            pedido.CondicionPago = (formaDePago ?? string.Empty).Trim();
            FactoryDataAccess.PedidoRepository.Update(pedido);

            new BitacoraService().RegistrarAuditoria("Finanzas", "FormaDePagoPrevista", "Pedido", pedido.Id,
                pedido.Numero + ": " + pedido.CondicionPago);
        }

        public CobroCliente RegistrarCobro(
            Guid clienteId,
            Guid? facturaId,
            Guid medioPagoId,
            decimal importe,
            string referencia,
            string entidadFinanciera,
            string observaciones)
        {
            Autorizacion.Exigir(Permiso.CobrosRegistrar);

            if (importe <= 0m)
            {
                throw Mensajes.Regla("The amount must be greater than zero.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(clienteId);

            if (cliente == null)
            {
                throw Mensajes.Regla("The customer does not exist.");
            }

            decimal deudaFinal = 0m;

            CobroCliente cobro = _almacen.Ejecutar(unidad =>
            {
                MedioPago medio = unidad.Conjunto<MedioPago>().FirstOrDefault(m => m.Id == medioPagoId);

                if (medio == null || !medio.Activo)
                {
                    throw Mensajes.Regla("The payment method is not available.");
                }

                if (medio.RequiereReferencia && string.IsNullOrWhiteSpace(referencia))
                {
                    throw Mensajes.Regla("This payment method requires a reference.");
                }

                if (medio.RequiereEntidadFinanciera && string.IsNullOrWhiteSpace(entidadFinanciera))
                {
                    throw Mensajes.Regla("This payment method requires the financial institution.");
                }

                Factura factura = null;

                if (facturaId.HasValue)
                {
                    Guid id = facturaId.Value;
                    factura = unidad.Conjunto<Factura>().FirstOrDefault(f => f.Id == id);

                    if (factura == null || factura.ClienteId != clienteId)
                    {
                        throw Mensajes.Regla("The invoice does not belong to the customer.");
                    }

                    if (factura.EstadoPago == EstadoPago.Anulada)
                    {
                        throw Mensajes.Regla("The invoice is void.");
                    }

                    if (importe > factura.SaldoPendiente)
                    {
                        throw Mensajes.Regla(
                            "The amount exceeds the pending balance of the invoice ({0}).",
                            factura.SaldoPendiente.ToString("N2"));
                    }
                }
                else if (importe > cliente.DeudaActual)
                {
                    throw Mensajes.Regla(
                        "The amount exceeds the current debt of the customer ({0}).",
                        cliente.DeudaActual.ToString("N2"));
                }

                List<string> numeros = unidad.Conjunto<CobroCliente>().Select(c => c.Numero).ToList();

                CobroCliente nuevo = new CobroCliente
                {
                    Numero = Numeracion.Siguiente("COB", numeros),
                    ClienteId = clienteId,
                    FacturaId = facturaId,
                    MedioPagoId = medioPagoId,
                    Importe = importe,
                    Referencia = referencia ?? string.Empty,
                    EntidadFinanciera = entidadFinanciera ?? string.Empty,
                    Observaciones = observaciones ?? string.Empty
                };

                unidad.Conjunto<CobroCliente>().Agregar(nuevo);
                unidad.Guardar();

                if (factura != null)
                {
                    factura.SaldoPendiente -= importe;
                    factura.EstadoPago = factura.SaldoPendiente <= 0m ? EstadoPago.Pagada : EstadoPago.Parcial;
                }

                decimal saldo = Math.Max(0m, cliente.DeudaActual - importe);
                deudaFinal = saldo;

                unidad.Conjunto<MovimientoCuentaCliente>().Agregar(new MovimientoCuentaCliente
                {
                    ClienteId = clienteId,
                    FacturaId = facturaId,
                    CobroId = nuevo.Id,
                    Tipo = TipoMovimientoCuenta.Cobro,
                    Haber = importe,
                    SaldoResultante = saldo,
                    Referencia = nuevo.Numero,
                    Observaciones = nuevo.Observaciones
                });

                return nuevo;
            });

            ActualizarDeudaCliente(clienteId, deudaFinal);

            new BitacoraService().RegistrarAuditoria(
                "Finanzas", "RegistrarCobro", "CobroCliente", cobro.Id,
                cobro.Numero + " por " + importe.ToString("N2"));

            return cobro;
        }

        public List<CobroDto> ObtenerCobros(Guid? clienteId)
        {
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Factura> facturas = unidad.Conjunto<Factura>().ToList().ToDictionary(f => f.Id);
                Dictionary<Guid, MedioPago> medios = unidad.Conjunto<MedioPago>().ToList().ToDictionary(m => m.Id);

                return unidad.Conjunto<CobroCliente>().ToList()
                    .Where(c => !clienteId.HasValue || c.ClienteId == clienteId.Value)
                    .OrderByDescending(c => c.FechaCobro)
                    .Select(c =>
                    {
                        Factura factura = null;

                        if (c.FacturaId.HasValue)
                        {
                            facturas.TryGetValue(c.FacturaId.Value, out factura);
                        }

                        MedioPago medio;
                        medios.TryGetValue(c.MedioPagoId, out medio);

                        return new CobroDto
                        {
                            Id = c.Id,
                            Numero = c.Numero,
                            Cliente = Referencias.Nombre(clientes, c.ClienteId),
                            Factura = factura == null ? string.Empty : factura.Numero,
                            Fecha = c.FechaCobro,
                            Medio = medio == null ? string.Empty : medio.Nombre,
                            Importe = c.Importe,
                            Referencia = c.Referencia,
                            Estado = c.Estado
                        };
                    }).ToList();
            });
        }

        // ---------------------------------------------------------------
        // Pagos a proveedores
        // ---------------------------------------------------------------

        public PagoProveedor RegistrarPagoProveedor(
            Guid proveedorId,
            Guid? ordenCompraId,
            Guid medioPagoId,
            decimal importe,
            string referencia,
            string entidadFinanciera,
            string observaciones)
        {
            Autorizacion.Exigir(Permiso.PagosRegistrar);

            if (importe <= 0m)
            {
                throw Mensajes.Regla("The amount must be greater than zero.");
            }

            PagoProveedor pago = _almacen.Ejecutar(unidad =>
            {
                Proveedor proveedor = unidad.Conjunto<Proveedor>().FirstOrDefault(p => p.Id == proveedorId);

                if (proveedor == null)
                {
                    throw Mensajes.Regla("The supplier does not exist.");
                }

                MedioPago medio = unidad.Conjunto<MedioPago>().FirstOrDefault(m => m.Id == medioPagoId);

                if (medio == null || !medio.Activo)
                {
                    throw Mensajes.Regla("The payment method is not available.");
                }

                if (medio.RequiereReferencia && string.IsNullOrWhiteSpace(referencia))
                {
                    throw Mensajes.Regla("This payment method requires a reference.");
                }

                if (medio.RequiereEntidadFinanciera && string.IsNullOrWhiteSpace(entidadFinanciera))
                {
                    throw Mensajes.Regla("This payment method requires the financial institution.");
                }

                if (importe > proveedor.SaldoCuenta)
                {
                    throw Mensajes.Regla(
                        "The amount exceeds the balance owed to the supplier ({0}).",
                        proveedor.SaldoCuenta.ToString("N2"));
                }

                OrdenCompra orden = null;

                if (ordenCompraId.HasValue)
                {
                    Guid id = ordenCompraId.Value;
                    orden = unidad.Conjunto<OrdenCompra>().FirstOrDefault(o => o.Id == id);

                    if (orden == null || orden.ProveedorId != proveedorId)
                    {
                        throw Mensajes.Regla("The purchase order does not belong to the supplier.");
                    }

                    if (importe > orden.SaldoPendiente)
                    {
                        throw Mensajes.Regla(
                            "The amount exceeds the pending balance of the purchase order ({0}).",
                            orden.SaldoPendiente.ToString("N2"));
                    }
                }

                List<string> numeros = unidad.Conjunto<PagoProveedor>().Select(p => p.Numero).ToList();

                PagoProveedor nuevo = new PagoProveedor
                {
                    Numero = Numeracion.Siguiente("PAG", numeros),
                    ProveedorId = proveedorId,
                    OrdenCompraId = ordenCompraId,
                    MedioPagoId = medioPagoId,
                    Importe = importe,
                    Referencia = referencia ?? string.Empty,
                    EntidadFinanciera = entidadFinanciera ?? string.Empty,
                    Observaciones = observaciones ?? string.Empty
                };

                unidad.Conjunto<PagoProveedor>().Agregar(nuevo);
                unidad.Guardar();

                if (orden != null)
                {
                    orden.SaldoPendiente -= importe;
                }

                proveedor.SaldoCuenta -= importe;

                unidad.Conjunto<MovimientoCuentaProveedor>().Agregar(new MovimientoCuentaProveedor
                {
                    ProveedorId = proveedorId,
                    OrdenCompraId = ordenCompraId,
                    PagoId = nuevo.Id,
                    Tipo = TipoMovimientoCuenta.Pago,
                    Debe = importe,
                    SaldoResultante = proveedor.SaldoCuenta,
                    Referencia = nuevo.Numero,
                    Observaciones = nuevo.Observaciones
                });

                return nuevo;
            });

            new BitacoraService().RegistrarAuditoria(
                "Finanzas", "RegistrarPagoProveedor", "PagoProveedor", pago.Id,
                pago.Numero + " por " + importe.ToString("N2"));

            return pago;
        }

        public List<PagoDto> ObtenerPagos(Guid? proveedorId)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Proveedor> proveedores = unidad.Conjunto<Proveedor>().ToList().ToDictionary(p => p.Id);
                Dictionary<Guid, OrdenCompra> ordenes = unidad.Conjunto<OrdenCompra>().ToList().ToDictionary(o => o.Id);
                Dictionary<Guid, MedioPago> medios = unidad.Conjunto<MedioPago>().ToList().ToDictionary(m => m.Id);

                return unidad.Conjunto<PagoProveedor>().ToList()
                    .Where(p => !proveedorId.HasValue || p.ProveedorId == proveedorId.Value)
                    .OrderByDescending(p => p.FechaPago)
                    .Select(p =>
                    {
                        Proveedor proveedor;
                        proveedores.TryGetValue(p.ProveedorId, out proveedor);
                        OrdenCompra orden = null;

                        if (p.OrdenCompraId.HasValue)
                        {
                            ordenes.TryGetValue(p.OrdenCompraId.Value, out orden);
                        }

                        MedioPago medio;
                        medios.TryGetValue(p.MedioPagoId, out medio);

                        return new PagoDto
                        {
                            Id = p.Id,
                            Numero = p.Numero,
                            Proveedor = proveedor == null ? string.Empty : proveedor.RazonSocial,
                            OrdenCompra = orden == null ? string.Empty : orden.Numero,
                            Fecha = p.FechaPago,
                            Medio = medio == null ? string.Empty : medio.Nombre,
                            Importe = p.Importe,
                            Referencia = p.Referencia,
                            Estado = p.Estado
                        };
                    }).ToList();
            });
        }

        // ---------------------------------------------------------------
        // Cuentas corrientes
        // ---------------------------------------------------------------

        public List<MovimientoCuentaDto> ObtenerCuentaCliente(Guid clienteId, DateTime? desde, DateTime? hasta)
        {
            return _almacen.Ejecutar(unidad =>
                Filtrar(unidad.Conjunto<MovimientoCuentaCliente>()
                    .Where(m => m.ClienteId == clienteId).ToList()
                    .Select(m => new MovimientoCuentaDto
                    {
                        Fecha = m.Fecha,
                        Tipo = Etiquetas.De(m.Tipo),
                        Referencia = m.Referencia,
                        Debe = m.Debe,
                        Haber = m.Haber,
                        Saldo = m.SaldoResultante,
                        Observaciones = m.Observaciones
                    }), desde, hasta));
        }

        public List<MovimientoCuentaDto> ObtenerCuentaProveedor(Guid proveedorId, DateTime? desde, DateTime? hasta)
        {
            return _almacen.Ejecutar(unidad =>
                Filtrar(unidad.Conjunto<MovimientoCuentaProveedor>()
                    .Where(m => m.ProveedorId == proveedorId).ToList()
                    .Select(m => new MovimientoCuentaDto
                    {
                        Fecha = m.Fecha,
                        Tipo = Etiquetas.De(m.Tipo),
                        Referencia = m.Referencia,
                        Debe = m.Debe,
                        Haber = m.Haber,
                        Saldo = m.SaldoResultante,
                        Observaciones = m.Observaciones
                    }), desde, hasta));
        }

        private static List<MovimientoCuentaDto> Filtrar(IEnumerable<MovimientoCuentaDto> movimientos, DateTime? desde, DateTime? hasta)
        {
            DateTime inicio = desde.HasValue ? desde.Value.Date : DateTime.MinValue;
            DateTime fin = hasta.HasValue ? hasta.Value.Date.AddDays(1) : DateTime.MaxValue;

            return movimientos
                .Where(m => m.Fecha >= inicio && m.Fecha < fin)
                .OrderBy(m => m.Fecha)
                .ToList();
        }

        public ResumenCuentaClienteDto ObtenerResumenCliente(Guid clienteId)
        {
            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(clienteId);

            if (cliente == null)
            {
                throw Mensajes.Regla("The customer does not exist.");
            }

            int pendientes = _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().Count(
                    f => f.ClienteId == clienteId && f.SaldoPendiente > 0m && f.EstadoPago != EstadoPago.Anulada));

            return new ResumenCuentaClienteDto
            {
                ClienteId = cliente.Id,
                Cliente = cliente.RazonSocial,
                LimiteCredito = cliente.LimiteCredito,
                DeudaActual = cliente.DeudaActual,
                CreditoDisponible = cliente.CalcularCreditoDisponible(),
                FacturasPendientes = pendientes
            };
        }
    }
}
