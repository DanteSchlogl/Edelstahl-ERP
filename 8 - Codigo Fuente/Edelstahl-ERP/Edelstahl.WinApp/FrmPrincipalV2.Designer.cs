namespace Edelstahl.WinApp
{
    partial class FrmPrincipalV2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlWorkspace = new System.Windows.Forms.Panel();
            this.pnlHeader = new Edelstahl.WinApp.Ui.PanelEncabezado();
            this.btnPerfil = new System.Windows.Forms.Button();
            this.btnPedirAyuda = new System.Windows.Forms.Button();
            this.btnAyuda = new System.Windows.Forms.Button();
            this.btnCerrarSesion = new System.Windows.Forms.Button();
            this.lblModulo = new System.Windows.Forms.Label();
            this.lblUsuario = new System.Windows.Forms.Label();
            this.lblAlertas = new System.Windows.Forms.Label();
            this.lblPedidos = new System.Windows.Forms.Label();
            this.lblIdioma = new System.Windows.Forms.Label();
            this.cboIdioma = new System.Windows.Forms.ComboBox();
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlAceroSidebar = new Edelstahl.WinApp.Ui.PanelAcero();
            this.lblLema = new System.Windows.Forms.Label();
            this.lblTituloAplicacion = new System.Windows.Forms.Label();
            this.pbEmblema = new System.Windows.Forms.PictureBox();
            this.pnlHeader.SuspendLayout();
            this.pnlSidebar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmblema)).BeginInit();
            this.SuspendLayout();
            //
            // pnlWorkspace
            //
            this.pnlWorkspace.BackColor = Edelstahl.WinApp.Ui.Estilo.Fondo;
            this.pnlWorkspace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWorkspace.Location = new System.Drawing.Point(250, 106);
            this.pnlWorkspace.Name = "pnlWorkspace";
            this.pnlWorkspace.Size = new System.Drawing.Size(550, 344);
            this.pnlWorkspace.TabIndex = 0;
            //
            // pnlHeader
            //
            this.pnlHeader.BackColor = Edelstahl.WinApp.Ui.Estilo.Superficie;
            this.pnlHeader.Controls.Add(this.btnPerfil);
            this.pnlHeader.Controls.Add(this.btnPedirAyuda);
            this.pnlHeader.Controls.Add(this.btnAyuda);
            this.pnlHeader.Controls.Add(this.btnCerrarSesion);
            this.pnlHeader.Controls.Add(this.lblModulo);
            this.pnlHeader.Controls.Add(this.lblUsuario);
            this.pnlHeader.Controls.Add(this.lblAlertas);
            this.pnlHeader.Controls.Add(this.lblPedidos);
            this.pnlHeader.Controls.Add(this.lblIdioma);
            this.pnlHeader.Controls.Add(this.cboIdioma);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(800, 106);
            this.pnlHeader.TabIndex = 1;
            this.pnlHeader.Resize += new System.EventHandler(this.PnlHeader_Resize);
            //
            // btnPerfil
            //
            this.btnPerfil.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPerfil.AutoSize = true;
            this.btnPerfil.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPerfil.BackColor = System.Drawing.Color.FromArgb(64, 106, 172);
            this.btnPerfil.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPerfil.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPerfil.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPerfil.ForeColor = System.Drawing.Color.White;
            this.btnPerfil.Location = new System.Drawing.Point(220, 68);
            this.btnPerfil.MinimumSize = new System.Drawing.Size(96, 28);
            this.btnPerfil.Name = "btnPerfil";
            this.btnPerfil.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnPerfil.Size = new System.Drawing.Size(96, 28);
            this.btnPerfil.TabIndex = 0;
            this.btnPerfil.Tag = "My profile";
            this.btnPerfil.Text = "My profile";
            this.btnPerfil.UseVisualStyleBackColor = false;
            this.btnPerfil.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 215, 245);
            this.btnPerfil.FlatAppearance.BorderSize = 1;
            this.btnPerfil.Click += new System.EventHandler(this.BtnPerfil_Click);
            //
            // btnPedirAyuda
            //
            this.btnPedirAyuda.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPedirAyuda.AutoSize = true;
            this.btnPedirAyuda.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPedirAyuda.BackColor = System.Drawing.Color.FromArgb(64, 106, 172);
            this.btnPedirAyuda.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPedirAyuda.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPedirAyuda.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPedirAyuda.ForeColor = System.Drawing.Color.White;
            this.btnPedirAyuda.Location = new System.Drawing.Point(330, 68);
            this.btnPedirAyuda.MinimumSize = new System.Drawing.Size(96, 28);
            this.btnPedirAyuda.Name = "btnPedirAyuda";
            this.btnPedirAyuda.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnPedirAyuda.Size = new System.Drawing.Size(96, 28);
            this.btnPedirAyuda.TabIndex = 1;
            this.btnPedirAyuda.Tag = "Request help";
            this.btnPedirAyuda.Text = "Request help";
            this.btnPedirAyuda.UseVisualStyleBackColor = false;
            this.btnPedirAyuda.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 215, 245);
            this.btnPedirAyuda.FlatAppearance.BorderSize = 1;
            this.btnPedirAyuda.Click += new System.EventHandler(this.BtnPedirAyuda_Click);
            //
            // btnAyuda
            //
            this.btnAyuda.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAyuda.AutoSize = true;
            this.btnAyuda.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnAyuda.BackColor = System.Drawing.Color.FromArgb(64, 106, 172);
            this.btnAyuda.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAyuda.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAyuda.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAyuda.ForeColor = System.Drawing.Color.White;
            this.btnAyuda.Location = new System.Drawing.Point(440, 68);
            this.btnAyuda.MinimumSize = new System.Drawing.Size(96, 28);
            this.btnAyuda.Name = "btnAyuda";
            this.btnAyuda.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnAyuda.Size = new System.Drawing.Size(96, 28);
            this.btnAyuda.TabIndex = 2;
            this.btnAyuda.Tag = "Help";
            this.btnAyuda.Text = "Help";
            this.btnAyuda.UseVisualStyleBackColor = false;
            this.btnAyuda.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 215, 245);
            this.btnAyuda.FlatAppearance.BorderSize = 1;
            this.btnAyuda.Click += new System.EventHandler(this.BtnAyuda_Click);
            //
            // btnCerrarSesion
            //
            this.btnCerrarSesion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCerrarSesion.AutoSize = true;
            this.btnCerrarSesion.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCerrarSesion.BackColor = System.Drawing.Color.FromArgb(64, 106, 172);
            this.btnCerrarSesion.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCerrarSesion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrarSesion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCerrarSesion.ForeColor = System.Drawing.Color.White;
            this.btnCerrarSesion.Location = new System.Drawing.Point(550, 68);
            this.btnCerrarSesion.MinimumSize = new System.Drawing.Size(96, 28);
            this.btnCerrarSesion.Name = "btnCerrarSesion";
            this.btnCerrarSesion.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCerrarSesion.Size = new System.Drawing.Size(96, 28);
            this.btnCerrarSesion.TabIndex = 3;
            this.btnCerrarSesion.Tag = "Sign out";
            this.btnCerrarSesion.Text = "Sign out";
            this.btnCerrarSesion.UseVisualStyleBackColor = false;
            this.btnCerrarSesion.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 215, 245);
            this.btnCerrarSesion.FlatAppearance.BorderSize = 1;
            this.btnCerrarSesion.Click += new System.EventHandler(this.BtnCerrarSesion_Click);
            //
            // lblModulo
            //
            this.lblModulo.AutoSize = true;
            this.lblModulo.BackColor = System.Drawing.Color.Transparent;
            this.lblModulo.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblModulo.ForeColor = System.Drawing.Color.White;
            this.lblModulo.Location = new System.Drawing.Point(84, 20);
            this.lblModulo.Name = "lblModulo";
            this.lblModulo.TabIndex = 4;
            //
            // lblUsuario
            //
            this.lblUsuario.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUsuario.AutoSize = true;
            this.lblUsuario.BackColor = System.Drawing.Color.Transparent;
            this.lblUsuario.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUsuario.ForeColor = System.Drawing.Color.FromArgb(222, 232, 252);
            this.lblUsuario.Location = new System.Drawing.Point(600, 46);
            this.lblUsuario.Name = "lblUsuario";
            this.lblUsuario.TabIndex = 5;
            //
            // lblAlertas
            //
            this.lblAlertas.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAlertas.AutoSize = true;
            this.lblAlertas.BackColor = System.Drawing.Color.Transparent;
            this.lblAlertas.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAlertas.ForeColor = System.Drawing.Color.FromArgb(255, 220, 110);
            this.lblAlertas.Location = new System.Drawing.Point(400, 46);
            this.lblAlertas.Name = "lblAlertas";
            this.lblAlertas.TabIndex = 6;
            this.lblAlertas.Visible = false;
            //
            // lblPedidos
            //
            this.lblPedidos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPedidos.AutoSize = true;
            this.lblPedidos.BackColor = System.Drawing.Color.Transparent;
            this.lblPedidos.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblPedidos.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPedidos.ForeColor = System.Drawing.Color.FromArgb(255, 190, 120);
            this.lblPedidos.Location = new System.Drawing.Point(300, 46);
            this.lblPedidos.Name = "lblPedidos";
            this.lblPedidos.TabIndex = 7;
            this.lblPedidos.Visible = false;
            this.lblPedidos.Click += new System.EventHandler(this.LblPedidos_Click);
            //
            // lblIdioma
            //
            this.lblIdioma.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblIdioma.AutoSize = true;
            this.lblIdioma.BackColor = System.Drawing.Color.Transparent;
            this.lblIdioma.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblIdioma.ForeColor = System.Drawing.Color.White;
            this.lblIdioma.Location = new System.Drawing.Point(500, 18);
            this.lblIdioma.Name = "lblIdioma";
            this.lblIdioma.TabIndex = 8;
            //
            // cboIdioma
            //
            this.cboIdioma.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cboIdioma.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboIdioma.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboIdioma.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboIdioma.ItemHeight = 24;
            this.cboIdioma.Location = new System.Drawing.Point(546, 12);
            this.cboIdioma.Name = "cboIdioma";
            this.cboIdioma.Size = new System.Drawing.Size(230, 32);
            this.cboIdioma.TabIndex = 9;
            this.cboIdioma.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.CboIdioma_DrawItem);
            this.cboIdioma.SelectedIndexChanged += new System.EventHandler(this.CboIdioma_SelectedIndexChanged);
            //
            // pnlSidebar
            //
            this.pnlSidebar.BackColor = Edelstahl.WinApp.Ui.Estilo.Lateral;
            this.pnlSidebar.Controls.Add(this.pnlAceroSidebar);
            this.pnlSidebar.Controls.Add(this.lblLema);
            this.pnlSidebar.Controls.Add(this.lblTituloAplicacion);
            this.pnlSidebar.Controls.Add(this.pbEmblema);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 106);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(250, 344);
            this.pnlSidebar.TabIndex = 2;
            //
            // pnlAceroSidebar
            //
            this.pnlAceroSidebar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlAceroSidebar.Location = new System.Drawing.Point(0, 44);
            this.pnlAceroSidebar.Name = "pnlAceroSidebar";
            this.pnlAceroSidebar.Size = new System.Drawing.Size(250, 300);
            this.pnlAceroSidebar.TabIndex = 0;
            //
            // lblLema
            //
            this.lblLema.AutoSize = true;
            this.lblLema.BackColor = System.Drawing.Color.Transparent;
            this.lblLema.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblLema.ForeColor = System.Drawing.Color.FromArgb(214, 224, 247);
            this.lblLema.Location = new System.Drawing.Point(80, 46);
            this.lblLema.Name = "lblLema";
            this.lblLema.Text = "ERP  |  STAINLESS STEEL";
            this.lblLema.TabIndex = 1;
            //
            // lblTituloAplicacion
            //
            this.lblTituloAplicacion.AutoSize = true;
            this.lblTituloAplicacion.BackColor = System.Drawing.Color.Transparent;
            this.lblTituloAplicacion.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblTituloAplicacion.ForeColor = System.Drawing.Color.White;
            this.lblTituloAplicacion.Location = new System.Drawing.Point(78, 18);
            this.lblTituloAplicacion.Name = "lblTituloAplicacion";
            this.lblTituloAplicacion.Text = "EDELSTAHL";
            this.lblTituloAplicacion.TabIndex = 2;
            //
            // pbEmblema
            //
            this.pbEmblema.BackColor = System.Drawing.Color.Transparent;
            this.pbEmblema.Location = new System.Drawing.Point(16, 14);
            this.pbEmblema.Name = "pbEmblema";
            this.pbEmblema.Size = new System.Drawing.Size(56, 56);
            this.pbEmblema.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbEmblema.TabIndex = 3;
            this.pbEmblema.TabStop = false;
            //
            // FrmPrincipalV2
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pnlWorkspace);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlSidebar);
            this.Name = "FrmPrincipalV2";
            this.Text = "FrmPrincipalV2";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmblema)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlWorkspace;
        private Edelstahl.WinApp.Ui.PanelEncabezado pnlHeader;
        private System.Windows.Forms.Button btnPerfil;
        private System.Windows.Forms.Button btnPedirAyuda;
        private System.Windows.Forms.Button btnAyuda;
        private System.Windows.Forms.Button btnCerrarSesion;
        private System.Windows.Forms.Label lblModulo;
        private System.Windows.Forms.Label lblUsuario;
        private System.Windows.Forms.Label lblAlertas;
        private System.Windows.Forms.Label lblPedidos;
        private System.Windows.Forms.Label lblIdioma;
        private System.Windows.Forms.ComboBox cboIdioma;
        private System.Windows.Forms.Panel pnlSidebar;
        private Edelstahl.WinApp.Ui.PanelAcero pnlAceroSidebar;
        private System.Windows.Forms.Label lblLema;
        private System.Windows.Forms.Label lblTituloAplicacion;
        private System.Windows.Forms.PictureBox pbEmblema;
    }
}
