namespace Edelstahl.WinApp.Ui
{
    partial class FrmPermisosUsuario
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
            this.pnlCentro = new System.Windows.Forms.Panel();
            this.lstPermisos = new System.Windows.Forms.CheckedListBox();
            this.flpBotones = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGuardar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnRestablecer = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnCerrar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.lblEncabezado = new System.Windows.Forms.Label();
            this.pnlCentro.SuspendLayout();
            this.flpBotones.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlCentro
            //
            this.pnlCentro.Controls.Add(this.lstPermisos);
            this.pnlCentro.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCentro.Location = new System.Drawing.Point(0, 78);
            this.pnlCentro.Name = "pnlCentro";
            this.pnlCentro.Padding = new System.Windows.Forms.Padding(12);
            this.pnlCentro.Size = new System.Drawing.Size(544, 475);
            this.pnlCentro.TabIndex = 0;
            //
            // lstPermisos
            //
            this.lstPermisos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstPermisos.CheckOnClick = true;
            this.lstPermisos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPermisos.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lstPermisos.FormattingEnabled = true;
            this.lstPermisos.IntegralHeight = false;
            this.lstPermisos.Location = new System.Drawing.Point(12, 12);
            this.lstPermisos.Name = "lstPermisos";
            this.lstPermisos.Size = new System.Drawing.Size(520, 451);
            this.lstPermisos.TabIndex = 0;
            //
            // flpBotones
            //
            this.flpBotones.Controls.Add(this.btnGuardar);
            this.flpBotones.Controls.Add(this.btnRestablecer);
            this.flpBotones.Controls.Add(this.btnCerrar);
            this.flpBotones.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpBotones.Location = new System.Drawing.Point(0, 553);
            this.flpBotones.Name = "flpBotones";
            this.flpBotones.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this.flpBotones.Size = new System.Drawing.Size(544, 48);
            this.flpBotones.TabIndex = 1;
            //
            // btnGuardar
            //
            this.btnGuardar.AutoSize = true;
            this.btnGuardar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGuardar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnGuardar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGuardar.FlatAppearance.BorderSize = 0;
            this.btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGuardar.ForeColor = System.Drawing.Color.White;
            this.btnGuardar.Location = new System.Drawing.Point(12, 8);
            this.btnGuardar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnGuardar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnGuardar.Size = new System.Drawing.Size(90, 30);
            this.btnGuardar.TabIndex = 0;
            this.btnGuardar.Tag = "Save";
            this.btnGuardar.Text = "Save";
            this.btnGuardar.UseVisualStyleBackColor = false;
            //
            // btnRestablecer
            //
            this.btnRestablecer.AutoSize = true;
            this.btnRestablecer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRestablecer.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnRestablecer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestablecer.FlatAppearance.BorderSize = 0;
            this.btnRestablecer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestablecer.ForeColor = System.Drawing.Color.White;
            this.btnRestablecer.Location = new System.Drawing.Point(108, 8);
            this.btnRestablecer.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnRestablecer.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnRestablecer.Name = "btnRestablecer";
            this.btnRestablecer.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnRestablecer.Size = new System.Drawing.Size(160, 30);
            this.btnRestablecer.TabIndex = 1;
            this.btnRestablecer.Tag = "Restore role permissions";
            this.btnRestablecer.Text = "Restore role permissions";
            this.btnRestablecer.UseVisualStyleBackColor = false;
            //
            // btnCerrar
            //
            this.btnCerrar.AutoSize = true;
            this.btnCerrar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnCerrar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Location = new System.Drawing.Point(274, 8);
            this.btnCerrar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnCerrar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCerrar.Size = new System.Drawing.Size(90, 30);
            this.btnCerrar.TabIndex = 2;
            this.btnCerrar.Tag = "Close";
            this.btnCerrar.Text = "Close";
            this.btnCerrar.UseVisualStyleBackColor = false;
            //
            // lblEncabezado
            //
            this.lblEncabezado.BackColor = System.Drawing.Color.FromArgb(226, 234, 247);
            this.lblEncabezado.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblEncabezado.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblEncabezado.Location = new System.Drawing.Point(0, 0);
            this.lblEncabezado.Name = "lblEncabezado";
            this.lblEncabezado.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this.lblEncabezado.Size = new System.Drawing.Size(544, 78);
            this.lblEncabezado.TabIndex = 2;
            this.lblEncabezado.Text = "Permissions of user";
            //
            // FrmPermisosUsuario
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(255, 252, 238);
            this.ClientSize = new System.Drawing.Size(544, 601);
            this.Controls.Add(this.pnlCentro);
            this.Controls.Add(this.flpBotones);
            this.Controls.Add(this.lblEncabezado);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.Name = "FrmPermisosUsuario";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Permissions";
            this.pnlCentro.ResumeLayout(false);
            this.flpBotones.ResumeLayout(false);
            this.flpBotones.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlCentro;
        private System.Windows.Forms.CheckedListBox lstPermisos;
        private System.Windows.Forms.FlowLayoutPanel flpBotones;
        private Edelstahl.WinApp.Ui.BotonEstilo btnGuardar;
        private Edelstahl.WinApp.Ui.BotonEstilo btnRestablecer;
        private Edelstahl.WinApp.Ui.BotonEstilo btnCerrar;
        private System.Windows.Forms.Label lblEncabezado;
    }
}
