using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Agrirouter.Api.Dto.Onboard;
using Agrirouter.Api.Dto.Onboard.Inner;
using Agrirouter.Api.Service.Parameters;
using Agrirouter.Commons;
using Agrirouter.Impl.Service.Common;
using Agrirouter.Impl.Service.Messaging;
using Agrirouter.Request;
using agrirouter_rest_api.Models;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace agrirouter_rest_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SendChunkedMessageController : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SendChunkedMessageResponse>> SendChunked([FromForm] SendChunkedMessageRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "Request is required" });
                }

                if (request.PayloadFile == null || request.PayloadFile.Length == 0)
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "PayloadFile is required" });
                }

                if (string.IsNullOrWhiteSpace(request.TechnicalMessageType))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "TechnicalMessageType is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Recipients))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "Recipients is required" });
                }

                if (string.IsNullOrWhiteSpace(request.SensorAlternateId))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "SensorAlternateId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.CapabilityAlternateId))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "CapabilityAlternateId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.MeasuresUrl))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "MeasuresUrl is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Certificate))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "Certificate is required" });
                }

                if (string.IsNullOrWhiteSpace(request.CertificateSecret))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "CertificateSecret is required" });
                }

                if (string.IsNullOrWhiteSpace(request.CertificateType))
                {
                    return BadRequest(new SendChunkedMessageResponse { Error = "CertificateType is required (P12 or PEM)" });
                }

                byte[] payloadBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.PayloadFile.CopyToAsync(memoryStream);
                    payloadBytes = memoryStream.ToArray();
                }

                var onboardResponse = new OnboardResponse
                {
                    SensorAlternateId = request.SensorAlternateId,
                    CapabilityAlternateId = request.CapabilityAlternateId,
                    ConnectionCriteria = new ConnectionCriteria
                    {
                        Measures = request.MeasuresUrl
                    },
                    Authentication = new Authentication
                    {
                        Type = request.CertificateType,
                        Certificate = request.Certificate,
                        Secret = request.CertificateSecret
                    }
                };

                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(X509CertificateService.GetCertificate(onboardResponse));
                var httpClient = new HttpClient(httpClientHandler);

                var messageHeaderParameters = new MessageHeaderParameters
                {
                    TeamSetContextId = request.TeamSetContextId ?? "",
                    TechnicalMessageType = request.TechnicalMessageType,
                    Mode = ParseMode(request.Mode),
                    Recipients = request.Recipients
                .Split(',')
             .Select(r => r.Trim())
          .Where(r => !string.IsNullOrWhiteSpace(r))
               .ToList()
                };

                if (!string.IsNullOrWhiteSpace(request.FileName))
                {
                    messageHeaderParameters.Metadata = new Metadata
                    {
                        FileName = request.FileName
                    };
                }

                var messagePayloadParameters = new MessagePayloadParameters
                {
                    TypeUrl = string.IsNullOrWhiteSpace(request.TypeUrl) ? "" : request.TypeUrl,
                    Value = ByteString.CopyFrom(payloadBytes)
                };

                var messageParameterTuples = EncodeMessageService.ChunkAndBase64EncodeEachChunk(
      messageHeaderParameters,
                 messagePayloadParameters);

                var applicationMessageIds = new List<string>();
                var sendMessageService = new SendDirectMessageService(new HttpMessagingService(httpClient));

                foreach (var tuple in messageParameterTuples)
                {
                    var encodedMessage = EncodeMessageService.Encode(
              tuple.MessageHeaderParameters,
        tuple.MessagePayloadParameters);

                    var messagingParameters = new MessagingParameters
                    {
                        OnboardResponse = onboardResponse,
                        ApplicationMessageId = tuple.MessageHeaderParameters.ApplicationMessageId,
                        EncodedMessages = new List<string> { encodedMessage }
                    };

                    await sendMessageService.SendAsync(messagingParameters);
                    applicationMessageIds.Add(tuple.MessageHeaderParameters.ApplicationMessageId);
                }

                httpClient.Dispose();

                var chunkContextId = messageParameterTuples.First().MessageHeaderParameters.ChunkInfo?.ContextId ?? "";

                return Ok(new SendChunkedMessageResponse
                {
                    Success = true,
                    TotalChunks = messageParameterTuples.Count,
                    ApplicationMessageIds = applicationMessageIds,
                    ChunkContextId = chunkContextId,
                    TotalSize = payloadBytes.Length
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
            new SendChunkedMessageResponse
            {
                Success = false,
                Error = $"Sending chunked message failed: {ex.Message}"
            });
            }
        }

        private RequestEnvelope.Types.Mode ParseMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return RequestEnvelope.Types.Mode.Direct;
            }

            return mode.ToLowerInvariant() switch
            {
                "direct" => RequestEnvelope.Types.Mode.Direct,
                "publish" => RequestEnvelope.Types.Mode.Publish,
                _ => RequestEnvelope.Types.Mode.Direct
            };
        }
    }
}
