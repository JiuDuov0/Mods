'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function HomePage() {
    const router = useRouter();
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        setMounted(true);
    }, []);

    useEffect(() => {
        if (!mounted) return;
        if (typeof window === 'undefined') return;

        const token = localStorage.getItem('token');
        if (!token) {
            router.replace('/login');
        }
    }, [mounted, router]);

    if (!mounted) {
        return null;
    }

    return (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
            <div style={{ textAlign: 'center' }}>
                <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '1rem' }}>
                    欢迎来到 Mods
                </h1>
                <p style={{ color: '#666', marginBottom: '2rem' }}>
                    已成功登录！
                </p>
            </div>
        </div>
    );
}




