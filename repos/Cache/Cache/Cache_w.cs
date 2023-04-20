using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cache
{
    public partial class Cache_w : Form
    {
        private TcpListener _listener;
        private const int CachePort = 8081;
        private const int ServerPort = 8080;
        private const string ServerIP = "127.0.0.1";
        private const string CacheDirectory = "../../../cache_files";
        private const string LogFilePath = "../../../log.txt";

        public Cache_w()
        {
            InitializeComponent();
            LoadCachedFiles();
            LoadLog();
            StartCache();
        }

        private void LoadCachedFiles()
        {
            listBox1.Items.Clear();
            var files = Directory.GetFiles(CacheDirectory);
            foreach (var file in files)
            {
                listBox1.Items.Add(Path.GetFileName(file));
            }
        }

        private void LoadLog()
        {
            textBox1.Clear();
            using StreamReader reader = new StreamReader(LogFilePath, Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                textBox1.AppendText(line + Environment.NewLine);
            }
        }

        private void WriteLog(string log)
        {
            File.AppendAllText(LogFilePath, log + Environment.NewLine);
            textBox1.AppendText(log + Environment.NewLine);
        }

        private void StartCache()
        {
            _listener = new TcpListener(IPAddress.Any, CachePort);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientConnected, null);
        }

        private async void ClientConnected(IAsyncResult ar)
        {
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(ClientConnected, null);

            using (NetworkStream stream = client.GetStream())
            {
                byte[] fileNameLengthBytes = new byte[4];
                await stream.ReadAsync(fileNameLengthBytes, 0, 4);
                int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                byte[] fileNameBytes = new byte[fileNameLength];
                await stream.ReadAsync(fileNameBytes, 0, fileNameLength);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                string cacheFilePath = Path.Combine(CacheDirectory, fileName);
                if (File.Exists(cacheFilePath))
                {
                    await SendFileContentAsync(stream, cacheFilePath);
                    WriteLog($"User request: File \"{fileName}\" at {DateTime.Now}");
                    WriteLog($"Response: Cache file \"{fileName}\"");
                }
                else
                {
                    await GetFileFromServerAsync(stream, fileName);
                }
            }
        }

        private async Task SendFileContentAsync(NetworkStream stream, string filePath)
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

        private async Task GetFileFromServerAsync(NetworkStream clientStream, string fileName)
        {
            using TcpClient serverClient = new TcpClient();
            await serverClient.ConnectAsync(ServerIP, ServerPort);
            using NetworkStream serverStream = serverClient.GetStream();

            byte[] fileNameLengthBytes = BitConverter.GetBytes(fileName.Length);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            await serverStream.WriteAsync(fileNameLengthBytes, 0, 4);
            await serverStream.WriteAsync(fileNameBytes, 0, fileName.Length);

            string cacheFilePath = Path.Combine(CacheDirectory, fileName);
            using FileStream cacheFile = File.Create(cacheFilePath);
            while (true)
            {
                byte[] lineLengthBytes = new byte[4];
                await serverStream.ReadAsync(lineLengthBytes, 0, 4);
                int lineLength = BitConverter.ToInt32(lineLengthBytes, 0);
                if (lineLength == 0)
                {
                    break;
                }

                byte[] lineBytes = new byte[lineLength];
                await serverStream.ReadAsync(lineBytes, 0, lineLength);
                await clientStream.WriteAsync(lineLengthBytes, 0, 4);
                await clientStream.WriteAsync(lineBytes, 0, lineLength);
                await cacheFile.WriteAsync(lineBytes, 0, lineLength);
                await cacheFile.WriteAsync(new byte[] { (byte)'\n' }, 0, 1);
            }

            WriteLog($"User request: File \"{fileName}\" at {DateTime.Now}");
            WriteLog($"Response: File \"{fileName}\" downloaded from the server");
            LoadCachedFiles();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DirectoryInfo directory = new DirectoryInfo(CacheDirectory);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            LoadCachedFiles();
            WriteLog($"Clean Log at {DateTime.Now}");
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}

