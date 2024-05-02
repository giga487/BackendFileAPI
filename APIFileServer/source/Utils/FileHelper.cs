using System.Security.Cryptography;

namespace APIFileServer.source.Utils
{
    public class FileHelper
    {
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
            fs.WriteByte(0);
            fs.Close();
        }
    }
}
