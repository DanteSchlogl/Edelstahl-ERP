using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Marca visual de Edelstahl: emblema (hexagono de acero con la E), textura de acero cepillado y paneles decorativos.
    /// Todo se dibuja por codigo: no usa fotos ni archivos externos.
    /// </summary>
    public static class Marca
    {
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        private static PointF[] Hexagono(float cx, float cy, float radio)
        {
            PointF[] puntos = new PointF[6];

            for (int i = 0; i < 6; i++)
            {
                double angulo = Math.PI / 180 * (60 * i - 90);
                puntos[i] = new PointF(cx + (float)(radio * Math.Cos(angulo)), cy + (float)(radio * Math.Sin(angulo)));
            }

            return puntos;
        }

        /// <summary>Emblema cuadrado: hexagono de acero con una E grabada.</summary>
        public static Bitmap Emblema(int lado)
        {
            string clave = "emb" + lado;
            Bitmap guardado;

            if (Cache.TryGetValue(clave, out guardado))
            {
                return guardado;
            }

            Bitmap bmp = new Bitmap(lado, lado);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float c = lado / 2f, r = lado * 0.48f;

                using (LinearGradientBrush acero = new LinearGradientBrush(new PointF(0, 0), new PointF(lado, lado),
                    Color.FromArgb(246, 248, 251), Color.FromArgb(108, 120, 140)))
                using (Pen borde = new Pen(Color.FromArgb(52, 64, 86), Math.Max(1.5f, lado / 28f)))
                {
                    borde.LineJoin = LineJoin.Round;
                    g.FillPolygon(acero, Hexagono(c, c, r));
                    g.DrawPolygon(borde, Hexagono(c, c, r));
                }

                using (LinearGradientBrush hueco = new LinearGradientBrush(new PointF(0, lado), new PointF(lado, 0),
                    Color.FromArgb(230, 235, 242), Color.FromArgb(96, 108, 128)))
                using (Pen filo = new Pen(Color.FromArgb(150, 255, 255, 255), Math.Max(1f, lado / 40f)))
                {
                    g.FillPolygon(hueco, Hexagono(c, c, r * 0.74f));
                    g.DrawPolygon(filo, Hexagono(c, c, r * 0.74f));
                }

                using (Font fuente = new Font("Segoe UI", lado * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (StringFormat centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    RectangleF area = new RectangleF(0, lado * 0.02f, lado, lado);
                    g.DrawString("E", fuente, new SolidBrush(Color.FromArgb(120, 255, 255, 255)), new RectangleF(area.X + 1, area.Y + 1, area.Width, area.Height), centro);
                    g.DrawString("E", fuente, new SolidBrush(Color.FromArgb(38, 50, 74)), area, centro);
                }
            }

            Cache[clave] = bmp;
            return bmp;
        }

        /// <summary>Acero cepillado: degradado vertical con vetas horizontales finas.</summary>
        public static Bitmap Acero(int ancho, int alto, Color arriba, Color abajo, int semilla, float fuerza = 1f)
        {
            ancho = Math.Max(ancho, 1);
            alto = Math.Max(alto, 1);
            Bitmap bmp = new Bitmap(ancho, alto);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (LinearGradientBrush fondo = new LinearGradientBrush(new Rectangle(0, 0, ancho, alto + 1), arriba, abajo, 90f))
                {
                    g.FillRectangle(fondo, 0, 0, ancho, alto);
                }

                Random azar = new Random(semilla);

                for (int y = 0; y < alto; y++)
                {
                    int claro = azar.Next(0, 3) == 0 ? (int)(azar.Next(10, 34) * fuerza) : 0;
                    int oscuro = azar.Next(0, 3) == 0 ? (int)(azar.Next(8, 26) * fuerza) : 0;

                    if (claro > 0)
                    {
                        using (Pen p = new Pen(Color.FromArgb(claro, 255, 255, 255)))
                        {
                            int x0 = azar.Next(0, Math.Max(1, ancho / 3));
                            g.DrawLine(p, x0, y, ancho - azar.Next(0, Math.Max(1, ancho / 4)), y);
                        }
                    }

                    if (oscuro > 0)
                    {
                        using (Pen p = new Pen(Color.FromArgb(oscuro, 20, 30, 50)))
                        {
                            g.DrawLine(p, azar.Next(0, Math.Max(1, ancho / 4)), y, ancho, y);
                        }
                    }
                }
            }

            return bmp;
        }

        /// <summary>Dibuja el emblema con transparencia, por ejemplo como marca de agua.</summary>
        public static void MarcaDeAgua(Graphics g, Rectangle area, int lado, float opacidad)
        {
            Bitmap emblema = Emblema(Math.Max(64, lado));
            ColorMatrix matriz = new ColorMatrix { Matrix33 = opacidad };

            using (ImageAttributes atributos = new ImageAttributes())
            {
                atributos.SetColorMatrix(matriz, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Rectangle destino = new Rectangle(area.X + (area.Width - lado) / 2, area.Y + (area.Height - lado) / 2, lado, lado);
                g.DrawImage(emblema, destino, 0, 0, emblema.Width, emblema.Height, GraphicsUnit.Pixel, atributos);
            }
        }
    }

    /// <summary>
    /// Boton plano que, deshabilitado, se ve claro y con texto legible (el de Windows pinta el texto gris oscuro sobre el color de fondo).
    /// </summary>
    public class BotonEstilo : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled)
            {
                base.OnPaint(e);
                return;
            }

            e.Graphics.Clear(Color.FromArgb(226, 229, 237));

            using (Pen borde = new Pen(Color.FromArgb(190, 196, 211)))
            {
                e.Graphics.DrawRectangle(borde, 0, 0, Width - 1, Height - 1);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Color.FromArgb(100, 110, 132),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Grilla de datos que, si sobra espacio debajo de las filas, muestra el emblema como marca de agua.</summary>
    public class GrillaMarca : DataGridView
    {
        private Dictionary<string, int> _tonos;
        private string _idiomaDeTonos;

        public GrillaMarca()
        {
            DoubleBuffered = true;
            CellFormatting += PintarEstados;
        }

        private int Tono(object valor)
        {
            string texto = valor as string;

            if (string.IsNullOrEmpty(texto))
            {
                return 0;
            }

            string idioma = Edelstahl.BLL.Infrastructure.Mensajes.T("Pending");

            if (_tonos == null || idioma != _idiomaDeTonos)
            {
                _tonos = Edelstahl.BLL.Infrastructure.Etiquetas.TonosPorTexto();
                _idiomaDeTonos = idioma;
            }

            int tono;
            return _tonos.TryGetValue(texto, out tono) ? tono : 0;
        }

        private void PintarEstados(object remitente, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            int tono = Tono(e.Value);

            if (tono > 0)
            {
                Color fondo = tono == 1 ? Color.FromArgb(46, 125, 50) : tono == 2 ? Color.FromArgb(245, 166, 35) : Color.FromArgb(198, 40, 40);
                e.CellStyle.BackColor = fondo;
                e.CellStyle.SelectionBackColor = fondo;
                e.CellStyle.ForeColor = tono == 2 ? Color.FromArgb(60, 38, 0) : Color.White;
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.Font = Estilo.Negrita;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                return;
            }

            if (Rows[e.RowIndex].Selected)
            {
                return;
            }

            foreach (DataGridViewCell celda in Rows[e.RowIndex].Cells)
            {
                if (Tono(celda.Value) == 3)
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 226, 224);
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int ocupado = ColumnHeadersVisible ? ColumnHeadersHeight : 0;

            foreach (DataGridViewRow fila in Rows)
            {
                if (fila.Visible)
                {
                    ocupado += fila.Height;
                }
            }

            Rectangle libre = new Rectangle(0, ocupado, ClientSize.Width, ClientSize.Height - ocupado);
            int lado = Math.Min(Math.Min(libre.Width, libre.Height) - 40, 260);

            if (lado >= 90)
            {
                Marca.MarcaDeAgua(e.Graphics, libre, lado, 0.13f);
            }
        }
    }

    /// <summary>Panel de acero cepillado con el emblema; se usa en la parte libre de la barra lateral.</summary>
    public class PanelAcero : Panel
    {
        private Bitmap _textura;

        public PanelAcero()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_textura == null || _textura.Width != Width || _textura.Height != Height)
            {
                if (_textura != null)
                {
                    _textura.Dispose();
                }

                _textura = Marca.Acero(Width, Height, Color.FromArgb(103, 120, 166), Color.FromArgb(70, 84, 120), 7);
            }

            e.Graphics.DrawImage(_textura, 0, 0);
            int lado = Math.Min(Width - 60, Height - 60);

            if (lado >= 70)
            {
                Marca.MarcaDeAgua(e.Graphics, new Rectangle(0, 0, Width, Height), Math.Min(lado, 170), 0.5f);
            }

            using (Font fuente = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (StringFormat centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
            using (SolidBrush pincel = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            {
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                e.Graphics.DrawString("STAINLESS STEEL  |  EDELSTAHL", fuente, pincel, new RectangleF(0, 0, Width, Height - 12), centro);
            }
        }
    }

    /// <summary>Encabezado de la ventana: acero claro con una linea de acento.</summary>
    public class PanelEncabezado : Panel
    {
        private Bitmap _textura;

        public PanelEncabezado()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_textura == null || _textura.Width != Width || _textura.Height != Height)
            {
                if (_textura != null)
                {
                    _textura.Dispose();
                }

                _textura = Marca.Acero(Width, Height, Color.FromArgb(58, 108, 172), Color.FromArgb(38, 82, 146), 3, 0.3f);
            }

            e.Graphics.DrawImage(_textura, 0, 0);

            using (Pen acento = new Pen(Color.FromArgb(242, 201, 76), 3))
            {
                e.Graphics.DrawLine(acento, 0, Height - 2, Width, Height - 2);
            }

            e.Graphics.DrawImage(Marca.Emblema(48), 22, (Height - 48) / 2 - 1, 48, 48);
        }
    }
}
