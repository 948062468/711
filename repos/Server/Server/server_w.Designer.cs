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
            button1 = new Button();
            listBox1 = new ListBox();
            button2 = new Button();
            listBox2 = new ListBox();
            button3 = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(30, 24);
            button1.Name = "button1";
            button1.Size = new Size(186, 46);
            button1.TabIndex = 0;
            button1.Text = "查看所有文件";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 31;
            listBox1.Location = new Point(30, 104);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(376, 190);
            listBox1.TabIndex = 1;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // button2
            // 
            button2.Location = new Point(459, 176);
            button2.Name = "button2";
            button2.Size = new Size(150, 46);
            button2.TabIndex = 2;
            button2.Text = "授权";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // listBox2
            // 
            listBox2.FormattingEnabled = true;
            listBox2.ItemHeight = 31;
            listBox2.Location = new Point(673, 104);
            listBox2.Name = "listBox2";
            listBox2.Size = new Size(378, 190);
            listBox2.TabIndex = 3;
            // 
            // button3
            // 
            button3.Location = new Point(673, 24);
            button3.Name = "button3";
            button3.Size = new Size(177, 46);
            button3.TabIndex = 4;
            button3.Text = "查看可用文件";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // server_w
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1197, 649);
            Controls.Add(button3);
            Controls.Add(listBox2);
            Controls.Add(button2);
            Controls.Add(listBox1);
            Controls.Add(button1);
            Name = "server_w";
            Text = "Server_w";
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private ListBox listBox1;
        private Button button2;
        private ListBox listBox2;
        private Button button3;
    }
}