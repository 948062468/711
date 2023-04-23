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

        private static string ReassembleFileIntoTempFile(string chunksFolderPath)
        {
            string tempFile = Path.Combine(chunksFolderPath, "reconstructed_temp.dat");

            using (FileStream reconstructedFileStream = new FileStream(tempFile, FileMode.Create))
            {
                string[] chunkFiles = Directory.GetFiles(chunksFolderPath, "chunk_*.dat");

                Array.Sort(chunkFiles, (file1, file2) =>
                {
                    int chunkNumber1 = int.Parse(Path.GetFileNameWithoutExtension(file1).Split('_')[1]);
                    int chunkNumber2 = int.Parse(Path.GetFileNameWithoutExtension(file2).Split('_')[1]);
                    return chunkNumber1.CompareTo(chunkNumber2);
                });

                foreach (string chunkFile in chunkFiles)
                {
                    byte[] chunkData = File.ReadAllBytes(chunkFile);
                    reconstructedFileStream.Write(chunkData, 0, chunkData.Length);
                }
            }

            return tempFile;
        }


        private void ClientConnected(IAsyncResult ar)
        {
            TcpClient cacheClient = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(ClientConnected, null);

            using (NetworkStream cacheStream = cacheClient.GetStream())
            {
                // 接收来自client的请求
                byte command = (byte)cacheStream.ReadByte();

                if (command == 0)
                {
                    using (TcpClient serverClient = new TcpClient())
                    {
                        serverClient.Connect(ServerAddress, ServerPort);
                        using (NetworkStream serverStream = serverClient.GetStream())
                        {
                            serverStream.WriteByte(command);
                            serverStream.Flush();

                            // 读取文件数量并发送给客户端
                            byte[] fileCountBytes = new byte[4];
                            serverStream.Read(fileCountBytes, 0, fileCountBytes.Length);
                            int fileCount = BitConverter.ToInt32(fileCountBytes, 0);
                            cacheStream.Write(fileCountBytes, 0, fileCountBytes.Length);
                            cacheStream.Flush();

                            // 读取文件名并发送给客户端
                            for (int i = 0; i < fileCount; i++)
                            {
                                byte[] fileNameLengthBytes = new byte[4];
                                serverStream.Read(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                                int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                                byte[] fileNameBytes = new byte[fileNameLength];
                                serverStream.Read(fileNameBytes, 0, fileNameBytes.Length);
                                cacheStream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                                cacheStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                                cacheStream.Flush();
                            }
                        }
                    }
                }
                else if (command == 1)
                {
                    //接收来自client的文件名长度和文件名
                    byte[] fileNameLengthBytes = new byte[4];
                    cacheStream.Read(fileNameLengthBytes, 0, 4);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    byte[] fileNameBytes = new byte[fileNameLength];
                    cacheStream.Read(fileNameBytes, 0, fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);


                    string tempFolderPath = Path.Combine("../../../temp");
                    string cacheChunksFolderPath = Path.Combine("../../../cache_chunks");

                    Directory.CreateDirectory(tempFolderPath);
                    Directory.CreateDirectory(cacheChunksFolderPath);

                    using (TcpClient serverClient = new TcpClient())
                    {
                        serverClient.Connect(ServerAddress, ServerPort);
                        using (NetworkStream serverStream = serverClient.GetStream())                        
                        {
                            //转发给server的获取文件内容指令 1
                            serverStream.WriteByte(command);
                            serverStream.Flush();

                            serverStream.Write(fileNameLengthBytes, 0, 4);
                            serverStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                            serverStream.Flush();

                            
                            // 读取 chunk 数量
                            byte[] chunkCountBytes = new byte[4];
                            serverStream.Read(chunkCountBytes, 0, chunkCountBytes.Length);
                            int chunkCount = BitConverter.ToInt32(chunkCountBytes, 0);

                            for (int i = 1; i <= chunkCount; i++)
                            {
                                byte[] operationCodeBytes = new byte[4];
                                serverStream.Read(operationCodeBytes, 0, operationCodeBytes.Length);
                                int operationCode = BitConverter.ToInt32(operationCodeBytes, 0);

                                if (operationCode == 1) // 完整的 chunk
                                {
                                    //chunk内容长度
                                    byte[] chunkLengthBytes = new byte[4];
                                    serverStream.Read(chunkLengthBytes, 0, chunkLengthBytes.Length);
                                    int chunkLength = BitConverter.ToInt32(chunkLengthBytes, 0);

                                    // 读取哈希值
                                    byte[] hashBytes = new byte[64];
                                    serverStream.Read(hashBytes, 0, hashBytes.Length);
                                    string hash = Encoding.UTF8.GetString(hashBytes);

                                    //读取chunk内容
                                    byte[] chunkData = new byte[chunkLength];
                                    serverStream.Read(chunkData, 0, chunkLength);

                                    // 存储到临时文件夹
                                    string tempFilePath = Path.Combine(tempFolderPath, $"chunk_{i}.dat");
                                    File.WriteAllBytes(tempFilePath, chunkData);

                                    // 存储到 cache_chunks 文件夹
                                    string cacheChunkFilePath = Path.Combine(cacheChunksFolderPath, $"{hash}.dat");
                                    File.WriteAllBytes(cacheChunkFilePath, chunkData);
                                }
                                else if (operationCode == 0) // 哈希值
                                {
                                    // 读取哈希值
                                    byte[] hashBytes = new byte[64];
                                    serverStream.Read(hashBytes, 0, hashBytes.Length);
                                    string hash = Encoding.UTF8.GetString(hashBytes);

                                    string cacheChunkFilePath = Path.Combine(cacheChunksFolderPath, $"{hash}.dat");
                                    
                                    // 拷贝到临时文件夹
                                    string tempFilePath = Path.Combine(tempFolderPath, $"chunk_{i}.dat");
                                    File.Copy(cacheChunkFilePath, tempFilePath);
                                    
                                }
                            }

                            // 循环结束后处理临时文件夹中的文件

                            // 将拼接后的文件发送给客户端
                            string tempFile = ReassembleFileIntoTempFile(tempFolderPath);

                            using (FileStream fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                            {
                                fileStream.CopyTo(cacheStream);
                            }


                            DirectoryInfo directory = new DirectoryInfo(tempFolderPath);
                            foreach (FileInfo file in directory.GetFiles())
                            {
                                file.Delete();
                            }
                        }
                    }
                }
            }
        }
    }
}

