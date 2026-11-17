namespace Edelstahl.WinApp.Ui
{
    partial class FrmFaltantes
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
            this.lblAviso = new System.Windows.Forms.Label();
            this.flpPie = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMas = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnMenos = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnAceptar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnCancelar = new Edelstahl.WinApp.Ui.BotonEstilo();
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).BeginInit();
            this.flpPie.SuspendLayout();
            this.SuspendLayout();
            //
            // grilla
            //
            this.grilla.AllowUserToAddRows = false;
            this.grilla.AllowUserToDeleteRows = false;
            this.grilla.AllowUserToResizeRows = false;
            this.grilla.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.grilla.BackgroundColor = System.Drawing.Color.FromArgb(255, 250, 228);
            this.grilla.BorderStyle = System.Windows.Forms.BorderStyle.None;
            estiloEncabezado.BackColor = System.Drawing.Color.FromArgb(54, 69, 98);
            estiloEncabezado.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            estiloEncabezado.ForeColor = System.Drawing.Color.White;
            this.grilla.ColumnHeadersDefaultCellStyle = estiloEncabezado;
            this.grilla.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            estiloCeldas.BackColor = System.Drawing.Color.FromArgb(255, 238, 236);
            estiloCeldas.ForeColor = System.Drawing.Color.FromArgb(120, 20, 20);
            this.grilla.DefaultCellStyle = estiloCeldas;
            estiloAlternas.BackColor = System.Drawing.Color.FromArgb(255, 230, 227);
            this.grilla.AlternatingRowsDefaultCellStyle = estiloAlternas;
            this.grilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grilla.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grilla.EnableHeadersVisualStyles = false;
            this.grilla.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grilla.GridColor = System.Drawing.Color.FromArgb(230, 218, 170);
            this.grilla.Location = new System.Drawing.Point(0, 58);
            this.grilla.MultiSelect = false;
            this.grilla.Name = "grilla";
            this.grilla.ReadOnly = true;
            this.grilla.RowHeadersVisible = false;
            this.grilla.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grilla.Size = new System.Drawing.Size(904, 371);
            this.grilla.TabIndex = 0;
            //
            // lblAviso
            //
            this.lblAviso.BackColor = System.Drawing.Color.FromArgb(255, 226, 224);
            this.lblAviso.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAviso.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAviso.ForeColor = System.Drawing.Color.FromArgb(150, 30, 30);
            this.lblAviso.Location = new System.Drawing.Point(0, 0);
            this.lblAviso.Name = "lblAviso";
            this.lblAviso.Padding = new System.Windows.Forms.Padding(12, 10, 12, 0);
            this.lblAviso.Size = new System.Drawing.Size(904, 58);
            this.lblAviso.TabIndex = 1;
            this.lblAviso.Text = "Missing stock";
            //
            // flpPie
            //
            this.flpPie.Controls.Add(this.btnMas);
            this.flpPie.Controls.Add(this.btnMenos);
            this.flpPie.Controls.Add(this.btnAceptar);
            this.flpPie.Controls.Add(this.btnCancelar);
            this.flpPie.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpPie.Location = new System.Drawing.Point(0, 429);
            this.flpPie.Name = "flpPie";
            this.flpPie.Padding = new System.Windows.Forms.Padding(10, 10, 10, 4);
            this.flpPie.Size = new System.Drawing.Size(904, 52);
            this.flpPie.TabIndex = 2;
            //
            // btnMas
            //
            this.btnMas.AutoSize = true;
            this.btnMas.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnMas.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnMas.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMas.FlatAppearance.BorderSize = 0;
            this.btnMas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMas.ForeColor = System.Drawing.Color.White;
            this.btnMas.Location = new System.Drawing.Point(10, 10);
            this.btnMas.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnMas.MinimumSize = new System.Drawing.Size(44, 30);
            this.btnMas.Name = "btnMas";
            this.btnMas.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnMas.Size = new System.Drawing.Size(44, 30);
            this.btnMas.TabIndex = 0;
            this.btnMas.Tag = "+ 1";
            this.btnMas.Text = "+ 1";
            this.btnMas.UseVisualStyleBackColor = false;
            //
            // btnMenos
            //
            this.btnMenos.AutoSize = true;
            this.btnMenos.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnMenos.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnMenos.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMenos.FlatAppearance.BorderSize = 0;
            this.btnMenos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMenos.ForeColor = System.Drawing.Color.White;
            this.btnMenos.Location = new System.Drawing.Point(60, 10);
            this.btnMenos.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnMenos.MinimumSize = new System.Drawing.Size(44, 30);
            this.btnMenos.Name = "btnMenos";
            this.btnMenos.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnMenos.Size = new System.Drawing.Size(44, 30);
            this.btnMenos.TabIndex = 1;
            this.btnMenos.Tag = "- 1";
            this.btnMenos.Text = "- 1";
            this.btnMenos.UseVisualStyleBackColor = false;
            //
            // btnAceptar
            //
            this.btnAceptar.AutoSize = true;
            this.btnAceptar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnAceptar.BackColor = System.Drawing.Color.FromArgb(46, 125, 50);
            this.btnAceptar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAceptar.FlatAppearance.BorderSize = 0;
            this.btnAceptar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location = new System.Drawing.Point(110, 10);
            this.btnAceptar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnAceptar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnAceptar.Size = new System.Drawing.Size(150, 30);
            this.btnAceptar.TabIndex = 2;
            this.btnAceptar.Tag = "Add stock and confirm";
            this.btnAceptar.Text = "Add stock and confirm";
            this.btnAceptar.UseVisualStyleBackColor = false;
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
            this.btnCancelar.Location = new System.Drawing.Point(266, 10);
            this.btnCancelar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnCancelar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCancelar.Size = new System.Drawing.Size(90, 30);
            this.btnCancelar.TabIndex = 3;
            this.btnCancelar.Tag = "Cancel";
            this.btnCancelar.Text = "Cancel";
            this.btnCancelar.UseVisualStyleBackColor = false;
            //
            // FrmFaltantes
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(255, 252, 238);
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(904, 481);
            this.Controls.Add(this.grilla);
            this.Controls.Add(this.lblAviso);
            this.Controls.Add(this.flpPie);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.Name = "FrmFaltantes";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Missing stock";
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).EndInit();
            this.flpPie.ResumeLayout(false);
            this.flpPie.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Edelstahl.WinApp.Ui.GrillaMarca grilla;
        private System.Windows.Forms.Label lblAviso;
        private System.Windows.Forms.FlowLayoutPanel flpPie;
        private Edelstahl.WinApp.Ui.BotonEstilo btnMas;
        private Edelstahl.WinApp.Ui.BotonEstilo btnMenos;
        private Edelstahl.WinApp.Ui.BotonEstilo btnAceptar;
        private Edelstahl.WinApp.Ui.BotonEstilo btnCancelar;
    }
}
