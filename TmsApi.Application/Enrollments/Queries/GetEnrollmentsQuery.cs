using MediatR;

namespace TmsApi.Application.Enrollments.Queries;

public sealed record GetEnrollmentsQuery
    : IRequest<IReadOnlyList<EnrollmentListDto>>;

public sealed record EnrollmentListDto(
    int Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string Status,
    DateTime EnrolledAt
);