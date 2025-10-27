namespace orderpath_server
{
    partial class Server
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
            this.lb_status = new System.Windows.Forms.Label();
            this.tb_status = new System.Windows.Forms.TextBox();
            this.bt_chay = new System.Windows.Forms.Button();
            this.bt_KetThuc = new System.Windows.Forms.Button();
            this.lb_TieuDe = new System.Windows.Forms.Label();
            this.tb_data = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lb_status
            // 
            this.lb_status.Location = new System.Drawing.Point(13, 9);
            this.lb_status.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lb_status.Name = "lb_status";
            this.lb_status.Size = new System.Drawing.Size(160, 63);
            this.lb_status.TabIndex = 0;
            this.lb_status.Text = "STATUS:";
            this.lb_status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tb_status
            // 
            this.tb_status.Location = new System.Drawing.Point(150, 24);
            this.tb_status.Name = "tb_status";
            this.tb_status.Size = new System.Drawing.Size(234, 30);
            this.tb_status.TabIndex = 1;
            // 
            // bt_chay
            // 
            this.bt_chay.Location = new System.Drawing.Point(607, 24);
            this.bt_chay.Name = "bt_chay";
            this.bt_chay.Size = new System.Drawing.Size(163, 48);
            this.bt_chay.TabIndex = 2;
            this.bt_chay.Text = "Start";
            this.bt_chay.UseVisualStyleBackColor = true;
            this.bt_chay.Click += new System.EventHandler(this.bt_chay_Click);
            // 
            // bt_KetThuc
            // 
            this.bt_KetThuc.Location = new System.Drawing.Point(899, 24);
            this.bt_KetThuc.Name = "bt_KetThuc";
            this.bt_KetThuc.Size = new System.Drawing.Size(163, 48);
            this.bt_KetThuc.TabIndex = 3;
            this.bt_KetThuc.Text = "End";
            this.bt_KetThuc.UseVisualStyleBackColor = true;
            this.bt_KetThuc.Click += new System.EventHandler(this.bt_KetThuc_Click);
            // 
            // lb_TieuDe
            // 
            this.lb_TieuDe.Location = new System.Drawing.Point(13, 161);
            this.lb_TieuDe.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lb_TieuDe.Name = "lb_TieuDe";
            this.lb_TieuDe.Size = new System.Drawing.Size(246, 63);
            this.lb_TieuDe.TabIndex = 4;
            this.lb_TieuDe.Text = "Client đang kết nối:";
            this.lb_TieuDe.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tb_data
            // 
            this.tb_data.Location = new System.Drawing.Point(365, 178);
            this.tb_data.Multiline = true;
            this.tb_data.Name = "tb_data";
            this.tb_data.Size = new System.Drawing.Size(697, 357);
            this.tb_data.TabIndex = 6;
            // 
            // Server
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 22F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1156, 767);
            this.Controls.Add(this.tb_data);
            this.Controls.Add(this.lb_TieuDe);
            this.Controls.Add(this.bt_KetThuc);
            this.Controls.Add(this.bt_chay);
            this.Controls.Add(this.tb_status);
            this.Controls.Add(this.lb_status);
            this.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Server";
            this.Text = "Server";
            this.Load += new System.EventHandler(this.Server_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lb_status;
        private System.Windows.Forms.TextBox tb_status;
        private System.Windows.Forms.Button bt_chay;
        private System.Windows.Forms.Button bt_KetThuc;
        private System.Windows.Forms.Label lb_TieuDe;
        private System.Windows.Forms.TextBox tb_data;
    }
}

