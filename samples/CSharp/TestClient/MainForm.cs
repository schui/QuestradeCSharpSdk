using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Questrade.BusinessObjects.Entities;
using System.Threading;

namespace QuestradeAPI.Net.TestClient
{
    public partial class MainForm : Form
    {
        #region Delegates

        delegate double GetBalanceValue(BalanceData balance);

        #endregion

        #region Constants

        private static readonly GetBalanceValue s_getCash = balance => balance.m_cash;
        private static readonly GetBalanceValue s_getMktVal = balance => balance.m_marketValue;
        private static readonly GetBalanceValue s_getTotalEquity = balance => balance.m_totalEquity;
        private static readonly GetBalanceValue s_getMaintExcess = balance => balance.m_maintenanceExcess;
        private static readonly GetBalanceValue s_getBuyingPower = balance => balance.m_buyingPower;

        #endregion

        #region Static Fields

        private static MainForm s_mainForm;

        #endregion

        #region Private Classes

        private class MarketsData
        {
            public List<string> m_tradingVenues;
            public List<string> m_primaryOrderRoutes;
            public List<string> m_secondaryOrderRoutes;
            public List<string> m_level1Feeds;
            public List<string> m_level2Feeds;

            public MarketsData(List<string> tradingVenues, List<string> primaryOrderRoutes, List<string> secondaryOrderRoutes, List<string> level1Feeds, List<string> level2Feeds)
            {
                m_tradingVenues = tradingVenues;
                m_primaryOrderRoutes = primaryOrderRoutes;
                m_secondaryOrderRoutes = secondaryOrderRoutes;
                m_level1Feeds = level1Feeds;
                m_level2Feeds = level2Feeds;
            }
        }

        #endregion

        #region Fields

        private AuthenticationInfoImplementation m_authImpl;
        private int m_nextAsyncRequestId = 0;
        private string m_selectedAccountNumber;
        private ulong? m_selectedOrderId = null;
        private ulong? m_selectedSymbolId = null;
        private Dictionary<ulong, List<OrderLegData>> m_legs = new Dictionary<ulong, List<OrderLegData>>();
        private Dictionary<ulong, KeyValuePair<List<MinTickData>, ContractDeliverables>> m_symbolData = new Dictionary<ulong, KeyValuePair<List<MinTickData>, ContractDeliverables>>();
        private Dictionary<string, MarketsData> m_marketsData = new Dictionary<string, MarketsData>();

        #endregion

        #region C'tor

        public MainForm()
        {
            s_mainForm = this;
            InitializeComponent();
            m_btnStreamQuotes.Tag = false;
            m_btnStreamOptionQuotes.Tag = false;
            m_btnStreamStrategyQuotes.Tag = false;

            InitEnumCombobox(m_cmbStateFilter, OrderStateFilterTypes.All);
            InitEnumCombobox(m_cmbInterval, CandlesGranularity.OneDay);
            InitEnumCombobox(m_cmbStrategyQuotesStrategy, StrategyType.Custom);
            
            Application.ApplicationExit += Application_ApplicationExit;
        }

        #endregion

        #region Event Handlers

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            OrderAndExecutionNotificationsResponse.StopOrderAndExecutionNotificationsStream();
            AuthAgent.GetInstance().Stop();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            initUI();
            enable(false);
            updateUI();
        }

        private void m_btnRefreshQuotesSync_Click(object sender, EventArgs e)
        {
            onGetQuotesRequest(false);
            GetQuoteResponse resp = GetQuoteResponse.GetQuote(m_authImpl, quotesSymbolIds);
            displayQuotes(resp);
        }

        private void m_btnRefreshQuotesAsync_Click(object sender, EventArgs e)
        {
            onGetQuotesRequest(true);
            GetQuoteResponse.BeginGetQuote(m_authImpl, new AsyncCallback(getL1DataCallbackMethod), NextAsyncRequestID, quotesSymbolIds);
        }

        private void m_btnStreamQuotes_Click(object sender, EventArgs e)
        {
            m_btnStreamQuotes.Tag = !(bool)m_btnStreamQuotes.Tag;
            bool streamStarted = (bool)m_btnStreamQuotes.Tag;
            onGetQuotesStream(streamStarted);
            if (streamStarted)
            {
                GetQuoteResponse.GetQuoteStream(m_authImpl, getL1DataStreamCallbackMethod, NextAsyncRequestID, quotesSymbolIds);
            }
            else
            {
                GetQuoteResponse.StopGetQuoteStream();
            }
        }

        private void m_btnRefreshOptionQuotesSync_Click(object sender, EventArgs e)
        {
            onGetOptionQuotesRequest(false);
            GetOptionQuotesResponse resp = GetOptionQuotesResponse.GetOptionQuotes(m_authImpl, optionQuotesOptionFilters, optionQuotesOptionIds);
            displayOptionQuotes(resp);
        }

        private void m_btnRefreshOptionQuotesAsync_Click(object sender, EventArgs e)
        {
            onGetOptionQuotesRequest(true);
            GetQuoteResponse.BeginGetQuote(m_authImpl, new AsyncCallback(getOptionL1DataCallbackMethod), NextAsyncRequestID, optionQuotesOptionIds);
        }

        private void m_btnStreamOptionQuotes_Click(object sender, EventArgs e)
        {
            m_btnStreamOptionQuotes.Tag = !(bool)m_btnStreamOptionQuotes.Tag;
            bool streamStarted = (bool)m_btnStreamOptionQuotes.Tag;
            onGetOptionQuotesStream(streamStarted);
            if (streamStarted)
            {
                GetOptionQuotesResponse.GetOptionQuotesStream(m_authImpl, getOptionL1DataStreamCallbackMethod, NextAsyncRequestID, optionQuotesOptionFilters, optionQuotesOptionIds);
            }
            else
            {
                GetOptionQuotesResponse.StopGetOptionQuotesStream();
            }
        }

        private void m_btnStrategyQuotesRefresh_Click(object sender, EventArgs e)
        {
            onGetStrategyQuotesRequest(false);
            GetStrategyQuoteResponse resp = GetStrategyQuoteResponse.GetStrategyQuote(m_authImpl, strategyQuotesVariants);
            displayStrategyQuotes(resp);
        }

        private void m_btnStrategyQuotesRefreshAsync_Click(object sender, EventArgs e)
        {
            onGetStrategyQuotesRequest(true);
            GetStrategyQuoteResponse.BeginGetStrategyQuote(m_authImpl, new AsyncCallback(getStrategyL1DataCallbackMethod), NextAsyncRequestID, strategyQuotesVariants);
        }

        private void m_btnStreamStrategyQuotes_Click(object sender, EventArgs e)
        {
            m_btnStreamStrategyQuotes.Tag = !(bool)m_btnStreamStrategyQuotes.Tag;
            bool streamStarted = (bool)m_btnStreamStrategyQuotes.Tag;
            onGetStrategyQuotesStream(streamStarted);
            if (streamStarted)
            {
                GetStrategyQuoteResponse.GetStrategyQuoteStream(m_authImpl, getStrategyL1DataStreamCallbackMethod, NextAsyncRequestID, strategyQuotesVariants);
            }
            else
            {
                GetStrategyQuoteResponse.StopGetStrategyQuoteStream();
            }
        }

        private void m_btnRefreshSymbolSearchSync_Click(object sender, EventArgs e)
        {
            onSymbolSearchRequest(false);
            ulong offset = 0;
            try
            {
                offset = Convert.ToUInt64(m_txtOffset.Text);
            }
            catch
            {
            }
            SearchSymbolsResponse resp = SearchSymbolsResponse.SearchSymbols(m_authImpl, m_txtPrefix.Text, offset);
            displaySymbolSearch(resp);
        }

        private void m_btnRefreshSymbolSearchAsync_Click(object sender, EventArgs e)
        {
            onSymbolSearchRequest(true);
            ulong offset = 0;
            try
            {
                offset = Convert.ToUInt64(m_txtOffset.Text);
            }
            catch
            {
            }
            SearchSymbolsResponse.BeginSearchSymbols(m_authImpl, new AsyncCallback(getSymbolsCallbackMethod), NextAsyncRequestID, m_txtPrefix.Text, offset);
        }

        private void m_btnRefreshCandlesSync_Click(object sender, EventArgs e)
        {
            if (m_selectedSymbolId == null)
            {
                MessageBox.Show("Select a security first");
                return;
            }

            onGetCandlesRequest(false);
            CandlesGranularity interval = GetEnum<CandlesGranularity>(m_cmbInterval);
            GetCandlesResponse resp = GetCandlesResponse.GetCandles(m_authImpl, (ulong)m_selectedSymbolId, m_dtpGetCandlesStartTime.Value, m_dtpGetCandlesEndTime.Value, interval);
            displayCandles(resp);
        }

