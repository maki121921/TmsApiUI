using TmsApi.Api;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Data;
using TmsApi.Infrastructure.Services;
using TmsApi.Application.Interfaces;
using TmsApi.Application.DTOs;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Domain.Entities;
using TmsApi.Api.Filters;
using TmsApi.Api.Middlewares;
using MediatR;
using FluentValidation;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Asp.Versioning;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Register TmsDbContext scoped for incoming HTTP requests
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information)
.EnableSensitiveDataLogging());
            


builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});
//API Versioning
builder.Services.AddOpenApi("v1", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v2";
});
builder.Services.AddApiVersioning(options =>
{
options.DefaultApiVersion = new ApiVersion(1, 0);
options.AssumeDefaultVersionWhenUnspecified = true;
options.ReportApiVersions = true;
options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
options.GroupNameFormat = "'v'VVV";
options.SubstituteApiVersionInUrl = true;
});
// update your scalar config


//builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services
    .AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
//builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();


builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions,
    TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();
//M7
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(
    typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST — it must wrap ValidationBehavior
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(LoggingBehavior<,>));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

            return tier switch
            {
                ApiKeyTier.Paid =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"paid:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 200,
                            TokensPerPeriod = 100,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),

                ApiKeyTier.Free =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"free:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 30,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),

                _ =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"anon:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,
                            TokensPerPeriod = 5,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        })
            };
            
        });
   options.AddConcurrencyLimiter("transcripts", opt =>
{
    opt.PermitLimit = 5;
    opt.QueueLimit = 20;
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
});
options.AddTokenBucketLimiter("search", opt =>
{
    opt.TokenLimit = 10;
    opt.TokensPerPeriod = 5;
    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    opt.QueueLimit = 2;
    opt.AutoReplenishment = true;
});

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfterValue))
        {
            retryAfter = ((int)retryAfterValue.TotalSeconds).ToString();
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail = $"Too many requests. Retry after {retryAfter} seconds.",
                Status = StatusCodes.Status429TooManyRequests,
                Type = "https://tms.local/errors/rate_limit_exceeded"
            },
            ct);
    };
});

builder.Services.AddCors(options =>
{
options.AddPolicy("AllowAngular", policy =>
policy.WithOrigins("http://localhost:4200")
.AllowAnyHeader()
.AllowAnyMethod());
});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient)
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
}

app.UseExceptionHandler();
app.UseStatusCodePages();



app.UseRouting();
app.UseRateLimiter();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<V1DeprecationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();

app.MapGet("/", () => "TMS Running");

/*app.MapGet("/test/enrollment/{id}", async (string id, IEnrollmentService service) =>
{
    var result = await service.GetByIdAsync(id);

    return result is null
        ? Results.NotFound()
        : Results.Ok(result);
});*/

/*app.MapGet("/api/enrollments/worker-smoke", async (EnrollmentWorker worker) =>
{
    worker.ProcessBatch(); ;
    return Results.Ok("processed");
});*/

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();
// Test endpoint for ProblemDe
// tails
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing");
});
app.MapGet("/environment", (IHostEnvironment env) =>
{
    return Results.Ok(env.EnvironmentName);
});
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();
    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() {RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true},
            new() {RegistrationNumber = "TMS-2026-0001", Name = "Bob Jones", GPA = 2.9m, IsActive = true},
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "DianaPrince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "EvanWright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);
        var courses = new List<Course>
    {
       new() { Code = "CSE-101", Title = "Introduction to ComputerScience", MaxCapacity = 30 },
       new() { Code = "CSE-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
       new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity =40 }
     };
     context.Courses.AddRange(courses);
     context.SaveChanges();
     var enrollments = new List<Enrollment>
{
     new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
     new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
     new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
     new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
};
     context.Enrollments.AddRange(enrollments);
     context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<TmsDbContext>();

    await DataSeeder.SeedAsync(context);
}

app.Run();
