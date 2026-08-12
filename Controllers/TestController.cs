using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TmsApi.Data;
using Microsoft.EntityFrameworkCore;
namespace TmsApi.Controllers;
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
   private readonly TmsDbContext _context;
   public TestController(TmsDbContext context)
   {
      _context = context;
   }
[HttpGet("deferred")]
public IActionResult TestDeferred()
{
   Console.WriteLine("\n>>> STEP 1: Building the query object (nodatabase contact)...");
   var query = _context.Students.Where(s => s.GPA >= 3.0m);
   Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
   var orderedQuery = query.OrderBy(s => s.Name);
   Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
   var results = orderedQuery.ToList();
   Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
   return Ok(results);
}
private static bool IsHonorRoll(decimal gpa)
{
return gpa >= 3.5m;
}
[HttpGet("translation-fail")]
public IActionResult TestTranslationFail()
{
Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
try
{
var students = _context.Students
.Where(s => IsHonorRoll(s.GPA)) // EF Core does not know how to map this method to SQL
.ToList();
return Ok(students);
}
catch (Exception ex)
{
Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
return BadRequest(new { Message = ex.Message });
}
}
[HttpGet("students")]
public async Task<IActionResult> GetStudents(
    int page = 1,
    CancellationToken cancellationToken = default)
{
    int pageSize = 20;

    var students = await _context.Students
        .OrderBy(s => s.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return Ok(students);
}
[HttpGet("top-courses")]
public async Task<IActionResult> GetTopCourses(
    CancellationToken cancellationToken)
{
    var result = await _context.Enrollments
        .GroupBy(e => e.Course.Title)
        .Select(g => new
        {
            CourseTitle = g.Key,
            EnrollmentCount = g.Count()
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .Take(5)
        .ToListAsync(cancellationToken);

    return Ok(result);
}
[HttpGet("nplus1")]
    public async Task<IActionResult> NPlusOne()
    {
        var students = await _context.Students
            .AsNoTracking()
            .ToListAsync();

        foreach (var s in students)
        {
            var count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id);

            Console.WriteLine($"{s.Name}: {count} enrollments");
        }

        return Ok();
    }
    [HttpGet("shaped-query")]
    public async Task<IActionResult> ShapedQuery()
    {
        var report = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync();

        return Ok(report);
    }
    [HttpPost("archive-enrollments")]
public async Task<IActionResult> ArchiveEnrollments()
{
    var cutoff = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    var affectedRows = await _context.Enrollments
        .Where(e => e.EnrolledAt < cutoff)
        .ExecuteUpdateAsync(s => s
            .SetProperty(e => e.IsArchived, true));

    return Ok(new
    {
        Message = "Archive completed.",
        RowsUpdated = affectedRows
    });
}
[HttpGet("students-normal")]
public async Task<IActionResult> GetStudentsNormal()
{
    var students = await _context.Students
        .ToListAsync();

    return Ok(students);
}
[HttpGet("students-admin")]
public async Task<IActionResult> GetStudentsAdmin()
{
    var students = await _context.Students
        .IgnoreQueryFilters()
        .ToListAsync();

    return Ok(students);
}
}