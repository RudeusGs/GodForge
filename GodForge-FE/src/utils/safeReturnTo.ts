export function safeReturnTo(value: unknown): string | null {
    if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return null;
    try {
        const origin = typeof window === 'undefined' ? 'https://godforge.invalid' : window.location.origin;
        const target = new URL(value, origin);
        return target.origin === origin ? `${target.pathname}${target.search}${target.hash}` : null;
    } catch {
        return null;
    }
}
