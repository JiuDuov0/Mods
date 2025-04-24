import axios from 'axios';
import router from './router/index';
import { ElMessage } from 'element-plus';

const instance = axios.create({
    baseURL: 'https://127.0.0.1:7114/api', // API 基础路径
    timeout: 10000, // 请求超时时间
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true, // 允许跨域携带 Cookie
});

// 是否正在刷新 Token
let isRefreshing = false;
// 存储待处理的请求队列
let requestsQueue = [];

// 请求拦截器
instance.interceptors.request.use(
    (config) => {
        const mail = localStorage.getItem('Mail');
        const token = localStorage.getItem('token' + mail);
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`; // 在请求头中添加 Token
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// 响应拦截器
instance.interceptors.response.use(
    (response) => {
        return response;
    },
    async (error) => {
        const originalRequest = error.config;
        const mail = localStorage.getItem('Mail');

        // 如果是 Token 过期错误
        if (error.response && error.response.status === 401 && !originalRequest._retry) {
            if (!isRefreshing) {
                isRefreshing = true;
                originalRequest._retry = true;

                try {
                    // 请求刷新 Token 接口
                    const refreshToken = localStorage.getItem('refresh_Token' + mail);
                    const refreshResponse = await instance.post('/Login/RefreshToken', {
                        RefreshToken: refreshToken,
                        Token: localStorage.getItem('token' + mail),
                    });
                    console.log(refreshResponse);
                    const newToken = refreshResponse.data.ResultData.Token;
                    const newRefreshToken = refreshResponse.data.ResultData.Refresh_Token;

                    // 更新本地存储的 Token
                    localStorage.setItem('token' + mail, newToken);
                    localStorage.setItem('refresh_Token' + mail, newRefreshToken);

                    // 重新发送队列中的请求
                    requestsQueue.forEach((callback) => callback(newToken));
                    requestsQueue = [];
                    isRefreshing = false;

                    // 重新发送原始请求
                    originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
                    return instance(originalRequest);
                } catch (refreshError) {
                    // 刷新 Token 失败，清除本地存储并跳转到登录页
                    localStorage.removeItem('token' + mail);
                    localStorage.removeItem('refresh_Token' + mail);
                    localStorage.removeItem('NickName' + mail);
                    localStorage.removeItem('HeadPic' + mail);
                    localStorage.removeItem('Role' + mail);
                    localStorage.removeItem('Mail');
                    ElMessage.error('登录已过期，请重新登录');
                    router.push('/');
                    isRefreshing = false;
                    return Promise.reject(refreshError);
                }
            } else {
                // 如果正在刷新 Token，将请求加入队列
                return new Promise((resolve) => {
                    requestsQueue.push((newToken) => {
                        originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
                        resolve(instance(originalRequest));
                    });
                });
            }
        }

        return Promise.reject(error);
    }
);

export default instance;