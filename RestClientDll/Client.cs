using System;
using System.Collections.Generic;
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
        RestSharp.RestClient _client { get; set; } = null;
        CancellationTokenSource _tokenSc { get; set; } = new CancellationTokenSource();
        public RestClient(Uri host, int maxTimeout = 100000)
        {

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

            if (type == RestRequestType.Get)
            {
                return await _client.GetAsync<T>(request, _tokenSc.Token);
            }

            return await Task.FromResult(default(T));
        }

        public async Task<byte[]> DownloadRequest(string controller, string action, string ids = "")
        {
            string requestString = $"{controller}/{action}/{ids}";

            return await DownloadRequest(requestString);
        }

        public async Task<byte[]> DownloadRequest(string requestString)
        {
            var request = new RestRequest(requestString);

            try
            {
                return await _client.DownloadDataAsync(request, _tokenSc.Token);
            }
            catch
            {
                return default(byte[]);
            }
        }
    }
}
