using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class DocumentSequence : BaseEntity
{
    public string DocumentType { get; private set; } = string.Empty;
    public string Prefix { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int LastNumber { get; private set; }

    private DocumentSequence() { }

    public static DocumentSequence Create(string documentType, string prefix, int year)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new DomainException("نوع المستند مطلوب.");
        if (string.IsNullOrWhiteSpace(prefix))
            throw new DomainException("البادئة مطلوبة.");
        if (year <= 0)
            throw new DomainException("السنة مطلوبة.");

        return new DocumentSequence
        {
            DocumentType = documentType,
            Prefix = prefix,
            Year = year,
            LastNumber = 0
        };
    }

    public string GetNextNumber()
    {
        LastNumber++;
        return $"{Prefix}-{Year:D4}-{LastNumber:D6}";
    }

    public int GetNextInt()
    {
        LastNumber++;
        return LastNumber;
    }

    public void Increment()
    {
        LastNumber++;
    }
}