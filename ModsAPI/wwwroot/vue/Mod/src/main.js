import { createApp } from 'vue'
import App from './App.vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import router from './router/index'; // 导入路由器
import $ from 'jquery'

const app = createApp(App);
const url = "https://127.0.0.1:7114";
app.config.globalProperties.$url = url; // 添加 url 到全局属性
app.config.globalProperties.$ = $; // 添加 jQuery 到全局属性
app.config.globalProperties.$router = router // 添加路由器到全局属性
app.use(router); // 使用路由器
app.use(ElementPlus);
app.mount('#app');