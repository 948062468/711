namespace Cache
{
    partial class Cache_w
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
            label1 = new Label();
            label2 = new Label();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(317, 307);
            button1.Name = "button1";
            button1.Size = new Size(150, 46);
            button1.TabIndex = 0;
            button1.Text = "清空";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 31;
            listBox1.Location = new Point(59, 87);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(408, 190);
            listBox1.TabIndex = 1;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(59, 39);
            label1.Name = "label1";
            label1.Size = new Size(134, 31);
            label1.TabIndex = 2;
            label1.Text = "已缓存文件";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(572, 39);
            label2.Name = "label2";
            label2.Size = new Size(62, 31);
            label2.TabIndex = 3;
            label2.Text = "日志";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(581, 87);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(463, 38);
            textBox1.TabIndex = 4;
            // 
            // Cache_w
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1056, 618);
            Controls.Add(textBox1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(listBox1);
            Controls.Add(button1);
            Name = "Cache_w";
            Text = "Cache_w";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private ListBox listBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox1;
    }
}