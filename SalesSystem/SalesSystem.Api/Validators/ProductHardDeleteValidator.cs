using FluentValidation;
using SalesSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SalesSystem.Api.Validators;

public class ProductHardDeleteValidator : AbstractValidator<int>
{
    public ProductHardDeleteValidator(IUnitOfWork uow)
    {
        RuleFor(id => id)
            .MustAsync(async (id, ct) =>
            {
                var hasSales = await uow.SalesInvoiceItems.Query().AnyAsync(i => i.ProductId == id, ct);
                return !hasSales;
            })
            .WithMessage("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات بيع");

        RuleFor(id => id)
            .MustAsync(async (id, ct) =>
            {
                var hasPurchase = await uow.PurchaseInvoiceItems.Query().AnyAsync(i => i.ProductId == id, ct);
                return !hasPurchase;
            })
            .WithMessage("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات شراء");
    }
}