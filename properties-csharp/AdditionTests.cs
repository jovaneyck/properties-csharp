using FsCheck;
using NUnit.Framework;
using PropertyAttribute = FsCheck.NUnit.PropertyAttribute;

namespace properties_csharp;

[TestFixture]
public class AdditionTests
{
    private static int Add(int a, int b) => a + b;
    
    [Test]
    public void A_SumsCorrectly()
    {
        Assert.That(Add(0,3), Is.EqualTo(3));
        Assert.That(Add(4,0), Is.EqualTo(4));
        Assert.That(Add(2,3), Is.EqualTo(5));
    }

    [Property]
    public void ZeroElement(int x)
    {
        Assert.That(Add(x,0), Is.EqualTo(x));
        Assert.That(Add(0,x), Is.EqualTo(x));
    }
    
    [Property]
    public void Commutativity(int x, int y)
    {
        Assert.That(Add(x,y), Is.EqualTo(Add(y,x)));
    }

    [Property]
    public void D_AlwaysLargerThanInputForPositiveInputs(PositiveInt x, PositiveInt y)
    {
        var sum = Add(x.Item, y.Item);
        Assert.That(sum >= x.Item, Is.True);
        Assert.That(sum >= y.Item, Is.True);
    }
    
    [Property]
    public void E_PeanoSuccessor(int x, int y)
    {
        Assert.That(Add(x, y), Is.EqualTo(Add(1, Add(x, y - 1))));
    }
}