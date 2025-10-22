namespace agrirouter_rest_api.Models
{
    public class DecodeResponse
    {
        public int ResponseCode { get; set; }
        public string ApplicationMessageId { get; set; }
        public string ResponseBodyType { get; set; }
        public string Timestamp { get; set; }
        public string PayloadTypeUrl { get; set; }
        public string PayloadValueRaw { get; set; }
        public object DecodedPayload { get; set; }
        public string Error { get; set; }
    }
}
