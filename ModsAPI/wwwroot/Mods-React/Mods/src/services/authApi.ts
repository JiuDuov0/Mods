import apiService from './api';
import { API_ENDPOINTS } from '@/config/api';
import { ApiResponse } from '@/types/api';

export interface ResponseToken {
    token: string;
    refresh_Token: string;
    nickName: string;
    role?: string;
    headPic?: string;
}

export interface LoginRequest {
    loginAccount: string;
    password: string;
}

export const authApi = {
    login: (credentials: LoginRequest): Promise<ApiResponse<ResponseToken>> =>
        apiService.post<ResponseToken>(API_ENDPOINTS.auth.login, credentials),

    register: (data: LoginRequest & { username: string }): Promise<ApiResponse<ResponseToken>> =>
        apiService.post<ResponseToken>(API_ENDPOINTS.auth.register, data),

    logout: (): Promise<ApiResponse<null>> =>
        apiService.post<null>(API_ENDPOINTS.auth.logout),

    getProfile: (): Promise<ApiResponse<any>> =>
        apiService.get<any>(API_ENDPOINTS.auth.profile),
};
