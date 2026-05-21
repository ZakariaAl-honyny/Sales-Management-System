
### Service Layer Error Handling Template (The Golden Template)
All application services MUST follow this structural template for robust global error catching and logging:

```csharp
public async Task<Result<MyDto>> DoSomethingAsync(int id, CancellationToken ct)
{
    try
    {
        // 1. Core Logic & DB Operations
        var entity = await _uow.Entities.GetByIdAsync(id, ct);
        if (entity == null)
            return Result<MyDto>.Failure("العنصر غير موجود", ErrorCodes.NotFound);

        // ... execute logic ...
        await _uow.SaveChangesAsync(ct);
        
        // 2. Info Logging (Optional but recommended for critical actions)
        _logger.LogInformation("Action completed for Entity: {EntityId}", entity.Id);

        return Result<MyDto>.Success(MapToDto(entity));
    }
    catch (Exception ex)
    {
        // 3. Catch all unexpected exceptions and log with full details
        _logger.LogError(ex, "Error occurred while executing DoSomethingAsync for ID: {Id}", id);
        
        // 4. Return unified internal error without crashing the application
        return Result<MyDto>.Failure("حدث خطأ أثناء تنفيذ العملية.", ErrorCodes.InternalError);
    }
}
```

