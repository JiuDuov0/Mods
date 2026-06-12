const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://127.0.0.1:7114';

export const API_ENDPOINTS = {
    auth: {
        login: `${API_BASE_URL}/api/Login/UserLogin`,
        register: `${API_BASE_URL}/api/Login/Register`,
        logout: `${API_BASE_URL}/api/Login/Logout`,
        profile: `${API_BASE_URL}/api/User/Profile`,
    },
};