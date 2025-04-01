<template>
    <el-container>
        <el-main>
            <el-row class="head-row">
                <el-col class="head-col">
                    <el-card class="head-el-card">
                        <div class="head-el-card-div">
                            <img src="../assets/Game-Icon-DRG.jpg" alt="Game Icon" class="head-el-card-div-img">
                            <h2>深岩银河</h2>
                            <el-button type="text" @click="handleCreateMod" class="head-el-card-div-el-button">
                                发布Mod
                            </el-button>
                        </div>
                    </el-card>
                </el-col>
            </el-row>
            <el-row class="card-sel">
                <el-col :span="3" class="col-sel">
                    <el-card class="el-card-sel">
                        <el-input v-model="select" placeholder="搜索..." clearable @keyup.enter="handleSearch"></el-input>
                    </el-card>
                </el-col>
                <el-col :span="colSpan">
                    <el-row :gutter="20" ref="modListContainer" id="allwidth">
                        <el-col :span="4" v-for="mod in modList" :key="mod.ModId" name="colsetwidth">
                            <el-card class="el-card-table" name="cardsetwidth">
                                <img referrerPolicy="no-referrer" @click="toModDetail(mod.ModVersion.Mod.ModId)"
                                    :src="mod.PicUrl || defaulturl" style="width: 100%;">
                                <nobr>
                                    <h3>{{ mod.ModVersion.Mod.Name }}</h3>
                                </nobr>

                                <!-- <div style="max-height: 4rem; height: 2rem;">
                                    <el-tag v-for="tag in mod.ModTypeEntities" :key="tag">{{ tag.Types.TypeName
                                    }}</el-tag>
                                </div> -->

                                <!-- <p id="" + mod.ModId>{{ getShortDescription(mod.Description) }}</p> -->
                                <div class="line"></div>
                                <el-button @click="ApproveModVersion(mod.VersionId)" type="primary">批准</el-button>
                                <el-button @click="RefuseModVersion(mod.VersionId)" type="primary">驳回</el-button>
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

                                <el-dropdown-item @click.native="handleapproveModVersion">审核Mod</el-dropdown-item>
                                <el-dropdown-item @click.native="handleroleAuthorization">添加审核人</el-dropdown-item>

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
            Role: localStorage.getItem('Role'),
            defaulturl: drg,
            selectedTypes: [], // 用于存储选中的类型
            select: "", // 用于存储搜索输入内容
            inputTimeout: null, // 用于存储 setTimeout 的引用
            showDialog: false // 控制对话框的显示
        };
    },
    mounted() {
        this.NickName = localStorage.getItem('NickName');
        $('img').attr('referrerPolicy', 'no-referrer');
        if (localStorage.getItem('HeadPic') !== 'null') { this.headurl = localStorage.getItem('HeadPic'); }
        this.fetchModList();
        this.setupIntersectionObserver();
        this.updateColWidth();
        window.addEventListener('resize', this.updateColWidth);
        this.updateColSpan(); // 初始化 colSpan
        window.addEventListener('resize', this.updateColSpan);
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
        fetchModList() {
            $.ajax({
                url: 'https://modcat.top:8089/api/Approve/GetApproveModVersionPageList',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    Skip: this.skip,
                    Take: this.take,
                    Search: this.select
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
                        this.modList = this.modList.concat(data.ResultData);
                        this.skip += this.take; // 更新 skip 值
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
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
        RefuseModVersion(VersionId) {
            $.ajax({
                url: 'https://modcat.top:8089/api/Approve/ApproveMod',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    VersionId: VersionId,
                    Comments: "驳回",
                    Status: "10",
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('审核失败: ' + data.ResultMsg);
                    } else if (data.ResultData === "审核成功") {
                        ElMessage.success('审核成功');
                        this.fetchModList();
                    } else {
                        ElMessage.error('审核失败');
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('请求失败: ' + err.responseJSON.ResultMsg);
                    console.log(err);
                }
            });
        },
        ApproveModVersion(VersionId) {
            $.ajax({
                url: 'https://modcat.top:8089/api/Approve/ApproveMod',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
                },
                data: JSON.stringify({
                    VersionId: VersionId,
                    Comments: "通过",
                    Status: "20",
                }),
                cache: false,
                dataType: "json",
                xhrFields: {
                    withCredentials: true
                },
                async: false,
                success: (data) => {
                    if (data.ResultData == null) {
                        ElMessage.error('审核失败: ' + data.ResultMsg);
                    } else if (data.ResultData === "审核成功") {
                        ElMessage.success('审核成功');
                        this.fetchModList();
                    } else {
                        ElMessage.error('审核失败');
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
        handleapproveModVersion() { router.push('/approveModVersion'); },
        handleroleAuthorization() { router.push('/roleAuthorization'); },
        handleLogout() {
            // 处理退出登录点击事件
            ElMessage.info('退出登录');
            localStorage.removeItem('token');
            localStorage.removeItem('refresh_Token');
            localStorage.removeItem('NickName');
            localStorage.removeItem('HeadPic');
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

<style scoped>
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
    margin-left: 0px;
    margin-top: 5px;
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
    height: 95%;
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
    width: 5rem;
}
</style>