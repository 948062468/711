using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Cache
{
    public partial class Cache_w : Form
    {
        private CacheServer _cacheServer;

        public Cache_w()
        {
            InitializeComponent();
            _cacheServer = new CacheServer(UpdateLogTextbox); // Pass UpdateLogTextbox method
            _cacheServer.UpdateCacheFileList = UpdateCacheFileList;
            UpdateCacheFileList();
            UpdateLogTextBox();
        }

        private void UpdateCacheFileList()
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke((MethodInvoker)delegate { UpdateCacheFileList(); });
            }
            else
            {
                listBox1.Items.Clear();
                var files = Directory.GetFiles("../../../cache_files");
                foreach (var file in files)
                {
                    listBox1.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void UpdateLogTextBox()
        {
            string logFilePath = "../../../log.txt";
            if (File.Exists(logFilePath))
            {
                textBox1.Text = File.ReadAllText(logFilePath);
            }
        }

        private void UpdateLogTextbox(string logEntry)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke((MethodInvoker)delegate { UpdateLogTextbox(logEntry); });
            }
            else
            {
                textBox1.AppendText(logEntry + Environment.NewLine);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(_cacheServer.CacheFolderPath);

            foreach (string file in files)
            {
                File.Delete(file);
            }

            _cacheServer.WriteLog("clean cache");
            UpdateCacheFileList();
            UpdateLogTextBox();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string logFilePath = "../../../log.txt";

            if (File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, string.Empty);
                textBox1.Clear(); // Clear the TextBox
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

    }

    public class CacheServer
    {
        private const int CachePort = 8081;
        private const int ServerPort = 8082;
        private const string ServerAddress = "127.0.0.1";
        private TcpListener _listener;

        public string CacheFolderPath { get; } = "../../../cache_files";

        public Action UpdateCacheFileList;

        public CacheServer()
        {
            StartCache();
        }

        private Action<string> _updateLogTextbox; // Add a private field to store the delegate

        public CacheServer(Action<string> updateLogTextbox) // Add a parameter to the constructor
        {
            _updateLogTextbox = updateLogTextbox;
            StartCache();
        }

        public void WriteLog(string message)
        {
            string logFilePath = "../../../log.txt";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"{timestamp}: {message}";

            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            _updateLogTextbox(logEntry); // Call the delegate after writing to the log file
        }

        private void StartCache()
        {
            _listener = new TcpListener(IPAddress.Any, CachePort);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientConnected, null);
        }

        private void ClientConnected(IAsyncResult ar)
        {
            TcpClient cacheClient = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(ClientConnected, null);

            using (NetworkStream cacheStream = cacheClient.GetStream())
            using (StreamReader cacheReader = new StreamReader(cacheStream, Encoding.UTF8))
            using (StreamWriter cacheWriter = new StreamWriter(cacheStream, Encoding.UTF8))
            {
                string command = cacheReader.ReadLine();
                if (command == "REQUEST_FILE")
                {
                    string fileName = cacheReader.ReadLine();
                    string cacheFilePath = Path.Combine(CacheFolderPath, fileName);

                    if (!File.Exists(cacheFilePath))
                    {
                        using (TcpClient serverClient = new TcpClient())
                        {
                            serverClient.Connect(ServerAddress, ServerPort);
                            using (NetworkStream serverStream = serverClient.GetStream())
                            using (StreamReader serverReader = new StreamReader(serverStream, Encoding.UTF8))
                            using (StreamWriter serverWriter = new StreamWriter(serverStream, Encoding.UTF8))
                            {
                                serverWriter.WriteLine("SEND_FILE");
                                serverWriter.WriteLine(fileName);
                                serverWriter.Flush();

                                string response = serverReader.ReadLine();
                                if (response == "FILE_FOUND")
                                {
                                    Directory.CreateDirectory(CacheFolderPath);
                                    using (FileStream fileStream = File.Create(cacheFilePath))
                                    {
                                        byte[] buffer = new byte[4096];
                                        int bytesRead;

                                        while ((bytesRead = serverStream.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            fileStream.Write(buffer, 0, bytesRead);
                                            if (bytesRead < buffer.Length) break;
                                        }
                                    }
                                    WriteLog($"User request: File \"{fileName}\"");
                                    WriteLog($"Response: File \"{fileName}\" downloaded from the server");
                                }
                            }
                        }
                    }

                    if (File.Exists(cacheFilePath))
                    {
                        cacheWriter.WriteLine("FILE_FOUND");
                        cacheWriter.Flush();

                        using (FileStream fileStream = File.OpenRead(cacheFilePath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;

                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                cacheStream.Write(buffer, 0, bytesRead);
                                if (bytesRead < buffer.Length) break;
                            }
                        }

                        WriteLog($"User request: File \"{fileName}\"");
                        WriteLog($"Response: Cache file \"{fileName}\"");

                        UpdateCacheFileList?.Invoke();
                    }
                    else
                    {
                        cacheWriter.WriteLine("FILE_NOT_FOUND");
                        cacheWriter.Flush();
                    }
                }
            }
        }
    }
}

