using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class client_w : Form
    {
        private const string ServerAddress = "127.0.0.1";
        private const int ServerPort = 8082;

        public client_w()
        {
            InitializeComponent();
            button1.Click += button1_Click;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ServerAddress, ServerPort);
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine("LIST_FILES");
                        writer.Flush();

                        int fileCount = int.Parse(reader.ReadLine());
                        for (int i = 0; i < fileCount; i++)
                        {
                            listBox1.Items.Add(reader.ReadLine());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string fileName = listBox1.SelectedItem.ToString();
                RequestFileFromCache(fileName);
            }
            else
            {
                MessageBox.Show("ÇëÏÈ´ÓÎÄ¼þÁÐ±íÖÐÑ¡ÔñÒ»¸öÎÄ¼þ¡£", "ÌáÊ¾", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RequestFileFromCache(string fileName)
        {
            try
            {
                using (TcpClient cacheClient = new TcpClient())
                {
                    cacheClient.Connect("127.0.0.1", 8081);
                    using (NetworkStream cacheStream = cacheClient.GetStream())
                    using (StreamReader cacheReader = new StreamReader(cacheStream, Encoding.UTF8))
                    using (StreamWriter cacheWriter = new StreamWriter(cacheStream, Encoding.UTF8))
                    {
                        cacheWriter.WriteLine("REQUEST_FILE");
                        cacheWriter.WriteLine(fileName);
                        cacheWriter.Flush();

                        string response = cacheReader.ReadLine();
                        if (response == "FILE_FOUND")
                        {
                            MemoryStream imageMemoryStream = new MemoryStream();
                            byte[] buffer = new byte[4096];
                            int bytesRead;

                            while ((bytesRead = cacheStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                imageMemoryStream.Write(buffer, 0, bytesRead);
                                if (bytesRead < buffer.Length) break;
                            }

                            Image image = Image.FromStream(imageMemoryStream);
                            pictureBox1.Image = image;
                        }
                        else
                        {
                            MessageBox.Show("文件未找到。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
