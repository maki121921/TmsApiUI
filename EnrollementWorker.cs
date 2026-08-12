
/*using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // Create a new scope
        using var scope = scopeFactory.CreateScope();

        // Resolve the scoped service from the scope
        var svc = scope.ServiceProvider
                       .GetRequiredService<IEnrollmentService>();

        // Use the service
        Console.WriteLine("Processing enrollment batch...");

        // When the method exits, the scope is disposed automatically
    }
}*/
