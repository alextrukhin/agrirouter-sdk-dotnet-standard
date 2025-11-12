using System.Collections.Generic;

namespace agrirouter_rest_api.Models
{
 public class SendChunkedMessageResponse
    {
        public bool Success { get; set; }
        public int TotalChunks { get; set; }
        public List<string> ApplicationMessageIds { get; set; }
   public string ChunkContextId { get; set; }
        public long TotalSize { get; set; }
      public string Error { get; set; }
    }
}
