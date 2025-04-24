using FsCheck;
using FsCheck.Fluent;
using Xunit;
using PropertyAttribute = FsCheck.Xunit.PropertyAttribute;

namespace properties_csharp;

public class SearchEngineTests
{
    public static string[] Search(string[] www, string[] terms)
    {
        return www
            .Where(page => terms.All(page.Contains))
            .ToArray();
    }
    
    [Property(StartSize = 100)]
    public Property StricterQueriesNarrowResultSet_Property()
    {
        var keywordGen = 
            Gen.OneOf(
                Gen.Constant("keyword1"), 
                Gen.Constant("keyword2"));
        
        var termGen = 
            Gen.Frequency(
                (25,keywordGen), 
                (75, ArbMap.Default.GeneratorFor<NonWhiteSpaceString>().Select(s=>s.Item)));
        
        var pageGen = termGen.NonEmptyListOf().Select(terms => string.Join(" ", terms));
        var pagesGen = pageGen.ArrayOf();

        return Prop.ForAll(
            pagesGen.ToArbitrary(),
            (pages) =>
            {
                var broadSearch = Search(pages, ["keyword1"]);
                var narrowSearch = Search(pages, ["keyword1","keyword2"]);
                return Prop
                    .ToProperty(() => Assert.Subset(broadSearch.ToHashSet(), narrowSearch.ToHashSet()))
                    .Collect($"#pages: {pages.Length} #broad: {broadSearch.Length} #narrow: {narrowSearch.Length}");
            });
    }
}