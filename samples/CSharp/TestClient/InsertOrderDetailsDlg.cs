using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Questrade.BusinessObjects.Entities;
using QuestradeAPI;

namespace QuestradeAPI.Net.TestClient
{
    public partial class InsertOrderDetailsDlg : Form
    {
        #region Fields

        private MainForm m_parent;
        private bool m_hasGtdDate = false;

        #endregion

        #region C'tor

        public InsertOrderDetailsDlg(MainForm parent)
        {
            m_parent = parent;
            InitializeComponent();

            MainForm.InitEnumCombobox(m_cmbOrderType, OrderType.Undefined);
            MainForm.InitEnumCombobox(m_cmbTimeInForce, OrderTimeInForce.Undefined);
            MainForm.InitEnumCombobox(m_cmbSide, OrderAction.Undefined);
            // strategy order
            MainForm.InitEnumCombobox(m_cmbAction1, OrderAction.Undefined);
            MainForm.InitEnumCombobox(m_cmbAction2, OrderAction.Undefined);
            MainForm.InitEnumCombobox(m_cmbStrategyType, StrategyType.Undefined);
            //bracket order
            MainForm.InitEnumCombobox(m_cmbBracketDuration1, OrderTimeInForce.Undefined);
            MainForm.InitEnumCombobox(m_cmbBracketDuration2, OrderTimeInForce.Undefined);

            m_chkBracketLimit.Checked = true;
            m_chkBracketStopLoss.Checked = true;

            this.Width = 300;
            this.Height = 650;
        }

        #endregion

        #region Event Handlers

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void m_dtpGtdDate_ValueChanged(object sender, EventArgs e)
        {
            m_hasGtdDate = true;
        }

        private void m_btnPreview_Click(object sender, EventArgs e)
        {
            if (IsStrategyOrder)
            {
                InsertStrategyOrderRequest insertStrategyOrderRequest = new InsertStrategyOrderRequest();
                insertStrategyOrderRequest.m_accountNumber = m_parent.SelectedAccountNumber;
                insertStrategyOrderRequest.m_symbolId = SymbolID;
                insertStrategyOrderRequest.m_limitPrice = LimitPrice;
                insertStrategyOrderRequest.m_orderType = OrderType;
                insertStrategyOrderRequest.m_timeInForce = TimeInForce;
                insertStrategyOrderRequest.m_primaryRoute = PrimaryRoute;
                insertStrategyOrderRequest.m_secondaryRoute = SecondaryRoute;
                insertStrategyOrderRequest.m_legs = OrderLegs;
                insertStrategyOrderRequest.m_strategy = StrategyType;

                if (IsAsync)
                {
                    m_parent.OnInsertStrategyOrderImpactRequest(true);
                    StrategyOrderImpactResponse.BeginStrategyOrderImpact(m_parent.AuthImpl, new AsyncCallback(m_parent.InsertStrategyOrderImpactCallbackMethod), m_parent.NextAsyncRequestID, insertStrategyOrderRequest);
                }
                else
                {
                    m_parent.OnInsertStrategyOrderImpactRequest(false);
                    StrategyOrderImpactResponse resp = StrategyOrderImpactResponse.StrategyOrderImpact(m_parent.AuthImpl, insertStrategyOrderRequest);
                    m_parent.DisplayInsertStrategyOrderImpactResponse(resp);
                }
            }
            else if (IsBracketOrder)
            {
                if (IsAsync)
                {
                    m_parent.OnInsertBracketOrderImpactRequest(true);
                    BracketOrderImpactResponse.BeginBracketOrderImpact(m_parent.AuthImpl, new AsyncCallback(m_parent.InsertBracketOrderImpactCallbackMethod), m_parent.NextAsyncRequestID,
                                                                       m_parent.SelectedAccountNumber, SymbolID, PrimaryRoute, SecondaryRoute, BracketComponents);
                }
                else
                {
                    m_parent.OnInsertBracketOrderImpactRequest(false);
                    BracketOrderImpactResponse resp = BracketOrderImpactResponse.BracketOrderImpact(m_parent.AuthImpl, m_parent.SelectedAccountNumber, SymbolID, 
                                                                                                    PrimaryRoute, SecondaryRoute, BracketComponents);
                    m_parent.DisplayInsertBracketOrderImpactResponse(resp);
                }
            }
            else
            {
                InsertOrderRequest insertOrderRequest = new InsertOrderRequest();
                insertOrderRequest.m_accountNumber = m_parent.SelectedAccountNumber;
                insertOrderRequest.m_symbolId = SymbolID;
                insertOrderRequest.m_quantity = Quantity;
                insertOrderRequest.m_icebergQuantity = IcebergQuantity;
                insertOrderRequest.m_minQuantity = MinQuantity;
                insertOrderRequest.m_limitPrice = LimitPrice;
                insertOrderRequest.m_stopPrice = StopPrice;
                insertOrderRequest.m_isAllOrNone = IsAllOrNone;
                insertOrderRequest.m_isAnonymous = IsAnonymous;
                insertOrderRequest.m_orderType = OrderType;
                insertOrderRequest.m_timeInForce = TimeInForce;
                insertOrderRequest.m_gtdDate = GtdDate;
                insertOrderRequest.m_action = Action;
                insertOrderRequest.m_primaryRoute = PrimaryRoute;
                insertOrderRequest.m_secondaryRoute = SecondaryRoute;
                insertOrderRequest.m_isLimitOffsetInDollar = IsLimitOffsetInDollar;

                if (IsAsync)
                {
                    m_parent.OnInsertOrderImpactRequest(true);
                    InsertOrderImpactResponse.BeginInsertOrderImpact(m_parent.AuthImpl, new AsyncCallback(m_parent.InsertOrderImpactCallbackMethod), m_parent.NextAsyncRequestID, insertOrderRequest);
                }
                else
                {
                    m_parent.OnInsertOrderImpactRequest(false);
                    InsertOrderImpactResponse resp = InsertOrderImpactResponse.InsertOrderImpact(m_parent.AuthImpl, insertOrderRequest);
                    m_parent.DisplayInsertOrderImpactResponse(resp);
                }
            }
        }

