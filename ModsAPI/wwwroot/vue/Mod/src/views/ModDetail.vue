<template>
    <el-container>
        <el-main>
            <el-row :gutter="20">
                <el-col :span="1">
                    <el-icon class="fullicon" @click="goBack">
                        <Back />
                    </el-icon>
                </el-col>
                <el-col :span="23">
                    <h2>{{ Name }}</h2>
                </el-col>
                <el-col :span="colSpan">
                    <iframe v-if="GameId !== 'drgchcode'" class="myiframe" width="100%" height="700rem" :src="videoUrl"
                        frameborder="0" allowfullscreen></iframe>

                    <el-card v-if="GameId === 'drgchcode'" style="max-height: 40rem;">
                        <div class="code-preview">
                            <h3>代码预览
                                <el-button type="primary" size="mini" @click="copyToClipboard" class="copy-button">
                                    <el-icon>
                                        <CopyDocument />
                                    </el-icon>
                                    复制
                                </el-button>
                            </h3>
                            <div id="monaco-editor" style="height: 30rem;"></div>
                        </div>
                    </el-card>

                    <el-card v-if="ModDependenceEntities.length > 0" style="margin-top: 20px;">
                        <h3>前置依赖</h3>
                        <ul>
                            <li v-for="dependence in ModDependenceEntities" :key="dependence.ModDependenceId"
                                @click="goToModDetail(dependence.DependenceModVersion.ModId)">
                                <template
                                    v-if="dependence.DependenceModVersionId !== '未知' && dependence.DependenceModVersionId !== null">
                                    {{ dependence.DependenceModVersion.Mod.Name }} - {{
                                        dependence.DependenceModVersion.VersionNumber }}
                                </template>
                                <template v-else>
                                    <a :href="dependence.ModIOURL" target="_blank" rel="noopener noreferrer">
                                        {{ dependence.ModIOURL }}
                                    </a>
                                </template>
                            </li>
                        </ul>
                    </el-card>
                    <el-card style="margin-top: 20px;">
                        <h3>Mod描述</h3>
                        <div id="text"></div>
                        <!-- {{ description }} -->
                    </el-card>
                </el-col>
                <el-col :span="5" class="sticky-subscribe">
                    <el-button type="primary" block @click="handleSubscribe(ModId)" v-if="!isSubscribed">订阅</el-button>
                    <el-button type="danger" block @click="handleUnsubscribe(ModId)" v-else>取消订阅</el-button>
                    <el-card style="margin-top: 20px;">
                        <h3>Mod 信息</h3>
                        <p @click="handleProfile(modAuthorId)" style="cursor: pointer;">作者: {{ modAuthor }}</p>
                        <p>下载次数: {{ downloads }}</p>
                        <p>创建时间: {{ createdAt }}</p>
                        <p>标签: <el-tag v-for="tag in tags" :key="tag">{{ tag }}</el-tag></p>
                        <p>
                            <el-rate v-model="AVGPoint" disabled show-score text-color="#ff9900"></el-rate>
                        </p>
                    </el-card>
                    <el-card style="margin-top: 20px;cursor: pointer;" @click="showVersionDetails">
                        <h3>最新版本</h3>
                        <p>版本号: {{ latestVersion.version }}</p>
                        <p>版本描述: <span v-html="formatDescription(latestVersion.description)"></span></p>
                        <p>更新时间: {{ latestVersion.CreatedAt }}</p>
                        <div style="text-align: center;">选择版本下载</div>
                        <!-- <el-button type="primary" block @click="downloadLatestVersion(ModId)">下载</el-button> -->
                    </el-card>
                    <el-dialog title="版本详细信息" v-model="versionDialogVisible" width="80%">
                        <el-table :data="versionDetails" style="width: 100%">
                            <el-table-column prop="VersionNumber" label="版本号" width="150"></el-table-column>
                            <el-table-column prop="Description" label="描述">
                                <template #default="scope">
                                    <div style="text-align: left;" v-html="formatDescription(scope.row.Description)">
                                    </div>
                                </template>
                            </el-table-column>
                            <el-table-column prop="CreatedAt" label="更新时间" width="200"></el-table-column>
                            <el-table-column prop="FilesId" label="操作" width="200">
                                <template #default="scope">
                                    <el-button v-if="GameId === 'drgchcode'" type="primary"
                                        @click="handlePreview(scope.row.FilesId)">
                                        预览
                                    </el-button>
                                    <el-button type="primary"
                                        @click="handleDownload(scope.row.FilesId, scope.row.VersionNumber)">
                                        下载
                                    </el-button>
                                </template>
                            </el-table-column>
                        </el-table>
                        <span slot="footer" class="dialog-footer">
                            <el-button @click="versionDialogVisible = false">关闭</el-button>
                        </span>
                    </el-dialog>
                </el-col>
            </el-row>
        </el-main>

        <!-- 评分悬浮窗 -->
        <div class="rating-float" @click="toggleRatingWindow">
            <el-icon>
                <Star />
            </el-icon>
            <span>评分</span>
        </div>

        <!-- 评分窗口 -->
        <el-dialog title="评分" v-model="ratingWindowVisible" class="el-dialog-rating">
            <div style="text-align: center;">
                <el-rate v-model="rating" :max="5"></el-rate>
                <p style="margin-top: 10px;">当前评分: {{ rating }}</p>
            </div>
            <span slot="footer" class="dialog-footer">
                <!-- <el-button @click="ratingWindowVisible = false">取消</el-button> -->
                <el-button type="primary" @click="submitRating">提交</el-button>
            </span>
        </el-dialog>

        <el-dialog title="下载进度" v-model="showStatus" width="30%">
            <div style="text-align: center;">
                <p style="margin-top: 10px;">下载进度: {{ progress }}%</p>
            </div>
        </el-dialog>

        <el-dialog title="预览进度" v-model="showPreviewStatus" width="30%">
            <div style="text-align: center;">
                <p style="margin-top: 10px;">预览进度: {{ progress }}%</p>
            </div>
        </el-dialog>
    </el-container>
