using Maliev.DeliveryService.Api.Controllers;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Controllers;

public class DeliveryNotesControllerTests
{
    private readonly Mock<IDeliveryNoteService> _mockService;
    private readonly Mock<IDeliveryNoteAuthorizationService> _mockAuthorizationService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<DeliveryNotesController>> _mockLogger;
    private readonly DeliveryNotesController _controller;

    public DeliveryNotesControllerTests()
    {
        _mockService = new Mock<IDeliveryNoteService>();
        _mockAuthorizationService = new Mock<IDeliveryNoteAuthorizationService>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<DeliveryNotesController>>();
        _mockAuthorizationService
            .Setup(x => x.HasUnrestrictedAccessAsync("test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _controller = new DeliveryNotesController(
            _mockService.Object,
            _mockAuthorizationService.Object,
            _mockPublishEndpoint.Object,
            _mockLogger.Object);

        // Setup User identity
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "test-user")
        }, "Test"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetDeliveryNote_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryNoteResponse?)null);

        // Act
        var result = await _controller.GetDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryStatusRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not found"));

        // Act
        var result = await _controller.UpdateDeliveryStatus("DN-1", new UpdateDeliveryStatusRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_InvalidTransition_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryStatusRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid transition"));

        // Act
        var result = await _controller.UpdateDeliveryStatus("DN-1", new UpdateDeliveryStatusRequest { NewStatus = "Delivered" }, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteDeliveryNote_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not found"));

        // Act
        var result = await _controller.DeleteDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GeneratePdf_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryNoteResponse?)null);

        // Act
        var result = await _controller.GeneratePdf("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateDeliveryNote_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest { OrderId = "ORD-1" };
        var response = new DeliveryNoteResponse { DeliveryNoteId = "DN-1" };
        _mockService.Setup(x => x.CreateAsync(request, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateDeliveryNote(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(response, createdResult.Value);
    }

    [Fact]
    public async Task SearchDeliveryNotes_ReturnsOk()
    {
        // Arrange
        var filter = new DeliveryNoteFilterRequest();
        var response = new PaginatedResponse<DeliveryNoteSummaryDto>();
        _mockService.Setup(x => x.SearchAsync(filter, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SearchDeliveryNotes(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetDeliveryStatusAudits_ReturnsOk()
    {
        var response = new List<DeliveryStatusAuditResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DeliveryNoteId = "DN-1",
                PreviousStatus = "Pending",
                NewStatus = "InTransit",
                ChangedBy = "test-user",
                ChangedAt = DateTime.UtcNow
            }
        };
        _mockService.Setup(x => x.GetStatusAuditsAsync("DN-1", "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetDeliveryStatusAudits("DN-1", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetDeliveryNote_Found_ReturnsOk()
    {
        // Arrange
        var response = new DeliveryNoteResponse { DeliveryNoteId = "DN-1", CustomerId = Guid.NewGuid() };
        _mockService.Setup(x => x.GetByIdAsync("DN-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetDeliveryNote_CustomerScopeDenied_ReturnsForbid()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _mockService.Setup(x => x.GetByIdAsync("DN-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteResponse { DeliveryNoteId = "DN-1", CustomerId = customerId });
        _mockAuthorizationService
            .Setup(x => x.HasUnrestrictedAccessAsync("test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockAuthorizationService
            .Setup(x => x.CanAccessCustomerAsync("test-user", customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task DownloadFile_Found_ReturnsFileContent()
    {
        var fileId = Guid.NewGuid();
        var content = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 77, 83 };
        _mockService
            .Setup(x => x.DownloadFileAsync("DN-1", fileId, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteFileContentResponse
            {
                FileId = fileId,
                OriginalFileName = "proof.png",
                ContentType = "image/png",
                Content = content
            });

        var result = await _controller.DownloadFile("DN-1", fileId, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal("proof.png", fileResult.FileDownloadName);
        Assert.Equal(content, fileResult.FileContents);
    }

    [Fact]
    public async Task UpdateDeliveryNote_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateDeliveryNoteRequest();
        var response = new DeliveryNoteResponse();
        _mockService.Setup(x => x.UpdateAsync("DN-1", request, "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateDeliveryNote("DN-1", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task DeleteDeliveryNote_Success_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(x => x.SoftDeleteAsync("DN-1", "test-user", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetFiles_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(x => x.GetByIdAsync("DN-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteResponse { DeliveryNoteId = "DN-1", CustomerId = Guid.NewGuid() });
        var response = new List<DeliveryNoteFileResponse>();
        _mockService.Setup(x => x.GetFilesAsync("DN-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetFiles("DN-1", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GeneratePdf_Success_ReturnsAccepted()
    {
        // Arrange
        var response = new DeliveryNoteResponse { DeliveryNoteId = "DN-1", CustomerId = Guid.NewGuid() };
        _mockService.Setup(x => x.GetByIdAsync("DN-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GeneratePdf("DN-1", CancellationToken.None);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result.Result);
        var pdfResponse = Assert.IsType<PdfGenerationResponse>(acceptedResult.Value);
        Assert.Equal("DN-1", pdfResponse.DeliveryNoteId);
        _mockService.Verify(
            x => x.RequestPdfGenerationAsync("DN-1", "test-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadFile_Success_ReturnsCreated()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var response = new DeliveryNoteFileResponse { FileId = Guid.NewGuid() };

        // Setup the mock to accept IFileData instead of IFormFile
        _mockService.Setup(x => x.AddFileAsync("DN-1", It.IsAny<IFileData>(), Maliev.DeliveryService.Domain.Entities.FileType.Photo, "Desc", "test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UploadFile("DN-1", fileMock.Object, "Photo", "Desc", CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(response, createdResult.Value);
    }

    [Fact]
    public async Task UpdateDeliveryNote_Conflict_ReturnsConflict()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("modified by another user"));

        // Act
        var result = await _controller.UpdateDeliveryNote("DN-1", new UpdateDeliveryNoteRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDeliveryNote_TerminalStatus_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("terminal status"));

        // Act
        var result = await _controller.UpdateDeliveryNote("DN-1", new UpdateDeliveryNoteRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDeliveryNote_UnexpectedError_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("BOOM"));

        // Act
        var result = await _controller.UpdateDeliveryNote("DN-1", new UpdateDeliveryNoteRequest(), CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryStatusRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid request"));

        // Act
        var result = await _controller.UpdateDeliveryStatus("DN-1", new UpdateDeliveryStatusRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GeneratePdf_PublishError_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteResponse { CustomerId = Guid.NewGuid() });
        _mockService.Setup(x => x.RequestPdfGenerationAsync("DN-1", "test-user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("BOOM"));

        // Act
        var result = await _controller.GeneratePdf("DN-1", CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UploadFile_ServiceNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.AddFileAsync(It.IsAny<string>(), It.IsAny<IFileData>(), It.IsAny<Maliev.DeliveryService.Domain.Entities.FileType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not found"));

        // Act
        var result = await _controller.UploadFile("DN-1", Mock.Of<IFormFile>(), "Photo", "Desc", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UploadFile_ServiceArgumentException_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.AddFileAsync(It.IsAny<string>(), It.IsAny<IFileData>(), It.IsAny<Maliev.DeliveryService.Domain.Entities.FileType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid file"));

        // Act
        var result = await _controller.UploadFile("DN-1", Mock.Of<IFormFile>(), "Photo", "Desc", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteDeliveryNote_BadRequest_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete"));

        // Act
        var result = await _controller.DeleteDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDeliveryNote_UnexpectedError_Returns500()
    {
        // Arrange
        _mockService.Setup(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("BOOM"));

        // Act
        var result = await _controller.DeleteDeliveryNote("DN-1", CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateDeliveryNote_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest { OrderId = "ORD-1" };
        _mockService.Setup(x => x.CreateAsync(request, "test-user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid request"));

        // Act
        var result = await _controller.CreateDeliveryNote(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDeliveryNote_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not found"));

        // Act
        var result = await _controller.UpdateDeliveryNote("DN-1", new UpdateDeliveryNoteRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