        private void m_btnRefreshCandlesAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedSymbolId == null)
            {
                MessageBox.Show("Select a security first");
                return;
            }

            onGetCandlesRequest(true);
            CandlesGranularity interval = GetEnum<CandlesGranularity>(m_cmbInterval);
            GetCandlesResponse.BeginGetCandles(m_authImpl, new AsyncCallback(getCandlesCallbackMethod), NextAsyncRequestID, (ulong)m_selectedSymbolId, m_dtpGetCandlesStartTime.Value, m_dtpGetCandlesEndTime.Value, interval);
        }

        private void m_btnInsertOrder_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            InsertOrderDetailsDlg dlg = new InsertOrderDetailsDlg(this);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (dlg.IsStrategyOrder)
                {
                    InsertStrategyOrderRequest insertStrategyOrderRequest = new InsertStrategyOrderRequest();
                    insertStrategyOrderRequest.m_accountNumber = m_selectedAccountNumber;
                    insertStrategyOrderRequest.m_symbolId = dlg.SymbolID;
                    insertStrategyOrderRequest.m_limitPrice = dlg.LimitPrice;
                    insertStrategyOrderRequest.m_orderType = dlg.OrderType;
                    insertStrategyOrderRequest.m_timeInForce = dlg.TimeInForce;
                    insertStrategyOrderRequest.m_primaryRoute = dlg.PrimaryRoute;
                    insertStrategyOrderRequest.m_secondaryRoute = dlg.SecondaryRoute;
                    insertStrategyOrderRequest.m_legs = dlg.OrderLegs;
                    insertStrategyOrderRequest.m_strategy = dlg.StrategyType;

                    if (dlg.IsAsync)
                    {
                        onInsertStrategyOrderRequest(true);
                        InsertStrategyOrderResponse.BeginInsertStrategyOrder(AuthImpl, new AsyncCallback(insertStrategyOrderCallbackMethod), NextAsyncRequestID, insertStrategyOrderRequest);
                    }
                    else
                    {
                        onInsertStrategyOrderRequest(false);
                        InsertStrategyOrderResponse resp = InsertStrategyOrderResponse.InsertStrategyOrder(AuthImpl, insertStrategyOrderRequest);
                        processInsertStrategyOrderResponse(resp);
                    }
                }
                else if (dlg.IsBracketOrder)
                {
                    if (dlg.IsAsync)
                    {
                        onInsertBracketOrderRequest(true);
                        InsertBracketOrderResponse.BeginInsertBracketOrder(AuthImpl, new AsyncCallback(insertBracketOrderCallbackMethod), NextAsyncRequestID, 
                                                                           m_selectedAccountNumber, dlg.SymbolID, dlg.PrimaryRoute, dlg.SecondaryRoute, dlg.BracketComponents);
                    }
                    else
                    {
                        onInsertBracketOrderRequest(false);
                        InsertBracketOrderResponse resp = InsertBracketOrderResponse.InsertBracketOrder(AuthImpl, m_selectedAccountNumber, dlg.SymbolID, 
                                                                                                        dlg.PrimaryRoute, dlg.SecondaryRoute, dlg.BracketComponents);
                        processInsertBracketOrderResponse(resp);
                    }
                }
                else
                {
                    InsertOrderRequest insertOrderRequest = new InsertOrderRequest();
                    insertOrderRequest.m_accountNumber = m_selectedAccountNumber;
                    insertOrderRequest.m_symbolId = dlg.SymbolID;
                    insertOrderRequest.m_quantity = dlg.Quantity;
                    insertOrderRequest.m_icebergQuantity = dlg.IcebergQuantity;
                    insertOrderRequest.m_minQuantity = dlg.MinQuantity;
                    insertOrderRequest.m_limitPrice = dlg.LimitPrice;
                    insertOrderRequest.m_stopPrice = dlg.StopPrice;
                    insertOrderRequest.m_isAllOrNone = dlg.IsAllOrNone;
                    insertOrderRequest.m_isAnonymous = dlg.IsAnonymous;
                    insertOrderRequest.m_orderType = dlg.OrderType;
                    insertOrderRequest.m_timeInForce = dlg.TimeInForce;
                    insertOrderRequest.m_gtdDate = dlg.GtdDate;
                    insertOrderRequest.m_action = dlg.Action;
                    insertOrderRequest.m_primaryRoute = dlg.PrimaryRoute;
                    insertOrderRequest.m_secondaryRoute = dlg.SecondaryRoute;
                    insertOrderRequest.m_isLimitOffsetInDollar = dlg.IsLimitOffsetInDollar;

                    if (dlg.IsAsync)
                    {
                        onInsertOrderRequest(true);
                        InsertOrderResponse.BeginInsertOrder(m_authImpl, new AsyncCallback(insertOrderCallbackMethod), NextAsyncRequestID, insertOrderRequest);
                    }
                    else
                    {
                        onInsertOrderRequest(false);
                        InsertOrderResponse resp = InsertOrderResponse.InsertOrder(m_authImpl, insertOrderRequest);
                        processInsertOrderResponse(resp);
                    }
                }
            }
            dlg.Dispose();
        }

        private void m_btnCancelOrder_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            if (m_selectedOrderId == null)
            {
                MessageBox.Show("Select an order first");
                return;
            }

            onCancelOrdersRequest(false);
            CancelOrderResponse resp = CancelOrderResponse.CancelOrder(m_authImpl, m_selectedAccountNumber, (ulong)m_selectedOrderId);
            processCancelOrderResponse(resp);
        }

        private void m_btnCancelOrderAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            if (m_selectedOrderId == null)
            {
                MessageBox.Show("Select an order first");
                return;
            }

            onCancelOrdersRequest(true);
            CancelOrderResponse.BeginCancelOrder(m_authImpl, new AsyncCallback(cancelOrderCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber, (ulong)m_selectedOrderId);
        }

        private void m_btnReplaceOrder_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            if (m_selectedOrderId == null)
            {
                MessageBox.Show("Select an order first");
                return;
            }

            var selectedRow = m_grdOrders.SelectedRows[0];
            ulong groupId = Convert.ToUInt64(selectedRow.Cells[m_colOrdersOrderGroupId.Index].Value);
            // if selected order is a bracket order
            if (groupId != Constants.InvalidULong && groupId != 0)
            {
                List<BracketOrderComponent> bracketOrderComponents = new List<BracketOrderComponent>();

                for (int i = 0; i < m_grdOrders.RowCount; ++i)
                {
                    // there can be no more than 3 orders from the same bracket order
                    if (Convert.ToUInt64(m_grdOrders.Rows[i].Cells[m_colOrdersOrderGroupId.Index].Value) == groupId || bracketOrderComponents.Count >= 3)
                    {
                        var currentRow = m_grdOrders.Rows[i];

                        BracketOrderComponent component = new BracketOrderComponent();
                        component.m_orderId = Convert.ToUInt64(currentRow.Cells[m_colOrdersId.Index].Value);
                        component.m_orderClass = (OrderClass)Enum.Parse(typeof(OrderClass), currentRow.Cells[m_colOrdersOrderClass.Index].Value.ToString());
                        component.m_orderType = (OrderType)Enum.Parse(typeof(OrderType), currentRow.Cells[m_colOrdersOrderType.Index].Value.ToString());
                        component.m_limitPrice = Convert.ToDouble(currentRow.Cells[m_colOrdersLimitPrice.Index].Value);
                        component.m_stopPrice = Convert.ToDouble(currentRow.Cells[m_colOrdersStopPrice.Index].Value);
                        component.m_timeInForce = (OrderTimeInForce)Enum.Parse(typeof(OrderTimeInForce), currentRow.Cells[m_colOrdersTimeInForce.Index].Value.ToString());
                        component.m_quantity = Convert.ToDouble(currentRow.Cells[m_colOrdersTotalQuantity.Index].Value);
                        component.m_action = (OrderAction)Enum.Parse(typeof(OrderAction), currentRow.Cells[m_colOrdersSide.Index].Value.ToString());

                        bracketOrderComponents.Add(component);
                    }
                }

                ReplaceBracketOrderDlg dlg = new ReplaceBracketOrderDlg(this, bracketOrderComponents);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (dlg.IsAsync)
                    {
                        onReplaceBracketOrderRequest(true);
                        ReplaceBracketOrderResponse.BeginReplaceBracketOrder(m_authImpl, new AsyncCallback(replaceBracketOrderCallbackMethod), NextAsyncRequestID, 
                                                                             SelectedAccountNumber, dlg.BracketComponents);
                    }
                    else
                    {
                        onReplaceBracketOrderRequest(false);
                        ReplaceBracketOrderResponse resp = ReplaceBracketOrderResponse.ReplaceBracketOrder(m_authImpl, SelectedAccountNumber, dlg.BracketComponents);
                        processReplaceBracketOrderResponse(resp);
                    }
                }
                dlg.Dispose();
            }
            else    // if not a bracket order
            {
                StrategyType strategyType = (StrategyType)Enum.Parse(typeof(StrategyType), selectedRow.Cells[m_colOrdersStrategyType.Index].Value.ToString());
                bool isStrategyOrder = strategyType != StrategyType.SingleLeg;

                if (!isStrategyOrder)
                {
                    ReplaceOrderDetailsDlg dlg = new ReplaceOrderDetailsDlg(this, false);
                    dlg.OrderID = Convert.ToUInt64(selectedRow.Cells[m_colOrdersId.Index].Value);
                    dlg.Quantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersTotalQuantity.Index].Value);
                    dlg.IcebergQuantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersIcebergQty.Index].Value);
                    dlg.MinQuantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersMinQty.Index].Value);
                    dlg.LimitPrice = Convert.ToDouble(selectedRow.Cells[m_colOrdersLimitPrice.Index].Value);
                    dlg.StopPrice = Convert.ToDouble(selectedRow.Cells[m_colOrdersStopPrice.Index].Value);
                    dlg.OrderType = (OrderType)Enum.Parse(typeof(OrderType), selectedRow.Cells[m_colOrdersOrderType.Index].Value.ToString());
                    dlg.TimeInForce = (OrderTimeInForce)Enum.Parse(typeof(OrderTimeInForce), selectedRow.Cells[m_colOrdersTimeInForce.Index].Value.ToString());
                    dlg.GtdDate = (selectedRow.Cells[m_colOrdersGtdDate.Index].Value == null || String.IsNullOrEmpty(selectedRow.Cells[m_colOrdersGtdDate.Index].Value.ToString()) ? DataTypeTraits<DateTime>.InvalidValue : Convert.ToDateTime(selectedRow.Cells[m_colOrdersGtdDate.Index].Value));
                    dlg.IsLimitOffsetInDollar = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsLimitOffsetInDollar.Index].Value);
                    dlg.IsAllOrNone = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsAllOrNone.Index].Value);
                    dlg.IsAnonymous = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsAnon.Index].Value);

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        ReplaceOrderRequest replaceOrderRequest = new ReplaceOrderRequest();
                        replaceOrderRequest.m_accountNumber = m_selectedAccountNumber;
                        replaceOrderRequest.m_orderId = dlg.OrderID;
                        replaceOrderRequest.m_quantity = dlg.Quantity;
                        replaceOrderRequest.m_icebergQuantity = dlg.IcebergQuantity;
                        replaceOrderRequest.m_minQuantity = dlg.MinQuantity;
                        replaceOrderRequest.m_limitPrice = dlg.LimitPrice;
                        replaceOrderRequest.m_stopPrice = dlg.StopPrice;
                        replaceOrderRequest.m_orderType = dlg.OrderType;
                        replaceOrderRequest.m_timeInForce = dlg.TimeInForce;
                        replaceOrderRequest.m_gtdDate = dlg.GtdDate;
                        replaceOrderRequest.m_isLimitOffsetInDollar = dlg.IsLimitOffsetInDollar;
                        replaceOrderRequest.m_isAllOrNone = dlg.IsAllOrNone;
                        replaceOrderRequest.m_isAnonymous = dlg.IsAnonymous;

                        if (dlg.IsAsync)
                        {
                            onReplaceOrderRequest(true);
                            ReplaceOrderResponse.BeginReplaceOrder(m_authImpl, new AsyncCallback(replaceOrderCallbackMethod), NextAsyncRequestID, replaceOrderRequest);
                        }
                        else
                        {
                            onReplaceOrderRequest(false);
                            ReplaceOrderResponse resp = ReplaceOrderResponse.ReplaceOrder(m_authImpl, replaceOrderRequest);
                            processReplaceOrderResponse(resp);
                        }
                    }
                    dlg.Dispose();
                }
                else    // if strategy order
                {
                    ReplaceOrderDetailsDlg dlg = new ReplaceOrderDetailsDlg(this, true);
                    dlg.OrderID = Convert.ToUInt64(selectedRow.Cells[m_colOrdersId.Index].Value);
                    dlg.Quantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersTotalQuantity.Index].Value);
                    dlg.IcebergQuantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersIcebergQty.Index].Value);
                    dlg.MinQuantity = Convert.ToDouble(selectedRow.Cells[m_colOrdersMinQty.Index].Value);
                    dlg.LimitPrice = Convert.ToDouble(selectedRow.Cells[m_colOrdersLimitPrice.Index].Value);
                    dlg.StopPrice = Convert.ToDouble(selectedRow.Cells[m_colOrdersStopPrice.Index].Value);
                    dlg.OrderType = (OrderType)Enum.Parse(typeof(OrderType), selectedRow.Cells[m_colOrdersOrderType.Index].Value.ToString());
                    dlg.TimeInForce = (OrderTimeInForce)Enum.Parse(typeof(OrderTimeInForce), selectedRow.Cells[m_colOrdersTimeInForce.Index].Value.ToString());
                    dlg.GtdDate = (selectedRow.Cells[m_colOrdersGtdDate.Index].Value == null || String.IsNullOrEmpty(selectedRow.Cells[m_colOrdersGtdDate.Index].Value.ToString()) ? DataTypeTraits<DateTime>.InvalidValue : Convert.ToDateTime(selectedRow.Cells[m_colOrdersGtdDate.Index].Value));
                    dlg.IsLimitOffsetInDollar = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsLimitOffsetInDollar.Index].Value);
                    dlg.IsAllOrNone = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsAllOrNone.Index].Value);
                    dlg.IsAnonymous = Convert.ToBoolean(selectedRow.Cells[m_colOrdersIsAnon.Index].Value);

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        ReplaceStrategyOrderRequest replaceStrategyOrderRequest = new ReplaceStrategyOrderRequest();
                        replaceStrategyOrderRequest.m_accountNumber = m_selectedAccountNumber;
                        replaceStrategyOrderRequest.m_orderId = dlg.OrderID;
                        replaceStrategyOrderRequest.m_quantity = dlg.Quantity;
                        replaceStrategyOrderRequest.m_limitPrice = dlg.LimitPrice;
                        replaceStrategyOrderRequest.m_orderType = dlg.OrderType;
                        replaceStrategyOrderRequest.m_timeInForce = dlg.TimeInForce;

                        if (dlg.IsAsync)
                        {
                            onReplaceStrategyOrderRequest(true);
                            ReplaceStrategyOrderResponse.BeginReplaceStrategyOrder(m_authImpl, new AsyncCallback(replaceStrategyOrderCallbackMethod), NextAsyncRequestID, replaceStrategyOrderRequest);
                        }
                        else
                        {
                            onReplaceStrategyOrderRequest(false);
                            ReplaceStrategyOrderResponse resp = ReplaceStrategyOrderResponse.ReplaceStrategyOrder(m_authImpl, replaceStrategyOrderRequest);
                            processReplaceStrategyOrderResponse(resp);
                        }
                    }
                    dlg.Dispose();
                }
            }
        }

        private void m_btnRefreshPositionsSync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetPositionsRequest(false);
            GetPositionsResponse resp = GetPositionsResponse.GetPositions(m_authImpl, m_selectedAccountNumber);
            displayPositions(resp);
        }

        private void m_btnRefreshPositionsAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetPositionsRequest(true);
            GetPositionsResponse.BeginGetPositions(m_authImpl, new AsyncCallback(getPositionsCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber);
        }

        private void m_btnRefreshAccountsSync_Click(object sender, EventArgs e)
        {
            onGetAccountsRequest(false);
            GetAccountsResponse resp = GetAccountsResponse.GetAccounts(m_authImpl);
            displayAccounts(resp);
        }

        private void m_btnRefreshAsync_Click(object sender, EventArgs e)
        {
            onGetAccountsRequest(true);
            GetAccountsResponse.BeginGetAccounts(m_authImpl, new AsyncCallback(getAccountsServiceCallbackMethod), NextAsyncRequestID);
        }

        private void m_btnRefreshOrdersSync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            if (m_rbGetOrdersByDate.Checked)
            {
                onGetOrdersRequest(false);
                OrderStateFilterTypes stateFilter = GetEnum<OrderStateFilterTypes>(m_cmbStateFilter);
                GetOrdersResponse resp = GetOrdersResponse.GetOrders(m_authImpl, m_selectedAccountNumber, stateFilter, m_dtpGetOrdersStartTime.Value, m_dtpGetOrdersEndTime.Value);
                displayOrders(resp);
            }
            else
            {
                onGetOrderByIdRequest(false);
                GetOrdersResponse resp = GetOrdersByIDResponse.GetOrdersByID(m_authImpl, m_selectedAccountNumber, orderIds);
                displayOrders(resp);
            }
        }

        private void m_btnRefreshOrdersAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            if (m_rbGetOrdersByDate.Checked)
            {
                onGetOrdersRequest(true);
                OrderStateFilterTypes stateFilter = GetEnum<OrderStateFilterTypes>(m_cmbStateFilter);
                GetOrdersResponse.BeginGetOrders(m_authImpl, new AsyncCallback(getOrdersCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber, stateFilter, m_dtpGetOrdersStartTime.Value, m_dtpGetOrdersEndTime.Value);
            }
            else
            {
                onGetOrderByIdRequest(true);
                GetOrdersByIDResponse.BeginGetOrdersByID(m_authImpl, new AsyncCallback(getOrdersByIDCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber, orderIds);
            }
        }

        private void m_btnRefreshOptionsSync_Click(object sender, EventArgs e)
        {
            List<ulong> symbolIds = optionChainSymbolIds;
            if (symbolIds.Count != 1)
            {
                MessageBox.Show("Select one security");
                return;
            }

            onGetOptionsRequest(false);
            GetOptionsResponse resp = GetOptionsResponse.GetOptions(m_authImpl, symbolIds[0]);
            displayOptions(resp);
        }

        private void m_btnRefreshOptionsAsync_Click(object sender, EventArgs e)
        {
            List<ulong> symbolIds = optionChainSymbolIds;
            if (symbolIds.Count != 1)
            {
                MessageBox.Show("Select one security");
                return;
            }

            onGetOptionsRequest(true);
            GetOptionsResponse.BeginGetOptions(m_authImpl, new AsyncCallback(getOptionChainShortAltCallbackMethod), NextAsyncRequestID, symbolIds[0]);
        }

        private void m_btnRefreshExecutionsSync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetExecutionsRequest(false);
            GetExecutionsResponse resp = GetExecutionsResponse.GetExecutions(m_authImpl, m_selectedAccountNumber, m_dtpGetExecutionsStartTime.Value, m_dtpGetExecutionsEndTime.Value);
            displayExecutions(resp);
        }

        private void m_btnRefreshExecutionsAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetExecutionsRequest(true);
            GetExecutionsResponse.BeginGetExecutions(m_authImpl, new AsyncCallback(getExecutionsCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber, m_dtpGetExecutionsStartTime.Value, m_dtpGetExecutionsEndTime.Value);
        }

        private void m_btnRefreshBalancesSync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetBalancesRequest(false);
            GetBalancesResponse resp = GetBalancesResponse.GetBalances(m_authImpl, m_selectedAccountNumber);
            displayBalances(resp);
        }

        private void m_btnRefreshBalancesAsync_Click(object sender, EventArgs e)
        {
            if (m_selectedAccountNumber == null)
            {
                MessageBox.Show("Select an account first");
                return;
            }

            onGetBalancesRequest(true);
            GetBalancesResponse.BeginGetBalances(m_authImpl, new AsyncCallback(getBalancesCallbackMethod), NextAsyncRequestID, m_selectedAccountNumber);
        }

        private void m_btnRefreshMarketsSync_Click(object sender, EventArgs e)
        {
            onGetMarketsRequest(false);
            GetMarketsResponse resp = GetMarketsResponse.GetMarkets(m_authImpl);
            displayMarkets(resp);
        }

        private void m_btnRefreshMarketsAsync_Click(object sender, EventArgs e)
        {
            onGetMarketsRequest(true);
            GetMarketsResponse.BeginGetMarkets(m_authImpl, new AsyncCallback(getListingsExchangeInfoCallbackMethod), NextAsyncRequestID);
        }

        private void m_btnRefreshSymbolsSync_Click(object sender, EventArgs e)
        {
            onGetSymbolsRequest(false);
            GetSymbolsResponse resp = GetSymbolsResponse.GetSymbols(m_authImpl, symbolDataSymbolIds, symbolDataSymbolNames);
            displaySymbols(resp);
        }

        private void m_btnRefreshSymbolsAsync_Click(object sender, EventArgs e)
        {
            onGetSymbolsRequest(true);
            GetSymbolsResponse.BeginGetSymbols(m_authImpl, new AsyncCallback(getSymbolDataCallbackMethod), NextAsyncRequestID, symbolDataSymbolIds, symbolDataSymbolNames);
        }

        private void m_btnGetTime_Click(object sender, EventArgs e)
        {
            GetServerTimeResponse resp = GetServerTimeResponse.GetServerTime(m_authImpl);
            string respStr = ResponseDumper.ToString(resp);
            MessageBox.Show(respStr);
        }

        private void m_btnAuth_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                m_authImpl = AuthAgent.GetInstance().Authenticate(m_txtRefreshToken.Text, m_chkIsDemo.Checked);
                if (m_authImpl == null || !m_authImpl.IsAuthenticated || !m_authImpl.IsValid)
                {
                    throw new Exception("Login failed");
                }
                m_lblAuthStatus.Text = "Authenticated";
                subscribeToOrderAndExecutionNotifications();
                enable(true);
            }
            catch (Exception ex)
            {
                m_lblAuthStatus.Text = String.Format("Not Authenticated - {0}", ex.Message);
                enable(false);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void m_txtRefreshToken_TextChanged(object sender, EventArgs e)
        {
            updateUI();
        }

        private void m_grdAccounts_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRows = m_grdAccounts.SelectedRows;
            if (selectedRows.Count == 1)
            {
                m_selectedAccountNumber = selectedRows[0].Cells[m_colAccountNumber.Index].Value.ToString();
            }
            else
            {
                m_selectedAccountNumber = null;
            }
        }

        private void m_rbGetOrdersByDate_CheckedChanged(object sender, EventArgs e)
        {
            updateUI();
        }

        private void m_rbGetOrderByID_CheckedChanged(object sender, EventArgs e)
        {
            updateUI();
        }

        private void m_grdOrders_SelectionChanged(object sender, EventArgs e)
        {
            m_grdOrderLegs.Rows.Clear();
            var selectedRows = m_grdOrders.SelectedRows;
            if (selectedRows.Count == 1)
            {
                m_selectedOrderId = Convert.ToUInt64(selectedRows[0].Cells[m_colOrdersId.Index].Value);
                
                List<OrderLegData> legs;
                if (m_legs.TryGetValue((ulong)m_selectedOrderId, out legs))
                {
                    for (int i = 0; i < legs.Count; ++i)
                    {
                        addOrderLegRow(legs[i]);
                    }
                }
            }
            else
            {
                m_selectedOrderId = null;
            }
        }

        private void m_grdSymbolSearch_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRows = m_grdSymbolSearch.SelectedRows;
            if (selectedRows.Count == 1)
            {
                m_selectedSymbolId = Convert.ToUInt64(selectedRows[0].Cells[m_colSymbolSearchSymbolId.Index].Value);
            }
            else
            {
                m_selectedSymbolId = null;
            }

            StringBuilder str = new StringBuilder();
            StringBuilder symbolIds = new StringBuilder();
            for (int i = 0; i < selectedRows.Count; ++i)
            {
                str.Append(selectedRows[i].Cells[m_colSymbolSearchSymbol.Index].Value);
                symbolIds.Append(selectedRows[i].Cells[m_colSymbolSearchSymbolId.Index].Value);
                if (i < selectedRows.Count - 1)
                {
                    str.Append(", ");
                    symbolIds.Append(", ");
                }
            }
            m_lblSymbolSearchSelectedSymbols.Text = str.ToString();
            m_txtSymbolDataSymbolIds.Text = m_txtQuotesSymbolIds.Text = m_txtOptionChainSymbolIds.Text = symbolIds.ToString();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_rtbActivityLog.Clear();
        }

        private void m_grdSymbols_SelectionChanged(object sender, EventArgs e)
        {
            m_grdSymbolsMinTicks.Rows.Clear();
            m_grdSymbolsContractDeliverables.Rows.Clear();

            var selectedRows = m_grdSymbols.SelectedRows;
            if (selectedRows.Count == 1)
            {
                ulong selectedSymbolId = Convert.ToUInt64(selectedRows[0].Cells[m_colSymbolsSymbolId.Index].Value);

                KeyValuePair<List<MinTickData>, ContractDeliverables> symbolData;
                if (m_symbolData.TryGetValue(selectedSymbolId, out symbolData))
                {
                    addMinTickRows(symbolData.Key);
                    addContractDeliverablesRows(symbolData.Value);
                }
            }
        }

        private void m_grdMarkets_SelectionChanged(object sender, EventArgs e)
        {
            m_lstTradingVenues.Items.Clear();
            m_lstPrimaryOrderRoutes.Items.Clear();
            m_lstSecondaryOrderRoutes.Items.Clear();
            m_lstLevel1Feeds.Items.Clear();
            m_lstLevel2Feeds.Items.Clear();

            var selectedRows = m_grdMarkets.SelectedRows;
            if (selectedRows.Count == 1)
            {
                string selectedMarketName = selectedRows[0].Cells[m_colMarketsName.Index].Value.ToString();

                MarketsData marketsData;
                if (m_marketsData.TryGetValue(selectedMarketName, out marketsData))
                {
                    addMarketsDataItems(marketsData);
                }
            }
        }

        private void m_txtPrefix_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                m_btnRefreshSymbolSearchSync_Click(sender, EventArgs.Empty);
            }
        }

        private void m_txtOffset_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                m_btnRefreshSymbolSearchSync_Click(sender, EventArgs.Empty);
            }
        }

        #endregion

        #region Callback Methods

        private void getAccountsServiceCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayAccountsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetAccountsResponse);
            GetAccountsResponse.EndGetAccounts(asyncResult);
        }

        private void getL1DataCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayQuotesThreadFunc));
            thread.Start(asyncResult.AsyncState as GetQuoteResponse);
            GetQuoteResponse.EndGetQuote(asyncResult);
        }

        private void getL1DataStreamCallbackMethod(GetQuoteResponse quotesResp)
        {
            displayQuotes(quotesResp);
        }

        private void getOptionL1DataCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayOptionQuotesThreadFunc));
            thread.Start(asyncResult.AsyncState as GetOptionQuotesResponse);
            GetOptionQuotesResponse.EndGetOptionQuotes(asyncResult);
        }

        private void getStrategyL1DataCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayStrategyQuotesThreadFunc));
            thread.Start(asyncResult.AsyncState as GetStrategyQuoteResponse);
            GetStrategyQuoteResponse.EndGetStrategyQuote(asyncResult);
        }

        private void getOptionL1DataStreamCallbackMethod(GetOptionQuotesResponse quotesResp)
        {
            displayOptionQuotes(quotesResp);
        }

        private void getStrategyL1DataStreamCallbackMethod(GetStrategyQuoteResponse quotesResp)
        {
            displayStrategyQuotes(quotesResp);
        }

        private void getListingsExchangeInfoCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayMarketsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetMarketsResponse);
            GetMarketsResponse.EndGetMarkets(asyncResult);
        }

        private void getOptionChainShortAltCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayOptionsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetOptionsResponse);
            GetOptionsResponse.EndGetOptions(asyncResult);
        }

        private void getSymbolDataCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displaySymbolsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetSymbolsResponse);
            GetSymbolsResponse.EndGetSymbols(asyncResult);
        }

        private void getOrdersByIDCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayOrdersThreadFunc));
            thread.Start(asyncResult.AsyncState as GetOrdersResponse);
            GetOrdersByIDResponse.EndGetOrdersByID(asyncResult);
        }

        private void getSymbolsCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displaySymbolsSearchThreadFunc));
            thread.Start(asyncResult.AsyncState as SearchSymbolsResponse);
            SearchSymbolsResponse.EndSearchSymbols(asyncResult);
        }

        private void getBalancesCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayBalancesThreadFunc));
            thread.Start(asyncResult.AsyncState as GetBalancesResponse);
            GetBalancesResponse.EndGetBalances(asyncResult);
        }

        private void getPositionsCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayPositionsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetPositionsResponse);
            GetPositionsResponse.EndGetPositions(asyncResult);
        }

        private void getOrdersCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayOrdersThreadFunc));
            thread.Start(asyncResult.AsyncState as GetOrdersResponse);
            GetOrdersResponse.EndGetOrders(asyncResult);
        }

        private void getExecutionsCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayExecutionsThreadFunc));
            thread.Start(asyncResult.AsyncState as GetExecutionsResponse);
            GetExecutionsResponse.EndGetExecutions(asyncResult);
        }

        private void getCandlesCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayCandlesThreadFunc));
            thread.Start(asyncResult.AsyncState as GetCandlesResponse);
            GetCandlesResponse.EndGetCandles(asyncResult);
        }

        private void cancelOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayCancelOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as CancelOrderResponse);
            CancelOrderResponse.EndCancelOrder(asyncResult);
        }

        internal void InsertOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as InsertOrderImpactResponse);
            InsertOrderImpactResponse.EndInsertOrderImpact(asyncResult);
        }

        internal void InsertStrategyOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertStrategyOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as InsertOrderImpactResponse);
            InsertOrderImpactResponse.EndInsertOrderImpact(asyncResult);
        }

        internal void InsertBracketOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertBracketOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as BracketOrderImpactResponse);
            BracketOrderImpactResponse.EndBracketOrderImpact(asyncResult);
        }

        private void insertOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as InsertOrderResponse);
            InsertOrderResponse.EndInsertOrder(asyncResult);
        }

        private void insertStrategyOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertStrategyOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as InsertStrategyOrderResponse);
            InsertStrategyOrderResponse.EndInsertStrategyOrder(asyncResult);
        }

        private void insertBracketOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayInsertBracketOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as InsertBracketOrderResponse);
            InsertBracketOrderResponse.EndInsertBracketOrder(asyncResult);
        }

        internal void ReplaceOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as ReplaceOrderImpactResponse);
            ReplaceOrderImpactResponse.EndReplaceOrderImpact(asyncResult);
        }

        internal void ReplaceStrategyOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceStrategyOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as StrategyReplaceImpactResponse);
            StrategyReplaceImpactResponse.EndStrategyReplaceImpact(asyncResult);
        }

        internal void ReplaceBracketOrderImpactCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceBracketOrderImpactThreadFunc));
            thread.Start(asyncResult.AsyncState as BracketReplaceImpactResponse);
            BracketReplaceImpactResponse.EndBracketReplaceImpact(asyncResult);
        }

        private void replaceOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as ReplaceOrderResponse);
            ReplaceOrderResponse.EndReplaceOrder(asyncResult);
        }

        private void replaceStrategyOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceStrategyOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as ReplaceStrategyOrderResponse);
            ReplaceStrategyOrderResponse.EndReplaceStrategyOrder(asyncResult);
        }

        private void replaceBracketOrderCallbackMethod(IAsyncResult asyncResult)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(displayReplaceBracketOrderThreadFunc));
            thread.Start(asyncResult.AsyncState as ReplaceBracketOrderResponse);
            ReplaceBracketOrderResponse.EndReplaceBracketOrder(asyncResult);
        }

        private void orderAndExecutionNotificationsStreamCallbackMethod(OrderAndExecutionNotificationsResponse resp)
        {
            processOrdersAndExecutionsPushNotification(resp);
        }

        #endregion

        #region Private Static Methods
        
        private static void displayAccountsThreadFunc(object obj)
        {
            s_mainForm.displayAccounts(obj as GetAccountsResponse);
        }

        private static void displayPositionsThreadFunc(object obj)
        {
            s_mainForm.displayPositions(obj as GetPositionsResponse);
        }

        private static void displayBalancesThreadFunc(object obj)
        {
            s_mainForm.displayBalances(obj as GetBalancesResponse);
        }

        private static void displayExecutionsThreadFunc(object obj)
        {
            s_mainForm.displayExecutions(obj as GetExecutionsResponse);
        }

        private static void displayOrdersThreadFunc(object obj)
        {
            s_mainForm.displayOrders(obj as GetOrdersResponse);
        }

        private static void displayCancelOrderThreadFunc(object obj)
        {
            s_mainForm.processCancelOrderResponse(obj as CancelOrderResponse);
        }

        private static void displayInsertOrderThreadFunc(object obj)
        {
            s_mainForm.processInsertOrderResponse(obj as InsertOrderResponse);
        }

        private static void displayInsertStrategyOrderThreadFunc(object obj)
        {
            s_mainForm.processInsertStrategyOrderResponse(obj as InsertStrategyOrderResponse);
        }

        private static void displayInsertBracketOrderThreadFunc(object obj)
        {
            s_mainForm.processInsertBracketOrderResponse(obj as InsertBracketOrderResponse);
        }

        private static void displayInsertOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayInsertOrderImpactResponse(obj as InsertOrderImpactResponse);
        }

        private static void displayInsertStrategyOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayInsertStrategyOrderImpactResponse(obj as StrategyOrderImpactResponse);
        }

        private static void displayInsertBracketOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayInsertBracketOrderImpactResponse(obj as BracketOrderImpactResponse);
        }

        private static void displayReplaceOrderThreadFunc(object obj)
        {
            s_mainForm.processReplaceOrderResponse(obj as ReplaceOrderResponse);
        }

        private static void displayReplaceStrategyOrderThreadFunc(object obj)
        {
            s_mainForm.processReplaceStrategyOrderResponse(obj as ReplaceStrategyOrderResponse);
        }

        private static void displayReplaceBracketOrderThreadFunc(object obj)
        {
            s_mainForm.processReplaceBracketOrderResponse(obj as ReplaceBracketOrderResponse);
        }

        private static void displayReplaceOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayReplaceOrderImpactResponse(obj as ReplaceOrderImpactResponse);
        }

        private static void displayReplaceStrategyOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayReplaceStrategyOrderImpactResponse(obj as StrategyReplaceImpactResponse);
        }

        private static void displayReplaceBracketOrderImpactThreadFunc(object obj)
        {
            s_mainForm.DisplayReplaceBracketOrderImpactResponse(obj as BracketReplaceImpactResponse);
        }

        private static void displayCandlesThreadFunc(object obj)
        {
            s_mainForm.displayCandles(obj as GetCandlesResponse);
        }

        private static void displaySymbolsSearchThreadFunc(object obj)
        {
            s_mainForm.displaySymbolSearch(obj as SearchSymbolsResponse);
        }

        private static void displayQuotesThreadFunc(object obj)
        {
            s_mainForm.displayQuotes(obj as GetQuoteResponse);
        }

        private static void displayOptionQuotesThreadFunc(object obj)
        {
            s_mainForm.displayOptionQuotes(obj as GetOptionQuotesResponse);
        }

        private static void displayStrategyQuotesThreadFunc(object obj)
        {
            s_mainForm.displayStrategyQuotes(obj as GetStrategyQuoteResponse);
        }

        private static void displaySymbolsThreadFunc(object obj)
        {
            s_mainForm.displaySymbols(obj as GetSymbolsResponse);
        }

        private static void displayOptionsThreadFunc(object obj)
        {
            s_mainForm.displayOptions(obj as GetOptionsResponse);
        }

        private static void displayMarketsThreadFunc(object obj)
        {
            s_mainForm.displayMarkets(obj as GetMarketsResponse);
        }

        private static List<ulong> commaSeparatedListToIdList(string text)
        {
            List<string> ids = commaSeparatedListToStringList(text);
            List<ulong> symbolIds = new List<ulong>();
            for (int i = 0; i < ids.Count; ++i)
            {
                try
                {
                    string id = ids[i];
                    if (String.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    symbolIds.Add(Convert.ToUInt64(id));
                }
                catch
                {
                }
            }
            return symbolIds;
        }

        private static List<string> commaSeparatedListToStringList(string text)
        {
            string[] strings = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(strings);
        }

        #endregion

        #region Internal Static Methods

        internal static void InitEnumCombobox<ENUM>(ComboBox cb, ENUM defValue)
        {
            Array enumVals = Enum.GetValues(typeof(ENUM));
            for (int i = 0; i < enumVals.Length; ++i)
            {
                object obj = enumVals.GetValue(i);
                string strVal = obj.ToString();
                if (strVal != "Undefined" && strVal != "Count")
                {
                    int idx = cb.Items.Add(obj);
                    if (obj.Equals(defValue))
                    {
                        cb.SelectedIndex = idx;
                    }
                }
            }
        }

        internal static ENUM GetEnum<ENUM>(ComboBox cb)
        {
            return (ENUM)(cb.SelectedItem == null ? Enum.Parse(typeof(ENUM), "Undefined") : cb.SelectedItem);
        }

        #endregion

        #region Private Methods

        private void initUI()
        {
            m_dtpGetExecutionsEndTime.Value = m_dtpGetOrdersEndTime.Value = DateTime.Now.Date.AddYears(1);
            ToolTip tt = new ToolTip();
            tt.SetToolTip(m_txtGetOrderByIdOrderId, "Comma-separated list of Order IDs");
        }

        private void updateUI()
        {
            m_btnAuth.Enabled = (m_txtRefreshToken.TextLength > 0);
            m_lblGetOrdersByDateStartTime.Enabled = m_lblGetOrdersByDateEndTime.Enabled = m_lblGetOrdersByDateStateFilter.Enabled = m_cmbStateFilter.Enabled = m_rbGetOrdersByDate.Checked;
            m_lblGetOrderByIdOrderId.Enabled = m_txtGetOrderByIdOrderId.Enabled = m_rbGetOrderByID.Checked;
        }

        private void enable(bool enabled)
        {
            m_tabs.Enabled = m_rtbActivityLog.Enabled = m_btnGetTime.Enabled = enabled;
        }

        private void logMessage(string msg)
        {
            log(msg, false);
        }

        private void logError(string error)
        {
            log(error, true);
        }

        private void log(string msg, bool isError)
        {
            if (isError)
            {
                int selectionStart = m_rtbActivityLog.TextLength;
                string errText = String.Format("{0}: {1}{2}", DateTime.Now, msg, Environment.NewLine);
                m_rtbActivityLog.AppendText(errText);
                m_rtbActivityLog.Select(selectionStart, errText.Length);
                m_rtbActivityLog.SelectionColor = System.Drawing.Color.Red;
            }
            else
            {
                m_rtbActivityLog.AppendText(String.Format("{0}: {1}{2}", DateTime.Now, msg, Environment.NewLine));
            }
            m_rtbActivityLog.SelectionStart = m_rtbActivityLog.TextLength;
            m_rtbActivityLog.ScrollToCaret();
            m_rtbActivityLog.Refresh();
        }

        private void displayAccounts(GetAccountsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayAccountsInternal(resp);
                });
            }
            else
            {
                displayAccountsInternal(resp);
            }
        }

        private void displayAccountsInternal(GetAccountsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get accounts request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get accounts request");
                m_lblAccountDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblAccountDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
                m_lblGetAccountsUserId.Text = resp.UserId.ToString();

                List<AccountData> accounts = resp.Accounts;
                for (int i = 0; i < accounts.Count; ++i)
                {
                    AccountData account = accounts[i];
                    m_grdAccounts.Rows.Add(new object[]{account.m_type, account.m_number, account.m_status, account.m_isPrimary, account.m_isBilling, account.m_clientAccountType});
                }
            }
        }

        private void onGetAccountsRequest(bool isAsync)
        {
            m_lblAccountDataRateLimitRemainingRequestsCount.Text = m_lblAccountDataRateLimitRequestsResetTime.Text = m_lblGetAccountsUserId.Text = String.Empty;
            m_grdAccounts.Rows.Clear();
            logMessage(String.Format("Sending {0} get accounts request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayPositions(GetPositionsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayPositionsInternal(resp);
                });
            }
            else
            {
                displayPositionsInternal(resp);
            }
        }

        private void displayPositionsInternal(GetPositionsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get positions request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get positions request");
                m_lblAccountDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblAccountDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<PositionData> positions = resp.Positions;
                for (int i = 0; i < positions.Count; ++i)
                {
                    PositionData position = positions[i];
                    m_grdPositions.Rows.Add(new object[]{position.m_symbol, position.m_symbolId, position.m_openQuantity, position.m_closedQuantity, position.m_currentMarketValue, position.m_currentPrice, position.m_averageEntryPrice, position.m_closedPnl, position.m_openPnl, position.m_totalCost, position.m_isRealTime, position.m_isUnderReorg});
                }
            }
        }

        private void onGetPositionsRequest(bool isAsync)
        {
            m_lblAccountDataRateLimitRemainingRequestsCount.Text = m_lblAccountDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdPositions.Rows.Clear();
            logMessage(String.Format("Sending {0} get positions request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayBalances(GetBalancesResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayBalancesInternal(resp);
                });
            }
            else
            {
                displayBalancesInternal(resp);
            }
        }

        private void displayBalancesInternal(GetBalancesResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get balances request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get balances request");
                m_lblAccountDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblAccountDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
                addBalanceRows(resp.PerCurrencyBalances, resp.CombinedBalances, m_grdCurrentBalances);
                addBalanceRows(resp.SodPerCurrencyBalances, resp.SodCombinedBalances, m_grdSodBalances);
            }
        }

        private void addBalanceRows(List<BalanceData> perCurrencyBalances, List<BalanceData> combinedBalances, DataGridView grid)
        {
            grid.Rows.Add(new object[]{"Cash", getBalanceValue(perCurrencyBalances, CurrencyType.CAD, s_getCash), getBalanceValue(perCurrencyBalances, CurrencyType.USD, s_getCash), getBalanceValue(combinedBalances, CurrencyType.CAD, s_getCash), getBalanceValue(combinedBalances, CurrencyType.USD, s_getCash)});
            grid.Rows.Add(new object[]{"Market value", getBalanceValue(perCurrencyBalances, CurrencyType.CAD, s_getMktVal), getBalanceValue(perCurrencyBalances, CurrencyType.USD, s_getMktVal), getBalanceValue(combinedBalances, CurrencyType.CAD, s_getMktVal), getBalanceValue(combinedBalances, CurrencyType.USD, s_getMktVal)});
            grid.Rows.Add(new object[]{"Total equity", getBalanceValue(perCurrencyBalances, CurrencyType.CAD, s_getTotalEquity), getBalanceValue(perCurrencyBalances, CurrencyType.USD, s_getTotalEquity), getBalanceValue(combinedBalances, CurrencyType.CAD, s_getTotalEquity), getBalanceValue(combinedBalances, CurrencyType.USD, s_getTotalEquity)});
            grid.Rows.Add(new object[]{"Maint excess", getBalanceValue(perCurrencyBalances, CurrencyType.CAD, s_getMaintExcess), getBalanceValue(perCurrencyBalances, CurrencyType.USD, s_getMaintExcess), getBalanceValue(combinedBalances, CurrencyType.CAD, s_getMaintExcess), getBalanceValue(combinedBalances, CurrencyType.USD, s_getMaintExcess)});
            grid.Rows.Add(new object[]{"Buying power", getBalanceValue(perCurrencyBalances, CurrencyType.CAD, s_getBuyingPower), getBalanceValue(perCurrencyBalances, CurrencyType.USD, s_getBuyingPower), getBalanceValue(combinedBalances, CurrencyType.CAD, s_getBuyingPower), getBalanceValue(combinedBalances, CurrencyType.USD, s_getBuyingPower)});
        }

        private double getBalanceValue(List<BalanceData> balances, CurrencyType currency, GetBalanceValue getCash)
        {
            for (int i = 0; i < balances.Count; ++i)
            {
                BalanceData balance = balances[i];
                if (balance.m_currency != currency)
                {
                    continue;
                }
                return getCash(balance);
            }
            return 0;
        }

        private void onGetBalancesRequest(bool isAsync)
        {
            m_lblAccountDataRateLimitRemainingRequestsCount.Text = m_lblAccountDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdCurrentBalances.Rows.Clear();
            m_grdSodBalances.Rows.Clear();
            logMessage(String.Format("Sending {0} get balances request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayExecutions(GetExecutionsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayExecutionsInternal(resp);
                });
            }
            else
            {
                displayExecutionsInternal(resp);
            }
        }

        private void displayExecutionsInternal(GetExecutionsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get executions request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get executions request");
                m_lblAccountDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblAccountDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<ExecutionData> executions = resp.Executions;
                for (int i = 0; i < executions.Count; ++i)
                {
                    ExecutionData execution = executions[i];
                    addExecutionsRow(execution);
                }
            }
        }

        private void addExecutionsRow(ExecutionData execution)
        {
            m_grdExecutions.Rows.Add(new object[]{execution.m_symbol, execution.m_symbolId, execution.m_quantity, execution.m_side, execution.m_price, execution.m_id, execution.m_orderId, execution.m_orderChainId, execution.m_exchangeExecId, execution.m_timestamp, execution.m_notes, execution.m_venue, execution.m_totalCost, execution.m_orderPlacementCommission, execution.m_commission, execution.m_executionFee, execution.m_secFee, execution.m_legId, execution.m_canadianExecutionFee, execution.m_parentId});
        }

        private void updateExecutionRow(DataGridViewRow row, ExecutionData execution)
        {
            row.Cells[m_colExecutionsSymbol.Index].Value = execution.m_symbol;
            row.Cells[m_colExecutionsSymbolId.Index].Value = execution.m_symbolId;
            row.Cells[m_colExecutionsQty.Index].Value = execution.m_quantity;
            row.Cells[m_colExecutionsSide.Index].Value = execution.m_side;
            row.Cells[m_colExecutionsPrice.Index].Value = execution.m_price;
            row.Cells[m_colExecutionsId.Index].Value = execution.m_id;
            row.Cells[m_colExecutionsOrderId.Index].Value = execution.m_orderId;
            row.Cells[m_colExecutionsOrderChainId.Index].Value = execution.m_orderChainId;
            row.Cells[m_colExecutionsExchangeExecId.Index].Value = execution.m_exchangeExecId;
            row.Cells[m_colExecutionsTimestamp.Index].Value = execution.m_timestamp;
            row.Cells[m_colExecutionsNotes.Index].Value = execution.m_notes;
            row.Cells[m_colExecutionsVenue.Index].Value = execution.m_venue;
            row.Cells[m_colExecutionsTotalCost.Index].Value = execution.m_totalCost;
            row.Cells[m_colExecutionsPlacementCommission.Index].Value = execution.m_orderPlacementCommission;
            row.Cells[m_colExecutionsCommission.Index].Value = execution.m_commission;
            row.Cells[m_colExecutionsExecFee.Index].Value = execution.m_executionFee;
            row.Cells[m_colExecutionsSecFee.Index].Value = execution.m_secFee;
            row.Cells[m_colExecutionsLegId.Index].Value = execution.m_legId;
            row.Cells[m_colExecutionsCdnExecFee.Index].Value = execution.m_canadianExecutionFee;
            row.Cells[m_colExecutionsParentId.Index].Value = execution.m_parentId;
        }

        private void onGetExecutionsRequest(bool isAsync)
        {
            m_lblAccountDataRateLimitRemainingRequestsCount.Text = m_lblAccountDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdExecutions.Rows.Clear();
            logMessage(String.Format("Sending {0} get executions request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayOrders(GetOrdersResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayOrdersInternal(resp);
                });
            }
            else
            {
                displayOrdersInternal(resp);
            }
        }

        private void displayOrdersInternal(GetOrdersResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get orders request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get orders request");
                m_lblAccountDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblAccountDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<OrderData> orders = resp.Orders;
                for (int i = 0; i < orders.Count; ++i)
                {
                    addOrderRow(orders[i]);
                }
            }
        }

        private void addOrderRow(OrderData order)
        {
            m_legs[order.m_id] = order.m_legs;
            m_grdOrders.Rows.Add(new object[]{order.m_id, order.m_symbol, order.m_symbolId, order.m_totalQuantity, order.m_openQuantity, order.m_filledQuantity, order.m_canceledQuantity, order.m_side, order.m_orderType, order.m_limitPrice, order.m_stopPrice, order.m_isAllOrNone, order.m_isAnonymous, order.m_icebergQuantity, order.m_minQuantity, order.m_avgExecPrice, order.m_lastExecPrice, order.m_source, order.m_timeInForce, order.m_gtdDate, order.m_state, order.m_rejectionReason, order.m_chainId, order.m_creationTime, order.m_updateTime, order.m_notes, order.m_primaryRoute, order.m_secondaryRoute, order.m_orderRoute, order.m_venueHoldingOrder, order.m_comissionCharged, order.m_exchangeOrderId, order.m_isSignificantShareHolder, order.m_isInsider, order.m_isLimitOffsetInDollar, order.m_userId, order.m_placementCommission, order.m_strategyType, order.m_triggerStopPrice, order.m_orderGroupId, order.m_orderClass});
        }

        private void addOrderLegRow(OrderLegData leg)
        {
            m_grdOrderLegs.Rows.Add(new object[]{leg.m_legId, leg.m_symbol, leg.m_symbolId, leg.m_legRatioQuantity, leg.m_side, leg.m_avgExecPrice, leg.m_lastExecPrice});
        }

        private void updateOrderRow(DataGridViewRow row, OrderData order)
        {
            row.Cells[m_colOrdersId.Index].Value = order.m_id;
            row.Cells[m_colOrdersSymbol.Index].Value = order.m_symbol;
            row.Cells[m_colOrdersSymbolId.Index].Value = order.m_symbolId;
            row.Cells[m_colOrdersTotalQuantity.Index].Value = order.m_totalQuantity;
            row.Cells[m_colOrdersOpenQuantity.Index].Value = order.m_openQuantity;
            row.Cells[m_colOrdersFilledQuantity.Index].Value = order.m_filledQuantity;
            row.Cells[m_colOrdersCanceledQuantity.Index].Value = order.m_canceledQuantity;
            row.Cells[m_colOrdersSide.Index].Value = order.m_side;
            row.Cells[m_colOrdersOrderType.Index].Value = order.m_orderType;
            row.Cells[m_colOrdersLimitPrice.Index].Value = order.m_limitPrice;
            row.Cells[m_colOrdersStopPrice.Index].Value = order.m_stopPrice;
            row.Cells[m_colOrdersIsAllOrNone.Index].Value = order.m_isAllOrNone;
            row.Cells[m_colOrdersIsAnon.Index].Value = order.m_isAnonymous;
            row.Cells[m_colOrdersIcebergQty.Index].Value = order.m_icebergQuantity;
            row.Cells[m_colOrdersMinQty.Index].Value = order.m_minQuantity;
            row.Cells[m_colOrdersAvgExecPrice.Index].Value = order.m_avgExecPrice;
            row.Cells[m_colOrdersLastExecPrice.Index].Value = order.m_lastExecPrice;
            row.Cells[m_colOrdersSource.Index].Value = order.m_source;
            row.Cells[m_colOrdersTimeInForce.Index].Value = order.m_timeInForce;
            row.Cells[m_colOrdersGtdDate.Index].Value = order.m_gtdDate;
            row.Cells[m_colOrdersState.Index].Value = order.m_state;
            row.Cells[m_colOrdersRejectionReason.Index].Value = order.m_rejectionReason;
            row.Cells[m_colOrdersChainId.Index].Value = order.m_chainId;
            row.Cells[m_colOrdersCreationTime.Index].Value = order.m_creationTime;
            row.Cells[m_colOrdersUpdateTime.Index].Value = order.m_updateTime;
            row.Cells[m_colOrdersNotes.Index].Value = order.m_notes;
            row.Cells[m_colOrdersPrimaryRoute.Index].Value = order.m_primaryRoute;
            row.Cells[m_colOrdersSecondaryRoute.Index].Value = order.m_secondaryRoute;
            row.Cells[m_colOrdersOrderRoute.Index].Value = order.m_orderRoute;
            row.Cells[m_colOrdersVenueHoldingOrder.Index].Value = order.m_venueHoldingOrder;
            row.Cells[m_colOrdersCommissionCharged.Index].Value = order.m_comissionCharged;
            row.Cells[m_colOrdersExchangeOrderId.Index].Value = order.m_exchangeOrderId;
            row.Cells[m_colOrdersIsSigShareHolder.Index].Value = order.m_isSignificantShareHolder;
            row.Cells[m_colOrdersIsInsider.Index].Value = order.m_isInsider;
            row.Cells[m_colOrdersIsLimitOffsetInDollar.Index].Value = order.m_isLimitOffsetInDollar;
            row.Cells[m_colOrdersUserId.Index].Value = order.m_userId;
            row.Cells[m_colOrdersPlacementCommission.Index].Value = order.m_placementCommission;
            row.Cells[m_colOrdersStrategyType.Index].Value = order.m_strategyType;
            row.Cells[m_colOrdersTriggerStopPrice.Index].Value = order.m_triggerStopPrice;
            row.Cells[m_colOrdersOrderGroupId.Index].Value = order.m_orderGroupId;
            row.Cells[m_colOrdersOrderClass.Index].Value = order.m_orderClass;
        }

        private void onGetOrdersRequest(bool isAsync)
        {
            m_lblAccountDataRateLimitRemainingRequestsCount.Text = m_lblAccountDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdOrders.Rows.Clear();
            m_grdOrderLegs.Rows.Clear();
            m_legs.Clear();
            logMessage(String.Format("Sending {0} get orders request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onGetOrderByIdRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            m_grdOrders.Rows.Clear();
            m_grdOrderLegs.Rows.Clear();
            m_legs.Clear();
            logMessage(String.Format("Sending {0} get orders by ID request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void processCancelOrderResponse(CancelOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processCancelOrderResponseInternal(resp);
                });
            }
            else
            {
                processCancelOrderResponseInternal(resp);
            }
        }

        private void processCancelOrderResponseInternal(CancelOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for cancel order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for cancel order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void onCancelOrdersRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} cancel order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void processInsertOrderResponse(InsertOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processInsertOrderResponseInternal(resp);
                });
            }
            else
            {
                processInsertOrderResponseInternal(resp);
            }
        }

        private void processInsertStrategyOrderResponse(InsertStrategyOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processInsertStrategyOrderResponseInternal(resp);
                });
            }
            else
            {
                processInsertStrategyOrderResponseInternal(resp);
            }
        }

        private void processInsertBracketOrderResponse(InsertBracketOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processInsertBracketOrderResponseInternal(resp);
                });
            }
            else
            {
                processInsertBracketOrderResponseInternal(resp);
            }
        }

        private void processInsertOrderResponseInternal(InsertOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for insert order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for insert order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void processInsertStrategyOrderResponseInternal(InsertStrategyOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for insert strategy order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for insert strategy order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void processInsertBracketOrderResponseInternal(InsertBracketOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for insert bracket order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for insert strategy order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void onInsertOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} insert order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onInsertStrategyOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} insert strategy order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onInsertBracketOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} insert bracket order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void DisplayInsertOrderImpactResponse(InsertOrderImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayInsertOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayInsertOrderImpactResponseInternal(resp);
            }
        }

        internal void DisplayInsertStrategyOrderImpactResponse(StrategyOrderImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayInsertStrategyOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayInsertStrategyOrderImpactResponseInternal(resp);
            }
        }

        internal void DisplayInsertBracketOrderImpactResponse(BracketOrderImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayInsertBracketOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayInsertBracketOrderImpactResponseInternal(resp);
            }
        }

        private void displayInsertOrderImpactResponseInternal(InsertOrderImpactResponse resp)
        {
            string respStr =  ResponseDumper.ToString(resp as InsertOrderImpactResponse);
            MessageBox.Show(respStr);
        }

        private void displayInsertStrategyOrderImpactResponseInternal(StrategyOrderImpactResponse resp)
        {
            string respStr = ResponseDumper.ToString(resp as StrategyOrderImpactResponse);
            MessageBox.Show(respStr);
        }

        private void displayInsertBracketOrderImpactResponseInternal(BracketOrderImpactResponse resp)
        {
            string respStr = ResponseDumper.ToString(resp as BracketOrderImpactResponse);
            MessageBox.Show(respStr);
        }

        internal void OnInsertOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} insert order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void OnInsertStrategyOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} insert strategy order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void OnInsertBracketOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} insert bracket order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void DisplayReplaceOrderImpactResponse(ReplaceOrderImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayReplaceOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayReplaceOrderImpactResponseInternal(resp);
            }
        }

        internal void DisplayReplaceStrategyOrderImpactResponse(StrategyReplaceImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayReplaceStrategyOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayReplaceStrategyOrderImpactResponseInternal(resp);
            }
        }

        internal void DisplayReplaceBracketOrderImpactResponse(BracketReplaceImpactResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayReplaceBracketOrderImpactResponseInternal(resp);
                });
            }
            else
            {
                displayReplaceBracketOrderImpactResponseInternal(resp);
            }
        }

        private void displayReplaceOrderImpactResponseInternal(ReplaceOrderImpactResponse resp)
        {
            string respStr = ResponseDumper.ToString(resp);
            MessageBox.Show(respStr);
        }

        private void displayReplaceStrategyOrderImpactResponseInternal(StrategyReplaceImpactResponse resp)
        {
            string respStr = ResponseDumper.ToString(resp);
            MessageBox.Show(respStr);
        }

        private void displayReplaceBracketOrderImpactResponseInternal(BracketReplaceImpactResponse resp)
        {
            string respStr = ResponseDumper.ToString(resp);
            MessageBox.Show(respStr);
        }

        internal void OnReplaceOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} replace order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void OnReplaceStrategyOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} replace strategy order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        internal void OnReplaceBracketOrderImpactRequest(bool isAsync)
        {
            logMessage(String.Format("Sending {0} replace bracket order impact request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void processReplaceOrderResponse(ReplaceOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processReplaceOrderResponseInternal(resp);
                });
            }
            else
            {
                processReplaceOrderResponseInternal(resp);
            }
        }

        private void processReplaceStrategyOrderResponse(ReplaceStrategyOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processReplaceStrategyOrderResponseInternal(resp);
                });
            }
            else
            {
                processReplaceStrategyOrderResponseInternal(resp);
            }
        }

        private void processReplaceBracketOrderResponse(ReplaceBracketOrderResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processReplaceBracketOrderResponseInternal(resp);
                });
            }
            else
            {
                processReplaceBracketOrderResponseInternal(resp);
            }
        }

        private void processReplaceOrderResponseInternal(ReplaceOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for replace order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for replace order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void processReplaceStrategyOrderResponseInternal(ReplaceStrategyOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for replace strategy order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for replace strategy order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void processReplaceBracketOrderResponseInternal(ReplaceBracketOrderResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for replace bracket order request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for replace bracket order request");
                m_lblGetOrdersRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblGetOrdersRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();
            }
        }

        private void onReplaceOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} replace order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onReplaceStrategyOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} replace strategy order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onReplaceBracketOrderRequest(bool isAsync)
        {
            m_lblGetOrdersRateLimitRemainingRequestsCount.Text = m_lblGetOrdersRateLimitRequestsResetTime.Text = String.Empty;
            logMessage(String.Format("Sending {0} replace bracket order request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayCandles(GetCandlesResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayCandlesInternal(resp);
                });
            }
            else
            {
                displayCandlesInternal(resp);
            }
        }

        private void displayCandlesInternal(GetCandlesResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get candles request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get candles request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<CandleData> candles = resp.Candles;
                for (int i = 0; i < candles.Count; ++i)
                {
                    CandleData candle = candles[i];
                    m_grdCandles.Rows.Add(new object[]{candle.m_start, candle.m_end, candle.m_low, candle.m_high, candle.m_open, candle.m_close, candle.m_volume});
                }
            }
        }

        private void onGetCandlesRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdCandles.Rows.Clear();
            logMessage(String.Format("Sending {0} get candles request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displaySymbolSearch(SearchSymbolsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displaySymbolSearchInternal(resp);
                });
            }
            else
            {
                displaySymbolSearchInternal(resp);
            }
        }

        private void displaySymbolSearchInternal(SearchSymbolsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for search symbols request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for search symbols request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<EquitySymbol> symbols = resp.Symbols;
                for (int i = 0; i < symbols.Count; ++i)
                {
                    EquitySymbol symbol = symbols[i];
                    m_grdSymbolSearch.Rows.Add(new object[]{symbol.m_symbol, symbol.m_symbolId, symbol.m_description, symbol.m_securityType, symbol.m_currency, symbol.m_listingExchange, symbol.m_isTradable, symbol.m_isQuotable});
                }
            }
        }

        private void onSymbolSearchRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = m_lblSymbolSearchSelectedSymbols.Text = m_txtSymbolDataSymbolIds.Text = m_txtQuotesSymbolIds.Text = m_txtOptionChainSymbolIds.Text = String.Empty;
            m_grdSymbolSearch.Rows.Clear();
            logMessage(String.Format("Sending {0} symbol search request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayQuotes(GetQuoteResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayQuotesInternal(resp);
                });
            }
            else
            {
                displayQuotesInternal(resp);
            }
        }

        private void displayOptionQuotes(GetOptionQuotesResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayOptionQuotesInternal(resp);
                });
            }
            else
            {
                displayOptionQuotesInternal(resp);
            }
        }

        private void displayStrategyQuotes(GetStrategyQuoteResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayStrategyQuotesInternal(resp);
                });
            }
            else
            {
                displayStrategyQuotesInternal(resp);
            }
        }

        private void displayQuotesInternal(GetQuoteResponse resp)
        {
            if (!resp.IsValid || resp.ErrorCode != 0)
            {
                logError(String.Format("Received error response for get quotes request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get quotes request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                m_grdQuotes.Rows.Clear();
                List<Level1DataItem> quotes = resp.Quotes;
                for (int i = 0; i < quotes.Count; ++i)
                {
                    Level1DataItem quote = quotes[i];
                    m_grdQuotes.Rows.Add(new object[]{quote.m_symbol, quote.m_symbolId, quote.m_bidPrice, quote.m_bidSize, quote.m_askPrice, quote.m_askSize, quote.m_lastTradePriceTrHrs, quote.m_lastTradePrice, quote.m_lastTradeSize, quote.m_lastTradeTick, quote.m_lastTradeTime, quote.m_volume, quote.m_openPrice, quote.m_highPrice, quote.m_lowPrice, quote.m_delay, quote.m_isHalted});
                }
            }
        }

        private void displayOptionQuotesInternal(GetOptionQuotesResponse resp)
        {
            if (!resp.IsValid || resp.ErrorCode != 0)
            {
                logError(String.Format("Received error response for get option quotes request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get option quotes request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                m_grdOptionQuotes.Rows.Clear();
                List<Level1OptionData> optionQuotes = resp.OptionQuotes;
                for (int i = 0; i < optionQuotes.Count; ++i)
                {
                    Level1OptionData optionQuote = optionQuotes[i];
                    m_grdOptionQuotes.Rows.Add(new object[] { optionQuote.m_underlying, optionQuote.m_underlyingId, optionQuote.m_symbol, optionQuote.m_symbolId, optionQuote.m_bidPrice, 
                                                        optionQuote.m_bidSize, optionQuote.m_askPrice, optionQuote.m_askSize, optionQuote.m_lastTradePriceTrHrs, optionQuote.m_lastTradePrice, 
                                                        optionQuote.m_lastTradeSize, optionQuote.m_lastTradeTick, optionQuote.m_lastTradeTime, optionQuote.m_volume, optionQuote.m_openPrice, 
                                                        optionQuote.m_highPrice, optionQuote.m_lowPrice, optionQuote.m_volatility, optionQuote.m_delta, optionQuote.m_gamma, optionQuote.m_theta,
                                                        optionQuote.m_vega, optionQuote.m_rho, optionQuote.m_openInterest, optionQuote.m_delay, optionQuote.m_isHalted, optionQuote.m_VWAP });
                }
            }
        }

        private void displayStrategyQuotesInternal(GetStrategyQuoteResponse resp)
        {
            if (!resp.IsValid || resp.ErrorCode != 0)
            {
                logError(String.Format("Received error response for get strategy quotes request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get strategy quotes request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                m_grdStrategyQuotes.Rows.Clear();
                List<Level1StrategyData> strategyQuotes = resp.StrategyQuotes;
                for (int i = 0; i < strategyQuotes.Count; ++i)
                {
                    Level1StrategyData strategyQuote = strategyQuotes[i];
                    m_grdStrategyQuotes.Rows.Add(new object[] { strategyQuote.m_variantId, strategyQuote.m_bidPrice, strategyQuote.m_askPrice, strategyQuote.m_underlying, 
                                                                strategyQuote.m_underlyingId, strategyQuote.m_openPrice, strategyQuote.m_volatility, strategyQuote.m_delta, 
                                                                strategyQuote.m_gamma, strategyQuote.m_theta, strategyQuote.m_vega, strategyQuote.m_rho, strategyQuote.m_isRealTime});
                }
            }
        }

        private void onGetQuotesRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdQuotes.Rows.Clear();
            logMessage(String.Format("Sending {0} get quotes request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onGetQuotesStream(bool streamStarted)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdQuotes.Rows.Clear();
            logMessage(String.Format("Get quotes streaming {0}", (streamStarted ? "started" : "stopped")));
        }

        private void onGetOptionQuotesRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdOptionQuotes.Rows.Clear();
            logMessage(String.Format("Sending {0} get option quotes request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onGetOptionQuotesStream(bool streamStarted)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdOptionQuotes.Rows.Clear();
            logMessage(String.Format("Get option quotes streaming {0}", (streamStarted ? "started" : "stopped")));
        }

        private void onGetStrategyQuotesRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdStrategyQuotes.Rows.Clear();
            logMessage(String.Format("Sending {0} get strategy quotes request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void onGetStrategyQuotesStream(bool streamStarted)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdStrategyQuotes.Rows.Clear();
            logMessage(String.Format("Get strategy quotes streaming {0}", (streamStarted ? "started" : "stopped")));
        }

        private void displaySymbols(GetSymbolsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displaySymbolsInternal(resp);
                });
            }
            else
            {
                displaySymbolsInternal(resp);
            }
        }

        private void displaySymbolsInternal(GetSymbolsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get symbols request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get symbols request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<SymbolData> symbols = resp.Symbols;
                for (int i = 0; i < symbols.Count; ++i)
                {
                    SymbolData symbol = symbols[i];
                    m_symbolData.Add(symbol.m_symbolId, new KeyValuePair<List<MinTickData>, ContractDeliverables>(symbol.m_minTicks, symbol.m_optionContractDeliverables));
                    m_grdSymbols.Rows.Add(new object[] { symbol.m_symbol, symbol.m_symbolId, symbol.m_prevDayClosePrice, symbol.m_highPrice52, symbol.m_lowPrice52, symbol.m_averageVol3Months, symbol.m_averageVol20Days, symbol.m_outstandingShares, symbol.m_eps, symbol.m_pe, symbol.m_dividend, symbol.m_yield, symbol.m_exDate, symbol.m_marketCap, symbol.m_tradeUnit, symbol.m_optionType, symbol.m_optionDurationType, symbol.m_optionRoot, symbol.m_optionExerciseType, symbol.m_listingExchange, symbol.m_description, symbol.m_securityType, symbol.m_optionExpiryDate, symbol.m_dividendDate, symbol.m_optionStrikePrice, symbol.m_isTradable, symbol.m_isQuotable, symbol.m_hasOptions, symbol.m_currency, symbol.m_optionContractDeliverables.m_cashInLieu });
                }
            }
        }

        private void addContractDeliverablesRows(ContractDeliverables contractDeliverables)
        {
            for (int i = 0; i < contractDeliverables.m_underlyings.Count; ++i)
            {
                UnderlyingMultiplierPair ump = contractDeliverables.m_underlyings[i];
                m_grdSymbolsContractDeliverables.Rows.Add(new object[]{ump.m_multiplier, ump.m_underlyingSymbol, ump.m_underlyingSymbolId});
            }
        }

        private void addMinTickRows(List<MinTickData> minTicks)
        {
            for (int i = 0; i < minTicks.Count; ++i)
            {
                MinTickData minTickData = minTicks[i];
                m_grdSymbolsMinTicks.Rows.Add(new object[]{minTickData.m_pivot, minTickData.m_minTick});
            }
        }

        private void onGetSymbolsRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdSymbols.Rows.Clear();
            m_grdSymbolsMinTicks.Rows.Clear();
            m_grdSymbolsContractDeliverables.Rows.Clear();
            m_symbolData.Clear();
            logMessage(String.Format("Sending {0} get symbols request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayOptions(GetOptionsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayOptionsInternal(resp);
                });
            }
            else
            {
                displayOptionsInternal(resp);
            }
        }

        private void displayOptionsInternal(GetOptionsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get options request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get options request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<ChainPerExpiryDate> optionChains = resp.OptionChain;
                for (int i = 0; i < optionChains.Count; ++i)
                {
                    ChainPerExpiryDate optionChain = optionChains[i];
                    for (int j = 0; j < optionChain.m_chainPerRoot.Count; ++j)
                    {
                        ChainPerRoot chainPerRoot = optionChain.m_chainPerRoot[j];
                        for (int k = 0; k < chainPerRoot.m_chainPerStrikePrice.Count; ++k)
                        {
                            ChainPerStrikePrice chainPerStrikePrice = chainPerRoot.m_chainPerStrikePrice[k];
                            m_grdOptions.Rows.Add(new object[]{optionChain.m_expiryDate, chainPerRoot.m_optionRoot, chainPerRoot.m_multiplier, chainPerStrikePrice.m_strikePrice, chainPerStrikePrice.m_callSymbolId, chainPerStrikePrice.m_putSymbolId});
                        }
                    }
                }
            }
        }

        private void onGetOptionsRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdOptions.Rows.Clear();
            logMessage(String.Format("Sending {0} get options request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void displayMarkets(GetMarketsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    displayMarketsInternal(resp);
                });
            }
            else
            {
                displayMarketsInternal(resp);
            }
        }

        private void displayMarketsInternal(GetMarketsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for get markets request: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received successful response for get markets request");
                m_lblMarketDataRateLimitRemainingRequestsCount.Text = resp.RateLimitRemainingRequestsCount.ToString();
                m_lblMarketDataRateLimitRequestsResetTime.Text = resp.RateLimitRequestsResetTime.ToString();

                List<Market> markets = resp.Markets;
                for (int i = 0; i < markets.Count; ++i)
                {
                    Market market = markets[i];
                    m_marketsData.Add(market.m_name, new MarketsData(market.m_tradingVenues, market.m_primaryOrderRoutes, market.m_secondaryOrderRoutes, market.m_level1Feeds, market.m_level2Feeds));
                    m_grdMarkets.Rows.Add(new object[]{market.m_name, market.m_defaultTradingVenue, market.m_extendedStartTime.TimeOfDay, market.m_startTime.TimeOfDay, market.m_endTime.TimeOfDay, market.m_extendedEndTime.TimeOfDay, market.m_snapQuotesLimit});
                }
            }
        }

        private void addMarketsDataItems(MarketsData marketsData)
        {
            addListBoxItems(marketsData.m_tradingVenues, m_lstTradingVenues);
            addListBoxItems(marketsData.m_primaryOrderRoutes, m_lstPrimaryOrderRoutes);
            addListBoxItems(marketsData.m_secondaryOrderRoutes, m_lstSecondaryOrderRoutes);
            addListBoxItems(marketsData.m_level1Feeds, m_lstLevel1Feeds);
            addListBoxItems(marketsData.m_level2Feeds, m_lstLevel2Feeds);
        }

        private void addListBoxItems(List<string> items, ListBox lst)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                lst.Items.Add(items[i]);
            }
        }

        private void onGetMarketsRequest(bool isAsync)
        {
            m_lblMarketDataRateLimitRemainingRequestsCount.Text = m_lblMarketDataRateLimitRequestsResetTime.Text = String.Empty;
            m_grdMarkets.Rows.Clear();
            m_lstTradingVenues.Items.Clear();
            m_lstPrimaryOrderRoutes.Items.Clear();
            m_lstSecondaryOrderRoutes.Items.Clear();
            m_lstLevel1Feeds.Items.Clear();
            m_lstLevel2Feeds.Items.Clear();
            m_marketsData.Clear();
            logMessage(String.Format("Sending {0} get markets request", (isAsync ? "asynchronous" : "synchronous")));
        }

        private void subscribeToOrderAndExecutionNotifications()
        {
            OrderAndExecutionNotificationsResponse.OrderAndExecutionNotificationsStream(m_authImpl, orderAndExecutionNotificationsStreamCallbackMethod, NextAsyncRequestID);
        }

        private void processOrdersAndExecutionsPushNotification(OrderAndExecutionNotificationsResponse resp)
        {
            if (m_rtbActivityLog.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    processOrdersAndExecutionsPushNotificationInternal(resp);
                });
            }
            else
            {
                processOrdersAndExecutionsPushNotificationInternal(resp);
            }
        }

        private void processOrdersAndExecutionsPushNotificationInternal(OrderAndExecutionNotificationsResponse resp)
        {
            if (!resp.IsValid)
            {
                logError(String.Format("Received error response for orders/executions push notification: Error Code: {0}, Error Message: {1}", resp.ErrorCode, resp.ErrorMessage));
            }
            else
            {
                logMessage("Received orders/executions push notification");
                List<OrderData> orders = resp.Orders;
                for (int i = 0; i < orders.Count; ++i)
                {
                    OrderData order = orders[i];

                    bool found = false;
                    for (int j = 0; j < m_grdOrders.Rows.Count; ++j)
                    {
                        ulong orderId = Convert.ToUInt64(m_grdOrders.Rows[j].Cells[m_colOrdersId.Index].Value);
                        if (orderId == order.m_id)
                        {
                            updateOrderRow(m_grdOrders.Rows[j], order);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        addOrderRow(order);
                    }
                }

                List<ExecutionData> executions = resp.Executions;
                for (int i = 0; i < executions.Count; ++i)
                {
                    ExecutionData exec = executions[i];
                    
                    bool found = false;
                    for (int j = 0; j < m_grdExecutions.Rows.Count; ++j)
                    {
                        ulong execId = Convert.ToUInt64(m_grdExecutions.Rows[j].Cells[m_colExecutionsId.Index].Value);
                        if (execId == exec.m_id)
                        {
                            updateExecutionRow(m_grdExecutions.Rows[j], exec);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        addExecutionsRow(exec);
                    }
                }
            }
        }

        #endregion

        #region Properties

        internal int NextAsyncRequestID
        {
            get
            {
                return ++m_nextAsyncRequestId;
            }
        }

        private List<ulong> orderIds
        {
            get
            {
                List<ulong> ids = new List<ulong>();
                string[] oidsStr = m_txtGetOrderByIdOrderId.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < oidsStr.Length; ++i)
                {
                    try
                    {
                        ulong orderId = Convert.ToUInt64(oidsStr[i]);
                        ids.Add(orderId);
                    }
                    catch
                    {
                    }
                }
                return ids;
            }
        }

        internal AuthenticationInfoImplementation AuthImpl
        {
            get
            {
                return m_authImpl;
            }
        }

        internal string SelectedAccountNumber
        {
            get
            {
                return m_selectedAccountNumber;
            }
        }

        private List<ulong> symbolDataSymbolIds
        {
            get
            {
                return commaSeparatedListToIdList(m_txtSymbolDataSymbolIds.Text);
            }
        }

        private List<string> symbolDataSymbolNames
        {
            get
            {
                return commaSeparatedListToStringList(m_txtSymbolDataSymbolNames.Text);
            }
        }

        private List<ulong> quotesSymbolIds
        {
            get
            {
                return commaSeparatedListToIdList(m_txtQuotesSymbolIds.Text);
            }
        }

        private List<ulong> optionQuotesOptionIds
        {
            get
            {
                return commaSeparatedListToIdList(m_txtOptionIds.Text);
            }
        }

        private List<OptionIdFilter> optionQuotesOptionFilters
        {
            get
            {
                List<OptionIdFilter> filters = new List<OptionIdFilter>();
                for (int i = 0; i < m_grdOptionFilters.RowCount; ++i)
                {
                    try
                    {
                        OptionIdFilter filter = new OptionIdFilter();
                        filter.m_underlyingId = Convert.ToUInt64(m_grdOptionFilters.Rows[i].Cells[m_colOptionQuotesUnderlyingID.Index].Value);
                        filter.m_optionType = (OptionType)Enum.Parse(typeof(OptionType), m_grdOptionFilters.Rows[i].Cells[m_colOptionQuotesOptionType.Index].Value.ToString());
                        filter.m_expiryDate = Convert.ToDateTime(m_grdOptionFilters.Rows[i].Cells[m_colOptionQuotesExpiryDate.Index].Value);
                        filter.m_minstrikePrice = Convert.ToDouble(m_grdOptionFilters.Rows[i].Cells[m_colOptionQuotesMinStrike.Index].Value);
                        filter.m_maxstrikePrice = Convert.ToDouble(m_grdOptionFilters.Rows[i].Cells[m_colOptionQuotesMaxStrike.Index].Value);
                        filters.Add(filter);
                    }
                    catch
                    {
                        continue;
                    }
                }
                return filters;
            }
        }

        private List<ulong> optionChainSymbolIds
        {
            get
            {
                return commaSeparatedListToIdList(m_txtOptionChainSymbolIds.Text);
            }
        }

        private List<StrategyVariantRequest> strategyQuotesVariants
        {
            get
            {
                List<StrategyVariantRequest> variants = new List<StrategyVariantRequest>();
                StrategyVariantRequest variant = new StrategyVariantRequest();
                variant.m_variantId = m_txtStrategyQuotesVariantId.Text.Length != 0 ? Convert.ToUInt64(m_txtStrategyQuotesVariantId.Text) : QuestradeAPI.Constants.InvalidULong;
                variant.m_strategy = GetEnum<StrategyType>(m_cmbStrategyQuotesStrategy);
                variant.m_legs = new List<StrategyVariantLeg>();

                for (int i = 0; i < m_grdStrategyQuotesLegs.RowCount; ++i)
                {
                    try
                    {
                        StrategyVariantLeg leg = new StrategyVariantLeg();
                        leg.m_symbolId = Convert.ToUInt64(m_grdStrategyQuotesLegs.Rows[i].Cells[m_colStrategyQuotesLegsSymbolID.Index].Value);
                        leg.m_ratio = Convert.ToUInt64(m_grdStrategyQuotesLegs.Rows[i].Cells[m_colStrategyQuotesLegsRatio.Index].Value);
                        leg.m_action = (OrderAction)Enum.Parse(typeof(OrderAction), m_grdStrategyQuotesLegs.Rows[i].Cells[m_colStrategyQuotesLegsAction.Index].Value.ToString());;
                        variant.m_legs.Add(leg);
                    }
                    catch
                    {
                        continue;
                    }
                }
                variants.Add(variant);
                return variants;
            }
        }

        #endregion
    }
}
