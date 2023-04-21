using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public partial class server_w : Form
    {
        private TcpListener _listener;
        private const int Port = 8082;

        public server_w()
        {
            InitializeComponent();
            StartServer();
        }

        private void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientConnected, null);
        }

    private void ClientConnected(IAsyncResult ar)
    {
        TcpClient client = _listener.EndAcceptTcpClient(ar);
        _listener.BeginAcceptTcpClient(ClientConnected, null);

        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
        {
            string command = reader.ReadLine();
            if (command == "LIST_FILES")
            {
                    var files = Directory.GetFiles("../../../available_files");
                    writer.WriteLine(files.Length);
                    foreach (var file in files)
                    {
                        writer.WriteLine(Path.GetFileName(file));
                    }
                }
            else if (command == "SEND_FILE")
            {
                string requestedFile = reader.ReadLine();
                string sourcePath = Path.Combine("../../../available_files", requestedFile);
                if (File.Exists(sourcePath))
                {
                    writer.WriteLine("FILE_FOUND");
                    writer.Flush();

                    using (FileStream fileStream = File.OpenRead(sourcePath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                            if (bytesRead < buffer.Length) break;
                        }
                    }
                }
                else
                {
                    writer.WriteLine("FILE_NOT_FOUND");
                    writer.Flush();
                }
            }
        }
    }


    private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            var files = Directory.GetFiles("../../../all_files");
            foreach (var file in files)
            {
                listBox1.Items.Add(Path.GetFileName(file));
            }
        }

        private void DisplayAvailableFiles()
        {
            listBox2.Items.Clear();
            var files = Directory.GetFiles("../../../available_files");
            foreach (var file in files)
            {
                listBox2.Items.Add(Path.GetFileName(file));
            }
        }

        private void CopySelectedFileToAvailableFiles()
        {
            if (listBox1.SelectedItem != null)
            {
                string fileName = listBox1.SelectedItem.ToString();
                string sourcePath = Path.Combine("../../../all_files", fileName);
                string destinationPath = Path.Combine("../../../available_files", fileName);

                if (!File.Exists(destinationPath))
                {
                    File.Copy(sourcePath, destinationPath);
                    DisplayAvailableFiles();
                }
                else
                {
                    MessageBox.Show("文件已存在于 available_files 文件夹中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("请先从 all_files 列表中选择一个文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CopySelectedFileToAvailableFiles();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DisplayAvailableFiles();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
