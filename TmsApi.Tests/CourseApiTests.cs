using System.Net;
using System.Net.Http.Json;

namespace TmsApi.Tests;

public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCourses_ReturnsOkAndPagedJson()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/v2.0/courses?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

Console.WriteLine("COURSES RESPONSE:");
Console.WriteLine(json);

Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public async Task CreateCourse_InvalidCode_ReturnsValidationError()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v2.0/courses",
            new
            {
                code = "",
                title = "Intro to TMS Security",
                maxCapacity = 30
            });

        // Assert
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity);
    }

    private sealed class PagedCoursesJson
    {
        public List<CourseRowJson> Items { get; set; } = default!;

        public int TotalCount { get; set; }
    }

    private sealed class CourseRowJson
    {
        public int Id { get; set; }

        public string Code { get; set; } = "";

        public string Title { get; set; } = "";

        public int MaxCapacity { get; set; }

        public int EnrollmentCount { get; set; }
    }
}