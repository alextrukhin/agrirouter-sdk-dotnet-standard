using Microsoft.AspNetCore.Http;

namespace agrirouter_rest_api.Models
{
    public class EncodeRequest
    {
        public string ApplicationMessageId { get; set; }
        public string TeamSetContextId { get; set; }
        public string TechnicalMessageType { get; set; }
        public string TypeUrl { get; set; }
        public string Mode { get; set; }
        public string Recipients { get; set; }
        public IFormFile PayloadFile { get; set; }
    }
}
