using Maliev.DeliveryService.Application.Abstractions;

namespace Maliev.DeliveryService.Tests.Fakes;

public class TestFileData : IFileData
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly long _length;
    private readonly string _contentType;

    public TestFileData(Stream stream, string fileName, long length, string contentType)
    {
        _stream = stream;
        _fileName = fileName;
        _length = length;
        _contentType = contentType;
    }

    public Stream OpenReadStream() => _stream;

    public string FileName => _fileName;

    public long Length => _length;

    public string ContentType => _contentType;
}
