from QuantConnect.Python import PythonQuandl
from QuantConnect.Data.Custom import *
from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Securities import *
from QuantConnect.Data.Consolidators import *
from datetime import timedelta
from collections import deque
from QuantConnect.Orders import OrderStatus
import pandas as pd
import numpy as np
from datetime import timedelta, datetime

### <summary>
### Example structure for structuring an algorithm with indicator and consolidator data for many tickers.
### </summary>
### <meta name="tag" content="consolidating data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="strategy example" />
class MultipleSymbolConsolidationAlgorithm(QCAlgorithm):
    
    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):
        
        self._contract = None
        self._nextContract = None
        
        self._bb = None
        self._nextBb = None
        
        self._newDay = True
        self.reset = True
        
        # brokerage model
        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage,
                               AccountType.Margin)        
        
        self.SetStartDate(2014, 12, 1)
        self.SetEndDate(2016, 2, 1)
        self.SetCash(100000)
        self.SetWarmUp(TimeSpan.FromDays(5))
        
        future = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute)
        future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(185))  
        
    def OnData(self, slice):
        
        if (self.Time.minute==0):
            self.Log('OnData')
            
        if not self.InitContract(slice): return
    
        if self.reset:
            self.reset=False
            
        if (self.Time.minute==0):
            
            if (self._bb != None and self._bb.IsReady):
            
                price = 0.
                if (slice.Bars.ContainsKey(self._contract.Symbol)):
                    
                    #self.Log(self._contract.Symbol)
                    
                    price = slice.Bars[self._contract.Symbol].Close

                    args = (self.Time,self._contract,price,self._bb.LowerBand,self._bb.MiddleBand,self._bb.UpperBand)
                    self.Log('onData: {} contract: {}, price: {}, BBL: {}, BBM: {}, BBU: {}'.format(*args))
                    
                if (slice.Bars.ContainsKey(self._nextContract.Symbol)):

                    price = slice.Bars[self._nextContract.Symbol].Close

                    args = (self.Time,self._nextContract,price,self._nextBb.LowerBand,self._nextBb.MiddleBand,self._nextBb.UpperBand)
                    self.Log('onData: {} contract: {}, price: {}, BBL: {}, BBM: {}, BBU: {}'.format(*args))                    

                    
            else:
                self.Log('BB not ready')
        
        return
    
    def InitContract(self, slice):
        
        if not self._newDay:
            return True

        if (self._contract != None and (self._contract.Expiry - self.Time).days >=3):
            return True
            
        for chain in slice.FutureChains.Values:
            contracts = chain.Contracts.Values
            
            #self.Log(str(chain))
            
            skip = 0
            if (self._contract != None):
                self.Log('Expiry days away {} - {} - {}'.format((self._contract.Expiry-self.Time).days, self._contract.Expiry, self.Time.date))
            if (self._contract != None and (self._contract.Expiry-self.Time).days <= 3):
                skip = 1
                
            chainContracts = list(contracts) #[contract for contract in chain]
            chainContracts = sorted(chainContracts, key=lambda x: x.Expiry)
                
            if (len(chainContracts) < skip+2):
                return False
                
            first = chainContracts[skip]
            second = chainContracts[skip+1]
            
            if (first != None and second != None):
                
                self.Log('RESET: ' + first.Symbol.Value + ' - ' + second.Symbol.Value)
                self.reset=True
                
                if (first != None and (self._contract == None or self._contract.Symbol != first.Symbol)):
                    
                    if (self._nextContract != None):
                        
                        self._bb = self._nextBb
                        self._contract = self._nextContract
                        
                    else:
                        
                        self._contract = first
                        
                        oneHour = TradeBarConsolidator(TimeSpan.FromMinutes(60))
                        oneHour.DataConsolidated += self.OnHour
                        
                        self.SubscriptionManager.AddConsolidator(self._contract.Symbol, oneHour)
                        self._bb = self.BB(self._contract.Symbol, 20, 2, MovingAverageType.Exponential, Resolution.Hour)
                        
                        history = self.History(self._contract.Symbol, 50*60, Resolution.Minute).reset_index(drop=False)
                        self.Log(len(history))
                        
                        for bar in history.itertuples():

                            #if (bar.EndTime.Minute == 0 and (self.Time-bar.EndTime).TotalMinutes >=2):

                            if (bar.time.minute == 0 and ((self.Time-bar.time)/pd.Timedelta(minutes=1)) >=2):
                                
                                self.Log(str(bar))
                                #self._bb.Update(self._contract.Symbol, bar.time, bar.close)
                                self._bb.Update(bar.time, bar.close)
                                
                        self.Log(str(self._bb.IsReady))

                if (second != None and (self._nextContract == None or (self._nextContract.Symbol != second.Symbol))):
                    
                    self._nextContract = second
                    oneHour = TradeBarConsolidator(TimeSpan.FromMinutes(60))
                    oneHour.DataConsolidated += self.OnHour
                    
                    self.SubscriptionManager.AddConsolidator(self._nextContract.Symbol, oneHour)
                    self._nextBb = self.BB(self._nextContract.Symbol, 20, 2, MovingAverageType.Exponential, Resolution.Hour)
                    
                self._newDay=False
                return True
                
        return False
        
        
    def OnHour(self, sender, TradeBar):
        pass
    
    def OnEndOfDay(self):
        self._newDay=True                        
