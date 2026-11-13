using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Mapa interactivo de la hoja de ruta: Leaflet con los mapas de OpenStreetMap dentro de WebView2.
    /// No necesita clave de API. Si el equipo no tiene WebView2 o no hay internet, muestra el mapa esquematico propio (MapaRuta).
    /// </summary>
    public class MapaWeb : UserControl
    {
        private const string Servidor = "edelstahl.local";

        private readonly MapaRuta _respaldo = new MapaRuta { Dock = DockStyle.Fill };
        private readonly WebView2 _web = new WebView2 { Dock = DockStyle.Fill, Visible = false };
        private bool _listo;
        private bool _iniciando;
        private string _titulo = string.Empty;
        private List<ParadaMapa> _paradas = new List<ParadaMapa>();

        public MapaWeb()
        {
            Dock = DockStyle.Fill;
            Controls.Add(_web);
            Controls.Add(_respaldo);
            HandleCreated += async (s, e) => await IniciarAsync();
        }

        /// <summary>Indica si se esta usando el mapa web (si es falso, se ve el mapa esquematico).</summary>
        public bool UsaMapaWeb
        {
            get { return _listo; }
        }

        public void Cargar(string titulo, IEnumerable<ParadaMapa> paradas)
        {
            _titulo = titulo ?? string.Empty;
            _paradas = paradas == null ? new List<ParadaMapa>() : paradas.OrderBy(p => p.Orden).ToList();
            _respaldo.Cargar(_titulo, _paradas);
            Enviar();
        }

        private async System.Threading.Tasks.Task IniciarAsync()
        {
            if (_iniciando || _listo)
            {
                return;
            }

            _iniciando = true;

            try
            {
                string carpetaMapa = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mapa");

                if (!File.Exists(Path.Combine(carpetaMapa, "mapa.html")))
                {
                    return;
                }

                string datos = Path.Combine(Path.GetTempPath(), "EdelstahlWebView2");
                CoreWebView2Environment entorno = await CoreWebView2Environment.CreateAsync(null, datos);
                await _web.EnsureCoreWebView2Async(entorno);

                _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _web.CoreWebView2.SetVirtualHostNameToFolderMapping(Servidor, carpetaMapa, CoreWebView2HostResourceAccessKind.Allow);
                _web.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess || _listo)
                    {
                        return;
                    }

                    _listo = true;
                    _web.Visible = true;
                    _respaldo.Visible = false;
                    Enviar();
                };
                _web.CoreWebView2.Navigate("https://" + Servidor + "/mapa.html");
            }
            catch (Exception)
            {
                // Sin WebView2: queda el mapa esquematico.
            }
            finally
            {
                _iniciando = false;
            }
        }

        private void Enviar()
        {
            if (!_listo || _web.CoreWebView2 == null)
            {
                return;
            }

            _web.CoreWebView2.ExecuteScriptAsync("cargar(" + ArmarJson() + ")");
        }

        private string ArmarJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"titulo\":").Append(Texto(_titulo));
            sb.Append(",\"sede\":{\"nombre\":").Append(Texto(GeoFicticio.Sede.Nombre))
              .Append(",\"lat\":").Append(Numero(GeoFicticio.Sede.Latitud))
              .Append(",\"lon\":").Append(Numero(GeoFicticio.Sede.Longitud)).Append("}");
            sb.Append(",\"paradas\":[");

            bool primera = true;
            foreach (ParadaMapa p in _paradas.Where(x => x.Lugar != null))
            {
                if (!primera)
                {
                    sb.Append(",");
                }

                primera = false;
                sb.Append("{\"orden\":").Append(p.Orden.ToString(CultureInfo.InvariantCulture))
                  .Append(",\"titulo\":").Append(Texto(p.Titulo))
                  .Append(",\"detalle\":").Append(Texto(p.Detalle))
                  .Append(",\"estado\":").Append(Texto(p.Estado.ToString()))
                  .Append(",\"aprox\":").Append(p.Lugar.Estimado ? "true" : "false")
                  .Append(",\"lat\":").Append(Numero(p.Lugar.Latitud))
                  .Append(",\"lon\":").Append(Numero(p.Lugar.Longitud)).Append("}");
            }

            sb.Append("],\"textos\":{");
            sb.Append("\"distancia\":").Append(Texto(Tr.T("Distance")));
            sb.Append(",\"parte\":").Append(Texto(Tr.T("Departs from")));
            sb.Append(",\"aproximada\":").Append(Texto(Tr.T("approximate location")));
            sb.Append(",\"tiempo\":").Append(Texto(Tr.T("Time")));
            sb.Append(",\"pendiente\":").Append(Texto(Tr.T("Pending")));
            sb.Append(",\"entregada\":").Append(Texto(Tr.T("Delivered")));
            sb.Append(",\"noEntregada\":").Append(Texto(Tr.T("Not delivered")));
            sb.Append(",\"sinParadas\":").Append(Texto(Tr.T("This route sheet has no stops.")));
            sb.Append(",\"calculando\":").Append(Texto(Tr.T("Calculating the route...")));
            sb.Append(",\"rectas\":").Append(Texto(Tr.T("Straight lines: street routing is not available.")));
            sb.Append("}}");
            return sb.ToString();
        }

        private static string Numero(double valor)
        {
            return valor.ToString("0.000000", CultureInfo.InvariantCulture);
        }

        private static string Texto(string valor)
        {
            StringBuilder sb = new StringBuilder("\"");

            foreach (char c in valor ?? string.Empty)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append(' '); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append(' ');
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append("\"").ToString();
        }
    }
}
