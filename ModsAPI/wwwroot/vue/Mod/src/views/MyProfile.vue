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
                                    <el-button type="primary" @click="showdialog">获取Token</el-button>
                                    <el-button type="primary" @click="copy">复制</el-button>
                                </el-form-item>
                                <el-button type="primary" @click="UserInfoUpdate">修改</el-button>
                            </el-form>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
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
            password: '',
            showStatus: false,
            defaultAvatar: head
        };
    },
    mounted() {
        $('img').attr('referrerPolicy', 'no-referrer');
        this.getUserInfo();
    },
    methods: {
        getUserInfo() {
            this.$axios({
                url: 'https://modcat.top:8089/api/User/GetUserByUserId',
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
                console.error(error);
            });
        },
        showdialog() {
            this.showStatus = true;
        },
        GetToken() {
            this.showStatus = false;
            this.$axios({
                url: 'https://modcat.top:8089/api/Login/CreateToken',
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
                console.error(error);
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
                url: 'https://modcat.top:8089/api/User/UpdateUserInfo',
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
                console.error(error);
            });
        }
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
</style>