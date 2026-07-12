using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TUnit.Core;

namespace Bnet.Extensions.Configuration.Consul.Parsers;

public class JsonConfigurationParserTests
{
    private readonly JsonConfigurationParser _parser = new();

    public sealed class Parse : JsonConfigurationParserTests
    {
        public static IEnumerable<(string, IDictionary<string, string?>)> TestCases()
        {
            yield return (
                "{\"Key\": \"Value\"}",
                new Dictionary<string, string?> { { "Key", "Value" } });
            yield return (
                "{\"parent\": {\"child\": \"Value\"} }",
                new Dictionary<string, string?> { { "parent:child", "Value" } });
            yield return (
                "{\"server\": {\"ip\": \"192.168.0.1\", \"port\": 5000} }",
                new Dictionary<string, string?> { { "server:ip", "192.168.0.1" }, { "server:port", "5000" } });
            yield return (
                "{\"Key/WithSlash\": \"Value\"}",
                new Dictionary<string, string?> { { "Key/WithSlash", "Value" } });
        }

        [Test]
        [MethodDataSource(nameof(TestCases))]
        public async Task ShouldParseSimpleJsonFromStream(string json, IDictionary<string, string?> expected)
        {
            await using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var result = _parser.Parse(stream);

            await Assert.That(result).IsEquivalentTo(expected);
        }

        [Test]
        [Arguments("\"localhost\"")]
        [Arguments("13")]
        [Arguments("true")]
        public async Task ShouldThrowWhenRootIsNotAnObject(string json)
        {
            await using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // ReSharper disable once AccessToDisposedClosure
            Action parsing = () => _parser.Parse(stream);

            await Assert.That(parsing).ThrowsException();
        }
    }
}
