using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.WinApp.Infrastructure;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Colores, fuentes y utilidades comunes de la interfaz.
    /// </summary>
    public static class Estilo
    {
        public static readonly Color Principal = Color.FromArgb(54, 69, 98);
        public static readonly Color Lateral = Color.FromArgb(103, 120, 166);
        public static readonly Color LateralActivo = Color.FromArgb(168, 188, 235);
        public static readonly Color Fondo = Color.FromArgb(255, 249, 222);
        public static readonly Color Superficie = Color.FromArgb(255, 252, 238);
        public static readonly Color Acento = Color.FromArgb(52, 101, 164);
        public static readonly Color Alerta = Color.FromArgb(176, 58, 46);

        public static readonly Font Normal = new Font("Segoe UI", 9f);
        public static readonly Font Negrita = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font Titulo = new Font("Segoe UI", 16f, FontStyle.Bold);

        public static Button Boton(string clave)
        {
            Button boton = new BotonEstilo
            {
                Text = Tr.T(clave),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Acento,
                ForeColor = Color.White,
                Font = Normal,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8, 0, 8, 0),
                Cursor = Cursors.Hand,
                Tag = clave
            };

            boton.FlatAppearance.BorderSize = 0;
            return boton;
        }

        public static DataGridView Grilla()
        {
            DataGridView grilla = new GrillaMarca
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.FromArgb(255, 250, 228),
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                Font = Normal
            };

            grilla.DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 242);
            grilla.ColumnHeadersDefaultCellStyle.BackColor = Principal;
            grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grilla.ColumnHeadersDefaultCellStyle.Font = Negrita;
            grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 245, 210);
            grilla.GridColor = Color.FromArgb(230, 218, 170);
            grilla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            return grilla;
        }

        public static Label Etiqueta(string clave, bool negrita)
        {
            return new Label
            {
                Text = Tr.T(clave),
                AutoSize = true,
                Font = negrita ? Negrita : Normal,
                ForeColor = Principal,
                Tag = clave
            };
        }

        public static void Informar(IWin32Window padre, string mensaje)
        {
            MessageBox.Show(padre, mensaje, Tr.T("Edelstahl ERP"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Avisar(IWin32Window padre, string mensaje)
        {
            MessageBox.Show(padre, mensaje, Tr.T("Edelstahl ERP"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Confirmar(IWin32Window padre, string mensaje)
        {
            return MessageBox.Show(padre, mensaje, Tr.T("Edelstahl ERP"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Ejecuta una accion de negocio mostrando los errores de forma amigable.
        /// Devuelve true si termino sin error.
        /// </summary>
        public static bool Ejecutar(IWin32Window padre, Action accion)
        {
            try
            {
                accion();
                return true;
            }
            catch (BusinessRuleException ex)
            {
                Avisar(padre, ex.Message);
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.ProcesarErrorControlado(
                    ex, "Interfaz", "Operacion", Tr.T("The operation could not be completed."));
            }

            return false;
        }

        public static void AbrirArchivo(IWin32Window padre, string ruta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
                {
                    Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
                }
            }
            catch (Exception)
            {
                Avisar(padre, Tr.T("The file was created at {0} but could not be opened.", ruta));
            }
        }

        public static void AbrirUrl(IWin32Window padre, string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception)
            {
                Avisar(padre, Tr.T("The web page could not be opened."));
            }
        }

        public static Guid? IdDe(object fila)
        {
            if (fila == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo propiedad = fila.GetType().GetProperty("Id");

            if (propiedad == null || propiedad.PropertyType != typeof(Guid))
            {
                return null;
            }

            return (Guid)propiedad.GetValue(fila, null);
        }

        public static T Valor<T>(object fila, string propiedad)
        {
            if (fila == null)
            {
                return default(T);
            }

            System.Reflection.PropertyInfo info = fila.GetType().GetProperty(propiedad);

            if (info == null)
            {
                return default(T);
            }

            object valor = info.GetValue(fila, null);
            return valor is T ? (T)valor : default(T);
        }
    }

    /// <summary>
    /// Elemento de lista desplegable con texto visible y valor asociado.
    /// </summary>
    public sealed class Opcion
    {
        public Opcion(string texto, object valor)
        {
            Texto = texto;
            Valor = valor;
        }

        public string Texto { get; private set; }

        public object Valor { get; private set; }

        public override string ToString()
        {
            return Texto;
        }

        public static List<Opcion> De(IEnumerable origen, Func<object, string> texto, Func<object, object> valor)
        {
            List<Opcion> opciones = new List<Opcion>();

            foreach (object elemento in origen)
            {
                opciones.Add(new Opcion(texto(elemento), valor(elemento)));
            }

            return opciones;
        }
    }
}
