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

        private static byte[] ReassembleFileFromChunks(string chunksFolderPath)
        {
            MemoryStream reconstructedFile = new MemoryStream();
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
                reconstructedFile.Write(chunkData, 0, chunkData.Length);
            }

            return reconstructedFile.ToArray();
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
                if (command == "LIST_FILES")
                {
                    using (TcpClient serverClient = new TcpClient())
                    {
                        serverClient.Connect(ServerAddress, ServerPort);
                        using (NetworkStream serverStream = serverClient.GetStream())
                        using (StreamReader serverReader = new StreamReader(serverStream, Encoding.UTF8))
                        using (StreamWriter serverWriter = new StreamWriter(serverStream, Encoding.UTF8))
                        {
                            serverWriter.WriteLine("LIST_FILES");
                            serverWriter.Flush();

                            int fileCount = int.Parse(serverReader.ReadLine());
                            cacheWriter.WriteLine(fileCount);
                            cacheWriter.Flush();

                            for (int i = 0; i < fileCount; i++)
                            {
                                string fileName = serverReader.ReadLine();
                                cacheWriter.WriteLine(fileName);
                                cacheWriter.Flush();
                            }
                        }
                    }
                }
                else if (command == "REQUEST_FILE")
                {
                    string fileName = cacheReader.ReadLine();
                    string tempFolderPath = Path.Combine("../../../temp");
                    string cacheChunksFolderPath = Path.Combine("../../../cache_chunks");

                    Directory.CreateDirectory(tempFolderPath);
                    Directory.CreateDirectory(cacheChunksFolderPath);

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

                            string response = serverReader.ReadLine().Trim();
                            if (response == "FILE_FOUND")
                            {
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
                                        byte[] chunkLengthBytes = new byte[4];
                                        serverStream.Read(chunkLengthBytes, 0, chunkLengthBytes.Length);
                                        int chunkLength = BitConverter.ToInt32(chunkLengthBytes, 0);

                                        // 读取哈希值
                                        byte[] hashBytes = new byte[64];
                                        serverStream.Read(hashBytes, 0, hashBytes.Length);
                                        string hash = Encoding.UTF8.GetString(hashBytes);

                                        byte[] chunkData = new byte[chunkLength];
                                        int bytesRead = 0;
                                        int remainingBytes = chunkLength;

                                        while (remainingBytes > 0)
                                        {
                                            int readBytes = serverStream.Read(chunkData, bytesRead, remainingBytes);
                                            bytesRead += readBytes;
                                            remainingBytes -= readBytes;
                                        }

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

                                        if (File.Exists(cacheChunkFilePath))
                                        {
                                            // 拷贝到临时文件夹
                                            string tempFilePath = Path.Combine(tempFolderPath, $"chunk_{i}.dat");
                                            File.Copy(cacheChunkFilePath, tempFilePath);
                                        }
                                        else
                                        {
                                            // 处理找不到哈希值对应的文件的情况（可以向客户端发送错误消息或中断连接）
                                            cacheWriter.WriteLine("ERROR");
                                            cacheWriter.WriteLine("出错：找不到哈希值对应的文件");
                                            cacheWriter.Flush();

                                            // 终止传输
                                            break;
                                        }
                                    }
                                }

                                // 循环结束后处理临时文件夹中的文件
                                byte[] reconstructedFile = ReassembleFileFromChunks(tempFolderPath);

                                DirectoryInfo directory = new DirectoryInfo(tempFolderPath);
                                foreach (FileInfo file in directory.GetFiles())
                                {
                                    file.Delete();
                                }

                                // 将拼接后的文件发送给客户端
                                cacheWriter.WriteLine("FILE_FOUND");
                                cacheWriter.Flush();

                                // 添加一个换行符以便客户端正确读取文件数据
                                cacheWriter.WriteLine();
                                cacheWriter.Flush();


                                using (MemoryStream ms = new MemoryStream(reconstructedFile))
                                {
                                    byte[] buffer = new byte[4096];
                                    int bytesRead;

                                    while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        cacheStream.Write(buffer, 0, bytesRead);
                                        if (bytesRead < buffer.Length) break;
                                    }
                                }

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
    }
}

