using Nethereum.Web3;
using NodeBlock.Engine;
using NodeBlock.Engine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeBlock.Plugin.GlqChain.Nodes.Eth
{
    [NodeDefinition("EstimateGasPriceNode", "Estimate Gas Price", NodeTypeEnum.Function, "Blockchain.Ethereum")]
    [NodeGraphDescription("Estimate the gas price from average of gas price from the previous block")]
    public class EstimateGasPriceNode : Node
    {
        public EstimateGasPriceNode(string id, BlockGraph graph)
            : base(id, graph, typeof(EstimateGasPriceNode).Name)
        {
            this.InParameters.Add("ethConnection", new NodeParameter(this, "ethConnection", typeof(object), true));
            this.OutParameters.Add("gasPrice", new NodeParameter(this, "gasPrice", typeof(int), false));

        }

        public override bool CanBeExecuted => false;

        public override bool CanExecute => false;

        public override object ComputeParameterValue(NodeParameter parameter, object value)
        {
            if (parameter.Name == "gasPrice")
            {
                GlqConnection ethConnection = this.InParameters["glqConnection"].GetValue() as GlqConnection;
                var gasPriceTask = ethConnection.Web3Client.Eth.GasPrice.SendRequestAsync();
                gasPriceTask.Wait();
                return Web3.Convert.FromWei(gasPriceTask.Result.Value, Nethereum.Util.UnitConversion.EthUnit.Gwei);
            }
            return base.ComputeParameterValue(parameter, value);
        }
    }
}
