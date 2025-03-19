<template>
    <el-container>
        <el-main>
            <el-row :gutter="20">
                <el-col :span="1"><el-icon class="fullicon" @click="goBack">
                        <Back />
                    </el-icon></el-col>
                <el-col :span="23">
                    <h2>{{ Name }}</h2>
                </el-col>
                <el-col :span="16">
                    <iframe width="100%" height="400" :src="videoUrl" frameborder="0" allowfullscreen></iframe>

                    <el-card style="margin-top: 20px;">
                        <h3>Mod描述</h3>
                        <div id="text"></div>
                        <!-- {{ description }} -->
                    </el-card>
                </el-col>
                <el-col :span="8" class="sticky-subscribe">
                    <el-button type="primary" block @click="handleSubscribe(ModId)" v-if="!isSubscribed">订阅</el-button>
                    <el-button type="danger" block @click="handleUnsubscribe(ModId)" v-else>取消订阅</el-button>
                    <el-card style="margin-top: 20px;">
                        <h3>Mod 信息</h3>
                        <p>作者: {{ modAuthor }}</p>
                        <p>下载次数: {{ downloads }}</p>
                        <p>创建时间: {{ createdAt }}</p>
                        <p>标签: <el-tag v-for="tag in tags" :key="tag">{{ tag }}</el-tag></p>
                        <p><el-rate v-model="AVGPoint" disabled show-score text-color="#ff9900"></el-rate>
                        </p>
                    </el-card>
                    <el-card style="margin-top: 20px;">
                        <h3>最新版本</h3>
                        <p>版本号: {{ latestVersion.version }}</p>
                        <p>版本描述: {{ latestVersion.description }}</p>
                        <p>更新时间: {{ latestVersion.CreatedAt }}</p>
                        <!-- <el-button type="primary" block @click="downloadLatestVersion(ModId)">下载</el-button> -->
                    </el-card>
                </el-col>
            </el-row>
        </el-main>

        <!-- 评分悬浮窗 -->
        <div class="rating-float" @click="toggleRatingWindow">
            <el-icon>
                <Star />
            </el-icon>
            <span>评分</span>
        </div>

        <!-- 评分窗口 -->
        <el-dialog title="评分" v-model="ratingWindowVisible" width="30%">
            <div style="text-align: center;">
                <el-rate v-model="rating" :max="5"></el-rate>
                <p style="margin-top: 10px;">当前评分: {{ rating }}</p>
            </div>
            <span slot="footer" class="dialog-footer">
                <!-- <el-button @click="ratingWindowVisible = false">取消</el-button> -->
                <el-button type="primary" @click="submitRating">提交</el-button>
            </span>
        </el-dialog>
    </el-container>
</template>
<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import { da } from 'element-plus/es/locale/index.mjs';

export default {
    name: 'ModDetail',
    data() {
        return {
            Name: '',
            videoUrl: '',
            description: '',
            isSubscribed: false,
            modAuthor: '',
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
            entity: null,
            rating: 5, // 当前评分
            AVGPoint: 0.0, // 平均评分
            pointentity: null,
            ratingWindowVisible: false // 评分窗口是否可见
        };
    },
    mounted() {
        this.modDetail();
        $("#text").html(this.description);
    },
    methods: {
        modDetail() {
            // 获取 Mod 详情逻辑
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/ModDetail',
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
                        this.AVGPoint = data.ResultData.AVGPoint;
                    }
                },
                error: (err) => {
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/GetModPointByModId',
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
                        //暂未评分
                    } else {
                        // 赋值
                        this.rating = data.ResultData.Point;
                        this.pointentity = data.ResultData;
                    }
                },
                error: (err) => {
                    console.log(err);
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                }
            });
        },
        handleSubscribe(ModId) {
            $.ajax({
                url: 'https://modcat.top:8089/api/User/ModSubscribe',
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
                url: 'https://modcat.top:8089/api/User/UserUnsubscribeMod',
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
        },
        toggleRatingWindow() {
            this.ratingWindowVisible = !this.ratingWindowVisible;
        },
        submitRating() {
            if (this.pointentity === null) {
                this.addPoint();
            } else {
                this.updatePoint();
            }
        },
        addPoint() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/AddModPoint',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModId: this.ModId,
                    Point: this.rating
                }),
                success: (data) => {
                    if (data.ResultCode === 200) {
                        ElMessage.success('评分提交成功');
                        this.ratingWindowVisible = false;
                    } else {
                        ElMessage.error('评分提交失败: ' + data.ResultMsg);
                    }
                },
                error: (err) => {
                    ElMessage.error('评分提交失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        updatePoint() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/UpdateModPoint',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModPointId: this.pointentity.ModPointId,
                    UserId: this.pointentity.UserId,
                    ModId: this.ModId,
                    Point: this.rating
                }),
                success: (data) => {
                    if (data.ResultCode === 200) {
                        ElMessage.success('评分提交成功');
                        this.ratingWindowVisible = false;
                    } else {
                        ElMessage.error('评分提交失败: ' + data.ResultMsg);
                    }
                },
                error: (err) => {
                    ElMessage.error('评分提交失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        goBack() {
            router.go(-1);
        }
    }
};
</script>
<style scoped>
.el-card {
    margin-bottom: 20px;
}

.el-tag {
    margin-left: 4px;
    background-color: #e4e7ed;
    color: black;
    border-color: #e4e7ed;
}

.el-button {
    width: 100%;
    color: black;
    background-color: #e4e7ed;
    border-style: solid;
    border-color: #e4e7ed;
}

.rating-float {
    position: fixed;
    bottom: 20px;
    right: 20px;
    background-color: #409eff;
    color: white;
    padding: 10px 20px;
    border-radius: 50px;
    cursor: pointer;
    display: flex;
    align-items: center;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

.rating-float:hover {
    background-color: #66b1ff;
}

.rating-float span {
    margin-left: 10px;
    font-size: 16px;
}

.el-dialog {
    z-index: 2000 !important;
}

.sticky-subscribe {
    position: fixed;
    z-index: 1000;
    right: 1%;
    width: 35%;
}

.fullicon {
    width: 100%;
    height: 100%;
}
</style>