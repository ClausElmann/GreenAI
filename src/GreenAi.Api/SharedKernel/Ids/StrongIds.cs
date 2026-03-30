namespace GreenAi.Api.SharedKernel.Ids;

public readonly record struct UserId(int Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct CustomerId(int Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct ProfileId(int Value)
{
    public override string ToString() => Value.ToString();
}
