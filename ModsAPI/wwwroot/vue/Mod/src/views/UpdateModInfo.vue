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
            GameId: localStorage.getItem('GameId'),
            GameName: localStorage.getItem('GameName'),
            Icon: localStorage.getItem('Icon'),
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
        this.detectDarkMode();
    },
    methods: {
        fetchTags() {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/GetAllModTypes`,
                data: {
                    GameId: this.GameId
                },
                method: 'POST',
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('获取标签失败: ' + response.data.ResultMsg);
                } else {
                    this.tags = response.data.ResultData;
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        fetchModDetails() {
            const formData = {
                ModId: this.$route.query.ModId
            };

            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/GetModDetailUpdate`,
                method: 'POST',
                data: JSON.stringify(formData),
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('获取 Mod 详情失败: ' + response.data.ResultMsg);
                } else {
                    this.modForm.name = response.data.ResultData.Name;
                    this.modForm.description = response.data.ResultData.Description;
                    this.modForm.VideoUrl = response.data.ResultData.VideoUrl.substring(39, response.data.ResultData.VideoUrl.indexOf('&'));
                    this.modForm.PicUrl = response.data.ResultData.PicUrl;
                    this.modForm.tags = response.data.ResultData.ModTypeEntities.map((type) => type.Types.TypesId);
                    this.modForm.ModDependenceEntities = response.data.ResultData.ModDependenceEntities;
                    this.modForm.ModDependenceEntities.forEach((dependence) => {
                        dependence.ModDependenceId = dependence.ModDependenceId;
                        dependence.ModId = dependence.ModId;
                        dependence.ModName = dependence.DependenceModVersion?.Mod.Name;
                        dependence.VersionId = dependence.DependenceModVersion?.VersionId;
                        dependence.VersionNumber = dependence.DependenceModVersion?.VersionNumber;
                    });
                    //console.log(this.modForm.ModDependenceEntities);
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        fetchMods(query) {
            if (query === null || query === undefined || query === '') {
                return;
            }

            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/ModListPageSearch`,
                method: 'POST',
                data: {
                    Skip: '0',
                    Take: '100',
                    Search: query
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('获取失败: ' + response.data.ResultMsg);
                } else {
                    this.mods = response.data.ResultData;
                    //this.modVersions = data.ResultData.ModVersionEntities;
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
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
            if (this.modForm.tags.length == 0) {
                ElMessage.error('请选择标签');
                return;
            }
            if (this.modForm.PicUrl.length > 200) {
                ElMessage.error('图片链接过长！');
                return;
            }

            const formData = {
                ModId: this.$route.query.ModId,
                Description: this.modForm.description,
                VideoUrl: this.modForm.VideoUrl,
                PicUrl: this.modForm.PicUrl,
                GameId: this.GameId,
                ModTypeEntities: this.modForm.tags.map((tag) => ({ TypesId: tag })),
                ModDependenceEntities: this.modForm.ModDependenceEntities.map((dependence) => ({
                    ModDependenceId: dependence.ModDependenceId,
                    ModId: this.$route.query.ModId,
                    DependenceModVersionId: dependence.VersionId,
                    ModIOURL: dependence.ModIOURL
                }))
            };

            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/UpdateModInfo`,
                method: 'POST',
                data: JSON.stringify(formData),
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('提交失败: ' + response.data.ResultMsg);
                } else {
                    ElMessage.success('提交成功');
                    router.push('/myCreateMods');
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
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
<style>
body {
    background-image: url("https://www.loliapi.com/acg/pc/") !important;
    background-repeat: no-repeat;
    background-size: cover;
    background-position: center center;
    background-attachment: fixed;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled) {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):focus {
    background-color: #444444;
    color: #ffffff;
    outline: none;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
}

body.dark-theme .head-el-card-div-el-button:hover {
    background-color: #444444;
    border-color: #444444;
}

.head-el-card-div-el-button:hover {
    color: white !important;
    background-color: black !important;
    box-shadow: none !important;
}

body.dark-theme .el-input.is-disabled .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border-radius: 4px;
    padding: 4px;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
}

body.dark-theme {
    background-color: #121212;
    color: #ffffffa6;
}

body.dark-theme .el-card {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

body.dark-theme h1 {
    color: #ffffff;
}

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-input__inner:focus {
    border-color: #666666;
}

body.dark-theme .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-button:hover {
    background-color: #444444;
    border-color: #555555;
    color: #ffffffa6;
}

body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled) {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-menu__item:not(.is-disabled):focus {
    background-color: #444444;
    color: #ffffff;
    outline: none;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
}

body.dark-theme .account-info {
    color: #ffffffa6;
}

body.dark-theme .el-form-item__label {
    color: #ffffffa6;
}

body.dark-theme .el-form-item__error {
    color: #ff6b6b;
}

body.dark-theme .el-table {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-table th {
    background-color: #2c2c2c;
    color: #ffffffa6;
}

body.dark-theme .el-table td {
    background-color: #1e1e1e;
    color: #ffffffa6;
}

body.dark-theme .el-pagination__button {
    background-color: #2c2c2c;
    color: #ffffffa6;
}

body.dark-theme .el-pagination__button:hover {
    background-color: #444444;
    color: #ffffff;
}

body.dark-theme .el-tag {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme a {
    color: #4a90e2;
}

body.dark-theme a:hover {
    color: #82b1ff;
}

body.dark-theme .el-textarea__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
    border-radius: 4px;
    padding: 8px;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
}

body.dark-theme .el-textarea__inner:focus {
    background-color: #333333;
    border-color: #666666;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
    outline: none;
}

body.dark-theme .el-textarea__inner[disabled] {
    background-color: #1e1e1e;
    color: #888888;
    border-color: #333333;
    cursor: not-allowed;
}

body.dark-theme .el-select {
    background-color: #2c2c2c;
    color: #ffffffa6;
}

body.dark-theme .el-select-dropdown {
    background-color: #1e1e1e;
    color: #ffffffa6;
}

body.dark-theme .el-select-dropdown__item {
    background-color: #1e1e1e;
    color: #ffffffa6;
}

body.dark-theme .el-select-dropdown__item:hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-select-dropdown__item.selected {
    background-color: #444444;
    color: #ffffff;
    font-weight: bold;
}

body.dark-theme .el-select:focus {
    border-color: #666666;
}

body.dark-theme .el-select.is-disabled {
    background-color: #1e1e1e;
    color: #888888;
}

body.dark-theme .el-select__wrapper {
    background-color: #2c2c2c;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-select__wrapper:focus-within {
    border-color: #666666;
    box-shadow: 0 0 1px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-select__wrapper.is-disabled {
    background-color: #1e1e1e;
    border-color: #333333;
    cursor: not-allowed;
}

body.dark-theme .el-select-dropdown__item.is-hovering {
    background-color: #333333;
    color: #ffffff;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-select__wrapper.is-hovering {
    background-color: #2c2c2c;
    border-color: #2c2c2c;
    box-shadow: 0 0 5px #2c2c2c;
    transition: background-color 0.3s ease, border-color 0.3s ease, box-shadow 0.3s ease;
}

body.dark-theme .el-table--enable-row-hover .el-table__body tr:hover>td.el-table__cell {
    background-color: #333333;
    color: #ffffff;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-table--enable-row-hover .el-table__body tr:hover>td.el-table__cell {
    border-color: #444444;
}
</style>