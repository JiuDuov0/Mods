<template>
    <el-container>
        <el-main>
            <el-row>
                <el-col :span="24" class="profile-summary">
                    <el-card class="profile-card">
                        <div class="profile-header">
                            <el-avatar :src="user.HeadPic || defaultAvatar" size="large"></el-avatar>
                            <h2>{{ user.NickName }}</h2>
                        </div>
                        <div class="profile-details">
                            <el-form ref="userForm" :model="user" label-width="120px">
                                <el-form-item label="头像URL">
                                    <el-input v-model="user.HeadPic" @input="onHeadPicChange"
                                        @blur="onHeadPicBlur"></el-input>
                                </el-form-item>
                                <el-form-item label="昵称">
                                    <el-input v-model="user.NickName"></el-input>
                                </el-form-item>
                                <el-form-item label="反馈邮箱">
                                    <el-input v-model="user.FeedBackMail"></el-input>
                                </el-form-item>
                                <el-form-item label="Token">
                                    <el-input v-model="user.Token" type="password" readonly></el-input>
                                    <el-button style="margin-top: 1rem;" type="primary"
                                        @click="showdialog">获取Token</el-button>
                                    <el-button style="margin-top: 1rem;" type="primary" @click="copy">复制</el-button>
                                </el-form-item>
                                <el-button type="primary" @click="UserInfoUpdate">修改</el-button>
                            </el-form>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
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

        </el-main>

        <el-dialog title="确认登录信息" v-model="showStatus" width="30%">
            <el-input v-model="user.Mail" readonly></el-input>
            <el-input v-model="password" type="password"></el-input>
            <el-button type="primary" @click="GetToken">确定</el-button>
        </el-dialog>

    </el-container>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import head from '../assets/head.jpg';
import { sha256 } from 'js-sha256';
import router from '../router/index.js';

export default {
    name: 'MyProfile',
    data() {
        return {
            user: {
                HeadPic: '',
                NickName: '',
                FeedBackMail: '',
                Token: ''
            },
            NickName: "",
            headurl: head,
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            GameId: localStorage.getItem('GameId'),
            GameName: localStorage.getItem('GameName'),
            Icon: localStorage.getItem('Icon'),
            password: '',
            showStatus: false,
            defaultAvatar: head
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        if (localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== 'null' && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== null && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== '') { this.headurl = localStorage.getItem('HeadPic' + localStorage.getItem('Mail')); }
        this.getUserInfo();
        this.detectDarkMode();
    },
    methods: {
        getUserInfo() {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/User/GetUserByUserId`,
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                }
            }).then(response => {
                if (response.data.ResultData) {
                    this.user = response.data.ResultData;
                } else {
                    ElMessage.error('失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    this.$router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
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
        showdialog() {
            this.showStatus = true;
        },
        GetToken() {
            this.showStatus = false;
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Login/CreateToken`,
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: {
                    LoginAccount: this.user.Mail,
                    Password: sha256(this.password)
                }
            }).then(response => {
                if (response.data.ResultData) {
                    this.user.Token = response.data.ResultData.Token;
                } else {
                    ElMessage.error('获取用户信息失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    this.$router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        onHeadPicChange() { $('img').attr('referrerPolicy', 'no-referrer'); },
        onHeadPicBlur() { $('img').attr('referrerPolicy', 'no-referrer'); console.log('blur'); },
        copy() {
            navigator.clipboard.writeText(this.user.Token).then(() => {
                ElMessage.success('Token 已复制到剪切板');
            }).catch(err => {
                ElMessage.error('复制失败: ' + err.message);
            });
        },
        UserInfoUpdate() {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/User/UpdateUserInfo`,
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: this.user
            }).then(response => {
                if (response.data.ResultData) {
                    ElMessage.success('修改成功');
                } else {
                    ElMessage.error('获取用户信息失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    this.$router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        handleDropdownClick() { },
        handleHome() { router.push('/home'); },
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
        handleProfile() { router.push('/myProfile'); },
        handleMyCreateMods() { router.push('/myCreateMods'); },
        handleSubscribeMod() { router.push('/mySubscribeMods'); },
        handleCreateMod() { router.push('/createMod'); },
        handleLogout() { ElMessage.info('退出登录'); localStorage.removeItem('token' + localStorage.getItem('Mail')); localStorage.removeItem('refresh_Token' + localStorage.getItem('Mail')); localStorage.removeItem('NickName' + localStorage.getItem('Mail')); localStorage.removeItem('HeadPic' + localStorage.getItem('Mail')); localStorage.removeItem('Role' + localStorage.getItem('Mail')); localStorage.removeItem('Mail'); router.push('/'); },
        toModDetail(ModId) { router.push({ path: '/modDetail', query: { ModId: ModId } }); }
    }
};
</script>

<style scoped>
.profile-summary {
    margin-top: 20px;
}

.profile-header {
    display: flex;
    align-items: center;
}

.profile-header h2 {
    margin-left: 20px;
}

.profile-details {
    margin-top: 20px;
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

body.dark-theme .profile-card {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

body.dark-theme .profile-header {
    background-color: #1e1e1e;
    color: #ffffffa6;
}

body.dark-theme .profile-header h2 {
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

body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-dropdown-item {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-item:hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-item.is-active {
    background-color: #444444;
    color: #ffffff;
    font-weight: bold;
}

body.dark-theme .account-info {
    color: #ffffffa6;
}

body.dark-theme .el-dialog {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
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

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}
</style>