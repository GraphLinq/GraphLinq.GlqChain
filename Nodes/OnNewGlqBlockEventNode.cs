using Nethereum.JsonRpc.Client.Streaming;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Subscriptions;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using NodeBlock.Engine;
using NodeBlock.Engine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeBlock.Plugin.GlqChain.Nodes
{
    [NodeDefinition("OnNewGlqBlockEventNode", "On GLQ Chain Block", NodeTypeEnum.Event, "Blockchain.GraphLinq")]
    [NodeGraphDescription("Event that occurs everytime a new GLQ block is minted")]
    public class OnNewGlqBlockEventNode : Node, IEventEthereumNode
    {
        private EthNewBlockHeadersObservableSubscription blockHeadersSubscription;

        public OnNewGlqBlockEventNode(string id, BlockGraph graph)
            : base(id, graph, typeof(OnNewGlqBlockEventNode).Name)
        {
            this.IsEventNode = true;

            this.InParameters.Add("connection", new NodeParameter(this, "connection", typeof(string), true));

            this.OutParameters.Add("block", new NodeParameter(this, "block", typeof(Nethereum.RPC.Eth.DTOs.Block), true));

        }

        public override bool CanBeExecuted => false;

        public override bool CanExecute => true;

        public override void SetupEvent()
        {
            GlqConnection glqConnection = this.InParameters["connection"].GetValue() as GlqConnection;
            if (glqConnection.UseManaged)
            {
                blockHeadersSubscription = Plugin.EventsManagerGlq.NewEventTypePendingBlocks(this);
            }
            else
            {
                this.blockHeadersSubscription = new EthNewBlockHeadersObservableSubscription(glqConnection.SocketClient);
                blockHeadersSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(async Block =>
                {
                    var instanciatedParameters = this.InstanciateParametersForCycle();
                    instanciatedParameters["block"].SetValue(Block);

                    this.Graph.AddCycle(this, instanciatedParameters);
                });
                blockHeadersSubscription.SubscribeAsync();
            }

        }

        public override void OnStop()
        {
            GlqConnection glqConnection = this.InParameters["connection"].GetValue() as GlqConnection;
            if (glqConnection.UseManaged)
            {
                string eventType = blockHeadersSubscription.GetType().ToString();
                Plugin.EventsManagerGlq.RemoveEventNode(eventType, this);
                return;
            }
            this.blockHeadersSubscription.UnsubscribeAsync().Wait();
        }

        public override void BeginCycle()
        {
            this.Next();
        }

        public void OnEventNode(object sender, dynamic e)
        {
            var instanciatedParameters = this.InstanciateParametersForCycle();
            instanciatedParameters["block"].SetValue(e);

            this.Graph.AddCycle(this, instanciatedParameters);
        }
    }
}
