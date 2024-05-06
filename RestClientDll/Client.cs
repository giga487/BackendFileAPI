using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;


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
