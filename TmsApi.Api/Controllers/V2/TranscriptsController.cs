using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController(
    Channel<TranscriptRequest> channel,
    ITranscriptStatusStore statusStore) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        TranscriptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // Check idempotency first.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing =
                await statusStore.GetReportIdForIdempotencyKeyAsync(
                    idempotencyKey,
                    ct);

            if (existing is not null)
            {
                var existingStatus =
                    await statusStore.GetAsync(existing, ct);

                if (existingStatus is not null)
                {
                    Response.Headers.RetryAfter = "5";

                    return Accepted(
                        Url.Action(
                            nameof(GetStatus),
                            new { id = existing }),
                        existingStatus);
                }
            }
        }

        // Create a new report.
        var reportId = Guid.NewGuid()
            .ToString("N")[..12];

        var status = await statusStore.CreateAsync(
            reportId,
            request.StudentId,
            ct);

        // Remember the idempotency key.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await statusStore.LinkIdempotencyKeyAsync(
                idempotencyKey,
                reportId,
                ct);
        }

        // Queue the transcript for the BackgroundService.
        await channel.Writer.WriteAsync(
            request.WithReportId(reportId),
            ct);

        Response.Headers.RetryAfter = "5";

        return Accepted(
            Url.Action(
                nameof(GetStatus),
                new { id = reportId }),
            status);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(
        string id,
        CancellationToken ct)
    {
        var status = await statusStore.GetAsync(id, ct);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }
}