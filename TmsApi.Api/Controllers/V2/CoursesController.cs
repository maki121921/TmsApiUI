using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(
    ICachedCourseService cachedCourseService, ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var request = new PagedRequest
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        var result = await cachedCourseService.GetCoursesAsync(
            request,
            ct);

        var hasNext = result.HasNext;
        var hasPrevious = result.HasPrevious;

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages,
                hasNext,
                hasPrevious
            },
            links = new
            {
                self = $"/api/v2/courses?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}"
                    : null,
                prev = hasPrevious
                    ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}"
                    : null,
                enroll = "/api/v2/enrollments"
            }
        });
    }

   [HttpGet("search")]
[EnableRateLimiting("search")]
public async Task<IActionResult> SearchCourses(
    [FromQuery] string? term,
    CancellationToken ct)
{
    var request = new PagedRequest
    {
        Page = 1,
        PageSize = 20,
        Search = term
    };

    var result = await cachedCourseService.GetCoursesAsync(request, ct);

    return Ok(new
    {
        data = result.Items,
        meta = new
        {
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages,
            hasNext = result.HasNext,
            hasPrevious = result.HasPrevious
        }
    });
}
   [HttpPut("{id:int}")]
public async Task<IActionResult> UpdateCourse(
    int id,
    [FromBody] UpdateCourseRequest request,
    CancellationToken ct)
{
    var result = await courseService.UpdateAsync(id, request, ct);

    if (result is null)
        return NotFound();

    await cachedCourseService.InvalidateCourseCacheAsync(ct);

    return Ok(result);
} 
    
}