using FsCheck;
using Xunit;
using PropertyAttribute = FsCheck.Xunit.PropertyAttribute;

namespace properties_csharp;

public class AdditionTests
{
    private static int Add(int a, int b) => a + b;
    
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
    public void Commutativity(int x, int y)
    {
        Assert.Equal(Add(x, y), Add(y, x));
    }

    [Property]
    public void D_AlwaysLargerThanInputForPositiveInputs(PositiveInt x, PositiveInt y)
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
}