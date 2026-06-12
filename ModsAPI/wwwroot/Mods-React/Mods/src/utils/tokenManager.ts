const TOKEN_KEY = 'token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_INFO_KEY = 'userInfo';

const isBrowser = typeof window !== 'undefined';

export const tokenManager = {
    setToken: (token: string) => {
        if (isBrowser) localStorage.setItem(TOKEN_KEY, token);
    },

    getToken: (): string | null => {
        if (!isBrowser) return null;
        return localStorage.getItem(TOKEN_KEY);
    },

    setRefreshToken: (token: string) => {
        if (isBrowser) localStorage.setItem(REFRESH_TOKEN_KEY, token);
    },

    getRefreshToken: (): string | null => {
        if (!isBrowser) return null;
        return localStorage.getItem(REFRESH_TOKEN_KEY);
    },

    setUserInfo: (userInfo: any) => {
        if (isBrowser) localStorage.setItem(USER_INFO_KEY, JSON.stringify(userInfo));
    },

    getUserInfo: () => {
        if (!isBrowser) return null;
        const info = localStorage.getItem(USER_INFO_KEY);
        return info ? JSON.parse(info) : null;
    },

    clear: () => {
        if (isBrowser) {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(REFRESH_TOKEN_KEY);
            localStorage.removeItem(USER_INFO_KEY);
        }
    },

    isAuthenticated: (): boolean => {
        if (!isBrowser) return false;
        return !!localStorage.getItem(TOKEN_KEY);
    },
};

