using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Application.Accounting.Services;

public class SystemAccountService : ISystemAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SystemAccountService> _logger;

    public SystemAccountService(IUnitOfWork uow, ILogger<SystemAccountService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<SystemAccountMappingsDto>> GetMappingsAsync(int? branchId = null, CancellationToken ct = default)
    {
        try
        {
            // Try branch-specific first, fall back to global (branchId == null)
            var mappings = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                m => (branchId == null && m.BranchId == null) || (branchId != null && m.BranchId == branchId),
                ct: ct);

            if (mappings == null)
                return Result<SystemAccountMappingsDto>.Failure("لم يتم إعداد حسابات النظام — يرجى الاتصال بالمسؤول");

            // Load all referenced accounts in a single query for name/code resolution
            var accountIds = new HashSet<int>
            {
                mappings.DefaultCashAccountId,
                mappings.DefaultBankAccountId,
                mappings.InventoryAssetAccountId,
                mappings.AccountsReceivableAccountId,
                mappings.AccountsPayableAccountId,
                mappings.VatOutputAccountId,
                mappings.VatInputAccountId,
                mappings.CapitalAccountId,
                mappings.SalesRevenueAccountId,
                mappings.SalesReturnAccountId,
                mappings.CogsAccountId,
                mappings.GeneralExpenseAccountId,
                mappings.SpoilageLossAccountId
            };

            if (mappings.OpeningBalanceEquityAccountId.HasValue)
                accountIds.Add(mappings.OpeningBalanceEquityAccountId.Value);

            var accounts = await _uow.Accounts.ToListAsync(
                a => accountIds.Contains(a.Id), ct: ct);

            var accountLookup = accounts.ToDictionary(a => a.Id);

            static (string? name, string? code) GetAccountInfo(Dictionary<int, Account> lookup, int id)
                => lookup.TryGetValue(id, out var acc) ? (acc.NameAr, acc.AccountCode) : (null, null);

            var (cashName, cashCode) = GetAccountInfo(accountLookup, mappings.DefaultCashAccountId);
            var (bankName, bankCode) = GetAccountInfo(accountLookup, mappings.DefaultBankAccountId);
            var (invName, invCode) = GetAccountInfo(accountLookup, mappings.InventoryAssetAccountId);
            var (arName, arCode) = GetAccountInfo(accountLookup, mappings.AccountsReceivableAccountId);
            var (apName, apCode) = GetAccountInfo(accountLookup, mappings.AccountsPayableAccountId);
            var (vatOutName, vatOutCode) = GetAccountInfo(accountLookup, mappings.VatOutputAccountId);
            var (vatInName, vatInCode) = GetAccountInfo(accountLookup, mappings.VatInputAccountId);
            var (capName, capCode) = GetAccountInfo(accountLookup, mappings.CapitalAccountId);
            var (revName, revCode) = GetAccountInfo(accountLookup, mappings.SalesRevenueAccountId);
            var (retName, retCode) = GetAccountInfo(accountLookup, mappings.SalesReturnAccountId);
            var (cogsName, cogsCode) = GetAccountInfo(accountLookup, mappings.CogsAccountId);
            var (expName, expCode) = GetAccountInfo(accountLookup, mappings.GeneralExpenseAccountId);
            var (spoilName, spoilCode) = GetAccountInfo(accountLookup, mappings.SpoilageLossAccountId);

            var (obeName, obeCode) = mappings.OpeningBalanceEquityAccountId.HasValue
                ? GetAccountInfo(accountLookup, mappings.OpeningBalanceEquityAccountId.Value)
                : (null, (string?)null);

            var dto = new SystemAccountMappingsDto(
                Id: mappings.Id,
                DefaultCashAccountId: mappings.DefaultCashAccountId,
                DefaultCashAccountName: cashName,
                DefaultCashAccountCode: cashCode,
                DefaultBankAccountId: mappings.DefaultBankAccountId,
                DefaultBankAccountName: bankName,
                DefaultBankAccountCode: bankCode,
                InventoryAssetAccountId: mappings.InventoryAssetAccountId,
                InventoryAssetAccountName: invName,
                InventoryAssetAccountCode: invCode,
                AccountsReceivableAccountId: mappings.AccountsReceivableAccountId,
                AccountsReceivableAccountName: arName,
                AccountsReceivableAccountCode: arCode,
                AccountsPayableAccountId: mappings.AccountsPayableAccountId,
                AccountsPayableAccountName: apName,
                AccountsPayableAccountCode: apCode,
                VatOutputAccountId: mappings.VatOutputAccountId,
                VatOutputAccountName: vatOutName,
                VatOutputAccountCode: vatOutCode,
                VatInputAccountId: mappings.VatInputAccountId,
                VatInputAccountName: vatInName,
                VatInputAccountCode: vatInCode,
                CapitalAccountId: mappings.CapitalAccountId,
                CapitalAccountName: capName,
                CapitalAccountCode: capCode,
                SalesRevenueAccountId: mappings.SalesRevenueAccountId,
                SalesRevenueAccountName: revName,
                SalesRevenueAccountCode: revCode,
                SalesReturnAccountId: mappings.SalesReturnAccountId,
                SalesReturnAccountName: retName,
                SalesReturnAccountCode: retCode,
                CogsAccountId: mappings.CogsAccountId,
                CogsAccountName: cogsName,
                CogsAccountCode: cogsCode,
                GeneralExpenseAccountId: mappings.GeneralExpenseAccountId,
                GeneralExpenseAccountName: expName,
                GeneralExpenseAccountCode: expCode,
                SpoilageLossAccountId: mappings.SpoilageLossAccountId,
                SpoilageLossAccountName: spoilName,
                SpoilageLossAccountCode: spoilCode,
                OpeningBalanceEquityAccountId: mappings.OpeningBalanceEquityAccountId,
                OpeningBalanceEquityAccountName: obeName,
                OpeningBalanceEquityAccountCode: obeCode,
                BranchId: mappings.BranchId);

            return Result<SystemAccountMappingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system account mappings for branch {BranchId}", branchId);
            return Result<SystemAccountMappingsDto>.Failure("حدث خطأ أثناء استرجاع حسابات النظام");
        }
    }
}
