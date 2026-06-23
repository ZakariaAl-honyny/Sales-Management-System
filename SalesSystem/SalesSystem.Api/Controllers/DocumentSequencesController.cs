using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/document-sequences")]
[Authorize(Policy = "AdminOnly")]
public class DocumentSequencesController : ControllerBase
{
    private readonly IDocumentSequenceService _sequenceService;

    public DocumentSequencesController(IDocumentSequenceService sequenceService)
    {
        _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
    }

    /// <summary>
    /// Gets all document sequences.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sequenceService.GetAllAsync(ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates a document sequence's next number (reset).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDocumentSequenceRequest request, CancellationToken ct)
    {
        var result = await _sequenceService.UpdateSequenceAsync(id, request.NextNumber, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });

        return BadRequest(new { error = result.Error });
    }
}
