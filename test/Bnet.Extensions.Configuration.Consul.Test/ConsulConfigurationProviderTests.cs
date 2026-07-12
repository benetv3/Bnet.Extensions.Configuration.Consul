using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul;

public class ConsulConfigurationProviderTests
{
    private readonly IConsulClient _consulClient;
    private readonly IConfigurationParser _parser;
    private readonly ConsulConfigurationProvider _provider;
    private readonly IConsulConfigurationSource _source;

    public ConsulConfigurationProviderTests()
    {
        _consulClient = Substitute.For<IConsulClient>();
        var consulClientFactory = Substitute.For<IConsulClientFactory>();
        consulClientFactory
            .Create()
            .Returns(_consulClient);
        _parser = Substitute.For<IConfigurationParser>();
        _source = new ConsulConfigurationSource("Test")
        {
            Parser = _parser
        };
        _provider = new ConsulConfigurationProvider(
            _source,
            consulClientFactory);
    }

    [After(HookType.Test)]
    public void DisposeProvider()
    {
        _provider.Dispose();
    }

    public sealed class Constructor : ConsulConfigurationProviderTests
    {
        [Test]
        public async Task ShouldThrowIfParserIsNull()
        {
            var source = new ConsulConfigurationSource("Test")
            {
                Parser = null!
            };

            // ReSharper disable once ObjectCreationAsStatement
            Action constructing =
                () =>
                    new ConsulConfigurationProvider(source, Substitute.For<IConsulClientFactory>());

            var ex = await Assert.That(constructing).ThrowsException().WithExceptionType(typeof(ArgumentNullException));
            await Assert.That(ex?.Message).Contains(nameof(IConsulConfigurationSource.Parser));
        }
    }

