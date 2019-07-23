import clr
import decimal as d


class BasicTemplateFuturesAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 31)
        self.SetCash(100000)
        self.SetWarmUp(30, Resolution.Minute)

        # Subscribe and set our expiry filter for the futures chain
        self.AddEquity("SPY", Resolution.Minute)
        
        self.fast_sma = self.EMA("SPY", 9)
        self.slow_sma = self.SMA("SPY", 50)
        
        consolidator = TradeBarConsolidator(timedelta(minutes=5))
        self.SubscriptionManager.AddConsolidator("SPY", consolidator)
        
        self.RegisterIndicator("SPY", self.fast_sma, consolidator)
        self.RegisterIndicator("SPY", self.slow_sma, consolidator)
        
        self.previous = None


    def OnData(self, slice):
        pass
    
    def OnDataConsolidated(self, sender, bar):
        # if not self.slow_sma.IsReady:
        #     return
        
        holdings = self.Portfolio["SPY"].Quantity
        
        if holdings <= 0:
            if self.fast_sma.Current.Value > self.slow_sma.Current.Value:
                self.Debug("BUY  >> {0}".format(self.Securities["SPY"].Price))
                self.SetHoldings("SPY", 1.0)
                
        if holdings > 0 and self.fast_sma.Current.Value < self.slow_sma.Current.Value:
            self.Debug("SELL >> {0}".format(self.Securities["SPY"].Price))
            self.Liquidate("SPY")
