import clr
import decimal as d


class FuturesContractRollover(QCAlgorithm):

    def Initialize(self):
        
        self.contract = None

        self.SetStartDate(2019, 3, 8)    #Set Start Date
        self.SetEndDate(2019, 5, 1)      #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        # self.SetWarmUp(TimeSpan.FromDays(5)) # Set warm up
        self.SetTimeZone('America/Los_Angeles') # Set timezone
        
        self.new_day = True
        self.reset = True
        
        # Risk management
        
        # Subscribe and set our expiry filter for the futures chain
        futureES = self.AddFuture(Futures.Indices.SP500EMini)
        futureES.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(360))
        
    def OnData(self, slice):
        
        if not self.InitUpdateContract(slice):
            return
    
        if self.reset:
            self.reset = False
            self.Log('RESET: closing all positions')
            self.Liquidate()
            
    def InitUpdateContract(self, slice):
        # Reset daily - everyday we check whether futures need to be rolled
        if not self.new_day:
            return True
            
        if self.contract != None and (self.contract.Expiry - self.Time).days >= 3: # rolling 3 days before expiry
            return True
            
        for chain in slice.FutureChains.Values:
            # When selecting contract, if on expiry date then skip first as it would be the same one.
            idx = 0
            if self.contract != None:
                self.Log('Expiry days away {} - {}'.format((self.contract.Expiry-self.Time).days, self.contract.Expiry))
            if self.contract != None and (self.contract.Expiry - self.Time).days < 3:
                idx = 1
            
            contracts = list(chain.Contracts.Values)
            
            chain_contracts = list(contracts) #[contract for contract in chain]
            chain_contracts = sorted(chain_contracts, key=lambda x: x.Expiry)
            
            if len(chain_contracts) < 2:
                return False
                
            first = chain_contracts[idx]
            second = chain_contracts[idx+1]
            
            if (first.Expiry - self.Time).days >= 3:
                self.contract = first
            elif (first.Expiry - self.Time).days < 3 and (second.Expiry - self.Time).days >= 3:
                self.contract = second
            self.Log("Setting contract to: {}".format(self.contract.Symbol.Value))
            
            self.new_day = False
            self.reset = True
            
            # Set up consolidators.
            one_hour = TradeBarConsolidator(TimeSpan.FromMinutes(60))
            one_hour.DataConsolidated += self.OnHour
            
            self.SubscriptionManager.AddConsolidator(self.contract.Symbol, one_hour)
            
            return True
        return False
        
    def OnHour(self, sender, bar):
        if bar.Symbol == self.contract.Symbol:
            self.Log("New hour for: {}".format(bar.Symbol.Value))
        
    def OnEndOfDay(self):
        # This needs to be set for the end of US session, i.e. 1:15pm PST.
        self.Log("OnEndOfDay")
        self.new_day = True
