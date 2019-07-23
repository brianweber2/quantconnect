import clr
import decimal as d


class FuturesTickChartExample(QCAlgorithm):

    def Initialize(self):
        '''
        Initialise the data and resolution required, as well as the cash and start-end
        dates for your algorithm. All algorithms must initialized.
        '''
        self.SetStartDate(2019, 5, 1)    #Set Start Date
        self.SetEndDate(2019, 5, 31)      #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        self.SetTimeZone('America/Los_Angeles') # Set timezone
        self.SetWarmUp(5, Resolution.Day) # Wait 5 days before trading
        
        self.fastPeriod = 50
        self.slowPeriod = 100
        self.tickLength = 512
        
        # Subscribe and set our expiry filter for the futures chain
        self.futureES = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Tick)
        self.futureES.SetFilter(timedelta(0), timedelta(182))
        
        self.consolidators = dict()
        
        # Indicators
        self.fastSMA = self.SMA(self.futureES.Symbol, self.fastPeriod)
        self.slowSMA = self.SMA(self.futureES.Symbol, self.slowPeriod)
        self.previous = None
        
    def onData(self, slice):
        pass
        
    def OnDataConsolidated(self, sender, bar):
        self.fastSMA.Update(bar.EndTime, bar.Close)
        self.slowSMA.Update(bar.EndTime, bar.Close)
        
        # Define a small tolerance on our checks to avoid bouncing
        tolerance = 0.00015
        
        position = self.Portfolio[self.futureES.Symbol].Quantity
        
        # Only go long if not currently short or flat
        if position <= 0:
            if self.fastSMA.Current.Value > self.slowSMA.Current.Value * (1 + tolerance):
                self.Debug("Buy >> {}".format(bar.Ask))
                self.MarketOrder(self.futureES.Symbol, 1)
                
        # Liquidate position if we are currently long if the fast sma is less than the
        # slow sma
        if position > 0 and self.fastSMA.Current.Value < self.slowSMA.Current.Value:
            self.Debug("Sell >> {}".format(bar.Bid))
            self.Liquidate(self.futureES.Symbol)

    def OnSecuritiesChanged(self, changes):
        for security in changes.AddedSecurities:
            # consolidator = QuoteBarConsolidator(timedelta(minutes=15))
            consolidator = TickQuoteBarConsolidator(self.tickLength)
            consolidator.DataConsolidated += self.OnDataConsolidated
            self.SubscriptionManager.AddConsolidator(security.Symbol, consolidator)
            self.consolidators[security.Symbol] = consolidator
            
        for security in changes.RemovedSecurities:
            consolidator = self.consolidators.pop(security.Symbol)
            self.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator)
            consolidator.DataConsolidated -= self.OnDataConsolidated
