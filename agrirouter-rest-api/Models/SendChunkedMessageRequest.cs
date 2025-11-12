using Microsoft.AspNetCore.Http;

namespace agrirouter_rest_api.Models
{
    public class SendChunkedMessageRequest
    {
        public IFormFile PayloadFile { get; set; }
        public string TechnicalMessageType { get; set; }
        public string Recipients { get; set; }
        public string Mode { get; set; }
        public string TypeUrl { get; set; }
        public string TeamSetContextId { get; set; }
        public string FileName { get; set; }
        public string SensorAlternateId { get; set; }
        public string CapabilityAlternateId { get; set; }
        public string MeasuresUrl { get; set; }
        public string CertificateType { get; set; }
        public string Certificate { get; set; }
        public string CertificateSecret { get; set; }
    }
}
