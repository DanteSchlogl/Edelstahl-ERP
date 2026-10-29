using System;
using System.Globalization;
using Edelstahl.Domain.Comercial;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Genera el comprobante PDF de un pedido confirmado.
    /// </summary>
    public class ComprobantePedidoPdfService
    {
        public string Generar(Pedido pedido, Cliente cliente)
        {
            if (pedido == null)
            {
                throw new ArgumentNullException(nameof(pedido));
            }

            if (cliente == null)
            {
                throw new ArgumentNullException(nameof(cliente));
            }

            if (pedido.Estado == EstadoPedido.Pendiente || pedido.Estado == EstadoPedido.Cancelado)
            {
                throw new InvalidOperationException(
                    FormatoDocumento.T("A receipt can only be generated for a confirmed order."));
            }

            return new DocumentoPedido(pedido, cliente).Generar();
        }

        private sealed class DocumentoPedido : DocumentoPdfBase
        {
            private readonly Pedido _pedido;
            private readonly Cliente _cliente;
            private readonly DatosEmisor _emisor = DatosEmisor.Obtener();

            public DocumentoPedido(Pedido pedido, Cliente cliente)
            {
                _pedido = pedido;
                _cliente = cliente;
            }

            protected override string NombreArchivo
            {
                get { return "Comprobante-" + _pedido.Numero; }
            }

            private string Moneda(decimal valor)
            {
                string simbolo = _pedido.Moneda == Edelstahl.Domain.Comercial.Moneda.DolaresEstadounidenses ? "USD " : "$ ";
                return simbolo + valor.ToString("N2", new CultureInfo("es-AR"));
            }

            protected override void ComponerEncabezado(IContainer contenedor, VarianteDocumento variante)
            {
                contenedor.Background(Colors.BlueGrey.Lighten4).Padding(14).Row(fila =>
                {
                    byte[] logo = FormatoDocumento.Logo();

                    if (logo != null)
                    {
                        fila.ConstantItem(58).PaddingRight(8).Image(logo).FitArea();
                    }

                    fila.RelativeItem().Column(columna =>
                    {
                        columna.Item().Text(_emisor.RazonSocial).FontSize(16).Bold().FontColor(Colors.BlueGrey.Darken3);
                        columna.Item().PaddingTop(3).Text(_emisor.Domicilio).FontColor(Colors.BlueGrey.Darken2);
                        columna.Item().PaddingTop(2).Text("CUIT " + _emisor.Cuit).FontColor(Colors.BlueGrey.Darken2);
                    });

                    fila.ConstantItem(210).Column(columna =>
                    {
                        columna.Item().AlignRight().Text(FormatoDocumento.T("ORDER RECEIPT")).FontSize(15).Bold()
                            .FontColor(Colors.BlueGrey.Darken3);
                        columna.Item().PaddingTop(4).AlignRight().Text(_pedido.Numero).FontSize(12).Bold();
                        columna.Item().PaddingTop(3).AlignRight()
                            .Text(FormatoDocumento.T("Date") + ": " + _pedido.FechaPedido.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            }

            protected override void ComponerCuerpo(IContainer contenedor)
            {
                contenedor.Column(columna =>
                {
                    columna.Spacing(12);

                    columna.Item().Border(1).BorderColor(Colors.BlueGrey.Lighten3).Padding(10).Row(fila =>
                    {
                        fila.RelativeItem().Column(datos =>
                        {
                            DatoRotulado(datos, FormatoDocumento.T("Customer"), _cliente.RazonSocial);
                            DatoRotulado(datos, _cliente.EtiquetaDocumento, _cliente.CUIT);
                            DatoRotulado(datos, FormatoDocumento.T("Email"), _cliente.Email);
                        });

                        fila.RelativeItem().PaddingLeft(10).Column(datos =>
                        {
                            DatoRotulado(datos, FormatoDocumento.T("Phone"), _cliente.Telefono);
                            DatoRotulado(datos, FormatoDocumento.T("City"), _cliente.Localidad);
                            DatoRotulado(datos, FormatoDocumento.T("Delivery address"), _cliente.DireccionEntrega);
                        });
                    });

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn();
                            c.ConstantColumn(48);
                            c.ConstantColumn(82);
                            c.ConstantColumn(44);
                            c.ConstantColumn(88);
                        });

                        tabla.Header(cabecera =>
                        {
                            cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Code"));
                            cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Description"));
                            cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Qty"));
                            cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Unit price"));
                            cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text("% " + FormatoDocumento.T("Disc."));
                            cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Subtotal"));
                        });

                        foreach (DetallePedido detalle in _pedido.Detalles)
                        {
                            tabla.Cell().Element(CeldaDeDatos).Text(detalle.Codigo);

                            tabla.Cell().Element(CeldaDeDatos).Column(texto =>
                            {
                                texto.Item().Text(detalle.Descripcion);

                                if (!string.IsNullOrWhiteSpace(detalle.DescripcionTecnica))
                                {
                                    texto.Item().Text(detalle.DescripcionTecnica).FontSize(7).FontColor(Colors.Grey.Darken1);
                                }
                            });

                            tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(detalle.Cantidad));
                            tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(Moneda(detalle.PrecioUnitario));
                            tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(detalle.PorcentajeDescuento));
                            tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(Moneda(detalle.CalcularSubtotal()));
                        }
                    });

                    columna.Item().AlignRight().Width(270).Column(totales =>
                    {
                        FilaDeTotal(totales, FormatoDocumento.T("Subtotal"), Moneda(_pedido.CalcularSubtotal()), false);

                        if (_pedido.PorcentajeDescuentoGeneral > 0m)
                        {
                            FilaDeTotal(totales, FormatoDocumento.T("General discount"),
                                "-" + Moneda(_pedido.CalcularDescuentoGeneral()), false);
                        }

                        if (_pedido.PorcentajeRecargo > 0m)
                        {
                            FilaDeTotal(totales, FormatoDocumento.T("Surcharge"), Moneda(_pedido.CalcularRecargo()), false);
                        }

                        FilaDeTotal(totales, "IVA " + FormatoDocumento.Numero(_pedido.PorcentajeIVA) + "%",
                            Moneda(_pedido.CalcularIVA()), false);
                        FilaDeTotal(totales, FormatoDocumento.T("Total"), Moneda(_pedido.CalcularTotal()), true);

                        if (_pedido.ImporteAnticipo > 0m)
                        {
                            FilaDeTotal(totales, FormatoDocumento.T("Deposit received"), Moneda(_pedido.ImporteAnticipo), false);
                            FilaDeTotal(totales, FormatoDocumento.T("Balance to invoice"), Moneda(_pedido.CalcularSaldoPendiente()), false);
                        }
                    });

                    columna.Item().Border(1).BorderColor(Colors.BlueGrey.Lighten3).Padding(10).Column(datos =>
                    {
                        DatoRotulado(datos, FormatoDocumento.T("Payment terms"), _pedido.CondicionPago);
                        DatoRotulado(datos, FormatoDocumento.T("Delivery term"), _pedido.PlazoEntrega);
                        DatoRotulado(datos, FormatoDocumento.T("Delivery included"),
                            FormatoDocumento.T(_pedido.EntregaIncluida ? "Yes" : "No"));
                        DatoRotulado(datos, FormatoDocumento.T("Notes"), _pedido.Observaciones);
                    });
                });
            }
        }
    }
}