</template>
<script>
import { CopyDocument } from '@element-plus/icons-vue';
import { watch } from 'vue'
import $ from 'jquery';
import { ElMessage } from 'element-plus';
import router from '../router/index.js';
import { da, el } from 'element-plus/es/locale/index.mjs';
import drg from '../assets/drg.png';

export default {
    name: 'ModDetail',
    components: {
        CopyDocument,
    },
    data() {
        return {
            colSpan: 19,
            Name: '',
            videoUrl: '',
            description: '',
            isSubscribed: false,
            modAuthor: '',
            modAuthorId: '',
            downloads: "",
            subscribers: "",
            createdAt: "",
            tags: [],
            progress: 0,
            showStatus: false,
            showPreviewStatus: false,
            Role: localStorage.getItem('Role' + localStorage.getItem('Mail')),
            GameId: localStorage.getItem('GameId'),
            GameName: localStorage.getItem('GameName'),
            Icon: localStorage.getItem('Icon'),
            latestVersion: {
                version: '',
                description: '',
                size: '',
                FilesId: ''
            },
            ModId: this.$route.query.ModId,
            ModDependenceEntities: [],
            entity: null,
            versionDialogVisible: false, // 控制弹窗显示
            versionDetails: [],
            rating: 5, // 当前评分
            AVGPoint: 0.0, // 平均评分
            pointentity: null,
            codeContent: {},
            ratingWindowVisible: false // 评分窗口是否可见
        };
    },
    mounted() {
        this.modDetail();
        $("#text").html(this.description);
        this.updateColSpan();
        window.addEventListener('resize', this.updateColSpan);
        const iframe = document.querySelector('.myiframe');
        if (iframe) {
            iframe.onload = () => {
                const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                const img = iframeDoc.querySelector('img');
                if (img) {
                    img.style.width = '100%';
                    img.style.height = '100%';
                }
            };
        }
        this.detectDarkMode();
        if (this.GameId === 'drgchcode') {
            this.initMonacoEditor(); // 初始化 Monaco Editor
        }
    },
    beforeDestroy() {
        window.removeEventListener('resize', this.updateColSpan);
    },
    methods: {
        updateColSpan() {
            const screenWidth = window.innerWidth;
            if (screenWidth < 1000) {
                this.colSpan = 24; setTimeout(() => {
                }, 100);
            } else { this.colSpan = 19 }
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
        modDetail() {
            // 获取 Mod 详情逻辑
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/ModDetail`,
                method: 'POST',
                data: {
                    ModId: this.$route.query.ModId
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('获取失败: ' + response.data.ResultMsg);
                } else {
                    // 赋值
                    this.entity = response.data.ResultData;
                    this.Name = response.data.ResultData.Name;
                    this.createdAt = response.data.ResultData.CreatedAt;
                    this.videoUrl = response.data.ResultData.VideoUrl;
                    if (this.videoUrl === null || this.videoUrl === "") {
                        if (response.data.ResultData.PicUrl === null || response.data.ResultData.PicUrl === "") {
                            this.videoUrl = drg;
                        } else {
                            this.videoUrl = '/pic.html';
                            localStorage.setItem("imgPath", response.data.ResultData.PicUrl);
                            const iframeWidth = document.querySelector('.myiframe').offsetWidth;
                            const iframeHeight = document.querySelector('.myiframe').offsetHeight;
                            localStorage.setItem("imgwidth", document.querySelector('.myiframe').offsetWidth - 20);
                            localStorage.setItem("imgheight", document.querySelector('.myiframe').offsetHeight - 20);
                            //this.videoUrl = drg;
                        }

                    }
                    this.description = response.data.ResultData.Description;
                    $("#text").html(this.description);
                    this.modAuthor = response.data.ResultData.CreatorEntity.NickName;
                    this.modAuthorId = response.data.ResultData.CreatorEntity.UserId;
                    this.downloads = response.data.ResultData.DownloadCount;
                    if (response.data.ResultData.ModTypeEntities !== null) {
                        response.data.ResultData.ModTypeEntities.forEach(element => {
                            this.tags.push(element.Types.TypeName);
                        });
                    }
                    this.ModDependenceEntities = response.data.ResultData.ModDependenceEntities;
                    this.isSubscribed = response.data.ResultData.IsMySubscribe;
                    this.latestVersion.version = response.data.ResultData.ModVersionEntities[0].VersionNumber;
                    this.latestVersion.description = response.data.ResultData.ModVersionEntities[0].Description;
                    this.latestVersion.FilesId = response.data.ResultData.ModVersionEntities[0].FilesId;
                    this.latestVersion.CreatedAt = response.data.ResultData.ModVersionEntities[0].CreatedAt;
                    if (response.data.ResultData.AVGPoint != null) {
                        this.AVGPoint = response.data.ResultData.AVGPoint;
                    }
                    if (this.GameId === 'drgchcode') {
                        this.handlePreview(response.data.ResultData.ModVersionEntities[0].FilesId);
                    }
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });

            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/GetModPointByModId`,
                method: 'POST',
                data: {
                    ModId: this.$route.query.ModId
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    //暂未评分
                } else {
                    // 赋值
                    this.rating = response.data.ResultData.Point;
                    this.pointentity = response.data.ResultData;
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        showVersionDetails() {
            if (this.entity && this.entity.ModVersionEntities) {
                this.versionDetails = this.entity.ModVersionEntities; // 加载版本数据
                this.versionDialogVisible = true; // 显示弹窗
            } else {
                ElMessage.error('暂无版本详细信息');
            }
        },
        handleSubscribe(ModId) {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/User/ModSubscribe`,
                method: 'POST',
                data: {
                    ModId: ModId
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == null) {
                    ElMessage.error('订阅失败: ' + response.data.ResultMsg);
                } else {
                    ElMessage.success('订阅成功');
                    this.isSubscribed = true;
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        handleUnsubscribe(ModId) {
            // 处理取消订阅逻辑
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/User/UserUnsubscribeMod`,
                method: 'POST',
                data: {
                    ModId: ModId
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultData == false || response.data.ResultData == null) {
                    ElMessage.error('取消订阅失败: ' + response.data.ResultMsg);
                } else {
                    ElMessage.success('取消订阅成功！');
                    this.isSubscribed = false;
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        downloadLatestVersion(ModId) {
            // 处理下载最新版本逻辑
        },
        toggleRatingWindow() {
            this.ratingWindowVisible = !this.ratingWindowVisible;
        },
        submitRating() {
            if (this.pointentity === null) {
                this.addPoint();
            } else {
                this.updatePoint();
            }
        },
        addPoint() {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/AddModPoint`,
                method: 'POST',
                data: {
                    ModId: this.ModId,
                    Point: this.rating
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultCode === 200) {
                    ElMessage.success('评分提交成功');
                    this.ratingWindowVisible = false;
                } else {
                    ElMessage.error('评分提交失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        updatePoint() {
            this.$axios({
                url: `${import.meta.env.VITE_API_BASE_URL}/Mod/UpdateModPoint`,
                method: 'POST',
                data: {
                    ModPointId: this.pointentity.ModPointId,
                    UserId: this.pointentity.UserId,
                    ModId: this.ModId,
                    Point: this.rating
                },
                contentType: "application/json; charset=utf-8",
                responseType: 'json'
            }).then(response => {
                if (response.data.ResultCode === 200) {
                    ElMessage.success('评分提交成功');
                    this.ratingWindowVisible = false;
                } else {
                    ElMessage.error('评分提交失败: ' + response.data.ResultMsg);
                }
            }).catch(error => {
                ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                console.log(error);
            });
        },
        handleProfile(UserId) {
            router.push({
                path: '/profile',
                query: {
                    UserId: UserId
                }
            });
        },
        async handleDownload(FileId, VersionNumber) {
            if (!FileId) {
                ElMessage.error('文件 ID 不存在，无法下载');
                return;
            }
            this.progress = 0;
            this.showStatus = true;
            this.versionDialogVisible = false;
            try {
                const response = await this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/Files/DownloadFile`,
                    method: 'POST',
                    timeout: 300000,
                    headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail')) },
                    data: { FileId: FileId },
                    responseType: 'blob',
                    onDownloadProgress: (progressEvent) => {
                        this.progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                    }
                });

                const url = window.URL.createObjectURL(new Blob([response.data]));
                const link = document.createElement('a');
                link.href = url;

                const contentType = response.headers['content-type'];
                let extension = '';

                // 根据 Content-Type 设置扩展名
                if (contentType === 'application/zip' || contentType === 'application/x-zip-compressed') {
                    extension = '.zip';
                } else if (contentType === 'application/json') {
                    extension = '.json';
                } else if (contentType === 'text/plain') {
                    extension = '.txt';
                } else if (contentType === 'application/octet-stream') {
                    extension = '.bin'; // 二进制文件
                } else if (contentType === 'application/x-7z-compressed') {
                    extension = '.7z'; // 7z 压缩文件
                } else if (contentType === 'application/x-rar-compressed') {
                    extension = '.rar'; // RAR 压缩文件
                } else if (contentType === 'application/x-tar') {
                    extension = '.tar'; // TAR 压缩文件
                } else if (contentType === 'application/x-gzip') {
                    extension = '.gz'; // GZ 压缩文件
                } else {
                    extension = ''; // 如果类型未知，不添加扩展名
                }

                var fileName = this.entity.Name + VersionNumber + extension;
                link.setAttribute('download', fileName); // 设置下载文件的名称
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                this.showStatus = false;
                this.progress = 0;
            }
            catch (error) {
                ElMessage.error('下载文件失败:' + error);
                console.log('下载文件失败:', error);
            }
        },
        async handlePreview(FileId) {
            if (!FileId) {
                ElMessage.error('文件 ID 不存在，无法预览');
                return;
            }
            this.progress = 0;
            this.showPreviewStatus = true;
            this.versionDialogVisible = false;
            try {
                const response = await this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/Files/DownloadFile`,
                    method: 'POST',
                    timeout: 300000,
                    headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail')) },
                    data: { FileId: FileId },
                    responseType: 'blob',
                    onDownloadProgress: (progressEvent) => {
                        this.progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                    }
                });

                const contentType = response.headers['content-type'];

                if (contentType === 'application/json' || contentType === 'text/plain') {
                    const reader = new FileReader();
                    reader.onload = () => {
                        try {
                            // 将文件内容解析为 JSON
                            const jsonContent = JSON.parse(reader.result);
                            this.codeContent = jsonContent; // 将 JSON 数据赋值给 codeContent
                            this.initMonacoEditor(); // 更新 Monaco Editor 内容
                        } catch (error) {
                            console.error('JSON 解析失败:', error);
                            ElMessage.error('文件内容不是有效的 JSON 格式');
                        }
                    };
                    reader.onerror = (error) => {
                        console.error('文件读取失败:', error);
                    };
                    reader.readAsText(response.data);
                }
                this.showPreviewStatus = false;
                this.progress = 0;
            } catch (error) {
                this.showPreviewStatus = false;
                this.progress = 0;
                ElMessage.error('预览文件失败:' + error);
                console.log('预览文件失败:', error);
            }
        },
        copyToClipboard() {
            if (!this.codeContent || Object.keys(this.codeContent).length === 0) {
                ElMessage.warning('没有可复制的内容');
                return;
            }

            // 将 JSON 数据格式化为字符串
            const contentToCopy = JSON.stringify(this.codeContent, null, 2);

            if (navigator.clipboard && navigator.clipboard.writeText) {
                // 使用 Clipboard API
                navigator.clipboard.writeText(contentToCopy)
                    .then(() => {
                        ElMessage.success('代码已复制到剪切板');
                    })
                    .catch((error) => {
                        console.error('复制失败:', error);
                        ElMessage.error('复制失败，请重试');
                    });
            } else {
                // 回退到传统方法
                const textarea = document.createElement('textarea');
                textarea.value = contentToCopy;
                document.body.appendChild(textarea);
                textarea.select();
                try {
                    document.execCommand('copy');
                    ElMessage.success('代码已复制到剪切板');
                } catch (error) {
                    console.error('复制失败:', error);
                    ElMessage.error('复制失败，请重试');
                }
                document.body.removeChild(textarea);
            }
        },
        async initMonacoEditor() {
            const editorContainer = document.getElementById('monaco-editor');
            if (editorContainer) {
                // 检查是否已经存在编辑器实例
                if (editorContainer.__monacoEditorInstance) {
                    editorContainer.__monacoEditorInstance.dispose(); // 销毁已有实例
                }

                // 配置 Monaco Editor 的 CDN 路径
                window.require.config({
                    paths: {
                        vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.34.1/min/vs', // CDN 路径
                    },
                });

                // 加载 Monaco Editor
                window.require(['vs/editor/editor.main'], (monaco) => {
                    // 检测是否是黑暗模式
                    const isDarkMode = document.body.classList.contains('dark-theme');
                    const theme = isDarkMode ? 'vs-dark' : 'vs'; // 根据模式设置主题

                    const editorInstance = monaco.editor.create(editorContainer, {
                        value: JSON.stringify(this.codeContent, null, 2), // 显示 JSON 数据
                        language: 'json',
                        theme: theme, // 动态设置主题
                        //readOnly: true,
                    });

                    // 将编辑器实例存储在容器上，防止重复初始化
                    editorContainer.__monacoEditorInstance = editorInstance;
                });
            } else {
                console.error('Monaco Editor 容器未找到');
            }
        },
        goToModDetail(ModId) {
            if (!ModId) {
                return;
            }
            router.push({
                path: '/modDetail',
                query: {
                    ModId: ModId
                }
            }).then(() => {
                window.location.reload();
            });
        },
        goBack() {
            router.go(-1);
            setTimeout(() => {
                window.location.reload();
            }, 100);
        },
        formatDescription(desc) {
            if (!desc) return '';
            return desc.replace(/(<\/?br\s*\/?>|\n)/gi, '<br>');
        }
    }
};
</script>
<style>
@media (max-width: 1000px) {
    .sticky-subscribe {
        display: none !important;
    }
}

@media (max-width: 500px) {
    .el-dialog-rating {
        width: 10rem !important;
    }

    .sticky-subscribe {
        display: none !important;
    }
}

.el-card {
    margin-bottom: 20px;
}

.el-tag {
    margin-left: 4px;
    background-color: #e4e7ed;
    color: black;
    border-color: #e4e7ed;
}

.el-button {
    width: 100%;
    color: black;
    background-color: #e4e7ed;
    border-style: solid;
    border-color: #e4e7ed;
}

.el-button+.el-button {
    margin-left: 0px;
    margin-top: 0.2rem;
}

.code-preview {
    padding: 20px;
    border-radius: 5px;
    margin-top: 20px;
}

.code-preview pre {
    padding: 10px;
    border-radius: 5px;
    overflow: auto;
    white-space: pre;
    word-wrap: normal;
    max-height: 350px;
}

.code-preview h3 {
    margin-bottom: 10px;
}

.copy-button {
    width: 5rem;
    border: none;
    cursor: pointer;
}


.rating-float {
    position: fixed;
    bottom: 20px;
    right: 20px;
    background-color: #409eff;
    color: white;
    padding: 10px 20px;
    border-radius: 50px;
    cursor: pointer;
    display: flex;
    align-items: center;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

.el-dialog-rating {
    width: 10%;
}

.rating-float:hover {
    background-color: #66b1ff;
}

.rating-float span {
    margin-left: 10px;
    font-size: 16px;
}

.el-dialog {
    z-index: 2000 !important;
}

.sticky-subscribe {
    position: fixed;
    z-index: 500;
    right: 0.3%;
    width: 35%;
}

.fullicon {
    width: 100%;
    height: 100%;
    cursor: pointer;
}

.myiframe {}

.myiframe img {
    width: 100%;
    height: 100%;
}

body.dark-theme .el-table {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
}

body.dark-theme .el-table th {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
    font-weight: bold;
    text-align: center;
}

body.dark-theme .el-table td {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    text-align: center;
}

body.dark-theme .el-table__row:hover>td {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-table__row.is-selected>td {
    background-color: #444444;
    color: #ffffff;
    font-weight: bold;
}

body.dark-theme .el-table--border::before {
    border-color: #444444
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

body.dark-theme .head-el-card-div-el-button:hover {
    background-color: #444444;
    border-color: #444444;
}

.head-el-card-div-el-button:hover {
    color: white !important;
    background-color: black !important;
    box-shadow: none !important;
}

body.dark-theme .el-input__wrapper {
    background-color: #2c2c2c;
    border: 1px solid #2c2c2c;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .account-info {
    color: #ffffffa6;
}
</style>
<style>
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

body.dark-theme .el-table {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
}

body.dark-theme .el-table th {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
    font-weight: bold;
    text-align: center;
}

body.dark-theme .el-table td {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    text-align: center;
}

body.dark-theme .el-table__row:hover>td {
    background-color: #333333;
    color: #ffffff;
}

body.dark-theme .el-table__row.is-selected>td {
    background-color: #444444;
    color: #ffffff;
    font-weight: bold;
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

body.dark-theme .el-input__inner {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-input__inner:focus {
    border-color: #666666;
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
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
    box-shadow: 0 0 5px rgba(255, 255, 255, 0.3);
}

body.dark-theme .el-rate {
    color: #ff9900;
}

body.dark-theme .el-rate .el-rate__item:hover {
    color: #ffd700;
}

body.dark-theme .el-tag {
    background-color: #2c2c2c;
    color: #ffffffa6;
    border-color: #444444;
}

body.dark-theme .el-dialog {
    background-color: #1e1e1e;
    color: #ffffffa6;
    border-color: #333333;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

body.dark-theme .account-info {
    color: #ffffffa6;
    border: 1px solid #333333;
}

body.dark-theme .rating-float {
    background-color: #409eff;
    color: white;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

body.dark-theme .rating-float:hover {
    background-color: #66b1ff;
}

body.dark-theme .el-pagination__button {
    background-color: #2c2c2c;
    color: #ffffffa6;
}

body.dark-theme .el-pagination__button:hover {
    background-color: #444444;
    color: #ffffff;
}

body.dark-theme .el-dialog__title {
    color: #ffffff;
}

body.dark-theme .code-preview {
    color: #ffffffa6;
}

body.dark-theme .code-preview pre::-webkit-scrollbar {
    width: 8px;
    height: 8px;
}

body.dark-theme .code-preview pre::-webkit-scrollbar-thumb {
    background-color: #555555;
    border-radius: 4px;
}

body.dark-theme .code-preview pre::-webkit-scrollbar-thumb:hover {
    background-color: #777777;
}

body.dark-theme .code-preview pre::-webkit-scrollbar-track {
    background-color: #1e1e1e;
    border-radius: 4px;
}

body.dark-theme .code-preview pre::-webkit-scrollbar-corner {
    background-color: #1e1e1e;
}
</style>