using Maliev.DeliveryService.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Maliev.DeliveryService.Api.Adapters;

/// <summary>
/// Adapter for converting IFormFile to IFileData.
/// </summary>
public class FormFileAdapter : IFileData
{
    private readonly IFormFile _formFile;

    /// <summary>
    /// Initializes a new instance of FormFileAdapter.
    /// </summary>
    public FormFileAdapter(IFormFile formFile)
    {
        _formFile = formFile;
    }

    /// <inheritdoc />
    public Stream OpenReadStream() => _formFile.OpenReadStream();

    /// <inheritdoc />
    public string FileName => _formFile.FileName;

    /// <inheritdoc />
    public long Length => _formFile.Length;

    /// <inheritdoc />
    public string ContentType => _formFile.ContentType;
}
