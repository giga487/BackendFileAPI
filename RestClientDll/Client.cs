using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;


namespace RestClientDll
{
    public class RestClient
    {
        RestSharp.RestClient _client { get; set; } = null;
        public RestClient(Uri host, int maxTimeout = 100000)
        {

            var options = new RestClientOptions(host)
            {
                ThrowOnAnyError = true,
                MaxTimeout = maxTimeout,
                BaseHost = host.Host
            };

            _client = new RestSharp.RestClient(options: options);
        }
    }
}
