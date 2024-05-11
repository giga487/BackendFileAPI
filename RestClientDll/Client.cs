using System;
using System.Collections.Generic;
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

        public class DownloadResult
        {
            public Result Result { get; private set; } = Result.Unknown;
            public int Size { get; private set; } = 0;

            public DownloadResult(Result result, int size = 0)
            {
                Result = result;
                Size = size;
            }
        }

        public class ChunkDowloadArgs : EventArgs
        {
            public Result Result { get; set; } = Result.Unknown;
            public int ID { get; private set; } = -1;
            public string Name { get; private set; } = string.Empty;
            public int Try { get; private set; } = 0;
            public int Size { get; private set; } = 0;
            public ChunkDowloadArgs(int id, string filename, Result result, int size, int downloadtry = 0)
            {
                ID = id;
                Name = filename;
                Result = result;
            }

            public ChunkDowloadArgs(int id, string filename, DownloadResult result, int downloadtry = 0)
            {
                ID = id;
                Name = filename;

                if(result != null)
                {
                    Result = result.Result;
                    Size = result.Size;
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
            double percentage = 0;

            string address = apiChunks;

            string requestString = DownloadChunkRequest(address, filenameToDownload, index);
            var request = new RestSharp.RestRequest(requestString, Method.Get);
            var fileData = _client.DownloadData(request);

            FileInfo f = new FileInfo(filenameToDownload);

            DownloadResult result = null;

            try
            {
                if (fileData != null)
                {
                    string filename = $"{f.Name.Replace(f.Extension, "")}_{index}{f.Extension}";
                    if (await FileHelper.MakeFile(fileData, path, filename, true))
                    {
                        return new DownloadResult(Result.Ok, fileData.Length);
                    }
                }
            }
            catch(Exception ex)
            {
                switch(ex.GetType())
                {
                    default:
                        return new DownloadResult(Result.FileSaveException);
                }
            }

            return new DownloadResult(Result.Bad);
        }

        private void OnDownloadedFileResult(ChunkDowloadArgs args)
        {
            var delegateF = FileDownloadResultEV;
            if(delegateF != null)
            {
                delegateF.Invoke(this, args);
            }
        }

        public Result ChunkTest(List<int> originalIndexes, Dictionary<int, RestClientDll.RestClient.Result> chunksResult)
        {
            Result finalChunksResult = Result.Ok;

            foreach (var index in originalIndexes)
            {
                if(chunksResult.TryGetValue(index, out var result))
                {
                    if (result != Result.Ok)
                    {
                        finalChunksResult = result;
                    }
                }
                else
                {
                    finalChunksResult = Result.Bad;
                }
            }

            return finalChunksResult;
        }

        public async Task<Result> DownloadChunksByIndexes(string apiChunks, List<int> chunksIds, string filenameToDownload, string path = "", int maxTry = 5)
        {
            double percentage = 0;
            //string address = "/api/File/DownloadFileByChunks?fileName={0}&Id={1}";
            string address = apiChunks;
            Dictionary<int, RestClientDll.RestClient.Result> ChunksResult = new Dictionary<int, Result>();

            List<int> chunksIdsCopy = chunksIds.ToList();
            foreach (var t in chunksIdsCopy.ToList())
            {
                var downloadStatus = await DownloadChunk(address, filenameToDownload, t, path);
                OnDownloadedFileResult(new ChunkDowloadArgs(t, filenameToDownload, downloadStatus));

                if (downloadStatus.Result == Result.Ok)
                {
                    ChunksResult[t] = downloadStatus.Result;
                    chunksIdsCopy.Remove(t);
                }
            }

            return ChunkTest(chunksIds, ChunksResult);
        }

        public int TimeToTryAgain_MS => 50;
        public async void DownloadChunks(string apiChunks, int chunkNumber, string filenameToDownload, string path = "", int maxTry = 5)
        {
            double percentage = 0;
            //string address = "/api/File/DownloadFileByChunks?fileName={0}&Id={1}";
            string address = apiChunks;
            for (int i = 0; i < chunkNumber; i++)
            {
                percentage = (double)i / chunkNumber * 100;

                for (int downloadTry = 0; downloadTry < maxTry; downloadTry++)
                {
                    var downloadStatus = await DownloadChunk(address, filenameToDownload, i);
                    OnDownloadedFileResult(new ChunkDowloadArgs(i, filenameToDownload, downloadStatus, downloadTry));

                    if (downloadStatus.Result == Result.Ok)
                        break;
                    else
                    { 
                        await Task.Delay(TimeToTryAgain_MS);
                        continue;
                    }
                }
            }
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
