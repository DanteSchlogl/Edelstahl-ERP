using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Emisión de facturas A y B y de notas de crédito. Cada comprobante
    /// genera su movimiento en la cuenta corriente y actualiza la deuda
    /// del cliente.
    /// </summary>
    public class FacturacionService
    {
        public const int PuntoVenta = 1;
        public const int DiasVencimientoPorDefecto = 30;

        private readonly INegocioStore _almacen;

        public FacturacionService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public FacturacionService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Facturas
        // ---------------------------------------------------------------

        public Factura EmitirFactura(Guid pedidoId, TipoComprobante tipo)
        {
            Autorizacion.Exigir(Permiso.FacturasEmitir);

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            if (pedido.Estado == EstadoPedido.Pendiente || pedido.Estado == EstadoPedido.Cancelado)
            {
                throw Mensajes.Regla("Only confirmed orders can be invoiced.");
            }

            if (pedido.Detalles.Count == 0)
            {
                throw Mensajes.Regla("The order has no lines to invoice.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(pedido.ClienteId);

            if (cliente == null || !cliente.Activo)
            {
                throw Mensajes.Regla("The customer is not active.");
            }

            if (tipo == TipoComprobante.FacturaA && SoloDigitos(cliente.CUIT).Length != 11)
            {
                throw Mensajes.Regla("Invoice A requires a customer with a valid CUIT.");
            }

            if (tipo == TipoComprobante.FacturaA && cliente.Tipo == TipoPersona.Particular)
            {
                throw Mensajes.Regla("Invoice A is only for companies (CUIT). Use Invoice B for individuals.");
            }

            decimal factor = pedido.Moneda == Moneda.DolaresEstadounidenses ? pedido.TipoCambio : 1m;

            if (factor <= 0m)
            {
                throw Mensajes.Regla("The exchange rate is not valid.");
            }

            decimal deudaFinal = 0m;

            Factura factura = _almacen.Ejecutar(unidad =>
            {
                if (unidad.Conjunto<Factura>().Any(
                    f => f.PedidoId == pedidoId && f.EstadoPago != EstadoPago.Anulada))
                {
                    throw Mensajes.Regla("The order already has an invoice.");
                }

                List<string> numeros = unidad.Conjunto<Factura>().Select(f => f.Numero).ToList();

                Factura nueva = new Factura
                {
                    PedidoId = pedido.Id,
                    ClienteId = cliente.Id,
                    PuntoVenta = PuntoVenta,
                    Numero = Numeracion.SiguienteFiscal(PuntoVenta, numeros),
                    Tipo = tipo,
                    Moneda = 1,
                    TipoCambio = factor,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Today.AddDays(DiasVencimientoPorDefecto),
                    CondicionPago = pedido.CondicionPago ?? string.Empty,
                    Observaciones = pedido.Moneda == Moneda.DolaresEstadounidenses
                        ? "Pedido " + pedido.Numero + " en USD, tipo de cambio " + factor.ToString("N2")
                        : "Pedido " + pedido.Numero
                };

                foreach (DetallePedido detalle in pedido.Detalles)
                {
                    nueva.Detalles.Add(new DetalleFactura
                    {
                        FacturaId = nueva.Id,
                        Codigo = detalle.Codigo,
                        Descripcion = detalle.Descripcion,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = Math.Round(detalle.PrecioUnitario * factor, 2),
                        PorcentajeDescuento = detalle.PorcentajeDescuento,
                        Subtotal = Math.Round(detalle.CalcularSubtotal() * factor, 2)
                    });
                }

                nueva.MontoNeto = Math.Round(pedido.CalcularNetoGravado() * factor, 2);
                nueva.Iva = Math.Round(pedido.CalcularIVA() * factor, 2);
                nueva.MontoTotal = nueva.MontoNeto + nueva.Iva;

                decimal anticipo = Math.Min(
                    nueva.MontoTotal,
                    Math.Round(pedido.ImporteAnticipo * factor, 2));

                nueva.SaldoPendiente = nueva.MontoTotal - anticipo;
                nueva.EstadoPago = nueva.SaldoPendiente <= 0m
                    ? EstadoPago.Pagada
                    : (anticipo > 0m ? EstadoPago.Parcial : EstadoPago.Pendiente);

                unidad.Conjunto<Factura>().Agregar(nueva);
                unidad.Guardar();

                decimal saldo = cliente.DeudaActual + nueva.MontoTotal;

                unidad.Conjunto<MovimientoCuentaCliente>().Agregar(new MovimientoCuentaCliente
                {
                    ClienteId = cliente.Id,
                    FacturaId = nueva.Id,
                    Tipo = TipoMovimientoCuenta.Factura,
                    Debe = nueva.MontoTotal,
                    SaldoResultante = saldo,
                    Referencia = nueva.Numero
                });

                if (anticipo > 0m)
                {
                    RegistrarSenaComoCobro(unidad, pedido, cliente, nueva, anticipo, ref saldo);
                }

                deudaFinal = saldo;
                return nueva;
            });

            FinanzasService.ActualizarDeudaCliente(cliente.Id, deudaFinal);

            new BitacoraService().RegistrarAuditoria(
                "Finanzas", "EmitirFactura", "Factura", factura.Id,
                factura.Numero + " (" + Etiquetas.De(tipo) + ") por " + factura.MontoTotal.ToString("N2"));

            return factura;
        }

        private static void RegistrarSenaComoCobro(
            IUnidadDeTrabajo unidad,
            Pedido pedido,
            Cliente cliente,
            Factura factura,
            decimal importe,
            ref decimal saldo)
        {
            string codigoMedio = FinanzasService.CodigoMedioPago(pedido.MedioPagoAnticipo);
            MedioPago medio = unidad.Conjunto<MedioPago>().FirstOrDefault(m => m.Codigo == codigoMedio);

            if (medio == null)
            {
                medio = unidad.Conjunto<MedioPago>().FirstOrDefault(m => m.Activo);
            }

            if (medio == null)
            {
                throw Mensajes.Regla("There are no payment methods configured.");
            }

            List<string> numeros = unidad.Conjunto<CobroCliente>().Select(c => c.Numero).ToList();

            CobroCliente cobro = new CobroCliente
            {
                Numero = Numeracion.Siguiente("COB", numeros),
                ClienteId = cliente.Id,
                FacturaId = factura.Id,
                MedioPagoId = medio.Id,
                FechaCobro = pedido.FechaAnticipo ?? DateTime.Now,
                Importe = importe,
                Referencia = pedido.ComprobanteAnticipo ?? string.Empty,
                Observaciones = "Seña del pedido " + pedido.Numero
            };

            unidad.Conjunto<CobroCliente>().Agregar(cobro);
            unidad.Guardar();

            saldo = Math.Max(0m, saldo - importe);

            unidad.Conjunto<MovimientoCuentaCliente>().Agregar(new MovimientoCuentaCliente
            {
                ClienteId = cliente.Id,
                FacturaId = factura.Id,
                CobroId = cobro.Id,
                Tipo = TipoMovimientoCuenta.Cobro,
                Haber = importe,
                SaldoResultante = saldo,
                Referencia = cobro.Numero,
                Observaciones = cobro.Observaciones
            });
        }

        public List<FacturaDto> ObtenerFacturas(Guid? clienteId, EstadoPago? estado, bool soloConSaldo)
        {
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();
            Dictionary<Guid, Pedido> pedidos = Referencias.Pedidos();

            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().ToList()
                    .Where(f => (!clienteId.HasValue || f.ClienteId == clienteId.Value)
                        && (!estado.HasValue || f.EstadoPago == estado.Value)
                        && (!soloConSaldo || f.SaldoPendiente > 0m))
                    .OrderByDescending(f => f.FechaEmision)
                    .Select(f =>
                    {
                        Pedido pedido;
                        pedidos.TryGetValue(f.PedidoId, out pedido);

                        return new FacturaDto
                        {
                            Id = f.Id,
                            Numero = f.Numero,
                            Tipo = Etiquetas.De(f.Tipo),
                            PedidoId = f.PedidoId,
                            Pedido = pedido == null ? string.Empty : pedido.Numero,
                            ClienteId = f.ClienteId,
                            Cliente = Referencias.Nombre(clientes, f.ClienteId),
                            Emision = f.FechaEmision,
                            Vencimiento = f.FechaVencimiento,
                            Neto = f.MontoNeto,
                            Iva = f.Iva,
                            Total = f.MontoTotal,
                            Saldo = f.SaldoPendiente,
                            Estado = Etiquetas.De(EstadoVisible(f)),
                            EstadoClave = EstadoVisible(f)
                        };
                    }).ToList());
        }

        /// <summary>
        /// Una factura con saldo cuyo vencimiento pasó se muestra como vencida.
        /// </summary>
        public static EstadoPago EstadoVisible(Factura factura)
        {
            if (factura.SaldoPendiente > 0m
                && factura.FechaVencimiento.HasValue
                && factura.FechaVencimiento.Value.Date < DateTime.Today
                && factura.EstadoPago != EstadoPago.Anulada)
            {
                return EstadoPago.Vencida;
            }

            return factura.EstadoPago;
        }

        public Factura ObtenerFactura(Guid facturaId)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().Incluir("Detalles").FirstOrDefault(f => f.Id == facturaId));
        }

        public Factura ObtenerFacturaDePedido(Guid pedidoId)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().Incluir("Detalles")
                    .FirstOrDefault(f => f.PedidoId == pedidoId && f.EstadoPago != EstadoPago.Anulada));
        }

        // ---------------------------------------------------------------
        // Notas de crédito
        // ---------------------------------------------------------------

        public NotaCredito EmitirNotaCredito(Guid facturaId, Guid? devolucionId, decimal montoNeto, string motivo)
        {
            Autorizacion.Exigir(Permiso.FacturasEmitir);

            if (montoNeto <= 0m)
            {
                throw Mensajes.Regla("The amount of the credit note must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("The reason of the credit note is required.");
            }

            decimal deudaFinal = 0m;
            Guid clienteId = Guid.Empty;
            decimal aplicado = 0m;

            NotaCredito nota = _almacen.Ejecutar(unidad =>
            {
                Factura factura = unidad.Conjunto<Factura>().FirstOrDefault(f => f.Id == facturaId);

                if (factura == null)
                {
                    throw Mensajes.Regla("The invoice does not exist.");
                }

                if (factura.EstadoPago == EstadoPago.Anulada)
                {
                    throw Mensajes.Regla("The invoice is already void.");
                }

                if (devolucionId.HasValue)
                {
                    Guid id = devolucionId.Value;
                    Devolucion devolucion = unidad.Conjunto<Devolucion>().FirstOrDefault(d => d.Id == id);

                    if (devolucion == null || devolucion.Estado != EstadoDevolucion.Resuelta)
                    {
                        throw Mensajes.Regla("A credit note requires an approved resolution of the return.");
                    }

                    if (devolucion.PedidoId != factura.PedidoId)
                    {
                        throw Mensajes.Regla("The return does not belong to the order of the invoice.");
                    }
                }

                decimal ivaPorcentaje = factura.MontoNeto > 0m
                    ? Math.Round(factura.Iva * 100m / factura.MontoNeto, 2)
                    : 0m;

                decimal iva = Math.Round(montoNeto * ivaPorcentaje / 100m, 2);
                decimal total = montoNeto + iva;

                decimal yaAcreditado = unidad.Conjunto<NotaCredito>()
                    .Where(n => n.FacturaId == factura.Id && n.Estado == "EMITIDA")
                    .Select(n => (decimal?)n.MontoTotal)
                    .Sum() ?? 0m;

                if (total + yaAcreditado > factura.MontoTotal + 0.01m)
                {
                    throw Mensajes.Regla("The credit note cannot exceed the balance of the invoice.");
                }

                Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(factura.ClienteId);
                clienteId = factura.ClienteId;

                List<string> numeros = unidad.Conjunto<NotaCredito>().Select(n => n.Numero).ToList();

                NotaCredito creada = new NotaCredito
                {
                    Numero = Numeracion.SiguienteFiscal(PuntoVenta, numeros),
                    FacturaId = factura.Id,
                    ClienteId = factura.ClienteId,
                    DevolucionId = devolucionId,
                    Tipo = factura.Tipo,
                    Motivo = motivo.Trim(),
                    MontoNeto = montoNeto,
                    Iva = iva,
                    MontoTotal = total
                };

                unidad.Conjunto<NotaCredito>().Agregar(creada);
                unidad.Guardar();

                aplicado = Math.Min(total, factura.SaldoPendiente);
                factura.SaldoPendiente -= aplicado;

                bool acreditadaTotal = yaAcreditado + total >= factura.MontoTotal - 0.01m;

                if (acreditadaTotal && factura.SaldoPendiente <= 0m)
                {
                    factura.EstadoPago = EstadoPago.Anulada;
                }
                else if (factura.SaldoPendiente <= 0m)
                {
                    factura.EstadoPago = EstadoPago.Pagada;
                }
                else if (factura.SaldoPendiente < factura.MontoTotal)
                {
                    factura.EstadoPago = EstadoPago.Parcial;
                }

                decimal saldo = Math.Max(0m, (cliente == null ? 0m : cliente.DeudaActual) - aplicado);
                deudaFinal = saldo;

                unidad.Conjunto<MovimientoCuentaCliente>().Agregar(new MovimientoCuentaCliente
                {
                    ClienteId = factura.ClienteId,
                    FacturaId = factura.Id,
                    NotaCreditoId = creada.Id,
                    Tipo = TipoMovimientoCuenta.NotaCredito,
                    Haber = total,
                    SaldoResultante = saldo,
                    Referencia = creada.Numero,
                    Observaciones = creada.Motivo
                });

                return creada;
            });

            FinanzasService.ActualizarDeudaCliente(clienteId, deudaFinal);

            new BitacoraService().RegistrarAuditoria(
                "Finanzas", "EmitirNotaCredito", "NotaCredito", nota.Id,
                nota.Numero + " por " + nota.MontoTotal.ToString("N2"));

            return nota;
        }

        public List<NotaCreditoDto> ObtenerNotasCredito(Guid? facturaId)
        {
            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Factura> facturas = unidad.Conjunto<Factura>().ToList().ToDictionary(f => f.Id);

                return unidad.Conjunto<NotaCredito>().ToList()
                    .Where(n => !facturaId.HasValue || n.FacturaId == facturaId.Value)
                    .OrderByDescending(n => n.Fecha)
                    .Select(n =>
                    {
                        Factura factura;
                        facturas.TryGetValue(n.FacturaId, out factura);

                        return new NotaCreditoDto
                        {
                            Id = n.Id,
                            Numero = n.Numero,
                            Factura = factura == null ? string.Empty : factura.Numero,
                            Cliente = Referencias.Nombre(clientes, n.ClienteId),
                            Fecha = n.Fecha,
                            Motivo = n.Motivo,
                            Neto = n.MontoNeto,
                            Iva = n.Iva,
                            Total = n.MontoTotal
                        };
                    }).ToList();
            });
        }

        public NotaCredito ObtenerNotaCredito(Guid notaId)
        {
            return _almacen.Ejecutar(unidad => unidad.Conjunto<NotaCredito>().FirstOrDefault(n => n.Id == notaId));
        }

        private static string SoloDigitos(string valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }
    }
}
