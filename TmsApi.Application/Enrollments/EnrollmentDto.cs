namespace TmsApi.Application.Enrollments;

public sealed record EnrollmentDto(
    int Id,
    int StudentId,
    int CourseId,
    string StudentName,
    string CourseName,
    string Status,
    decimal? Grade,
    DateTime EnrolledAt
);