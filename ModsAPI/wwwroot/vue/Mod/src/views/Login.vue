<template>
    <a-row type="flex" justify="center" align="middle" style="height: 100vh;">
        <a-col :span="8">
            <a-card>
                <h1 style="text-align: center; margin-bottom: 24px;">登录</h1>
                <div>
                    <a-input v-model:value="loginForm.username" placeholder="请输入用户名" style="margin-bottom: 16px;" />
                    <a-input-password v-model:value="loginForm.password" placeholder="请输入密码"
                        style="margin-bottom: 16px;" />
                    <a-button type="primary" block @click="handleLogin">登录</a-button>
                </div>
            </a-card>
        </a-col>
    </a-row>
</template>

<script>
import $ from 'jquery';
import { message } from 'ant-design-vue';
import { sha256 } from 'js-sha256'
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
                message.error('请填写完整的登录信息');
                return;
            }

            $.ajax({
                url: this.$url + '/api/Login/UserLogin',
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
                        message.error('登录失败: ' + data.resultMsg);
                    } else {
                        localStorage.setItem("token", data.ResultData.Token);
                        localStorage.setItem("refresh_Token", data.ResultData.Refresh_Token);
                        //location.href = "../../../wwwroot/html/Default/Index.html";
                        router.push('/home');
                    }
                },
                error: function (err) {
                    message.error('登录失败: ' + err);
                    console.log(err);
                }
            });
        }
    }
};
</script>

<style scoped>
.ant-card {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.ant-input,
.ant-input-password {
    margin-bottom: 16px;
}

.ant-btn {
    height: 40px;
    font-size: 16px;
}
</style>