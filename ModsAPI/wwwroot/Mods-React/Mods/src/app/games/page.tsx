'use client';

import React, { useState, useEffect } from 'react';
import { useGame } from '@/contexts/GameContext';

interface GameEntity {
    GameId: string;
    GameName: string;
    Picture: string;
    Icon: string;
    DownLoadCount: number;
    SubscribeCount: number;
}

interface GameListResponse {
    ResultCode: number;
    ResultMsg?: string;
    ResultData: GameEntity[];
}

const GamePage = () => {
    const { selectedGameId, setSelectedGameId } = useGame();
    const [games, setGames] = useState<GameEntity[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [currentPage, setCurrentPage] = useState(0);
    const [pageSize] = useState(12);

    const apiUrl = process.env.NEXT_PUBLIC_API_URL;

    useEffect(() => {
        fetchGames();
    }, [currentPage]);

    const fetchGames = async () => {
        try {
            setLoading(true);
            setError('');
            setGames([]);

            const response = await fetch(`${apiUrl}/api/Game/GetGamePageList`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    Skip: (currentPage * pageSize).toString(),
                    Take: pageSize.toString(),
                }),
            });

            if (!response.ok) {
                throw new Error(`HTTP错误: ${response.status}`);
            }

            const data: GameListResponse = await response.json();
            console.log('游戏列表加载成功:', data.ResultData.length, '条游戏');

            if (data.ResultCode === 200 && data.ResultData && Array.isArray(data.ResultData)) {
                setGames(data.ResultData);
            } else {
                throw new Error(data.ResultMsg || '获取游戏列表失败');
            }
        } catch (err) {
            console.error('获取游戏列表错误:', err);
            setError(err instanceof Error ? err.message : '加载失败');
            setGames([]);
        } finally {
            setLoading(false);
        }
    };

    const handleSelectGame = (gameId: string, gameName: string) => {
        setSelectedGameId(gameId);
        //localStorage.setItem('GameId', gameId);
        console.log('选择游戏:', gameId, gameName);
        setTimeout(() => {
            window.location.href = '/mainpage';
        }, 300);
    };

    if (loading) {
        return (
            <div className="min-h-screen bg-white dark:bg-gray-950 flex items-center justify-center">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
                    <p className="text-gray-600 dark:text-gray-400">正在加载游戏列表...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-white dark:bg-gray-950">
            <div className="max-w-7xl mx-auto px-4 py-8">
                <div className="mb-8">
                    <h1 className="text-3xl sm:text-4xl font-bold mb-2 text-gray-900 dark:text-gray-100">
                        选择游戏
                    </h1>
                    <p className="text-gray-600 dark:text-gray-400">
                        选择一个游戏查看其 MOD 列表
                    </p>
                </div>

                {error && (
                    <div className="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg">
                        <p className="font-medium">加载失败</p>
                        <p className="text-sm mt-1">{error}</p>
                    </div>
                )}

                {games.length > 0 ? (
                    <>
                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 mb-8">
                            {games.map((game, idx) => (
                                <button
                                    key={game.GameId ?? `game-${idx}`}
                                    onClick={() => handleSelectGame(game.GameId, game.GameName)}
                                    className={`group rounded-lg overflow-hidden shadow hover:shadow-lg transition-all duration-300 transform hover:-translate-y-1 text-left ${selectedGameId === game.GameId
                                        ? 'ring-2 ring-blue-500'
                                        : 'ring-1 ring-gray-200 dark:ring-gray-700'
                                        }`}
                                >
                                    <div className="relative h-48 bg-gray-200 dark:bg-gray-700 overflow-hidden">
                                        {game.Picture ? (
                                            <img
                                                src={game.Picture}
                                                alt={game.GameName}
                                                className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-300"
                                            />
                                        ) : (
                                            <div className="w-full h-full flex items-center justify-center text-gray-400 text-sm">
                                                无图片
                                            </div>
                                        )}

                                        {game.Icon && (
                                            <div className="absolute top-2 right-2 w-12 h-12 rounded-full overflow-hidden border-2 border-white dark:border-gray-800 shadow-lg">
                                                <img
                                                    src={game.Icon}
                                                    alt={game.GameName}
                                                    className="w-full h-full object-cover"
                                                />
                                            </div>
                                        )}

                                        {selectedGameId === game.GameId && (
                                            <div className="absolute inset-0 bg-blue-500/20 flex items-center justify-center">
                                                <div className="bg-blue-500 text-white rounded-full p-2">
                                                    <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                                                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                                                    </svg>
                                                </div>
                                            </div>
                                        )}
                                    </div>

                                    <div className="p-4 bg-white dark:bg-gray-800">
                                        <h3 className="font-bold text-lg text-gray-900 dark:text-gray-100 mb-3 line-clamp-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                                            {game.GameName}
                                        </h3>

                                        <div className="space-y-2 text-sm">
                                            <div className="flex justify-between items-center text-gray-600 dark:text-gray-400">
                                                <span>📥 下载</span>
                                                <span className="font-semibold text-gray-900 dark:text-gray-100">
                                                    {game.DownLoadCount?.toLocaleString() || '0'}
                                                </span>
                                            </div>
                                            <div className="flex justify-between items-center text-gray-600 dark:text-gray-400">
                                                <span>⭐ 订阅</span>
                                                <span className="font-semibold text-gray-900 dark:text-gray-100">
                                                    {game.SubscribeCount?.toLocaleString() || '0'}
                                                </span>
                                            </div>
                                        </div>
                                    </div>
                                </button>
                            ))}
                        </div>

                        {games.length >= pageSize && (
                            <div className="flex justify-center gap-2">
                                <button
                                    onClick={() => setCurrentPage(Math.max(0, currentPage - 1))}
                                    disabled={currentPage === 0}
                                    className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
                                >
                                    ← 上一页
                                </button>
                                <span className="px-4 py-2 text-gray-700 dark:text-gray-300">
                                    第 {currentPage + 1} 页
                                </span>
                                <button
                                    onClick={() => setCurrentPage(currentPage + 1)}
                                    className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
                                >
                                    下一页 →
                                </button>
                            </div>
                        )}
                    </>
                ) : (
                    <div className="text-center py-12">
                        <p className="text-gray-600 dark:text-gray-400 text-lg mb-4">没有找到游戏</p>
                        <button
                            onClick={() => {
                                setCurrentPage(0);
                                fetchGames();
                            }}
                            className="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg font-medium transition-colors"
                        >
                            重新加载
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default GamePage;