using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Agrirouter.Api.Env;
using Agrirouter.Api.Exception;
using Agrirouter.Api.Service.Parameters;
using Agrirouter.Impl.Service.Onboard;
using agrirouter_rest_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace agrirouter_rest_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RevokeController : ControllerBase
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<RevokeResponse>> Revoke([FromBody] Models.RevokeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new RevokeResponse { Error = "Request is required" });
                }

                if (string.IsNullOrWhiteSpace(request.AccountId))
                {
                    return BadRequest(new RevokeResponse { Error = "AccountId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.EndpointIds))
                {
                    return BadRequest(new RevokeResponse { Error = "EndpointIds is required" });
                }

                if (string.IsNullOrWhiteSpace(request.ApplicationId))
                {
                    return BadRequest(new RevokeResponse { Error = "ApplicationId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.PrivateKey))
                {
                    return BadRequest(new RevokeResponse { Error = "PrivateKey is required" });
                }

                var environment = ParseEnvironment(request.Environment);

                var revokeParameters = new RevokeParameters
                {
                    AccountId = request.AccountId,
                    EndpointIds = request.EndpointIds
                       .Split(',')
                     .Select(e => e.Trim())
                          .Where(e => !string.IsNullOrWhiteSpace(e))
                      .ToList(),
                    ApplicationId = request.ApplicationId
                };

                var revokeService = new RevokeService(environment, HttpClient);
                await revokeService.RevokeAsync(revokeParameters, request.PrivateKey);

                return Ok(new RevokeResponse
                {
                    Success = true,
                    Message = $"Successfully revoked {revokeParameters.EndpointIds.Count} endpoint(s)"
                });
            }
            catch (RevokeException ex)
            {
                return StatusCode((int)ex.StatusCode,
                new RevokeResponse
                {
                    Success = false,
                    Error = $"Revoke failed: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
               new RevokeResponse
               {
                   Success = false,
                   Error = $"Revoke failed: {ex.Message}"
               });
            }
        }

        private Agrirouter.Api.Env.Environment ParseEnvironment(string environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                return new QualityAssuranceEnvironment();
            }

            return environment.ToLowerInvariant() switch
            {
                "qa" => new QualityAssuranceEnvironment(),
                "production" => new ProductionEnvironment(),
                "ar2qa" => new Ar2QualityAssuranceEnvironment(),
                _ => new QualityAssuranceEnvironment()
            };
        }
    }
}
