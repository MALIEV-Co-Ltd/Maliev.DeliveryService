namespace Maliev.DeliveryService.Tests.Security;

public class ConfigurationSecurityTests
{
    [Fact]
    public void AppSettings_DoesNotContainRabbitMqDefaultCredentials()
    {
        var appsettings = File.ReadAllText(FindRepoFile("Maliev.DeliveryService.Api", "appsettings.json"));

        Assert.DoesNotContain("\"Password\": \"guest\"", appsettings);
        Assert.DoesNotContain("\"Username\": \"guest\"", appsettings);
    }

    [Fact]
    public void DesignTimeFactory_DoesNotContainHardcodedDatabasePassword()
    {
        var factorySource = File.ReadAllText(FindRepoFile(
            "Maliev.DeliveryService.Infrastructure",
            "Persistence",
            "DeliveryDbContextFactory.cs"));

        Assert.DoesNotContain("Password=postgres", factorySource);
        Assert.Contains("ConnectionStrings__DeliveryDbContext", factorySource);
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. relativeParts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {Path.Combine(relativeParts)}.");
    }
}
