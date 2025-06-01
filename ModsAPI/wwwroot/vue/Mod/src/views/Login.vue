<template>
    <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
        <el-col :span="colSpan">
            <el-card>
                <div>
                    <img src="../assets/logo.png" class="img-logo" />
                    <span class="span-logo">Modcat</span>
                </div>
                <h1 style="margin-bottom: 24px;font-size: 24px;">登录</h1>
                <div>
                    <input type="text" v-model="loginForm.mail" placeholder="请输入登入邮箱" />
                    <input type="password" v-model="loginForm.password" placeholder="请输入密码" />
                    <div style="font-size: 0.8125rem;">没有账户？<a style="color: #0067b8;"
                            @click="handleRegister">立即创建一个！</a><a style="color: #0067b8;"
                            @click="handleChangePassword">忘记密码</a></div>
                    <el-button type="primary" block @click="handleLogin">登录</el-button>
                    <!-- <el-button type="primary" block @click="handleRegister">注册</el-button> -->
                </div>
            </el-card>
        </el-col>
    </el-row>
</template>

<script>
import { ElMessage } from 'element-plus';
import { sha256 } from 'js-sha256';
import router from '../router/index.js';
import { el } from 'element-plus/es/locales.mjs';

export default {
    name: 'Login',
    data() {
        return {
            colSpan: 8,
            loginForm: {
                mail: '',
                password: ''
            }
        };
    },
    mounted() {
        this.updateColSpan();
        window.addEventListener('resize', this.updateColSpan);
        this.detectDarkMode();
    },
    beforeDestroy() {
        window.removeEventListener('resize', this.updateColSpan);
    },
    methods: {
        updateColSpan() {
            const screenWidth = window.innerWidth;
            this.colSpan = screenWidth < 600 ? 24 : 8;
        },
        handleLogin() {
            if (!this.loginForm.mail || !this.loginForm.password) {
                ElMessage.error('请填写完整的登录信息');
                return;
            }

            var mail = this.loginForm.mail;
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Login/UserLogin`,
                method: 'POST',
                data: {
                    LoginAccount: this.loginForm.mail,
                    Password: sha256(this.loginForm.password)
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('登录失败: ' + response.data.ResultMsg);
                } else {
                    localStorage.setItem("Mail", mail);
                    localStorage.setItem("NickName" + mail, response.data.ResultData.NickName);
                    localStorage.setItem("HeadPic" + mail, response.data.ResultData.HeadPic);
                    localStorage.setItem("Role" + mail, response.data.ResultData.Role);
                    localStorage.setItem("token" + mail, response.data.ResultData.Token);
                    localStorage.setItem("refresh_Token" + mail, response.data.ResultData.Refresh_Token);
                    setTimeout(() => {
                        if (localStorage.getItem("GameId")) { router.push('/home'); } else { router.push('/game'); }
                    }, 100);
                }
            }).catch(error => {
                if (error.response && error.response.status === 401) {
                    router.push('/');
                }
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        handleChangePassword() { router.push('/changePassword'); },
        handleRegister() {
            router.push('/register');
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
        }
    }
};
</script>

<style scoped>
@media (max-width: 600px) {
    .el-row {
        padding: 0 10px;
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
    }

    .el-input {
        font-size: 14px;
    }

    .el-button {
        font-size: 14px;
        height: 36px;
        width: 100%;
        margin: 8px 0;
    }
}

.el-card {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.el-input {
    margin-bottom: 16px;
}

.el-button {
    height: 40px;
    font-size: 16px;
    margin-top: 2rem;
    margin-bottom: 1rem;
    float: right;
    border-radius: 0 !important;
}

input {
    border-top: none !important;
    border-left: none !important;
    border-right: none !important;
    border-bottom: 1px solid rgb(102, 102, 102) !important;
    box-shadow: none !important;
    background-color: transparent !important;
    outline: none;
    margin-bottom: 16px;
    width: 98%;
    height: 1.5rem;
}

.img-logo {
    width: 1rem;
    height: 1rem;
}

.span-logo {
    font-size: 25px;
    color: rgb(102, 102, 102);
    margin-left: 0.5rem;
    margin-bottom: 1rem;
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
    border-color: #1e1e1e;
}

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6 !important;
    border-color: #444444;
}

body.dark-theme .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .span-logo {
    color: #ffffffa6;
}

input:-webkit-autofill {
    background-color: transparent !important;
    color: inherit !important;
    box-shadow: 0 0 0px 1000px transparent inset !important;
    -webkit-text-fill-color: inherit !important;
    transition: background-color 5000s ease-in-out 0s;
}

body.dark-theme input:-webkit-autofill {
    background-color: #2c2c2c !important;
    -webkit-text-fill-color: #ffffffa6 !important;
    box-shadow: 0 0 0px 1000px #2c2c2c inset !important;
}

body.dark-theme input {
    color: #ffffffa6;
}
</style>