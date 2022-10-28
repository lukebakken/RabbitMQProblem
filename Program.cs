using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vendeq.Api.Common.Messages;

namespace RabbitMQProblem;

class Program
{
    public static JsonSerializerSettings SerializerSettings { get; } =
        new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters =
            {
                new StringEnumConverter()
            },
            NullValueHandling = NullValueHandling.Ignore
        };


    static readonly ConcurrentDictionary<ulong, TaskCompletionSource> _published = new();

    public async static Task SendOne(IModel channel)
    {
        var mtMessage = CreateTestMessage();

        channel.BasicAcks += (object? model, BasicAckEventArgs e) =>
        {
            if (e.Multiple)
            {
                foreach (var id in _published.Keys.Where(x => x <= e.DeliveryTag))
                {
                    if (_published.TryRemove(id, out var value))
                        // value.Acknowledged();
                        value.SetResult();
                }
            }
            else
            {
                if (_published.TryRemove(e.DeliveryTag, out var value))
                    // value.Acknowledged();
                    value.SetResult();
            }
        };
        channel.BasicNacks += (object? Model, BasicNackEventArgs e) =>
        {
            Console.WriteLine($"unexpected NACK tag={e.DeliveryTag}, multiple={e.Multiple}");
        };
        string exchange = "Vendeq.Api.Common.Messages:AuditEvent";
        string routeKey = "";
        bool mandatory = false;
        var msgJson = JsonConvert.SerializeObject(mtMessage, SerializerSettings);
        var msgBytes = Encoding.UTF8.GetBytes(msgJson);

        var basicProperties = channel.CreateBasicProperties();
        basicProperties.ContentType = "application/vnd.masstransit+json";
        basicProperties.MessageId = "55740000-5058-c8f7-3d10-08dab75e201a";
        basicProperties.DeliveryMode = 2;
        basicProperties.Persistent = true;
        var publishTag = channel.NextPublishSeqNo;
        basicProperties.Headers = new Dictionary<string, object> { { "publishId", publishTag.ToString("F0") } };

        var tcs = new TaskCompletionSource();
        _published.AddOrUpdate(publishTag, key => tcs, (key, existing) =>
        {
            throw new Exception("Duplicate key: {key}");
        });

        channel.BasicPublish(exchange, routeKey, mandatory, basicProperties, msgBytes);
        // channel.WaitForConfirmsOrDie();
        await tcs.Task;
    }


    public async static Task TryMain()
    {
        int messages = 10000;
        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };

        const string certPath = "/rabbitmq-client-cert/tls.crt";
        const string keyPath = "/rabbitmq-client-cert/tls.key";
        // const string certPath = "tls.crt";
        // const string keyPath = "tls.key";
        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            var cert = X509Certificate2.CreateFromPem(
                File.ReadAllText(certPath), File.ReadAllText(keyPath));
            var certs = new X509CertificateCollection { cert };
            factory.AuthMechanisms = new IAuthMechanismFactory[] { new ExternalMechanismFactory() };
            factory.HostName = "hoppy.rabbitmq.svc.cluster.local";
            factory.Port = 5671;
            factory.Ssl = new SslOption
            {
                Certs = certs,
                Enabled = true,
                ServerName = factory.HostName,
                CertificateValidationCallback = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return true;
                }
            };
//            factory.HostName = "localhost";
            factory.VirtualHost = "dev";
        };

        int tc = Environment.TickCount;

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        int afterConnTicks = Environment.TickCount;
        channel.ConfirmSelect();
        int afterConfirmTicks = Environment.TickCount;

        channel.ExchangeDeclare("Vendeq.Api.Common.Messages:AuditEvent", "fanout", true, false, new Dictionary<string, object>());
        int afterDeclareTicks = Environment.TickCount;
        Console.WriteLine("******* WAITED FOR {0}ms for conn start ({1} conn, {2} confirm, {3} declare)",
            afterDeclareTicks - tc, afterConnTicks - tc, afterConfirmTicks - afterConnTicks, afterDeclareTicks - afterConfirmTicks);

        var sw = new Stopwatch();
        sw.Start();

        long lastAt = 0;
        int totalSent = 0;

        for (int i = 0; i < messages; ++i)
        {
            if (sw.ElapsedMilliseconds - lastAt > 2000)
            {
                lastAt = sw.ElapsedMilliseconds;
                //logger.LogInformation("Sent {messages} in {s:F2} (rate={per_s:F2}/s)",
                Console.WriteLine("Sent {0} in {1:F2} (rate={2:F2}/s)",
                    totalSent, sw.Elapsed.TotalSeconds, totalSent / sw.Elapsed.TotalSeconds);
            }
            Interlocked.Increment(ref totalSent);
            tc = Environment.TickCount;
            await SendOne(channel);
            int elapsed = Environment.TickCount - tc;
            if (elapsed > 300)
            {
                Console.WriteLine("******* WAITED FOR {0}ms to publish message", elapsed);
            }
        }
    }

    static async Task<int> Main()
    {
        try
        {
            await TryMain();
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Caught error during TryMain: {e}");
            return 1;
        }
    }

    public static MTMessage CreateTestMessage()
    {
        var msg = new AuditEvent
        {
            CustomerId = new Guid("bf2c4c7c-8b73-4a7c-a789-4a92684372bd"),
            UserId = new Guid("b1fb51a7-c13f-4cbf-832b-304348c8270e"),
            EventType = "person_modified",
            EventParams = null,
            DateTime = new DateTime(2022, 10, 3, 5, 48, 38, 841),
            ClientIP = null,
            TargetId = "a72ca84edd9a421f89aca2652d961d5f",
            TargetName = "Carol Cooper",
            TargetType = "PersonEntity",
            ChildTargetId = null,
            ChildTargetName = null,
            ChildTargetType = null,
            Details = new
            {
                __type = "AuditEventFieldChanges",
                Value = new object[]
                {
                    new
                    {
                        FieldName = "PersonCreatedDtm",
                        OldValue = "2019-03-27T09:02:37.098Z",
                        NewValue = "2019-03-27T11:24:51.605Z"
                    }
                }
            }
        };

        return new MTMessage(
            "55740000-5058-c8f7-3d10-08dab75e201a",
            "55740000-5058-c8f7-5fe7-08dab75e201e",
            "rabbitmq://localhost/pollux_MessageTest_bus_ki4yyynomdrxqu3ebdpmqzoh8x?autodelete=true",
            "rabbitmq://localhost/Vendeq.Api.Common.Messages:AuditEvent",
            new [] { "urn:message:Vendeq.Api.Common.Messages:AuditEvent" },
            msg,
            new DateTimeOffset(2022, 10, 26, 14, 26, 51, 975, TimeSpan.Zero),
            new Dictionary<string ,string>(),
            new MTHost(
                "pollux",
                "MessageTest",
                95317,
                "MessageTest",
                "1.0.0.0",
                "6.0.10",
                "1.0.0.0",
                "Unix 5.15.0.52"
            )
        );
    }
}
