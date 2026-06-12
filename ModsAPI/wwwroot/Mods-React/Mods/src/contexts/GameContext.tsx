'use client';

import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';

interface GameContextType {
    selectedGameId: string;
    setSelectedGameId: (gameId: string) => void;
}

const GameContext = createContext<GameContextType | undefined>(undefined);

export function GameProvider({ children }: { children: React.ReactNode }) {
    // 初始化时从 localStorage 读取
    const [selectedGameId, setSelectedGameId] = useState<string>(() => {
        if (typeof window !== 'undefined') {
            return localStorage.getItem('selectedGameId') || 'default';
        }
        return 'default';
    });

    // 更新时写入 localStorage
    const handleSetSelectedGameId = useCallback((gameId: string) => {
        setSelectedGameId(gameId);
        if (typeof window !== 'undefined') {
            localStorage.setItem('selectedGameId', gameId);
        }
    }, []);

    // 确保状态和 localStorage 同步（防止外部修改）
    useEffect(() => {
        const stored = localStorage.getItem('selectedGameId');
        if (stored && stored !== selectedGameId) {
            setSelectedGameId(stored);
        }
    }, []);

    return (
        <GameContext.Provider value={{ selectedGameId, setSelectedGameId: handleSetSelectedGameId }}>
            {children}
        </GameContext.Provider>
    );
}

export function useGame() {
    const context = useContext(GameContext);
    if (context === undefined) {
        throw new Error('useGame must be used within GameProvider');
    }
    return context;
}
