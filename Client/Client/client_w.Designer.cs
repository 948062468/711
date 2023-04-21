namespace Client
{
    partial class client_w
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
            listBox1 = new ListBox();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 31;
            listBox1.Location = new Point(32, 32);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(393, 190);
            listBox1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(32, 268);
            button1.Name = "button1";
            button1.Size = new Size(158, 46);
            button1.TabIndex = 1;
            button1.Text = "加载/更新";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(311, 268);
            button2.Name = "button2";
            button2.Size = new Size(114, 46);
            button2.TabIndex = 2;
            button2.Text = "清理";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(507, 32);
            button3.Name = "button3";
            button3.Size = new Size(126, 46);
            button3.TabIndex = 5;
            button3.Text = "查看";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // client_w
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1108, 613);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(listBox1);
            Name = "client_w";
            Text = "client_w";
            ResumeLayout(false);
        }

        #endregion

        private ListBox listBox1;
        private Button button1;
        private Button button2;
        private Button button3;
    }
}