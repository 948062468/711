namespace Server
{
    partial class server_w
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button2 = new System.Windows.Forms.Button();
            this.listBox2 = new System.Windows.Forms.ListBox();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 12);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(93, 22);
            this.button1.TabIndex = 0;
            this.button1.Text = "查看所有文件";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 15;
            this.listBox1.Location = new System.Drawing.Point(15, 50);
            this.listBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(190, 94);
            this.listBox1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(230, 85);
            this.button2.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 22);
            this.button2.TabIndex = 2;
            this.button2.Text = "授权";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // listBox2
            // 
            this.listBox2.FormattingEnabled = true;
            this.listBox2.ItemHeight = 15;
            this.listBox2.Location = new System.Drawing.Point(336, 50);
            this.listBox2.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.listBox2.Name = "listBox2";
            this.listBox2.Size = new System.Drawing.Size(191, 94);
            this.listBox2.TabIndex = 3;
            this.listBox2.SelectedIndexChanged += new System.EventHandler(this.listBox2_SelectedIndexChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(336, 12);
            this.button3.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(88, 22);
            this.button3.TabIndex = 4;
            this.button3.Text = "查看可用文件";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(130, 159);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 5;
            this.button4.Text = "添加文件";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // server_w
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(598, 314);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.listBox2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Name = "server_w";
            this.Text = "Server_w";
            this.ResumeLayout(false);

        }

        #endregion

        private Button button1;
        private ListBox listBox1;
        private Button button2;
        private ListBox listBox2;
        private Button button3;
        private Button button4;
    }
}