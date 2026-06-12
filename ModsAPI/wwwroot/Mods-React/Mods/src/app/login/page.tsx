'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

const LoginPage = () => {
    const router = useRouter();
    const [LoginAccount, setLoginAccount] = useState('');
    const [password, setPassword] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [successMsg, setSuccessMsg] = useState('');

    async function sha256Hex(input: string): Promise<string> {
        const encoder = new TextEncoder();
        const data = encoder.encode(input);
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError('');
        setSuccessMsg('');

        if (!LoginAccount || !password) {
            setError('请输入账号和密码');
            setIsLoading(false);
            return;
        }

        try {
            // 在发送前对密码进行 SHA-256 哈希
            const hashedPassword = await sha256Hex(password);

            const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/Login/UserLogin`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ LoginAccount, Password: hashedPassword }),
            });

            if (!response.ok) {
                throw new Error(`网络错误: ${response.status}`);
            }

            const data = await response.json();

            // <-- 这里改为与后端示例返回字段一致（PascalCase）
            if (data.ResultCode === 200 && data.ResultData) {
                localStorage.setItem('token', data.ResultData.Token);
                localStorage.setItem('refresh_token', data.ResultData.Refresh_Token);
                localStorage.setItem('userInfo', JSON.stringify({
                    nickName: data.ResultData.NickName,
                    role: data.ResultData.Role,
                    headPic: data.ResultData.HeadPic,
                }));

                setSuccessMsg(`登录成功，欢迎 ${data.ResultData.NickName}！`);
                setTimeout(() => {
                    router.push('/mainpage');
                }, 1000);
            } else {
                setError(data.ResultMsg || data.ResultMsg || '登录失败，请重试');
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : '登录失败');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-white dark:bg-gray-950 flex items-center justify-center px-4">
            <div className="w-full max-w-md">
                <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 sm:p-8">
                    <h1 className="text-3xl sm:text-4xl font-bold mb-2 text-center text-gray-900 dark:text-gray-100">
                        MODCAT
                    </h1>
                    <p className="text-center text-gray-600 dark:text-gray-400 mb-8">
                        登录你的账户
                    </p>

                    {error && (
                        <div className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg text-sm">
                            {error}
                        </div>
                    )}

                    {successMsg && (
                        <div className="mb-4 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg text-sm">
                            {successMsg}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label htmlFor="loginAccount" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                                邮箱地址
                            </label>
                            <input
                                id="loginAccount"
                                type="email"
                                value={LoginAccount}
                                onChange={(e) => setLoginAccount(e.target.value)}
                                placeholder="请输入邮箱"
                                className="w-full px-4 py-2 sm:py-3 text-sm sm:text-base border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 transition-all duration-200"
                                disabled={isLoading}
                                required
                            />
                        </div>

                        <div>
                            <label htmlFor="password" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                                密码
                            </label>
                            <input
                                id="password"
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                placeholder="请输入密码"
                                className="w-full px-4 py-2 sm:py-3 text-sm sm:text-base border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 transition-all duration-200"
                                disabled={isLoading}
                                required
                            />
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-2 sm:py-3 px-4 bg-blue-500 hover:bg-blue-600 dark:bg-blue-600 dark:hover:bg-blue-700 text-white font-semibold text-sm sm:text-base rounded-lg disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-blue-400 transition-all duration-200"
                        >
                            {isLoading ? '登录中...' : '登录'}
                        </button>
                    </form>

                    <div className="mt-6 text-center text-xs sm:text-sm text-gray-600 dark:text-gray-400">
                        <p>
                            还没有账户？{' '}
                            <Link href="/register" className="text-blue-500 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-semibold transition-colors duration-200">
                                现在注册
                            </Link>
                        </p>
                    </div>
                </div>

                <p className="text-center text-xs sm:text-sm text-gray-500 dark:text-gray-500 mt-6 sm:mt-8">
                    © 2024 MODCAT. 保护隐私 • 服务条款
                </p>
            </div>
        </div>
    );
};

export default LoginPage;
