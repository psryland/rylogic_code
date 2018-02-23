module.exports =
	{
		entry: {
			rylogic: "./src/rylogic.ts",
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
				exclude: /(built|node_modules|dist|obj|bin)/,
				options: { transpileOnly: false }
			}]
		},
		//devtool:"source-map",
		plugins: []
	}