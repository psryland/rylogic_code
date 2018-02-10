var fs = require('fs');
var path = require('path');
var webpack = require('webpack');

var entryFile = './src/trading_chart.js';

module.exports = {
    entry: entryFile,
    output: {
        path: __dirname + '/dist',
        filename: 'trading_chart.js',
        libraryTarget: 'umd',
    },
    module: {
        loaders: [{
            test: path.join(__dirname, 'src'),
            loader: 'babel-loader',
        }]
    },
    plugins: [
    ]
};