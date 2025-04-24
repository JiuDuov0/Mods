<template>
    <el-container>
        <el-header>
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
                                @click.native="handleroleAuthorization">添加审核人</el-dropdown-item>

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
import head from '../assets/head.jpg';

export default {
    name: 'AddNewVersion',
    data() {
        return {
            NickName: '',
            headurl: head,
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            ModId: this.$route.query.ModId, // 从路由参数获取 Mod ID
            versionForm: {
                version: '',
                description: ''
            }
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        this.detectDarkMode();
        if (localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== 'null' && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== null && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== '') { this.headurl = localStorage.getItem('HeadPic' + localStorage.getItem('Mail')); }
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

            this.$axios({
                url: 'https://modcat.top:8089/api/Mod/ModAddVersion',
                method: 'POST',
                data: {
                    ModId: this.ModId,
                    VersionNumber: this.versionForm.version,
                    Description: this.versionForm.description
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultCode === 200) {
                    ElMessage.success('版本添加成功');
                    this.versionForm.version = '';
                    this.versionForm.description = '';
                    router.push({
                        path: '/addVersionFile',
                        query: {
                            VersionId: response.data.ResultData.VersionId
                        }
                    });
                } else {
                    ElMessage.error('版本添加失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });



            // $.ajax({
            //     url: 'https://modcat.top:8089/api/Mod/ModAddVersion',
            //     type: 'POST',
            //     contentType: 'application/json; charset=utf-8',
            //     headers: {
            //         'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
            //     },
            //     data: JSON.stringify({
            //         ModId: this.ModId,
            //         VersionNumber: this.versionForm.version,
            //         Description: this.versionForm.description
            //     }),
            //     success: (data) => {
            //         if (data.ResultCode === 200) {
            //             ElMessage.success('版本添加成功');
            //             this.versionForm.version = '';
            //             this.versionForm.description = '';
            //             router.push({
            //                 path: '/addVersionFile',
            //                 query: {
            //                     VersionId: data.ResultData.VersionId
            //                 }
            //             });
            //         } else {
            //             ElMessage.error('版本添加失败: ' + data.ResultMsg);
            //         }
            //     },
            //     error: (err) => {
            //         if (err.status == "401") { router.push('/'); }
            //         ElMessage.error('版本添加失败: ' + err.responseText);
            //     }
            // });
        },
        detectDarkMode() {
            const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (isDarkMode) {
                document.body.classList.add('dark-theme'); // 添加夜间主题样式
            } else {
                document.body.classList.remove('dark-theme'); // 移除夜间主题样式
            }

            // 监听主题变化
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                if (e.matches) {
                    document.body.classList.add('dark-theme');
                } else {
                    document.body.classList.remove('dark-theme');
                }
            });
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            router.push('/home');
        },
        handleProfile() { router.push('/myProfile'); },
        handleMyCreateMods() {
            router.push('/myCreateMods');
        },
        handleSubscribeMod() {
            router.push('/mySubscribeMods');
        },
        handleCreateMod() {
            router.push('/createMod');
        },
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
        handleLogout() {
            ElMessage.info('退出登录');
            localStorage.removeItem('token' + localStorage.getItem('Mail'));
            localStorage.removeItem('refresh_Token' + localStorage.getItem('Mail'));
            localStorage.removeItem('NickName' + localStorage.getItem('Mail'));
            localStorage.removeItem('HeadPic' + localStorage.getItem('Mail'));
            localStorage.removeItem('Role' + localStorage.getItem('Mail'));
            localStorage.removeItem('Mail');
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
    left: 20px;
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

<style>
body.dark-theme {
    background-color: #121212;
    color: #ffffffa6;
}

body.dark-theme .el-card {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.5);
}

body.dark-theme h1 {
    color: #ffffff;
    text-align: center;
    margin-bottom: 24px;
}

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-input__inner:focus {
    border-color: #666666;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
    transition: background-color 0.3s ease, border-color 0.3s ease;
}

body.dark-theme .el-button:hover {
    background-color: #444444;
    border-color: #555555;
}

body.dark-theme .account-info {
    color: #ffffffa6;
    padding: 10px;
    border-radius: 8px;
}

body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled) {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):focus {
    background-color: #444444;
    color: #ffffff;
    outline: none;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-form-item__error {
    color: #ff6b6b;
}

body.dark-theme a {
    color: #4a90e2;
}

body.dark-theme a:hover {
    color: #82b1ff;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled) {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):focus {
    background-color: #444444;
    color: #ffffff;
    outline: none;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .head-el-card-div-el-button:hover {
    background-color: #444444;
    border-color: #444444;
}

.head-el-card-div-el-button:hover {
    color: white !important;
    background-color: black !important;
    box-shadow: none !important;
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .account-info {
    color: #ffffffa6;
}

body.dark-theme .el-textarea__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
    border-radius: 4px;
    padding: 8px;
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
}

body.dark-theme .el-textarea__inner:focus {
    background-color: #333333;
    border-color: #666666;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
    outline: none;
}

body.dark-theme .el-textarea__inner[disabled] {
    background-color: #1e1e1e;
    color: #888888;
    border-color: #333333;
    cursor: not-allowed;
}
</style>