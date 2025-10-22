using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Agrirouter.Api.Service.Parameters;
using Agrirouter.Feed.Response;
using Agrirouter.Impl.Service.Common;
using Agrirouter.Request;
using Agrirouter.Response.Payload.Account;
using agrirouter_rest_api.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace agrirouter_rest_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        [HttpPost("encode")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EncodeResponse>> Encode([FromForm] EncodeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new EncodeResponse { Error = "Request is required" });
                }

                if (request.PayloadFile == null || request.PayloadFile.Length == 0)
                {
                    return BadRequest(new EncodeResponse { Error = "PayloadFile is required" });
                }

                if (string.IsNullOrWhiteSpace(request.TechnicalMessageType))
                {
                    return BadRequest(new EncodeResponse { Error = "TechnicalMessageType is required" });
                }

                byte[] payloadBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.PayloadFile.CopyToAsync(memoryStream);
                    payloadBytes = memoryStream.ToArray();
                }

                var messageHeaderParameters = new MessageHeaderParameters
                {
                    ApplicationMessageId = string.IsNullOrWhiteSpace(request.ApplicationMessageId)
                      ? MessageIdService.ApplicationMessageId()
                         : request.ApplicationMessageId,
                    TeamSetContextId = request.TeamSetContextId ?? "",
                    TechnicalMessageType = request.TechnicalMessageType,
                    Mode = ParseMode(request.Mode)
                };

                if (!string.IsNullOrWhiteSpace(request.Recipients))
                {
                    messageHeaderParameters.Recipients = request.Recipients
                        .Split(',')
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                             .ToList();
                }

                var messagePayloadParameters = new MessagePayloadParameters
                {
                    TypeUrl = string.IsNullOrWhiteSpace(request.TypeUrl) ? "" : request.TypeUrl,
                    Value = ByteString.CopyFromUtf8(Convert.ToBase64String(payloadBytes))
                };

                var encodedMessage = EncodeMessageService.Encode(messageHeaderParameters, messagePayloadParameters);

                return Ok(new EncodeResponse
                {
                    EncodedMessage = encodedMessage,
                    ApplicationMessageId = messageHeaderParameters.ApplicationMessageId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                 new EncodeResponse { Error = $"Encoding failed: {ex.Message}" });
            }
        }

        [HttpPost("decode")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<DecodeResponse>> Decode([FromForm] DecodeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new DecodeResponse { Error = "Request is required" });
                }

                if (string.IsNullOrWhiteSpace(request.EncodedMessage))
                {
                    return BadRequest(new DecodeResponse
                    {
                        Error = "EncodedMessage is required"
                    });
                }

                var encodedMessage = request.EncodedMessage.Trim();

                if (!IsValidBase64String(encodedMessage))
                {
                    return BadRequest(new DecodeResponse
                    {
                        Error = $"Invalid base64 string format. Length: {encodedMessage.Length} characters. " +
  "Ensure the entire message is provided without truncation."
                    });
                }

                var decodedMessage = DecodeMessageService.Decode(encodedMessage);

                object decodedPayload = null;
                var payloadTypeUrl = decodedMessage.ResponsePayloadWrapper?.Details?.TypeUrl ?? "";
                var payloadValueRaw = decodedMessage.ResponsePayloadWrapper?.Details?.Value?.ToBase64() ?? "";

                if (decodedMessage.ResponsePayloadWrapper?.Details != null)
                {
                    decodedPayload = DecodePayload(decodedMessage.ResponsePayloadWrapper.Details);
                }

                var response = new DecodeResponse
                {
                    ResponseCode = decodedMessage.ResponseEnvelope.ResponseCode,
                    ApplicationMessageId = decodedMessage.ResponseEnvelope.ApplicationMessageId,
                    ResponseBodyType = decodedMessage.ResponseEnvelope.Type.ToString(),
                    Timestamp = decodedMessage.ResponseEnvelope.Timestamp?.ToString() ?? "",
                    PayloadTypeUrl = payloadTypeUrl,
                    PayloadValueRaw = payloadValueRaw,
                    DecodedPayload = decodedPayload
                };

                return Ok(response);
            }
            catch (FormatException ex)
            {
                return BadRequest(new DecodeResponse
                {
                    Error = $"Invalid base64 format: {ex.Message}. The message may be truncated or corrupted."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new DecodeResponse
                {
                    Error = $"Invalid message format: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
               new DecodeResponse
               {
                   Error = $"Decoding failed: {ex.Message}. " +
                "This may indicate a truncated message or an issue with the protobuf structure."
               });
            }
        }

        private bool IsValidBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            base64 = base64.Trim();

            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private object DecodePayload(Any anyPayload)
        {
            try
            {
                var typeUrl = anyPayload.TypeUrl;

                if (typeUrl.Contains("agrirouter.commons.Messages"))
                {
                    var messages = DecodeMessageService.Decode(anyPayload);
                    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(messages));
                }
                else if (typeUrl.Contains("agrirouter.feed.response.MessageQueryResponse"))
                {
                    var messageQueryResponse = MessageQueryResponse.Parser.ParseFrom(anyPayload.Value);
                    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(messageQueryResponse));
                }
                else if (typeUrl.Contains("agrirouter.feed.response.HeaderQueryResponse"))
                {
                    var headerQueryResponse = HeaderQueryResponse.Parser.ParseFrom(anyPayload.Value);
                    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(headerQueryResponse));
                }
                else if (typeUrl.Contains("agrirouter.response.payload.account.ListEndpointsResponse"))
                {
                    var listEndpointsResponse = ListEndpointsResponse.Parser.ParseFrom(anyPayload.Value);
                    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(listEndpointsResponse));
                }
                else
                {
                    return new
                    {
                        typeUrl = typeUrl,
                        note = "Unknown message type - returning raw base64",
                        valueBase64 = anyPayload.Value?.ToBase64()
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Could not decode payload: {ex.Message}",
                    typeUrl = anyPayload.TypeUrl,
                    valueBase64 = anyPayload.Value?.ToBase64()
                };
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
