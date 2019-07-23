import clr
import decimal as d


class FuturesLongExample(QCAlgorithm):

    def Initialize(self):
        '''
        Initialise the data and resolution required, as well as the cash and start-end
        dates for your algorithm. All algorithms must initialized.
        '''
        self.SetStartDate(2019, 4, 1)    #Set Start Date
        self.SetEndDate(2019, 5, 1)      #Set End Date
        self.starting_cash = 100000
        self.SetCash(100000)             #Set Strategy Cash
        self.SetTimeZone('America/Los_Angeles') # Set timezone
        
        # Risk management
        self.stop_loss = 0.02 * -1 # in percent
        self.take_profit = 0.02 # in percent
        self.profit_hit = False
        self.stop_hit = False
        
        # Subscribe and set our expiry filter for the futures chain
        futureES = self.AddFuture(Futures.Indices.SP500EMini)
        futureES.SetFilter(timedelta(0), timedelta(182))
        
        # Indicators
        
    def OnData(self, slice):
        for chain in slice.FutureChains:
            # Get contracts expiring no earlier than in 90 days
            contracts = list(filter(lambda x: x.Expiry > self.Time + timedelta(90), chain.Value))

            # if there is any contract, trade the front contract
            if len(contracts) == 0: continue
            front = sorted(contracts, key = lambda x: x.Expiry, reverse=True)[0]
            front_symbol = front.Symbol
            current_position_size = self.Securities[front_symbol].Holdings.Quantity
            
            # my portfolio does not have a position with ES, and the TP or SL have not been triggered yet
            if current_position_size == 0:
                # Check margin requirements and available cash that aligns with risk tolerance
            
                # Enter 1 lot long position
                if not self.stop_hit and not self.profit_hit:
                    self.MarketOrder(front_symbol, 1)
                    self.Log("Buy >> {}".format(front.LastPrice))
            else: # Manage long position on ES
                pos_return = self.Securities[front_symbol].Holdings.UnrealizedProfitPercent
                if pos_return >= self.take_profit or pos_return <= self.stop_loss:
                    self.Liquidate()
                    pos_return = pos_return * 100
                    if pos_return > 0:
                        self.profit_hit = True
                        self.Debug("Profit hit: {}%".format(pos_return))
                    else:
                        self.stop_hit = True
                        self.Debug("Stop loss hit: {}%".format(pos_return))
