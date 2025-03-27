<template>
    <el-container>
        <el-main>
            <el-row
                style="position: fixed;z-index: 600;left: 0%;top:0;width: 101%;padding: 0px;margin: 0px;height: 4rem;">
                <el-col style="height: 4rem;">
                    <el-card style="border-color: white;padding: 0px;margin: 0px; height: 100%;">
                        <div style="display: flex; align-items: center; margin-top: -1%;">
                            <img src="../assets/Game-Icon-DRG.jpg" alt="Game Icon"
                                style="width: 2%; height: 2%; margin-right: 1%;border-radius:20%;">
                            <h2>深岩银河</h2>
                            <el-button type="text" @click="handleCreateMod"
                                style="margin-left: auto; background-color: black; color: white;width: 7%;">
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
                <el-col :span="21">
                    <el-row :gutter="20" ref="modListContainer">
                        <el-col :span="4" v-for="mod in modList" :key="mod.ModId">
                            <el-card class="el-card-table">
                                <img referrerPolicy="no-referrer" @click="toModDetail(mod.ModId)"
                                    :src="mod.PicUrl || defaulturl" style="width: 100%;height: 10rem;">
                                <nobr>
                                    <h3>{{ mod.Name }}</h3>
                                </nobr>

                                <div style="max-height: 4rem; height: 2rem;">
                                    <el-tag v-for="tag in mod.ModTypeEntities" :key="tag">{{ tag.Types.TypeName
                                    }}</el-tag>
                                </div>

                                <!-- <p id="" + mod.ModId>{{ getShortDescription(mod.Description) }}</p> -->
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

export default {
    name: 'Home',
    data() {
        return {
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
        //this.Role = localStorage.getItem('Role');
        this.fetchModTypes();
        this.fetchModList();
        this.setupIntersectionObserver();
    },
    watch: {
        selectedTypes() {
            this.skip = 0; // 选中的类型变化时，重置 skip
            this.modList = []; // 清空当前 modList
            this.fetchModList(); // 当选中的类型变化时，重新获取 mod 列表
        }
    },
    methods: {
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
                        this.modTypes = data.ResultData;
                    }
                },
                error: (err) => {
                    if (err.status == "401") { router.push('/'); }
                    ElMessage.error('获取失败: ' + err.responseJSON.ResultMsg);
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
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
        btnUnsubscribeClick(ModId) {
            // 处理取消订阅按钮点击事件
            $.ajax({
                url: 'https://modcat.top:8089/api/User/UserUnsubscribeMod',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
                    'Authorization': 'Bearer ' + localStorage.getItem('token')
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
        handleLogout() {
            // 处理退出登录点击事件
            ElMessage.info('退出登录');
            localStorage.removeItem('token');
            localStorage.removeItem('refresh_Token');
            localStorage.removeItem('NickName');
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
</style>