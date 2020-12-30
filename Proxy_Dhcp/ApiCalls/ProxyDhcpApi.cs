using CloneDeploy_ApiCalls;
using CloneDeploy_Proxy_Dhcp.Dtos;
using RestSharp;
using System.Text;

namespace CloneDeploy_Proxy_Dhcp.ApiCalls
{
    public class ProxyDhcpApi
    {
        private readonly RestRequest _request;

        public ProxyDhcpApi()
        {
            _request = new RestRequest();
        }

        public ProxyReservationDTO GetProxyReservation(string mac)
        {
            _request.Method = Method.GET;
            _request.Resource = string.Format("ClientImaging/GetProxyReservation/");
            _request.AddParameter("mac", mac);
            return new ApiRequest().Execute<ProxyReservationDTO>(_request);
        }

        public TftpServerDTO GetComputerTftpServers(string mac)
        {
            _request.Method = Method.GET;
            _request.Resource = string.Format("ClientImaging/GetComputerTftpServers/");
            _request.AddParameter("mac", mac);
            return new ApiRequest().Execute<TftpServerDTO>(_request);
        }

        public TftpServerDTO GetAllTftpServers()
        {
            _request.Method = Method.GET;
            _request.Resource = string.Format("ClientImaging/GetAllTftpServers/");
            return new ApiRequest().Execute<TftpServerDTO>(_request);
        }



        public string Test()
        {
            _request.Method = Method.GET;
            _request.Resource = string.Format("ClientImaging/Test/");
            var response = new ApiRequest().ExecuteRaw(_request);
            return Encoding.UTF8.GetString(response, 0, response.Length);
        }
    }
}
