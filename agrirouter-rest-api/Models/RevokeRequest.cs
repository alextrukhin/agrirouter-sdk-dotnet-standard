namespace agrirouter_rest_api.Models
{
    public class RevokeRequest
    {
        public string AccountId { get; set; }
        public string EndpointIds { get; set; }
        public string ApplicationId { get; set; }
        public string PrivateKey { get; set; }
        public string Environment { get; set; }
    }
}
