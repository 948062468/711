using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Linq;


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
            SplitAllFilesIntoChunks();
        }

        private void SplitAllFilesIntoChunks()
        {
            var files = Directory.GetFiles("../../../all_files");

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                // 创建名为 filename_chunk 的文件夹
                string chunkFolder = Path.Combine("../../../chunks", fileName + "_chunk");

                // 如果文件夹已经存在，删除原有文件夹并创建新的文件夹
                if (Directory.Exists(chunkFolder))
                {
                    Directory.Delete(chunkFolder, true);
                }
                Directory.CreateDirectory(chunkFolder);

                // 对图片文件进行可变大小的切割
                byte[] fileData = File.ReadAllBytes(file);
                List<byte[]> chunks = SplitFileIntoChunks(fileData); // 使用滚动哈希实现

                // 将切割得到的数据块存储在 filename_chunk 文件夹中
                int chunkIndex = 1;
                foreach (var chunk in chunks)
                {
                    string chunkFilePath = Path.Combine(chunkFolder, chunkIndex.ToString() + ".chunk");
                    File.WriteAllBytes(chunkFilePath, chunk);
                    chunkIndex++;
                }
            }
        }

        // 实现此方法以使用滚动哈希将文件分割成可变大小的块
        private static List<byte[]> SplitFileIntoChunks(byte[] fileData)
        {
            List<byte[]> chunks = new List<byte[]>();
            int fixedLength = 3;
            MemoryStream currentChunk = new MemoryStream();

            for (int i = 0; i < fileData.Length - fixedLength + 1; i++)
            {
                byte[] tempData = new byte[fixedLength];
                Array.Copy(fileData, i, tempData, 0, fixedLength);
                byte[] hash = CalculateSHA256(tempData);

                if (BitConverter.ToUInt64(hash, 0) % 2048 == 0)
                {
                    // 当前满足条件，进行切割
                    currentChunk.WriteByte(fileData[i]);
                    chunks.Add(currentChunk.ToArray());
                    currentChunk = new MemoryStream();
                }
                else
                {
                    // 将数据添加到当前块
                    currentChunk.WriteByte(fileData[i]);
                }
            }

            // 将剩余的数据添加到最后一个块
            for (int i = fileData.Length - fixedLength + 1; i < fileData.Length; i++)
            {
                if (i < fileData.Length)
                {
                    currentChunk.WriteByte(fileData[i]);
                }
            }
            chunks.Add(currentChunk.ToArray());

            return chunks;
        }

        private static byte[] CalculateSHA256(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        void WriteToLogFile(string message)
        {
            string logFilePath = "../../../debug_log.txt";
            using (StreamWriter logFile = new StreamWriter(logFilePath, true))
            {
                logFile.WriteLine($"{DateTime.Now}: {message}");
            }
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
            {

                //接收来自cache的请求
                byte command = (byte)stream.ReadByte();
                if (command == 0)
                {
                    var files = Directory.GetFiles("../../../available_files");
                    // 发送文件数量
                    byte[] fileCountBytes = BitConverter.GetBytes(files.Length);
                    stream.Write(fileCountBytes, 0, fileCountBytes.Length);
                    stream.Flush();

                    // 发送文件名
                    foreach (var file in files)
                    {
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(file));
                        byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                        stream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);
                        stream.Flush();
                    }

                }
                //处理chunk
                else if (command == 1)
                {
                    byte[] fileNameLengthBytes = new byte[4];
                    stream.Read(fileNameLengthBytes, 0, 4);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    byte[] fileNameBytes = new byte[fileNameLength];
                    stream.Read(fileNameBytes, 0, fileNameLength);
                    string requestedFile = Encoding.UTF8.GetString(fileNameBytes);

                    string sourcePath = Path.Combine("../../../available_files", requestedFile);
                    if (File.Exists(sourcePath))
                    {
                        // 获取碎片文件夹路径
                        string chunksFolderPath = Path.Combine("../../../chunks", requestedFile + "_chunk");
                        DirectoryInfo chunksDirectory = new DirectoryInfo(chunksFolderPath);

                        // 计算并发送 chunk 数量
                        int chunkCount = chunksDirectory.GetFiles().Length;
                        byte[] chunkCountBytes = BitConverter.GetBytes(chunkCount);
                        stream.Write(chunkCountBytes, 0, chunkCountBytes.Length);
                        stream.Flush();

                        // 读取 cache_hash.txt 中的哈希值
                        string cacheHashPath = "../../../cache_hash.txt";
                        HashSet<string> cacheHashes = new HashSet<string>();

                        if (File.Exists(cacheHashPath))
                        {
                            foreach (var line in File.ReadLines(cacheHashPath))
                            {
                                cacheHashes.Add(line);
                            }
                        }

                        List<FileInfo> sortedChunkFiles = chunksDirectory.GetFiles()
                            .OrderBy(f => int.Parse(Regex.Match(f.Name, @"(\d+)\.chunk").Groups[1].Value))
                            .ToList();

                        // 遍历碎片文件夹
                        foreach (FileInfo chunkFile in sortedChunkFiles)
                        {
                            byte[] chunkData = File.ReadAllBytes(chunkFile.FullName);
                            byte[] hashData = CalculateSHA256(chunkData);
                            string hashString = BitConverter.ToString(hashData).Replace("-", "").ToLower();

                            if (cacheHashes.Contains(hashString))
                            {
                                // 告诉请求者哈希值
                                int operationCode = 0;
                                byte[] operationCodeBytes = BitConverter.GetBytes(operationCode);
                                stream.Write(operationCodeBytes, 0, operationCodeBytes.Length);
                                stream.Flush();

                                byte[] hashBytes = Encoding.UTF8.GetBytes(hashString);
                                stream.Write(hashBytes, 0, hashBytes.Length);
                                stream.Flush();
                            }
                            else
                            {
                                // 发送操作码 1 和 chunk 长度
                                int operationCode = 1;
                                byte[] operationCodeBytes = BitConverter.GetBytes(operationCode);
                                stream.Write(operationCodeBytes, 0, operationCodeBytes.Length);
                                stream.Flush();

                                byte[] chunkLengthBytes = BitConverter.GetBytes(chunkData.Length);
                                stream.Write(chunkLengthBytes, 0, chunkLengthBytes.Length);
                                stream.Flush();

                                // 发送碎片块给请求者
                                byte[] hashBytes = Encoding.UTF8.GetBytes(hashString);
                                stream.Write(hashBytes, 0, hashBytes.Length);
                                stream.Flush();

                                stream.Write(chunkData, 0, chunkData.Length);
                                stream.Flush(); // 确保 chunkData 已经发送


                                // 将新的哈希值记录到 cache_hash.txt 中
                                using (StreamWriter sw = File.AppendText(cacheHashPath))
                                {
                                    sw.WriteLine(hashString);
                                }

                                // 将新的哈希值添加到 HashSet 中
                                cacheHashes.Add(hashString);
                            }
                        }
                        // 发送结束信号
                    }
                    else
                    {
                        //文件未找到
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
                    MessageBox.Show("ÎÄ¼þÒÑ´æÔÚÓÚ available_files ÎÄ¼þ¼ÐÖÐ¡£", "ÌáÊ¾", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("ÇëÏÈ´Ó all_files ÁÐ±íÖÐÑ¡ÔñÒ»¸öÎÄ¼þ¡£", "ÌáÊ¾", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}
