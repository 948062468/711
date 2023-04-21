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
                MessageBox.Show("���ȴ��ļ��б���ѡ��һ���ļ���", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            // ������Խ��ļ�������ʾ��һ���ı���������ؼ���
                            string fileContent = cacheReader.ReadToEnd();
                            MessageBox.Show(fileContent, "�ļ�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("�ļ�δ�ҵ���", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
