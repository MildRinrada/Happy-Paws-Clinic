namespace HappyPaws.Domain.Common;

/// <summary>
/// Monetary amount in satang (1/100 THB), matching the API contract:
/// money is transported as an integer number of satang (<c>146000</c> = ฿1,460.00).
/// Never store or transport clinic money as a floating point value.
/// </summary>
public readonly record struct Money(long Satang)
{
    public static readonly Money Zero = new(0);

    public static Money operator +(Money a, Money b) => new(a.Satang + b.Satang);

    public static Money operator -(Money a, Money b) => new(a.Satang - b.Satang);

    public bool IsNegative => Satang < 0;

    public override string ToString() => (Satang / 100m).ToString("0.00");
}
