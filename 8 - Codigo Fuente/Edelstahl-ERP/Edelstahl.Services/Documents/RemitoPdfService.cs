using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Remito de entrega en tres copias (original, duplicado y triplicado),
    /// cada una impresa sobre una hoja de color pastel: verde, amarillo y rosa.
    /// </summary>
    public class RemitoPdfService : DocumentoPdfBase
    {
        private readonly DatosRemito _datos;

        public RemitoPdfService(DatosRemito datos)
        {
            _datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        protected override string NombreArchivo
        {
            get { return "Remito-" + _datos.Numero; }
        }

        protected override IEnumerable<VarianteDocumento> Variantes()
        {
            yield return new VarianteDocumento
            {
                Etiqueta = FormatoDocumento.T("ORIGINAL"),
                Fondo = Colors.Green.Lighten4
            };

            yield return new VarianteDocumento
            {
                Etiqueta = FormatoDocumento.T("DUPLICATE"),
                Fondo = Colors.Yellow.Lighten4
            };

            yield return new VarianteDocumento
            {
                Etiqueta = FormatoDocumento.T("TRIPLICATE"),
                Fondo = Colors.Pink.Lighten4
            };
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

                fila.ConstantItem(78).Border(1).BorderColor(Colors.Grey.Darken1).Column(letra =>
                {
                    letra.Item().AlignCenter().Text("R").FontSize(30).Bold();
                    letra.Item().AlignCenter().PaddingBottom(2).Text(FormatoDocumento.T("Not valid as an invoice")).FontSize(6);
                });

                fila.RelativeItem().PaddingLeft(14).Column(columna =>
                {
                    columna.Item().Row(titulo =>
                    {
                        titulo.RelativeItem().Text(FormatoDocumento.T("DELIVERY NOTE")).FontSize(16).Bold();

                        titulo.AutoItem().Border(1).BorderColor(Colors.Grey.Darken2).PaddingHorizontal(5).PaddingVertical(2)
                            .Text(variante.Etiqueta).FontSize(8).Bold();
                    });

                    columna.Item().PaddingTop(3).Text(FormatoDocumento.T("No.") + " " + _datos.Numero).FontSize(11).Bold();
                    DatoRotulado(columna, FormatoDocumento.T("Date"), FormatoDocumento.Fecha(_datos.Fecha));
                    DatoRotulado(columna, "CUIT", _datos.Emisor.Cuit);
                    DatoRotulado(columna, FormatoDocumento.T("Gross income"), _datos.Emisor.IngresosBrutos);
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

                columna.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(fila =>
                {
                    fila.RelativeItem().Column(datos =>
                    {
                        DatoRotulado(datos, FormatoDocumento.T("Customer"), _datos.Receptor.RazonSocial);
                        DatoRotulado(datos, string.IsNullOrEmpty(_datos.Receptor.EtiquetaDocumento) ? "CUIT" : _datos.Receptor.EtiquetaDocumento, _datos.Receptor.Cuit);
                        DatoRotulado(datos, FormatoDocumento.T("Delivery address"), _datos.DireccionEntrega);
                    });

                    fila.RelativeItem().PaddingLeft(10).Column(datos =>
                    {
                        DatoRotulado(datos, FormatoDocumento.T("Order"), _datos.Pedido);
                        DatoRotulado(datos, FormatoDocumento.T("Route sheet"), _datos.HojaRuta);
                        DatoRotulado(datos, FormatoDocumento.T("Driver"), _datos.Chofer);
                        DatoRotulado(datos, FormatoDocumento.T("Vehicle"), _datos.Vehiculo);
                    });
                });

                columna.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(80);
                        c.RelativeColumn();
                        c.ConstantColumn(80);
                        c.ConstantColumn(70);
                    });

                    tabla.Header(cabecera =>
                    {
                        cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Code"));
                        cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Description"));
                        cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Unit"));
                        cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Quantity"));
                    });

                    foreach (LineaRemito linea in _datos.Lineas)
                    {
                        tabla.Cell().Element(CeldaDeDatos).Text(linea.Codigo);
                        tabla.Cell().Element(CeldaDeDatos).Text(linea.Descripcion);
                        tabla.Cell().Element(CeldaDeDatos).Text(linea.Unidad);
                        tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(linea.Cantidad));
                    }
                });

                if (!string.IsNullOrWhiteSpace(_datos.Observaciones))
                {
                    columna.Item().Text(texto =>
                    {
                        texto.Span(FormatoDocumento.T("Notes") + ": ").Bold();
                        texto.Span(_datos.Observaciones);
                    });
                }

                columna.Item().PaddingTop(30).Row(firmas =>
                {
                    firmas.Spacing(20);
                    firmas.RelativeItem().Element(c => BloqueDeFirma(c, FormatoDocumento.T("Delivered by")));
                    firmas.RelativeItem().Element(c => BloqueDeFirma(c, FormatoDocumento.T("Received in good order")));
                });
            });
        }

        private static void BloqueDeFirma(IContainer contenedor, string titulo)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Height(55).BorderBottom(1).BorderColor(Colors.Grey.Darken2);
                columna.Item().PaddingTop(3).Text(titulo).Bold();
                columna.Item().Text(FormatoDocumento.T("Signature and clarification")).FontSize(7).FontColor(Colors.Grey.Darken1);
                columna.Item().PaddingTop(10).Text(FormatoDocumento.T("Full name") + ": ____________________");
                columna.Item().PaddingTop(6).Text(FormatoDocumento.T("ID / CUIT / CUIL") + ": ____________________");
                columna.Item().PaddingTop(6).Text(FormatoDocumento.T("Date") + ": ____ / ____ / ________");
            });
        }
    }
}
