using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Edelstahl.WinApp.Ui
{
    public enum EstadoParada
    {
        Pendiente,
        Entregada,
        NoEntregada
    }

    public sealed class ParadaMapa
    {
        public int Orden { get; set; }
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public EstadoParada Estado { get; set; }
        public Lugar Lugar { get; set; }
    }

    /// <summary>
    /// Mapa esquematico e interactivo del recorrido: sede en la Capital y paradas numeradas.
    /// Rueda del mouse para acercar, arrastrar para mover, doble clic para ajustar la vista.
    /// </summary>
    public class MapaRuta : Control
    {
        private readonly List<ParadaMapa> _paradas = new List<ParadaMapa>();
        private string _titulo = string.Empty;
        private double _centroLat = -34.62;
        private double _centroLon = -58.45;
        private double _escala = 600;
        private Point _arrastre;
        private bool _arrastrando;
        private ParadaMapa _bajoMouse;
        private bool _sedeBajoMouse;

        public MapaRuta()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(236, 232, 220);
            Font = Estilo.Normal;
            Cursor = Cursors.Hand;
        }

        public void Cargar(string titulo, IEnumerable<ParadaMapa> paradas)
        {
            _titulo = titulo ?? string.Empty;
            _paradas.Clear();

            if (paradas != null)
            {
                _paradas.AddRange(paradas.OrderBy(p => p.Orden));
            }

            Ajustar();
            Invalidate();
        }

        public double DistanciaKm
        {
            get
            {
                double total = 0;
                Lugar anterior = GeoFicticio.Sede;

                foreach (ParadaMapa p in _paradas)
                {
                    total += GeoFicticio.Kilometros(anterior.Latitud, anterior.Longitud, p.Lugar.Latitud, p.Lugar.Longitud);
                    anterior = p.Lugar;
                }

                if (_paradas.Count > 0)
                {
                    total += GeoFicticio.Kilometros(anterior.Latitud, anterior.Longitud, GeoFicticio.Sede.Latitud, GeoFicticio.Sede.Longitud);
                }

                // Las calles no son lineas rectas: se estima un 25% adicional.
                return total * 1.25;
            }
        }

        public void Ajustar()
        {
            List<Lugar> puntos = new List<Lugar> { GeoFicticio.Sede };
            puntos.AddRange(_paradas.Select(p => p.Lugar));

            double minLat = puntos.Min(p => p.Latitud), maxLat = puntos.Max(p => p.Latitud);
            double minLon = puntos.Min(p => p.Longitud), maxLon = puntos.Max(p => p.Longitud);

            _centroLat = (minLat + maxLat) / 2;
            _centroLon = (minLon + maxLon) / 2;

            double alto = Math.Max(maxLat - minLat, 0.12);
            double ancho = Math.Max((maxLon - minLon) * Math.Cos(_centroLat * Math.PI / 180), 0.12);
            double porAlto = Math.Max(Height, 100) * 0.72 / alto;
            double porAncho = Math.Max(Width, 100) * 0.72 / ancho;
            _escala = Math.Min(porAlto, porAncho);
        }

        private PointF Proyectar(double lat, double lon)
        {
            double cos = Math.Cos(_centroLat * Math.PI / 180);
            return new PointF(
                (float)(Width / 2.0 + (lon - _centroLon) * cos * _escala),
                (float)(Height / 2.0 - (lat - _centroLat) * _escala));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            DibujarAgua(g);
            DibujarCuadricula(g);
            DibujarReferencias(g);
            DibujarRecorrido(g);
            DibujarMarcas(g);
            DibujarCabecera(g);
            DibujarSugerencia(g);
        }

        private void DibujarAgua(Graphics g)
        {
            List<PointF> poligono = new List<PointF>();

            foreach (double[] punto in GeoFicticio.Costa)
            {
                poligono.Add(Proyectar(punto[0], punto[1]));
            }

            poligono.Add(Proyectar(-34.90, -57.55));
            poligono.Add(Proyectar(-33.20, -57.55));
            poligono.Add(Proyectar(-33.20, -58.62));

            using (SolidBrush agua = new SolidBrush(Color.FromArgb(190, 214, 235)))
            using (Pen costa = new Pen(Color.FromArgb(120, 160, 200), 2f))
            {
                g.FillPolygon(agua, poligono.ToArray());
                PointF[] linea = GeoFicticio.Costa.Select(c => Proyectar(c[0], c[1])).ToArray();
                g.DrawLines(costa, linea);
            }

            PointF etiqueta = Proyectar(-34.50, -58.12);

            using (Font cursiva = new Font("Segoe UI", 9f, FontStyle.Italic))
            using (SolidBrush texto = new SolidBrush(Color.FromArgb(110, 150, 190)))
            {
                g.DrawString("Río de la Plata", cursiva, texto, etiqueta);
            }
        }

        private void DibujarCuadricula(Graphics g)
        {
            double[] pasos = { 0.02, 0.05, 0.1, 0.25, 0.5, 1, 2, 5, 10 };
            double paso = pasos.First(p => p * _escala >= 70 || p == pasos[pasos.Length - 1]);

            double cos = Math.Cos(_centroLat * Math.PI / 180);
            double latMin = _centroLat - Height / 2.0 / _escala, latMax = _centroLat + Height / 2.0 / _escala;
            double lonMin = _centroLon - Width / 2.0 / (_escala * cos), lonMax = _centroLon + Width / 2.0 / (_escala * cos);

            using (Pen pluma = new Pen(Color.FromArgb(60, 120, 120, 120), 1f))
            {
                for (double lat = Math.Floor(latMin / paso) * paso; lat <= latMax; lat += paso)
                {
                    PointF a = Proyectar(lat, lonMin);
                    g.DrawLine(pluma, 0, a.Y, Width, a.Y);
                }

                for (double lon = Math.Floor(lonMin / paso) * paso; lon <= lonMax; lon += paso)
                {
                    PointF a = Proyectar(latMin, lon);
                    g.DrawLine(pluma, a.X, 0, a.X, Height);
                }
            }
        }

        private void DibujarReferencias(Graphics g)
        {
            List<RectangleF> ocupados = new List<RectangleF>();

            using (Font fuente = new Font("Segoe UI", 8f))
            using (SolidBrush texto = new SolidBrush(Color.FromArgb(120, 100, 100, 100)))
            using (SolidBrush punto = new SolidBrush(Color.FromArgb(140, 130, 130, 130)))
            {
                foreach (Lugar lugar in GeoFicticio.Referencias.OrderByDescending(l => l.Principal))
                {
                    PointF p = Proyectar(lugar.Latitud, lugar.Longitud);

                    if (p.X < -20 || p.Y < -20 || p.X > Width + 20 || p.Y > Height + 20)
                    {
                        continue;
                    }

                    SizeF medida = g.MeasureString(lugar.Nombre, fuente);
                    RectangleF caja = new RectangleF(p.X + 4, p.Y - 7, medida.Width, medida.Height);

                    if (!lugar.Principal && _escala < 250)
                    {
                        continue;
                    }

                    if (ocupados.Any(o => o.IntersectsWith(caja)))
                    {
                        continue;
                    }

                    ocupados.Add(caja);
                    g.FillEllipse(punto, p.X - 2.5f, p.Y - 2.5f, 5, 5);
                    g.DrawString(lugar.Nombre, fuente, texto, caja.Location);
                }
            }
        }

        private void DibujarRecorrido(Graphics g)
        {
            if (_paradas.Count == 0)
            {
                return;
            }

            List<PointF> puntos = new List<PointF> { Proyectar(GeoFicticio.Sede.Latitud, GeoFicticio.Sede.Longitud) };
            puntos.AddRange(_paradas.Select(p => Proyectar(p.Lugar.Latitud, p.Lugar.Longitud)));

            using (Pen ida = new Pen(Color.FromArgb(220, 52, 101, 164), 3.5f))
            using (Pen vuelta = new Pen(Color.FromArgb(170, 52, 101, 164), 2f) { DashStyle = DashStyle.Dash })
            {
                ida.LineJoin = LineJoin.Round;
                ida.CustomEndCap = new AdjustableArrowCap(4, 5);

                for (int i = 0; i < puntos.Count - 1; i++)
                {
                    g.DrawLine(ida, puntos[i], puntos[i + 1]);
                }

                g.DrawLine(vuelta, puntos[puntos.Count - 1], puntos[0]);
            }
        }

        private void DibujarMarcas(Graphics g)
        {
            PointF s = Proyectar(GeoFicticio.Sede.Latitud, GeoFicticio.Sede.Longitud);

            using (SolidBrush relleno = new SolidBrush(Color.FromArgb(150, 40, 40)))
            using (Pen borde = new Pen(Color.White, 2f))
            using (Font negrita = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.FillRectangle(relleno, s.X - 9, s.Y - 9, 18, 18);
                g.DrawRectangle(borde, s.X - 9, s.Y - 9, 18, 18);
                g.DrawString("S", negrita, Brushes.White, s.X - 6, s.Y - 8);

                foreach (ParadaMapa parada in _paradas)
                {
                    PointF p = Proyectar(parada.Lugar.Latitud, parada.Lugar.Longitud);
                    Color color = parada.Estado == EstadoParada.Entregada ? Color.FromArgb(46, 140, 80)
                        : parada.Estado == EstadoParada.NoEntregada ? Color.FromArgb(176, 58, 46)
                        : Color.FromArgb(230, 140, 30);

                    using (SolidBrush b = new SolidBrush(color))
                    {
                        g.FillEllipse(b, p.X - 11, p.Y - 11, 22, 22);
                    }

                    g.DrawEllipse(borde, p.X - 11, p.Y - 11, 22, 22);
                    string numero = parada.Orden.ToString();
                    SizeF medida = g.MeasureString(numero, negrita);
                    g.DrawString(numero, negrita, Brushes.White, p.X - medida.Width / 2 + 1, p.Y - medida.Height / 2 + 1);
                }
            }

            string sugerencia = null;
            PointF donde = PointF.Empty;

            if (_bajoMouse != null)
            {
                sugerencia = _bajoMouse.Titulo + Environment.NewLine + _bajoMouse.Detalle
                    + (_bajoMouse.Lugar.Estimado ? Environment.NewLine + Tr.T("Estimated location") : string.Empty);
                donde = Proyectar(_bajoMouse.Lugar.Latitud, _bajoMouse.Lugar.Longitud);
            }
            else if (_sedeBajoMouse)
            {
                sugerencia = Tr.T("Edelstahl - Head office (Capital)");
                donde = s;
            }

            if (sugerencia != null)
            {
                SizeF caja = g.MeasureString(sugerencia, Font);
                float x = Math.Min(donde.X + 14, Width - caja.Width - 10);
                float y = Math.Max(donde.Y - caja.Height - 8, 4);

                using (SolidBrush fondo = new SolidBrush(Color.FromArgb(235, 255, 255, 235)))
                using (Pen lineaCaja = new Pen(Color.FromArgb(120, 120, 120)))
                {
                    g.FillRectangle(fondo, x - 4, y - 2, caja.Width + 8, caja.Height + 4);
                    g.DrawRectangle(lineaCaja, x - 4, y - 2, caja.Width + 8, caja.Height + 4);
                    g.DrawString(sugerencia, Font, Brushes.Black, x, y);
                }
            }
        }

        private void DibujarCabecera(Graphics g)
        {
            double km = DistanciaKm;
            double horas = km / 32.0 + _paradas.Count * 0.25;
            string linea1 = string.IsNullOrEmpty(_titulo) ? Tr.T("Select a route sheet to see its route.") : _titulo;
            string linea2 = _paradas.Count == 0
                ? Tr.T("Schematic map (fictitious)")
                : Tr.T("{0} stop(s) - estimated distance {1} km - estimated time {2}", _paradas.Count, km.ToString("N1"), FormatoHoras(horas));

            using (Font negrita = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (SolidBrush fondo = new SolidBrush(Color.FromArgb(215, 248, 249, 252)))
            {
                SizeF a = g_Medir(g, linea1, negrita), b = g_Medir(g, linea2, Font);
                float ancho = Math.Max(a.Width, b.Width) + 16;
                g.FillRectangle(fondo, 8, 8, ancho, a.Height + b.Height + 10);
                g.DrawString(linea1, negrita, Brushes.Black, 14, 12);
                g.DrawString(linea2, Font, Brushes.DimGray, 14, 14 + a.Height);
            }
        }

        private static SizeF g_Medir(Graphics g, string texto, Font fuente)
        {
            return g.MeasureString(texto, fuente);
        }

        private static string FormatoHoras(double horas)
        {
            int minutos = (int)Math.Round(horas * 60);
            return (minutos / 60) + " h " + (minutos % 60).ToString("00") + " min";
        }

        private void DibujarSugerencia(Graphics g)
        {
            string ayuda = Tr.T("Mouse wheel: zoom - drag: move - double click: fit");

            using (Font chica = new Font("Segoe UI", 8f))
            {
                SizeF m = g.MeasureString(ayuda, chica);
                g.DrawString(ayuda, chica, Brushes.DimGray, Width - m.Width - 8, Height - m.Height - 6);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            double cos = Math.Cos(_centroLat * Math.PI / 180);
            double lonAntes = _centroLon + (e.X - Width / 2.0) / (_escala * cos);
            double latAntes = _centroLat - (e.Y - Height / 2.0) / _escala;

            _escala = Math.Max(15, Math.Min(60000, _escala * (e.Delta > 0 ? 1.25 : 0.8)));

            _centroLon = lonAntes - (e.X - Width / 2.0) / (_escala * cos);
            _centroLat = latAntes + (e.Y - Height / 2.0) / _escala;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                _arrastrando = true;
                _arrastre = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _arrastrando = false;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_arrastrando)
            {
                double cos = Math.Cos(_centroLat * Math.PI / 180);
                _centroLon -= (e.X - _arrastre.X) / (_escala * cos);
                _centroLat += (e.Y - _arrastre.Y) / _escala;
                _arrastre = e.Location;
                Invalidate();
                return;
            }

            ParadaMapa nuevo = _paradas.FirstOrDefault(p =>
            {
                PointF q = Proyectar(p.Lugar.Latitud, p.Lugar.Longitud);
                return Math.Abs(q.X - e.X) <= 13 && Math.Abs(q.Y - e.Y) <= 13;
            });

            PointF s = Proyectar(GeoFicticio.Sede.Latitud, GeoFicticio.Sede.Longitud);
            bool sede = nuevo == null && Math.Abs(s.X - e.X) <= 11 && Math.Abs(s.Y - e.Y) <= 11;

            if (nuevo != _bajoMouse || sede != _sedeBajoMouse)
            {
                _bajoMouse = nuevo;
                _sedeBajoMouse = sede;
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Ajustar();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }
}
