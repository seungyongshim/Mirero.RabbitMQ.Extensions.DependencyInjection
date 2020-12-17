![CI](https://github.com/seungyongshim/Mirero.RabbitMQ.Extensions.DependencyInjection/workflows/CI/badge.svg)

## Innovation
- https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/970

## Configuration Hocon
```json
rabbitmq.connection
{
	hostname = "127.0.0.1"
	vhost = "/"
	username = "username"
	password = "password"
}
```

## Publish Topic  
```csharp
[Fact]
public async Task PublishTopic()
{
    const string topicName = "topic";

    var host = Host.CreateDefaultBuilder()
                   .ConfigureAppConfiguration(config =>
                   {
                       config.AddHoconFile("config.hocon");
                   })
                   .ConfigureServices((context,services) =>
                   {
                       services.AddRabbitMQ(context.Configuration, model =>
                       {
                           model.QueueDeclare(topicName, false, false, false, null);
                       });
                   })
                   .Build();

    await host.StartAsync();

    using (var publisher = host.Services.GetService<IMQPublisher>())
    {
        await publisher.TellAsync(topicName, "Hello");
    }

    await host.StopAsync();
}
```

## Subscribe Topic
```csharp
[Fact]
public async Task SubscribeTopic()
{
    const string topicName = "topic";

    var host = Host.CreateDefaultBuilder()
                   .ConfigureAppConfiguration(config =>
                   {
                       config.AddHoconFile("config.hocon");
                   })
                   .ConfigureServices((context,services) =>
                   {
                       services.AddRabbitMQ(context.Configuration, model =>
                       {
                           model.QueueDeclare(topicName, false, false, false, null);
                       });
                   })
                   .Build();

    await host.StartAsync();

    using (var receiver = host.Services.GetService<IMQReceiver>())
    {
        receiver.StartListening(topicName);
        
        var (message, commit) = await receiver.ReceiveAsync<string>(10.Seconds());
        message.Should().Be("Hello");

        await commit.Ack();
    }
        
    await host.StopAsync();
}
```

## RPC
```csharp
[Fact]
public async Task RpcTopic()
{
    const string topicName = "topic";

    var host = Host.CreateDefaultBuilder()
                   .ConfigureAppConfiguration(config =>
                   {
                       config.AddHoconFile("config.hocon");
                   })
                   .ConfigureServices((context,services) =>
                   {
                       services.AddRabbitMQ(context.Configuration, model =>
                       {
                           model.QueueDeclare(topicName, false, false, false, null);
                       });
                   })
                   .Build();

    await host.StartAsync();

    using(var rpc = host.Services.GetService<IMQRpcClient>())
    {

        var (res, commit) = await rpc.AskAsync<string>(topicName, "Hello", 10.Seconds());

        res.Should().Be("World");

        await commit.Ack();
    }
        
    await host.StopAsync();
}
```
