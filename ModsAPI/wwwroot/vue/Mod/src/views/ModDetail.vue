<template>
    <el-container>
        <el-main>
            <el-row :gutter="20">
                <el-col :span="16">
                    <!-- 红色框：视频 iframe -->
                    <el-card>
                        <iframe width="100%" height="400" :src="videoUrl" frameborder="0" allowfullscreen></iframe>
                    </el-card>
                    <!-- 黄色框：描述 -->
                    <el-card style="margin-top: 20px;">
                        <h3>描述</h3>
                        <p>{{ description }}</p>
                    </el-card>
                </el-col>
                <el-col :span="8">
                    <!-- 绿色框：订阅按钮 -->
                    <el-card>
                        <el-button type="primary" block @click="handleSubscribe" v-if="!isSubscribed">订阅</el-button>
                        <el-button type="danger" block @click="handleUnsubscribe" v-else>取消订阅</el-button>
                    </el-card>
                    <!-- 蓝色框：Mod 作者等信息 -->
                    <el-card style="margin-top: 20px;">
                        <h3>Mod 信息</h3>
                        <p>作者: {{ modAuthor }}</p>
                        <p>下载次数: {{ downloads }}</p>
                        <p>订阅者: {{ subscribers }}</p>
                        <p>标签: <el-tag v-for="tag in tags" :key="tag">{{ tag }}</el-tag></p>
                    </el-card>
                    <!-- 白色框：Mod 的最后一个版本的信息 -->
                    <el-card style="margin-top: 20px;">
                        <h3>最新版本</h3>
                        <p>版本: {{ latestVersion.version }}</p>
                        <p>文件大小: {{ latestVersion.size }}</p>
                        <el-button type="primary" block @click="downloadLatestVersion">下载</el-button>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
export default {
    name: 'ModDetail',
    data() {
        return {
            videoUrl: 'https://www.youtube.com/embed/dQw4w9WgXcQ', // 示例视频 URL
            description: '这是一个示例描述。',
            isSubscribed: false,
            modAuthor: '作者名称',
            downloads: 2712775,
            subscribers: 534133,
            tags: ['1.39', 'Verified', 'Framework', 'Optional'],
            latestVersion: {
                version: '1.2.5',
                size: '1.66 MB'
            }
        };
    },
    methods: {
        handleSubscribe() {
            this.isSubscribed = true;
            // 处理订阅逻辑
        },
        handleUnsubscribe() {
            this.isSubscribed = false;
            // 处理取消订阅逻辑
        },
        downloadLatestVersion() {
            // 处理下载最新版本逻辑
        }
    }
};
</script>

<style scoped>
.el-card {
    margin-bottom: 20px;
}
</style>