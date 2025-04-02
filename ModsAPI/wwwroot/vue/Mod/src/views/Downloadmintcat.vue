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
            </div>
        </div>

    </main>
</template>

<script>
import router from '../router/index.js';
export default {
    name: 'AddNewVersion',
    data() {
        return {};
    },
    mounted() {
    },
    methods: {
        download() {

        }
    }
};
</script>

<style scoped>
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
    /* background-image: url("../src/assets/mintcat-pic.png"); */
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
</style>