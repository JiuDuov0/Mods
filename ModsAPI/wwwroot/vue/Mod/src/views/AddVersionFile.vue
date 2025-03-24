<template>
    <el-container>
        <el-header>
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
                                @click.native="handleProfile">添加审核人</el-dropdown-item>

                            <el-dropdown-item @click.native="handleCreateMod">发布新Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleMyCreateMods">我发布的Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleSubscribeMod">我订阅的Mod</el-dropdown-item>
                            <el-dropdown-item @click.native="handleLogout">退出登录</el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </el-header>
        <el-main>
            <el-row type="flex" justify="center" align="middle" style="height: 100vh;">
                <el-col :span="12">
                    <el-card>
                        <h1 style="text-align: center; margin-bottom: 24px;">上传版本文件</h1>
                        <el-upload class="upload-demo" action="" :auto-upload="false" :before-upload="beforeUpload"
                            :on-change="handleFileChange" :file-list="fileList" accept=".zip">
                            <el-button type="primary">选择文件</el-button>
                            <div slot="tip" class="el-upload__tip">仅支持 .zip 格式文件</div>
                        </el-upload>
                        <el-button type="primary" block @click="submit">提交</el-button>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import head from '../assets/head.jpg';

export default {
    name: 'AddVersionFile',
    data() {
        return {
            NickName: '',
            Role: localStorage.getItem('Role'),
            headurl: head,
            fileList: [], // 存储选中的文件
            VersionId: this.$route.query.VersionId // 从路由参数获取版本 ID
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
    },
    methods: {
        beforeUpload(file) {
            const isZip = file.type === 'application/zip' || file.name.endsWith('.zip');
            if (!isZip) {
                ElMessage.error('仅支持 .zip 格式文件');
            }
            return isZip;
        },
        handleFileChange(file, fileList) {
            // 仅存储最后选择的文件
            this.fileList = [file];
        },
        submit() {
            if (!this.fileList.length) {
                ElMessage.error('请选择文件');
                return;
            }

            const formData = new FormData();
            formData.append('VersionId', this.VersionId);
            formData.append('file', this.fileList[0].raw);

            fetch('https://modcat.top:8089/api/Files/UploadMod', {
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    if (data.ResultCode === 200) {
                        ElMessage.success('文件上传成功');
                        this.fileList = []; // 清空文件列表
                        setTimeout(() => {
                            router.push('/home');
                        }, 2000);
                    } else {
                        ElMessage.error('文件上传失败: ' + data.ResultMsg);
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    ElMessage.error('文件上传失败');
                });
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            router.push('/home');
        },
        handleProfile() {
            // 处理个人资料点击事件
        },
        handleMyCreateMods() {
            router.push('/myCreateMods');
        },
        handleSubscribeMod() {
            router.push('/mySubscribeMods');
        },
        handleCreateMod() {
            router.push('/createMod');
        },
        handleLogout() {
            ElMessage.info('退出登录');
            localStorage.removeItem('token');
            localStorage.removeItem('refresh_Token');
            localStorage.removeItem('NickName');
            router.push('/');
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
    width: 100%;
    height: 40px;
    font-size: 16px;
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