const path = require("path");
const MonacoWebpackPlugin = require('monaco-editor-webpack-plugin');

module.exports = {
    entry: "./components/index.tsx",

    output: {
        filename: "bundle.js",
        path: __dirname + "/dist/bundle",
        publicPath: "",
    },

    plugins: [
        new MonacoWebpackPlugin()
    ],

    // Enable sourcemaps for debugging webpack"s output.
    devtool: "source-map",

    resolve: {
        // Add ".ts" and ".tsx" as resolvable extensions.
        extensions: [".ts", ".tsx", ".js", ".json", ".css"],
        modules: [ "node_modules" ]
    },

    module: {
        rules: [
            // All files with a ".ts" or ".tsx" extension will be handled by "awesome-typescript-loader".
            { test: /\.tsx?$/, loader: "awesome-typescript-loader" },

            // All output ".js" files will have any sourcemaps re-processed by "source-map-loader".
            {
                "enforce": "pre",
                "test": /\.js$/,
                "loader": "source-map-loader",
                "exclude": [
                  // instead of /\/node_modules\//
                  path.join(process.cwd(), 'node_modules')
                ]
              },

            { test: /\.css$/, use: ["style-loader", "css-loader"] }
        ]
    },

    // When importing a module whose path matches one of the following, just
    // assume a corresponding global variable exists and use that instead.
    // This is important because it allows us to avoid bundling all of our
    // dependencies, which allows browsers to cache those libraries between builds.
    externals: {
        "react": "React",
        "react-dom": "ReactDOM"
    },
};
