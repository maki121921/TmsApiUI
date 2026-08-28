
namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Title { get; set; }

    public int MaxCapacity { get; set; }

    // Identity user ID of the instructor responsible
    // for this course.
    public string? LeadInstructorId { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();

    public ICollection<Assessment> Assessments { get; set; }
        = new List<Assessment>();

    public ICollection<Certificate> Certificates { get; set; }
        = new List<Certificate>();
}
