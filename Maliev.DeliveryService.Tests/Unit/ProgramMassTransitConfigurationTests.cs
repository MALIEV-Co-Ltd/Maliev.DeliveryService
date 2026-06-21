namespace Maliev.DeliveryService.Tests.Unit;

public sealed class ProgramMassTransitConfigurationTests
{
    [Fact]
    public void Program_RegistersPdfGenerationConsumersOnDeliverySpecificEndpoints()
    {
        var programSource = File.ReadAllText(FindProgramFile());

        Assert.Contains(
            "x.AddConsumer<PdfGenerationCompletedEventConsumer>()",
            programSource,
            StringComparison.Ordinal);
        Assert.Contains(
            ".Endpoint(endpoint => endpoint.Name = \"delivery-pdf-generation-completed\")",
            programSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "x.AddConsumer<PdfGenerationFailedEventConsumer>()",
            programSource,
            StringComparison.Ordinal);
        Assert.Contains(
            ".Endpoint(endpoint => endpoint.Name = \"delivery-pdf-generation-failed\")",
            programSource,
            StringComparison.Ordinal);
    }

    private static string FindProgramFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Maliev.DeliveryService.Api",
                "Program.cs");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Maliev.DeliveryService.Api/Program.cs.");
    }
}
