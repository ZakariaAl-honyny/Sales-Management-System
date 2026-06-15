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
            .MaximumLength(20).WithMessage("رمز العملة (Symbol) لا يمكن أن يتجاوز 20 حرفاً");

        RuleFor(x => x.FractionName)
            .MaximumLength(50).WithMessage("اسم الجزء الكسري لا يمكن أن يتجاوز 50 حرفاً")
            .When(x => !string.IsNullOrEmpty(x.FractionName));

        RuleFor(x => x.DecimalPlaces)
            .InclusiveBetween(0, 4).WithMessage("عدد المنازل العشرية يجب أن يكون بين 0 و 4");
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
            .MaximumLength(20).WithMessage("رمز العملة (Symbol) لا يمكن أن يتجاوز 20 حرفاً");

        RuleFor(x => x.FractionName)
            .MaximumLength(50).WithMessage("اسم الجزء الكسري لا يمكن أن يتجاوز 50 حرفاً")
            .When(x => !string.IsNullOrEmpty(x.FractionName));

        RuleFor(x => x.DecimalPlaces)
            .InclusiveBetween(0, 4).WithMessage("عدد المنازل العشرية يجب أن يكون بين 0 و 4");
    }
}


