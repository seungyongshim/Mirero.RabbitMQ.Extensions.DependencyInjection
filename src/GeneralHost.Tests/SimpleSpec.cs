using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using System.Threading;
using FluentAssertions.Extensions;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hocon.Extensions.Configuration;
using AutoFixture.Xunit2;
using AutoFixture;
using System.Linq;

namespace GeneralHost.Tests
{
    public class SimpleSpec
    {
        public class TestMessage
        {
            public TestMessage(string value) => Value = value;
            public string Value { get; }
        }

        [Theory]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        public async Task Simple(Generator<TestMessage> messageGenerator)
        {
            const string topicName = "test.simple-case";

            var messageFixtures = messageGenerator.Take(2).ToArray();


            var host = Host.CreateDefaultBuilder()
                           .ConfigureAppConfiguration(config =>
                           {
                               config.AddHoconFile("test.hocon");
                           })
                           .ConfigureServices((context,services) =>
                           {
                               services.AddRabbitMQ(context.Configuration, model =>
                               {
                                   model.QueueDelete(topicName, false, false);
                                   model.QueueDeclare(topicName, false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            // 메시지 전송
            using (var publisher = host.Services.GetService<IMQPublisher>())
            {
                await publisher.TellAsync(topicName, new[] { "Hello", "World" });
                await publisher.TellAsync(topicName, messageFixtures[0]);
                await publisher.TellAsync(topicName, messageFixtures[1]);
                await publisher.TellAsync(topicName, 1);
            }

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                // 메시지 수신 시작
                receiver.StartListening(topicName);

                {
                    var (message, commit) = await receiver.ReceiveAsync<IEnumerable<string>>(2.Seconds());
                    message.Should().BeSubsetOf(new[] {"Hello", "World"});

                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.Should().BeOfType<TestMessage>();
                    message.As<TestMessage>().Value.Should().Be(messageFixtures[0].Value);

                    await commit.Nack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.Should().BeOfType<TestMessage>();
                    message.As<TestMessage>().Value.Should().Be(messageFixtures[0].Value);
                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync<TestMessage>(2.Seconds());
                    message.Value.Should().Be(messageFixtures[1].Value);
                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.As<long>().Should().Be(1);
                    await commit.Ack();
                }
            }

            await host.StopAsync(1.Seconds());
            await Task.Delay(500);
        }
    }
}
