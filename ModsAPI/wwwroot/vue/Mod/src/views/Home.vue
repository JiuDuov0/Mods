<template>
    <el-container>
        <el-main>
            <el-row class="head-row">
                <el-col class="head-col">
                    <el-card class="head-el-card">
                        <div class="head-el-card-div">
                            <img src="../assets/Game-Icon-DRG.jpg" alt="Game Icon" class="head-el-card-div-img">
                            <h2>深岩银河</h2>
                            <el-button type="text" @click="handleDownloadmintcat" class="head-el-card-div-el-button">
                                下载mintcat
                            </el-button>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
            <el-row class="card-sel">
                <el-col :span="3" class="col-sel">
                    <el-card class="el-card-sel">
                        <el-input v-model="select" placeholder="搜索..." clearable @keyup.enter="handleSearch"></el-input>
                        <h3>Mod 类型</h3>
                        <el-checkbox-group v-model="selectedTypes">
                            <div v-for="type in modTypes" :key="type.TypesId" class="checkbox-item">
                                <el-checkbox :label="type.TypesId">
                                    {{ type.TypeName }}
                                </el-checkbox>
                            </div>
                        </el-checkbox-group>
                    </el-card>
                </el-col>
                <el-col :span="colSpan" class="colrightclass">
                    <el-row :gutter="20" ref="modListContainer" id="allwidth">
                        <el-col v-for="mod in modList" :key="mod.ModId" name="colsetwidth">
                            <el-card class="el-card-table" name="cardsetwidth">
                                <img referrerPolicy="no-referrer" @click="toModDetail(mod.ModId)"
                                    :src="mod.PicUrl || defaulturl" style="width: 100%; height: 10rem;">
                                <nobr>
                                    <h3>{{ mod.Name }}</h3>
                                </nobr>
                                <div style="max-height: 4rem; height: 2rem;">
                                    <el-tag v-for="tag in mod.ModTypeEntities" :key="tag">
                                        {{ tag.Types.TypeName }}
                                    </el-tag>
                                </div>
                                <div class="line"></div>
                                <el-button v-if="!mod.IsMySubscribe" @click="UserModSubscribe(mod.ModId)"
                                    type="primary">订阅</el-button>
                                <el-button v-else @click="btnUnsubscribeClick(mod.ModId)"
                                    type="primary">取消订阅</el-button>
                            </el-card>
                        </el-col>
                    </el-row>
                    <div ref="bottomObserver" style="height: 1px;"></div>
                </el-col>
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

                                <el-dropdown-item @click.native="handleCreateMod">发布新Mod</el-dropdown-item>
                                <el-dropdown-item @click.native="handleMyCreateMods">我发布的Mod</el-dropdown-item>
                                <el-dropdown-item @click.native="handleSubscribeMod">我订阅的Mod</el-dropdown-item>
                                <el-dropdown-item @click.native="handleLogout">退出登录</el-dropdown-item>
                            </el-dropdown-menu>
                        </template>
                    </el-dropdown>
                </div>
            </el-row>
        </el-main>
    </el-container>
</template>

<script>
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import head from '../assets/head.jpg';
import drg from '../assets/drg.png';
import { el } from 'element-plus/es/locales.mjs';
import { compile } from 'vue';

