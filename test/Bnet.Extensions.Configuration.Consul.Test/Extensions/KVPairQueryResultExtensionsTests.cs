using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TUnit.Core;

namespace Bnet.Extensions.Configuration.Consul.Extensions;

public class KvPairQueryResultExtensionsTests
{
    public sealed class HasValue : KvPairQueryResultExtensionsTests
    {
        public static IEnumerable<(object?, bool)> TestCases()
        {
            yield return (null, false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    StatusCode = HttpStatusCode.NotFound
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = null!,
                    StatusCode = HttpStatusCode.OK
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = [],
                    StatusCode = HttpStatusCode.OK
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = [new ConsulKvPair("Key") { Value = null }],
                    StatusCode = HttpStatusCode.OK
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = [new ConsulKvPair("Key/")],
                    StatusCode = HttpStatusCode.OK
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = [new ConsulKvPair("Key") { Value = [] }],
                    StatusCode = HttpStatusCode.OK
                },
                false);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response = [new ConsulKvPair("Key") { Value = [1] }],
                    StatusCode = HttpStatusCode.OK
                },
                true);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response =
                    [
                        new ConsulKvPair("Key1") { Value = [1] },
                        new ConsulKvPair("Key2")
                    ],
                    StatusCode = HttpStatusCode.OK
                },
                true);
            yield return (
                new QueryResult<ConsulKvPair[]>
                {
                    Response =
                    [
                        new ConsulKvPair("Key1") { Value = [1] },
                        new ConsulKvPair("Key2/")
                    ],
                    StatusCode = HttpStatusCode.OK
                },
                true);
        }

        [Test]
        [MethodDataSource(nameof(TestCases))]
        public async Task ShouldBeTrueWhenThereIsAtLeastOneLeafNodeWithSomeData(
            object? queryResultData,
            bool expected)
        {
            var queryResult = (QueryResult<ConsulKvPair[]>?)queryResultData;

            var hasValue = queryResult.HasValue();

            await Assert.That(hasValue).IsEqualTo(expected);
        }
    }

    public class ToConfigDictionary : KvPairQueryResultExtensionsTests
    {
        [Test]
        public async Task ShouldBeEmptyIfResponseIsNull()
        {
            var result = new QueryResult<ConsulKvPair[]>
            {
                StatusCode = HttpStatusCode.OK
            };

            var config = result.ToConfigDictionary(_ => new Dictionary<string, string?> { { "key", "value" } });

            await Assert.That(config).IsEmpty();
        }

        [Test]
        public async Task ShouldNotParseIfConfigBytesIsNull()
        {
            var result = new QueryResult<ConsulKvPair[]>
            {
                Response = [
                    new ConsulKvPair("path/test") { Value = new List<byte>().ToArray() }
                    ],
                StatusCode = HttpStatusCode.OK
            };

            var config = result.ToConfigDictionary(_ => new Dictionary<string, string?> { { "key", "value" } });

            await Assert.That(config).IsEmpty();
        }

        [Test]
        [Arguments("Key")]
        [Arguments("KEY")]
        [Arguments("key")]
        [Arguments("KeY")]
        public async Task ShouldParseIntoCaseInsensitiveDictionary(string key)
        {
            var result = new QueryResult<ConsulKvPair[]>
            {
                Response = [
                    new ConsulKvPair("path/test") { Value = new List<byte> { 1 }.ToArray() }
                    ],
                StatusCode = HttpStatusCode.OK
            };

            var config = result.ToConfigDictionary(_ => new Dictionary<string, string?> { { "kEy", "value" } });

            await Assert.That(config.ContainsKey(key)).IsTrue();
        }
    }
}
