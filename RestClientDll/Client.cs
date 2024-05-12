using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Utils.FileHelper;


namespace RestClientDll
{
    public enum RestRequestType
    {
        Get,
        Put
    }

    public class RestClient
    {
        public RestSharp.RestClient Client { get => _client; }
        RestSharp.RestClient _client { get; set; } = null;
        CancellationTokenSource _tokenSc { get; set; } = new CancellationTokenSource();

        public enum Result
        {
            Ok,
            Bad,
            Unknown,
            FileSaveException
        }

        public class DownloadChunksResult
        {
            public List<DownloadResult> Chunks { get; set; } = new List<DownloadResult>();
            public Result Result { get; set; } = Result.Unknown;
            public int Size { get; set; } = 0;
        }

        public class DownloadResult
        {
            public Result Result { get; set; } = Result.Unknown;
            public int Size { get; set; } = 0;
            public string FileName { get; set; } = string.Empty;
            public long Milliseconds { get; set; } = 0;
            public FileInfo FileInfo { get; set; } = null;
            public int Index { get; set; } = 0;
            public DownloadResult(Result result, int index, int size = 0)
            {
                Result = result;
                Size = size;
                Index = index;
            }
        }

        public class ChunkDowloadArgs : EventArgs
        {
            public int ID { get; private set; } = -1;
            public string Name { get; private set; } = string.Empty;
            public int Try { get; private set; } = 0;
            public DownloadResult ResultData { get; set; } = null;

            public ChunkDowloadArgs(int id, DownloadResult result, int downloadtry = 0)
            {
                ID = id;
                Name = result.FileName;

                if(result != null)
                {
                    ResultData = result;
                }
            }
        }

        public EventHandler<ChunkDowloadArgs> FileDownloadResultEV;
        public int MaxTimeout { get; set; } = 10000;
        public RestClient(Uri host, int maxTimeout = 100000)
        {
            MaxTimeout = maxTimeout;
            var options = new RestClientOptions(host)
            {
                ThrowOnAnyError = true,
                MaxTimeout = maxTimeout,
                BaseHost = host.Host
            };

            _client = new RestSharp.RestClient(options);

        }

        public async Task<T> CreateRequest<T>(RestRequestType type, string controller, string action, string ids = "")
        {
            string requestString = $"{controller}/{action}/{ids}";

            return await CreateRequest<T>(type, requestString);
        }

        public async Task<T> CreateRequest<T>(RestRequestType type, string stringRequest)
        {
            var request = new RestRequest(stringRequest);
            request.Timeout = MaxTimeout;

            if (type == RestRequestType.Get)
            {
                return await _client.GetAsync<T>(request, _tokenSc.Token);
            }

            return await Task.FromResult(default(T));
        }

        public async Task<byte[]> DownloadRequest(string controller, string action, string ids = "")
        {
            string requestString = $"{controller}/{action}/{ids}";

            return await DownloadRequestAsync(requestString);
        }

        public string DownloadChunkRequest(string address, string filename, int id)
        {
            //return $"/api/File/DownloadFileByChunks?fileName={filename}&Id={id}";
            return string.Format(address, filename, id);
        }

        public async Task<DownloadResult> DownloadChunk(string apiChunks, string filenameToDownload, int index, string path = "")
        {
            string requestString = DownloadChunkRequest(apiChunks, filenameToDownload, index);
            var request = new RestSharp.RestRequest(requestString, Method.Get);
            var fileData = _client.DownloadData(request);

            Stopwatch st = new Stopwatch();
            FileInfo f = new FileInfo(filenameToDownload);

            st.Start();
            try
            {
                if (fileData != null)
                {
                    string filename = $"{f.Name.Replace(f.Extension, "")}_{index}{f.Extension}";
                    st.Stop();

                    FileInfo made = await FileHelper.MakeFile(fileData, path, filename, true);

                    if(made != null)
                    {
                        return new DownloadResult(Result.Ok, index, size: fileData.Length)
                        {
                            Milliseconds = st.ElapsedMilliseconds,
                            FileName = filename,
                            FileInfo = made,
                            Index = index
                        };
                    }
                }
            }
            catch(Exception ex)
            {
                st.Stop();

                switch (ex.GetType())
                {
                    default:
                        return new DownloadResult(Result.FileSaveException, index)
                        {
                            Milliseconds = st.ElapsedMilliseconds
                        };
                }
            }

            return new DownloadResult(Result.Bad, index);
        }

