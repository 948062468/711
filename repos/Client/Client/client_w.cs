using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class client_w : Form
    {
        private const string CacheServerIP = "127.0.0.1";
        private const int CacheServerPort = 8081;
        private const string ServerAddress = "127.0.0.1";
        private const int ServerPort = 8080;

        public client_w()
        {
            InitializeComponent();
            button1.Click += button1_Click;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ServerAddress, ServerPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send the command for listing files (0)
                        await stream.WriteAsync(new byte[] { 0 }, 0, 1);

                        int fileCount = BitConverter.ToInt32(await ReadBytesAsync(stream, 4), 0);
                        for (int i = 0; i < fileCount; i++)
                        {
                            int fileNameLength = BitConverter.ToInt32(await ReadBytesAsync(stream, 4), 0);
                            byte[] fileNameBytes = await ReadBytesAsync(stream, fileNameLength);
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

        private async void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                string fileName = listBox1.SelectedItem.ToString();
                string fileContent = await GetFileContentAsync(fileName);
                MessageBox.Show(fileContent, fileName);
            }
            else
            {
                MessageBox.Show("请先从列表中选择一个文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<string> GetFileContentAsync(string fileName)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(CacheServerIP, CacheServerPort);
            using NetworkStream stream = client.GetStream();

            byte[] fileNameLengthBytes = BitConverter.GetBytes(fileName.Length);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            await stream.WriteAsync(fileNameLengthBytes, 0, 4);
            await stream.WriteAsync(fileNameBytes, 0, fileName.Length);

            string receivedText = await ReceiveTextAsync(stream);
            return receivedText;
        }

        private async Task<string> ReceiveTextAsync(NetworkStream stream)
        {
            using MemoryStream memoryStream = new MemoryStream();

            while (true)
            {
                byte[] lineLengthBytes = new byte[4];
                await stream.ReadAsync(lineLengthBytes, 0, 4);
                int lineLength = BitConverter.ToInt32(lineLengthBytes, 0);
                if (lineLength == 0)
                {
                    break;
                }

                byte[] lineBytes = new byte[lineLength];
                await stream.ReadAsync(lineBytes, 0, lineLength);
                memoryStream.Write(lineBytes, 0, lineLength);
                memoryStream.WriteByte((byte)'\n');
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            using StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8);
            string text = await reader.ReadToEndAsync();
            return text;
        }

        private async Task<byte[]> ReadBytesAsync(NetworkStream stream, int count)
        {
            byte[] bytes = new byte[count];
            int bytesRead = 0;
            while (bytesRead < count)
            {
                bytesRead += await stream.ReadAsync(bytes, bytesRead, count - bytesRead);
            }
            return bytes;
        }
    }
}

