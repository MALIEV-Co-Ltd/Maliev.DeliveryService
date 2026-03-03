using Maliev.DeliveryService.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Maliev.DeliveryService.Api.Adapters;

public class FormFileAdapter : IFileData
{
    private readonly IFormFile _formFile;

    public FormFileAdapter(IFormFile formFile)
    {
        _formFile = formFile;
    }

    public Stream OpenReadStream() => _formFile.OpenReadStream();

    public string FileName => _formFile.FileName;

    public long Length => _formFile.Length;

    public string ContentType => _formFile.ContentType;
}
