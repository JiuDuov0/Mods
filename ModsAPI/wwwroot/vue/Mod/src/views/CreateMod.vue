<template>
    <el-container>
        <el-main>
            <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
                <el-col :span="12">
                    <el-card>
                        <h1 style="text-align: center; margin-bottom: 24px;">发布 Mod</h1>
                        <div>
                            <el-input v-model="modForm.name" placeholder="请输入 Mod 名称" style="margin-bottom: 16px;" />
                            <el-input type="textarea" v-model="modForm.description" placeholder="请输入 Mod 描述"
                                style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.videoUrl" placeholder="请输入视频BV号" style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.PicUrl" placeholder="请输入封面URL" style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.version" placeholder="请输入版本号" style="margin-bottom: 16px;" />
                            <el-input type="textarea" v-model="modForm.versionDescription" placeholder="请输入版本描述"
                                style="margin-bottom: 16px;" />
                            <el-select v-model="modForm.tags" multiple placeholder="请选择标签"
                                style="margin-bottom: 16px; width: 100%;">
                                <el-option v-for="tag in tags" :key="tag.TypesId" :label="tag.TypeName"
                                    :value="tag.TypesId"></el-option>
                            </el-select>
                            <el-button type="primary" block @click="handleSubmit">提交</el-button>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
        <div class="account-info">
            <el-avatar :src="headurl"></el-avatar>
            <el-dropdown>
                <span class="username" @click="handleDropdownClick">{{ NickName }}</span>
                <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item @click.native="handleHome">主页</el-dropdown-item>
                        <el-dropdown-item @click.native="handleProfile">个人资料</el-dropdown-item>

                        <el-dropdown-item v-if="Role === 'Auditors'"
                            @click.native="handleapproveModVersion">审核Mod</el-dropdown-item>
                        <el-dropdown-item v-if="Role === 'Developer'"
                            @click.native="handleapproveModVersion">审核Mod</el-dropdown-item>
                        <el-dropdown-item v-if="Role === 'Developer'"
                            @click.native="handleProfile">添加审核人</el-dropdown-item>

                        <el-dropdown-item @click.native="handleCreateMod">发布新Mod</el-dropdown-item>
                        <el-dropdown-item @click.native="handleMyCreateMods">我发布的Mod</el-dropdown-item>
                        <el-dropdown-item @click.native="handleSubscribeMod">我订阅的Mod</el-dropdown-item>
                        <el-dropdown-item @click.native="handleLogout">退出登录</el-dropdown-item>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
        </div>
    </el-container>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import { th } from 'element-plus/es/locale/index.mjs';
import head from '../assets/head.jpg';

export default {
    name: 'CreateMod',
    data() {
        return {
            modForm: {
                name: '',
                description: '',
                version: '',
                versionDescription: '',
                PicUrl: '',
                videoUrl: '',
                tags: []
            },
            tags: [],
            Role: localStorage.getItem('Role'),
            headurl: head,
            NickName: ""
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
        this.fetchTags();
    },
    methods: {
        fetchTags() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/GetAllModTypes',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
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
                        this.tags = data.ResultData;
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleSubmit() {
            if (!this.modForm.name || this.modForm.name.length > 20) {
                ElMessage.error('请输入有效的 Mod 名称（20 字以内）');
                return;
            }
            if (!this.modForm.description) {
                ElMessage.error('请输入 Mod 描述');
                return;
            }
            if (!this.modForm.version || this.modForm.version.length > 10) {
                ElMessage.error('请输入有效的版本号（10 字以内）');
                return;
            }
            if (!this.modForm.versionDescription) {
                ElMessage.error('请输入版本描述');
                return;
            }
            if (this.modForm.tags.length == 0) {
                ElMessage.error('请选择标签');
                return;
            }

            var formData = {
                Name: this.modForm.name,
                Description: this.modForm.description,
                VideoUrl: this.modForm.videoUrl,
                ModVersionEntities: [{
                    VersionNumber: this.modForm.version,
                    Description: this.modForm.versionDescription
                }],
                PicUrl: this.modForm.PicUrl,
                ModTypeEntities: []
            };
            this.modForm.tags.forEach(tag => {
                formData.ModTypeEntities.push({
                    TypesId: tag
                });
            });

            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/CreateMod',
                type: "POST",
                data: JSON.stringify(formData),
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('提交失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('提交成功');
                        router.push({
                            path: '/addVersionFile',
                            query: {
                                VersionId: data.ResultData.ModVersionEntities[0].VersionId
                            }
                        });
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('提交失败 ');
                    console.log(err);
                }
            });
        },
        handleReset() {
            this.modForm = {
                name: '',
                description: '',
                videoUrl: '',
                version: '',
                versionDescription: '',
                tags: []
            };
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            // 处理点击事件返回主页
            router.push('/home');
        },
        handleProfile() {
            // 处理个人资料点击事件
        },
        handleMyCreateMods() {
            // 处理我上传的Mod点击事件
            router.push('/myCreateMods');
        },
        handleSubscribeMod() {
            // 处理我订阅的Mod点击事件
            router.push('/mySubscribeMods');
        },
        handleCreateMod() {
            // 处理发布新Mod点击事件
            router.push('/createMod');
        },
        handleLogout() {
            // 处理退出登录点击事件
            ElMessage.info('退出登录');
            localStorage.removeItem('token');
            localStorage.removeItem('refresh_Token');
            localStorage.removeItem('NickName');
            router.push('/');
        }
    }
};
</script>

<style scoped>
.el-card {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.el-input {
    margin-bottom: 16px;
}

.el-button {
    width: 100%;
    height: 40px;
    font-size: 16px;
}

.account-info {
    position: fixed;
    bottom: 20px;
    /* 距离页面底部 20px */
    left: 20px;
    /* 距离页面左侧 20px */
    display: flex;
    align-items: center;
    cursor: pointer;
    z-index: 500;
    padding: 10px;
}

.username {
    margin-left: 10px;
    margin-right: 10px;
    font-size: 16px;
}
</style>