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

namespace QuestradeAPI.Net.TestClient
{
    public partial class ReplaceBracketOrderDlg : Form
    {
        #region Fields

        private MainForm m_parent;
        readonly BracketOrderComponent m_componentPrimary;
        readonly BracketOrderComponent m_componentLimit;
        readonly BracketOrderComponent m_componentStopLoss;

        #endregion

        #region C'tor

        public ReplaceBracketOrderDlg(MainForm parent, List<BracketOrderComponent> bracketOrderComponents)
        {
            m_parent = parent;
            foreach (BracketOrderComponent component in bracketOrderComponents)
            {
                if (component.m_orderClass == OrderClass.Primary)
                    m_componentPrimary = component;
                else if (component.m_orderClass == OrderClass.Limit)
                    m_componentLimit = component;
                else if (component.m_orderClass == OrderClass.StopLoss)
                    m_componentStopLoss = component;
            }

            InitializeComponent();
            SetupUI();
        }

        #endregion

        #region Event Handlers

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void m_btnPreview_Click(object sender, EventArgs e)
        {
            if (IsAsync)
            {
                m_parent.OnReplaceBracketOrderImpactRequest(true);
                BracketReplaceImpactResponse.BeginBracketReplaceImpact(m_parent.AuthImpl, new AsyncCallback(m_parent.ReplaceBracketOrderImpactCallbackMethod),
                                                                       m_parent.NextAsyncRequestID, m_parent.SelectedAccountNumber, BracketComponents);
            }
            else
            {
                m_parent.OnReplaceBracketOrderImpactRequest(false);
                BracketReplaceImpactResponse resp = BracketReplaceImpactResponse.BracketReplaceImpact(m_parent.AuthImpl, m_parent.SelectedAccountNumber, BracketComponents);
                m_parent.DisplayReplaceBracketOrderImpactResponse(resp);
            }
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

        public ulong OrderID
        {
            get
            {
                return getULong(m_txtOrderId);
            }
            private set
            {
                m_txtOrderId.Text = value.ToString();
            }
        }

        public double Quantity
        {
            get
            {
                return getDouble(m_txtSize);
            }
            private set
            {
                fillDoubleTextBox(m_txtSize, value);
            }
        }

        public double LimitPrice
        {
            get
            {
                return getDouble(m_txtLimitPrice);
            }
            private set
            {
                fillDoubleTextBox(m_txtLimitPrice, value);
            }
        }

        public double StopPrice
        {
            get
            {
                return getDouble(m_txtStopPrice);
            }
            private set
            {
                fillDoubleTextBox(m_txtStopPrice, value);
            }
        }

        public OrderType OrderType
        {
            get
            {
                return MainForm.GetEnum<OrderType>(m_cmbOrderType);
            }
            private set
            {
                setCurrentEnumIndex(m_cmbOrderType, value.ToString());
            }
        }

        public OrderTimeInForce TimeInForce
        {
            get
            {
                return MainForm.GetEnum<OrderTimeInForce>(m_cmbTimeInForce);
            }
            private set
            {
                setCurrentEnumIndex(m_cmbTimeInForce, value.ToString());
            }
        }

        public List<BracketOrderComponent> BracketComponents
        {
            get
            {
                List<BracketOrderComponent> brackets = new List<BracketOrderComponent>();
                BracketOrderComponent bracketPrimary = new BracketOrderComponent();
                bracketPrimary.m_orderId = m_componentPrimary.m_orderId;
                bracketPrimary.m_orderClass = OrderClass.Primary;
                bracketPrimary.m_action = m_componentPrimary.m_action;
                bracketPrimary.m_orderType = MainForm.GetEnum<OrderType>(m_cmbOrderType);
                bracketPrimary.m_quantity = getDouble(m_txtSize);
                bracketPrimary.m_stopPrice = getDouble(m_txtStopPrice);
                bracketPrimary.m_limitPrice = getDouble(m_txtLimitPrice);
                bracketPrimary.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbOrderType);
                brackets.Add(bracketPrimary);

                if (m_componentLimit != null)
                {
                    BracketOrderComponent bracketLimit = new BracketOrderComponent();
                    bracketLimit.m_orderId = m_componentLimit.m_orderId;
                    bracketLimit.m_orderClass = OrderClass.Limit;
                    bracketLimit.m_action = m_componentLimit.m_action;
                    bracketLimit.m_orderType = OrderType.Limit;
                    bracketLimit.m_quantity = Convert.ToDouble(m_spinBracketQuantity1.Value);
                    //bracketLimit.m_stopPrice;
                    bracketLimit.m_limitPrice = getDouble(m_txtBracketLimit1);
                    bracketLimit.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbBracketDuration1);
                    brackets.Add(bracketLimit);
                }

                if (m_componentStopLoss != null)
                {
                    BracketOrderComponent bracketStopLoss = new BracketOrderComponent();
                    bracketStopLoss.m_orderId = m_componentStopLoss.m_orderId;
                    bracketStopLoss.m_orderClass = OrderClass.StopLoss;
                    bracketStopLoss.m_action = m_componentStopLoss.m_action;
                    bracketStopLoss.m_orderType = OrderType.StopLimit;
                    bracketStopLoss.m_quantity = Convert.ToDouble(m_spinBracketQuantity2.Value);
                    bracketStopLoss.m_stopPrice = getDouble(m_txtBracketStop2);
                    bracketStopLoss.m_limitPrice = getDouble(m_txtBracketLimit2);
                    bracketStopLoss.m_timeInForce = MainForm.GetEnum<OrderTimeInForce>(m_cmbBracketDuration2);
                    brackets.Add(bracketStopLoss);
                }

                return brackets;
            }
        }

