<template>
    <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
        <el-col :span="8">
            <el-card>
                <h1 style="text-align: center; margin-bottom: 24px;">注册</h1>
                <div>
                    <el-input v-model="registerForm.mail" placeholder="请输入邮箱" style="margin-bottom: 16px;" />
                    <el-input v-model="registerForm.nickname" placeholder="请输入昵称" style="margin-bottom: 16px;" />
                    <el-input type="password" v-model="registerForm.password" placeholder="请输入密码"
                        style="margin-bottom: 16px;" />
                    <el-button type="primary" block @click="handleRegister">注册</el-button>
                    <el-button type="primary" block @click="handleLogin">登录</el-button>
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
    name: 'Register',
    data() {
        return {
            registerForm: {
                mail: '',
                nickname: '',
                password: ''
            }
        };
    },
    methods: {
        handleLogin() {
            router.push('/');
        },
        handleRegister() {
            if (!this.registerForm.mail || !this.registerForm.nickname || !this.registerForm.password) {
                ElMessage.error('请填写完整的注册信息');
                return;
            }

            if (!this.validateEmail(this.registerForm.mail)) {
                ElMessage.error('请输入有效的邮箱地址');
                return;
            }
            if (this.registerForm.nickname.length > 6) {
                ElMessage.error('昵称不得多于6个字');
                return;
            }
            $.ajax({
                url: 'https://modcat.top:8089/api/Login/UserRegister',
                type: "POST",
                data: JSON.stringify({
                    LoginAccount: this.registerForm.mail,
                    NickName: this.registerForm.nickname,
                    Password: sha256(this.registerForm.password)
                }),
                contentType: "application/json; charset=utf-8",
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('注册失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('注册成功');
                        localStorage.setItem("NickName", data.ResultData.NickName);
                        localStorage.setItem("token", data.ResultData.Token);
                        localStorage.setItem("refresh_Token", data.ResultData.Refresh_Token);
                        router.push('/home');
                    }
                },
                error: (err) => {
                    ElMessage.error('注册失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        validateEmail(email) {
            const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return re.test(email);
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