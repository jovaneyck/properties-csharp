using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using properties_csharp.xml;
using Xunit;

namespace properties_csharp;

public record Coin(int Amount)
{
    public static int MAX_COINS = 100_000;

    public Result<Coin, string> Add(Coin other)
    {
        var sum = Amount + other.Amount;
        return sum <= MAX_COINS
            ? Result<Coin, string>.Success(new Coin(sum))
            : Result<Coin, string>.Failure("Overflow");
    }
}

public class CoinTests
{
    [Fact]
    public void Normal_Addition()
    {
        var result = new Coin(1).Add(new Coin(2));
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Amount);
    }
    
    [Fact]
    public void Boundary_Addition()
    {
        var result = new Coin(Coin.MAX_COINS - 1).Add(new Coin(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(Coin.MAX_COINS, result.Value!.Amount);
    }

    [Fact]
    public void Overflow_Addition()
    {
        var result = new Coin(Coin.MAX_COINS).Add(new Coin(1));
        Assert.False(result.IsSuccess);
    }

    [Property(Arbitrary = [typeof(CoinArbs)])]
    public bool ArbGeneratesValidCoins_Property(Coin c)
    {
        return c.Amount >= 0 && c.Amount <= Coin.MAX_COINS;
    }


    [Property(MaxTest = 10_000)]
    public Property ArbCoinDistribution_Property()
    {
        return Prop.ForAll(
            CoinArbs.CoinArb(),
            CoinArbs.CoinArb(),
            (c1, c2) => Prop.ToProperty(() => true)
                .Classify(c1.Amount + c2.Amount <= Coin.MAX_COINS, "OK")
                .Classify(Math.Abs(Coin.MAX_COINS - (c1.Amount + c2.Amount)) < 3, "boundary")
                .Classify(c1.Amount + c2.Amount > Coin.MAX_COINS, "overflow")
        );
    }
    
    [Property(StartSize = 1_000_000)]
    public Property NormalAddition_Property()
    {
        var coinArb = ArbMap.Default
            .GeneratorFor<PositiveInt>()
            .Select(pi=>new Coin(pi.Item))
            .ToArbitrary();

        return Prop.ForAll(
            coinArb,
            coinArb,
            (c1, c2) =>
            {
                var result = c1.Add(c2);
                Assert.True(result.IsSuccess);
                Assert.Equal(
                    c1.Amount + c2.Amount,
                    result.Value!.Amount);
            });
    }
    
    [Property(Arbitrary = [typeof(CoinArbs)])]
    public Property NormalAddition_Property2(Coin c1, Coin c2)
    {
        return (c1.Amount + c2.Amount < Coin.MAX_COINS).Implies(() =>
        {
            var result = c1.Add(c2);
            Assert.True(result.IsSuccess);
            Assert.Equal(
                c1.Amount + c2.Amount,
                result.Value!.Amount);
        });
    }

    [Property(Arbitrary =
        [typeof(CoinArbs)])]
    public Property OverflowAddition_Property(Coin c1, Coin c2)
    {
        return (c1.Amount + c2.Amount > Coin.MAX_COINS).Implies(() =>
        {
            var result = c1.Add(c2);
            Assert.True(result.IsFailure);
        });
    }

    [Property(Arbitrary = [typeof(CoinArbs)])]
    public Property NormalAddition_Property_ConstrainedType(NormalPairOfCoins pair)
    {
        var (c1, c2) = pair;
        return Prop.ToProperty(() =>
            {
                var result = c1.Add(c2);
                Assert.True(result.IsSuccess);
                Assert.Equal(
                    c1.Amount + c2.Amount,
                    result.Value!.Amount);
            })
            .Classify(c1.Amount + c2.Amount <= Coin.MAX_COINS, "OK")
            .Classify(Math.Abs(Coin.MAX_COINS - (c1.Amount + c2.Amount)) < 3, "boundary")
            .Classify(c1.Amount + c2.Amount > Coin.MAX_COINS, "overflow");
    }

    [Property(Arbitrary = [typeof(CoinArbs)], MaxTest = 1_000_000)]
    public Property OverflowingAddition_Property_ConstrainedType(OverflowingPairOfCoins pair)
    {
        var (c1, c2) = pair;
        return Prop.ToProperty(() =>
            {
                var result = c1.Add(c2);
                Assert.False(result.IsSuccess);
            })
            .Classify(c1.Amount + c2.Amount <= Coin.MAX_COINS, "OK")
            .Classify(Math.Abs(Coin.MAX_COINS - (c1.Amount + c2.Amount)) < 3, "boundary")
            .Classify(c1.Amount + c2.Amount > Coin.MAX_COINS, "overflow");
    }

    [Property(Arbitrary = [typeof(CoinArbs)])]
    public Property Addition_OnePropertyToRuleThemAll(Coin c1, Coin c2)
    {
        return Prop.ToProperty(() =>
            {
                var result = c1.Add(c2);
                var sum = c1.Amount + c2.Amount;
                if (sum <= Coin.MAX_COINS)
                {
                    Assert.True(result.IsSuccess);
                    Assert.Equal(sum, result.Value!.Amount);
                }
                else
                {
                    Assert.False(result.IsSuccess);
                }
            })
            .Classify(c1.Amount + c2.Amount <= Coin2.MAX_COINS, "OK")
            .Classify(Math.Abs(Coin2.MAX_COINS - (c1.Amount + c2.Amount)) < 3, "boundary")
            .Classify(c1.Amount + c2.Amount > Coin2.MAX_COINS, "overflow");
    }
    
    [Property(Arbitrary = [typeof(CoinArbs)])]
    public Property Addition_OnePropertyToRuleThemAll2(Coin2 c1, Coin2 c2)
    {
        return Prop.ToProperty(() =>
            {
                var result = c1.Add(c2);
                var sum = c1.Amount + c2.Amount;
                if (sum <= Coin.MAX_COINS)
                {
                    Assert.True(result.IsSuccess);
                    Assert.Equal(sum, result.Value!.Amount);
                }
                else
                {
                    Assert.False(result.IsSuccess);
                }
            })
            .Classify(c1.Amount + c2.Amount <= Coin2.MAX_COINS, "OK")
            .Classify(Math.Abs(Coin2.MAX_COINS - (c1.Amount + c2.Amount)) < 3, "boundary")
            .Classify(c1.Amount + c2.Amount > Coin2.MAX_COINS, "overflow");
    }
}

/// <summary>
///     Represents 2 coins whose sum does not exceed the maximum.
/// </summary>
public record NormalPairOfCoins(Coin One, Coin Other);

/// <summary>
///     Represents 2 coins whose sum overflows.
/// </summary>
public record OverflowingPairOfCoins(Coin One, Coin Other);

public static class CoinArbs
{
    public static Arbitrary<Coin> CoinArb()
    {
        return Gen.Choose(0, Coin.MAX_COINS)
            .Select(i => new Coin(i))
            .ToArbitrary();
    }

    public static Arbitrary<NormalPairOfCoins> NormalPairOfCoinsArb()
    {
        return CoinArb().Generator.SelectMany(c =>
            {
                var remainder = Coin.MAX_COINS - c.Amount;
                return Gen.Choose(0, remainder).Select(r => new NormalPairOfCoins(c, new Coin(r)));
            })
            .ToArbitrary();
    }

    public static Arbitrary<OverflowingPairOfCoins> OverflowingPairOfCoinsArb()
    {
        return CoinArb().Generator.SelectMany(c =>
            {
                var min = Coin.MAX_COINS - c.Amount;
                return Gen.Choose(min, Coin.MAX_COINS).Select(r => new OverflowingPairOfCoins(c, new Coin(r)));
            })
            .ToArbitrary();
    }

    public static Arbitrary<Coin2> Coin2Arb()
    {
        return
            Gen.OneOf(
                    Gen.Choose(0, 3),
                    Gen.Choose(Coin2.MAX_COINS - 3, Coin2.MAX_COINS),
                    Gen.Choose(0, Coin2.MAX_COINS)
                )
                .Select(i => new Coin2(i))
                .ToArbitrary();
    }
}

public record Coin2(int Amount)
{
    public static int MAX_COINS = 100_000;

    public Result<Coin2, string> Add(Coin2 other)
    {
        var sum = Amount + other.Amount;
        return sum <= MAX_COINS
            ? Result<Coin2, string>.Success(new Coin2(sum))
            : Result<Coin2, string>.Failure("Overflow");
    }
}