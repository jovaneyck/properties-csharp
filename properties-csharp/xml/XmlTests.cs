using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using PurchaseOrderXml;
using Xunit;

namespace properties_csharp.xml;

public class PurchaseOrderParser
{
    public Result<PurchaseOrderType, Exception[]> Parse(Stream xml)
    {
        var xsdPath = @"C:\projects\properties-csharp\properties-csharp\xml\po.xsd";

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, xsdPath);
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
            ValidationFlags =
                XmlSchemaValidationFlags.ReportValidationWarnings |
                XmlSchemaValidationFlags.ProcessIdentityConstraints |
                XmlSchemaValidationFlags.ProcessInlineSchema |
                XmlSchemaValidationFlags.ProcessSchemaLocation
        };
        List<ValidationEventArgs> validationEvents = [];
        settings.ValidationEventHandler += (sender, e) => { validationEvents.Add(e); };

        using var xmlReader = XmlReader.Create(xml, settings);
        var serializer = new XmlSerializer(typeof(PurchaseOrderType));

        try
        {
            var po = (PurchaseOrderType)serializer.Deserialize(xmlReader)!;
            if (validationEvents.Any())
                return Result<PurchaseOrderType, Exception[]>.Failure(validationEvents
                    .Where(e=>e.Severity == XmlSeverityType.Error)
                    .Select(e=>e.Exception).Cast<Exception>().ToArray());

            return Result<PurchaseOrderType, Exception[]>.Success(po);
        }
        catch (Exception e)
        {
            return Result<PurchaseOrderType, Exception[]>.Failure([e]);
        }
    }

    public Stream Serialize(PurchaseOrderType purchaseOrder)
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "    "
        };
        var stream = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(stream, settings);
        var serializer = new XmlSerializer(typeof(PurchaseOrderType));
        serializer.Serialize(xmlWriter, purchaseOrder, ns);
        stream.Position = 0;
        return stream;
    }
}

public class XmlTests
{
    [Fact]
    public void XmlTest1()
    {
        using var xml = File.OpenRead(@"C:\projects\properties-csharp\properties-csharp\xml\po.xml");

        var parser = new PurchaseOrderParser();

        var result = parser.Parse(xml);
        Assert.True(result.IsSuccess);

        var serialized = parser.Serialize(result.Value!);
        var streamToString = StreamToString(xml);
        var actualMemory = StreamToString(serialized);
        Assert.Equal(streamToString, actualMemory);
    }

    public string StreamToString(Stream stream)
    {
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return text;
    }

    //[Property(Replay = "(6386861553722905307,8220442266619396519)")]
    [Property(Arbitrary = [typeof(PurchaseOrderArbs)])]
    public Property Serialize_Deserialize_Idempotent_Property(PurchaseOrderType po)
    {
        var parser = new PurchaseOrderParser();
        var serialized = parser.Serialize(po);
        var deserialized = parser.Parse(serialized);
        return deserialized.IsSuccess.And(() => Assert.Equivalent(po, deserialized.Value))
            .Label(
                $"serialized: {StreamToString(serialized)}")
            .Label(
                $"parse errors: {string.Join(",", deserialized.Error?.Select(e => $"{e.Message}") ?? Array.Empty<string?>()!)}");
    }

    [Property(Arbitrary = [typeof(PurchaseOrderArbs)])]
    public Property Serialize_Deserialize_HandlesAllInputs_Property(PurchaseOrderArbs.PotentialPurchaseOrderXml xml)
    {
        var parser = new PurchaseOrderParser();
        return Prop
            .ToProperty(() => parser.Parse(ToStream(xml.Xml)))
            .Collect(xml.XmlCase)
            .Label($"case: {xml.XmlCase}")
            .Label($"xml: {xml.Xml}");
    }

    private Stream ToStream(string s)
    {
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(s);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }
}

public record Result<TOk, TError>(TOk? Value, TError? Error)
{
    public static Result<TOk, TError> Success(TOk v)
    {
        return new Result<TOk, TError>(v, default);
    }

    public static Result<TOk, TError> Failure(TError e)
    {
        return new Result<TOk, TError>(default, e);
    }

    public bool IsFailure => Error != null;
    public bool IsSuccess => !IsFailure;
}

