using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class DocumentSequence : AuditableEntity
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

    /// <summary>
    /// Resets the last number to a new value (e.g., during year rollover or manual correction).
    /// </summary>
    /// <param name="newNumber">The new last number (must be >= 0).</param>
    /// <exception cref="DomainException">If newNumber is negative.</exception>
    public void SetLastNumber(int newNumber)
    {
        if (newNumber < 0)
            throw new DomainException("الرقم التسلسلي لا يمكن أن يكون سالباً.");
        LastNumber = newNumber;
        UpdateTimestamp();
    }
}