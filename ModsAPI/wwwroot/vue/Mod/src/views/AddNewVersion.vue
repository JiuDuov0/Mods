<template>
    <el-container>
        <el-header>
            <div class="account-info">
                <el-avatar src="../src/assets/head.jpg"></el-avatar>
                <el-dropdown>
                    <span class="username" @click="handleDropdownClick">{{ NickName }}</span>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item @click.native="handleHome">主页</el-dropdown-item>
                            <el-dropdown-item @click.native="handleProfile">个人资料</el-dropdown-item>
                            <el-dropdown-item @click.native="handleCreateMod">发布新Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleMyCreateMods">我发布的Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleSubscribeMod">我订阅的Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleLogout">退出登录</el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </el-header>
        <el-main>
            <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
                <el-col :span="12">
                    <el-card>
                        <h1 style="text-align: center; margin-bottom: 24px;">添加新版本</h1>
                        <div>
                            <el-input v-model="versionForm.version" placeholder="请输入版本号" maxlength="10"
                                style="margin-bottom: 16px;" />
                            <el-input type="textarea" v-model="versionForm.description" placeholder="请输入版本描述"
                                style="margin-bottom: 16px;" />
                            <el-button type="primary" block @click="submitVersion">提交</el-button>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import $ from 'jquery';

export default {
    name: 'AddNewVersion',
    data() {
        return {
            NickName: '',
            ModId: this.$route.query.ModId, // 从路由参数获取 Mod ID
            versionForm: {
                version: '',
                description: ''
            }
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
    },
    methods: {
        submitVersion() {
            if (!this.versionForm.version || this.versionForm.version.length > 10) {
                ElMessage.error('请输入有效的版本号（10字以内）');
                return;
            }
            if (!this.versionForm.description) {
                ElMessage.error('请输入版本描述');
                return;
            }

            $.ajax({
                url: 'http://43.160.202.17:8099/api/Mod/ModAddVersion', // 替换为实际的 API URL
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    ModId: this.ModId,
                    VersionNumber: this.versionForm.version,
                    Description: this.versionForm.description
                }),
                success: (data) => {
                    if (data.ResultCode === 200) {
                        ElMessage.success('版本添加成功');
                        this.versionForm.version = '';
                        this.versionForm.description = '';
                        router.push({
                            path: '/addVersionFile',
                            query: {
                                VersionId: data.ResultData.VersionId
                            }
                        });
                    } else {
                        ElMessage.error('版本添加失败: ' + data.ResultMsg);
                    }
                },
                error: (err) => {
                    ElMessage.error('版本添加失败: ' + err.responseText);
                }
            });
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            router.push('/home');
        },
        handleProfile() {
            // 处理个人资料点击事件
        },
        handleMyCreateMods() {
            router.push('/myCreateMods');
        },
        handleSubscribeMod() {
            router.push('/mySubscribeMods');
        },
        handleCreateMod() {
            router.push('/createMod');
        },
        handleLogout() {
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
    display: flex;
    align-items: center;
    cursor: pointer;
}

.username {
    margin-left: 10px;
    margin-right: 10px;
    font-size: 16px;
}
</style>