/*
    * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
    * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
    * 
    * Licensed under the Apache License, Version 2.0 (the "License"); 
    * you may not use this file except in compliance with the License.
    * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
    * 
    * Unless required by applicable law or agreed to in writing, software
    * distributed under the License is distributed on an "AS IS" BASIS,
    * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    * See the License for the specific language governing permissions and
    * limitations under the License.
*/

using System;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// 
    /// QCU: Opening Breakout Algorithm
    /// 
    /// In this algorithm we attempt to provide a working algorithm that
    /// addresses many of the primary algorithm concerns. These concerns
    /// are:
    /// 
    ///     1. Signal Generation
    ///             This algorithm aims to generate signals for an opening
    ///             breakout move before 10am. Signals are generated by
    ///             producing the opening five minute bar, and then trading
    ///             in the direction of the breakout from that bar.
    /// 
    ///     2. Position Sizing
    ///             Positions are sized using recently the average true range.
    ///             The higher the recently movement, the smaller position.
    ///             This helps to reduce the risk of losing a lot on a single
    ///             transaction.
    /// 
    ///     3. Active Stop Loss
    ///             Stop losses are maintained at a fixed global percentage to
    ///             limit maximum losses per day, while also a trailing stop
    ///             loss is implemented using the parabolic stop and reverse
    ///             in order to gauge exit points
    /// 
    /// </summary>
    public class OpeningBreakoutAlgorithm : QCAlgorithm
    {
        // the equity symbol we're trading
        private const string Symbol = "SPY";

        // plotting and logging control
        private const bool EnablePlotting = true;
        private const bool EnableOrderUpdateLogging = false;
        private const int PricePlotFrequencyInSeconds = 15;

        // risk control
        private const bool UseRecentVolatilityRequirement = true;

        private const decimal MaximumLeverage = 4;
        private const decimal PercentProfitStartPsarTrailingStop = 0.0005m; // @100k order size this is 50 bucks
        private const decimal MaximumPorfolioRiskPercentPerPosition = .0025m;

        // entrance criteria
        private const int OpeningSpanInMinutes = 3;
        private const decimal BreakoutThresholdPercent = 0.00005m;
        private const decimal AtrVolatilityThresholdPercent = 0.002m;
        private const decimal StdVolatilityThresholdPercent = 0.0025m;

        // this is the security we're trading
        public Security Security;

        // define our indicators used for trading decisions
        public HullMovingAverage HMA;
        public AverageTrueRange ATR14;
        public StandardDeviation STD14;
        public ParabolicStopAndReverse PSARMin;

        // smoothed values
        public ExponentialMovingAverage SmoothedSTD14;
        public ExponentialMovingAverage SmoothedATR14;

        // working variable to control our algorithm

        // this flag is used to run some code only once after the algorithm is warmed up
        private bool FinishedWarmup;
        // this is used to record the last time we closed a position
        private DateTime LastExitTime;
        // this is our opening n minute bar
        private TradeBar OpeningBarRange;
        // this is the ticket from our market order (entrance)
        private OrderTicket MarketTicket;
        // this is the ticket from our stop loss order (exit)
        private OrderTicket StopLossTicket;
        // this flag is used to indicate we've switched from a global, non changing
        // stop loss to a dynamic trailing stop using the PSAR
        private bool EnablePsarTrailingStop;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // initialize algorithm level parameters
            SetStartDate(2015, 01, 01);
            SetEndDate(2015, 06, 01);
            SetCash(100000);

            // leverage tradier $1 traders
            SetBrokerageModel(BrokerageName.TradierBrokerage);

            // request high resolution equity data
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Second);

            // save off our security so we can reference it quickly later
            Security = Securities[Symbol];

            // Set our max leverage
            Security.SetLeverage(MaximumLeverage);

            // define a hull for trend detection
            HMA = new HullMovingAverage(Symbol + "_HMA14", 4);
            var hmaDaily = new TradeBarConsolidator(TimeSpan.FromMinutes(30));
            RegisterIndicator(Symbol, HMA, hmaDaily, Field.Close);

            // define our longer term indicators
            STD14 = STD(Symbol, 14, Resolution.Daily);
            ATR14 = ATR(Symbol, 14, resolution: Resolution.Daily);
            PSARMin = new ParabolicStopAndReverse(Symbol, afStart: 0, afIncrement: 0.000025m);

            // smooth our ATR over a week, we'll use this to determine if recent volatilty warrants entrance
            var oneWeekInMarketHours = (int)(5*6.5);
            SmoothedATR14 = new ExponentialMovingAverage("Smoothed_" + ATR14.Name, oneWeekInMarketHours).Of(ATR14);
            // smooth our STD over a week as well
            SmoothedSTD14 = new ExponentialMovingAverage("Smoothed_"+STD14.Name, oneWeekInMarketHours).Of(STD14);

            // initialize our charts
            var chart = new Chart(Symbol);
            chart.AddSeries(new Series(HMA.Name));
            chart.AddSeries(new Series("Enter", SeriesType.Scatter));
            chart.AddSeries(new Series("Exit", SeriesType.Scatter));
            chart.AddSeries(new Series(PSARMin.Name, SeriesType.Scatter));
            AddChart(chart);

            var history = History(Symbol, 20, Resolution.Daily);
            foreach (var bar in history)
            {
                hmaDaily.Update(bar);
                ATR14.Update(bar);
                STD14.Update(bar.EndTime, bar.Close);
            }

            // schedule an event to run every day at five minutes after our Symbol's market open
            Schedule.Event("MarketOpenSpan")
                .EveryDay(Symbol)
                .AfterMarketOpen(Symbol, minutesAfterOpen: OpeningSpanInMinutes)
                .Run(MarketOpeningSpanHandler);

            Schedule.Event("MarketOpen")
                .EveryDay(Symbol)
                .AfterMarketOpen(Symbol, minutesAfterOpen: -1)
                .Run(() => PSARMin.Reset());
        }

        /// <summary>
        /// This function is scheduled to be run every day at the specified number of minutes after market open
        /// </summary>
        public void MarketOpeningSpanHandler()
        {
            // request the last n minutes of data in minute bars, we're going to
            // define the opening rang
            var history = History(Symbol, OpeningSpanInMinutes, Resolution.Minute);

            // this is our bar size
            var openingSpan = TimeSpan.FromMinutes(OpeningSpanInMinutes);

            // we only care about the high and low here
            OpeningBarRange = new TradeBar
            {
                // time values
                Time = Time - openingSpan,
                EndTime = Time,
                Period = openingSpan,
                // high and low
                High = Security.Close,
                Low = Security.Close
            };

            // aggregate the high/low for the opening range
            foreach (var tradeBar in history)
            {
                OpeningBarRange.Low = Math.Min(OpeningBarRange.Low, tradeBar.Low);
                OpeningBarRange.High = Math.Max(OpeningBarRange.High, tradeBar.High);
            }

            // widen the bar when looking for breakouts
            OpeningBarRange.Low *= 1 - BreakoutThresholdPercent;
            OpeningBarRange.High *= 1 + BreakoutThresholdPercent;

            Log("---------" + Time.Date + "---------");
            Log("OpeningBarRange: Low: " + OpeningBarRange.Low.SmartRounding() + " High: " + OpeningBarRange.High.SmartRounding());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // we don't need to run any of this during our warmup phase
            if (IsWarmingUp) return;

            // when we're done warming up, register our indicators to start plotting
            if (!IsWarmingUp && !FinishedWarmup)
            {
                // this is a run once flag for when we're finished warmup
                FinishedWarmup = true;

                // plot our hourly indicators automatically, wait for them to ready
                PlotIndicator(Symbol, HMA);

                PlotIndicator("ATR", ATR14);
                PlotIndicator("STD", STD14);
                PlotIndicator("ATR", SmoothedATR14);
            }

            // update our PSAR
            PSARMin.Update((TradeBar) Security.GetLastData());

            // plot price until an hour after we close so we can see our execution skillz
            if (ShouldPlot)
            {
                // we can plot price more often if we want
                Plot(Symbol, "Price", Security.Close);
                // only plot psar on the minute
                if (PSARMin.IsReady && Time.RoundDown(TimeSpan.FromMinutes(1)) == Time)
                {
                    Plot(Symbol, PSARMin);
                }
            }

            // first wait for our opening range bar to be set to today
            if (OpeningBarRange == null || OpeningBarRange.EndTime.Date != Time.Date || OpeningBarRange.EndTime == Time) return;

            // we only trade max once per day, so if we've already exited the stop loss, bail
            if (StopLossTicket != null && StopLossTicket.Status == OrderStatus.Filled)
            {
                // null these out to signal that we're done trading for the day
                OpeningBarRange = null;
                StopLossTicket = null;
                return;
            }

            // now that we have our opening bar, test to see if we're already in a positio
            if (!Security.Invested)
            {
                ScanForEntrance();
            }
            else
            {
                // if we haven't exited yet then manage our stop loss, this controls our exit point
                if (Security.Invested)
                {
                    ManageStopLoss();
                }
                else if (StopLossTicket != null && StopLossTicket.Status.IsOpen())
                {
                    StopLossTicket.Cancel();
                }
            }
        }

        /// <summary>
        /// Scans for a breakout from the opening range bar
        /// </summary>
        private void ScanForEntrance()
        {
            // scan for entrances, we only want to do this before 10am
            if (Time.TimeOfDay.Hours >= 10) return;

            // expect capture 10% of the daily range
            var expectedCaptureRange = 0.1m*ATR14;
            var allowedDollarLoss = MaximumPorfolioRiskPercentPerPosition*Portfolio.TotalPortfolioValue;
            var shares = (int) (allowedDollarLoss/expectedCaptureRange);

            // max out at a little below our stated max, prevents margin calls and such
            var maxShare = CalculateOrderQuantity(Symbol, .75m*MaximumLeverage);
            shares = Math.Min(shares, maxShare);

            // the stop percentage defined by dollars loss
            var stopLossPercentage = allowedDollarLoss/(shares*Security.Close);

            // min out at 1x leverage
            //var minShare = CalculateOrderQuantity(Symbol, MaximumLeverage/2m);
            //shares = Math.Max(shares, minShare);

            // we're looking for a breakout of the opening range bar in the direction of the medium term trend
            if (ShouldEnterLong)
            {
                // breakout to the upside, go long (fills synchronously)
                MarketTicket = MarketOrder(Symbol, shares);
                Log("Enter long @ " + MarketTicket.AverageFillPrice.SmartRounding() + " Shares: " + shares);
                Plot(Symbol, "Enter", MarketTicket.AverageFillPrice);

                // we'll start with a global, non-trailing stop loss
                EnablePsarTrailingStop = false;

                // submit stop loss order for max loss on the trade
                var stopPrice = Security.Low*(1 - stopLossPercentage);
                StopLossTicket = StopMarketOrder(Symbol, -shares, stopPrice);
                Log("Submitted stop loss @ " + stopPrice.SmartRounding());
            }
            else if (ShouldEnterShort)
            {
                // breakout to the downside, go short
                MarketTicket = MarketOrder(Symbol, - -shares);
                Log("Enter short @ " + MarketTicket.AverageFillPrice.SmartRounding());
                Plot(Symbol, "Enter", MarketTicket.AverageFillPrice);

                // we'll start with a global, non-trailing stop loss
                EnablePsarTrailingStop = false;

                // submit stop loss order for max loss on the trade
                var stopPrice = Security.High*(1 + stopLossPercentage);
                StopLossTicket = StopMarketOrder(Symbol, -shares, stopPrice);
                Log("Submitted stop loss @ " + stopPrice.SmartRounding() + " Shares: " + shares);
            }
        }

        /// <summary>
        /// Manages our stop loss ticket
        /// </summary>
        private void ManageStopLoss()
        {
            // if we've already exited then no need to do more
            if (StopLossTicket == null || StopLossTicket.Status == OrderStatus.Filled) return;

            // only do this once per minute
            //if (Time.RoundDown(TimeSpan.FromMinutes(1)) != Time) return;

            // get the current stop price
            var stopPrice = StopLossTicket.Get(OrderField.StopPrice);

            // check for enabling the psar trailing stop
            if (ShouldEnablePsarTrailingStop(stopPrice))
            {
                EnablePsarTrailingStop = true;
                if (EnableOrderUpdateLogging)
                {
                    Log("Enabled PSAR trailing stop @ ProfitPercent: " + Security.Holdings.UnrealizedProfitPercent.SmartRounding());
                }
            }

            // we've trigger the psar trailing stop, so start updating our stop loss tick
            if (EnablePsarTrailingStop && PSARMin.IsReady)
            {
                StopLossTicket.Update(new UpdateOrderFields { StopPrice = PSARMin });
                if (EnableOrderUpdateLogging)
                {
                    Log("Submitted stop loss @ " + PSARMin.Current.Value.SmartRounding());
                }
            }
        }

        /// <summary>
        /// This event handler is fired for each and every order event the algorithm
        /// receives. We'll perform some logging and house keeping here
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // print debug messages for all order events
            if (LiveMode || orderEvent.Status.IsFill() || EnableOrderUpdateLogging)
            {
                LiveDebug("Filled: " + orderEvent.FillQuantity + " Price: " + orderEvent.FillPrice);
            }

            // if this is a fill and we now don't own any stock, that means we've closed for the day
            if (!Security.Invested && orderEvent.Status == OrderStatus.Filled)
            {
                // reset values for tomorrow
                LastExitTime = Time;
                var ticket = Transactions.GetOrderTickets(x => x.OrderId == orderEvent.OrderId).Single();
                Plot(Symbol, "Exit", ticket.AverageFillPrice);
            }
        }

        /// <summary>
        /// If we're still invested by the end of the day, liquidate
        /// </summary>
        public override void OnEndOfDay()
        {
            Liquidate();
        }

        /// <summary>
        /// Determines whether or not we should plot. This is used
        /// to provide enough plot points but not too many, we don't
        /// need to plot every second in backtests to get an idea of
        /// how good or bad our algorithm is performing
        /// </summary>
        public bool ShouldPlot
        {
            get
            {
                // always in live
                if (LiveMode) return true;
                // set in top to override plotting during long backtests
                if (!EnablePlotting) return false;
                // every 30 seconds in backtest
                if (Time.RoundDown(TimeSpan.FromSeconds(PricePlotFrequencyInSeconds)) != Time) return false;
                // always if we're invested
                if (Security.Invested) return true;
                // always if it's before noon
                if (Time.TimeOfDay.Hours < 10.25) return true;
                // for an hour after our exit
                if (Time - LastExitTime < TimeSpan.FromMinutes(30)) return true;

                return false;
            }
        }

        /// <summary>
        /// In live mode it's nice to push messages to the debug window
        /// as well as the log, this allows easy real time inspection of
        /// how the algorithm is performing
        /// </summary>
        public void LiveDebug(object msg)
        {
            if (msg == null) return;

            if (LiveMode)
            {
                Debug(msg.ToString());
                Log(msg.ToString());
            }
            else
            {
                Log(msg.ToString());
            }
        }

        /// <summary>
        /// Determines whether or not we should end a long position
        /// </summary>
        private bool ShouldEnterLong
        {
            // check to go in the same direction of longer term trend and opening break out
            get
            {
                return IsUptrend
                    && HasEnoughRecentVolatility
                    && Security.Close > OpeningBarRange.High;
            }
        }

        /// <summary>
        /// Determines whether or not we're currently in a medium term up trend
        /// </summary>
        private bool IsUptrend
        {
            get { return Security.Close > HMA; }
        }

        /// <summary>
        /// Determines whether or not we should enter a short position
        /// </summary>
        private bool ShouldEnterShort
        {
            // check to go in the same direction of longer term trend and opening break out
            get
            {
                return IsDowntrend 
                    && HasEnoughRecentVolatility
                    && Security.Close < OpeningBarRange.Low;
            }
        }

        /// <summary>
        /// Determines whether or not we're currently in a medium term down trend
        /// </summary>
        private bool IsDowntrend
        {
            get { return Security.Close < HMA; }
        }

        /// <summary>
        /// Determines whether or not there's been enough recent volatility for
        /// this strategy to work
        /// </summary>
        private bool HasEnoughRecentVolatility
        {
            get
            {
                return !UseRecentVolatilityRequirement
                    || SmoothedATR14 > Security.Close*AtrVolatilityThresholdPercent 
                    || SmoothedSTD14 > Security.Close*StdVolatilityThresholdPercent;
            }
        }

        /// <summary>
        /// Determines whether or not we should enable the psar trailing stop
        /// </summary>
        /// <param name="stopPrice">current stop price of our stop loss tick</param>
        private bool ShouldEnablePsarTrailingStop(decimal stopPrice)
        {
            // no need to enable if it's already enabled
            return !EnablePsarTrailingStop
                // once we're up a certain percentage, we'll use PSAR to control our stop
                && Security.Holdings.UnrealizedProfitPercent > PercentProfitStartPsarTrailingStop
                // make sure the PSAR is on the right side
                && PsarIsOnRightSideOfPrice
                // make sure the PSAR is more profitable than our global loss
                && IsPsarMoreProfitableThanStop(stopPrice);
        }

        /// <summary>
        /// Determines whether or not the PSAR is on the right side of price depending on our long/short
        /// </summary>
        private bool PsarIsOnRightSideOfPrice
        {
            get
            {
                return (Security.Holdings.IsLong && PSARMin < Security.Close) 
                    || (Security.Holdings.IsShort && PSARMin > Security.Close);
            }
        }

        /// <summary>
        /// Determines whether or not the PSAR stop price is better than the specified stop price
        /// </summary>
        private bool IsPsarMoreProfitableThanStop(decimal stopPrice)
        {
            return (Security.Holdings.IsLong && PSARMin > stopPrice) 
                || (Security.Holdings.IsShort && PSARMin < stopPrice);
        }
    }

    public class HullMovingAverage : IndicatorBase<IndicatorDataPoint>
    {
        private readonly LinearWeightedMovingAverage _fast;
        private readonly LinearWeightedMovingAverage _slow;
        private readonly LinearWeightedMovingAverage _smooth;

        public HullMovingAverage(string name, int period)
            : base(name)
        {
            var nsquared = period*period;
            _fast = new LinearWeightedMovingAverage(nsquared/2);
            _slow = new LinearWeightedMovingAverage(nsquared);
            _smooth = new LinearWeightedMovingAverage(period);
        }

        public override bool IsReady
        {
            get { return _smooth.IsReady; }
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _fast.Update(input);
            _slow.Update(input);
            _smooth.Update(input.Time, 2*_fast - _slow);
            return _smooth;
        }
    }
} 
