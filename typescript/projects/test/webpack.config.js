module.exports = 
{
	mode: "development",
	entry:{
		test: "./src/test.ts",
	},
	output:{
		filename: "./dist/[name].bundle.js",
		sourceMapFilename: "./dist/[name].bundle.js.map",
		pathinfo: true
	},
	resolve:{
		extensions: [".ts"]
	},
	module:{
		rules: [{
			use: [{
				loader: "ts-loader",
				options:{}
			}]
		}]
	},
	devtool:"source-map",
}