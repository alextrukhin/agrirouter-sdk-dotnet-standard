namespace agrirouter_rest_api.Models
{
    public class EncodeResponse
    {
        public string EncodedMessage { get; set; }
        public string ApplicationMessageId { get; set; }
        public string PayloadBase64 { get; set; }
        public string Error { get; set; }
    }
}
