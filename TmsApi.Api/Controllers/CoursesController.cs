using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Routing;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CoursesController(ICourseService courseService,ICachedCourseService cachedService, LinkGenerator linkGenerator) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[EndpointSummary("Get a course by ID")]
[EndpointDescription("Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        if (course is null)
        {
            return NotFound();
        }

        var selfLink = linkGenerator.GetPathByName(
        HttpContext,
        nameof(GetCourseById),
        new { id });


    var enrollmentsLink = linkGenerator.GetPathByName(
        HttpContext,
        "ListCourseEnrollments",
        new { courseId = id });


    var links = new List<LinkDto>
    {
        new(
            selfLink!,
            "self",
            "GET"),

        new(
            selfLink!,
            "update",
            "PUT"),

        new(
            selfLink!,
            "delete",
            "DELETE"),

        new(
            enrollmentsLink!,
            "enrollments",
            "GET")
    };


    if (course.EnrollmentCount < course.MaxCapacity)
    {
        links.Add(
            new LinkDto(
                enrollmentsLink!,
                "enroll",
                "POST"));
    }


    var detail = new CourseDetailDto
    {
        Id = course.Id,
        Code = course.Code,
        Title = course.Title,
        MaxCapacity = course.MaxCapacity,
        EnrollmentCount = course.EnrollmentCount,
        Links = links
    };


    return Ok(detail);
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses with pagination")]
    [EndpointDescription("Returns a paginated, optionally filtered list of TMS courses. PageSize is capped at 50.")]    public async Task<IActionResult> GetCourses(
    [FromQuery] PagedRequest request,
    CancellationToken ct)
   {
     var result = await courseService.GetCoursesAsync(request, ct);

     return Ok(result);
   }

     [HttpPost]
    [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists.")]
        public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request,
        CancellationToken ct)
    {
         if (await courseService.CodeExistsAsync(request.Code, ct))
    {
        return Conflict(new ProblemDetails
        {
            Title = "Course code already exists",
            Detail = $"A course with code '{request.Code}' is already registered.",
            Status = StatusCodes.Status409Conflict
        });
    }
        
       var result = await courseService.CreateAsync(request, ct);

await cachedService.InvalidateCourseCacheAsync(ct);

return CreatedAtAction(
    nameof(GetCourseById),
    new { id = result.Id },
    result);
    }
    [HttpDelete("{id:int}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public async Task<IActionResult> DeleteCourse(
    int id,
    CancellationToken ct)
{
    var course = await courseService.GetByIdAsync(id, ct);

    if (course is null)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Course not found",
            detail: $"Course with ID {id} was not found.",
            type: "https://tms.local/errors/course_not_found");
    }

    if (course.EnrollmentCount > 0)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Course deletion rejected",
            detail: $"Course {course.Code} cannot be deleted because active enrollments exist.",
            type: "https://tms.local/errors/course_has_enrollments");
    }

    var deleted = await courseService.DeleteAsync(id, ct);

    if (!deleted)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Course not found",
            detail: $"Course with ID {id} was not found.",
            type: "https://tms.local/errors/course_not_found");
    }

    return NoContent();
}
}