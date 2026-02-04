<template>
    <el-container class="profile-container">
        <el-main>
            <el-row class="profile-grid">
                <!-- 左侧资料卡 -->
                <el-col>
                    <el-card class="profile-card">
                        <div class="profile-card-header">
                            <el-avatar :src="validHeadPic" :size="80" @error="onAvatarError"></el-avatar>
                            <div class="header-text">
                                <h2 class="nick">{{ user.NickName || '未设置昵称' }}</h2>
                                <p class="mail" v-if="user.Mail">{{ user.Mail }}</p>
                            </div>
                        </div>

                        <el-divider content-position="left">基础信息</el-divider>
                        <el-form ref="userForm" :model="user" label-width="110px" size="default" class="profile-form">
                            <el-form-item label="头像URL">
                                <el-input v-model="user.HeadPic" placeholder="输入头像图片地址" clearable
                                    @input="onHeadPicChange" @blur="onHeadPicBlur"></el-input>
                            </el-form-item>

                            <el-form-item label="昵称">
                                <el-input v-model="user.NickName" placeholder="输入昵称" maxlength="30"
                                    show-word-limit></el-input>
                            </el-form-item>

                            <el-form-item label="反馈邮箱">
                                <el-input v-model="user.FeedBackMail" placeholder="用于接收反馈的邮箱" type="email"></el-input>
                            </el-form-item>

                            <el-divider content-position="left">认证信息</el-divider>

                            <el-form-item label="Token">
                                <el-input v-model="user.Token" type="password" :readonly="true" placeholder="未获取 Token"
                                    show-password></el-input>
                                <div class="token-actions">
                                    <el-button @click="showdialog">获取 Token</el-button>
                                    <el-button :disabled="!user.Token" @click="copy">
                                        {{ user.Token ? '复制 Token' : '不可复制' }}
                                    </el-button>
                                    <el-button :disabled="!user.Token" type="primary" @click="openMintCat">
                                        唤起 MintCat
                                    </el-button>
                                </div>
                            </el-form-item>

                            <!-- <el-form-item label="角色" v-if="Role">
                                <el-tag type="info">{{ Role }}</el-tag>
                            </el-form-item> -->

                            <el-divider content-position="left">操作</el-divider>
                            <div class="form-footer">
                                <el-button @click="UserInfoUpdate">保存修改</el-button>
                                <el-button @click="getUserInfo">刷新数据</el-button>
                            </div>
                        </el-form>
                    </el-card>
                </el-col>

            </el-row>

            <!-- 固定账户信息浮层（保留原逻辑） -->
            <div class="account-info">
                <el-avatar :src="headurl"></el-avatar>
                <el-dropdown>
                    <span class="username" @click="handleDropdownClick">{{ NickName }}</span>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item @click.native="handleHome">主页</el-dropdown-item>
                            <el-dropdown-item @click.native="handleProfile">个人资料</el-dropdown-item>
                            <el-dropdown-item v-if="Role === 'Auditors' || Role === 'Developer'"
                                @click.native="handleapproveModVersion">审核 Mod</el-dropdown-item>
                            <el-dropdown-item v-if="Role === 'Developer'"
                                @click.native="handleroleAuthorization">添加审核人</el-dropdown-item>
                            <el-dropdown-item v-if="Role === 'Developer'"
                                @click.native="handleroleStatistics">数据面板</el-dropdown-item>
                            <el-dropdown-item @click.native="handleCreateMod">发布新 Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleMyCreateMods">我发布的 Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleSubscribeMod">我订阅的 Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleLogout">退出登录</el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </el-main>

        <!-- 获取 Token 弹窗 -->
        <el-dialog title="确认登录信息" v-model="showStatus" width="30%">
            <el-form label-width="80px">
                <el-form-item label="账号">
                    <el-input v-model="user.Mail" readonly></el-input>
                </el-form-item>
                <el-form-item label="密码">
                    <el-input v-model="password" type="password" placeholder="请输入登录密码以验证" show-password
                        autocomplete="current-password"></el-input>
                </el-form-item>
                <el-form-item>
                    <el-button type="primary" @click="GetToken">确定</el-button>
                    <el-button @click="showStatus = false">取消</el-button>
                </el-form-item>
            </el-form>
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
                Token: '',
                Mail: ''
            },
            NickName: '',
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
    computed: {
        validHeadPic() {
            return this.user.HeadPic || this.defaultAvatar;
        }
    },
    mounted() {
        document.body.classList.remove('dark-theme');
        this.detectDarkMode();
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        const localHead = localStorage.getItem('HeadPic' + localStorage.getItem('Mail'));
        if (localHead && localHead !== 'null') {
            this.headurl = localHead;
        }
        this.getUserInfo();
        this.detectDarkMode();
    },
    methods: {
        detectDarkMode() {
            const matcher = window.matchMedia('(prefers-color-scheme: dark)');
            const apply = () => { this.isDark = matcher.matches; };
            apply();
            matcher.addEventListener('change', apply);
        },
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
            document.body.classList.toggle('dark-theme', isDarkMode);
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                document.body.classList.toggle('dark-theme', e.matches);
            });
        },
        showdialog() {
            this.password = '';
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
                    ElMessage.success('Token 已获取');
                } else {
                    ElMessage.error('获取 Token 失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    this.$router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        onHeadPicChange() {
            $('img').attr('referrerPolicy', 'no-referrer');
        },
        onHeadPicBlur() {
            $('img').attr('referrerPolicy', 'no-referrer');
        },
        onAvatarError() {
            this.user.HeadPic = '';
        },
        copy() {
            if (!this.user.Token) {
                ElMessage.warning('尚未获取 Token');
                return;
            }
            navigator.clipboard.writeText(this.user.Token).then(() => {
                ElMessage.success('Token 已复制');
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
                    ElMessage.error('修改失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    this.$router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        openMintCat() {
            if (!this.user.Token) {
                ElMessage.warning('尚未获取 Token');
                return;
            }
            const url = `mintcat://oauth/callback?platform=modcat&access_token=${encodeURIComponent(this.user.Token)}`;
            // 优先使用 window.location 触发 deeplink
            window.location.href = url;

            // 可选：降级提示（未安装时）
            setTimeout(() => {
                ElMessage.info('若未唤起，请确认已安装 MintCat 客户端');
            }, 1200);
        },
        handleDropdownClick() { },
        handleHome() { router.push('/home'); },
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
        handleroleStatistics() { router.push('/statistics'); },
        handleProfile() { router.push('/myProfile'); },
        handleMyCreateMods() { router.push('/myCreateMods'); },
        handleSubscribeMod() { router.push('/mySubscribeMods'); },
        handleCreateMod() { router.push('/createMod'); },
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
body {
    background-image: url("https://www.loliapi.com/acg/pc/") !important;
    background-repeat: no-repeat;
    background-size: cover;
    background-position: center center;
    background-attachment: fixed;
}

body.dark-theme {
    background-color: #121212;
    color: #ffffffa6;
}
</style>

<style scoped>
.el-button {
    width: auto;
}

.profile-container {
    padding: 10px;
}

.profile-grid {
    margin-top: 10px;
}

.profile-card-header {
    display: flex;
    align-items: center;
    gap: 16px;
    padding-bottom: 10px;
}

.header-text .nick {
    margin: 0;
    font-size: 22px;
    font-weight: 600;
}

.header-text .mail {
    margin: 2px 0 0;
    font-size: 13px;
    color: #666;
}

.profile-form {
    margin-top: 10px;
}

.token-actions {
    display: flex;
    gap: 10px;
    margin-top: 8px;
    flex-wrap: wrap;
}

.form-footer {
    display: flex;
    gap: 12px;
    margin-top: 10px;
}

.side-card {
    min-height: 100%;
}

.side-card h3 {
    margin-top: 0;
    font-weight: 600;
    font-size: 16px;
    margin-bottom: 12px;
}

.w-btn {
    width: 100%;
    margin-bottom: 8px;
}

.game-info {
    display: flex;
    align-items: center;
    gap: 8px;
}

.game-icon {
    width: 32px;
    height: 32px;
    border-radius: 6px;
    object-fit: cover;
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
    gap: 10px;
}

.username {
    font-size: 14px;
}

@media (max-width: 800px) {
    .account-info {
        display: none;
    }
}
</style>

<style>
body.dark-theme .profile-card,
body.dark-theme .side-card {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, .5);
}

body.dark-theme .header-text .mail {
    color: #999;
}

body.dark-theme .el-input__inner,
body.dark-theme .el-textarea__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
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

body.dark-theme .el-tag {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-divider__text {
    color: #ffffffa6;
    background-color: #353535;
}

body.dark-theme .el-divider--horizontal {
    border-color: #494949;
}

body.dark-theme .el-dialog {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .account-info {
    color: #ffffffa6;
}

body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-input__count {
    background-color: #2c2c2c;
}

body.dark-theme .el-input__count-inner {
    background-color: #2c2c2c !important;
}
</style>