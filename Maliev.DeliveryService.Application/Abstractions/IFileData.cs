namespace Maliev.DeliveryService.Application.Abstractions;

public interface IFileData
{
    Stream OpenReadStream();
    string FileName { get; }
    long Length { get; }
    string ContentType { get; }
}