        private void m_chkIsStrategyOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (IsStrategyOrder && IsBracketOrder)
                m_chkIsBraketOrder.Checked = false;

            m_bracketOrderGroupBox.Enabled = IsBracketOrder;
            m_strategyOrderGroupBox.Enabled = IsStrategyOrder;
            if (IsStrategyOrder)
                this.Width = 680;
            else
                this.Width = 300;
        }

        private void m_chkIsBraketOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (IsStrategyOrder && IsBracketOrder)
                m_chkIsStrategyOrder.Checked = false;

            m_bracketOrderGroupBox.Enabled = IsBracketOrder;
            m_strategyOrderGroupBox.Enabled = IsStrategyOrder;
            if (IsBracketOrder)
                this.Width = 680;
            else
                this.Width = 300;
        }

        #endregion

        #region Properties

        public bool IsAsync
        {
            get
            {
                return m_chkAsync.Checked;
            }
        }

        public ulong SymbolID
        {
            get
            {
                return getULong(m_txtSymbolId);
            }
        }

        public double Quantity
        {
            get
            {
                return getDouble(m_txtSize);
            }
        }

        public double IcebergQuantity
        {
            get
            {
                return getDouble(m_txtIcebergSize);
            }
        }

        public double MinQuantity
        {
            get
            {
                return getDouble(m_txtMinSize);
            }
        }

        public double LimitPrice
        {
            get
            {
                return getDouble(m_txtLimitPrice);
            }
        }

        public double StopPrice
        {
            get
            {
                return getDouble(m_txtStopPrice);
            }
        }

        public bool IsAllOrNone
        {
            get
            {
                return m_chkIsAllOrNone.Checked;
            }
        }

        public bool IsAnonymous
        {
            get
            {
                return m_chkIsAnonymous.Checked;
            }
        }

        public OrderType OrderType
        {
            get
            {
                return MainForm.GetEnum<OrderType>(m_cmbOrderType);
            }
        }

        public OrderTimeInForce TimeInForce
        {
            get
            {
                return MainForm.GetEnum<OrderTimeInForce>(m_cmbTimeInForce);
            }
        }

        public OrderAction Action
        {
            get
            {
                return MainForm.GetEnum<OrderAction>(m_cmbSide);
            }
        }

        public string PrimaryRoute
        {
            get
            {
                return m_txtPrimaryRoute.Text;
            }
        }

        public string SecondaryRoute
        {
            get
            {
                return m_txtSecondaryRoute.Text;
            }
        }

        public bool IsLimitOffsetInDollar
        {
            get
            {
                return m_chkIsLimitOffsetInDollar.Checked;
            }
        }

        public DateTime GtdDate
        {
            get
            {
                return (m_hasGtdDate ? m_dtpGtdDate.Value.Date : DataTypeTraits<DateTime>.InvalidValue);
            }
        }

        public bool IsStrategyOrder
        {
            get 
            {
                return m_chkIsStrategyOrder.CheckState == CheckState.Checked;
            }
        }

        public bool IsBracketOrder
        {
            get
            {
                return m_chkIsBraketOrder.Checked;
            }
        }

        public StrategyType StrategyType
        {
            get
            {
                return MainForm.GetEnum<StrategyType>(m_cmbStrategyType);
            }
        }

        public List<InsertOrderLegData> OrderLegs
        {
            get 
            {
                List<InsertOrderLegData> legs = new List<InsertOrderLegData>();

                InsertOrderLegData leg1 = new InsertOrderLegData();
                InsertOrderLegData leg2 = new InsertOrderLegData();

                leg1.m_symbolId = getULong(m_txtOptionId1);
                leg2.m_symbolId = getULong(m_txtOptionId2);

                leg1.m_action = MainForm.GetEnum<OrderAction>(m_cmbAction1);
                leg2.m_action = MainForm.GetEnum<OrderAction>(m_cmbAction2);

                leg1.m_legQuantity = Convert.ToUInt64(m_spinQuantity1.Value);
                leg2.m_legQuantity = Convert.ToUInt64(m_spinQuantity2.Value);

                legs.Add(leg1);
                legs.Add(leg2);

                return legs;
            }
        }

        public List<BracketOrderComponent> BracketComponents
        {
            get
            {
                List<BracketOrderComponent> brackets = new List<BracketOrderComponent>();
                BracketOrderComponent bracket1 = new BracketOrderComponent();
                BracketOrderComponent bracket2 = new BracketOrderComponent();
                BracketOrderComponent bracket3 = new BracketOrderComponent();

                bracket1.m_orderClass = OrderClass.Primary;
                bracket2.m_orderClass = OrderClass.Limit;
                bracket3.m_orderClass = OrderClass.StopLoss;

                bracket1.m_action = MainForm.GetEnum<OrderAction>(m_cmbSide);
                bracket2.m_action = bracket1.m_action == OrderAction.Buy ? OrderAction.Sell : OrderAction.Buy;
                bracket3.m_action = bracket1.m_action == OrderAction.Buy ? OrderAction.Sell : OrderAction.Buy;

                bracket1.m_orderType = MainForm.GetEnum<OrderType>(m_cmbOrderType);
                bracket2.m_orderType = OrderType.Limit;
                bracket3.m_orderType = OrderType.StopLimit;

                bracket1.m_quantity = getDouble(m_txtSize);
                bracket2.m_quantity = Convert.ToDouble(m_spinBracketQuantity1.Value);
                bracket3.m_quantity = Convert.ToDouble(m_spinBracketQuantity2.Value);

                bracket1.m_stopPrice = getDouble(m_txtStopPrice);
                //bracket2.m_stopPrice;
                bracket3.m_stopPrice = getDouble(m_txtBracketStop2);

                bracket1.m_limitPrice = getDouble(m_txtLimitPrice);
                bracket2.m_limitPrice = getDouble(m_txtBracketLimit1);
                bracket3.m_limitPrice = getDouble(m_txtBracketLimit2);

                bracket1.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbOrderType);
                bracket2.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbBracketDuration1);
                bracket3.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbBracketDuration2);

                brackets.Add(bracket1);
                if (m_chkBracketLimit.Checked)
                    brackets.Add(bracket2);
                if (m_chkBracketStopLoss.Checked)
                    brackets.Add(bracket3);

                return brackets;
            }
        }

        #endregion

        #region Private Static Methods

        private static ulong getULong(TextBox tb)
        {
            try
            {
                return Convert.ToUInt64(tb.Text);
            }
            catch
            {
                return 0;
            }
        }

        private static double getDouble(TextBox tb)
        {
            try
            {
                return Convert.ToDouble(tb.Text);
            }
            catch
            {
                return QuestradeAPI.Constants.InvalidDbl;
            }
        }

        #endregion
    }
}