    public sealed class Dispose : ConsulConfigurationProviderTests
    {
        [Test]
        public async Task ShouldCancelPollingTaskWhenReloading()
        {
            var expectedKvCalls = 0;
            var pollingCancelled = new TaskCompletionSource<bool>();

            _source.ReloadOnChange = true;
            _source.Optional = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var token = callInfo.ArgAt<CancellationToken>(2);

                    if (!pollingCancelled.Task.IsCompleted)
                    {
                        expectedKvCalls++;
                        if (token.CanBeCanceled)
                        {
                            token.Register(() => pollingCancelled.TrySetResult(true));
                        }
                    }

                    return token.IsCancellationRequested
                        ? Task.FromCanceled<QueryResult<ConsulKvPair[]>>(token)
                        : Task.Delay(5, token).ContinueWith(_ => new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.OK }, token);
                });

            _provider.Load();

            // allow polling loop to spin up
            await Task.Delay(25);

            _provider.Dispose();

            await pollingCancelled.Task;

            // It's possible that one additional call to client List endpoint is made depending on when the loop is interrupted.
            var callCount = _consulClient.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "List");
            await Assert.That(callCount).IsGreaterThanOrEqualTo(expectedKvCalls);
            await Assert.That(callCount).IsLessThanOrEqualTo(expectedKvCalls + 1);
        }

        [Test]
        public async Task ShouldNotThrowOnMultipleDisposeCalls()
        {
            _source.ReloadOnChange = true;
            _source.Optional = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.OK }));

            _provider.Load();
            _provider.Dispose();

            Action secondDispose = () => _provider.Dispose();

            await Assert.That(secondDispose).ThrowsNothing();
        }
    }

    public sealed class DoNotReloadOnChange : ConsulConfigurationProviderTests
    {
        public DoNotReloadOnChange()
        {
            _source.ReloadOnChange = false;
        }

        [Test]
        public async Task ShouldCallLoadExceptionWhenConsulReturnsBadRequest()
        {
            var calledOnLoadException = false;
            _source.OnLoadException = ctx =>
            {
                ctx.Ignore = true;
                calledOnLoadException = true;
            };
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new QueryResult<ConsulKvPair[]>
                    {
                        StatusCode = HttpStatusCode.BadRequest
                    }));

            _provider.Load();

            await Assert.That(calledOnLoadException).IsTrue();
        }

        [Test]
        public async Task ShouldCallOnLoadExceptionActionWhenLoadingThrows()
        {
            var calledOnLoadException = false;

            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<QueryResult<ConsulKvPair[]>>(new Exception()));
            _source.OnLoadException = ctx =>
            {
                ctx.Ignore = true;
                calledOnLoadException = true;
            };

            _provider.Load();

            await Assert.That(calledOnLoadException).IsTrue();
        }

        [Test]
        public async Task ShouldHaveEmptyDataWhenConfigDoesNotExistAndIsOptional()
        {
            _source.Optional = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.NotFound }));

            _provider.Load();

            await Assert.That(_provider.GetChildKeys([], string.Empty)).IsEmpty();
        }

        [Test]
        public async Task ShouldNotMakeABlockingCall()
        {
            var allQueryOptions = new List<QueryOptions>();
            _parser
                .Parse(Arg.Any<MemoryStream>())
                .Returns(new Dictionary<string, string?> { { "Key", "Value" } });
            _consulClient
                .List(
                    "Test",
                    Arg.Any<QueryOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    allQueryOptions.Add(callInfo.ArgAt<QueryOptions>(1));
                    return Task.FromResult(
                        new QueryResult<ConsulKvPair[]>
                        {
                            LastIndex = 1234,
                            Response = [
                                new ConsulKvPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                                ],
                            StatusCode = HttpStatusCode.OK
                        });
                });

            _provider.Load();
            _provider.Load();

            await Assert.That(allQueryOptions).IsNotEmpty();
            foreach (var options in allQueryOptions)
            {
                await Assert.That(options.WaitIndex).IsEqualTo((ulong)0);
                await Assert.That(options.WaitTime).IsEqualTo(_source.PollWaitTime);
            }
        }

        [Test]
        public async Task ShouldNotThrowExceptionIfOnLoadExceptionIsSetToIgnore()
        {
            _source.OnLoadException = ctx => ctx.Ignore = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<QueryResult<ConsulKvPair[]>>(new Exception("Failed to load from Consul agent")));

            Action loading = () => _provider.Load();

            await Assert.That(loading).ThrowsNothing();
        }

        [Test]
        public void ShouldReloadWhenNotPolling()
        {
            _source.Optional = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.NotFound }));

            _provider.Load();
            _provider.Load();

            _consulClient.Received(2).List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ShouldSetData()
        {
            _parser
                .Parse(Arg.Any<MemoryStream>())
                .Returns(new Dictionary<string, string?> { { "Key", "Value" } });
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new QueryResult<ConsulKvPair[]>
                    {
                        Response = [
                            new ConsulKvPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            ],
                        StatusCode = HttpStatusCode.OK
                    }));

            _provider.Load();

            _provider.TryGet("Key", out var value);
            await Assert.That(value).IsEqualTo("Value");
        }

        [Test]
        public async Task ShouldSetLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext? exceptionContext = null;
            var exception = new Exception("Failed to load from Consul agent");

            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<QueryResult<ConsulKvPair[]>>(exception));
            _source.OnLoadException = ctx =>
            {
                ctx.Ignore = true;
                exceptionContext = ctx;
            };

            _provider.Load();

            await Assert.That(exceptionContext).IsNotNull();
            await Assert.That(exceptionContext!.Exception).IsEqualTo(exception);
            await Assert.That(exceptionContext.Source).IsEqualTo(_source);
            await Assert.That(exceptionContext.Ignore).IsTrue();
        }

        [Test]
        public async Task ShouldThrowExceptionIfNotIgnoredByClient()
        {
            _source.OnLoadException = ctx => ctx.Ignore = false;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<QueryResult<ConsulKvPair[]>>(new Exception("Error")));

            var loading = (Action)(() => _provider.Load());

            var ex = await Assert.That(loading).ThrowsException().WithExceptionType(typeof(Exception));
            await Assert.That(ex?.Message).IsEqualTo("Error");
        }

        [Test]
        public async Task ShouldThrowWhenConfigDoesNotExistAndIsNotOptional()
        {
            _source.Optional = false;
            _source.OnLoadException = ctx => ctx.Ignore = false;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.NotFound }));

            var loading = (Action)(() => _provider.Load());

            var ex = await Assert.That(loading).ThrowsException().WithExceptionType(typeof(Exception));
            await Assert.That(ex?.Message).IsEqualTo("The configuration for key Test was not found and is not optional.");
        }
    }

    public sealed class ReloadOnChange : ConsulConfigurationProviderTests
    {
        public ReloadOnChange()
        {
            _source.ReloadOnChange = true;
        }

        [Test]
        public async Task ShouldCallOnWatchExceptionWithCountOfConsecutiveFailures()
        {
            var exceptionContexts = new List<ConsulWatchExceptionContext>();
            _source.Optional = true;
            _source.OnWatchException = ctx =>
            {
                exceptionContexts.Add(ctx);
                return TimeSpan.Zero;
            };

            var pollingCompleted = new TaskCompletionSource<bool>();

            var exception1 = new Exception("Error during watch 1.");
            var exception2 = new Exception("Error during watch 2.");
            var callCount = 0;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult(new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK });
                    }

                    if (callCount == 2)
                    {
                        return Task.FromException<QueryResult<ConsulKvPair[]>>(exception1);
                    }

                    if (callCount == 3)
                    {
                        return Task.FromException<QueryResult<ConsulKvPair[]>>(exception2);
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await Assert.That(exceptionContexts.Count).IsEqualTo(2);
            await Assert.That(exceptionContexts[0].Exception).IsEqualTo(exception1);
            await Assert.That(exceptionContexts[0].ConsecutiveFailureCount).IsEqualTo(1);
            await Assert.That(exceptionContexts[0].Source).IsEqualTo(_source);
            await Assert.That(exceptionContexts[1].Exception).IsEqualTo(exception2);
            await Assert.That(exceptionContexts[1].ConsecutiveFailureCount).IsEqualTo(2);
            await Assert.That(exceptionContexts[1].Source).IsEqualTo(_source);
        }

        [Test]
        public async Task ShouldNotOverwriteNonOptionalConfigIfDoesNotExist()
        {
            var pollingCompleted = new TaskCompletionSource<bool>();
            _source.Optional = false;
            var callCount = 0;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult(
                            new QueryResult<ConsulKvPair[]>
                            {
                                Response = [
                                    new ConsulKvPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                                    ],
                                StatusCode = HttpStatusCode.OK
                            });
                    }

                    if (callCount == 2)
                    {
                        return Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.NotFound });
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });
            _parser
                .Parse(Arg.Any<MemoryStream>())
                .Returns(new Dictionary<string, string?> { { "Key", "Test" } });

            _provider.Load();

            await pollingCompleted.Task;

            var found = _provider.TryGet("Key", out _);
            await Assert.That(found).IsTrue();
        }

        [Test]
        public void ShouldNotReloadWhenPolling()
        {
            _source.Optional = true;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK }));

            _provider.Load();
            _provider.Load();

            _consulClient.Received(1).List(
                "Test",
                Arg.Is<QueryOptions>(options => options.WaitIndex == 0),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ShouldReloadConfigWhenDataInConsulHasChanged()
        {
            var reload = new TaskCompletionSource<bool>();
            _provider
                .GetReloadToken()
                .RegisterChangeCallback(_ => reload.TrySetResult(true), new object());
            var incompleteTask = new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
            var callCount = 0;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult(
                            new QueryResult<ConsulKvPair[]>
                            {
                                LastIndex = 12,
                                Response = [
                                    new ConsulKvPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                                    ],
                                StatusCode = HttpStatusCode.OK
                            });
                    }

                    if (callCount == 2)
                    {
                        return Task.FromResult(
                            new QueryResult<ConsulKvPair[]>
                            {
                                LastIndex = 13,
                                Response = [
                                    new ConsulKvPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                                    ],
                                StatusCode = HttpStatusCode.OK
                            });
                    }

                    return incompleteTask;
                });
            var parseCallCount = 0;
            _parser
                .Parse(Arg.Any<Stream>())
                .Returns(_ =>
                {
                    parseCallCount++;
                    return parseCallCount == 1
                        ? new Dictionary<string, string?> { { "Key", "Test" } }
                        : new Dictionary<string, string?> { { "Key", "Test2" } };
                });

            _provider.Load();

            await reload.Task;

            _provider.TryGet("Key", out var value);
            await Assert.That(value).IsEqualTo("Test2");
        }

        [Test]
        public async Task ShouldResetConsecutiveFailureCountAfterASuccessfulPoll()
        {
            var exceptionContexts = new List<ConsulWatchExceptionContext>();
            _source.Optional = true;
            _source.OnWatchException = ctx =>
            {
                exceptionContexts.Add(ctx);
                return TimeSpan.Zero;
            };

            var pollingCompleted = new TaskCompletionSource<bool>();

            var exception1 = new Exception("Error during watch 1.");
            var exception2 = new Exception("Error during watch 2.");
            var callCount = 0;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult(new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK });
                    }

                    if (callCount == 2)
                    {
                        return Task.FromException<QueryResult<ConsulKvPair[]>>(exception1);
                    }

                    if (callCount == 3)
                    {
                        return Task.FromResult(new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK });
                    }

                    if (callCount == 4)
                    {
                        return Task.FromException<QueryResult<ConsulKvPair[]>>(exception2);
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await Assert.That(exceptionContexts.Count).IsEqualTo(2);
            await Assert.That(exceptionContexts[0].Exception).IsEqualTo(exception1);
            await Assert.That(exceptionContexts[0].ConsecutiveFailureCount).IsEqualTo(1);
            await Assert.That(exceptionContexts[0].Source).IsEqualTo(_source);
            await Assert.That(exceptionContexts[1].Exception).IsEqualTo(exception2);
            await Assert.That(exceptionContexts[1].ConsecutiveFailureCount).IsEqualTo(1);
            await Assert.That(exceptionContexts[1].Source).IsEqualTo(_source);
        }

        private static async Task AssertQueryOptionsMatch(List<QueryOptions> actual, IReadOnlyList<QueryOptions> expected)
        {
            await Assert.That(actual.Count).IsEqualTo(expected.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                await Assert.That(actual[i].WaitIndex).IsEqualTo(expected[i].WaitIndex);
                await Assert.That(actual[i].WaitTime).IsEqualTo(expected[i].WaitTime);
            }
        }

        [Test]
        public async Task ShouldResetLastIndexWhenItGoesBackwards()
        {
            _source.Optional = true;
            var queryOptions = new List<QueryOptions>();
            var pollingCompleted = new TaskCompletionSource<bool>();

            var results = new Queue<QueryResult<ConsulKvPair[]>>(
                new List<QueryResult<ConsulKvPair[]>>
                {
                        new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK },
                        new QueryResult<ConsulKvPair[]> { LastIndex = 12, StatusCode = HttpStatusCode.OK }
                });
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var options = callInfo.ArgAt<QueryOptions>(1);
                    queryOptions.Add(options);
                    if (results.TryDequeue(out var result))
                    {
                        return Task.FromResult(result);
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await AssertQueryOptionsMatch(
                queryOptions,
                [
                    new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime }
                ]);
        }

        [Test]
        public async Task ShouldSetLastIndexToOneWhenConsulReturnsIndexNotGreaterThanZero()
        {
            _source.Optional = true;
            var queryOptions = new List<QueryOptions>();
            var pollingCompleted = new TaskCompletionSource<bool>();

            var results = new Queue<QueryResult<ConsulKvPair[]>>(
                new List<QueryResult<ConsulKvPair[]>>
                {
                        new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK },
                        new QueryResult<ConsulKvPair[]> { LastIndex = 0, StatusCode = HttpStatusCode.OK },
                        new QueryResult<ConsulKvPair[]> { LastIndex = 0, StatusCode = HttpStatusCode.OK }
                });
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var options = callInfo.ArgAt<QueryOptions>(1);
                    queryOptions.Add(options);
                    if (results.TryDequeue(out var result))
                    {
                        return Task.FromResult(result);
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await AssertQueryOptionsMatch(
                queryOptions,
                [
                    new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 1, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 1, WaitTime = _source.PollWaitTime }
                ]);
        }

        [Test]
        public async Task ShouldWaitForChangesAfterInitialLoad()
        {
            _source.Optional = true;
            var queryOptions = new List<QueryOptions>();
            var pollingCompleted = new TaskCompletionSource<bool>();

            var results = new Queue<QueryResult<ConsulKvPair[]>>(
                new List<QueryResult<ConsulKvPair[]>>
                {
                        new QueryResult<ConsulKvPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK }
                });
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var options = callInfo.ArgAt<QueryOptions>(1);
                    queryOptions.Add(options);
                    if (results.TryDequeue(out var result))
                    {
                        return Task.FromResult(result);
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await AssertQueryOptionsMatch(
                queryOptions,
                [
                    new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                    new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime }
                ]);
        }

        [Test]
        public async Task ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
        {
            var pollingCompleted = new TaskCompletionSource<bool>();
            _source.Optional = true;
            var callCount = 0;
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult(new QueryResult<ConsulKvPair[]> { StatusCode = HttpStatusCode.OK });
                    }

                    pollingCompleted.SetResult(true);
                    return new TaskCompletionSource<QueryResult<ConsulKvPair[]>>().Task;
                });

            _provider.Load();

            await pollingCompleted.Task;

            await _consulClient.Received(2).List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>());
        }
    }

    public sealed class CustomizeConvertConsulKvPairToConfig : ConsulConfigurationProviderTests
    {
        public CustomizeConvertConsulKvPairToConfig()
        {
            _source.ReloadOnChange = false;
        }

        [Test]
        public async Task ShouldSetData()
        {
            _parser
                .Parse(Arg.Any<MemoryStream>())
                .Throws(new Exception("Should not get here..."));
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new QueryResult<ConsulKvPair[]>
                    {
                        Response = [
                            new ConsulKvPair("Test/key__with__double__underscores") { Value = Encoding.UTF8.GetBytes("Value") }
                            ],
                        StatusCode = HttpStatusCode.OK
                    }));

            _source.ConvertConsulKvPairToConfig = kvPair =>
            {
                var normalizedKey = kvPair.Key
                                          .Replace("__", ":")
                                          .Replace(_source.KeyToRemove, string.Empty)
                                          .Trim('/');

                using Stream valueStream = new MemoryStream(kvPair.Value!);
                using var streamReader = new StreamReader(valueStream);
                var parsedValue = streamReader.ReadToEnd();

                return new Dictionary<string, string?>()
                {
                        { normalizedKey, parsedValue }
                };
            };

            _provider.Load();

            _provider.TryGet("key:with:double:underscores", out var value);
            await Assert.That(value).IsEqualTo("Value");
        }

        [Test]
        public async Task ShouldSetDataUsingDefinedParser()
        {
            _parser
                .Parse(Arg.Any<MemoryStream>())
                .Returns(new Dictionary<string, string?> { { string.Empty, "Value" } });
            _consulClient
                .List("Test", Arg.Any<QueryOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new QueryResult<ConsulKvPair[]>
                    {
                        Response = [
                            new ConsulKvPair("Test/key__with__double__underscores") { Value = Encoding.UTF8.GetBytes("Value") }
                            ],
                        StatusCode = HttpStatusCode.OK
                    }));

            _source.ConvertConsulKvPairToConfig = kvPair =>
            {
                var normalizedKey = kvPair.Key
                                          .Replace("__", ":")
                                          .Replace(_source.KeyToRemove, string.Empty)
                                          .Trim('/');

                using Stream valueStream = new MemoryStream(kvPair.Value!);
                var parsedPairs = _source.Parser.Parse(valueStream);
                return parsedPairs.Select(parsedPair =>
                {
                    return new KeyValuePair<string, string?>(
                        $"{normalizedKey}/{parsedPair.Key}".Trim('/'),
                        parsedPair.Value);
                });
            };

            _provider.Load();

            _provider.TryGet("key:with:double:underscores", out var value);
            await Assert.That(value).IsEqualTo("Value");
        }
    }
}
