using System.Collections.ObjectModel;
using System.Runtime.Intrinsics.Arm;
using System.Security.Authentication;
using System.Text;
using System.Text.Unicode;
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
    public Result<PurchaseOrderType, ValidationEventArgs[]> Parse(Stream xml)
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
        var po = (PurchaseOrderType)serializer.Deserialize(xmlReader)!;

        if (validationEvents.Any())
            return Result<PurchaseOrderType, ValidationEventArgs[]>.Failure(validationEvents.ToArray());

        return Result<PurchaseOrderType, ValidationEventArgs[]>.Ok(po);
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
        StreamReader reader = new StreamReader(stream);
        string text = reader.ReadToEnd();
        return text;
    }

    [Property]
    public Property Serialize_Deserialize_Idempotent_Property()
    {
        var stringGen = ArbMap.Default.ArbFor<XmlEncodedString>().Generator.Select(s=>s.Item);

        //https://www.datypic.com/sc/xsd/t-xsd_NMTOKEN.html
        var countryGen = Gen.NonEmptyListOf(
            Gen.OneOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_:".Select(Gen.Constant)))
            .Select(c=>string.Join("",c));

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
            (from city in stringGen
            from country in countryGen
            from name in stringGen
            from state in stringGen
            from street in stringGen
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
        
        var itemArb =
            (from comment in stringGen
                from qty in ArbMap.Default.ArbFor<PositiveInt>().Generator.Select(pi=>pi.Item)
                from partNum in partNumGen
                from productName in stringGen
                from shipdate in ArbMap.Default.ArbFor<DateTime>().Generator
                from usPrice in ArbMap.Default.ArbFor<decimal>().Generator
                select new ItemsItem
                {
                    Comment = comment,
                    Quantity = qty.ToString(),
                    ProductName = productName,
                    PartNum =  partNum,
                    ShipDate = shipdate,
                    UsPrice = usPrice
                }).ToArbitrary();
        var itemsArb =
            itemArb.Generator.ListOf().ToArbitrary();
        
        var purchaseOrderArb =
            (from comment in stringGen
                from billTo in usAddressArb.Generator
                from shipTo in usAddressArb.Generator
                from orderDate in ArbMap.Default.ArbFor<DateTime>().Generator
                from items in itemsArb.Generator
                select new PurchaseOrderType
                {
                    Comment = comment,
                    BillTo = billTo,
                    ShipTo = shipTo,
                    OrderDate = orderDate,
                    Items = new Collection<ItemsItem>(items.ToArray())
                }).ToArbitrary();
        
        
        return Prop.ForAll(
            purchaseOrderArb,
            po =>
            {
                var parser = new PurchaseOrderParser();
                var serialized = parser.Serialize(po);
                var deserialized = parser.Parse(serialized);
                
                return (deserialized.IsSuccess)
                    .Label($"{StreamToString(serialized)} - {string.Join(",",deserialized.Error?.Select(e=>$"{e.Severity}: {e.Message}") ?? Array.Empty<string?>()!)}");
            });
    }
}

public record Result<TOk, TError>(TOk? Value, TError? Error)
{
    public static Result<TOk, TError> Ok(TOk v)
    {
        return new Result<TOk, TError>(v, default);
    }

    public static Result<TOk, TError> Failure(TError e)
    {
        return new Result<TOk, TError>(default, e);
    }

    public bool IsError => Error != null;
    public bool IsSuccess => !IsError;
}