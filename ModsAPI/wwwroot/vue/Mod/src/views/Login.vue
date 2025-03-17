<template>
    <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
        <el-col :span="8">
            <el-card>
                <h1 style="text-align: center; margin-bottom: 24px;">登录</h1>
                <div>
                    <el-input v-model="loginForm.username" placeholder="请输入用户名" style="margin-bottom: 16px;" />
                    <el-input type="password" v-model="loginForm.password" placeholder="请输入密码"
                        style="margin-bottom: 16px;" />
                    <el-button type="primary" block @click="handleLogin">登录</el-button>
                    <el-button type="primary" block @click="handleRegister">注册</el-button>
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
            loginForm: {
                username: '',
                password: ''
            }
        };
    },
    methods: {
        handleLogin() {
            if (!this.loginForm.username || !this.loginForm.password) {
                ElMessage.error('请填写完整的登录信息');
                return;
            }

            $.ajax({
                url: 'http://43.160.202.17:8099/api/Login/UserLogin',
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
                        localStorage.setItem("NickName", data.ResultData.NickName);
                        localStorage.setItem("token", data.ResultData.Token);
                        localStorage.setItem("refresh_Token", data.ResultData.Refresh_Token);
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
.el-card {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.el-input {
    margin-bottom: 16px;
}

.el-button {
    height: 40px;
    font-size: 16px;
}
</style>