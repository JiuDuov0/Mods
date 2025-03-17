<template>
    <el-container>
        <el-main>

            <el-row :gutter="20">
                <el-col :span="1"></el-col>
                <el-col :span="23">
                    <h2>{{ Name }}</h2>
                </el-col>
                <el-col :span="16">
                    <el-card>
                        <iframe width="100%" height="400" :src="videoUrl" frameborder="0" allowfullscreen></iframe>
                    </el-card>
                    <el-card style="margin-top: 20px;">
                        <h3>Mod描述</h3>
                        <p>{{ description }}</p>
                    </el-card>
                </el-col>
                <el-col :span="8">
                    <el-card>
                        <el-button type="primary" block @click="handleSubscribe(ModId)"
                            v-if="!isSubscribed">订阅</el-button>
                        <el-button type="danger" block @click="handleUnsubscribe(ModId)" v-else>取消订阅</el-button>
                    </el-card>
                    <el-card style="margin-top: 20px;">
                        <h3>Mod 信息</h3>
                        <p>作者: {{ modAuthor }}</p>
                        <p>下载次数: {{ downloads }}</p>
                        <p>创建时间: {{ createdAt }}</p>
                        <p>标签: <el-tag v-for="tag in tags" :key="tag">{{ tag }}</el-tag></p>
                    </el-card>
                    <el-card style="margin-top: 20px;">
                        <h3>最新版本</h3>
                        <p>版本号: {{ latestVersion.version }}</p>
                        <p>版本描述: {{ latestVersion.description }}</p>
                        <p>更新时间: {{ latestVersion.CreatedAt }}</p>
                        <el-button type="primary" block @click="downloadLatestVersion(ModId)">下载</el-button>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';

export default {
    name: 'ModDetail',
    data() {
        return {
            Name: '',
            videoUrl: '',
            description: '',
            isSubscribed: false,
            modAuthor: '作者名称',
            downloads: "",
            subscribers: "",
            createdAt: "",
            tags: [],
            latestVersion: {
                version: '',
                description: '',
                size: '',
                FilesId: ''
            },
            ModId: this.$route.query.ModId,
            entity: null
        };
    },
    mounted() {
        this.modDetail();
    },
    methods: {
        modDetail() {
            // 处理获取 Mod 详情逻辑
            $.ajax({
                url: 'http://43.160.202.17:8099/api/Mod/ModDetail',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModId: this.$route.query.ModId
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('获取失败: ' + data.ResultMsg);
                    } else {
                        // 赋值
                        this.entity = data.ResultData;
                        this.Name = data.ResultData.Name;
                        this.createdAt = data.ResultData.CreatedAt;
                        this.videoUrl = data.ResultData.VideoUrl;
                        this.description = data.ResultData.Description;
                        this.modAuthor = data.ResultData.CreatorEntity.NickName;
                        this.downloads = data.ResultData.DownloadCount;
                        if (data.ResultData.ModTypeEntities !== null) {
                            data.ResultData.ModTypeEntities.forEach(element => {
                                this.tags.push(element.Types.TypeName);
                            });
                        }
                        this.isSubscribed = data.ResultData.IsMySubscribe;
                        this.latestVersion.version = data.ResultData.ModVersionEntities[0].VersionNumber;
                        this.latestVersion.description = data.ResultData.ModVersionEntities[0].Description;
                        this.latestVersion.FilesId = data.ResultData.ModVersionEntities[0].FilesId;
                        this.latestVersion.CreatedAt = data.ResultData.ModVersionEntities[0].CreatedAt;
                    }
                },
                error: (err) => {
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleSubscribe(ModId) {
            $.ajax({
                url: 'http://43.160.202.17:8099/api/User/ModSubscribe',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModId: ModId
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('订阅失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('订阅成功');
                        this.isSubscribed = true;
                    }
                },
                error: (err) => {
                    ElMessage.error('订阅失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleUnsubscribe(ModId) {
            // 处理取消订阅逻辑
            $.ajax({
                url: 'http://43.160.202.17:8099/api/User/UserUnsubscribeMod',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModId: ModId
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == false || data.ResultData == null) {
                        ElMessage.error('取消订阅失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('取消订阅成功！');
                        this.isSubscribed = false;
                    }
                },
                error: (err) => {
                    ElMessage.error('请求失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        downloadLatestVersion(ModId) {
            // 处理下载最新版本逻辑
        }
    }
};
</script>

<style scoped>
.el-card {
    margin-bottom: 20px;
}

.el-button {
    width: 100%;
}
</style>