public static class PurchaseOrderArbs
{
    public static Arbitrary<PurchaseOrderType> PurchaseOrderArb()
    {
        var xmlStringGen = ArbMap.Default
            .ArbFor<XmlEncodedString>().Generator.Select(s =>
                s.Item.Replace("\r\n", "\n").Replace("\r", "\n"));

        //https://www.datypic.com/sc/xsd/t-xsd_NMTOKEN.html
        var countryGen = Gen.OneOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_:".Select(Gen.Constant)).NonEmptyListOf()
            .Select(c => string.Join("", c));

        var azGen = Gen.OneOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(Gen.Constant));
        var numGen = Gen.Choose(0, 9);
        var partNumGen =
            from num1 in numGen
            from num2 in numGen
            from num3 in numGen
            from az1 in azGen
            from az2 in azGen
            select $"{num1}{num2}{num3}-{az1}{az2}";

        var usAddressArb =
            (from city in xmlStringGen
                from country in Gen.Constant("US")
                from name in xmlStringGen
                from state in xmlStringGen
                from street in xmlStringGen
                from zip in ArbMap.Default.ArbFor<decimal>().Generator
                select new UsAddress
                {
                    City = city,
                    Country = country,
                    Name = name,
                    State = state,
                    Street = street,
                    Zip = zip
                }).ToArbitrary();

        var dateGenerator = ArbMap.Default.ArbFor<DateTime>().Generator
            .Select(d => d.Date);
        var itemArb =
            (from comment in xmlStringGen
                from qty in ArbMap.Default.ArbFor<PositiveInt>().Generator.Select(pi => pi.Item)
                from partNum in partNumGen
                from productName in xmlStringGen
                from shipdate in dateGenerator
                from usPrice in ArbMap.Default.ArbFor<decimal>().Generator
                select new ItemsItem
                {
                    Comment = comment,
                    Quantity = qty.ToString(),
                    ProductName = productName,
                    PartNum = partNum,
                    ShipDate = shipdate,
                    ShipDateSpecified = true,
                    UsPrice = usPrice
                }).ToArbitrary();
        var itemsArb =
            itemArb.Generator.NonEmptyListOf().ToArbitrary();

        var purchaseOrderArb =
            (from comment in xmlStringGen
                from billTo in usAddressArb.Generator
                from shipTo in usAddressArb.Generator
                from orderDate in dateGenerator
                from items in itemsArb.Generator
                select new PurchaseOrderType
                {
                    Comment = comment,
                    BillTo = billTo,
                    ShipTo = shipTo,
                    OrderDate = orderDate,
                    OrderDateSpecified = true,
                    Items = new Collection<ItemsItem>(items.ToArray())
                }).ToArbitrary();

        return purchaseOrderArb;
    }

    public enum Case { Valid, CorruptXml, SchemaValidationProblems }
    public record PotentialPurchaseOrderXml(Case XmlCase, string Xml);

    public static Arbitrary<PotentialPurchaseOrderXml> PurchaseOrderXmlArb()
    {
        return Gen.OneOf(
            CorruptXmlGen(),
            XsdValidationErrorGen(),
            ValidPurchaseOrderXmlGen(),
            ValidPurchaseOrderXmlGen(),
            ValidPurchaseOrderXmlGen(),
            ValidPurchaseOrderXmlGen(),
            ValidPurchaseOrderXmlGen(),
            ValidPurchaseOrderXmlGen())
            .ToArbitrary();
    }
    
    private static Gen<PotentialPurchaseOrderXml> CorruptXmlGen()
    {
        return from valid in ValidPurchaseOrderXmlGen()
        select new PotentialPurchaseOrderXml(Case.CorruptXml, valid.Xml.Replace('>','<'));
    }
    
    private static Gen<PotentialPurchaseOrderXml> XsdValidationErrorGen()
    {
        return PurchaseOrderArb().Generator.Select(valid =>
        {
            valid.Items = null;
            return new PotentialPurchaseOrderXml(Case.SchemaValidationProblems, SerializeToString(valid));
        });
    }

    private static string SerializeToString(PurchaseOrderType po)
    {
        var parser = new PurchaseOrderParser();
        return parser.Serialize(po).StreamToString();
    }

    private static Gen<PotentialPurchaseOrderXml> ValidPurchaseOrderXmlGen()
    {
        return from po in PurchaseOrderArb().Generator
            select new PotentialPurchaseOrderXml(Case.Valid, SerializeToString(po));
    }
}

public static class StreamExtensions {
    public static string StreamToString(this Stream stream)
    {
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return text;
    }
}