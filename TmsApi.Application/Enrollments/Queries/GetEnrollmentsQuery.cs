using MediatR;
using TmsApi.Application.Enrollments;

namespace TmsApi.Application.Enrollments.Queries;

public sealed record GetEnrollmentsQuery
    : IRequest<IReadOnlyList<EnrollmentDto>>;