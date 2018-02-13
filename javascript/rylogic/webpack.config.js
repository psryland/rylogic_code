var fs = require('fs');
var path = require('path');
var webpack = require('webpack');

var entryFile = './src/rylogic.js';

module.exports = {
	entry: entryFile,
	output: {
		path: __dirname + '/dist',
		filename: 'rylogic.js',
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