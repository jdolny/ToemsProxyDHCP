using System;
using CloneDeploy_Proxy_Dhcp.Config;
using RestSharp;

namespace CloneDeploy_ApiCalls
{
    public class ApiRequest
    {
        private readonly Uri _baseUrl;

        public ApiRequest()
        {
            _baseUrl = new Uri(Settings.ComServerURL);
        }

        public TClass Execute<TClass>(RestRequest request) where TClass : new()
        {
            var client = new RestClient();
            client.BaseUrl = _baseUrl;
            //client.Timeout = 5000;

            var response = client.Execute<TClass>(request);

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response: ";
                //Logger.Log(message + response.ErrorException);
                return default(TClass);
            }
            return response.Data;
        }

        public byte[] ExecuteRaw(RestRequest request)
        {
            var client = new RestClient();
            client.BaseUrl = _baseUrl;

            if (request == null)
            {
                return null;
            }

            var response = client.DownloadData(request);

            if (response == null)
            {
                return null;
            }

            return response;
        }
    }
}