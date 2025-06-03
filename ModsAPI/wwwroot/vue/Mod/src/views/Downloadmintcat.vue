<template>
    <header class="header">
        <img src="../assets/mintcat-icon.ico" class="modcat-logo-header" />
        <span class="span-logo">mintcat</span>
    </header>
    <main>
        <div class="div-main">
            <div class="div-main-content">
                <img src="../assets/mintcat-icon.ico" class="modcat-logo-main" />
                <div class="div-main-thx">感谢您下载mintcat！</div>
                <div class="div-main-download">下载即将开始。 如果下载未开始，请<a style="text-decoration:underline"
                        @click="download">单击此处以重试</a></div>
                <div class="div-main-download">
                    <a style="text-decoration:underline;color: #F2F2F2;" href="https://mintcat.v1st.net/">mintcat官网</a>
                    <a style="text-decoration:underline;color: #F2F2F2;margin-left: 1rem;"
                        href="https://github.com/iriscats/mintcat/tree/main">GitHub</a>
                </div>
            </div>
        </div>

        <!-- 新增内容 -->
        <div class="div-main-extra">
            <div class="sidebar">
                <ul>
                    <li :class="{ active: activeSection === 'section1' }"><a
                            @click.prevent="scrollToSection('section1')">入门</a></li>
                    <li :class="{ active: activeSection === 'section2' }">安装 / 卸载<a
                            @click.prevent="scrollToSection('section2')"></a></li>
                    <li :class="{ active: activeSection === 'section3' }"><a
                            @click.prevent="scrollToSection('section3')">新用户</a></li>
                    <li :class="{ active: activeSection === 'section4' }"><a
                            @click.prevent="scrollToSection('section4')">老用户</a></li>
                    <li :class="{ active: activeSection === 'section5' }"><a
                            @click.prevent="scrollToSection('section5')">常见问题</a></li>
                </ul>
            </div>
            <div class="content">
                <section id="section1">
                    <h2>入门</h2>
                    <p>mintcat 是一款虚幻引擎游戏的 MOD 管理器，目前只支持《深岩银河》这款游戏，后续会支持更多的虚幻引擎游戏。</p>
                </section>
                <section id="section2">
                    <h2>安装 / 卸载</h2>
                    <p>在 mintcat 0.4.x 以后的版本将应用程序打包在一个独立的 exe 安装文件中，便于用户安装和卸载。</p>
                    <h3>安装步骤</h3>
                    <ul class="numbered-list">
                        <li> 下载 mintcat 0.4.x 版本的安装包。 </li>
                        <li> 运行安装包，按照提示安装 mintcat 到你的电脑中。 </li>
                        <li>安装完成后，双击桌面上 mintcat 图标，即可打开软件主界面。</li>
                    </ul>
                    <h3>卸载步骤</h3>
                    <ul class="numbered-list">
                        <li>在 mintcat 安装目录中找到 uninstall.exe 文件。</li>
                        <li>运行 uninstall.exe 文件，按照提示卸载 mintcat。</li>
                    </ul>
                </section>
                <section id="section3">
                    <h2>新用户</h2>
                    <p>新用户需要填写授权，才能正常访问 mod.io 平台。</p>
                    <h3>获取 mod.io 授权</h3>
                    <ul>
                        <li>点击侧边栏的 设置（齿轮）按钮，进入设置界面。</li>
                        <li>点击 打开 mod.io 按钮，将会打开浏览器访问<a href="https://mod.io/me/access">https://mod.io/me/access</a>。
                        </li>
                    </ul>
                </section>
                <section id="section4">
                    <h2>老用户</h2>
                    <p>老用户可以选择自动导入数据，将 MINT 0.2.x 版本，或者 MINTCAT 0.3.x 版本的数据装换为 MINTCAT 0.4.x 的数据。</p>
                    <h3>导入数据</h3>
                    <ul class="numbered-list">
                        <li>安装完成后，第一次打开后将会出现提示 发现旧版 MINT(0.2, 0.3) 配置文件，注意将会覆盖当前配置，是否导入？</li>
                        <li>点击 确定 按钮，将会自动导入数据。</li>
                        <li>点击 取消 按钮，将会直接进入软件界面。</li>
                    </ul>
                    <p><span class="span-imp">注意：</span>如果使用过 MINTCAT 0.4.x，存在用户数据，并且不希望覆盖之前的数据，那么需要点击取消 按钮不导入数据。</p>
                </section>
                <section id="section5">
                    <h2>常见问题</h2>
                    <!-- <h3>为什么我的游戏变成了沙盒模式</h3> -->
                </section>
            </div>
        </div>
    </main>
