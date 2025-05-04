using FsCheck;
using FsCheck.Fluent;
using Xunit;
using Xunit.Abstractions;
using PropertyAttribute = FsCheck.Xunit.PropertyAttribute;

namespace properties_csharp;

public class Generators(ITestOutputHelper output)
{
    [Fact]
    public void Generate_Ints()
    {
        var intGen = ArbMap.Default.GeneratorFor<int>();
        var xes = intGen.Sample(100);
        output.WriteLine($"xes: {string.Join(",", xes.Select(e => e.ToString()).ToArray())}");
    }
    
    [Property(MaxTest = 10_000, StartSize = 0, EndSize = int.MaxValue)]
    public Property Sampling_Int_Sizes(int i)
    {
        return Prop.ToProperty(() => true)
                .Classify(i == 0, "zero")
                .Classify(i is > 0 and < 10, "pos 0-ish")
                .Classify(i is < 0 and > -10, "minus 0-ish")
                //.Collect(i)
                ;
    }
    [Property]
    public Property Sampling_Int_Sizes_Custom_Generator()
    {
        var smallish = Gen.Choose(-1000, 1000);
        var zero = Gen.Constant(0);
        var allPos = Gen.Choose(0, int.MaxValue);
        var allNeg = Gen.Choose(int.MinValue, 0);
        var broadIntGen = Gen.OneOf(
            Enumerable.Range(0, 100).Select(_ => allPos).ToArray()
                .Union(Enumerable.Range(0, 100).Select(_ => allNeg).ToArray())
                .Union([smallish,zero]));
        
        return Prop.ForAll(
            broadIntGen.ToArbitrary(), 
            n => Prop.ToProperty(() => true)
                .Classify(n > 0 && n<100, "around 100")
                .Classify(n<0 && n>-100, "around 100")
                .Collect(n));
    }
    
    [Fact]
    public void Generate_Strings()
    {
        var stringGen = ArbMap.Default.GeneratorFor<string>();
        var xes = stringGen.Sample(10);
        output.WriteLine($"{string.Join("\n", xes)}");
    }

    public enum State
    {
        Ordered,
        Paid,
        Shipping,
        Delivered,
        Cancelled
    }

    public record Order(State State, int Amount, decimal? Price, string Customer);

    [Fact]
    public void Generate_Custom_Records()
    {
        var intGen = ArbMap.Default.GeneratorFor<Order>();
        var xes = intGen.Sample(10);
        output.WriteLine($"xes: {string.Join("\n", xes.Select(e => e.ToString()).ToArray())}");
    }

    [Fact]
    public void Constraining_Values()
    {
        var positiveIntGen = ArbMap.Default.GeneratorFor<int>()
            .Where(i=> i >= 0);
        var xes = positiveIntGen.Sample(100);
        output.WriteLine($"xes: {string.Join(",", xes.Select(e => e.ToString()).ToArray())}");
    }
    
    [Fact]
    public void Combining_Generators()
    {
        var oneOrTwoOrThree = Gen.Choose(1,3);
        var ten = Gen.Constant(10);
        var gen = Gen.ListOf(Gen.OneOf(oneOrTwoOrThree, ten));
            
        var xes = gen.Sample(10);
        foreach (var s in xes)
        {
            output.WriteLine($"{string.Join(",", s)}");
        }
    }


    [Fact]
    public void Smart_Value_Generation()
    {
        var neStringGen = ArbMap.Default.GeneratorFor<NonEmptyString>().Select(nes => nes.Item);
        var charGen = Gen.OneOf("abcdefghijklmnopqrstuvwxyz".Select(Gen.Constant));
        var tldGen =
            charGen.ListOf(2)
                .Or(charGen.ListOf(3))
                .Select(c => string.Join("", c));
        var emailGen =
            from local in neStringGen
            from domain in neStringGen
            from tld in tldGen
            select $"{local}@{domain}.{tld}";

        var xes = emailGen.Sample(10);

        output.WriteLine($"xes: {string.Join("\n", xes)}");
    }

    [Fact]
    public void _1d6_Generator()
    {
        var _1d6Gen = Gen.Choose(1, 6);
        var xes = _1d6Gen.Sample(100);
        output.WriteLine($"{string.Join(",", xes)}");
    }
    
    [Property(MaxTest = 10_000)]
    public Property Checking_Distributions()
    {
        var _1d6Gen = Gen.Choose(1, 6).ToArbitrary();

        return Prop.ForAll(
            _1d6Gen,
            value => true.Collect(value));
    }

    [Property(MaxTest = 10_000)]
    public Property Checking_Distributions_2()
    {
        var _2d6Gen = Gen.Choose(1, 12).ToArbitrary();

        return Prop.ForAll(
            _2d6Gen,
            value => true.Collect(value));
    }

    [Property(MaxTest = 10_000)]
    public Property Correct_2d6()
    {
        var _2d6Gen = Gen.OneOf(
            new List<int>
                {
                    2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10,
                    10, 11, 11, 12
                }
                .Select(Gen.Constant));

        return Prop.ForAll(
            _2d6Gen.ToArbitrary(),
            value => true.Collect(value));
    }

    [Property(MaxTest = 10_000)]
    public Property Correct_2d6_Alternative()
    {
        Gen<int> _1d6Gen = Gen.Choose(1, 6);
        Gen<int> _2d6Gen =
            from one in _1d6Gen
            from other in _1d6Gen
            select one + other;

        return Prop.ForAll(
            _2d6Gen.ToArbitrary(),
            value => true.Collect(value));
    }
}