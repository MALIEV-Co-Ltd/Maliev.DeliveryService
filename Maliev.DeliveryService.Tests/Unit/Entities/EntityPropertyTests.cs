using Maliev.DeliveryService.Domain.Entities;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Entities;

public class EntityPropertyTests
{
    [Fact]
    public void Address_Properties_CanBeSetAndGet()
    {
        // Arrange
        var address = new Address();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        address.Id = id;
        address.CompanyName = "Company";
        address.ContactName = "Contact";
        address.AddressLine1 = "Line 1";
        address.AddressLine2 = "Line 2";
        address.City = "City";
        address.StateProvince = "State";
        address.PostalCode = "12345";
        address.Country = "Country";
        address.PhoneNumber = "123456";
        address.EmailAddress = "test@example.com";
        address.CreatedAt = now;
        address.CreatedBy = "user";
        address.UpdatedAt = now;
        address.UpdatedBy = "user";

        // Assert
        Assert.Equal(id, address.Id);
        Assert.Equal("Company", address.CompanyName);
        Assert.Equal("Contact", address.ContactName);
        Assert.Equal("Line 1", address.AddressLine1);
        Assert.Equal("Line 2", address.AddressLine2);
        Assert.Equal("City", address.City);
        Assert.Equal("State", address.StateProvince);
        Assert.Equal("12345", address.PostalCode);
        Assert.Equal("Country", address.Country);
        Assert.Equal("123456", address.PhoneNumber);
        Assert.Equal("test@example.com", address.EmailAddress);
        Assert.Equal(now, address.CreatedAt);
        Assert.Equal("user", address.CreatedBy);
        Assert.Equal(now, address.UpdatedAt);
        Assert.Equal("user", address.UpdatedBy);
    }

    [Fact]
    public void DeliveryNoteFile_Properties_CanBeSetAndGet()
    {
        // Arrange
        var file = new DeliveryNoteFile();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        file.Id = id;
        file.DeliveryNoteId = "DN-1";
        file.FileName = "test.txt";
        file.StorageUrl = "gs://bucket/test.txt";
        file.ContentType = "text/plain";
        file.FileSize = 1024;
        file.FileType = FileType.Other;
        file.Description = "Desc";
        file.UploadedAt = now;
        file.UploadedBy = "user";
        file.IsDeleted = true;
        file.DeletedAt = now;

        // Assert
        Assert.Equal(id, file.Id);
        Assert.Equal("DN-1", file.DeliveryNoteId);
        Assert.Equal("test.txt", file.FileName);
        Assert.Equal("gs://bucket/test.txt", file.StorageUrl);
        Assert.Equal("text/plain", file.ContentType);
        Assert.Equal(1024, file.FileSize);
        Assert.Equal(FileType.Other, file.FileType);
        Assert.Equal("Desc", file.Description);
        Assert.Equal(now, file.UploadedAt);
        Assert.Equal("user", file.UploadedBy);
        Assert.True(file.IsDeleted);
        Assert.Equal(now, file.DeletedAt);
    }
}
