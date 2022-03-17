const path = require('path')

module.exports =
{
	mode: "production",
	entry: {
		rylogic: path.resolve(__dirname, "src/rylogic.ts"),
	},
	output: {
		library: "rylogic",
		path: path.resolve(__dirname,"obj"),
		filename: "[name].bundle.js",
		sourceMapFilename: "[name].bundle.js.map",
		libraryTarget: "umd",
		pathinfo: true
	},
	resolve: {
		extensions: [".ts"],
	},
	module: {
		rules: [{
			use:[{
				loader: "ts-loader",
				options: {
					transpileOnly: false
				},
			}]
		}]
	},
	plugins: []
}