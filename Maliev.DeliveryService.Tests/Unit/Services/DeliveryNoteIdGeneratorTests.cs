using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

[Collection("PostgreSqlDatabase")]
public class DeliveryNoteIdGeneratorTests : IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private readonly DeliveryDbContext _context;
    private readonly DeliveryNoteIdGenerator _generator;

    public DeliveryNoteIdGeneratorTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateDbContext();
        _generator = new DeliveryNoteIdGenerator(_context);
    }

    public async Task InitializeAsync()
    {
        // Clean up before each test
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
    }

    [Fact]
    public async Task GenerateNextIdAsync_NoExistingIds_ReturnsFirstId()
    {
        // Act
        var id = await _generator.GenerateNextIdAsync();

        // Assert
        var year = DateTime.UtcNow.Year;
        Assert.Equal($"DN-{year}-000001", id);
    }

    [Fact]
    public async Task GenerateNextIdAsync_WithExistingIds_ReturnsIncrementedId()
    {
        // Arrange
        var year = DateTime.UtcNow.Year;
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = $"DN-{year}-000042",
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow,
            Status = DeliveryStatus.Pending
        });
        await _context.SaveChangesAsync();

        // Act
        var id = await _generator.GenerateNextIdAsync();

        // Assert
        Assert.Equal($"DN-{year}-000043", id);
    }

    [Fact]
    public async Task GenerateNextIdAsync_WithInvalidLastId_ReturnsFirstId()
    {
        // Arrange
        var year = DateTime.UtcNow.Year;
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = $"DN-{year}-INVALID",
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow,
            Status = DeliveryStatus.Pending
        });
        await _context.SaveChangesAsync();

        // Act
        var id = await _generator.GenerateNextIdAsync();

        // Assert
        // nextNumber remains 1 because int.TryParse fails
        Assert.Equal($"DN-{year}-000001", id);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
