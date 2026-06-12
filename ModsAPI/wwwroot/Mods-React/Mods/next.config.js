const nextConfig = {
    reactStrictMode: false,
    images: {
        remotePatterns: [
            {
                protocol: 'https',
                hostname: '**',
            },
        ],
    },
    // 修复 Turbopack 工作区根目录警告
    turbopack: {
        root: __dirname,
    },
};

module.exports = nextConfig;
