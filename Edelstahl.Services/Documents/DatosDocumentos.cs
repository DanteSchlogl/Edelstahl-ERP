using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace Edelstahl.Services.Documents
{
    /// <summary>
    /// Datos de la empresa que emite los documentos. Son ficticios y se
    /// pueden cambiar en App.config (claves Emisor...).
    /// </summary>
    public class DatosEmisor
    {
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Domicilio { get; set; }
        public string CondicionIva { get; set; }
        public string IngresosBrutos { get; set; }
        public string InicioActividades { get; set; }

        public static DatosEmisor Obtener()
        {
            return new DatosEmisor
            {
                RazonSocial = Leer("EmisorRazonSocial", "Edelstahl S.R.L. (ejemplo)"),
                Cuit = Leer("EmisorCuit", "30-00000000-0"),
                Domicilio = Leer("EmisorDomicilio", "Parque Industrial, nave 3 - Pilar, Buenos Aires"),
                CondicionIva = Leer("EmisorCondicionIva", "IVA Responsable Inscripto"),
                IngresosBrutos = Leer("EmisorIngresosBrutos", "000-000000-0"),
                InicioActividades = Leer("EmisorInicioActividades", "01/03/2020")
            };
        }

        private static string Leer(string clave, string valorPorDefecto)
        {
            string valor = ConfigurationManager.AppSettings[clave];
            return string.IsNullOrWhiteSpace(valor) ? valorPorDefecto : valor;
        }
    }

    public class DatosReceptor
    {
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string EtiquetaDocumento { get; set; }
        public string Domicilio { get; set; }
        public string Localidad { get; set; }
        public string CondicionIva { get; set; }
        public string Email { get; set; }
    }

    public class LineaDocumento
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// Factura o nota de crédito. La letra A discrimina el IVA; en la B el IVA
    /// está incluido en los precios.
    /// </summary>
    public class DatosFactura
    {
        public string Letra { get; set; }
        public string CodigoComprobante { get; set; }
        public bool EsNotaCredito { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Vencimiento { get; set; }
        public DatosEmisor Emisor { get; set; }
        public DatosReceptor Receptor { get; set; }
        public string CondicionVenta { get; set; }
        public string Referencia { get; set; }
        public string Motivo { get; set; }
        public List<LineaDocumento> Lineas { get; set; }
        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal AlicuotaIva { get; set; }
        public decimal Total { get; set; }
        public decimal Anticipo { get; set; }
        public decimal Saldo { get; set; }
        public string Observaciones { get; set; }

        public DatosFactura()
        {
            Lineas = new List<LineaDocumento>();
            Emisor = DatosEmisor.Obtener();
        }
    }

    public class LineaRemito
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }
    }

    public class DatosRemito
    {
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public DatosEmisor Emisor { get; set; }
        public DatosReceptor Receptor { get; set; }
        public string DireccionEntrega { get; set; }
        public string Pedido { get; set; }
        public string Chofer { get; set; }
        public string Vehiculo { get; set; }
        public string HojaRuta { get; set; }
        public string Observaciones { get; set; }
        public List<LineaRemito> Lineas { get; set; }

        public DatosRemito()
        {
            Lineas = new List<LineaRemito>();
            Emisor = DatosEmisor.Obtener();
        }
    }

    public class DatosOrdenCompra
    {
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Entrega { get; set; }
        public DatosEmisor Emisor { get; set; }
        public string Proveedor { get; set; }
        public string ProveedorCuit { get; set; }
        public string ProveedorDomicilio { get; set; }
        public string Contacto { get; set; }
        public string CondicionPago { get; set; }
        public List<LineaDocumento> Lineas { get; set; }
        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public string Observaciones { get; set; }

        public DatosOrdenCompra()
        {
            Lineas = new List<LineaDocumento>();
            Emisor = DatosEmisor.Obtener();
        }
    }

    /// <summary>
    /// Formatos comunes a todos los documentos.
    /// </summary>
    internal static class FormatoDocumento
    {
        private static readonly CultureInfo Cultura = new CultureInfo("es-AR");

        public static string Dinero(decimal valor)
        {
            return "$ " + valor.ToString("N2", Cultura);
        }

        public static string Numero(decimal valor)
        {
            return valor.ToString("0.###", Cultura);
        }

        public static string Fecha(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yyyy", Cultura);
        }

        public static string Valor(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? "-" : texto.Trim();
        }

        public static string T(string clave)
        {
            return Localization.LanguageService.Translate(clave);
        }

        private static byte[] _logo;
        private static bool _logoBuscado;

        /// <summary>Logo de Edelstahl (Resources/Logo/logo_edelstahl.png) o null si no esta.</summary>
        public static byte[] Logo()
        {
            if (!_logoBuscado)
            {
                _logoBuscado = true;

                try
                {
                    string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Logo", "logo_edelstahl.png");

                    if (System.IO.File.Exists(ruta))
                    {
                        _logo = System.IO.File.ReadAllBytes(ruta);
                    }
                }
                catch (Exception)
                {
                    _logo = null;
                }
            }

            return _logo;
        }

        /// <summary>Texto de la politica de entrega y devoluciones que se imprime en facturas y remitos.</summary>
        public static string PoliticaDeEntrega()
        {
            return T("Check the goods when you receive them. Once the goods are delivered and the delivery note is signed, no returns are accepted. Only goods rejected or damaged at the time of delivery, noted on the delivery note, can be returned. Made-to-measure orders (custom manufacturing) cannot be returned.");
        }
    }
}
