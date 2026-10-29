using System;
using System.Globalization;
using Edelstahl.Domain.Comercial;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Genera el presupuesto en PDF para que el cliente se lo lleve y lo evalúe con calma.
    /// Indica hasta cuándo es válido y que los precios pueden variar pasada esa fecha.
    /// </summary>
    public class PresupuestoPdfService
    {
        public string Generar(Presupuesto presupuesto, Cliente cliente)
        {
            if (presupuesto == null)
            {
                throw new ArgumentNullException(nameof(presupuesto));
            }

            if (cliente == null)
            {
                throw new ArgumentNullException(nameof(cliente));
            }

            return new DocumentoPresupuesto(presupuesto, cliente).Generar();
        }

        /// <summary>Genera el presupuesto como imagenes (una por pagina), para revisarlo sin abrir el PDF.</summary>
        public System.Collections.Generic.List<string> GenerarImagenes(Presupuesto presupuesto, Cliente cliente, string prefijo)
        {
            return new DocumentoPresupuesto(presupuesto, cliente).GenerarImagenes(prefijo);
        }

        private sealed class DocumentoPresupuesto : DocumentoPdfBase
        {
            private readonly Presupuesto _presupuesto;
            private readonly Cliente _cliente;
            private readonly DatosEmisor _emisor = DatosEmisor.Obtener();

            public DocumentoPresupuesto(Presupuesto presupuesto, Cliente cliente)
            {
                _presupuesto = presupuesto;
                _cliente = cliente;
            }

            protected override string NombreArchivo
            {
                get { return "Presupuesto-" + _presupuesto.Numero; }
            }

            protected override string Politica
            {
                get { return FormatoDocumento.PoliticaDeEntrega(); }
            }

            private bool EnDolares
            {
                get { return _presupuesto.Moneda == Edelstahl.Domain.Comercial.Moneda.DolaresEstadounidenses; }
            }

            private string Moneda(decimal valor)
            {
                return (EnDolares ? "USD " : "$ ") + valor.ToString("N2", new CultureInfo("es-AR"));
            }

            private int DiasDeValidez
            {
                get { return Math.Max(1, (int)Math.Round((_presupuesto.FechaVencimiento.Date - _presupuesto.FechaEmision.Date).TotalDays)); }
            }

            private bool Vencido
            {
                get
                {
                    return _presupuesto.FechaVencimiento.Date < DateTime.Today
                        && _presupuesto.Estado != EstadoPresupuesto.ConvertidoEnPedido
                        && _presupuesto.Estado != EstadoPresupuesto.Aceptado;
                }
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
                        columna.Item().AlignRight().Text(FormatoDocumento.T("QUOTATION")).FontSize(17).Bold()
                            .FontColor(Colors.BlueGrey.Darken3);
                        columna.Item().PaddingTop(4).AlignRight().Text(_presupuesto.Numero).FontSize(12).Bold();
                        columna.Item().PaddingTop(3).AlignRight()
                            .Text(FormatoDocumento.T("Date") + ": " + _presupuesto.FechaEmision.ToString("dd/MM/yyyy"));
                        columna.Item().PaddingTop(1).AlignRight()
                            .Text(FormatoDocumento.T("Valid until") + ": " + _presupuesto.FechaVencimiento.ToString("dd/MM/yyyy")).Bold();
                    });
                });
            }

            protected override void ComponerCuerpo(IContainer contenedor)
            {
                contenedor.Column(columna =>
                {
                    columna.Spacing(11);

                    // Aviso de validez, bien visible: es lo primero que lee el cliente.
                    columna.Item().Border(1.2f).BorderColor(Vencido ? Colors.Red.Darken2 : Colors.Orange.Darken2)
                        .Background(Vencido ? Colors.Red.Lighten5 : Colors.Orange.Lighten5).Padding(9).Column(aviso =>
                        {
                            aviso.Item().Text(Vencido
                                ? FormatoDocumento.T("This quotation has expired. Ask for an updated one.")
                                : string.Format(FormatoDocumento.T("Quotation valid for {0} days (until {1})."), DiasDeValidez, _presupuesto.FechaVencimiento.ToString("dd/MM/yyyy")))
                                .FontSize(11).Bold().FontColor(Vencido ? Colors.Red.Darken3 : Colors.Orange.Darken4);
                            aviso.Item().PaddingTop(3).Text(FormatoDocumento.T("Prices may vary according to the date. The amounts are guaranteed only until the date of validity."))
                                .FontSize(8.5f);
                        });

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

                        foreach (DetallePresupuesto detalle in _presupuesto.Detalles)
                        {
                            tabla.Cell().Element(CeldaDeDatos).Text(detalle.Codigo);

                            tabla.Cell().Element(CeldaDeDatos).Column(texto =>
                            {
                                texto.Item().Text(detalle.Descripcion);

                                if (detalle.TipoItem == TipoItemPresupuesto.FabricacionEspecial)
                                {
                                    texto.Item().Text(FormatoDocumento.T("Made to measure")).FontSize(7).Bold().FontColor(Colors.Orange.Darken3);
                                }

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
                        FilaDeTotal(totales, FormatoDocumento.T("Subtotal"), Moneda(_presupuesto.CalcularSubtotal()), false);

                        if (_presupuesto.PorcentajeDescuentoGeneral > 0m)
                        {
                            FilaDeTotal(totales, FormatoDocumento.T("General discount"),
                                "-" + Moneda(_presupuesto.CalcularDescuentoGeneral()), false);
                        }

                        if (_presupuesto.PorcentajeRecargo > 0m)
                        {
                            FilaDeTotal(totales, FormatoDocumento.T("Surcharge"), Moneda(_presupuesto.CalcularRecargo()), false);
                        }

                        FilaDeTotal(totales, "IVA " + FormatoDocumento.Numero(_presupuesto.PorcentajeIVA) + "%",
                            Moneda(_presupuesto.CalcularIVA()), false);
                        FilaDeTotal(totales, FormatoDocumento.T("Total"), Moneda(_presupuesto.CalcularTotal()), true);

                        if (_presupuesto.RequiereAnticipo())
                        {
                            FilaDeTotal(totales,
                                string.Format(FormatoDocumento.T("Deposit required to start manufacturing ({0}%)"), FormatoDocumento.Numero(_presupuesto.PorcentajeAnticipo)),
                                Moneda(_presupuesto.CalcularAnticipo()), false);
                        }
                    });

                    columna.Item().Border(1).BorderColor(Colors.BlueGrey.Lighten3).Padding(10).Column(datos =>
                    {
                        DatoRotulado(datos, FormatoDocumento.T("Payment terms"), _presupuesto.CondicionPago);
                        DatoRotulado(datos, FormatoDocumento.T("Delivery term"), _presupuesto.PlazoEntrega);
                        DatoRotulado(datos, FormatoDocumento.T("Delivery included"),
                            FormatoDocumento.T(_presupuesto.EntregaIncluida ? "Yes" : "No"));
                        DatoRotulado(datos, FormatoDocumento.T("Notes"), _presupuesto.Observaciones);
                    });

                    columna.Item().Column(condiciones =>
                    {
                        condiciones.Item().PaddingBottom(2).Text(FormatoDocumento.T("Terms and conditions")).Bold().FontSize(8.5f);
                        Condicion(condiciones, FormatoDocumento.T("Prices may vary according to the date. The amounts are guaranteed only until the date of validity."));
                        Condicion(condiciones, FormatoDocumento.T("This quotation does not reserve stock. Availability is confirmed when the order is placed."));

                        if (_presupuesto.RequiereAnticipo())
                        {
                            Condicion(condiciones, FormatoDocumento.T("Made-to-measure items start manufacturing once the deposit is received, and they cannot be returned."));
                        }

                        if (EnDolares)
                        {
                            Condicion(condiciones, string.Format(FormatoDocumento.T("Amounts in US dollars. Reference exchange rate: {0}."),
                                _presupuesto.TipoCambio.ToString("N2", new CultureInfo("es-AR"))));
                        }

                        Condicion(condiciones, FormatoDocumento.T("Delivery times start when the order is confirmed and, for made-to-measure items, when the deposit is received."));
                    });

                    columna.Item().PaddingTop(6).Border(1).BorderColor(Colors.BlueGrey.Lighten3).Padding(10).Column(aceptacion =>
                    {
                        aceptacion.Item().Text(FormatoDocumento.T("To accept this quotation, sign below or confirm it to your salesperson within the validity period."))
                            .FontSize(8);
                        aceptacion.Item().PaddingTop(20).Row(fila =>
                        {
                            fila.RelativeItem().Column(c => { c.Item().LineHorizontal(0.6f); c.Item().Text(FormatoDocumento.T("Signature")).FontSize(7); });
                            fila.ConstantItem(14);
                            fila.RelativeItem().Column(c => { c.Item().LineHorizontal(0.6f); c.Item().Text(FormatoDocumento.T("Full name")).FontSize(7); });
                            fila.ConstantItem(14);
                            fila.RelativeItem().Column(c => { c.Item().LineHorizontal(0.6f); c.Item().Text(FormatoDocumento.T("ID number") + " / " + FormatoDocumento.T("Date")).FontSize(7); });
                        });
                    });
                });
            }

            private static void Condicion(ColumnDescriptor columna, string texto)
            {
                columna.Item().PaddingBottom(1).Text("- " + texto).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            }
        }
    }
}
