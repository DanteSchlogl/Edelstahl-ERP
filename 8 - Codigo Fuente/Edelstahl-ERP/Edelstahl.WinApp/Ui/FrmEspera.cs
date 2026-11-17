using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Ventana modal con barra de progreso que ejecuta un trabajo en un hilo aparte.
    /// La distribución de los controles está en FrmEspera.Designer.cs.
    /// </summary>
    public sealed partial class FrmEspera : Form
    {
        private Func<string> _trabajo;
        private Exception _error;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmEspera()
        {
            InitializeComponent();
        }

        private FrmEspera(string mensajeClave, Func<string> trabajo) : this()
        {
            _trabajo = trabajo;

            Text = Tr.T("Edelstahl ERP");
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);
            lblMensaje.Text = Tr.T(mensajeClave);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_trabajo == null)
            {
                return;
            }

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
