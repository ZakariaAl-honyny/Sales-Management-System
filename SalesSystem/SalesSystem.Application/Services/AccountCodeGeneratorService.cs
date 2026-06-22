using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Services;

/// <summary>
/// Thread-safe service for generating hierarchical account codes.
/// Uses SemaphoreSlim to ensure unique codes under concurrent requests.
/// Code patterns:
///   Level 1: single digit (e.g. "5")
///   Level 2: parent code + 1 digit suffix (e.g. "101" from parent "10" + "1")
///   Level 3: parent code + 2 digit suffix (e.g. "101001" from parent "1010" + "01")
///   Level 4: parent code + 4 digit suffix (e.g. "1010010001" from parent "101001" + "0001")
/// </summary>
public class AccountCodeGeneratorService : IAccountCodeGeneratorService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountCodeGeneratorService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AccountCodeGeneratorService(IUnitOfWork uow, ILogger<AccountCodeGeneratorService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> GenerateCodeAsync(int? parentId, byte level, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            string code;

            if (parentId == null)
            {
                // Level 1: no parent — find max single-digit code
                var existingCodes = await _uow.Accounts.ToListAsync(
                    a => a.Level == 1, ct: ct);

                var maxNum = existingCodes
                    .Select(a => int.TryParse(a.AccountCode, out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                code = (maxNum + 1).ToString();
            }
            else
            {
                var parent = await _uow.Accounts.GetByIdAsync(parentId.Value, ct);
                if (parent == null)
                    return Result<string>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);

                var parentCode = parent.AccountCode;
                var prefixLength = parentCode.Length;

                // Find max child code with this parent prefix
                var children = await _uow.Accounts.ToListAsync(
                    a => a.ParentId == parentId.Value, ct: ct);

                var maxSuffix = 0;
                foreach (var child in children)
                {
                    if (child.AccountCode.Length > prefixLength &&
                        int.TryParse(child.AccountCode[prefixLength..], out var num))
                    {
                        maxSuffix = Math.Max(maxSuffix, num);
                    }
                }

                var nextSuffix = maxSuffix + 1;

                code = level switch
                {
                    2 => parentCode + nextSuffix.ToString("0"),
                    3 => parentCode + nextSuffix.ToString("00"),
                    4 => parentCode + nextSuffix.ToString("0000"),
                    _ => parentCode + nextSuffix.ToString()
                };
            }

            _logger.LogDebug("Generated account code '{Code}' at level {Level} (parentId: {ParentId})",
                code, level, parentId);

            return Result<string>.Success(code);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
