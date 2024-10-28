#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import os, requests, json, datetime
import matplotlib.pyplot as plt
from typing import List

__dir__ = os.path.dirname(os.path.abspath(__file__))

# Create a type definition from this
class CandleData:
    def __init__(self, open_time: int, open_price: str, high_price: str, low_price: str, close_price: str, volume: str, close_time: int, quote_asset_volume: str, number_of_trades: int, taker_buy_base_asset_volume: str, taker_buy_quote_asset_volume: str, unused: str):
        self.open_time = datetime.datetime.fromtimestamp(open_time / 1000)
        self.open_price = float(open_price)
        self.high_price = float(high_price)
        self.low_price = float(low_price)
        self.close_price = float(close_price)
        self.volume = float(volume)
        self.close_time = datetime.datetime.fromtimestamp(close_time / 1000)
        self.quote_asset_volume = float(quote_asset_volume)
        self.number_of_trades = int(number_of_trades)
        self.taker_buy_base_asset_volume = float(taker_buy_base_asset_volume)
        self.taker_buy_quote_asset_volume = float(taker_buy_quote_asset_volume)
        self.unused = int(unused)

# Bot for buying and selling BTC
class Bot:
    def __init__(self, trade_size_usdt: float, step_change_usdt: float, data: List[CandleData]):
        self.data = data
        self.usdt = 2000
        self.btc = 0.02574
        self.trade_size_usdt = trade_size_usdt
        self.step_change_usdt = step_change_usdt
        self.last_trade_price = 0
        self.last_trade_type = "buy"
        self.spread = 50
        self.buys = []
        self.sells = []
        self.verbose = False

    def Run(self):

        self.initial_value = self.usdt + self.btc * self.data[0].close_price

        for candle in self.data:

            # Sell on the way up
            if candle.close_price > self.last_trade_price + self.step_change_usdt:
                self.Sell(candle.close_price, candle.close_time)

            # But on the way down
            if candle.close_price < self.last_trade_price - self.step_change_usdt:
                self.Buy(candle.close_price, candle.close_time)
        
        self.final_value = self.usdt + self.btc * self.data[-1].close_price
        self.final_value_normalised = self.usdt + self.btc * self.data[0].close_price

    def Buy(self, price_usdt_per_btc: float, date: datetime.datetime):
        trade_size_usdt = self.trade_size_usdt
        price_usdt_per_btc += self.spread
        
        # Not enough USDT to buy with
        if (self.usdt < trade_size_usdt):
            #self.usdt += trade_size_usdt # top up the account
            return # don't buy, not enough USDT

        self.last_trade_type = "buy"
        self.usdt -= trade_size_usdt
        self.btc += trade_size_usdt / price_usdt_per_btc
        self.last_trade_price = price_usdt_per_btc
        self.buys.append((date, price_usdt_per_btc))
        if self.verbose: self.ShowTrade("Buy", price_usdt_per_btc, date)

    def Sell(self, price_usdt_per_btc: float, date: datetime.datetime):
        trade_size_usdt = self.trade_size_usdt

        if (self.btc < trade_size_usdt / price_usdt_per_btc):
            return # don't sell, not enough BTC

        self.last_trade_type = "sell"
        self.usdt += trade_size_usdt
        self.btc -= trade_size_usdt / price_usdt_per_btc
        self.last_trade_price = price_usdt_per_btc
        self.sells.append((date, price_usdt_per_btc))
        if self.verbose: self.ShowTrade("Sell", price_usdt_per_btc, date)

    def ShowTrade(self, trade_type: str, price: float, date: datetime.datetime):
        print(f"{trade_type} at {date.date()}: {price} USDT/BTC -- Balance: {self.usdt} USDT, {self.btc} BTC")

    def AdjustedProfit(self):
        return self.final_value_normalised - self.initial_value

# Read price data
def ReadData(symbol: str):
    # If the data are cached, use the cache
    data_file = os.path.join(__dir__, f"data/{symbol}.json")
    if os.path.exists(data_file):
        with open(data_file, "r") as file:
            json_data = json.load(file)

    # Otherwise, request it
    else:
        json_data = []
        start_time = int(datetime.datetime(2015, 1, 1).timestamp() * 1000)
        while start_time < int(datetime.datetime.now().timestamp() * 1000):
            url = f"https://api.binance.com/api/v3/klines?symbol={symbol}&interval=1d&startTime={start_time}&limit=1000"
            response = requests.get(url)
            chunk = response.json()
            json_data.extend(chunk)

            start_time = int(chunk[-1][6])

        # Write the json data to a file
        with open(data_file, "w") as file:
            json.dump(json_data, file, indent=4)

    # Convert the data to a list of CandleData objects
    data = [CandleData(*candle) for candle in json_data]
    rdata = [CandleData(*candle) for candle in reversed(json_data)]
    
    for i in range(0, len(rdata)):
        rdata[i].open_time = data[i].open_time
        rdata[i].close_time = data[i].close_time
        pass

    return data, rdata

