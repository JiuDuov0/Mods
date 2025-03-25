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
                                @click.native="handleroleAuthorization">添加审核人</el-dropdown-item>

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
                        <h1 style="text-align: center; margin-bottom: 24px;">更新 Mod 信息</h1>
                        <div>
                            <el-input v-model="modForm.name" placeholder="请输入 Mod 名称" style="margin-bottom: 16px;"
                                disabled />
                            <el-input type="textarea" v-model="modForm.description" placeholder="请输入 Mod 描述"
                                style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.VideoUrl" placeholder="请输入BV号" style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.PicUrl" placeholder="请输入封面URL" style="margin-bottom: 16px;" />
                            <el-select v-model="modForm.tags" multiple placeholder="请选择标签"
                                style="margin-bottom: 16px; width: 100%;">
                                <el-option v-for="tag in tags" :key="tag.TypesId" :label="tag.TypeName"
                                    :value="tag.TypesId"></el-option>
                            </el-select>
                            <el-button type="primary" block @click="handleSubmit">提交</el-button>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import { th } from 'element-plus/es/locale/index.mjs';
import head from '../assets/head.jpg';

export default {
    name: 'UpdateModInfo',
    data() {
        return {
            modForm: {
                name: '', // Mod 名称（不可编辑）
                PicUrl: '', // Mod 封面
                description: '', // Mod 描述
                VideoUrl: '', // Mod 视频链接
                tags: [] // Mod 类型（多选框）
            },
            headurl: head,
            Role: localStorage.getItem('Role'),
            tags: [], // 存储所有可选的 Mod 类型
            NickName: ''
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
        this.fetchTags();
        this.fetchModDetails();
    },
    methods: {
        fetchTags() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/GetAllModTypes',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                headers: {
                    Authorization: 'Bearer ' + localStorage.getItem('token')
                },
                cache: false,
                dataType: 'json',
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('获取标签失败: ' + data.ResultMsg);
                    } else {
                        this.tags = data.ResultData;
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取标签失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        fetchModDetails() {
            const formData = {
                ModId: this.$route.query.ModId
            };
            $.ajax({
                url: `https://modcat.top:8089/api/Mod/GetModDetailUpdate`,
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json; charset=utf-8',
                headers: {
                    Authorization: 'Bearer ' + localStorage.getItem('token')
                },
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('获取 Mod 详情失败: ' + data.ResultMsg);
                    } else {
                        this.modForm.name = data.ResultData.Name;
                        this.modForm.description = data.ResultData.Description;
                        this.modForm.VideoUrl = data.ResultData.VideoUrl.substring(39, data.ResultData.VideoUrl.indexOf('&'));
                        this.modForm.PicUrl = data.ResultData.PicUrl;
                        this.modForm.tags = data.ResultData.ModTypeEntities.map((type) => type.Types.TypesId);
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取 Mod 详情失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleSubmit() {
            if (!this.modForm.description) {
                ElMessage.error('请输入 Mod 描述');
                return;
            }

            const formData = {
                ModId: this.$route.query.ModId,
                Description: this.modForm.description,
                VideoUrl: this.modForm.VideoUrl,
                PicUrl: this.modForm.PicUrl,
                ModTypeEntities: this.modForm.tags.map((tag) => ({ TypesId: tag }))
            };

            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/UpdateModInfo',
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json; charset=utf-8',
                headers: {
                    Authorization: 'Bearer ' + localStorage.getItem('token')
                },
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('提交失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('提交成功');
                        router.push('/myCreateMods');
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('提交失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
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
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
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