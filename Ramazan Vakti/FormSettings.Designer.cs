namespace Ramazan_Vakti {
    partial class FormSettings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSettings));
            cbEnableReminder = new CheckBox();
            lblTransparency = new Label();
            tbTransparency = new TrackBar();
            lblTransparencyValue = new Label();
            btnClose = new Label();
            cbChangeCity = new ComboBox();
            cbRunOnStartup = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)tbTransparency).BeginInit();
            SuspendLayout();
            // 
            // cbEnableReminder
            // 
            cbEnableReminder.AutoSize = true;
            cbEnableReminder.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            cbEnableReminder.ForeColor = Color.WhiteSmoke;
            cbEnableReminder.Location = new Point(24, 40);
            cbEnableReminder.Name = "cbEnableReminder";
            cbEnableReminder.Size = new Size(105, 17);
            cbEnableReminder.TabIndex = 0;
            cbEnableReminder.Text = "Hatırlatıcı Uyarı";
            cbEnableReminder.UseVisualStyleBackColor = true;
            cbEnableReminder.CheckedChanged += cbEnableReminder_CheckedChanged;
            // 
            // lblTransparency
            // 
            lblTransparency.AutoSize = true;
            lblTransparency.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblTransparency.ForeColor = Color.WhiteSmoke;
            lblTransparency.Location = new Point(34, 97);
            lblTransparency.Name = "lblTransparency";
            lblTransparency.Size = new Size(70, 13);
            lblTransparency.TabIndex = 2;
            lblTransparency.Text = "Şeffaflık (%)";
            // 
            // tbTransparency
            // 
            tbTransparency.Location = new Point(34, 113);
            tbTransparency.Maximum = 100;
            tbTransparency.Minimum = 50;
            tbTransparency.Name = "tbTransparency";
            tbTransparency.Size = new Size(150, 45);
            tbTransparency.TabIndex = 3;
            tbTransparency.TickFrequency = 5;
            tbTransparency.Value = 80;
            tbTransparency.ValueChanged += tbTransparency_ValueChanged;
            // 
            // lblTransparencyValue
            // 
            lblTransparencyValue.AutoSize = true;
            lblTransparencyValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblTransparencyValue.ForeColor = Color.WhiteSmoke;
            lblTransparencyValue.Location = new Point(155, 97);
            lblTransparencyValue.Name = "lblTransparencyValue";
            lblTransparencyValue.Size = new Size(29, 13);
            lblTransparencyValue.TabIndex = 4;
            lblTransparencyValue.Text = "80%";
            // 
            // btnClose
            // 
            btnClose.AutoSize = true;
            btnClose.Font = new Font("Webdings", 11.25F, FontStyle.Bold);
            btnClose.ForeColor = Color.WhiteSmoke;
            btnClose.Location = new Point(194, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(25, 20);
            btnClose.TabIndex = 8;
            btnClose.Text = "r";
            btnClose.Click += btnClose_Click;
            // 
            // cbChangeCity
            // 
            cbChangeCity.BackColor = Color.White;
            cbChangeCity.DropDownStyle = ComboBoxStyle.DropDownList;
            cbChangeCity.FlatStyle = FlatStyle.Flat;
            cbChangeCity.FormattingEnabled = true;
            cbChangeCity.Items.AddRange(new object[] { "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkâri", "Hatay", "Iğdır", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis", "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Sivas", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak" });
            cbChangeCity.Location = new Point(50, 180);
            cbChangeCity.Name = "cbChangeCity";
            cbChangeCity.Size = new Size(121, 23);
            cbChangeCity.TabIndex = 9;
            cbChangeCity.DropDownClosed += cbChangeCity_DropDownClosed;
            // 
            // cbRunOnStartup
            // 
            cbRunOnStartup.AutoSize = true;
            cbRunOnStartup.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            cbRunOnStartup.ForeColor = Color.WhiteSmoke;
            cbRunOnStartup.Location = new Point(24, 63);
            cbRunOnStartup.Name = "cbRunOnStartup";
            cbRunOnStartup.Size = new Size(123, 17);
            cbRunOnStartup.TabIndex = 10;
            cbRunOnStartup.Text = "Başlangıçta Çalıştır";
            cbRunOnStartup.UseVisualStyleBackColor = true;
            cbRunOnStartup.CheckedChanged += cbRunStartup_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label1.ForeColor = Color.White;
            label1.Location = new Point(5, 6);
            label1.Name = "label1";
            label1.Size = new Size(44, 15);
            label1.TabIndex = 11;
            label1.Text = "Ayarlar";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 162);
            label2.ForeColor = Color.WhiteSmoke;
            label2.Location = new Point(87, 161);
            label2.Name = "label2";
            label2.Size = new Size(46, 13);
            label2.TabIndex = 14;
            label2.Text = "Şehirler";
            // 
            // FormSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(220, 220);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(cbRunOnStartup);
            Controls.Add(cbChangeCity);
            Controls.Add(lblTransparencyValue);
            Controls.Add(tbTransparency);
            Controls.Add(lblTransparency);
            Controls.Add(btnClose);
            Controls.Add(cbEnableReminder);
            ForeColor = SystemColors.ControlText;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormSettings";
            ShowInTaskbar = false;
            Text = "Settings";
            Load += FormSettings_Load;
            MouseDown += FormSettings_MouseDown;
            MouseMove += FormSettings_MouseMove;
            ((System.ComponentModel.ISupportInitialize)tbTransparency).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox cbEnableReminder;
        private Label lblTransparency;
        private TrackBar tbTransparency;
        private Label lblTransparencyValue;
        private Label btnClose;
        private ComboBox cbChangeCity;
        private CheckBox cbRunOnStartup;
        private Label label1;
        private Label label2;
    }
}