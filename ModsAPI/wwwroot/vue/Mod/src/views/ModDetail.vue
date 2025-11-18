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
                            <h3>
                                代码预览
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
                                <template v-if="dependence.DependenceModVersionId !== '未知' && dependence.DependenceModVersionId !== null">
                                    {{ dependence.DependenceModVersion.Mod.Name }} - {{ dependence.DependenceModVersion.VersionNumber }}
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
                    </el-card>
                    <el-dialog title="版本详细信息" v-model="versionDialogVisible" width="80%">
                        <el-table :data="versionDetails" style="width: 100%">
                            <el-table-column prop="VersionNumber" label="版本号" width="150"></el-table-column>
                            <el-table-column prop="Description" label="描述">
                                <template #default="scope">
                                    <div style="text-align: left;" v-html="formatDescription(scope.row.Description)"></div>
                                </template>
                            </el-table-column>
                            <el-table-column prop="CreatedAt" label="更新时间" width="200"></el-table-column>
                            <el-table-column prop="FilesId" label="操作" width="260">
                                <template #default="scope">
                                    <el-button v-if="GameId === 'drgchcode'" type="primary"
                                               @click="handlePreview(scope.row.FilesId)">预览</el-button>
                                    <el-button type="primary"
                                               @click="handleDownload(scope.row.FilesId, scope.row.VersionNumber)">下载</el-button>
                                    <el-button v-if="hasResume(scope.row.FilesId)"
                                               type="warning"
                                               @click="restartDownload(scope.row.FilesId, scope.row.VersionNumber)">重新开始</el-button>
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

        <div class="rating-float" @click="toggleRatingWindow">
            <el-icon><Star /></el-icon>
            <span>评分</span>
        </div>

        <el-dialog title="评分" v-model="ratingWindowVisible" class="el-dialog-rating">
            <div style="text-align: center;">
                <el-rate v-model="rating" :max="5"></el-rate>
                <p style="margin-top: 10px;">当前评分: {{ rating }}</p>
            </div>
            <span slot="footer" class="dialog-footer">
                <el-button type="primary" @click="submitRating">提交</el-button>
            </span>
        </el-dialog>

        <el-dialog title="下载进度" v-model="showStatus" width="30%">
            <div style="text-align: center;">
                <p style="margin-top: 10px;">下载进度: {{ progress }}%</p>
                <el-progress :percentage="progress" :status="isDownloadTimeout ? 'exception' : (progress===100 ? 'success':'')"></el-progress>
                <p v-if="isDownloadTimeout" style="color:#f56c6c;margin-top:10px;">下载超时，可重试。</p>
                <el-button v-if="isDownloadTimeout" type="warning" @click="retryDownload">重试</el-button>
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
    import $ from 'jquery';
    import { ElMessage } from 'element-plus';
    import router from '../router/index.js';
    import drg from '../assets/drg.png';

    export default {
        name: 'ModDetail',
        components: { CopyDocument },
        data() {
            return {
                debugDownload: false,
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
                latestVersion: { version: '', description: '', size: '', FilesId: '' },
                ModId: this.$route.query.ModId,
                ModDependenceEntities: [],
                entity: null,
                versionDialogVisible: false,
                versionDetails: [],
                rating: 5,
                AVGPoint: 0.0,
                pointentity: null,
                codeContent: {},
                ratingWindowVisible: false,
                // 新增
                isDownloadTimeout: false,
                currentDownloadFileId: null,
                currentDownloadVersionNumber: null
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
                this.initMonacoEditor();
            }
        },
        beforeDestroy() {
            window.removeEventListener('resize', this.updateColSpan);
        },
        methods: {
            updateColSpan() {
                const screenWidth = window.innerWidth;
                this.colSpan = screenWidth < 1000 ? 24 : 19;
            },
            detectDarkMode() {
                const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
                document.body.classList.toggle('dark-theme', isDarkMode);
                window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                    document.body.classList.toggle('dark-theme', e.matches);
                });
            },
            modDetail() {
                this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/Mod/ModDetail`,
                    method: 'POST',
                    data: { ModId: this.$route.query.ModId },
                    contentType: "application/json; charset=utf-8",
                    responseType: 'json'
                }).then(response => {
                    if (!response.data.ResultData) {
                        ElMessage.error('获取失败: ' + response.data.ResultMsg);
                        return;
                    }
                    this.entity = response.data.ResultData;
                    this.Name = response.data.ResultData.Name;
                    this.createdAt = response.data.ResultData.CreatedAt;
                    this.videoUrl = response.data.ResultData.VideoUrl;
                    if (!this.videoUrl) {
                        if (!response.data.ResultData.PicUrl) {
                            this.videoUrl = drg;
                        } else {
                            this.videoUrl = '/pic.html';
                            localStorage.setItem("imgPath", response.data.ResultData.PicUrl);
                            localStorage.setItem("imgwidth", document.querySelector('.myiframe')?.offsetWidth - 20 || 0);
                            localStorage.setItem("imgheight", document.querySelector('.myiframe')?.offsetHeight - 20 || 0);
                        }
                    }
                    this.description = response.data.ResultData.Description;
                    $("#text").html(this.description);
                    this.modAuthor = response.data.ResultData.CreatorEntity.NickName;
                    this.modAuthorId = response.data.ResultData.CreatorEntity.UserId;
                    this.downloads = response.data.ResultData.DownloadCount;
                    if (response.data.ResultData.ModTypeEntities) {
                        response.data.ResultData.ModTypeEntities.forEach(e => this.tags.push(e.Types.TypeName));
                    }
                    this.ModDependenceEntities = response.data.ResultData.ModDependenceEntities;
                    this.isSubscribed = response.data.ResultData.IsMySubscribe;
                    const firstVersion = response.data.ResultData.ModVersionEntities[0];
                    this.latestVersion.version = firstVersion.VersionNumber;
                    this.latestVersion.description = firstVersion.Description;
                    this.latestVersion.FilesId = firstVersion.FilesId;
                    this.latestVersion.CreatedAt = firstVersion.CreatedAt;
                    if (response.data.ResultData.AVGPoint != null) this.AVGPoint = response.data.ResultData.AVGPoint;
                    if (this.GameId === 'drgchcode') {
                        this.handlePreview(firstVersion.FilesId);
                    }
                }).catch(error => {
                    ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                });

                this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/Mod/GetModPointByModId`,
                    method: 'POST',
                    data: { ModId: this.$route.query.ModId },
                    contentType: "application/json; charset=utf-8",
                    responseType: 'json'
                }).then(response => {
                    if (response.data.ResultData) {
                        this.rating = response.data.ResultData.Point;
                        this.pointentity = response.data.ResultData;
                    }
                }).catch(error => {
                    ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
                });
            },
            showVersionDetails() {
                if (this.entity?.ModVersionEntities) {
                    this.versionDetails = this.entity.ModVersionEntities;
                    this.versionDialogVisible = true;
                } else {
                    ElMessage.error('暂无版本详细信息');
                }
            },
            handleSubscribe(ModId) {
                this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/User/ModSubscribe`,
                    method: 'POST',
                    data: { ModId },
                    contentType: "application/json; charset=utf-8",
                    responseType: 'json'
                }).then(r => {
                    if (!r.data.ResultData) {
                        ElMessage.error('订阅失败: ' + r.data.ResultMsg);
                    } else {
                        ElMessage.success('订阅成功');
                        this.isSubscribed = true;
                    }
                }).catch(e => ElMessage.error('请求失败: ' + (e.response?.data?.ResultMsg || e.message)));
            },
            handleUnsubscribe(ModId) {
                this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/User/UserUnsubscribeMod`,
                    method: 'POST',
                    data: { ModId },
                    contentType: "application/json; charset=utf-8",
                    responseType: 'json'
                }).then(r => {
                    if (!r.data.ResultData) {
                        ElMessage.error('取消订阅失败: ' + r.data.ResultMsg);
                    } else {
                        ElMessage.success('取消订阅成功！');
                        this.isSubscribed = false;
                    }
                }).catch(e => ElMessage.error('请求失败: ' + (e.response?.data?.ResultMsg || e.message)));
            },
            toggleRatingWindow() { this.ratingWindowVisible = !this.ratingWindowVisible; },
            submitRating() { this.pointentity === null ? this.addPoint() : this.updatePoint(); },
            addPoint() {
                this.$axios({
                    url: `${import.meta.env.VITE_API_BASE_URL}/Mod/AddModPoint`,
                    method: 'POST',
                    data: { ModId: this.ModId, Point: this.rating }
                }).then(r => {
                    if (r.data.ResultCode === 200) {
                        ElMessage.success('评分提交成功');
                        this.ratingWindowVisible = false;
                    } else {
                        ElMessage.error('评分提交失败: ' + r.data.ResultMsg);
                    }
                }).catch(e => ElMessage.error('请求失败: ' + (e.response?.data?.ResultMsg || e.message)));
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
                    }
                }).then(r => {
                    if (r.data.ResultCode === 200) {
                        ElMessage.success('评分提交成功');
                        this.ratingWindowVisible = false;
                    } else {
                        ElMessage.error('评分提交失败: ' + r.data.ResultMsg);
                    }
                }).catch(e => ElMessage.error('请求失败: ' + (e.response?.data?.ResultMsg || e.message)));
            },
            handleProfile(UserId) {
                router.push({ path: '/profile', query: { UserId } });
            },
            hasResume(FileId) {
                if (!FileId) return false;
                try {
                    const meta = JSON.parse(localStorage.getItem(`file_meta_${FileId}`));
                    const offset = parseInt(localStorage.getItem(`file_offset_${FileId}`) || '0', 10);
                    if (!meta || !meta.total) return false;
                    return offset > 0 && offset < meta.total;
                } catch { return false; }
            },
            restartDownload(FileId, VersionNumber) {
                if (!FileId) return;
                localStorage.removeItem(`file_meta_${FileId}`);
                localStorage.removeItem(`file_offset_${FileId}`);
                localStorage.removeItem(`file_blob_${FileId}`);
                ElMessage.info('已清除断点信息，开始重新下载');
                this.handleDownload(FileId, VersionNumber, true);
            },
            isAxiosTimeout(err) {
                return err?.code === 'ECONNABORTED' || /timeout/i.test(err?.message || '');
            },
            retryDownload() {
                if (!this.currentDownloadFileId) return;
                ElMessage.info('重新尝试下载...');
                this.isDownloadTimeout = false;
                // 不重置进度，继续断点逻辑
                this.handleDownload(this.currentDownloadFileId, this.currentDownloadVersionNumber, false);
            },
            async handleDownload(FileId, VersionNumber, forceReset = false) {
                if (!FileId) {
                    ElMessage.error('文件 ID 不存在，无法下载');
                    return;
                }
                // 记录当前下载信息供重试
                this.currentDownloadFileId = FileId;
                this.currentDownloadVersionNumber = VersionNumber;
                this.isDownloadTimeout = false;

                const CHUNK_SIZE = 64 * 1024;
                const metaKey = `file_meta_${FileId}`;
                const offsetKey = `file_offset_${FileId}`;
                const blobKey = `file_blob_${FileId}`;
                const defaultMime = 'application/octet-stream';

                this.progress = 0;
                this.showStatus = true;
                this.versionDialogVisible = false;

                let savedMeta = null;
                try { savedMeta = JSON.parse(localStorage.getItem(metaKey)); } catch { savedMeta = null; }
                let savedOffset = parseInt(localStorage.getItem(offsetKey) || '0', 10);
                if (isNaN(savedOffset)) savedOffset = 0;
                if (forceReset) {
                    savedMeta = null;
                    savedOffset = 0;
                    localStorage.removeItem(offsetKey);
                }
                const canResume = savedMeta && savedMeta.total && savedOffset > 0 && savedOffset < savedMeta.total;
                let resume = canResume && !forceReset;

                const parseContentRange = (cr) => {
                    if (!cr) return { total: 0, start: 0, end: 0 };
                    const m = /bytes\s+(\d+)-(\d+)\/(\d+)/i.exec(cr);
                    if (m) return { start: parseInt(m[1], 10), end: parseInt(m[2], 10), total: parseInt(m[3], 10) };
                    return { total: 0, start: 0, end: 0 };
                };

                let timeoutOccurred = false;

                try {
                    if (!savedMeta || forceReset) {
                        const metaResp = await this.$axios({
                            url: `${import.meta.env.VITE_API_BASE_URL}/Files/DownloadFile`,
                            method: 'POST',
                            data: { FileId, NoCount: savedMeta && !forceReset ? true : false },
                            headers: {
                                'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail')),
                                'Range': 'bytes=0-'
                            },
                            responseType: 'arraybuffer',
                            timeout: 60000,
                            validateStatus: s => [200, 206].includes(s)
                        });
                        const disposition = metaResp.headers['content-disposition'] || '';
                        const fileNameMatch = /filename="?(?<name>[^"]+)"?/i.exec(disposition);
                        const fileName = fileNameMatch?.groups?.name || FileId;
                        const crHeader = metaResp.headers['content-range'];
                        const cr = parseContentRange(crHeader);
                        const ct = (metaResp.headers['content-type'] || defaultMime).split(';')[0].trim().toLowerCase();

                        if (!cr.total) {
                            const total = metaResp.data.byteLength;
                            savedMeta = { total, fileName, mime: ct };
                            localStorage.setItem(metaKey, JSON.stringify(savedMeta));
                            const blob = new Blob([metaResp.data], { type: ct });
                            const url = URL.createObjectURL(blob);
                            localStorage.setItem(blobKey, url);
                            localStorage.removeItem(offsetKey);
                            this.progress = 100;
                            this.showStatus = false;
                            this.saveBlobAndDownload(url, fileName, VersionNumber, ct);
                            ElMessage.success('下载完成');
                            return;
                        } else {
                            savedMeta = { total: cr.total, fileName, mime: ct };
                            localStorage.setItem(metaKey, JSON.stringify(savedMeta));
                            const blob = new Blob([metaResp.data], { type: ct });
                            const url = URL.createObjectURL(blob);
                            localStorage.setItem(blobKey, url);
                            localStorage.removeItem(offsetKey);
                            this.progress = 100;
                            this.showStatus = false;
                            this.saveBlobAndDownload(url, fileName, VersionNumber, ct);
                            ElMessage.success('下载完成');
                            return;
                        }
                    }

                    let startOffset = resume ? savedOffset : 0;
                    if (!resume && savedOffset > 0) {
                        localStorage.removeItem(offsetKey);
                        startOffset = 0;
                    }

                    const totalSize = savedMeta.total;
                    if (!totalSize || totalSize <= 0) {
                        ElMessage.error('无法获取文件大小');
                        this.showStatus = true;
                        return;
                    }

                    if (startOffset >= totalSize) {
                        this.progress = 100;
                        this.showStatus = true;
                        const cacheUrl = localStorage.getItem(blobKey);
                        if (cacheUrl) {
                            this.saveBlobAndDownload(cacheUrl, savedMeta.fileName, VersionNumber, savedMeta.mime || defaultMime);
                            ElMessage.success('文件已下载（缓存）');
                            this.showStatus = false;
                            return;
                        }
                        startOffset = 0;
                    }

                    const chunks = [];
                    let downloaded = startOffset;
                    let lastContentType = savedMeta.mime || defaultMime;

                    while (downloaded < totalSize) {
                        const chunkEnd = Math.min(downloaded + CHUNK_SIZE - 1, totalSize - 1);
                        const rangeHeader = `bytes=${downloaded}-${chunkEnd}`;

                        let partResp;
                        try {
                            partResp = await this.$axios({
                                url: `${import.meta.env.VITE_API_BASE_URL}/Files/DownloadFile`,
                                method: 'POST',
                                data: { FileId, NoCount: true },
                                headers: {
                                    'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail')),
                                    'Range': rangeHeader
                                },
                                responseType: 'arraybuffer',
                                timeout: 60000,
                                validateStatus: s => [200, 206].includes(s)
                            });
                        } catch (err) {
                            if (this.isAxiosTimeout(err)) {
                                timeoutOccurred = true;
                                this.isDownloadTimeout = true;
                                ElMessage.warning('分块下载超时，您可以重试');
                                break;
                            } else {
                                throw err;
                            }
                        }

                        if (!partResp) break; // 超时退出

                        if (![200, 206].includes(partResp.status)) {
                            throw new Error(`分块失败 状态码: ${partResp.status}`);
                        }

                        const partCt = (partResp.headers['content-type'] || '').split(';')[0].trim().toLowerCase();
                        if (partCt) lastContentType = partCt;

                        chunks.push(new Uint8Array(partResp.data));
                        downloaded = chunkEnd + 1;
                        localStorage.setItem(offsetKey, downloaded.toString());
                        this.progress = Math.round(downloaded / totalSize * 100);
                        if (timeoutOccurred) break;
                    }

                    if (timeoutOccurred) {
                        // 保留 showStatus 和 progress，不合并已下载分块，等待重试继续
                        return;
                    }

                    const merged = this.mergeUint8Arrays(chunks);
                    const finalBlob = new Blob([merged], { type: lastContentType || defaultMime });
                    const finalUrl = URL.createObjectURL(finalBlob);
                    localStorage.setItem(blobKey, finalUrl);
                    localStorage.setItem(metaKey, JSON.stringify({
                        total: totalSize,
                        fileName: savedMeta.fileName,
                        mime: lastContentType || defaultMime
                    }));
                    localStorage.removeItem(offsetKey);

                    this.progress = 100;
                    this.saveBlobAndDownload(finalUrl, savedMeta.fileName, VersionNumber, lastContentType || defaultMime);
                    this.showStatus = false;
                    ElMessage.success('下载完成');
                } catch (e) {
                    if (this.isAxiosTimeout(e)) {
                        this.isDownloadTimeout = true;
                        timeoutOccurred = true;
                        ElMessage.warning('下载请求超时，可点击重试');
                        // 不关闭弹窗，不清零进度
                    } else {
                        ElMessage.error('下载失败: ' + (e?.message || e));
                        this.showStatus = false;
                        this.progress = 0;
                    }
                } finally {
                    // 非超时且已完成或失败才清理进度
                    if (!timeoutOccurred && this.progress === 100) {
                        setTimeout(() => { this.progress = 0; }, 800);
                    }
                }
            },
            mergeUint8Arrays(arrays) {
                const totalLength = arrays.reduce((sum, arr) => sum + arr.length, 0);
                const result = new Uint8Array(totalLength);
                let offset = 0;
                for (const arr of arrays) {
                    result.set(arr, offset);
                    offset += arr.length;
                }
                return result;
            },
            buildFileName(versionNumber, contentType) {
                const base = this.entity?.Name ? this.entity.Name.replace(/\s+/g, '_') : 'file';
                let ext = '';
                switch (contentType) {
                    case 'application/zip':
                    case 'application/x-zip-compressed': ext = '.zip'; break;
                    case 'application/json': ext = '.json'; break;
                    case 'text/plain': ext = '.txt'; break;
                    case 'application/x-rar-compressed': ext = '.rar'; break;
                }
                return `${base}${versionNumber || ''}${ext}`;
            },
            triggerBrowserDownload(blob, fileName) {
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', fileName);
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                setTimeout(() => URL.revokeObjectURL(url), 30000);
            },
            saveBlobAndDownload(objectUrl, rawNameOrFileId, versionNumber, contentType) {
                const finalName = this.buildFileName(versionNumber, (contentType || '').split(';')[0].trim().toLowerCase());
                const a = document.createElement('a');
                a.href = objectUrl;
                a.setAttribute('download', finalName);
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
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
                        data: { FileId },
                        responseType: 'blob',
                        onDownloadProgress: (e) => {
                            if (e.total) this.progress = Math.round((e.loaded * 100) / e.total);
                        }
                    });
                    const contentType = response.headers['content-type'];
                    if (contentType === 'application/json' || contentType === 'text/plain') {
                        const reader = new FileReader();
                        reader.onload = () => {
                            try {
                                const jsonContent = JSON.parse(reader.result);
                                this.codeContent = jsonContent;
                                this.initMonacoEditor();
                            } catch {
                                ElMessage.error('文件内容不是有效的 JSON 格式');
                            }
                        };
                        reader.readAsText(response.data);
                    }
                    this.showPreviewStatus = false;
                    this.progress = 0;
                } catch (error) {
                    this.showPreviewStatus = false;
                    this.progress = 0;
                    ElMessage.error('预览文件失败:' + error);
                }
            },
            copyToClipboard() {
                if (!this.codeContent || Object.keys(this.codeContent).length === 0) {
                    ElMessage.warning('没有可复制的内容');
                    return;
                }
                const contentToCopy = JSON.stringify(this.codeContent, null, 2);
                if (navigator.clipboard?.writeText) {
                    navigator.clipboard.writeText(contentToCopy)
                        .then(() => ElMessage.success('代码已复制到剪切板'))
                        .catch(() => ElMessage.error('复制失败，请重试'));
                } else {
                    const textarea = document.createElement('textarea');
                    textarea.value = contentToCopy;
                    document.body.appendChild(textarea);
                    textarea.select();
                    try {
                        document.execCommand('copy');
                        ElMessage.success('代码已复制到剪切板');
                    } catch {
                        ElMessage.error('复制失败，请重试');
                    }
                    document.body.removeChild(textarea);
                }
            },
            async initMonacoEditor() {
                const editorContainer = document.getElementById('monaco-editor');
                if (!editorContainer) return;
                if (editorContainer.__monacoEditorInstance) {
                    editorContainer.__monacoEditorInstance.dispose();
                }
                window.require.config({
                    paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.34.1/min/vs' },
                });
                window.require(['vs/editor/editor.main'], (monaco) => {
                    const isDarkMode = document.body.classList.contains('dark-theme');
                    const theme = isDarkMode ? 'vs-dark' : 'vs';
                    const editorInstance = monaco.editor.create(editorContainer, {
                        value: JSON.stringify(this.codeContent, null, 2),
                        language: 'json',
                        theme
                    });
                    editorContainer.__monacoEditorInstance = editorInstance;
                });
            },
            goToModDetail(ModId) {
                if (!ModId) return;
                router.push({
                    path: '/modDetail',
                    query: { ModId }
                });
            },
            goBack() {
                router.go(-1);
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

        .el-button + .el-button {
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

    .myiframe {
    }

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

    body.dark-theme .el-table__row:hover > td {
        background-color: #333333;
        color: #ffffff;
    }

    body.dark-theme .el-table__row.is-selected > td {
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

    body.dark-theme .el-table--enable-row-hover .el-table__body tr:hover > td.el-table__cell {
        background-color: #333333;
        color: #ffffff;
        transition: background-color 0.3s ease, color 0.3s ease;
    }

    body.dark-theme .el-table--enable-row-hover .el-table__body tr:hover > td.el-table__cell {
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

        body.dark-theme .el-table__row:hover > td {
            background-color: #333333;
            color: #ffffff;
        }

        body.dark-theme .el-table__row.is-selected > td {
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