        private void OnDownloadedFileResult(ChunkDowloadArgs args)
        {
            var delegateF = FileDownloadResultEV;
            if(delegateF != null)
            {
                delegateF.Invoke(this, args);
            }
        }

        public DownloadChunksResult ChunkTest(List<int> originalIndexes, Dictionary<int, RestClientDll.RestClient.DownloadResult> chunksResult)
        {
            DownloadChunksResult finalChunksResult = new DownloadChunksResult();

            foreach (var index in originalIndexes)
            {
                if(chunksResult.TryGetValue(index, out var result))
                {
                    if (result.Result == Result.Ok)
                    {
                        finalChunksResult.Size += result.Size;
                        finalChunksResult.Chunks.Add(result);
                    }

                    finalChunksResult.Result = result.Result;
                }
            }

            return finalChunksResult;
        }

        public async Task<FileInfo> MakeFileFromChunks(DownloadChunksResult chunksResult)
        {

            return await Task.FromResult(default(FileInfo));
        }

        public async Task<DownloadChunksResult> DownloadChunksByIndexes(string apiChunks, List<int> chunksIds, string filenameToDownload, string path = "", int maxTry = 5)
        {
            double percentage = 0;
            //string address = "/api/File/DownloadFileByChunks?fileName={0}&Id={1}";
            Dictionary<int, DownloadResult> ChunksResult = new Dictionary<int, DownloadResult>();

            List<int> chunksIdsCopy = chunksIds.ToList();
            foreach (var t in chunksIdsCopy.ToList())
            {
                var downloadStatus = await DownloadChunk(apiChunks, filenameToDownload, t, path);
                OnDownloadedFileResult(new ChunkDowloadArgs(t, downloadStatus));

                if (downloadStatus.Result == Result.Ok)
                {
                    ChunksResult[t] = downloadStatus;
                    chunksIdsCopy.Remove(t);
                }
            }

            return ChunkTest(chunksIds, ChunksResult);
        }

        public int TimeToTryAgain_MS => 50;
        public async Task<DownloadChunksResult> DownloadChunks(string apiChunks, int chunkNumber, string filenameToDownload, string path = "", int maxTry = 5)
        {
            List<int> chunks = new List<int>();

            for (int i = 0; i < chunkNumber; i++)
            {
                chunks.Add(i);
            }

            return await DownloadChunksByIndexes(apiChunks, chunks, filenameToDownload, path, maxTry);
        }

        public async Task<bool> GetFileByChunks(string apiChunks, int chunkNumber, string oldMd5, string filenameToDownload, string path = "", int maxTry = 5)
        {
            var downloads = await DownloadChunks(apiChunks, chunkNumber, filenameToDownload, path, maxTry);

            if (downloads.Result == Result.Ok)
            {
                Dictionary<int, byte[]> chunks = new Dictionary<int, byte[]>();

                int position = 0;
                string newFileName = Path.Combine(path, filenameToDownload);

                try
                {
                    using (var fileToCreate = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        foreach (var downloadedChunk in downloads.Chunks.OrderBy(t => t.Index))
                        {
                            chunks[downloadedChunk.Index] = new byte[255];
                            //chunks

                            using (var fs = new FileStream(downloadedChunk.FileInfo.FullName, FileMode.Open, FileAccess.Read))
                            {
                                byte[] bytes = new byte[downloadedChunk.FileInfo.Length];
                                var bytesToAddAtPosition = fs.Read(bytes, 0, (int)downloadedChunk.FileInfo.Length);
                                fileToCreate.Write(bytes, 0, downloadedChunk.Size);

                                position += bytesToAddAtPosition;
                                fs.Close();
                            }
                        }
                    }

                    if (FileHelper.Md5Result(newFileName) != oldMd5)
                    {
                        return false;
                    }

                }
                catch
                {

                }
            }

            return true;
        }


        public async Task<byte[]> DownloadRequestAsync(string requestString)
        {
            var request = new RestRequest(requestString);
            request.Timeout = 60;

            try
            {
                return await _client.DownloadDataAsync(request, _tokenSc.Token);
            }
            catch(Exception e)
            {
                return default(byte[]);
            }
        }

        public byte[] DownloadRequest(string requestString)
        {
            var request = new RestRequest(requestString);
            request.Timeout = 60;

            try
            {
                var stream = _client.DownloadStream(request);
                var memoryS = new MemoryStream();
                stream.CopyTo(memoryS, (int)stream.Length);
                return memoryS.GetBuffer();
            }
            catch (Exception e)
            {
                return default(byte[]);
            }
        }
    }
}
