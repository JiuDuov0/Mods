import type { Metadata } from 'next';
import React from 'react';
import '../styles/globals.css';
import Header from '../components/Header';
import Footer from '../components/Footer';
import { ThemeProvider } from '../contexts/ThemeContext';
import { GameProvider } from '../contexts/GameContext';

export const metadata: Metadata = {
    title: 'MODCAT',
    description: 'MODCAT - Mods Management Platform',
    viewport: 'width=device-width, initial-scale=1',
    icons: {
        icon: '/favicon.ico',
    },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="zh-CN" suppressHydrationWarning>
            <body suppressHydrationWarning>
                <ThemeProvider>
                    <GameProvider>
                        <div className="flex flex-col min-h-screen">
                            {children}
                        </div>
                    </GameProvider>
                </ThemeProvider>
            </body>
        </html>
    );
}
