var path = require('path');

module.exports =
	{
		entry: {
			trading_chart: "./src/trading_chart.ts",
		},
		output: {
			filename: "./dist/[name].bundle.js",
			sourceMapFilename: "./dist/[name].bundle.js.map",
			libraryTarget: "umd",
			devtoolLineToLine: true,
			pathinfo: true
		},
		resolve: {
			extensions: [".ts"]
		},
		module: {
			loaders: [{
				loader: "ts-loader",
				exclude: /(node_modules|built|dist|obj|bin)/,
				options: { transpileOnly: false }
			}]
		},
		//devtool:"source-map",
		plugins: []
	}