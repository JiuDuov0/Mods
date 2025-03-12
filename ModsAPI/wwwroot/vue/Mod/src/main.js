import { createApp } from 'vue'
import App from './App.vue'
import anti from 'ant-design-vue';
import "ant-design-vue/dist/reset.css";
import router from './router'; // 导入路由器
import axios from 'axios'
import $ from 'jquery'

const app = createApp(App);
const url = "https://127.0.0.1:7114";
app.config.globalProperties.$url = url; // 添加 url 到全局属性
app.config.globalProperties.$axios = axios; // 添加 axios 到全局属性
app.config.globalProperties.$ = $; // 添加 jQuery 到全局属性
app.config.globalProperties.$router = router // 添加路由器到全局属性
app.use(router); // 使用路由器
app.use(anti);
app.mount('#app');