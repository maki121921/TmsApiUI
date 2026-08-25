using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public sealed class GetEnrollmentsQueryHandler(
    IEnrollmentService enrollmentService)
    : IRequestHandler<GetEnrollmentsQuery, IReadOnlyList<EnrollmentListDto>>
{
    public async Task<IReadOnlyList<EnrollmentListDto>> Handle(
        GetEnrollmentsQuery request,
        CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);

        return enrollments
            .Select(e => new EnrollmentListDto(
                e.Id,
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Title,
                e.Grade.HasValue
                    ? e.Grade.Value.ToString()
                    : "Pending",
                e.EnrolledAt
            ))
            .ToList();
    }
}