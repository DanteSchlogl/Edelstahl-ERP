using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Ventana modal con barra de progreso que ejecuta un trabajo en un hilo aparte.
    /// </summary>
    public sealed class FrmEspera : Form
    {
        private readonly Func<string> _trabajo;
        private Exception _error;

        private FrmEspera(string mensajeClave, Func<string> trabajo)
        {
            _trabajo = trabajo;

            Text = Tr.T("Edelstahl ERP");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ControlBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 90);
            Font = Estilo.Normal;

            Label mensaje = new Label
            {
                Text = Tr.T(mensajeClave),
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(388, 24)
            };

            ProgressBar barra = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(16, 46),
                Size = new Size(388, 20)
            };

            Controls.Add(mensaje);
            Controls.Add(barra);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            Task.Run(() =>
            {
                try
                {
                    return _trabajo();
                }
                catch (Exception ex)
                {
                    _error = ex;
                    return null;
                }
            }).ContinueWith(tarea =>
            {
                Resultado = tarea.Result;

                try
                {
                    BeginInvoke((MethodInvoker)Close);
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        public string Resultado { get; private set; }

        /// <summary>
        /// Ejecuta el trabajo mostrando la ventana de espera. Devuelve el texto
        /// que produjo el trabajo y vuelve a lanzar su excepcion si fallo.
        /// </summary>
        public static string Ejecutar(string mensajeClave, Func<string> trabajo)
        {
            using (FrmEspera espera = new FrmEspera(mensajeClave, trabajo))
            {
                espera.ShowDialog();

                if (espera._error != null)
                {
                    throw espera._error;
                }

                return espera.Resultado;
            }
        }
    }
}
