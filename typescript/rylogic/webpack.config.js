const path = require('path')

module.exports =
{
	entry: {
		rylogic: path.resolve(__dirname, "src/rylogic.ts"),
	},
	output: {
		library: "rylogic",
		path: path.resolve(__dirname,"obj"),
		filename: "[name].bundle.js",
		sourceMapFilename: "[name].bundle.js.map",
		libraryTarget: "umd",
		devtoolLineToLine: true,
		pathinfo: true
	},
	module: {
		loaders: [{
			loader: "ts-loader",
			exclude: /(bin|node_modules|obj|unittests)/,
			options: { transpileOnly: false }
		}]
	},
	resolve: {
		extensions: [".ts"],
	},
	//devtool:"source-map",
	plugins: []
}