        #endregion

        #region Private Methods

        private void SetupUI()
        {
            MainForm.InitEnumCombobox(m_cmbOrderType, OrderType.Undefined);
            MainForm.InitEnumCombobox(m_cmbTimeInForce, OrderTimeInForce.Undefined);
            // bracket part
            MainForm.InitEnumCombobox(m_cmbBracketDuration1, OrderTimeInForce.Undefined);
            MainForm.InitEnumCombobox(m_cmbBracketDuration2, OrderTimeInForce.Undefined);


            m_txtOrderId.ReadOnly = true;
            OrderID = m_componentPrimary.m_orderId;
            Quantity = m_componentPrimary.m_quantity;
            LimitPrice = m_componentPrimary.m_limitPrice;
            StopPrice = m_componentPrimary.m_stopPrice;
            OrderType = m_componentPrimary.m_orderType;
            TimeInForce = m_componentPrimary.m_timeInForce;

            // if Limit component is present, fill the GUI
            m_chkBracketLimit.Enabled = false;
            if (m_componentLimit != null)
            {
                m_chkBracketLimit.Checked = true;
                m_spinBracketQuantity1.Value = Convert.ToDecimal(m_componentLimit.m_quantity);
                // m_componentLimit.m_stopPrice
                fillDoubleTextBox(m_txtBracketLimit1, m_componentLimit.m_limitPrice);
                setCurrentEnumIndex(m_cmbBracketDuration1, m_componentLimit.m_timeInForce.ToString());
            }
            else
            {
                m_chkBracketLimit.Checked = true;
            }

            // if Stop Loss component is present, fill the GUI
            m_chkBracketStopLoss.Enabled = false;
            if (m_componentStopLoss != null)
            {
                m_chkBracketStopLoss.Checked = true;
                m_spinBracketQuantity2.Value = Convert.ToDecimal(m_componentLimit.m_quantity);
                fillDoubleTextBox(m_txtBracketStop2, m_componentStopLoss.m_stopPrice);
                fillDoubleTextBox(m_txtBracketLimit2, m_componentStopLoss.m_limitPrice);
                setCurrentEnumIndex(m_cmbBracketDuration2, m_componentStopLoss.m_timeInForce.ToString());
            }
            else
            {
                m_chkBracketStopLoss.Checked = false;
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

        private static void fillDoubleTextBox(TextBox tb, double value)
        {
            tb.Text = value == QuestradeAPI.Constants.InvalidDbl ? String.Empty : value.ToString();
        }

        private static void setCurrentEnumIndex(ComboBox cmb, string value)
        {
            cmb.SelectedIndex = Math.Max(cmb.FindString(value), 0);
        }

        #endregion
    }
}
