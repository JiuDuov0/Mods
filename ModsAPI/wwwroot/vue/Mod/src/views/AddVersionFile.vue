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
                                   :on-change="handleFileChange" :file-list="fileList" accept=".zip,.json,.txt,.rar">
                            <el-button type="primary">选择文件</el-button>
                            <div slot="tip" class="el-upload__tip">支持.zip .json .txt .rar格式文件</div>
                        </el-upload>
                        <el-progress v-if="uploadProgress > 0" :percentage="uploadProgress"
                                     status="success"></el-progress>
                        <el-button type="primary" block @click="submit" :disabled="isSubmitting">提交</el-button>
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
    import { fa } from 'element-plus/es/locales.mjs';

    export default {
        name: 'AddVersionFile',
        data() {
            return {
                NickName: '',
                isSubmitting: false,
                GameId: localStorage.getItem('GameId'),
                GameName: localStorage.getItem('GameName'),
                Icon: localStorage.getItem('Icon'),
                Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
                headurl: head,
                fileList: [], // 存储选中的文件
                uploadProgress: 0,
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
                const allowedExtensions = ['.zip', '.json', '.txt', '.rar'];
                const fileName = file.name.toLowerCase();
                const isAllowed = allowedExtensions.some(ext => fileName.endsWith(ext));
                if (!isAllowed) {
                    ElMessage.error('仅支持 .zip .json .txt .rar 格式文件');
                }
                return isAllowed;
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
                // 限制文件大小为10MB
                const maxSize = 50 * 1024 * 1024; // 10MB
                if (this.fileList[0].size > maxSize) {
                    ElMessage.error('文件大小不能超过50MB');
                    return;
                }
                const formData = new FormData();
                formData.append('VersionId', this.VersionId);
                formData.append('file', this.fileList[0].raw);

                // 使用 XMLHttpRequest 实现上传进度
                const xhr = new XMLHttpRequest();
                xhr.open('POST', `${import.meta.env.VITE_API_BASE_URL}/Files/UploadMod`, true);
                xhr.setRequestHeader(
                    'Authorization',
                    'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                );

                xhr.timeout = 300000;

                // 监听上传进度
                xhr.upload.onprogress = (event) => {
                    if (event.lengthComputable) {
                        const progress = Math.round((event.loaded * 100) / event.total);
                        this.uploadProgress = progress; // 更新进度条
                    }
                };

                // 请求完成的回调
                xhr.onload = () => {
                    if (xhr.status === 200) {
                        this.isSubmitting = false;
                        const response = JSON.parse(xhr.responseText);
                        if (response.ResultCode === 200) {
                            ElMessage.success('文件上传成功');
                            this.fileList = []; // 清空文件列表
                            this.uploadProgress = 0; // 重置进度条
                            setTimeout(() => {
                                router.push('/home');
                            }, 2000);
                        } else {
                            ElMessage.error('文件上传失败: ' + response.ResultMsg);
                        }
                    } else {
                        ElMessage.error('文件上传失败');
                    }
                };

                // 请求失败的回调
                xhr.onerror = () => {
                    this.isSubmitting = false;
                    ElMessage.error('文件上传失败');
                    this.uploadProgress = 0; // 重置进度条
                };

                // 发送请求
                xhr.send(formData);
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
</style>
<style>
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
    body.dark-theme {
        background-color: #121212;
        color: #ffffffa6;
    }

        body.dark-theme .el-card {
            background-color: #1e1e1e;
            color: #ffffffa6;
            border-color: #333333;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
        }

        body.dark-theme h1 {
            color: #ffffff;
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
        }

            body.dark-theme .el-button:hover {
                background-color: #444444;
                border-color: #555555;
            }

        body.dark-theme .el-upload {
            background-color: #1e1e1e;
            border: 1px solid #333333;
            color: #ffffffa6;
        }

        body.dark-theme .el-upload__tip {
            color: #888888;
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

        body.dark-theme .account-info {
            color: #ffffffa6;
        }

        body.dark-theme .el-upload .el-button {
            background-color: #333333;
            color: #ffffffa6;
            border-color: #444444;
        }

            body.dark-theme .el-upload .el-button:hover {
                background-color: #444444;
                border-color: #555555;
            }

        body.dark-theme .el-form-item__label {
            color: #ffffffa6;
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

        body.dark-theme .el-upload-list__item:hover {
            background-color: #333333;
            color: #ffffffa6;
        }
</style>