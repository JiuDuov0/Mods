<template>
    <div class="change-password-container">
        <h2>修改密码</h2>
        <el-form :model="form" :rules="rules" ref="form" label-width="100px">
            <!-- 邮箱 -->
            <el-form-item label="邮箱" prop="email">
                <el-input v-model="form.email" placeholder="请输入邮箱"></el-input>
            </el-form-item>

            <!-- 验证码 -->
            <el-form-item label="验证码" prop="code">
                <el-input v-model="form.code" placeholder="请输入验证码">
                    <!-- 将按钮放入 el-input 的后置插槽 -->
                    <template #append>
                        <el-button type="primary" @click="sendCode" :disabled="isSendingCode">
                            {{ isSendingCode ? `已发送验证码` : '发送验证码' }}
                        </el-button>
                    </template>
                </el-input>
            </el-form-item>

            <!-- 密码 -->
            <el-form-item label="新密码" prop="password">
                <el-input v-model="form.password" type="password" placeholder="请输入新密码"></el-input>
            </el-form-item>

            <!-- 确认修改 -->
            <el-button style="width: 100%;" type="primary" @click="submitForm">确认修改</el-button>
        </el-form>
    </div>
</template>

<script>
import router from '../router/index.js';
import { sha256 } from 'js-sha256';

export default {
    data() {
        return {
            form: {
                email: '',
                code: '',
                password: '',
            },
            rules: {
                email: [
                    { required: true, message: '请输入邮箱', trigger: 'blur' },
                    { type: 'email', message: '请输入有效的邮箱地址', trigger: ['blur', 'change'] },
                ],
                code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
                password: [{ required: true, message: '请输入新密码', trigger: 'blur' }],
            },
            isSendingCode: false,
            countdown: 60,
        };
    },
    mounted() {
        this.detectDarkMode();
    },
    methods: {
        sendCode() {
            if (!this.form.email) {
                this.$message.error('请先输入邮箱');
                return;
            }
            this.isSendingCode = true;
            this.countdown = 60;

            // 模拟发送验证码
            this.$axios({
                url: 'https://modcat.top:8089/api/Login/SendVerificationCode',
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: {
                    Mail: this.form.email,
                },
            }).then(response => {
                if (response.data.ResultData) { this.$message.success('验证码已发送'); } else { this.$message.error('发送失败: ' + response.data.ResultMsg); }
            }).catch(error => {
            });
        },
        submitForm() {
            this.$axios({
                url: 'https://modcat.top:8089/api/Login/VerifyEmailCodeAndChangePassWord',
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: {
                    Mail: this.form.email,
                    VerificationCode: this.form.code,
                    Password: sha256(this.form.password)
                },
            }).then(response => {
                if (response.data.ResultData) { this.$message.success('修改成功！'); router.push('/'); } else { this.$message.error('修改失败: ' + response.data.ResultMsg); }
            }).catch(error => {
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
        }
    },
};
</script>

<style>
.change-password-container {
    max-width: 400px;
    margin: 50px auto;
    padding: 20px;
    border: 1px solid #ddd;
    border-radius: 8px;
    background-color: #fff;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

h2 {
    text-align: center;
    margin-bottom: 20px;
    color: #333;
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-input-group__append {
    background-color: #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme {
    background-color: #121212;
    color: #ffffffa6;
}

body.dark-theme .change-password-container {
    background-color: #1e1e1e;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

body.dark-theme h2 {
    color: #ffffffa6;
}

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-button {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #2c2c2c;
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

body.dark-theme .el-form-item__label {
    color: #ffffffa6;
}

body.dark-theme .el-form-item__error {
    color: #ff6b6b;
}

body.dark-theme .el-input__inner:focus {
    border-color: #666666;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-button:hover {
    color: #ffffffa6;
}

body.dark-theme a {
    color: #4a90e2;
}

body.dark-theme a:hover {
    color: #82b1ff;
}
</style>