using Maliev.DeliveryService.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Maliev.DeliveryService.Api.Services;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class DeliveryNoteIdGenerator
{
    private readonly DeliveryDbContext _context;

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public DeliveryNoteIdGenerator(DeliveryDbContext context)
    {
        _context = context;
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        var prefix = $"DN-{currentYear}-";

        // Use serializable transaction to prevent race conditions
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            try
            {
                var lastId = await _context.DeliveryNotes
                    .Where(dn => dn.DeliveryNoteId.StartsWith(prefix))
                    .OrderByDescending(dn => dn.DeliveryNoteId)
                    .Select(dn => dn.DeliveryNoteId)
                    .FirstOrDefaultAsync(ct);

                int nextNumber = 1;
                if (lastId != null)
                {
                    var numberPart = lastId.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }

                await transaction.CommitAsync(ct);
                return $"{prefix}{nextNumber:D6}";
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}
