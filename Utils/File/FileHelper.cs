using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Utils.FileHelper
{
    public partial class FileHelper
    {
        public async static Task<byte[]> Compress(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }

            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            {
                return await Compress(fileStream);
            }
        }

        public async static Task<byte[]> Compress(Stream fileStream)
        {
            int length = (int)fileStream.Length;
            byte[] buffer = new byte[length];
            int bytesRead = fileStream.Read(buffer, 0, length);

            using (MemoryStream originalMemoryStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(originalMemoryStream, mode: CompressionMode.Compress))
                {
                    try
                    {
                        compressor.Write(buffer, 0, buffer.Length);
                        //Console.WriteLine($"Original size{fileStream.Length} - {originalMemoryStream.GetBuffer().Length}");

                        return originalMemoryStream.GetBuffer();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        public static void DecompressFileToFile(string compressedFileName, string filename)
        {
            using (FileStream originalFileStream = new FileStream(compressedFileName, FileMode.Open))
            {
                using (FileStream decompressedFileStream = File.Create(filename))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        public static void DecompressToFile(byte[] byteDecompressed, string filename)
        {
            using (MemoryStream originalFileStream = new MemoryStream(byteDecompressed))
            {
                using (FileStream decompressedFileStream = File.Create(filename))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        public bool CheckBetweenCompressedbyte(byte[] compressed, byte[] decompressed)
        {
            string md5 = FileHelper.Md5Result(compressed);
            string md5ToCheck = FileHelper.Md5Result(decompressed);

            if (string.Compare(md5, md5ToCheck) == 0)
                return true;

            return false;
        }

        public async static Task<byte[]> Decompress(byte[] compressedArray)
        {
            byte[] result = null;

            MemoryStream decompressedMS = new MemoryStream();

            using (MemoryStream originalMemoryStream = new MemoryStream(compressedArray))
            {
                using (GZipStream decompressionStream = new GZipStream(originalMemoryStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(decompressedMS);
                    byte[] bytysDecompressed = new byte[decompressedMS.Length];

                    Array.Copy(decompressedMS.GetBuffer(), bytysDecompressed, decompressedMS.Length);

                    return bytysDecompressed;
                }
            }
        }

        public async static Task<string> Md5ResultAsync(string fileName)
        {
            string md5Result = string.Empty;

            if (!File.Exists(fileName))
            {
                return md5Result;
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    md5Result = await Task.Run(() =>
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    });

                }
            }

            return md5Result;
        }

        public async static Task<FileInfo> MakeFile(byte[] bytes, string path, string file, bool overwrite)
        {
            try
            {
                string fileName = Path.Combine(path, file);

                if (overwrite && File.Exists(file))
                {
                    return null;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (bytes is null)
                {
                    return null;
                }

                return await Task.Run(() => 
                {
                    if(CreateFile(fileName, bytes))
                    {
                        return new FileInfo(fileName);
                    }
                    return null;
                });
            }
            catch
            {
                return null;
            }
        }

        public static bool CreateFile(string fileName, byte[] bytes)
        {
            File.WriteAllBytes(fileName, bytes);

            if (File.Exists(fileName))
            {
                return true;
            }

            return false;
        }

        public async static Task MakeFile(Stream fileStream, string path, string filename, bool overwrite)
        {
            string fileName = Path.Combine(path, filename);
            
            if(overwrite && File.Exists(filename))
            {
                return;
            }

            await Task.Run(() =>
            {
                using (FileStream writeStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    int length = (int)fileStream.Length;
                    byte[] buffer = new byte[length];
                    int bytesRead = fileStream.Read(buffer, 0, length);

                    while (bytesRead > 0)
                    {
                        writeStream.Write(buffer, 0, bytesRead);
                        bytesRead = fileStream.Read(buffer, 0, length);
                    }

                    fileStream.Close();
                    writeStream.Close();
                }
            });
        }


        public async static Task<string> Md5ResultAsync(string fileName, string filePath)
        {
            string file = Path.Combine(filePath, fileName);

            return await Md5ResultAsync(file);
        }

        public static string Md5Result(string file)
        {
            string md5Result = string.Empty;

            if (!File.Exists(file))
            {
                return md5Result;
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string RelativePath(string filename, string parentPathToExtract)
        {
            string prova = GetRelativePath(parentPathToExtract, filename);

            return prova;
        }

        public static string GetRelativeParentPath(string basePath, string path)
        {
            return GetRelativePath(basePath, Path.GetDirectoryName(path));
        }

        public static string GetRelativePath(string basePath, string path)
        {
            // normalize paths
            basePath = Path.GetFullPath(basePath);
            path = Path.GetFullPath(path);

            // same path case
            if (basePath == path)
                return string.Empty;

            // path is not contained in basePath case
            if (!path.StartsWith(basePath))
                return string.Empty;

            // extract relative path
            if (basePath[basePath.Length - 1] != Path.DirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }

            return path.Substring(basePath.Length);
        }

        public static string Md5Result(byte[] file)
        {
            string md5Result = string.Empty;

            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(file);
                md5Result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            return md5Result;
        }

        public static void MakeTestFile(int maxMadimension, string path, string fileName = "dummy_file")
        {
            string filePath = Path.Combine(path, fileName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (File.Exists(filePath))
                File.Delete(filePath);

            FileStream fs = new FileStream(filePath, FileMode.CreateNew);
            fs.Seek(maxMadimension, SeekOrigin.Begin);

            fs.WriteByte(1);
            fs.Close();
        }



    }
}
