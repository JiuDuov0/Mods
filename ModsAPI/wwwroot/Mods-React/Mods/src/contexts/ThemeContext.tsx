'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';

type Theme = 'light' | 'dark' | 'system';

interface ThemeContextType {
    theme: Theme;
    setTheme: (theme: Theme) => void;
    isDark: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

const applyTheme = (theme: Theme) => { 
    if (typeof window === 'undefined' || typeof document === 'undefined') return false;

    const root = document.documentElement;
    let isDark = false;

    if (theme === 'dark') {
        root.classList.add('dark');
        isDark = true;
    } else if (theme === 'light') {
        root.classList.remove('dark');
        isDark = false;
    } else {
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        if (prefersDark) {
            root.classList.add('dark');
            isDark = true;
        } else {
            root.classList.remove('dark');
            isDark = false;
        }
    }

    return isDark;
};

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [state, setState] = useState<{ theme: Theme; isDark: boolean } | null>(null);

    useEffect(() => {
        if (typeof window === 'undefined') return;

        const savedTheme = (localStorage.getItem('theme') as Theme) || 'system';
        const isDark = applyTheme(savedTheme);
        setState({ theme: savedTheme, isDark });
    }, []);

    const setTheme = (newTheme: Theme) => {
        if (typeof window === 'undefined') return;

        localStorage.setItem('theme', newTheme);
        const isDark = applyTheme(newTheme);
        setState({ theme: newTheme, isDark });
    };

    if (!state) {
        return <>{children}</>;
    }

    return (
        <ThemeContext.Provider value={{ ...state, setTheme }}>
            {children}
        </ThemeContext.Provider>
    );
};

export const useTheme = () => {
    const context = useContext(ThemeContext);
    if (context === undefined) {
        throw new Error('useTheme must be used within ThemeProvider');
    }
    return context;
};


