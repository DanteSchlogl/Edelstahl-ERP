using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Una copia del documento: su etiqueta (Original, Duplicado...) y el
    /// color de fondo de la hoja.
    /// </summary>
    public sealed class VarianteDocumento
    {
        public string Etiqueta { get; set; }
        public Color Fondo { get; set; }
    }

    /// <summary>
    /// Patrón Template Method: esta clase fija el esqueleto de todos los
    /// documentos (página, encabezado, cuerpo, pie y guardado) y cada
    /// documento concreto completa solo el encabezado y el cuerpo.
    /// </summary>
    public abstract class DocumentoPdfBase
    {
        static DocumentoPdfBase()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        protected abstract string NombreArchivo { get; }

        protected abstract void ComponerEncabezado(IContainer contenedor, VarianteDocumento variante);

        protected abstract void ComponerCuerpo(IContainer contenedor);

        /// <summary>
        /// Por omisión el documento tiene una sola copia sobre fondo blanco.
        /// </summary>
        protected virtual IEnumerable<VarianteDocumento> Variantes()
        {
            yield return new VarianteDocumento { Etiqueta = string.Empty, Fondo = Colors.White };
        }

        public static string CarpetaDeSalida
        {
            get
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Edelstahl ERP",
                    "Comprobantes");

                Directory.CreateDirectory(carpeta);
                return carpeta;
            }
        }

        /// <summary>
        /// Genera el PDF y devuelve la ruta del archivo.
        /// </summary>
        public string Generar()
        {
            return Generar(Path.Combine(CarpetaDeSalida, LimpiarNombre(NombreArchivo) + ".pdf"));
        }

        public string Generar(string ruta)
        {
            string carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));

            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            List<VarianteDocumento> copias = new List<VarianteDocumento>(Variantes());

            CrearDocumento(copias).GeneratePdf(ruta);

            return ruta;
        }

        /// <summary>Exporta cada pagina como imagen PNG (sirve para revisar el diseno y para las pruebas).</summary>
        public List<string> GenerarImagenes(string prefijo)
        {
            List<VarianteDocumento> copias = new List<VarianteDocumento>(Variantes());
            List<string> archivos = new List<string>();
            int n = 0;

            foreach (byte[] imagen in CrearDocumento(copias).GenerateImages())
            {
                n++;
                string ruta = prefijo + "_" + n + ".png";
                File.WriteAllBytes(ruta, imagen);
                archivos.Add(ruta);
            }

            return archivos;
        }

        private Document CrearDocumento(List<VarianteDocumento> copias)
        {
            return Document.Create(documento =>
            {
                foreach (VarianteDocumento copia in copias)
                {
                    VarianteDocumento actual = copia;

                    documento.Page(pagina =>
                    {
                        pagina.Size(PageSizes.A4);
                        pagina.Margin(1.3f, Unit.Centimetre);
                        pagina.PageColor(actual.Fondo);
                        pagina.DefaultTextStyle(estilo => estilo.FontFamily("Arial").FontSize(9));

                        pagina.Header().Element(c => ComponerEncabezado(c, actual));
                        pagina.Content().PaddingVertical(10).Element(ComponerCuerpo);
                        pagina.Footer().Element(ComponerPie);
                    });
                }
            });
        }

        /// <summary>Texto opcional de politica que se imprime en el pie (facturas y remitos).</summary>
        protected virtual string Politica
        {
            get { return null; }
        }

        protected virtual void ComponerPie(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                string politica = Politica;

                if (!string.IsNullOrEmpty(politica))
                {
                    columna.Item().PaddingBottom(4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(texto =>
                    {
                        texto.Span(FormatoDocumento.T("Delivery and returns policy") + ": ").Bold().FontSize(7);
                        texto.Span(politica).FontSize(7);
                    });
                }

                columna.Item().AlignCenter().Text(FormatoDocumento.T("Demonstration document, without fiscal validity"))
                    .FontSize(7).FontColor(Colors.Grey.Darken1);

                columna.Item().AlignCenter().Text(texto =>
                {
                    texto.Span("Edelstahl ERP - ").FontSize(7).FontColor(Colors.Grey.Medium);
                    texto.Span(FormatoDocumento.T("Page") + " ").FontSize(7).FontColor(Colors.Grey.Medium);
                    texto.CurrentPageNumber().FontSize(7);
                    texto.Span(" / ").FontSize(7);
                    texto.TotalPages().FontSize(7);
                });
            });
        }

        protected static string LimpiarNombre(string nombre)
        {
            foreach (char invalido in Path.GetInvalidFileNameChars())
            {
                nombre = nombre.Replace(invalido, '_');
            }

            return nombre;
        }

        // ---------------------------------------------------------------
        // Piezas de diseño compartidas
        // ---------------------------------------------------------------

        protected static IContainer CeldaDeEncabezado(IContainer contenedor)
        {
            return contenedor
                .Background(Colors.BlueGrey.Darken3)
                .PaddingVertical(4)
                .PaddingHorizontal(5)
                .DefaultTextStyle(estilo => estilo.FontSize(8).Bold().FontColor(Colors.White));
        }

        protected static IContainer CeldaDeDatos(IContainer contenedor)
        {
            return contenedor
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(3)
                .PaddingHorizontal(5);
        }

        protected static void DatoRotulado(ColumnDescriptor columna, string rotulo, string valor)
        {
            columna.Item().PaddingTop(2).Text(texto =>
            {
                texto.Span(rotulo + ": ").Bold();
                texto.Span(FormatoDocumento.Valor(valor));
            });
        }

        protected static void FilaDeTotal(ColumnDescriptor columna, string rotulo, string valor, bool destacada)
        {
            columna.Item().PaddingTop(2).Row(fila =>
            {
                fila.RelativeItem().AlignRight().Text(rotulo + ":").Bold().FontSize(destacada ? 11 : 9);
                fila.ConstantItem(95).AlignRight().Text(valor).Bold().FontSize(destacada ? 11 : 9);
            });
        }
    }
}
