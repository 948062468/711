using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class server_w : Form
    {
        private TcpListener _listener;
        private const int Port = 8080;

        public server_w()
        {
            InitializeComponent();
            StartServer();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientConnected, null);
        }

        private async void ClientConnected(IAsyncResult ar)
        {
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(ClientConnected, null);

            using (NetworkStream stream = client.GetStream())
            {
                byte[] commandBuffer = new byte[1];
                await stream.ReadAsync(commandBuffer, 0, 1);
                int command = commandBuffer[0];
                switch (command)
                {
                    case 0: // List files
                        await SendFilesListAsync(stream);
                        break;
                    case 1: // Send file content
                        await SendFileContentAsync(stream);
                        break;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }

        private async Task SendFilesListAsync(NetworkStream stream)
        {
            var files = Directory.GetFiles("../../../available_files");
            byte[] filesCountBytes = BitConverter.GetBytes(files.Length);
            await stream.WriteAsync(filesCountBytes, 0, 4);

            foreach (var file in files)
            {
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(file));
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                await stream.WriteAsync(fileNameLengthBytes, 0, 4);
                await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);
            }
        }

        private async Task SendFileContentAsync(NetworkStream stream)
        {
            byte[] fileNameLengthBytes = new byte[4];
            await stream.ReadAsync(fileNameLengthBytes, 0, 4);
            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

            byte[] fileNameBytes = new byte[fileNameLength];
            await stream.ReadAsync(fileNameBytes, 0, fileNameLength);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);

            string filePath = Path.Combine("C:/files", fileName);
            if (File.Exists(filePath))
            {
                using StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    byte[] lineBytes = Encoding.UTF8.GetBytes(line);
                    byte[] lineLengthBytes = BitConverter.GetBytes(lineBytes.Length);
                    await stream.WriteAsync(lineLengthBytes, 0, 4);
                    await stream.WriteAsync(lineBytes, 0, lineBytes.Length);
                }

                byte[] emptyLineLengthBytes = BitConverter.GetBytes(0);
                await stream.WriteAsync(emptyLineLengthBytes, 0, 4);
            }
            else
            {
                Console.WriteLine($"File not found: {fileName}");
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


        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            var files = Directory.GetFiles("../../../all_files");
            foreach (var file in files)
            {
                listBox1.Items.Add(Path.GetFileName(file));
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

    }
}