def Optimise(data: List[CandleData], reversed_data: List[CandleData]):

    results0 = []
    for trade_size_usdt in range(250, 450, 5):
        for step_change_usdt in range(200, 600, 20):
            bot = Bot(trade_size_usdt, step_change_usdt, data)
            bot.Run()
            results0.append((trade_size_usdt, step_change_usdt, bot.AdjustedProfit()))

    results1 = []
    for trade_size_usdt in range(250, 450, 5):
        for step_change_usdt in range(200, 600, 20):
            bot = Bot(trade_size_usdt, step_change_usdt, reversed_data)
            bot.Run()
            results1.append((trade_size_usdt, step_change_usdt, bot.AdjustedProfit()))


    best_trade_size, best_step_change, best_profit = max(results0, key=lambda x: x[2])
    print(f"Best Fwd Trade Size: {best_trade_size}")
    print(f"Best Fwd Step Change: {best_step_change}")
    print(f"Best Fwd Profit: {best_profit}")

    best_trade_size, best_step_change, best_profit = max(results1, key=lambda x: x[2])
    print(f"Best Bwd Trade Size: {best_trade_size}")
    print(f"Best Bwd Step Change: {best_step_change}")
    print(f"Best Bwd Profit: {best_profit}")

    # Find the best trade size and step change
    results = results0 + results1
    best_trade_size, best_step_change, best_profit = max(results, key=lambda x: x[2])
    print(f"Best Trade Size: {best_trade_size}")
    print(f"Best Step Change: {best_step_change}")
    print(f"Best Profit: {best_profit}")

    # Create a surface plot
    fig = plt.figure()
    ax = fig.add_subplot(111, projection='3d')
    x = [x[0] for x in results]
    y = [x[1] for x in results]
    z = [x[2] for x in results]
    surf = ax.plot_trisurf(x, y, z)
    ax.set_xlabel('Trade Size (USDT)')
    ax.set_ylabel('Step Change (USDT)')
    ax.set_zlabel('Profit (USDT)')

    # Add a color bar which maps values to colors.
    fig.colorbar(surf, shrink=0.5, aspect=5)

    plt.show()

    # # Create a 3d plot of trade size and step change vs. profit
    # fig = plt.figure()
    # ax = fig.add_subplot(111, projection='3d')
    # ax.scatter([x[0] for x in results], [x[1] for x in results], [x[2] for x in results])
    # ax.set_xlabel('Trade Size (USDT)')
    # ax.set_ylabel('Step Change (USDT)')
    # ax.set_zlabel('Profit (USDT)')
    # plt.show()

    return (best_trade_size, best_step_change, best_profit)

def ShowResults(bot: Bot):
    print(f"Trade Count (buy:sell): {len(bot.buys)}:{len(bot.sells)}")
    print(f"USDT: {bot.usdt}")
    print(f"BTC: {bot.btc}")
    print(f"Profit: {bot.final_value - bot.initial_value} USDT")
    print(f"Profit (N): {bot.final_value_normalised - bot.initial_value} USDT")

    # Plot the BTC and buy/sells
    times = [candle.close_time for candle in bot.data]
    prices = [float(candle.close_price) for candle in bot.data]
    plt.plot(times, prices)

    # Add buy/sell points
    for buy in bot.buys:
        plt.scatter(buy[0], buy[1], color="green")
    for sell in bot.sells:
        plt.scatter(sell[0], sell[1], color="red")

    plt.show()

# Entry Point
if __name__ == "__main__":
    symbol = "BTCUSDT"
    step_change = 380.0
    trade_size = 330.0

    # Load the price data
    data, rdata = ReadData(symbol)

    # Search for the best choose of trade size and step change
    trade_size, step_change, profit = Optimise(data, rdata)

    botf = Bot(trade_size, step_change, data)
    #botb = Bot(trade_size, step_change, rdata)
    botf.Run()
    #botb.Run()
    ShowResults(botf)
    #ShowResults(botb)

    input("Any key to exit...")