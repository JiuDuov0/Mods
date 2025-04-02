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
                    <input type="text" v-model="loginForm.username" placeholder="请输入登入邮箱" />
                    <input type="password" v-model="loginForm.password" placeholder="请输入密码" />
                    <div style="font-size: 0.8125rem;">没有账户？<a style="color: #0067b8;"
                            @click="handleRegister">立即创建一个！</a></div>
                    <el-button type="primary" block @click="handleLogin">登录</el-button>
                    <!-- <el-button type="primary" block @click="handleRegister">注册</el-button> -->
                </div>
            </el-card>
        </el-col>
    </el-row>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import { sha256 } from 'js-sha256';
import router from '../router/index.js';

export default {
    name: 'Login',
    data() {
        return {
            colSpan: 8,
            loginForm: {
                username: '',
                password: ''
            }
        };
    },
    mounted() {
        this.updateColSpan();
        window.addEventListener('resize', this.updateColSpan);
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
            if (!this.loginForm.username || !this.loginForm.password) {
                ElMessage.error('请填写完整的登录信息');
                return;
            }

            var mail = this.loginForm.username;
            $.ajax({
                url: 'https://modcat.top:8089/api/Login/UserLogin',
                type: "POST",
                data: JSON.stringify({
                    LoginAccount: this.loginForm.username,
                    Password: sha256(this.loginForm.password)
                }),
                contentType: "application/json; charset=utf-8",
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: function (data) {
                    if (data.ResultData == null) {
                        ElMessage.error('登录失败: ' + data.ResultMsg);
                    } else {
                        localStorage.setItem("Mail", mail);
                        localStorage.setItem("NickName" + mail, data.ResultData.NickName);
                        localStorage.setItem("HeadPic" + mail, data.ResultData.HeadPic);
                        localStorage.setItem("Role" + mail, data.ResultData.Role);
                        localStorage.setItem("token" + mail, data.ResultData.Token);
                        localStorage.setItem("refresh_Token" + mail, data.ResultData.Refresh_Token);
                        router.push('/home');
                    }
                },
                error: function (err) {
                    ElMessage.error('登录失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleRegister() {
            router.push('/register');
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