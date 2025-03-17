<template>
    <el-container>
        <el-header>
            <div class="account-info">
                <el-avatar src="../src/assets/head.jpg"></el-avatar>
                <el-dropdown>
                    <span class="username" @click="handleDropdownClick">{{ NickName }}</span>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item @click.native="handleHome">主页</el-dropdown-item>
                            <el-dropdown-item @click.native="handleProfile">个人资料</el-dropdown-item>
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
                        <h1 style="text-align: center; margin-bottom: 24px;">发布 Mod</h1>
                        <div>
                            <el-input v-model="modForm.name" placeholder="请输入 Mod 名称" style="margin-bottom: 16px;" />
                            <el-input type="textarea" v-model="modForm.description" placeholder="请输入 Mod 描述"
                                style="margin-bottom: 16px;" />
                            <div>教程为B站视频链接</div>
                            <div>示例:https://player.bilibili.com/player.html?aid={aid}&cid={cid}&page=1</div>
                            <div>在浏览器输入以下链接：</div>
                            <div>https://api.bilibili.com/x/web-interface/view?bvid=BV号</div>
                            <div>获取aid与cid，替换上面的{aid}与{cid} </div>
                            <i class="el-icon-info"></i>
                            <i class="el-icon-info"></i>
                            <i class="el-icon-info"></i>
                            <el-input v-model="modForm.videoUrl" placeholder="请输入视频链接" style="margin-bottom: 16px;" />
                            <el-input v-model="modForm.version" placeholder="请输入版本号" style="margin-bottom: 16px;" />
                            <el-input type="textarea" v-model="modForm.versionDescription" placeholder="请输入版本描述"
                                style="margin-bottom: 16px;" />
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

export default {
    name: 'CreateMod',
    data() {
        return {
            modForm: {
                name: '',
                description: '',
                videoUrl: '',
                version: '',
                versionDescription: '',
                tags: []
            },
            tags: [],
            NickName: ""
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
        this.fetchTags();
    },
    methods: {
        fetchTags() {
            $.ajax({
                url: 'http://43.160.202.17:8099/api/Mod/GetAllModTypes',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('获取失败: ' + data.ResultMsg);
                    } else {
                        this.tags = data.ResultData;
                    }
                },
                error: (err) => {
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleSubmit() {
            if (!this.modForm.name || this.modForm.name.length > 20) {
                ElMessage.error('请输入有效的 Mod 名称（20 字以内）');
                return;
            }
            if (!this.modForm.description) {
                ElMessage.error('请输入 Mod 描述');
                return;
            }
            if (!this.modForm.version || this.modForm.version.length > 10) {
                ElMessage.error('请输入有效的版本号（10 字以内）');
                return;
            }
            if (!this.modForm.versionDescription) {
                ElMessage.error('请输入版本描述');
                return;
            }

            var formData = {
                Name: this.modForm.name,
                Description: this.modForm.description,
                VideoUrl: this.modForm.videoUrl,
                ModVersionEntities: [{
                    VersionNumber: this.modForm.version,
                    Description: this.modForm.versionDescription
                }],
                ModTypeEntities: []
            };
            this.modForm.tags.forEach(tag => {
                formData.ModTypeEntities.push({
                    ModTypeId: tag
                });
            });

            $.ajax({
                url: 'https://127.0.0.1:7114/api/Mod/CreateMod',
                type: "POST",
                data: JSON.stringify(formData),
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('提交失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('提交成功');
                        //todo
                        //router.push('/home');
                    }
                },
                error: (err) => {
                    ElMessage.error('提交失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleReset() {
            this.modForm = {
                name: '',
                description: '',
                videoUrl: '',
                version: '',
                versionDescription: '',
                tags: []
            };
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            // 处理点击事件返回主页
            router.push('/home');
        },
        handleProfile() {
            // 处理个人资料点击事件
        },
        handleMyCreateMods() {
            // 处理我上传的Mod点击事件
            router.push('/myCreateMods');
        },
        handleSubscribeMod() {
            // 处理我订阅的Mod点击事件
            router.push('/mySubscribeMods');
        },
        handleCreateMod() {
            // 处理发布新Mod点击事件
            router.push('/createMod');
        },
        handleLogout() {
            // 处理退出登录点击事件
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
    display: flex;
    align-items: center;
    cursor: pointer;
    /* 添加鼠标指针样式 */
}

.username {
    margin-left: 10px;
    margin-right: 10px;
    font-size: 16px;
}
</style>