using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/document-sequences")]
[Authorize(Policy = "AdminOnly")]
public class DocumentSequencesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;

    public DocumentSequencesController(IUnitOfWork uow, IDocumentSequenceService sequenceService)
    {
        _uow = uow;
        _sequenceService = sequenceService;
    }

    /// <summary>
    /// Gets all document sequences.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var sequences = await _uow.DocumentSequences.ToListAsync(ct);
            var dtos = sequences.Select(s => new DocumentSequenceDto(
                s.Id, s.DocumentType, s.Prefix, s.Year, s.LastNumber)).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error retrieving document sequences");
            return BadRequest(new { error = "حدث خطأ أثناء استرجاع تسلسل المستندات" });
        }
    }

    /// <summary>
    /// Updates a document sequence's last number (reset).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDocumentSequenceRequest request, CancellationToken ct)
    {
        try
        {
            var sequence = await _uow.DocumentSequences.GetByIdAsync(id, ct);
            if (sequence == null)
                return NotFound(new { error = "تسلسل المستند غير موجود" });

            sequence.SetLastNumber(request.NextNumber);

            await _uow.SaveChangesAsync(ct);

            Serilog.Log.Information("Document sequence {Id} ({Type}) updated. New LastNumber: {Number}",
                id, sequence.DocumentType, request.NextNumber);

            return Ok(new DocumentSequenceDto(
                sequence.Id, sequence.DocumentType, sequence.Prefix, sequence.Year, sequence.LastNumber));
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error updating document sequence {Id}", id);
            return BadRequest(new { error = "حدث خطأ أثناء تحديث تسلسل المستند" });
        }
    }
}
