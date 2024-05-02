using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Utils.FileHelper
{
    public class FileInfoToShare
    {
        public string Filename { get; set; } = string.Empty;
        public string MD5 { get; set; } = string.Empty;
        public long Dim { get; set; } = 0;
        public FileInfo FileInfo { get; private set; } = null;

        public FileInfoToShare(string filename, string mainFolder)
        {
            string relativeName = FileHelper.RelativePath(filename, mainFolder);

            FileInfo = new FileInfo(filename);
            Dim = FileInfo.Length;
            Filename = relativeName;
        }


        public async Task<FileInfoToShare> InitializeAsync()
        {
            MD5 = await FileHelper.Md5ResultAsync(Filename);
            return this;
        }

        public FileInfoToShare Initialize()
        {
            MD5 = FileHelper.Md5Result(FileInfo.FullName);

            return this;
        }
    }

    public class FileList
    {
        public Dictionary<string, FileInfoToShare> FilesDict = new Dictionary<string, FileInfoToShare>();
        public string Folder = string.Empty;
        private string[] Files = null;
        public long TotalFileSize { get; private set; } = 0;
        public FileList(string folderPath)
        {
            Folder = folderPath;
            Files = Directory.GetFiles(Folder, "*.*", SearchOption.AllDirectories);

        }

        public async Task<FileList> AddFilesAsync()
        {
            foreach (var f in Files)
            {
                FileInfo fff = new FileInfo(f);

                string relativeName = FileHelper.RelativePath(fff.FullName, Folder);

                FileInfoToShare toShare = new FileInfoToShare(relativeName, Folder);

                AddFile(relativeName, await toShare.InitializeAsync());
            }

            return this;
        }

        public FileList AddFiles()
        {
            foreach (var f in Files)
            {
                FileInfo fff = new FileInfo(f);

                FileInfoToShare toShare = new FileInfoToShare(fff.FullName, Folder).Initialize();
                AddFile(toShare.Filename, toShare);
            }

            return this;
        }


        public void AddFile(string name, FileInfoToShare f)
        {
            if (!FilesDict.ContainsKey(name) && f != null)
            {
                FilesDict[name] = f;
                TotalFileSize += f.Dim;
            }
        }


    }
}
