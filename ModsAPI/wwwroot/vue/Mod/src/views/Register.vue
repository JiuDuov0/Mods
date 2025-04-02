<template>
    <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
        <el-col :span="colSpan">
            <el-card>
                <div>
                    <img src="../assets/logo.png" class="img-logo" />
                    <span class="span-logo">Modcat</span>
                </div>
                <h1 style="margin-bottom: 24px;font-size: 24px;">注册</h1>
                <div>
                    <input type="text" v-model="registerForm.mail" placeholder="请输入邮箱" />
                    <input type="text" v-model="registerForm.nickname" placeholder="请输入昵称" />
                    <input type="password" v-model="registerForm.password" placeholder="请输入密码" />
                    <div style="font-size: 0.8125rem;">已有账号？<a style="color: #0067b8;" @click="handleLogin">返回登录</a>
                    </div>
                    <el-button type="primary" block @click="handleRegister">注册</el-button>
                    <!-- <el-button type="primary" block @click="handleLogin">登录</el-button> -->
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
            colSpan: 8,
            registerForm: {
                mail: '',
                nickname: '',
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
            var mail = this.registerForm.mail;
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
                        localStorage.setItem("Mail", mail);
                        localStorage.setItem("NickName" + mail, data.ResultData.NickName);
                        localStorage.setItem("token" + mail, data.ResultData.Token);
                        localStorage.setItem("refresh_Token" + mail, data.ResultData.Refresh_Token);
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