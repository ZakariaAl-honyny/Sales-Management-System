namespace SalesSystem.Domain.Entities;

public class DocumentSequence
{
    public int DocumentSequenceId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string Prefix { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int LastNumber { get; private set; }

    private DocumentSequence() { }

    public static DocumentSequence Create(string documentType, string prefix, int year)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("DocumentType is required.", nameof(documentType));
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix is required.", nameof(prefix));
        if (year <= 0)
            throw new ArgumentException("Year is required.", nameof(year));

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

    public void Increment()
    {
        LastNumber++;
    }
}