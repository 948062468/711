using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace Client
{
    public partial class client_w : Form
    {

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
                using (TcpClient cacheClient = new TcpClient())
                {
                    cacheClient.Connect("127.0.0.1", 8081);
                    using (NetworkStream cacheStream = cacheClient.GetStream())
                    {
                        // 发送操作码0，请求文件列表
                        byte command = 0;
                        cacheStream.WriteByte(command);
                        cacheStream.Flush();

                        // 读取文件数量
                        byte[] fileCountBytes = new byte[4];
                        cacheStream.Read(fileCountBytes, 0, fileCountBytes.Length);
                        int fileCount = BitConverter.ToInt32(fileCountBytes, 0);

                        // 读取文件名
                        for (int i = 0; i < fileCount; i++)
                        {
                            byte[] fileNameLengthBytes = new byte[4];
                            cacheStream.Read(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                            byte[] fileNameBytes = new byte[fileNameLength];
                            cacheStream.Read(fileNameBytes, 0, fileNameBytes.Length);
                            string fileName = Encoding.UTF8.GetString(fileNameBytes);

                            listBox1.Items.Add(fileName);
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
                    {
                        //指令1，请求文件内容
                        Byte command = 1;
                        cacheStream.WriteByte(command);
                        cacheStream.Flush();

                        //发送所选文件名和文件名长度
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                        byte[] fileNameLengBytes = BitConverter.GetBytes(fileNameBytes.Length);
                        cacheStream.Write(fileNameLengBytes, 0, 4);
                        cacheStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                        cacheStream.Flush();

                        using (MemoryStream receivedImageData = new MemoryStream())
                        {
                            int bufferSize = 4096; // 缓冲区大小
                            byte[] buffer = new byte[bufferSize];
                            int bytesRead = 0;

                            do
                            {
                                bytesRead = cacheStream.Read(buffer, 0, bufferSize);
                                receivedImageData.Write(buffer, 0, bytesRead);
                            } while (bytesRead > 0);

                            receivedImageData.Position = 0; // 将内存流的位置重置为0，以便从头开始读取数据
                            Image receivedImage = Image.FromStream(receivedImageData);
                            pictureBox1.Image = receivedImage;
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
