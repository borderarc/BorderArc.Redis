using System;
using System.Threading.Tasks;
using Serilog;
using StackExchange.Redis;

namespace BorderArc.Redis
{
    /// <summary>
    ///     Methods for Redis clients
    /// </summary>
    public class Client
    {
        public Client(string redisUri)
        {
            RedisUri = redisUri;
            RedisConnection = ConnectionMultiplexer.Connect(redisUri);
        }

        private ConnectionMultiplexer RedisConnection { get; }
        private string RedisUri { get; }

        /// <summary>
        ///     Subscribe to a channel on a Redis server
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="onEventReceived">Action to process received events</param>
        /// <returns>Subscription success boolean</returns>
        public async Task<bool> Subscribe(string channel, Action<RedisChannel, RedisValue> onEventReceived)
        {
            try
            {
                Log.Logger.Debug($"Getting subscriber from RedisConnection on redis server [{RedisUri}]");
                var sub = RedisConnection.GetSubscriber();
                Log.Logger.Debug($"Subscribing to channel [{channel}] on redis server [{RedisUri}]");
                sub.Subscribe(channel, onEventReceived);
                Log.Logger.Information($"Successfully subscribed to channel [{channel}] on redis server [{RedisUri}]");
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Exception subscribing to channel [{channel}] " +
                                 $"Message [{ex.Message}] " +
                                 $"StackTrace [{ex.StackTrace}] " +
                                 $"InnerEx [{ex.InnerException}]");
                return false;
            }
        }

        /// <summary>
        ///     Publish to a channel on a Redis server
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="message">Message to be published</param>
        /// <returns></returns>
        public async Task<long> Publish(string channel, string message)
        {
            try
            {
                Log.Logger.Debug($"Getting subscriber from RedisConnection on [{RedisUri}]");
                var sub = RedisConnection.GetSubscriber();
                Log.Logger.Debug($"Publishing to channel [{channel}] on redis server [{RedisUri}]");
                var result = sub.Publish(channel, message);
                Log.Logger.Information(
                    $"Successfully published message to channel [{channel}] on redis server [{RedisUri}]");
                return result;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Exception publishing to channel [{channel}] " +
                                 $"Message [{ex.Message}] " +
                                 $"StackTrace [{ex.StackTrace}] " +
                                 $"InnerEx [{ex.InnerException}]");
                throw;
            }
        }
    }
}