namespace Edelstahl.WinApp.Forms.Startup
{
    partial class FrmSeleccionModo
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
            this.pnlEncabezado = new System.Windows.Forms.Panel();
            this.lblMarca = new System.Windows.Forms.Label();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblDescripcion = new System.Windows.Forms.Label();
            this.pnlSql = new System.Windows.Forms.Panel();
            this.btnSqlServer = new System.Windows.Forms.Button();
            this.lblSqlDescripcion = new System.Windows.Forms.Label();
            this.pnlDemo = new System.Windows.Forms.Panel();
            this.btnDemo = new System.Windows.Forms.Button();
            this.lblDemoDescripcion = new System.Windows.Forms.Label();
            this.btnSalir = new System.Windows.Forms.Button();
            this.pnlEncabezado.SuspendLayout();
            this.pnlSql.SuspendLayout();
            this.pnlDemo.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlEncabezado
            //
            this.pnlEncabezado.BackColor = System.Drawing.Color.FromArgb(103, 120, 166);
            this.pnlEncabezado.Controls.Add(this.lblMarca);
            this.pnlEncabezado.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlEncabezado.Location = new System.Drawing.Point(0, 0);
            this.pnlEncabezado.Name = "pnlEncabezado";
            this.pnlEncabezado.Size = new System.Drawing.Size(760, 105);
            this.pnlEncabezado.TabIndex = 0;
            //
            // lblMarca
            //
            this.lblMarca.AutoSize = true;
            this.lblMarca.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblMarca.ForeColor = System.Drawing.Color.White;
            this.lblMarca.Location = new System.Drawing.Point(30, 28);
            this.lblMarca.Name = "lblMarca";
            this.lblMarca.Size = new System.Drawing.Size(220, 41);
            this.lblMarca.TabIndex = 0;
            this.lblMarca.Text = "Edelstahl ERP";
            //
            // lblTitulo
            //
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblTitulo.Location = new System.Drawing.Point(35, 135);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(188, 32);
            this.lblTitulo.TabIndex = 1;
            this.lblTitulo.Text = "Select the mode";
            //
            // lblDescripcion
            //
            this.lblDescripcion.AutoSize = true;
            this.lblDescripcion.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDescripcion.ForeColor = System.Drawing.Color.DimGray;
            this.lblDescripcion.Location = new System.Drawing.Point(38, 178);
            this.lblDescripcion.Name = "lblDescripcion";
            this.lblDescripcion.Size = new System.Drawing.Size(200, 19);
            this.lblDescripcion.TabIndex = 2;
            this.lblDescripcion.Text = "Choose how to start";
            //
            // pnlSql
            //
            this.pnlSql.BackColor = System.Drawing.Color.FromArgb(221, 235, 247);
            this.pnlSql.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSql.Controls.Add(this.btnSqlServer);
            this.pnlSql.Controls.Add(this.lblSqlDescripcion);
            this.pnlSql.Location = new System.Drawing.Point(35, 220);
            this.pnlSql.Name = "pnlSql";
            this.pnlSql.Size = new System.Drawing.Size(330, 225);
            this.pnlSql.TabIndex = 3;
            //
            // btnSqlServer
            //
            this.btnSqlServer.BackColor = System.Drawing.Color.FromArgb(92, 126, 215);
            this.btnSqlServer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSqlServer.FlatAppearance.BorderSize = 0;
            this.btnSqlServer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSqlServer.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSqlServer.ForeColor = System.Drawing.Color.White;
            this.btnSqlServer.Location = new System.Drawing.Point(20, 20);
            this.btnSqlServer.Name = "btnSqlServer";
            this.btnSqlServer.Size = new System.Drawing.Size(288, 50);
            this.btnSqlServer.TabIndex = 0;
            this.btnSqlServer.Text = "START WITH SQL SERVER";
            this.btnSqlServer.UseVisualStyleBackColor = false;
            //
            // lblSqlDescripcion
            //
            this.lblSqlDescripcion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSqlDescripcion.ForeColor = System.Drawing.Color.FromArgb(55, 65, 80);
            this.lblSqlDescripcion.Location = new System.Drawing.Point(20, 82);
            this.lblSqlDescripcion.Name = "lblSqlDescripcion";
            this.lblSqlDescripcion.Size = new System.Drawing.Size(285, 115);
            this.lblSqlDescripcion.TabIndex = 1;
            this.lblSqlDescripcion.Text = "Uses the SQL Server database.";
            //
            // pnlDemo
            //
            this.pnlDemo.BackColor = System.Drawing.Color.FromArgb(233, 243, 229);
            this.pnlDemo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDemo.Controls.Add(this.btnDemo);
            this.pnlDemo.Controls.Add(this.lblDemoDescripcion);
            this.pnlDemo.Location = new System.Drawing.Point(395, 220);
            this.pnlDemo.Name = "pnlDemo";
            this.pnlDemo.Size = new System.Drawing.Size(330, 225);
            this.pnlDemo.TabIndex = 4;
            //
            // btnDemo
            //
            this.btnDemo.BackColor = System.Drawing.Color.FromArgb(111, 172, 135);
            this.btnDemo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDemo.FlatAppearance.BorderSize = 0;
            this.btnDemo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDemo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDemo.ForeColor = System.Drawing.Color.White;
            this.btnDemo.Location = new System.Drawing.Point(20, 20);
            this.btnDemo.Name = "btnDemo";
            this.btnDemo.Size = new System.Drawing.Size(288, 50);
            this.btnDemo.TabIndex = 0;
            this.btnDemo.Text = "START DEMONSTRATION MODE";
            this.btnDemo.UseVisualStyleBackColor = false;
            //
            // lblDemoDescripcion
            //
            this.lblDemoDescripcion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDemoDescripcion.ForeColor = System.Drawing.Color.FromArgb(55, 65, 80);
            this.lblDemoDescripcion.Location = new System.Drawing.Point(20, 82);
            this.lblDemoDescripcion.Name = "lblDemoDescripcion";
            this.lblDemoDescripcion.Size = new System.Drawing.Size(285, 115);
            this.lblDemoDescripcion.TabIndex = 1;
            this.lblDemoDescripcion.Text = "Uses sample data in memory.";
            //
            // btnSalir
            //
            this.btnSalir.BackColor = System.Drawing.Color.FromArgb(210, 215, 225);
            this.btnSalir.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSalir.FlatAppearance.BorderSize = 0;
            this.btnSalir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSalir.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.btnSalir.Location = new System.Drawing.Point(290, 485);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Size = new System.Drawing.Size(180, 42);
            this.btnSalir.TabIndex = 5;
            this.btnSalir.Text = "Exit";
            this.btnSalir.UseVisualStyleBackColor = false;
            //
            // FrmSeleccionModo
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(239, 243, 248);
            this.CancelButton = this.btnSalir;
            this.ClientSize = new System.Drawing.Size(760, 560);
            this.Controls.Add(this.pnlEncabezado);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblDescripcion);
            this.Controls.Add(this.pnlSql);
            this.Controls.Add(this.pnlDemo);
            this.Controls.Add(this.btnSalir);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSeleccionModo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edelstahl ERP";
            this.pnlEncabezado.ResumeLayout(false);
            this.pnlEncabezado.PerformLayout();
            this.pnlSql.ResumeLayout(false);
            this.pnlDemo.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlEncabezado;
        private System.Windows.Forms.Label lblMarca;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblDescripcion;
        private System.Windows.Forms.Panel pnlSql;
        private System.Windows.Forms.Button btnSqlServer;
        private System.Windows.Forms.Label lblSqlDescripcion;
        private System.Windows.Forms.Panel pnlDemo;
        private System.Windows.Forms.Button btnDemo;
        private System.Windows.Forms.Label lblDemoDescripcion;
        private System.Windows.Forms.Button btnSalir;
    }
}
