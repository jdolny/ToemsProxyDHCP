namespace CloneDeploy_Proxy_Dhcp.ApiCalls
{
    public class APICall : IAPICall
    {
        public ProxyDhcpApi ProxyDhcpApi
        {
            get { return new ProxyDhcpApi(); }
        }
    }
}
