using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Tests.Fakes;
using Maliev.DeliveryService.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Unit.Services;

[Collection("PostgreSqlDatabase")]
public sealed class DeliveryNoteServiceAuditReadTests : IAsyncLifetime
{
    private readonly DeliveryDbContext _context;

    public DeliveryNoteServiceAuditReadTests(PostgreSqlTestFixture fixture)
    {
        _context = fixture.CreateDbContext();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_status_audits");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
    }

    [Fact]
    public async Task GetStatusAuditsAsync_ReturnsTransitionsOrderedByChangedAt()
    {
        var deliveryNoteId = "DN-AUDIT-READ";
        var customerId = Guid.NewGuid();
        var firstChange = DateTime.UtcNow.AddMinutes(-5);
        var secondChange = DateTime.UtcNow.AddMinutes(-2);

        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = deliveryNoteId,
            OrderId = "ORD-AUDIT-READ",
            CustomerId = customerId,
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Status = DeliveryStatus.PartiallyDelivered,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedBy = "scanner-user",
            IsDeleted = false
        });
        _context.DeliveryStatusAudits.AddRange(
            new DeliveryStatusAudit
            {
                Id = Guid.NewGuid(),
                DeliveryNoteId = deliveryNoteId,
                PreviousStatus = DeliveryStatus.InTransit,
                NewStatus = DeliveryStatus.PartiallyDelivered,
                ChangedBy = "scanner-user",
                ChangedAt = secondChange
            },
            new DeliveryStatusAudit
            {
                Id = Guid.NewGuid(),
                DeliveryNoteId = deliveryNoteId,
                PreviousStatus = DeliveryStatus.Pending,
                NewStatus = DeliveryStatus.InTransit,
                ChangedBy = "scanner-user",
                ChangedAt = firstChange
            });
        await _context.SaveChangesAsync();

        var service = CreateService(new FakeAuthorizationService());

        var audits = await service.GetStatusAuditsAsync(deliveryNoteId, "scanner-user");

        Assert.Equal(2, audits.Count);
        Assert.Equal("Pending", audits[0].PreviousStatus);
        Assert.Equal("InTransit", audits[0].NewStatus);
        Assert.Equal("InTransit", audits[1].PreviousStatus);
        Assert.Equal("PartiallyDelivered", audits[1].NewStatus);
        Assert.True(audits[0].ChangedAt <= audits[1].ChangedAt);
        Assert.All(audits, audit =>
        {
            Assert.Equal(deliveryNoteId, audit.DeliveryNoteId);
            Assert.Equal("scanner-user", audit.ChangedBy);
        });
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private DeliveryNoteService CreateService(IDeliveryNoteAuthorizationService authorizationService)
    {
        IDistributedCache cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));

        return new DeliveryNoteService(
            _context,
            new DeliveryNoteIdGenerator(_context),
            new FakePublishEndpoint(),
            new FakeOrderServiceClient(),
            cache,
            authorizationService,
            new FakeFileStorageService(),
            new FakeDeliveryPdfRequestPublisher(),
            new TestHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNoteService>.Instance);
    }

    private sealed class FakeAuthorizationService : IDeliveryNoteAuthorizationService
    {
        public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> HasUnrestrictedAccessAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(new List<Guid>());
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
