using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TUnit.Core;

namespace Bnet.Extensions.Configuration.Consul.Parsers;

public class SimpleConfigurationParserTests
{
    private readonly SimpleConfigurationParser _parser = new();

    public sealed class Parse : SimpleConfigurationParserTests
    {
        public static IEnumerable<(string, string)> TestCases()
        {
            yield return ("value", "value");
            yield return ("", "");
            yield return ("line1\nline2", "line1\nline2");
            yield return ("{\"Key\": \"Value\"}", "{\"Key\": \"Value\"}");
        }

        [Test]
        [MethodDataSource(nameof(TestCases))]
        public async Task ShouldParseValueFromStreamUnderEmptyKey(string content, string expected)
        {
            await using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var result = _parser.Parse(stream);

            await Assert.That(result).IsEquivalentTo(new Dictionary<string, string?> { { string.Empty, expected } });
        }
    }
}
