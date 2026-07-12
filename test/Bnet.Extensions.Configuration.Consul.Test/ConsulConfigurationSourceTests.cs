using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul;

public class ConsulConfigurationSourceTests
{
    public sealed class Constructor : ConsulConfigurationSourceTests
    {
        [Test]
        public async Task ShouldHaveJsonConfigurationParserByDefault()
        {
            var source = new ConsulConfigurationSource("Key");

            await Assert.That(source.Parser.GetType()).IsEqualTo(typeof(JsonConfigurationParser));
        }

        [Test]
        public async Task ShouldSetKey()
        {
            var source = new ConsulConfigurationSource("Key");

            await Assert.That(source.Key).IsEqualTo("Key");
        }

        [Test]
        public async Task ShouldSetKeyToRemoveToKeyByDefault()
        {
            var source = new ConsulConfigurationSource("Key");

            await Assert.That(source.KeyToRemove).IsEqualTo("Key");
        }

        [Test]
        public async Task ShouldSetOptionalToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key");

            await Assert.That(source.Optional).IsFalse();
        }

        [Test]
        public async Task ShouldSetReloadOnChangeToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key");

            await Assert.That(source.ReloadOnChange).IsFalse();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("   ")]
        public async Task ShouldThrowIfKeyIsInvalid(string? key)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action constructing = () => new ConsulConfigurationSource(key!);

            var ex = await Assert.That(constructing).ThrowsException();
            await Assert.That(ex?.Message).Contains("key");
        }

        [Test]
        public async Task ShouldSetDefaultConvertConsulKvPairToConfigStrategy()
        {
            var source = new ConsulConfigurationSource("Key");

            var consulKvPair = new ConsulKvPair("key") { Value = "{\"a\": \"b\", \"c\": \"d\"}"u8.ToArray() };

            var result = source.ConvertConsulKvPairToConfig(consulKvPair);
            await Assert.That(result)
                        .IsEquivalentTo(new Dictionary<string, string?> { { "key:a", "b" }, { "key:c", "d" } });
        }
    }
}
