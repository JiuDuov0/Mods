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
                            <el-select v-model="selectedMod" filterable remote placeholder="请选择 Mod 依赖"
                                :filter-method="fetchMods" @change="fetchModVersions"
                                style="margin-bottom: 16px; width: 100%;">
                                <el-option v-for="mod in mods" :key="mod.ModId" :label="mod.Name"
                                    :value="mod.ModId"></el-option>
                            </el-select>
                            <el-select v-model="selectedModVersion" placeholder="请选择 Mod 依赖版本"
                                style="margin-bottom: 16px; width: 100%;">
                                <el-option v-for="version in modVersions" :key="version.VersionId"
                                    :label="version.VersionNumber" :value="version.VersionId"></el-option>
                            </el-select>
                            <el-input v-model="ModIOURL" placeholder="请输入ModIO依赖URL" style="margin-bottom: 16px;" />
                            <el-button type="primary" @click="addModDependence">添加依赖</el-button>
                            <el-table :data="modForm.ModDependenceEntities" style="width: 100%; margin-top: 16px;">
                                <el-table-column prop="ModName" label="Mod 名称"></el-table-column>
                                <el-table-column prop="VersionNumber" label="版本号"></el-table-column>
                                <el-table-column prop="ModIOURL" label="ModIO依赖"></el-table-column>
                                <el-table-column label="操作">
                                    <template v-slot="scope">
                                        <el-button @click="removeModDependence(scope.$index)" type="text"
                                            size="small">移除</el-button>
                                    </template>
                                </el-table-column>
                            </el-table>
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
                tags: [],
                ModDependenceEntities: []
            },
            mods: [],
            modVersions: [],
            selectedMod: '',
            selectedModVersion: '',
            headurl: head,
            ModIOURL: '',
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            tags: [], // 存储所有可选的 Mod 类型
            NickName: ''
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        if (localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== 'null' && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== null && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== '') { this.headurl = localStorage.getItem('HeadPic' + localStorage.getItem('Mail')); }
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
                    Authorization: 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
                    Authorization: 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
                        this.modForm.ModDependenceEntities = data.ResultData.ModDependenceEntities;
                        this.modForm.ModDependenceEntities.forEach((dependence) => {
                            dependence.ModDependenceId = dependence.ModDependenceId;
                            dependence.ModId = dependence.ModId;
                            dependence.ModName = dependence.DependenceModVersion.Mod.Name;
                            dependence.VersionId = dependence.DependenceModVersion.VersionId;
                            dependence.VersionNumber = dependence.DependenceModVersion.VersionNumber;
                        });
                        console.log(this.modForm.ModDependenceEntities);
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取 Mod 详情失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        fetchMods(query) {
            if (query === null || query === undefined || query === '') {
                return;
            }
            $.ajax({
                url: `https://modcat.top:8089/api/Mod/ModListPageSearch`,
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: JSON.stringify({
                    Skip: '0',
                    Take: '10',
                    Search: query
                }),
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
                        this.mods = data.ResultData;
                        //this.modVersions = data.ResultData.ModVersionEntities;
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        fetchModVersions(modId) {
            const selectedMod = this.mods.find(mod => mod.ModId === this.selectedMod);
            this.modVersions = selectedMod.ModVersionEntities;
        },
        addModDependence() {
            const selectedMod = this.mods.find(mod => mod.ModId === this.selectedMod);
            const selectedModVersion = this.modVersions.find(version => version.VersionId === this.selectedModVersion);
            if (selectedMod && selectedModVersion) {
                this.modForm.ModDependenceEntities.push({
                    ModId: selectedMod.ModId,
                    ModName: selectedMod.Name,
                    VersionId: selectedModVersion.VersionId,
                    VersionNumber: selectedModVersion.VersionNumber
                });
                this.selectedMod = '';
                this.selectedModVersion = '';
            } else if (this.ModIOURL) {
                this.modForm.ModDependenceEntities.push({
                    ModId: null,
                    ModName: 'ModIO 依赖',
                    VersionId: '未知',
                    VersionNumber: null,
                    ModIOURL: this.ModIOURL
                });
                this.ModIOURL = '';
            } else {
                ElMessage.error('请选择有效的 Mod 和 Mod 版本');
            }
        },
        removeModDependence(index) {
            this.modForm.ModDependenceEntities.splice(index, 1);
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
                ModTypeEntities: this.modForm.tags.map((tag) => ({ TypesId: tag })),
                ModDependenceEntities: this.modForm.ModDependenceEntities.map((dependence) => ({
                    ModDependenceId: dependence.ModDependenceId,
                    ModId: this.$route.query.ModId,
                    DependenceModVersionId: dependence.VersionId,
                    ModIOURL: dependence.ModIOURL
                }))
            };

            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/UpdateModInfo',
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json; charset=utf-8',
                headers: {
                    Authorization: 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
        handleProfile() { router.push('/myProfile'); },
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
            localStorage.removeItem('token' + localStorage.getItem('Mail'));
            localStorage.removeItem('refresh_Token' + localStorage.getItem('Mail'));
            localStorage.removeItem('NickName' + localStorage.getItem('Mail'));
            localStorage.removeItem('HeadPic' + localStorage.getItem('Mail'));
            localStorage.removeItem('Role' + localStorage.getItem('Mail'));
            localStorage.removeItem('Mail');
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