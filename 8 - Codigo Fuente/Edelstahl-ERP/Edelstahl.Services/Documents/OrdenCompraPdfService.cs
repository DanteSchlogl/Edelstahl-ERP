using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Orden de compra dirigida a un proveedor.
    /// </summary>
    public class OrdenCompraPdfService : DocumentoPdfBase
    {
        private readonly DatosOrdenCompra _datos;

        public OrdenCompraPdfService(DatosOrdenCompra datos)
        {
            _datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        protected override string NombreArchivo
        {
            get { return "OrdenCompra-" + _datos.Numero; }
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
                    columna.Item().PaddingTop(2).Text("CUIT " + _datos.Emisor.Cuit);
                });

                fila.RelativeItem().Column(columna =>
                {
                    columna.Item().AlignRight().Text(FormatoDocumento.T("PURCHASE ORDER")).FontSize(16).Bold();
                    columna.Item().PaddingTop(3).AlignRight().Text(FormatoDocumento.T("No.") + " " + _datos.Numero).FontSize(11).Bold();
                    columna.Item().PaddingTop(2).AlignRight().Text(FormatoDocumento.T("Date") + ": " + FormatoDocumento.Fecha(_datos.Fecha));

                    if (_datos.Entrega.HasValue)
                    {
                        columna.Item().PaddingTop(2).AlignRight()
                            .Text(FormatoDocumento.T("Expected delivery") + ": " + FormatoDocumento.Fecha(_datos.Entrega.Value));
                    }
                });
            });
        }

        protected override void ComponerCuerpo(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(10);

                columna.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(datos =>
                {
                    DatoRotulado(datos, FormatoDocumento.T("Supplier"), _datos.Proveedor);
                    DatoRotulado(datos, "CUIT", _datos.ProveedorCuit);
                    DatoRotulado(datos, FormatoDocumento.T("Address"), _datos.ProveedorDomicilio);
                    DatoRotulado(datos, FormatoDocumento.T("Contact"), _datos.Contacto);
                    DatoRotulado(datos, FormatoDocumento.T("Payment terms"), _datos.CondicionPago);
                });

                columna.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(70);
                        c.RelativeColumn();
                        c.ConstantColumn(50);
                        c.ConstantColumn(80);
                        c.ConstantColumn(44);
                        c.ConstantColumn(84);
                    });

                    tabla.Header(cabecera =>
                    {
                        cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Code"));
                        cabecera.Cell().Element(CeldaDeEncabezado).Text(FormatoDocumento.T("Description"));
                        cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Qty"));
                        cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Unit cost"));
                        cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text("% " + FormatoDocumento.T("Disc."));
                        cabecera.Cell().Element(CeldaDeEncabezado).AlignRight().Text(FormatoDocumento.T("Subtotal"));
                    });

                    foreach (LineaDocumento linea in _datos.Lineas)
                    {
                        tabla.Cell().Element(CeldaDeDatos).Text(linea.Codigo);
                        tabla.Cell().Element(CeldaDeDatos).Text(linea.Descripcion);
                        tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(linea.Cantidad));
                        tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Dinero(linea.PrecioUnitario));
                        tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Numero(linea.DescuentoPorcentaje));
                        tabla.Cell().Element(CeldaDeDatos).AlignRight().Text(FormatoDocumento.Dinero(linea.Subtotal));
                    }
                });

                columna.Item().AlignRight().Width(260).Column(totales =>
                {
                    FilaDeTotal(totales, FormatoDocumento.T("Net amount"), FormatoDocumento.Dinero(_datos.Neto), false);
                    FilaDeTotal(totales, "IVA", FormatoDocumento.Dinero(_datos.Iva), false);
                    FilaDeTotal(totales, FormatoDocumento.T("Total"), FormatoDocumento.Dinero(_datos.Total), true);
                });

                if (!string.IsNullOrWhiteSpace(_datos.Observaciones))
                {
                    columna.Item().Text(texto =>
                    {
                        texto.Span(FormatoDocumento.T("Notes") + ": ").Bold();
                        texto.Span(_datos.Observaciones);
                    });
                }
            });
        }
    }
}
