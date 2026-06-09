using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing Bills of Materials and producing assembled products.
/// </summary>
public interface IAssemblyService
{
    // ─── BOM CRUD ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new Bill of Materials entry linking a component to an assembly product.
    /// </summary>
    Task<Result<BillOfMaterialDto>> CreateBomAsync(CreateBillOfMaterialRequest request, CancellationToken ct);

    /// <summary>
    /// Updates an existing Bill of Materials entry.
    /// </summary>
    Task<Result<BillOfMaterialDto>> UpdateBomAsync(int id, UpdateBillOfMaterialRequest request, CancellationToken ct);

    /// <summary>
    /// Soft-deletes a Bill of Materials entry.
    /// </summary>
    Task<Result> DeleteBomAsync(int id, CancellationToken ct);

    /// <summary>
    /// Gets a single Bill of Materials entry by ID.
    /// </summary>
    Task<Result<BillOfMaterialDto>> GetBomByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Gets all BOM entries for a specific assembly product.
    /// </summary>
    Task<Result<List<BillOfMaterialDto>>> GetBomsForAssemblyAsync(int assemblyProductId, CancellationToken ct);

    /// <summary>
    /// Gets all BOM entries across all assembly products.
    /// </summary>
    Task<Result<List<BillOfMaterialDto>>> GetAllBomsAsync(CancellationToken ct);

    // ─── Assembly Production ────────────────────────────────────────────

    /// <summary>
    /// Produces an assembly by deducting component quantities from inventory
    /// using FIFO/FEFO allocation and adding the finished product as a new inventory batch.
    /// </summary>
    Task<Result<ProduceAssemblyResultDto>> ProduceAsync(ProduceAssemblyRequest request, int userId, CancellationToken ct);
}
