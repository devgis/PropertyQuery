namespace MyGIS
{
    partial class NetworkForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button6 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.gisPanel1 = new MyGIS.GISPanel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.button5);
            this.panel1.Controls.Add(this.button4);
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.checkBox1);
            this.panel1.Controls.Add(this.listBox1);
            this.panel1.Controls.Add(this.button6);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.comboBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(194, 501);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 388);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(161, 12);
            this.label1.TabIndex = 6;
            this.label1.Text = "（在相应位置双击鼠标左键）";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(55, 110);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(120, 23);
            this.button5.TabIndex = 5;
            this.button5.Text = "导出网络结构";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(55, 81);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(120, 23);
            this.button4.TabIndex = 5;
            this.button4.Text = "读取网络结构";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(55, 472);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(120, 23);
            this.button3.TabIndex = 4;
            this.button3.Text = "显示最短路径";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(55, 443);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(120, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "清空起止点";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(13, 369);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(84, 16);
            this.checkBox1.TabIndex = 3;
            this.checkBox1.Text = "添加起止点";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(13, 155);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(162, 208);
            this.listBox1.TabIndex = 2;
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(55, 412);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(120, 23);
            this.button6.TabIndex = 1;
            this.button6.Text = "加载起止点";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(55, 51);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "构建网络结构";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 25);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(163, 20);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.comboBox1_MouseClick);
            // 
            // gisPanel1
            // 
            this.gisPanel1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.gisPanel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gisPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gisPanel1.Location = new System.Drawing.Point(194, 0);
            this.gisPanel1.Name = "gisPanel1";
            this.gisPanel1.Size = new System.Drawing.Size(583, 501);
            this.gisPanel1.TabIndex = 1;
            this.gisPanel1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.gisPanel1_MouseDoubleClick);
            // 
            // NetworkForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(777, 501);
            this.Controls.Add(this.gisPanel1);
            this.Controls.Add(this.panel1);
            this.Name = "NetworkForm";
            this.Text = "NetworkForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBox1;
        private MyGIS.GISPanel gisPanel1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button6;

    }
}