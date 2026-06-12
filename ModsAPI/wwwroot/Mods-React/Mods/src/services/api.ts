import { tokenManager } from '@/utils/tokenManager';

interface ApiResponse<T> {
    resultCode?: number;
    resultMsg?: string;
    resultData?: T;
}

interface RequestOptions extends RequestInit {
    headers?: Record<string, string>;
}

class ApiService {
    private async request<T>(
        url: string,
        options: RequestOptions = {}
    ): Promise<ApiResponse<T>> {
        const defaultHeaders: Record<string, string> = {
            'Content-Type': 'application/json',
        };

        const token = tokenManager.getToken();
        if (token) {
            defaultHeaders['Authorization'] = `Bearer ${token}`;
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers: {
                    ...defaultHeaders,
                    ...options.headers,
                },
            });

            const data = await response.json();

            if (response.status === 401) {
                tokenManager.clear();
                window.location.href = '/login';
            }

            return data;
        } catch (error) {
            return {
                resultCode: 500,
                resultMsg: error instanceof Error ? error.message : '请求失败',
            };
        }
    }

    get<T>(url: string, options?: RequestOptions): Promise<ApiResponse<T>> {
        return this.request<T>(url, { ...options, method: 'GET' });
    }

    post<T>(url: string, body?: unknown, options?: RequestOptions): Promise<ApiResponse<T>> {
        return this.request<T>(url, {
            ...options,
            method: 'POST',
            body: body ? JSON.stringify(body) : undefined,
        });
    }

    put<T>(url: string, body?: unknown, options?: RequestOptions): Promise<ApiResponse<T>> {
        return this.request<T>(url, {
            ...options,
            method: 'PUT',
            body: body ? JSON.stringify(body) : undefined,
        });
    }

    delete<T>(url: string, options?: RequestOptions): Promise<ApiResponse<T>> {
        return this.request<T>(url, { ...options, method: 'DELETE' });
    }
}

export default new ApiService();
