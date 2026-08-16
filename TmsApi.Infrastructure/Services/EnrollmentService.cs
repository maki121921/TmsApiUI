using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger)
    : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct)
    {
        return context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }
     public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Student {StudentId} enrolled in course {CourseId}",
            enrollment.StudentId,
            enrollment.CourseId);

        return (await GetByIdAsync(
            courseId,
            enrollment.Id,
            ct))!;
    }
    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(
    int courseId,
    CancellationToken ct)
{
    return await context.Enrollments
        .AsNoTracking()
        .Where(e => e.CourseId == courseId)
        .Select(e => new EnrollmentResponseDto(
            e.Id,
            e.CourseId,
            e.StudentId,
            e.EnrolledAt))
        .ToListAsync(ct);
}
public async Task<bool> ExistsAsync(
    int studentId,
    string courseCode,
    CancellationToken ct)
{
    return await context.Enrollments
        .AsNoTracking()
        .AnyAsync(
            e => e.StudentId == studentId &&
                 e.Course.Code == courseCode,
            ct);
}

public async Task<Enrollment> AddAsync(
    Enrollment enrollment,
    CancellationToken ct)
{
    context.Enrollments.Add(enrollment);

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Student {StudentId} enrolled in course {CourseId}",
        enrollment.StudentId,
        enrollment.CourseId);

    return enrollment;
}

public async Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct)
{
    return await context.Enrollments
        .AsNoTracking()
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);
}
}