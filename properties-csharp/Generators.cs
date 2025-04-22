using FsCheck;
using FsCheck.Fluent;
using PropertyAttribute = FsCheck.Xunit.PropertyAttribute;
using Xunit;
using Xunit.Abstractions;

namespace properties_csharp;

public class Generators(ITestOutputHelper output)
{
    [Fact]
    public void Generate_Ints()
    {
        var intGen = ArbMap.Default.GeneratorFor<int>();
        var xes = intGen.Sample(100);
        output.WriteLine($"xes: {string.Join(",", xes.Select(e=>e.ToString()).ToArray())}");
    }
    
    public enum State {Ordered, Paid, Shipping, Delivered, Cancelled }
    public record Order(State State, int Amount, decimal? Price, string Customer);
    
    [Fact]
    public void Generate_Custom_Records()
    {
        var intGen = ArbMap.Default.GeneratorFor<Order>();
        var xes = intGen.Sample(10);
        output.WriteLine($"xes: {string.Join("\n", xes.Select(e=>e.ToString()).ToArray())}");
    }
    
    [Fact]
    public void Constraining_Values()
    {
        var intGen = ArbMap.Default.GeneratorFor<int>()
            .Where(i=> i > 0);
        var xes = intGen.Sample(100);
        output.WriteLine($"xes: {string.Join(",", xes.Select(e=>e.ToString()).ToArray())}");
    }
    
    
    [Fact]
    public void Smart_Value_Generation()
    {
        var neStringGen = ArbMap.Default.GeneratorFor<NonEmptyString>().Select(nes=>nes.Item);
        var charGen = Gen.OneOf("abcdefghijklmnopqrstuvwxyz".Select(Gen.Constant));
        var tldGen = 
            charGen.ListOf(2)
                .Or(charGen.ListOf(3))
                .Select(c=>string.Join("",c));
        var emailGen =
            from local in neStringGen
            from domain in neStringGen
            from tld in tldGen
            select $"{local}@{domain}.{tld}";
        
        var xes = emailGen.Sample(100);
        
        output.WriteLine($"xes: {string.Join("\n", xes)}");
    }

    [Property(MaxTest = 10_000)]
    public Property Checking_Distributions()
    {
        var _1d6Gen = Gen.Choose(1, 6).ToArbitrary();
        
        return Prop.ForAll(
            _1d6Gen, 
            value => (true).Collect(value));
    }
    
    [Property(MaxTest = 10_000)]
    public Property Checking_Distributions_2()
    {
        var _2d6Gen = Gen.Choose(1, 12).ToArbitrary();
        
        return Prop.ForAll(
            _2d6Gen, 
            value => (true).Collect(value));
    }
    
    [Property(MaxTest = 10_000)]
    public Property Correct_2d6()
    {
        var _2d6Gen = Gen.OneOf(
            new List<int>{2,3,3,4,4,4,5,5,5,5,6,6,6,6,6,7,7,7,7,7,7,8,8,8,8,8,9,9,9,9,10,10,10,11,11,12}
                .Select(Gen.Constant));
        
        return Prop.ForAll(
            _2d6Gen.ToArbitrary(), 
            value => (true).Collect(value));
    }
    
    [Property(MaxTest = 10_000)]
    public Property Correct_2d6_Alternative()
    {
        var _1d6Gen = Gen.Choose(1, 6);
        var _2d6Gen =
            from one in _1d6Gen
            from other in _1d6Gen
            select one + other;
        
        return Prop.ForAll(
            _2d6Gen.ToArbitrary(), 
            value => (true).Collect(value));
    }
}