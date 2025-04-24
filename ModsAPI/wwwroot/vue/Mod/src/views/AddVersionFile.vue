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
                        <h1 class="h1">上传版本文件</h1>
                        <el-upload class="upload-demo" action="" :auto-upload="false" :before-upload="beforeUpload"
                            :on-change="handleFileChange" :file-list="fileList" accept=".zip">
                            <el-button type="primary">选择文件</el-button>
                            <div slot="tip" class="el-upload__tip">仅支持 .zip 格式文件</div>
                        </el-upload>
                        <el-button type="primary" block @click="submit">提交</el-button>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import head from '../assets/head.jpg';
import $ from 'jquery';

export default {
    name: 'AddVersionFile',
    data() {
        return {
            NickName: '',
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            headurl: head,
            fileList: [], // 存储选中的文件
            VersionId: this.$route.query.VersionId // 从路由参数获取版本 ID
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        this.detectDarkMode();
        if (localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== 'null' && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== null && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== '') { this.headurl = localStorage.getItem('HeadPic' + localStorage.getItem('Mail')); }
    },
    methods: {
        beforeUpload(file) {
            const isZip = file.type === 'application/zip' || file.name.endsWith('.zip');
            if (!isZip) {
                ElMessage.error('仅支持 .zip 格式文件');
            }
            return isZip;
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
        handleFileChange(file, fileList) {
            // 仅存储最后选择的文件
            this.fileList = [file];
        },
        submit() {
            if (!this.fileList.length) {
                ElMessage.error('请选择文件');
                return;
            }

            const formData = new FormData();
            formData.append('VersionId', this.VersionId);
            formData.append('file', this.fileList[0].raw);

            fetch('https://modcat.top:8089/api/Files/UploadMod', {
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    if (data.ResultCode === 200) {
                        ElMessage.success('文件上传成功');
                        this.fileList = []; // 清空文件列表
                        setTimeout(() => {
                            router.push('/home');
                        }, 2000);
                    } else {
                        ElMessage.error('文件上传失败: ' + data.ResultMsg);
                    }
                })
                .catch(error => {
                    console.log('Error:', error);
                    ElMessage.error('文件上传失败');
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

<style>
@media (max-width: 600px) {
    .h1 {
        text-align: center;
        margin-bottom: 24px;
        display: none;
    }

    .el-container {
        padding: 0 10px;
    }

    .el-header {
        flex-direction: column;
        align-items: flex-start;
        padding: 10px;
    }

    .el-main {
        padding: 0;
    }

    .el-col {
        width: 100% !important;
        max-width: 100%;
    }

    .el-card {
        padding: 20px;
        box-shadow: none;
    }

    h1 {
        font-size: 20px;
        text-align: center;
    }

    .el-upload {
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    .el-button {
        font-size: 14px;
        height: 36px;
        margin-bottom: 10px;
    }

    .el-upload__tip {
        display: none;
    }
}

.h1 {
    text-align: center;
    margin-bottom: 24px;
}

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
    border-radius: 4px;
    padding: 4px;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .account-info {
    color: #ffffffa6;
}
</style>

<style>
/* 全局黑暗模式背景和字体颜色 */
body.dark-theme {
    background-color: #121212;
    color: #ffffffa6;
}

/* 黑暗模式下卡片样式 */
body.dark-theme .el-card {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

/* 黑暗模式下标题样式 */
body.dark-theme h1 {
    color: #ffffff;
}

/* 黑暗模式下输入框样式 */
body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

/* 输入框聚焦样式 */
body.dark-theme .el-input__inner:focus {
    border-color: #666666;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

/* 黑暗模式下按钮样式 */
body.dark-theme .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

/* 按钮悬停样式 */
body.dark-theme .el-button:hover {
    background-color: #444444;
    border-color: #555555;
}

/* 黑暗模式下上传组件样式 */
body.dark-theme .el-upload {
    background-color: #1e1e1e;
    border: 1px solid #333333;
    color: #ffffffa6;
}

body.dark-theme .el-upload__tip {
    color: #888888;
}

/* 黑暗模式下下拉菜单样式 */
body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

/* 黑暗模式下下拉菜单项 */
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

/* 黑暗模式下头像信息 */
body.dark-theme .account-info {
    color: #ffffffa6;
}

/* 黑暗模式下上传按钮样式 */
body.dark-theme .el-upload .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-upload .el-button:hover {
    background-color: #444444;
    border-color: #555555;
}

/* 黑暗模式下表单标签 */
body.dark-theme .el-form-item__label {
    color: #ffffffa6;
}

/* 黑暗模式下表单校验错误提示 */
body.dark-theme .el-form-item__error {
    color: #ff6b6b;
}

/* 黑暗模式下链接样式 */
body.dark-theme a {
    color: #4a90e2;
}

body.dark-theme a:hover {
    color: #82b1ff;
}
</style>
```