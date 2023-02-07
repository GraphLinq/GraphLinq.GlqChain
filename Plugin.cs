using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.Web3;
using NodeBlock.Engine;
using NodeBlock.Engine.Interop;
using NodeBlock.Engine.Interop.Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using Nethereum.JsonRpc.Client.Streaming;
using Nethereum.RPC.Eth.Subscriptions;
using Nethereum.RPC.Eth.DTOs;
using System.Collections.Concurrent;
using System.Linq;
using NodeBlock.Plugin.Ethereum.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NodeBlock.Engine.Storage.MariaDB;
using Microsoft.EntityFrameworkCore;
using NodeBlock.Plugin.GlqChain.Nodes;

namespace NodeBlock.Plugin.GlqChain
{
    public class Plugin : BasePlugin
    {
        public static string WEB_3_API_URL_GLQ = "";
        public static string WEB_3_WS_URL_GLQ = "";

        public static object mutex = new object();

        public static Web3 Web3ClientGLQ { get; set; }
        public static StreamingWebSocketClient SocketClientGLQ { get; set; }


        public static ManagedGlqChainEvents EventsManagerGlq { get; set; }


        public static bool PluginAlive = true;

        public static ServiceProvider Services;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public override void Load()
        {
            // ETH
            WEB_3_API_URL_GLQ = Environment.GetEnvironmentVariable("glq_api_http_url");
            WEB_3_WS_URL_GLQ = Environment.GetEnvironmentVariable("glq_api_ws_url");

            Web3ClientGLQ = new Web3(WEB_3_API_URL_GLQ);
            SocketClientGLQ = new StreamingWebSocketClient(WEB_3_WS_URL_GLQ);

            try
            {
                SocketClientGLQ.StartAsync().Wait();
                logger.Info("Success! Connected to GLQ network");
            }
            catch(Exception exception)
            {
                logger.Error("Failed connecting to GLQ network: {0}", exception.Message);
            }

            // Init database plugin
            Services = new ServiceCollection()
                .AddScoped(provider => provider.GetService<Storage.DatabaseStorage>())
                .AddDbContextPool<Storage.DatabaseStorage>(options =>
                {
                    options.UseMySQL(
                        Environment.GetEnvironmentVariable("mariadb_uri"));
                })
                .BuildServiceProvider();


            //Managers AutoManaged Events

            // eth events
            EventsManagerGlq = new ManagedGlqChainEvents(SocketClientGLQ, new Web3(WEB_3_WS_URL_GLQ));

        }

    }
}
