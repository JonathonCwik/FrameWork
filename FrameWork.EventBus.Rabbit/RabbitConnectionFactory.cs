using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace FrameWork.EventBus.Rabbit
{
    public class RabbitConnectionFactory
    {
        public static Task<IConnection> Create(Action<ConnectionOptions> optsAction)
        {
            var options = new ConnectionOptions();
            optsAction(options);

            var connFactory = new ConnectionFactory
            {
                HostName = options.URIs?.First().Host ?? "localhost",
                Port = options.URIs?.First().Port ?? 5672,
                UserName = options.URIs?.First().UserInfo.Split(':')[0] ?? "guest",
                Password = options.URIs?.First().UserInfo.Split(':')[1] ?? "guest",
                AutomaticRecoveryEnabled = options.AutomaticReconnect,
                RequestedHeartbeat = options.HeartbeatInterval,
                RequestedConnectionTimeout = 500,
                ContinuationTimeout = TimeSpan.FromMilliseconds(500),
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(500)
            };

            if (options.URIs == null || options.URIs.Count <= 1)
            {
                return Task.FromResult(connFactory.CreateConnection());
            }
            else
            {
                var uris = options.URIs.Select(u => u.ToString()).ToList();
                return Task.FromResult(connFactory.CreateConnection(uris));
            }
        }

        private static string ReplaceHost(string original, string newHostName)
        {
            var builder = new UriBuilder(original);
            builder.Host = newHostName;
            return builder.Uri.ToString();
        }
    }

    public class ConnectionOptions
    {
        public ConnectionOptions()
        {
            AutomaticReconnect = true;
            HeartbeatInterval = 60;
        }
        public IConnection Connection { get; set; }
        public List<Uri> URIs { get; set; }
        public bool AutomaticReconnect { get; set; }
        public ushort HeartbeatInterval { get; set; }
    }
}