'use client';

import React, { useState, useEffect } from 'react';
import { useGame } from '@/contexts/GameContext';
import { ApiResponse } from '@/types/api';
import { API_ENDPOINTS } from '@/config/api';

interface ModType {
    TypesId: string;
    TypeName: string;
}

interface ModItem {
    ModId: string;
    Name: string;
    PicUrl: string;
    ModTypeEntities: ModType[];
    IsMySubscribe: boolean;
    AVGPoint: number;
    DownloadCount: number;
    CreatorUserId: string;
    CreatorNickName: string;
    CreatorHeadPic: string;
}

const MainPage = () => {
    const { selectedGameId } = useGame();
    const [mods, setMods] = useState<ModItem[]>([]);
    const [types, setTypes] = useState<ModType[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [selectedTypes, setSelectedTypes] = useState<string[]>([]);
    const [searchText, setSearchText] = useState('');
    const [currentPage, setCurrentPage] = useState(0);
    const [pageSize] = useState(10);

    const apiUrl = process.env.NEXT_PUBLIC_API_URL
    const gameId = selectedGameId;

    useEffect(() => {
        const fetchData = async () => {
            try {
                // 如果未选择游戏，跳过加载
                if (!gameId) {
                    setLoading(false);
                    setTypes([]);
                    setMods([]);
                    return;
                }

                setLoading(true);
                setError('');

                // 获取所有类型
                const typesResponse = await fetch(API_ENDPOINTS.mods.getAllTypes, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ GameId: gameId }),
                });

                if (!typesResponse.ok) {
                    throw new Error(`获取类型失败: ${typesResponse.status}`);
                }

                const typesData: ApiResponse<ModType[]> = await typesResponse.json();
                if (typesData.ResultCode === 200 && typesData.ResultData) {
                    setTypes(typesData.ResultData);
                } else {
                    throw new Error(typesData.ResultMsg || '获取类型失败');
                }

                // 获取 Mod 列表（只有在有 gameId 时）
                await fetchModList(0, []);
            } catch (err) {
                setError(err instanceof Error ? err.message : '加载失败');
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [gameId]);

    const fetchModList = async (skip: number, typeIds: string[]) => {
        try {
            const response = await fetch(API_ENDPOINTS.mods.listPage, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    Skip: skip.toString(),
                    Take: pageSize.toString(),
                    Search: searchText,
                    Types: typeIds.length > 0 ? typeIds : undefined,
                    GameId: gameId,
                }),
            });

            if (!response.ok) {
                throw new Error(`获取列表失败: ${response.status}`);
            }

            const data: ApiResponse<ModItem[]> = await response.json();
            if (data.ResultCode === 200 && data.ResultData) {
                setMods(data.ResultData);
                setCurrentPage(skip / pageSize);
            } else {
                throw new Error(data.ResultMsg || '获取列表失败');
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : '加载列表失败');
        }
    };

    const handleTypeToggle = (typeId: string) => {
        const newTypes = selectedTypes.includes(typeId)
            ? selectedTypes.filter(t => t !== typeId)
            : [...selectedTypes, typeId];
        setSelectedTypes(newTypes);
        fetchModList(0, newTypes);
    };

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        setCurrentPage(0);
        fetchModList(0, selectedTypes);
    };

    const handlePageChange = (newPage: number) => {
        const skip = newPage * pageSize;
        fetchModList(skip, selectedTypes);
    };

    if (loading) {
        return (
            <div className="min-h-screen bg-white dark:bg-gray-950 flex items-center justify-center">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
                    <p className="text-gray-600 dark:text-gray-400">正在加载...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-white dark:bg-gray-950">
            <div className="max-w-7xl mx-auto px-4 py-8">
                {/* 搜索和筛选 */}
                <div className="mb-8">
                    <form onSubmit={handleSearch} className="mb-6">
                        <div className="flex gap-2">
                            <input
                                type="text"
                                value={searchText}
                                onChange={(e) => setSearchText(e.target.value)}
                                placeholder="搜索 Mod..."
                                className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                            />
                            <button
                                type="submit"
                                className="px-6 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg font-medium transition-colors"
                            >
                                搜索
                            </button>
                        </div>
                    </form>

                    {/* 类型筛选 */}
                    <div className="flex flex-wrap gap-2">
                        <span className="text-sm font-medium text-gray-700 dark:text-gray-300 self-center">类型:</span>
                        {types.map((type) => (
                            <button
                                key={type.TypesId}
                                onClick={() => handleTypeToggle(type.TypesId)}
                                className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${selectedTypes.includes(type.TypesId)
                                    ? 'bg-blue-500 text-white'
                                    : 'bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 hover:bg-gray-300 dark:hover:bg-gray-600'
                                    }`}
                            >
                                {type.TypeName}
                            </button>
                        ))}
                    </div>
                </div>

                {/* 错误提示 */}
                {error && (
                    <div className="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg">
                        {error}
                    </div>
                )}

                {/* Mod 列表 */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {mods.map((mod) => (
                        <div
                            key={mod.ModId}
                            className="bg-white dark:bg-gray-800 rounded-lg shadow hover:shadow-lg transition-shadow overflow-hidden"
                        >
                            {/* 图片 */}
                            <div className="relative h-48 bg-gray-200 dark:bg-gray-700 overflow-hidden">
                                {mod.PicUrl ? (
                                    <img
                                        src={mod.PicUrl}
                                        alt={mod.Name}
                                        className="w-full h-full object-cover hover:scale-105 transition-transform"
                                    />
                                ) : (
                                    <div className="w-full h-full flex items-center justify-center text-gray-400">
                                        无图片
                                    </div>
                                )}
                            </div>

                            {/* 内容 */}
                            <div className="p-4">
                                <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-2 line-clamp-2">
                                    {mod.Name}
                                </h3>

                                {/* 类型标签 */}
                                <div className="flex flex-wrap gap-1 mb-3">
                                    {mod.ModTypeEntities?.map((type) => (
                                        <span
                                            key={type.TypesId}
                                            className="text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-2 py-1 rounded"
                                        >
                                            {type.TypeName}
                                        </span>
                                    ))}
                                </div>

                                {/* 统计信息 */}
                                <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 mb-3">
                                    <span>⭐ {mod.AVGPoint?.toFixed(1) || '0'}</span>
                                    <span>⬇️ {mod.DownloadCount?.toLocaleString() || 0}</span>
                                </div>

                                {/* 作者信息 */}
                                <div className="flex items-center gap-2 text-sm">
                                    {mod.CreatorHeadPic && (
                                        <img
                                            src={mod.CreatorHeadPic}
                                            alt={mod.CreatorNickName}
                                            className="w-6 h-6 rounded-full object-cover"
                                        />
                                    )}
                                    <span className="text-gray-700 dark:text-gray-300 truncate">
                                        {mod.CreatorNickName}
                                    </span>
                                </div>

                                {/* 订阅状态 */}
                                {mod.IsMySubscribe && (
                                    <div className="mt-3 py-2 px-3 bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200 rounded text-xs font-medium text-center">
                                        ✓ 已订阅
                                    </div>
                                )}
                            </div>
                        </div>
                    ))}
                </div>

                {/* 分页 */}
                {mods.length > 0 && (
                    <div className="flex justify-center gap-2 mt-8">
                        <button
                            onClick={() => handlePageChange(currentPage - 1)}
                            disabled={currentPage === 0}
                            className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-300 dark:hover:bg-gray-600"
                        >
                            上一页
                        </button>
                        <span className="px-4 py-2 text-gray-700 dark:text-gray-300">
                            第 {currentPage + 1} 页
                        </span>
                        <button
                            onClick={() => handlePageChange(currentPage + 1)}
                            disabled={mods.length < pageSize}
                            className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-300 dark:hover:bg-gray-600"
                        >
                            下一页
                        </button>
                    </div>
                )}

                {/* 空状态 */}
                {!loading && mods.length === 0 && !error && (
                    <div className="text-center py-12">
                        <p className="text-gray-600 dark:text-gray-400 text-lg">没有找到相关 Mod</p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default MainPage;
