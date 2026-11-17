namespace Edelstahl.WinApp.Ui
{
    partial class FrmVerificarCarga
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle estiloEncabezado = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle estiloCeldas = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle estiloAlternas = new System.Windows.Forms.DataGridViewCellStyle();
            this.grilla = new Edelstahl.WinApp.Ui.GrillaMarca();
            this.lblExplicacion = new System.Windows.Forms.Label();
            this.flpPie = new System.Windows.Forms.FlowLayoutPanel();
            this.flpNotas = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNotas = new System.Windows.Forms.Label();
            this.txtObservaciones = new System.Windows.Forms.TextBox();
            this.flpAcciones = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGenerar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnCancelar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.lblResumen = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).BeginInit();
            this.flpPie.SuspendLayout();
            this.flpNotas.SuspendLayout();
            this.flpAcciones.SuspendLayout();
            this.SuspendLayout();
            //
            // grilla
            //
            this.grilla.AllowUserToAddRows = false;
            this.grilla.AllowUserToDeleteRows = false;
            this.grilla.AllowUserToResizeRows = false;
            estiloAlternas.BackColor = System.Drawing.Color.FromArgb(255, 245, 210);
            this.grilla.AlternatingRowsDefaultCellStyle = estiloAlternas;
            this.grilla.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.grilla.BackgroundColor = System.Drawing.Color.FromArgb(255, 250, 228);
            this.grilla.BorderStyle = System.Windows.Forms.BorderStyle.None;
            estiloEncabezado.BackColor = System.Drawing.Color.FromArgb(54, 69, 98);
            estiloEncabezado.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            estiloEncabezado.ForeColor = System.Drawing.Color.White;
            this.grilla.ColumnHeadersDefaultCellStyle = estiloEncabezado;
            this.grilla.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            estiloCeldas.BackColor = System.Drawing.Color.FromArgb(255, 253, 242);
            this.grilla.DefaultCellStyle = estiloCeldas;
            this.grilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grilla.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grilla.EnableHeadersVisualStyles = false;
            this.grilla.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grilla.GridColor = System.Drawing.Color.FromArgb(230, 218, 170);
            this.grilla.Location = new System.Drawing.Point(0, 62);
            this.grilla.MultiSelect = false;
            this.grilla.Name = "grilla";
            this.grilla.RowHeadersVisible = false;
            this.grilla.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grilla.Size = new System.Drawing.Size(884, 363);
            this.grilla.TabIndex = 0;
            //
            // lblExplicacion
            //
            this.lblExplicacion.BackColor = System.Drawing.Color.FromArgb(226, 234, 247);
            this.lblExplicacion.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblExplicacion.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblExplicacion.Location = new System.Drawing.Point(0, 0);
            this.lblExplicacion.Name = "lblExplicacion";
            this.lblExplicacion.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this.lblExplicacion.Size = new System.Drawing.Size(884, 62);
            this.lblExplicacion.TabIndex = 1;
            this.lblExplicacion.Text = "Customer";
            //
            // flpPie
            //
            this.flpPie.Controls.Add(this.flpNotas);
            this.flpPie.Controls.Add(this.flpAcciones);
            this.flpPie.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpPie.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPie.Location = new System.Drawing.Point(0, 425);
            this.flpPie.Name = "flpPie";
            this.flpPie.Padding = new System.Windows.Forms.Padding(10, 6, 10, 4);
            this.flpPie.Size = new System.Drawing.Size(884, 96);
            this.flpPie.TabIndex = 2;
            //
            // flpNotas
            //
            this.flpNotas.AutoSize = true;
            this.flpNotas.Controls.Add(this.lblNotas);
            this.flpNotas.Controls.Add(this.txtObservaciones);
            this.flpNotas.Location = new System.Drawing.Point(10, 6);
            this.flpNotas.Margin = new System.Windows.Forms.Padding(0);
            this.flpNotas.Name = "flpNotas";
            this.flpNotas.Size = new System.Drawing.Size(619, 29);
            this.flpNotas.TabIndex = 0;
            this.flpNotas.WrapContents = false;
            //
            // lblNotas
            //
            this.lblNotas.AutoSize = true;
            this.lblNotas.Location = new System.Drawing.Point(0, 6);
            this.lblNotas.Margin = new System.Windows.Forms.Padding(0, 6, 8, 0);
            this.lblNotas.Name = "lblNotas";
            this.lblNotas.Size = new System.Drawing.Size(35, 15);
            this.lblNotas.TabIndex = 0;
            this.lblNotas.Text = "Notes";
            //
            // txtObservaciones
            //
            this.txtObservaciones.Location = new System.Drawing.Point(43, 3);
            this.txtObservaciones.Name = "txtObservaciones";
            this.txtObservaciones.Size = new System.Drawing.Size(560, 23);
            this.txtObservaciones.TabIndex = 1;
            //
            // flpAcciones
            //
            this.flpAcciones.AutoSize = true;
            this.flpAcciones.Controls.Add(this.btnGenerar);
            this.flpAcciones.Controls.Add(this.btnCancelar);
            this.flpAcciones.Controls.Add(this.lblResumen);
            this.flpAcciones.Location = new System.Drawing.Point(10, 41);
            this.flpAcciones.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.flpAcciones.Name = "flpAcciones";
            this.flpAcciones.Size = new System.Drawing.Size(330, 30);
            this.flpAcciones.TabIndex = 1;
            this.flpAcciones.WrapContents = false;
            //
            // btnGenerar
            //
            this.btnGenerar.AutoSize = true;
            this.btnGenerar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGenerar.BackColor = System.Drawing.Color.FromArgb(46, 125, 50);
            this.btnGenerar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGenerar.FlatAppearance.BorderSize = 0;
            this.btnGenerar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerar.ForeColor = System.Drawing.Color.White;
            this.btnGenerar.Location = new System.Drawing.Point(0, 0);
            this.btnGenerar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnGenerar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnGenerar.Name = "btnGenerar";
            this.btnGenerar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnGenerar.Size = new System.Drawing.Size(140, 30);
            this.btnGenerar.TabIndex = 0;
            this.btnGenerar.Tag = "Create delivery note";
            this.btnGenerar.Text = "Create delivery note";
            this.btnGenerar.UseVisualStyleBackColor = false;
            //
            // btnCancelar
            //
            this.btnCancelar.AutoSize = true;
            this.btnCancelar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancelar.BackColor = System.Drawing.Color.Gray;
            this.btnCancelar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatAppearance.BorderSize = 0;
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.ForeColor = System.Drawing.Color.White;
            this.btnCancelar.Location = new System.Drawing.Point(146, 0);
            this.btnCancelar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnCancelar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCancelar.Size = new System.Drawing.Size(90, 30);
            this.btnCancelar.TabIndex = 1;
            this.btnCancelar.Tag = "Cancel";
            this.btnCancelar.Text = "Cancel";
            this.btnCancelar.UseVisualStyleBackColor = false;
            //
            // lblResumen
            //
            this.lblResumen.AutoSize = true;
            this.lblResumen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResumen.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblResumen.Location = new System.Drawing.Point(258, 8);
            this.lblResumen.Margin = new System.Windows.Forms.Padding(16, 8, 0, 0);
            this.lblResumen.Name = "lblResumen";
            this.lblResumen.Size = new System.Drawing.Size(72, 15);
            this.lblResumen.TabIndex = 2;
            this.lblResumen.Text = "Verified: 0 of 0";
            //
            // FrmVerificarCarga
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(255, 252, 238);
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(884, 521);
            this.Controls.Add(this.grilla);
            this.Controls.Add(this.lblExplicacion);
            this.Controls.Add(this.flpPie);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.Name = "FrmVerificarCarga";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Verify the load";
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).EndInit();
            this.flpPie.ResumeLayout(false);
            this.flpPie.PerformLayout();
            this.flpNotas.ResumeLayout(false);
            this.flpNotas.PerformLayout();
            this.flpAcciones.ResumeLayout(false);
            this.flpAcciones.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Edelstahl.WinApp.Ui.GrillaMarca grilla;
        private System.Windows.Forms.Label lblExplicacion;
        private System.Windows.Forms.FlowLayoutPanel flpPie;
        private System.Windows.Forms.FlowLayoutPanel flpNotas;
        private System.Windows.Forms.Label lblNotas;
        private System.Windows.Forms.TextBox txtObservaciones;
        private System.Windows.Forms.FlowLayoutPanel flpAcciones;
        private Edelstahl.WinApp.Ui.BotonEstilo btnGenerar;
        private Edelstahl.WinApp.Ui.BotonEstilo btnCancelar;
        private System.Windows.Forms.Label lblResumen;
    }
}
