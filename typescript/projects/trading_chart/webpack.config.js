var path = require('path');

module.exports =
{
	mode: "production",
	entry: {
		trading_chart: "./src/trading_chart.ts",
	},
	output: {
		filename: "./dist/[name].bundle.js",
		sourceMapFilename: "./dist/[name].bundle.js.map",
		libraryTarget: "umd",
		pathinfo: true
	},
	resolve: {
		extensions: [".ts"]
	},
	module: {
		rules: [{
			use:[{
				loader: "ts-loader",
				options: {
					transpileOnly: false
				}
			}]
		}]
	},
	plugins: []
}