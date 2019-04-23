using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

namespace BorderArc.Redis
{
    /// <summary>
    ///     Methods for Redis clients
    /// </summary>
    public class Client : IDisposable
    {
        /// <summary>
        ///     Utilities to manage Redis caches
        /// </summary>
        /// <param name="redisUri">The URI to connect to the Redis cache</param>
        public Client(string redisUri)
        {
            RedisConnection = ConnectionMultiplexer.Connect(redisUri);
            RedisUri = redisUri;
        }

        private ConnectionMultiplexer RedisConnection { get; }
        private string RedisUri { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            RedisConnection?.Dispose();
        }

        /// <summary>
        ///     Subscribe to a channel
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="subscriptionResponse">Action to be performed upon receiving a message</param>
        public async Task Subscribe(string channel, Action<RedisChannel, RedisValue> subscriptionResponse)
        {
            Log.Logger.Debug($"Getting subscriber for redis connection on Redis Server [{RedisUri}]");
            var sub = RedisConnection.GetSubscriber();
            Log.Logger.Information($"Subscribing to channel [{channel}] with given action");
            sub.Subscribe(channel, subscriptionResponse);
            Log.Logger.Debug($"Finished subscribing to channel [{channel}] on Redis Server [{RedisUri}]");
        }

        /// <summary>
        ///     Publish message to given subscription
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="message">Message to publish</param>
        public async Task<bool> Publish(string channel, string message)
        {
            try
            {
                Log.Logger.Debug($"Getting subscriber for redis connection on Redis Server [{RedisUri}]");
                var sub = RedisConnection.GetSubscriber();
                Log.Logger.Debug($"Publishing message [{message}] to channel [{channel}] on Redis Server [{RedisUri}]");
                sub.Publish(channel, message);
                Log.Logger.Information($"Published message to channel [{channel}]");
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to publish message on Redis Server [{RedisUri}]. " +
                                 $"Message [{ex.Message}] " +
                                 $"StackTrace [{ex.StackTrace}] " +
                                 $"InnerEx [{ex.InnerException}]");
                return false;
            }
        }

        /// <summary>
        ///     Publish message to given subscription
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="message">Message to publish</param>
        public async Task<bool> Publish(string channel, object message)
        {
            try
            {
                return await Publish(channel, JsonConvert.SerializeObject(message));
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to publish message to Redis server on Redis Server [{RedisUri}]. " +
                                 $"Message [{ex.Message}] " +
                                 $"StackTrace [{ex.StackTrace}] " +
                                 $"InnerEx [{ex.InnerException}]");
                return false;
            }
        }

        /// <summary>
        ///     Send a key value pair to be stored in the Redis cache
        /// </summary>
        /// <param name="key">The identifier of the payload</param>
        /// <param name="value">The payload to store in the Redis cache</param>
        /// <param name="expire">TimeSpan for the payload to last</param>
        /// <returns>Success boolean</returns>
        public async Task<bool> SendToRedisDatabase(string key, string value, TimeSpan? expire = null)
        {
            var db = RedisConnection.GetDatabase();
            return expire != null ? db.StringSet(key, value, TimeSpan.FromMinutes(1)) : db.StringSet(key, value);
        }

        /// <summary>
        ///     Send a key value pair to be stored in the Redis cache
        /// </summary>
        /// <param name="key">The identifier of the payload</param>
        /// <param name="value">The payload to store in the Redis cache</param>
        /// <param name="expire">TimeSpan for the payload to last</param>
        /// <returns>Success boolean</returns>
        public async Task<bool> SendToRedisDatabase(string key, object value, TimeSpan? expire = null)
        {
            return expire != null
                ? await SendToRedisDatabase(key, JsonConvert.SerializeObject(value), expire)
                : await SendToRedisDatabase(key, JsonConvert.SerializeObject(value));
        }

        /// <summary>
        ///     Send a key value pair to be stored in the Redis cache and broadcast event to listeners
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="key">The identifier of the payload</param>
        /// <param name="message">The payload to store in the Redis cache and broadcast</param>
        /// <param name="expire">TimeSpan for the payload to last</param>
        /// <returns></returns>
        public async Task<bool> SendAndBroadcast(string channel, string key, string message, TimeSpan? expire = null)
        {
            var databaseResponse = SendToRedisDatabase(key, message, expire);
            var broadcastResponse = Publish(channel, message);

            Task.WaitAll(databaseResponse, broadcastResponse);

            if (!databaseResponse.Result)
            {
                Log.Logger.Error($"Failed to send message to Redis database with key [{key}] on Redis Server [{RedisUri}]");
            }

            if (!broadcastResponse.Result)
            {
                Log.Logger.Error($"Failed to broadcast message on channel [{channel}] on Redis Server [{RedisUri}]");
            }

            return databaseResponse.Result && broadcastResponse.Result;
        }

        /// <summary>
        ///     Send a key value pair to be stored in the Redis cache and broadcast event to listeners
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="key">The identifier of the payload</param>
        /// <param name="message">The payload to store in the Redis cache and broadcast</param>
        /// <param name="expire">TimeSpan for the payload to last</param>
        /// <returns></returns>
        public async Task<bool> SendAndBroadcast(string channel, string key, object message, TimeSpan? expire = null)
        {
            return await SendAndBroadcast(channel, key, JsonConvert.SerializeObject(message), expire);
        }

        /// <summary>
        ///     Get a key value pair from the Redis cache
        /// </summary>
        /// <param name="key">The identifier of the payload</param>
        /// <returns>String payload</returns>
        public async Task<string> GetFromRedisDatabase(string key)
        {
            var db = RedisConnection.GetDatabase();
            return db.StringGet(key).ToString();
        }

        /// <summary>
        ///     Get a key value pair from the Redis cache
        /// </summary>
        /// <param name="key">The identifier of the payload</param>
        /// <typeparam name="TJsonDataType">The DataType to convert JSON to</typeparam>
        /// <returns>String payload</returns>
        public async Task<TJsonDataType> GetFromRedisDatabase<TJsonDataType>(string key) where TJsonDataType : class
        {
            return JsonConvert.DeserializeObject<TJsonDataType>(await GetFromRedisDatabase(key));
        }

    }
}