﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Utils.FileHelper
{
    public class FileInfoToShare
    {
        [JsonProperty]
        public string Filename { get; set; } = string.Empty;
        [JsonProperty]
        public string MD5 { get; set; } = string.Empty;
        public long Dim { get; set; } = 0;
        [JsonIgnore]
        public FileInfo FileInfo { get; private set; } = null;
        [JsonIgnore]
        public FileHelper.ChunkFile Chunks { get; private set; } = null;
        [JsonIgnore]
        public bool IsCompressed { get; set; } = false;
        [JsonIgnore]
        public DateTime? ChunkCreationTime { get; set; } = null;
        [JsonIgnore]
        public long? ChunkCreationElapsedTime { get; set; } = null;
        public FileInfoToShare(string filename, string mainFolder)
        {
            string relativeName = FileHelper.RelativePath(filename, mainFolder);

            FileInfo = new FileInfo(filename);
            Dim = FileInfo.Length;
            Filename = relativeName;
        }

        public FileInfoToShare MakeChunksFiles(string chunkFolder, int maxSize, bool compressed)
        {
            //Task.Run(() =>
            //{
            Stopwatch st = new Stopwatch();
            st.Start();
            Chunks = new FileHelper.ChunkFile(FileInfo.FullName, chunkFolder, maxSize, compressed);
            IsCompressed = compressed;

            ChunkCreationTime = DateTime.Now;
            st.Stop();

            ChunkCreationElapsedTime = st.ElapsedMilliseconds;
            //});

            return this;
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

    public class ApiFileInfo
    {
        public string Filename { get; set; } = string.Empty;
        public string MD5 { get; set; } = string.Empty;
        public long Dim { get; set; } = 0;
        public int ChunksNumber { get; set; } = 0;
        public bool IsCompressed { get; set; } = false;
        public ApiFileInfo() { }
        public ApiFileInfo(FileInfoToShare f)
        {
            Filename = f.Filename;
            MD5 = f.MD5;
            Dim = f.Dim;
            IsCompressed = f.IsCompressed;
        }

        public ApiFileInfo(string filename, string mD5, long dim)
        {
            Filename = filename;
            MD5 = mD5;
            Dim = dim;
        }
    }

    public class FileList
    {
        [JsonProperty]
        public Dictionary<string, FileInfoToShare> FilesDict = new Dictionary<string, FileInfoToShare>();
        [JsonIgnore]
        public Dictionary<string, ApiFileInfo> ChunkDictionary = new Dictionary<string, ApiFileInfo>();
        [JsonIgnore]
        public string Folder = string.Empty;
        private string[] Files = null;
        public long TotalFileSize { get; private set; } = 0;
        public FileList(string folderPath)
        {
            Folder = folderPath;
            Files = Directory.GetFiles(Folder, "*.*", SearchOption.AllDirectories);

        }

        public int GetAmountOfFile()
        {
            int amount = 0;

            foreach (var file in ChunkDictionary)
            {
                amount += file.Value.ChunksNumber;
            }

            return amount;
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

        public FileList MakeChunksFiles(string chunkFolder, int maxSize, bool compressed = false)
        {
            foreach (var f in FilesDict)
            {
                f.Value?.MakeChunksFiles(chunkFolder, maxSize, compressed);

                if (ChunkDictionary.TryGetValue(f.Key, out ApiFileInfo value)) //Aggiorno i minimal api 
                {
                    value.ChunksNumber = f.Value.Chunks.ChunksList.Count;
                    value.IsCompressed = compressed;
                }
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

            if (!ChunkDictionary.ContainsKey(name) && f != null)
            {
                ChunkDictionary[name] = new ApiFileInfo(f);
                TotalFileSize += f.Dim;
            }
        }


    }
}
