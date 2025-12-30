using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Questrade.BusinessObjects.Entities;

namespace QuestradeAPI.Net.TestClient
{
    public static class ResponseDumper
    {
        #region Public Static Methods

        public static string ToString(GetServerTimeResponse getTimeResp)
        {
            StringBuilder str = new StringBuilder();

            if (!getTimeResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", getTimeResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", getTimeResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("Time: {0}", getTimeResp.Time));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(InsertOrderImpactResponse orderImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!orderImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", orderImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", orderImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", orderImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", orderImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", orderImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", orderImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", orderImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Side: {0}", orderImpactResp.Side));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", orderImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", orderImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(StrategyOrderImpactResponse strategyOrderImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!strategyOrderImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", strategyOrderImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", strategyOrderImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", strategyOrderImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", strategyOrderImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", strategyOrderImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", strategyOrderImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", strategyOrderImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Side: {0}", strategyOrderImpactResp.Side));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", strategyOrderImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", strategyOrderImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(BracketOrderImpactResponse bracketOrderImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!bracketOrderImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", bracketOrderImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", bracketOrderImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", bracketOrderImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", bracketOrderImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", bracketOrderImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", bracketOrderImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", bracketOrderImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Side: {0}", bracketOrderImpactResp.Side));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", bracketOrderImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", bracketOrderImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(ReplaceOrderImpactResponse replaceImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!replaceImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", replaceImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", replaceImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", replaceImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", replaceImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", replaceImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", replaceImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", replaceImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", replaceImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", replaceImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(StrategyReplaceImpactResponse replaceStrategyImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!replaceStrategyImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", replaceStrategyImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", replaceStrategyImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", replaceStrategyImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", replaceStrategyImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", replaceStrategyImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", replaceStrategyImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", replaceStrategyImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", replaceStrategyImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", replaceStrategyImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        public static string ToString(BracketReplaceImpactResponse replaceBracketImpactResp)
        {
            StringBuilder str = new StringBuilder();

            if (!replaceBracketImpactResp.IsValid)
            {
                str.Append(String.Format("Error: {0}", replaceBracketImpactResp.ErrorCode));
                str.Append(Environment.NewLine);
                str.Append(String.Format("Error Message: {0}", replaceBracketImpactResp.ErrorMessage));
            }
            else
            {
                str.Append(String.Format("EstimatedCommissions: {0}", replaceBracketImpactResp.EstimatedCommissions));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerEffect: {0}", replaceBracketImpactResp.BuyingPowerEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("BuyingPowerResult: {0}", replaceBracketImpactResp.BuyingPowerResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessEffect: {0}", replaceBracketImpactResp.MaintExcessEffect));
                str.Append(Environment.NewLine);

                str.Append(String.Format("MaintExcessResult: {0}", replaceBracketImpactResp.MaintExcessResult));
                str.Append(Environment.NewLine);

                str.Append(String.Format("TradeValueCalculation: {0}", replaceBracketImpactResp.TradeValueCalculation));
                str.Append(Environment.NewLine);

                str.Append(String.Format("Price: {0}", replaceBracketImpactResp.Price));
                str.Append(Environment.NewLine);
            }

            return str.ToString();
        }

        #endregion
    }
}
