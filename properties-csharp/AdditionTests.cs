using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using Xunit;
using Xunit.Abstractions;
using PropertyAttribute = FsCheck.Xunit.PropertyAttribute;

namespace properties_csharp;

public partial class AdditionTests(ITestOutputHelper output)
{
    private static int Add(int a, int b)
    {
        return a + b;
    }

    [Fact]
    public void A_SumsCorrectly()
    {
        Assert.Equal(3, Add(0, 3));
        Assert.Equal(4, Add(4, 0));
        Assert.Equal(5, Add(2, 3));
    }

    [Property]
    public void ZeroElement(int x)
    {
        Assert.Equal(x, Add(0, x));
        Assert.Equal(x, Add(x, 0));
    }

    [Property]
    public Property ZeroElement_Labeled()
    {
        return Prop.ForAll<int>(x =>
        {
            return Prop.Label(() => x == Add(0, x), $"left: {x} == 0+{x}")
                .And(Prop.Label(() => x == Add(x, 0), $"right: {x} == {x}+0"));
        });
    }

    [Fact]
    public void ZeroElement_NoFluff()
    {
        Prop.ForAll(
                ArbMap.Default.ArbFor<int>(),
                x => x == Add(0, x) && x == Add(x, 0))
            .VerboseCheckThrowOnFailure();
    }

    [Property]
    // [Property(Replay="11173762298825490426,5811471820487309261")]
    public void Commutativity(int x, int y)
    {
        Assert.Equal(Add(x, y), Add(y, x));
    }

    [Property]
    public Property D_AlwaysLargerThanInputForPositiveInputs_Filtering(int x, int y)
    {
        return (x > 0 && y > 0).When(() =>
        {
            var sum = Add(x, y);
            return sum > x && sum > y;
        });
    }

    [Property]
    public void D_AlwaysLargerThanInputForPositiveInputs_ConstrainedTypes(PositiveInt x, PositiveInt y)
    {
        var sum = Add(x.Item, y.Item);
        Assert.True(sum >= x.Item);
        Assert.True(sum >= y.Item);
    }

    [Property]
    public void E_PeanoSuccessor(int x, int y)
    {
        Assert.Equal(Add(x, y), Add(1, Add(x, y - 1)));
    }
    
    [GeneratedRegex(@"(.*)@(.*)\.(.*)")]
    private static partial Regex MyRegex();

    public string? ExtractDomain(string emailaddress)
    {
        var match = MyRegex().Match(emailaddress);
        return !match.Success ? null : match.Groups[2].Value;
    }

    [Fact]
    public void ExtractsDomain_FromValidEmails()
    {
        Assert.Equal("gmail", ExtractDomain("jo.vaneyck@gmail.com"));
    }

    [Fact]
    public void CannotExtractDomain_FromInvalidEmail()
    {
        Assert.Null(ExtractDomain("not_an_email"));
    }

    [Property]
    public Property ExtractsDomainProperty()
    {
        return Prop.ForAll(
            Arbitraries.ValidEmails,
            e => Assert.Equal(e.Domain, ExtractDomain(e.ToString())));
    }

    [Fact]
    public void Sandbox()
    {
        var generator =
            Arbitraries.ValidEmails.Generator;

        var xes = generator.Sample(10);
        // output.WriteLine($"xes: {string.Join("\n", xes.Select(e=>e.ToString()).ToArray())}");
    }
}

public record Email(string LocalName, string Domain, string TLD)
{
    public override string ToString()
    {
        return $"{LocalName}@{Domain}.{TLD}";
    }
}

public static class Arbitraries
{
    public static Arbitrary<char> AlphaNumericCharacters =
        Gen.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Select(Gen.Constant))
            .ToArbitrary();

    public static Arbitrary<string> AlphaNumericStrings =
        (from chars in AlphaNumericCharacters.Generator.NonEmptyListOf()
            select string.Join("", chars))
        .ToArbitrary();


    public static Arbitrary<Email> ValidEmails =
        (from localName in AlphaNumericStrings.Generator
            from domain in AlphaNumericStrings.Generator
            from tld in AlphaNumericStrings.Generator
            select new Email(localName, domain, tld))
        .ToArbitrary();
}