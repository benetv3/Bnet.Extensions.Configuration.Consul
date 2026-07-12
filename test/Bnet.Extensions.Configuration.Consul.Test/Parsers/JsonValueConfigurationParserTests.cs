using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TUnit.Core;

namespace Bnet.Extensions.Configuration.Consul.Parsers;

public class JsonValueConfigurationParserTests
{
    private readonly JsonValueConfigurationParser _parser = new();

    public sealed class Parse : JsonValueConfigurationParserTests
    {
        public static IEnumerable<(string, IDictionary<string, string?>)> ObjectTestCases()
        {
            yield return (
                "{\"Key\": \"Value\"}",
                new Dictionary<string, string?> { { "Key", "Value" } });
            yield return (
                "{\"parent\": {\"child\": \"Value\"} }",
                new Dictionary<string, string?> { { "parent", null }, { "parent:child", "Value" } });
            yield return (
                "{\"list\": [ \"First\", \"Second\" ] }",
                new Dictionary<string, string?> { { "list", null }, { "list:0", "First" }, { "list:1", "Second" } });
        }

        [Test]
        [MethodDataSource(nameof(ObjectTestCases))]
        public async Task ShouldParseJsonObjectFromStream(string json, IDictionary<string, string?> expected)
        {
            await using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var result = _parser.Parse(stream);

            await Assert.That(result).IsEquivalentTo(expected);
        }

        [Test]
        [Arguments("\"localhost\"", "localhost")]
        [Arguments("13", "13")]
        [Arguments("true", "True")]
        public async Task ShouldParseScalarRootValueUnderEmptyKey(string json, string expected)
        {
            await using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var result = _parser.Parse(stream);

            await Assert.That(result).IsEquivalentTo(new Dictionary<string, string?> { { string.Empty, expected } });
        }
    }
}
