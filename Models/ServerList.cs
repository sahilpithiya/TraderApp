namespace TraderApp.Models
{
    public class ServerList
    {
        public int licenseId { get; set; }
        public string companyName { get; set; }
        public string serverDisplayName { get; set; }
        public string primaryDomain { get; set; }
        public string secondaryDomain { get; set; }
        public string socketUrl { get; set; }
    }

    public class ServerListResponse
    {
        public ServerListData data { get; set; }
        public bool isSuccess { get; set; }
    }

    public class ServerListData
    {
        public System.Collections.Generic.List<ServerList> licenseDetail { get; set; }
    }
}