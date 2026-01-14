module.exports = {
    assetsDir: 'static',
    parallel: false,
    publicPath: './', // 确保这里设置为相对路径
    filenameHashing: true,
    configureWebpack: { output: { filename: '[name].[contenthash].js', } }
}