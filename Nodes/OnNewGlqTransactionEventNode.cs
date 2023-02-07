using Nethereum.JsonRpc.Client.Streaming;
using Nethereum.RPC.Eth.Subscriptions;
using NodeBlock.Engine;
using NodeBlock.Engine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using NodeBlock.Plugin.GlqChain;

namespace NodeBlock.Plugin.GlqChain.Nodes
{
    [NodeDefinition("OnNewGlqTransactionEventNode", "On GLQ Chain Transaction", NodeTypeEnum.Event, "Blockchain.GraphLinq")]
    [NodeGraphDescription("Event that occurs everytime a new GLQ transaction appears in the last network block")]
    public class OnNewGlqTransactionEventNode : Node, IEventEthereumNode
    {
        private EthNewPendingTransactionSubscription glqNewPendingTransactionSubscription;

        public OnNewGlqTransactionEventNode(string id, BlockGraph graph)
            : base(id, graph, typeof(OnNewGlqTransactionEventNode).Name)
        {
            this.IsEventNode = true;

            this.InParameters.Add("connection", new NodeParameter(this, "connection", typeof(string), true));

            this.OutParameters.Add("transactionHash", new NodeParameter(this, "transactionHash", typeof(string), true));
        }

        public override bool CanBeExecuted => false;

        public override bool CanExecute => true;

        public override void SetupEvent()
        {
            GlqConnection glqConnection = this.InParameters["connection"].GetValue() as GlqConnection;
            if (glqConnection.UseManaged)
            {
                glqNewPendingTransactionSubscription = Plugin.EventsManagerGlq.NewEventTypePendingTxs(this);
            }
            else
            {
                this.glqNewPendingTransactionSubscription = new EthNewPendingTransactionSubscription(glqConnection.SocketClient);
                glqNewPendingTransactionSubscription.SubscriptionDataResponse += OnEventNode;
                glqNewPendingTransactionSubscription.SubscribeAsync().Wait();
            }
        }

        public override void OnStop()
        {
            GlqConnection glqConnection = this.InParameters["connection"].GetValue() as GlqConnection;
            if (glqConnection.UseManaged)
            {
                string eventType = glqNewPendingTransactionSubscription.GetType().ToString();
                Plugin.EventsManagerGlq.RemoveEventNode(eventType, this);
                return;
            }
            this.glqNewPendingTransactionSubscription.UnsubscribeAsync().Wait();
        }

        public void OnEventNode(object sender, dynamic e)
        {
            StreamingEventArgs<string> eventData = e;

            if (eventData.Response == null) return;
            var instanciatedParameters = this.InstanciateParametersForCycle();
            instanciatedParameters["transactionHash"].SetValue(eventData.Response);

            this.Graph.AddCycle(this, instanciatedParameters);
        }
        public override void BeginCycle()
        {
            this.Next();
        }

    }
}
