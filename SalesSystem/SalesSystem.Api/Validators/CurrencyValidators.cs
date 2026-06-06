using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCurrencyRequestValidator : AbstractValidator<CreateCurrencyRequest>
{
    public CreateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العملة مطلوب")
            .MaximumLength(100).WithMessage("اسم العملة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("رمز العملة مطلوب")
            .MaximumLength(10).WithMessage("رمز العملة لا يمكن أن يتجاوز 10 أحرف")
            .Matches("^[A-Z]{3}$").WithMessage("رمز العملة يجب أن يكون 3 أحرف إنجليزية كبيرة (مثل SAR)");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("رمز العملة (Symbol) مطلوب")
            .MaximumLength(10).WithMessage("رمز العملة (Symbol) لا يمكن أن يتجاوز 10 أحرف");

        RuleFor(x => x.ExchangeRateToBase)
            .GreaterThan(0).WithMessage("سعر الصرف يجب أن يكون أكبر من صفر");

        RuleFor(x => x.FractionName)
            .MaximumLength(20).WithMessage("اسم الجزء الكسري لا يمكن أن يتجاوز 20 حرفاً")
            .When(x => !string.IsNullOrEmpty(x.FractionName));
    }
}

public class UpdateCurrencyRequestValidator : AbstractValidator<UpdateCurrencyRequest>
{
    public UpdateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العملة مطلوب")
            .MaximumLength(100).WithMessage("اسم العملة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("رمز العملة (Symbol) مطلوب")
            .MaximumLength(10).WithMessage("رمز العملة (Symbol) لا يمكن أن يتجاوز 10 أحرف");

        RuleFor(x => x.ExchangeRateToBase)
            .GreaterThan(0).WithMessage("سعر الصرف يجب أن يكون أكبر من صفر");

        RuleFor(x => x.FractionName)
            .MaximumLength(20).WithMessage("اسم الجزء الكسري لا يمكن أن يتجاوز 20 حرفاً")
            .When(x => !string.IsNullOrEmpty(x.FractionName));
    }
}

public class UpdateExchangeRateRequestValidator : AbstractValidator<UpdateExchangeRateRequest>
{
    public UpdateExchangeRateRequestValidator()
    {
        RuleFor(x => x.NewRate)
            .GreaterThan(0).WithMessage("سعر الصرف الجديد يجب أن يكون أكبر من صفر");
    }
}
