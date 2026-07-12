using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul.Extensions;

public class KvPairExtensionsTests
{
    public sealed class ConvertConsulKvPairToConfig : KvPairExtensionsTests
    {
        private readonly IConfigurationParser _parserMock = Substitute.For<IConfigurationParser>();

        public static IEnumerable<(string, string, Dictionary<string, string?>, IEnumerable<KeyValuePair<string, string>>)> ConvertConsulKvPairToConfigTestCases()
        {
            yield return (
                "rootKey",
                "rootKey",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Key", "value")]);
            yield return (
                "rootKey",
                "rootKey/",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Key", "value")]);
            yield return (
                "rootKey",
                "rootKey/Key",
                new Dictionary<string, string?> { { string.Empty, "value" } },
                    [new KeyValuePair<string, string>("Key", "value")]);
            yield return (
                "RootKey/Settings",
                "RootKey/Settings/Root/",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Root:Key", "value")]);
            yield return (
                "rootKey",
                "rootKey",
                new Dictionary<string, string?> { { "Section:Property", "value" } },
                    [new KeyValuePair<string, string>("Section:Property", "value")]);
            yield return (
                "rootKey",
                "rootKey/Section",
                new Dictionary<string, string?> { { "SubSection:Property", "value" } },
                    [new KeyValuePair<string, string>("Section:SubSection:Property", "value")]);
            yield return (
                "rootKey",
                "rootKey/Section/SubSection",
                new Dictionary<string, string?> { { "Property", "value" } },
                    [new KeyValuePair<string, string>("Section:SubSection:Property", "value")]);
            yield return (
                "rootKey",
                "rootKey/Section/SubSection",
                new Dictionary<string, string?> { { "Property", "1" }, { "AnotherSubSection:Property", "2" } },
                    [
                        new KeyValuePair<string, string>("Section:SubSection:Property", "1"),
                    new KeyValuePair<string, string>("Section:SubSection:AnotherSubSection:Property", "2")
                        ]);
            yield return (
                "path/to/rootKey",
                "path/to/rootKey",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Key", "value")]);
            yield return (
                "path/to/rootKey",
                "path/to/rootKey/",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Key", "value")]);
            yield return (
                "path/to/rootKey",
                "path/to/rootKey/Section",
                new Dictionary<string, string?> { { "Key", "value" } },
                    [new KeyValuePair<string, string>("Section:Key", "value")]);
            yield return (
                string.Empty,
                "Root/Section",
                new Dictionary<string, string?> { { "JsonKey/With/Slash", "value" } },
                    [new KeyValuePair<string, string>("Root:Section:JsonKey/With/Slash", "value")]);
        }

        [Test]
        [MethodDataSource(nameof(ConvertConsulKvPairToConfigTestCases))]
        public async Task ShouldConvertKvPairToConfigCorrectly(
            string rootKey,
            string kvPairKey,
            Dictionary<string, string?> parsedConfig,
            IEnumerable<KeyValuePair<string, string?>> expected)
        {
            _parserMock
                .Parse(Arg.Any<Stream>())
                .Returns(parsedConfig);
            var kvPair = new ConsulKvPair(kvPairKey)
            {
                Value = [
                    1
                    ]
            };

            var config = kvPair.ConvertToConfig(rootKey, _parserMock);

            await Assert.That(config).IsEquivalentTo(expected);
        }

        [Test]
        public async Task ShouldThrowIfTheRootKeyPointsToASingleValue()
        {
            _parserMock
                .Parse(Arg.Any<Stream>())
                .Returns(new Dictionary<string, string?> { { string.Empty, "value" } });
            var kvPair = new ConsulKvPair("rootKey")
            {
                Value = [
                    1
                    ]
            };

            var ex = await Assert.That(() => kvPair.ConvertToConfig("rootKey", _parserMock).ToList()).ThrowsException();

            await Assert.That(ex?.Message).IsEqualTo(
                "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
        }
    }

    public sealed class HasValue : KvPairExtensionsTests
    {
        public static IEnumerable<(ConsulKvPair, bool)> TestCases()
        {
            yield return (new ConsulKvPair("key") { Value = null }, false);
            yield return (new ConsulKvPair("key") { Value = [] }, false);
            yield return (new ConsulKvPair("key/") { Value = [1] }, false);
            yield return (new ConsulKvPair("key") { Value = [1] }, true);
        }

        [Test]
        [MethodDataSource(nameof(TestCases))]
        public async Task ShouldReturnTrueIfKvPairIsLeafWithNonEmptyArrayValue(ConsulKvPair kvPair, bool expected)
        {
            var hasValue = kvPair.HasValue();

            await Assert.That(hasValue).IsEqualTo(expected);
        }
    }

    public sealed class IsLeafNode : KvPairExtensionsTests
    {
        [Test]
        [Arguments("key", true)]
        [Arguments("key/", false)]
        public async Task ShouldBeTrueIfKeyDoesNotEndWithAForwardSlash(string key, bool expected)
        {
            var kvPair = new ConsulKvPair(key);

            var isLeafNode = kvPair.IsLeafNode();

            await Assert.That(isLeafNode).IsEqualTo(expected);
        }
    }
}
