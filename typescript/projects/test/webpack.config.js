module.exports = 
{
	entry:{
		test: "./src/test.ts",
	},
	output:{
		filename: "./dist/[name].bundle.js",
		devtoolLineToLine: true,
		sourceMapFilename: "./dist/[name].bundle.js.map",
		pathinfo: true
	},
	resolve:{
		extensions: [".ts"]
	},
	module:{
		loaders: [
			{
				exclude: /(node_modules|dist)/,
				loader: "ts-loader",
			}
		]
	},
	devtool:"source-map",
}