</template>

<script>
import router from '../router/index.js';

export default {
    name: 'AddNewVersion',
    data() {
        return { activeSection: '', };
    },
    mounted() {
        this.observeSections();
        this.download(); // 页面加载时自动触发下载
        document.body.classList.remove('dark-theme');
    },
    methods: {
        download() {
            const fileUrl = new URL('../assets/mintcat_0.4.5_x64-setup.zip', import.meta.url).href;
            const link = document.createElement('a');
            link.href = fileUrl; // 设置文件路径
            link.download = 'mintcat_0.4.5_x64-setup.zip'; // 设置下载文件名
            link.click(); // 触发下载
        },
        scrollToSection(sectionId) {
            const section = document.getElementById(sectionId);
            if (section) {
                section.scrollIntoView({ behavior: 'smooth' }); // 平滑滚动到目标内容块
            }
        },
        observeSections() {
            const sections = document.querySelectorAll('.content section');
            const observer = new IntersectionObserver(
                (entries) => {
                    entries.forEach((entry) => {
                        if (entry.isIntersecting) {
                            this.activeSection = entry.target.id; // 更新当前可见的 section ID
                        }
                    });
                },
                {
                    root: null, // 使用默认视口
                    threshold: [0.1, 0.5, 0.9], // 多个阈值，确保在不同可见比例时触发
                    rootMargin: '0px 0px -50% 0px', // 提前触发，确保向上滚动时也能检测到
                }
            );
            sections.forEach((section) => observer.observe(section));
        }
    }
};
</script>

<style scoped>
html {
    scroll-behavior: smooth;
}

.header {
    background-color: #000000;
    display: flex;
    align-items: center;
    padding: 0.5rem 1rem;
}

.modcat-logo-header {
    width: 2.2rem;
    height: 2.2rem;
    margin-left: 5rem;
}

.modcat-logo-main {
    width: 9rem;
    height: 9rem;
    margin-left: 6rem;
    margin-top: 5rem;
}

.span-logo {
    font-size: 25px;
    color: #F2F2F2;
    margin-left: 0.5rem;
    margin-bottom: 0;
    line-height: 1;
}

.div-main {
    background-color: #000000;
    background-repeat: no-repeat;
    background-position: top right;
    height: 22rem;
}

.div-main-content {}

.div-main-thx {
    font-size: 25px;
    color: #F2F2F2;
    margin-left: 6.2rem;
    margin-bottom: 0;
    line-height: 1;
}

.div-main-download {
    font-size: 16px;
    color: #F2F2F2;
    margin-top: 1.5rem;
    margin-left: 6.2rem;
    margin-bottom: 0;
    line-height: 1;
}

.div-main-extra {
    display: flex;
    position: relative;
    margin-top: 2rem;
}

.sidebar {
    position: sticky;
    /* 设置为 sticky */
    top: 2rem;
    /* 距离视口顶部的距离 */
    width: 15%;
    padding: 1rem;
    overflow-y: auto;
    height: fit-content;
    /* 根据内容调整高度 */
}

.sidebar ul {
    list-style: none;
    border: 1px solid #ddd;
    border-radius: 0.4rem;
    padding: 0;
    padding-top: 1rem;
    margin-top: 0px;
    margin-bottom: 0px;
}

.sidebar ul li {
    margin-bottom: 1rem;
    display: block;
    padding-left: 0.6rem;
    padding-right: 0.6rem;
}

.sidebar ul li.active a {
    border-bottom: 2px solid #191919;
    /* 当前 section 的样式 */
    font-weight: bold;
    color: #000;
}

.sidebar ul li a {
    border-bottom: 1px solid #C8C6C4;
    display: block;
    width: 100%;
    text-decoration: none;
    color: #333;
    cursor: pointer;
}

.sidebar ul li a:hover {
    border-bottom: 2px solid #191919;
    text-decoration: none;
    font-weight: bold;
}

.content {
    width: 80%;
    padding: 1rem;
}

.content section {
    margin-top: 2rem;
    margin-bottom: 2rem;
}

.content section h2 {
    margin-top: 0;
}

.content section ul li a {
    color: #8661c5;
}

.numbered-list {
    list-style: none;
    counter-reset: list-counter;
}

.numbered-list li {
    counter-increment: list-counter;
    position: relative;
    padding-left: 2rem;
}

.numbered-list li::before {
    content: counter(list-counter) ". ";
    position: absolute;
    left: 0;
    color: #333;
    font-weight: bold;
}

.span-imp {
    font-weight: bold;
    color: #c21515;
}
</style>