using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

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
                //处理chunk
                else if (command == "SEND_FILE")
                {
                    string requestedFile = reader.ReadLine();
                    string sourcePath = Path.Combine("../../../available_files", requestedFile);
                    if (File.Exists(sourcePath))
                    {
                        writer.WriteLine("FILE_FOUND");
                        writer.Flush();

                        // 获取碎片文件夹路径
                        string chunksFolderPath = Path.Combine("../../../chunks", requestedFile + "_chunk");
                        DirectoryInfo chunksDirectory = new DirectoryInfo(chunksFolderPath);

                        // 计算并发送 chunk 数量
                        int chunkCount = chunksDirectory.GetFiles().Length;
                        writer.WriteLine(chunkCount);
                        writer.Flush();

                        // 读取 cache_hash.txt 中的哈希值
                        string cacheHashPath = "../../../cache_hash.txt";
                        HashSet<string> cacheHashes = new HashSet<string>();
                        if (File.Exists(cacheHashPath))
                        {
                            using (StreamReader sr = new StreamReader(cacheHashPath))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    cacheHashes.Add(line);
                                }
                            }
                        }

                        // 遍历碎片文件夹
                        foreach (FileInfo chunkFile in chunksDirectory.GetFiles())
                        {
                            byte[] chunkData = File.ReadAllBytes(chunkFile.FullName);
                            byte[] hashData = CalculateSHA256(chunkData);
                            string hashString = BitConverter.ToString(hashData).Replace("-", "").ToLower();

                            if (cacheHashes.Contains(hashString))
                            {
                                // 告诉请求者哈希值
                                writer.WriteLine("0"); // 操作码 0
                                writer.WriteLine(hashString);
                            }
                            else
                            {
                                // 发送操作码 1 和 chunk 长度
                                writer.WriteLine("1"); // 操作码 1
                                writer.WriteLine(chunkData.Length);

                                // 发送碎片块给请求者
                                writer.WriteLine(hashString);
                                stream.Write(chunkData, 0, chunkData.Length);

                                // 将新的哈希值记录到 cache_hash.txt 中
                                using (StreamWriter sw = File.AppendText(cacheHashPath))
                                {
                                    sw.WriteLine(hashString);
                                }

                                // 将新的哈希值添加到 HashSet 中
                                cacheHashes.Add(hashString);
                            }
                            writer.Flush();
                        }

                        // 发送结束信号
                        //writer.WriteLine("END_OF_FILE");
                        //writer.Flush();
                    }
                    else
                    {
                        writer.WriteLine("FILE_NOT_FOUND");
                        writer.Flush();
                    }
                }


                /*原本传输整个文件
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
                */
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
