using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class DeliveryNoteIdGeneratorTests : IDisposable
{
    private readonly DeliveryDbContext _context;
    private readonly DeliveryNoteIdGenerator _generator;

    public DeliveryNoteIdGeneratorTests()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseInMemoryDatabase(databaseName: $"IdGenTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new DeliveryDbContext(options);
        _generator = new DeliveryNoteIdGenerator(_context);
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
