using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Services.Documents;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Arma los datos de cada documento imprimible y genera su PDF.
    /// </summary>
    public class DocumentosService
    {
        private readonly INegocioStore _almacen;

        public DocumentosService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public DocumentosService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        private static DatosReceptor ReceptorDe(Cliente cliente, string letra)
        {
            return new DatosReceptor
            {
                RazonSocial = cliente == null ? string.Empty : cliente.RazonSocial,
                Cuit = cliente == null ? string.Empty : cliente.CUIT,
                EtiquetaDocumento = cliente == null ? "CUIT" : cliente.EtiquetaDocumento,
                Domicilio = cliente == null ? string.Empty : cliente.DireccionFacturacion,
                Localidad = cliente == null ? string.Empty : (cliente.Localidad + " " + cliente.Provincia).Trim(),
                Email = cliente == null ? string.Empty : cliente.Email,
                CondicionIva = letra == "B"
                    ? Mensajes.T("Final consumer")
                    : Mensajes.T("VAT registered taxpayer")
            };
        }

        // ---------------------------------------------------------------
        // Facturas y notas de crédito
        // ---------------------------------------------------------------

        public DatosFactura ConstruirFactura(Guid facturaId)
        {
            Factura factura = new FacturacionService(_almacen).ObtenerFactura(facturaId);

            if (factura == null)
            {
                throw Mensajes.Regla("The invoice does not exist.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(factura.ClienteId);
            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(factura.PedidoId);
            string letra = factura.Tipo == TipoComprobante.FacturaA ? "A" : "B";

            decimal cobrado = _almacen.Ejecutar(unidad =>
                unidad.Conjunto<CobroCliente>()
                    .Where(c => c.FacturaId == facturaId)
                    .Select(c => (decimal?)c.Importe)
                    .Sum() ?? 0m);

            DatosFactura datos = new DatosFactura
            {
                Letra = letra,
                CodigoComprobante = letra == "A" ? "001" : "006",
                Numero = factura.Numero,
                Fecha = factura.FechaEmision,
                Vencimiento = factura.FechaVencimiento,
                Receptor = ReceptorDe(cliente, letra),
                CondicionVenta = factura.CondicionPago,
                Referencia = pedido == null ? string.Empty : Mensajes.T("Order {0}", pedido.Numero),
                Neto = factura.MontoNeto,
                Iva = factura.Iva,
                AlicuotaIva = factura.MontoNeto > 0m ? Math.Round(factura.Iva * 100m / factura.MontoNeto, 2) : 0m,
                Total = factura.MontoTotal,
                Anticipo = cobrado,
                Saldo = factura.SaldoPendiente,
                Observaciones = factura.Observaciones
            };

            foreach (DetalleFactura detalle in factura.Detalles)
            {
                datos.Lineas.Add(new LineaDocumento
                {
                    Codigo = detalle.Codigo,
                    Descripcion = detalle.Descripcion,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    DescuentoPorcentaje = detalle.PorcentajeDescuento,
                    Subtotal = detalle.Subtotal
                });
            }

            return datos;
        }

        public DatosFactura ConstruirNotaCredito(Guid notaId)
        {
            FacturacionService facturacion = new FacturacionService(_almacen);
            NotaCredito nota = facturacion.ObtenerNotaCredito(notaId);

            if (nota == null)
            {
                throw Mensajes.Regla("The credit note does not exist.");
            }

            Factura factura = facturacion.ObtenerFactura(nota.FacturaId);
            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(nota.ClienteId);
            string letra = nota.Tipo == TipoComprobante.FacturaA ? "A" : "B";

            DatosFactura datos = new DatosFactura
            {
                Letra = letra,
                CodigoComprobante = letra == "A" ? "003" : "008",
                EsNotaCredito = true,
                Numero = nota.Numero,
                Fecha = nota.Fecha,
                Receptor = ReceptorDe(cliente, letra),
                Referencia = factura == null ? string.Empty : Mensajes.T("Invoice {0}", factura.Numero),
                Motivo = nota.Motivo,
                Neto = nota.MontoNeto,
                Iva = nota.Iva,
                AlicuotaIva = nota.MontoNeto > 0m ? Math.Round(nota.Iva * 100m / nota.MontoNeto, 2) : 0m,
                Total = nota.MontoTotal
            };

            datos.Lineas.Add(new LineaDocumento
            {
                Codigo = "NC",
                Descripcion = nota.Motivo,
                Cantidad = 1m,
                PrecioUnitario = nota.MontoNeto,
                Subtotal = nota.MontoNeto
            });

            return datos;
        }

        public string GenerarFactura(Guid facturaId)
        {
            return new FacturaPdfService(ConstruirFactura(facturaId)).Generar();
        }

        public string GenerarNotaCredito(Guid notaId)
        {
            return new FacturaPdfService(ConstruirNotaCredito(notaId)).Generar();
        }

        // ---------------------------------------------------------------
        // Remito
        // ---------------------------------------------------------------

        public DatosRemito ConstruirRemito(Guid remitoId)
        {
            RemitoEntrega remito = new LogisticaService(_almacen).ObtenerRemito(remitoId);

            if (remito == null)
            {
                throw Mensajes.Regla("The delivery note does not exist.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(remito.ClienteId);
            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(remito.PedidoId);

            DatosRemito datos = new DatosRemito
            {
                Numero = remito.Numero,
                Fecha = remito.FechaEmision,
                Receptor = ReceptorDe(cliente, "A"),
                DireccionEntrega = remito.DireccionEntrega,
                Pedido = pedido == null ? string.Empty : pedido.Numero,
                Observaciones = remito.Observaciones
            };

            _almacen.Ejecutar(unidad =>
            {
                Dictionary<string, string> unidades = unidad.Conjunto<Producto>().ToList()
                    .ToDictionary(p => p.CodigoSku, p => p.UnidadMedida, StringComparer.OrdinalIgnoreCase);

                foreach (DetalleRemito detalle in remito.Detalles)
                {
                    string unidadMedida;
                    unidades.TryGetValue(detalle.Codigo, out unidadMedida);

                    datos.Lineas.Add(new LineaRemito
                    {
                        Codigo = detalle.Codigo,
                        Descripcion = detalle.Descripcion,
                        Cantidad = detalle.Cantidad,
                        Unidad = unidadMedida ?? string.Empty
                    });
                }

                HojaRuta hoja = unidad.Conjunto<HojaRuta>().Incluir("Detalles").ToList()
                    .FirstOrDefault(h => h.Detalles.Any(d => d.RemitoId == remitoId) && h.Estado != EstadoHojaRuta.Cancelada);

                if (hoja != null)
                {
                    Vehiculo vehiculo = unidad.Conjunto<Vehiculo>().FirstOrDefault(v => v.Id == hoja.VehiculoId);
                    Chofer chofer = unidad.Conjunto<Chofer>().FirstOrDefault(c => c.Id == hoja.ChoferId);

                    datos.HojaRuta = hoja.Numero;
                    datos.Vehiculo = vehiculo == null ? string.Empty : vehiculo.Patente + " " + vehiculo.Marca + " " + vehiculo.Modelo;
                    datos.Chofer = chofer == null ? string.Empty : chofer.NombreCompleto;
                }
            });

            return datos;
        }

        public string GenerarRemito(Guid remitoId)
        {
            return new RemitoPdfService(ConstruirRemito(remitoId)).Generar();
        }

        // ---------------------------------------------------------------
        // Orden de compra
        // ---------------------------------------------------------------

        public DatosOrdenCompra ConstruirOrdenCompra(Guid ordenId)
        {
            ComprasService compras = new ComprasService(_almacen);
            OrdenCompra orden = compras.ObtenerOrden(ordenId);

            if (orden == null)
            {
                throw Mensajes.Regla("The purchase order does not exist.");
            }

            Proveedor proveedor = compras.ObtenerProveedores(string.Empty, false)
                .FirstOrDefault(p => p.Id == orden.ProveedorId);

            DatosOrdenCompra datos = new DatosOrdenCompra
            {
                Numero = orden.Numero,
                Fecha = orden.FechaEmision,
                Entrega = orden.FechaEntregaEstimada,
                Proveedor = proveedor == null ? string.Empty : proveedor.RazonSocial,
                ProveedorCuit = proveedor == null ? string.Empty : proveedor.Cuit,
                ProveedorDomicilio = proveedor == null
                    ? string.Empty
                    : (proveedor.Direccion + " " + proveedor.Localidad).Trim(),
                Contacto = proveedor == null ? string.Empty : proveedor.ContactoComercial,
                CondicionPago = orden.CondicionPago,
                Neto = orden.MontoNeto,
                Iva = orden.Impuestos,
                Total = orden.MontoTotal,
                Observaciones = orden.Observaciones
            };

            _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Producto> productos = unidad.Conjunto<Producto>().ToList().ToDictionary(p => p.Id);

                foreach (DetalleOrdenCompra detalle in orden.Detalles)
                {
                    Producto producto;
                    productos.TryGetValue(detalle.ProductoId, out producto);

                    datos.Lineas.Add(new LineaDocumento
                    {
                        Codigo = producto == null ? string.Empty : producto.CodigoSku,
                        Descripcion = producto == null ? string.Empty : producto.Nombre,
                        Cantidad = detalle.CantidadSolicitada,
                        Unidad = detalle.UnidadMedida,
                        PrecioUnitario = detalle.CostoUnitario,
                        DescuentoPorcentaje = detalle.PorcentajeDescuento,
                        Subtotal = detalle.Subtotal
                    });
                }
            });

            return datos;
        }

        public string GenerarOrdenCompra(Guid ordenId)
        {
            return new OrdenCompraPdfService(ConstruirOrdenCompra(ordenId)).Generar();
        }
    }
}
