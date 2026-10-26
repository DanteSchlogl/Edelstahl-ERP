using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Factura A, factura B y nota de crédito. En la A se discrimina el IVA;
    /// en la B los precios ya lo incluyen.
    /// </summary>
    public class FacturaPdfService : DocumentoPdfBase
    {
        private readonly DatosFactura _datos;

        public FacturaPdfService(DatosFactura datos)
        {
            _datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        private bool EsB
        {
            get { return string.Equals(_datos.Letra, "B", StringComparison.OrdinalIgnoreCase); }
        }

        protected override string NombreArchivo
        {
            get
            {
                return (_datos.EsNotaCredito ? "NotaCredito-" : "Factura-") + _datos.Letra + "-" + _datos.Numero;
            }
        }

        private string Titulo
        {
            get
            {
                return FormatoDocumento.T(_datos.EsNotaCredito ? "CREDIT NOTE" : "INVOICE");
            }
        }

        protected override void ComponerEncabezado(IContainer contenedor, VarianteDocumento variante)
        {
            contenedor.Border(1).BorderColor(Colors.Grey.Darken1).Padding(8).Row(fila =>
            {
                byte[] logo = FormatoDocumento.Logo();

                if (logo != null)
                {
                    fila.ConstantItem(58).PaddingRight(8).Image(logo).FitArea();
                }

                fila.RelativeItem().Column(columna =>
                {
                    columna.Item().Text(_datos.Emisor.RazonSocial).FontSize(13).Bold();
                    columna.Item().PaddingTop(3).Text(_datos.Emisor.Domicilio);
                    columna.Item().PaddingTop(2).Text(_datos.Emisor.CondicionIva).Bold();
                });

                fila.ConstantItem(70).Border(1).BorderColor(Colors.Grey.Darken1).Column(letra =>
                {
                    letra.Item().AlignCenter().Text(_datos.Letra).FontSize(30).Bold();
                    letra.Item().AlignCenter().PaddingBottom(2)
                        .Text(FormatoDocumento.T("Code") + " " + _datos.CodigoComprobante).FontSize(7);
                });

                fila.RelativeItem().PaddingLeft(14).Column(columna =>
                {
                    columna.Item().Text(Titulo).FontSize(16).Bold();
                    columna.Item().PaddingTop(3).Text(FormatoDocumento.T("No.") + " " + _datos.Numero).FontSize(11).Bold();
                    DatoRotulado(columna, FormatoDocumento.T("Date"), FormatoDocumento.Fecha(_datos.Fecha));
                    DatoRotulado(columna, "CUIT", _datos.Emisor.Cuit);
                    DatoRotulado(columna, FormatoDocumento.T("Gross income"), _datos.Emisor.IngresosBrutos);
                    DatoRotulado(columna, FormatoDocumento.T("Start of activities"), _datos.Emisor.InicioActividades);
                });
            });
        }

        protected override string Politica
        {
            get { return FormatoDocumento.PoliticaDeEntrega(); }
        }

        protected override void ComponerCuerpo(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(10);

                columna.Item().Element(ComponerReceptor);
                columna.Item().Element(ComponerTabla);
                columna.Item().Element(ComponerTotales);

                if (!string.IsNullOrWhiteSpace(_datos.Observaciones))
                {
                    columna.Item().Text(texto =>
                    {
                        texto.Span(FormatoDocumento.T("Notes") + ": ").Bold();
                        texto.Span(_datos.Observaciones);
                    });
                }

                columna.Item().PaddingTop(6).Text("CAE: " + FormatoDocumento.T("(demonstration, not issued by the tax authority)"))
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }

        private void ComponerReceptor(IContainer contenedor)
        {
            contenedor.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(fila =>
            {
                fila.RelativeItem().Column(columna =>
                {
                    DatoRotulado(columna, FormatoDocumento.T("Customer"), _datos.Receptor.RazonSocial);
                    DatoRotulado(columna, string.IsNullOrEmpty(_datos.Receptor.EtiquetaDocumento) ? "CUIT" : _datos.Receptor.EtiquetaDocumento, _datos.Receptor.Cuit);
                    DatoRotulado(columna, FormatoDocumento.T("Address"),
                        (_datos.Receptor.Domicilio + " " + _datos.Receptor.Localidad).Trim());
                });

                fila.RelativeItem().PaddingLeft(10).Column(columna =>
                {
                    DatoRotulado(columna, FormatoDocumento.T("VAT condition"), _datos.Receptor.CondicionIva);
                    DatoRotulado(columna, FormatoDocumento.T("Payment terms"), _datos.CondicionVenta);

                    if (_datos.Vencimiento.HasValue)
                    {
                        DatoRotulado(columna, FormatoDocumento.T("Due date"), FormatoDocumento.Fecha(_datos.Vencimiento.Value));
                    }

                    DatoRotulado(columna, FormatoDocumento.T("Reference"), _datos.Referencia);

                    if (_datos.EsNotaCredito)
                    {
                        DatoRotulado(columna, FormatoDocumento.T("Reason"), _datos.Motivo);
                    }
                });
            });
        }

        private void ComponerTabla(IContainer contenedor)
        {
            decimal factor = EsB ? 1m + _datos.AlicuotaIva / 100m : 1m;

            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);
                    c.RelativeColumn();
                    c.ConstantColumn(48);
                    c.ConstantColumn(80);
                    c.ConstantColumn(44);
                    c.ConstantColumn(84);
                });

                tabla.Header(cabecera =>
                {
                    cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Code"));
                    cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Description"));
                    cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Qty"));
                    cabecera.Cell().Element(CeldaDeEncabezado).AlignRight()
                        .Text(FormatoDocumento.T(EsB ? "Unit price (VAT incl.)" : "Unit price"));
                    cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text("% " + FormatoDocumento.T("Disc."));
                    cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Subtotal"));
                });

                foreach (LineaDocumento linea in _datos.Lineas)
                {
                    tabla.Cell().Element(CeldaDeDatos).Text(linea.Codigo);
                    tabla.Cell().Element(CeldaDeDatos).Text(linea.Descripcion);
                    tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(linea.Cantidad));
                    tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Dinero(linea.PrecioUnitario * factor));
                    tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(linea.DescuentoPorcentaje));
                    tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Dinero(linea.Subtotal * factor));
                }
            });
        }

        private void ComponerTotales(IContainer contenedor)
        {
            contenedor.AlignRight().Width(260).Column(columna =>
            {
                if (EsB)
                {
                    FilaDeTotal(columna, FormatoDocumento.T("Total"), FormatoDocumento.Dinero(_datos.Total), true);
                    columna.Item().PaddingTop(3).AlignRight()
                        .Text(FormatoDocumento.T("VAT contained") + ": " + FormatoDocumento.Dinero(_datos.Iva))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    FilaDeTotal(columna, FormatoDocumento.T("Net amount"), FormatoDocumento.Dinero(_datos.Neto), false);
                    FilaDeTotal(columna, "IVA " + FormatoDocumento.Numero(_datos.AlicuotaIva) + "%",
                        FormatoDocumento.Dinero(_datos.Iva), false);
                    FilaDeTotal(columna, FormatoDocumento.T("Total"), FormatoDocumento.Dinero(_datos.Total), true);
                }

                if (!_datos.EsNotaCredito && _datos.Anticipo > 0m)
                {
                    FilaDeTotal(columna, FormatoDocumento.T("Deposit applied"), FormatoDocumento.Dinero(_datos.Anticipo), false);
                    FilaDeTotal(columna, FormatoDocumento.T("Balance due"), FormatoDocumento.Dinero(_datos.Saldo), false);
                }
            });
        }
    }
}
