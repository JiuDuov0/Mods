import { createApp } from 'vue'
import App from './App.vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import router from './router/index'; // 导入路由器
import $ from 'jquery'
import axios from 'axios' // 导入 axios


import * as ElementPlusIconsVue from '@element-plus/icons-vue'

const app = createApp(App);
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
    app.component(key, component)
}
app.config.globalProperties.$ = $; // 添加 jQuery 到全局属性
app.config.globalProperties.$router = router // 添加路由器到全局属性
axios.defaults.headers.common['Authorization'] = `Bearer ${localStorage.getItem('token')}`; // 设置默认的 Authorization 头
app.config.globalProperties.$axios = axios; // 添加 axios 到全局属性
app.use(router); // 使用路由器
app.use(ElementPlus);
app.mount('#app');