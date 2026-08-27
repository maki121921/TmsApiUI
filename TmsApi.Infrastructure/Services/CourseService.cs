using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;


namespace TmsApi.Infrastructure.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    //Gets a course by it is ID
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
{
    return context.Courses
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .FirstOrDefaultAsync(ct);
}

    public Task<CourseResponseDto?> GetByCodeAsync(
    string code,
    CancellationToken ct)
{
    return context.Courses
        .AsNoTracking()
        .Where(c => c.Code == code)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .FirstOrDefaultAsync(ct);
}
    //Creats a new Course
    public async Task<CourseResponseDto> CreateAsync(
    CreateCourseRequest request,
    CancellationToken ct)
{
    var course = new Course
    {
        Code = request.Code,
        Title = request.Title,
        MaxCapacity = request.MaxCapacity
    };

    context.Courses.Add(course);

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Created course {CourseId} ({Code})",
        course.Id,
        course.Code);

    return (await GetByIdAsync(course.Id, ct))!;
}
public Task<bool> CodeExistsAsync(
    string code,
    CancellationToken ct)
{
    return context.Courses
        .AsNoTracking()
        .AnyAsync(c => c.Code == code, ct);
}
//Even this empty implementation will remove the CS0535 error.Later you'll replace it with the pagination logic.
public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
    PagedRequest request,
    CancellationToken ct)
{
    // Step 1: Start query without tracking
    IQueryable<Course> query = context.Courses.AsNoTracking();
     // Step 2: Search filtering
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
            EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }
 // Step 3: Count BEFORE Skip and Take
    var totalCount = await query.CountAsync(ct);
     // Step 4: Sorting
    query = request.OrderBy switch
    {
        "Code" => request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),

        "MaxCapacity" => request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),

        _ => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };
     // Step 5: Paging + Projection
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);
    // Step 6: Return response
    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };

}
   public async Task<CourseResponseDto?> UpdateAsync(
    int id,
    UpdateCourseRequest request,
    CancellationToken ct)
{
    var course = await context.Courses
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    if (course is null)
        return null;

    course.Title = request.Title;

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Updated course {CourseId}",
        course.Id);

    return await GetByIdAsync(course.Id, ct);
}
public async Task<bool> DeleteAsync(int id, CancellationToken ct)
{
    var course = await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    if (course is null)
        return false;

    if (course.Enrollments.Any())
        throw new InvalidOperationException(
            $"Course {course.Code} cannot be deleted because active enrollments exist.");

    context.Courses.Remove(course);

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Deleted course {CourseId} ({Code})",
        course.Id,
        course.Code);

    return true;
}
}