export default {
    name: 'Home',
    data() {
        return {
            colSpan: 21,
            skip: 0,
            take: 18,
            modTypes: [],
            modList: [],
            NickName: "",
            headurl: head,
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            defaulturl: drg,
            selectedTypes: [], // 用于存储选中的类型
            select: "", // 用于存储搜索输入内容
            inputTimeout: null, // 用于存储 setTimeout 的引用
            showDialog: false // 控制对话框的显示
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName' + localStorage.getItem('Mail'));
        $('img').attr('referrerPolicy', 'no-referrer');
        if (localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== 'null' && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== null && localStorage.getItem('HeadPic' + localStorage.getItem('Mail')) !== '') { this.headurl = localStorage.getItem('HeadPic' + localStorage.getItem('Mail')); }
        //this.Role = localStorage.getItem('Role');
        this.fetchModTypes();
        this.fetchModList();
        this.setupIntersectionObserver();
        this.updateColWidth();
        window.addEventListener('resize', this.updateColWidth);
        this.updateColSpan(); // 初始化 colSpan
        window.addEventListener('resize', this.updateColSpan);
        this.detectDarkMode();
    },
    watch: {
        selectedTypes() {
            this.skip = 0; // 选中的类型变化时，重置 skip
            this.modList = []; // 清空当前 modList
            this.fetchModList(); // 当选中的类型变化时，重新获取 mod 列表
        }
    },
    beforeDestroy() {
        window.removeEventListener('resize', this.updateColWidth); // 移除监听器
        window.removeEventListener('resize', this.updateColSpan); // 移除监听器
    },
    methods: {
        updateColSpan() {
            const screenWidth = window.innerWidth; // 获取窗口宽度
            if (screenWidth < 1870) {
                this.colSpan = 24; setTimeout(() => {
                    this.updateColWidth();
                }, 100);
            } else { this.colSpan = 21 }
            //this.colSpan =  ? 24 : 21; // 动态设置 colSpan
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
        updateColWidth() {
            const allwidth = document.getElementById('allwidth').offsetWidth; // 获取父容器宽度
            const cardMinWidth = 200; // 卡片的最小宽度
            const gutter = 20; // 列间距
            const columns = Math.floor(allwidth / (cardMinWidth + gutter)); // 计算每行显示的列数
            var colWidth = Math.floor((allwidth - (columns - 1) * gutter) / columns); // 计算每列宽度

            var odd = (allwidth - (columns * colWidth)) / columns; // 计算剩余宽度
            if ((colWidth + odd) * columns >= allwidth) { odd = odd - 1; }
            if (columns === 1) { colWidth = allwidth - 20; odd = 0; }
            // 设置动态宽度
            const colElements = document.getElementsByName('colsetwidth');

            const observer = new MutationObserver(() => {
                colElements.forEach((col) => {
                    col.style.flex = `0 0 ${colWidth + odd}px`; // 设置列宽
                    col.style.maxWidth = `${colWidth + odd}px`;
                });
            });
            observer.observe(document.getElementById('allwidth'), { childList: true, subtree: true });
            colElements.forEach((col) => {
                col.style.flex = `0 0 ${colWidth + odd}px`; // 设置列宽
                col.style.maxWidth = `${colWidth + odd}px`;
            });
        },
        getShortDescription(description) {
            if (description.length > 15) {
                return description.substring(0, 15) + '...';
            }
            return description;
        },
        handleSearch() {
            this.skip = 0; // 搜索时重置 skip
            this.modList = []; // 清空当前 modList
            this.fetchModList(); // 调用 fetchModList 方法重新获取 mod 列表
        },
        fetchModTypes() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/GetAllModTypes',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        if (data.ResultCode == "500") { router.push('/'); }
                        ElMessage.error('获取失败: ' + data.ResultMsg);
                    } else {
                        this.modTypes = data.ResultData;
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    else if (err.status == "403") { ElMessage.error('没有权限访问'); }
                    else if (err.status == "404") { ElMessage.error('请求的资源不存在'); }
                    else if (err.status == "408") { ElMessage.error('请求超时'); }
                    else if (err.status == "429") { ElMessage.error('请求过于频繁'); }
                    else if (err.status == "503") { ElMessage.error('服务不可用'); }
                    else if (err.status == "504") { ElMessage.error('网关超时'); }
                    else if (err.status == "401") { router.push('/'); }
                    else if (err.status == "403") { ElMessage.error('没有权限访问'); }
                    else if (err.status == "404") { ElMessage.error('请求的资源不存在'); }
                    else if (err.status == "408") { ElMessage.error('请求超时'); }
                    else if (err.status == "429") { ElMessage.error('请求过于频繁'); }
                    else if (err.status == "503") { ElMessage.error('服务不可用'); }
                    else if (err.status == "504") { ElMessage.error('网关超时'); }
                    else if (err.status == "500") { router.push('/'); }
                    else { ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg); }
                    console.log(err);
                }
            });
        },
        fetchModList() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Mod/ModListPage',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: JSON.stringify({
                    Skip: this.skip,
                    Take: this.take,
                    Types: this.selectedTypes, // 传递选中的类型
                    Search: this.select // 传递搜索输入内容
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
                        this.modList = this.modList.concat(data.ResultData); // 将新数据附加到 modList
                        this.skip += this.take; // 更新 skip 值
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                },
                complete: () => {
                    setTimeout(() => {
                        this.updateColWidth();
                    }, 100);
                }
            });
            //this.updateColWidth();
        },
        setupIntersectionObserver() {
            const options = {
                root: null,
                rootMargin: '0px',
                threshold: 1.0
            };

            const observer = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        if (this.skip <= this.modList.length) {
                            this.fetchModList();
                        }
                    }
                });
            }, options);

            observer.observe(this.$refs.bottomObserver);
        },
        btnUnsubscribeClick(ModId) {
            // 处理取消订阅按钮点击事件
            $.ajax({
                url: 'https://modcat.top:8089/api/User/UserUnsubscribeMod',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: JSON.stringify({
                    ModId: ModId
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == false || data.ResultData == null) {
                        ElMessage.error('取消订阅失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('取消订阅成功！');
                        this.modList.forEach((item) => {
                            if (item.ModId == ModId) {
                                item.IsMySubscribe = false;
                            }
                        });
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('请求失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        UserModSubscribe(modId) {
            $.ajax({
                url: 'https://modcat.top:8089/api/User/ModSubscribe',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
                },
                data: JSON.stringify({
                    ModId: modId
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('订阅失败: ' + data.ResultMsg);
                    } else {
                        ElMessage.success('订阅成功');
                        this.modList.forEach((item) => {
                            if (item.ModId == modId) {
                                item.IsMySubscribe = true;
                            }
                        });
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('订阅失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        handleDropdownClick() {
            // 处理下拉菜单点击事件
        },
        handleHome() {
            // 处理点击事件返回主页
            router.push('/home');
        },
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
        handleProfile() { router.push('/myProfile'); },
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
        handleDownloadmintcat() {
            router.push('/downloadmintcat');
        },
        handleLogout() {
            // 处理退出登录点击事件
            ElMessage.info('退出登录');
            localStorage.removeItem('token' + localStorage.getItem('Mail'));
            localStorage.removeItem('refresh_Token' + localStorage.getItem('Mail'));
            localStorage.removeItem('NickName' + localStorage.getItem('Mail'));
            localStorage.removeItem('HeadPic' + localStorage.getItem('Mail'));
            localStorage.removeItem('Role' + localStorage.getItem('Mail'));
            localStorage.removeItem('Mail');
            router.push('/');
        },
        toModDetail(ModId) {
            // 处理点击事件跳转到 Mod 详情页
            router.push({
                path: '/modDetail',
                query: {
                    ModId: ModId
                }
            });
        }
    }
};
</script>

<style>
@media (max-width: 1870px) {
    .col-sel {
        display: none;
    }
}

@media (max-width: 600px) {
    .head-row {
        display: none;
    }

    .el-card-table {
        display: contents;
    }
}

.el-card {
    margin-bottom: 20px;
    border-radius: 2%;
    min-width: 200px;
}

.checkbox-item {
    margin-bottom: 10px;
}

.el-button {
    width: 100%;
    color: black;
    background-color: #e4e7ed;
    border-style: solid;
    border-color: #e4e7ed;
}

.el-tag {
    margin-right: 1%;
    margin-bottom: 1%;
    margin-top: 1%;
    background-color: #e4e7ed;
    color: black;
    border-color: #e4e7ed;
}

.line {
    width: 100%;
    height: 1px;
    background-color: #e4e7ed;
    margin-top: 3%;
    margin-bottom: 3%;
}

.el-card-table {
    min-width: 13rem;
    max-width: 16rem;
}

.el-card-sel {
    position: fixed;
    z-index: 500;
    left: 1%;
    width: 11.5%;
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

.card-sel {
    margin-top: 3%;
}

.col-sel {
    margin-right: 1%;
    margin-left: -1%;
}

h3 {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

[name="colsetwidth"] {
    flex: 1 1 auto;
    max-width: 25%;
    min-width: 200px;
}

[name="cardsetwidth"] {
    width: 100%;
    margin-bottom: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.head-row {
    position: fixed;
    z-index: 600;
    left: 0%;
    top: 0;
    width: 101%;
    padding: 0px;
    margin: 0px;
    height: 4rem;
}

.head-col {
    height: 4rem;
}

.head-el-card {
    border-color: white;
    padding: 0px;
    margin: 0px;
    height: 100%;
}

.head-el-card-div {
    display: flex;
    align-items: center;
    margin-top: -1%;
}

.head-el-card-div-img {
    width: 2%;
    height: 2%;
    margin-right: 1%;
    border-radius: 20%;
}

.head-el-card-div-el-button {
    margin-left: auto;
    background-color: black;
    color: white;
    width: 5.2rem;
}

.head-el-card-div-el-button:hover {
    color: white !important;
    background-color: black !important;
    box-shadow: none !important;
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

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-button:hover {
    background-color: #444444;
    border-color: #555555;
}

body.dark-theme .el-tag {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .line {
    background-color: #444444;
}

body.dark-theme .account-info {
    color: #ffffffa6;
}

body.dark-theme .el-dropdown-menu {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
}

body.dark-theme .el-form-item__label {
    color: #ffffffa6;
}

body.dark-theme .el-form-item__error {
    color: #ff6b6b;
}

body.dark-theme .el-input {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme [name="cardsetwidth"] {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.5);
}

body.dark-theme h3 {
    color: #ffffffa6;
}

body.dark-theme .el-avatar {
    border: 1px solid #444444;
}

body.dark-theme .head-row {
    background-color: #1e1e1e;
    border-bottom: 1px solid #333333;
}

body.dark-theme .head-el-card-div-el-button {
    background-color: #333333;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .head-el-card-div-el-button:hover {
    background-color: #444444;
    border-color: #444444;
}

body.dark-theme .el-checkbox {
    color: #ffffffa6;
}

body.dark-theme .el-checkbox__input.is-checked .el-checkbox__inner {
    background-color: #444444;
    border-color: #555555;
}

body.dark-theme .el-checkbox__inner:hover {
    border-color: #666666;
}

body.dark-theme .el-pagination {
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

body.dark-theme .el-dropdown-item {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-radius: 4px;
    padding: 8px 12px;
    transition: background-color 0.3s ease, color 0.3s ease;
}

body.dark-theme .el-dropdown-item:hover {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-dropdown-item.is-active {
    background-color: #444444;
    color: #ffffff;
    font-weight: bold;
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
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border-radius: 4px;
    padding: 4px;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}
</style>