import { createApp } from 'vue'
import App from './App.vue'
import 'element-plus/dist/index.css'
import router from './router/index'; // 导入路由器
import axios from 'axios' // 导入 axios
import { ElMessage } from 'element-plus';
import { Back, Star } from '@element-plus/icons-vue';

const app = createApp(App);
app.component('Back', Back);
app.component('Star', Star);
app.config.globalProperties.$router = router // 添加路由器到全局属性
axios.defaults.headers.common['Authorization'] = `Bearer ${localStorage.getItem('token' + localStorage.getItem('Mail'))}`; // 设置默认的 Authorization 头
app.config.globalProperties.$axios = axios; // 添加 axios 到全局属性
app.config.globalProperties.$message = ElMessage; // 添加 ElMessage 到全局属性
app.use(router); // 使用路由器
app.mount('#app');