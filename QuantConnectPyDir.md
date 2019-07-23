# QuantConnectPyDir Information

This document includes all of the available methods and attributes on each Quant Connect class available in their API.

## self.Securities[symbol]

### self.Securities[Symbol].Holdings

2019-05-20 21:01:00 ['AbsoluteHoldingsCost', 'AbsoluteHoldingsValue', 'AbsoluteQuantity', 'AddNewFee', 'AddNewProfit', 'AddNewSale', 'AveragePrice', 'Equals', 'Finalize', 'GetHashCode', 'GetType', 'HoldStock', 'HoldingsCost', 'HoldingsValue', 'Invested', 'IsLong', 'IsShort', 'LastTradeProfit', 'Leverage', 'MemberwiseClone', 'NetProfit', 'Overloads', 'Price', 'Profit', 'Quantity', 'ReferenceEquals', 'Security', 'SetHoldings', 'SetLastTradeProfit', 'Symbol', 'Target', 'ToString', 'TotalCloseProfit', 'TotalFees', 'TotalSaleVolume', 'Type', 'UnleveredAbsoluteHoldingsCost', 'UnleveredHoldingsCost', 'UnrealizedProfit', 'UnrealizedProfitPercent', 'UpdateMarketPrice', '__call__', '__class__', '__delattr__', '__delitem__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__overloads__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__setitem__', '__sizeof__', '__str__', '__subclasshook__', 'get_AbsoluteHoldingsCost', 'get_AbsoluteHoldingsValue', 'get_AbsoluteQuantity', 'get_AveragePrice', 'get_HoldStock', 'get_HoldingsCost', 'get_HoldingsValue', 'get_Invested', 'get_IsLong', 'get_IsShort', 'get_LastTradeProfit', 'get_Leverage', 'get_NetProfit', 'get_Price', 'get_Profit', 'get_Quantity', 'get_Security', 'get_Symbol', 'get_Target', 'get_TotalFees', 'get_TotalSaleVolume', 'get_Type', 'get_UnleveredAbsoluteHoldingsCost', 'get_UnleveredHoldingsCost', 'get_UnrealizedProfit', 'get_UnrealizedProfitPercent', 'set_AveragePrice', 'set_Price', 'set_Quantity', 'set_Target']


## def OnData(self, slice)

### slice.FutureChains

['Add', 'Clear', 'Contains', 'ContainsKey', 'CopyTo', 'Count', 'Equals', 'Finalize', 'GetEnumerator', 'GetHashCode', 'GetType', 'GetValue', 'IsReadOnly', 'Keys', 'MemberwiseClone', 'Overloads', 'ReferenceEquals', 'Remove', 'Time', 'ToString', 'TryGetValue', 'Values', '__call__', '__class__', '__delattr__', '__delitem__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__overloads__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__setitem__', '__sizeof__', '__str__', '__subclasshook__', 'get_Count', 'get_IsReadOnly', 'get_Item', 'get_Keys', 'get_Time', 'get_Values', 'set_Item', 'set_Time']

### for chain in slice.FutureChains:

['Deconstruct', 'Equals', 'Finalize', 'GetHashCode', 'GetType', 'Key', 'MemberwiseClone', 'Overloads', 'ReferenceEquals', 'ToString', 'Value', '__call__', '__class__', '__delattr__', '__delitem__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__overloads__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__setitem__', '__sizeof__', '__str__', '__subclasshook__', 'get_Key', 'get_Value']


## def OnDataConsolidated(self, sender, bar)

### sender

2019-05-20 21:00:00 ['AggregateBar', 'Consolidated', 'DataConsolidated', 'Dispose', 'Equals', 'Finalize', 'GetHashCode', 'GetRoundedBarTime', 'GetType', 'InputType', 'IsTimeBased', 'MemberwiseClone', 'OnDataConsolidated', 'OutputType', 'Overloads', 'Period', 'ReferenceEquals', 'Scan', 'ShouldProcess', 'ToString', 'Update', 'WorkingData', '__call__', '__class__', '__delattr__', '__delitem__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__overloads__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__setitem__', '__sizeof__', '__str__', '__subclasshook__', 'add_DataConsolidated', 'get_Consolidated', 'get_InputType', 'get_IsTimeBased', 'get_OutputType', 'get_Period', 'get_WorkingData', 'remove_DataConsolidated']

### bar

2019-05-20 21:00:00 ['Ask', 'Bid', 'Clone', 'Close', 'Collapse', 'DataType', 'DeserializeMessage', 'EndTime', 'Equals', 'Finalize', 'GetHashCode', 'GetSource', 'GetType', 'High', 'IsFillForward', 'LastAskSize', 'LastBidSize', 'Low', 'MemberwiseClone', 'Open', 'Overloads', 'ParseCfd', 'ParseEquity', 'ParseForex', 'ParseFuture', 'ParseOption', 'Period', 'Price', 'Reader', 'ReferenceEquals', 'Symbol', 'Time', 'ToString', 'Update', 'UpdateAsk', 'UpdateBid', 'UpdateQuote', 'UpdateTrade', 'Value', '__call__', '__class__', '__delattr__', '__delitem__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__overloads__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__setitem__', '__sizeof__', '__str__', '__subclasshook__', 'get_Ask', 'get_Bid', 'get_Close', 'get_DataType', 'get_EndTime', 'get_High', 'get_IsFillForward', 'get_LastAskSize', 'get_LastBidSize', 'get_Low', 'get_Open', 'get_Period', 'get_Price', 'get_Symbol', 'get_Time', 'get_Value', 'set_Ask', 'set_Bid', 'set_DataType', 'set_EndTime', 'set_LastAskSize', 'set_LastBidSize', 'set_Period', 'set_Symbol', 'set_Time', 'set_Value']


## for bar in history.itertuples()

2019-05-01 06:35:00 ['Index', '__add__', '__class__', '__contains__', '__delattr__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__getitem__', '__getnewargs__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__iter__', '__le__', '__len__', '__lt__', '__module__', '__mul__', '__ne__', '__new__', '__reduce__', '__reduce_ex__', '__repr__', '__rmul__', '__setattr__', '__sizeof__', '__slots__', '__str__', '__subclasshook__', '_asdict', '_fields', '_make', '_replace', '_source', 'close', 'count', 'high', 'index', 'low', 'open', 'symbol', 'time', 'volume']
