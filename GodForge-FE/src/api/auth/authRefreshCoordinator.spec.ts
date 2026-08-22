import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuthResponseDto } from './auth.models';
import {
    AuthRefreshCoordinator,
    AuthRefreshCoordinationUnavailableError,
    AuthRefreshInvalidatedError,
    AuthRefreshPeerFailedError,
    IndexedDbRefreshLeaseStore,
    type RefreshLeaseStore,
} from './authRefreshCoordinator';
import 'fake-indexeddb/auto';

class FakeBroadcastChannel {
    static channels = new Map<string, Set<FakeBroadcastChannel>>();
    private listener: ((event: MessageEvent) => void) | null = null;
    private readonly name: string;

    constructor(name: string) {
        this.name = name;
        const channels = FakeBroadcastChannel.channels.get(name) ?? new Set();
        channels.add(this);
        FakeBroadcastChannel.channels.set(name, channels);
    }

    addEventListener(_type: string, listener: (event: MessageEvent) => void) { this.listener = listener; }
    removeEventListener() { this.listener = null; }
    postMessage(data: unknown) {
        FakeBroadcastChannel.channels.get(this.name)?.forEach(channel => {
            if (channel !== this) queueMicrotask(() => channel.listener?.({ data } as MessageEvent));
        });
    }
    close() { FakeBroadcastChannel.channels.get(this.name)?.delete(this); }
}

class MemoryRefreshLeaseStore implements RefreshLeaseStore {
    private lease: { ownerId: string; expiresAt: number } | null = null;
    renewCalls = 0;

    seed(ownerId: string, expiresAt: number): void {
        this.lease = { ownerId, expiresAt };
    }

    async tryAcquire(ownerId: string, now: number, durationMs: number): Promise<boolean> {
        if (this.lease && this.lease.expiresAt > now && this.lease.ownerId !== ownerId) {
            return false;
        }
        this.lease = { ownerId, expiresAt: now + durationMs };
        return true;
    }

    async renew(ownerId: string, now: number, durationMs: number): Promise<boolean> {
        this.renewCalls += 1;
        if (this.lease?.ownerId !== ownerId) {
            return false;
        }
        this.lease = { ownerId, expiresAt: now + durationMs };
        return true;
    }

    async release(ownerId: string): Promise<void> {
        if (this.lease?.ownerId === ownerId) {
            this.lease = null;
        }
    }
}

const auth = (token: string): AuthResponseDto => ({
    accessToken: token,
    accessTokenExpiresAt: '2026-08-22T01:15:00Z',
    refreshTokenExpiresAt: '2026-09-22T01:00:00Z',
    user: { id: 'u1', email: 'a@example.com', displayName: 'A', status: 'active', emailVerifiedAt: null, createdAt: '2026-08-22T01:00:00Z', version: 1 },
    session: { id: 's1', deviceName: 'Chrome on Windows', createdAt: '2026-08-22T01:00:00Z', lastSeenAt: '2026-08-22T01:00:00Z', expiresAt: '2026-09-22T01:00:00Z', current: true, revokedAt: null },
});

describe('IndexedDbRefreshLeaseStore', () => {
    let store: IndexedDbRefreshLeaseStore;

    beforeEach(() => {
        store = new IndexedDbRefreshLeaseStore();
    });

    afterEach(async () => {
        const req = indexedDB.deleteDatabase('godforge-auth-coordination-v1');
        await new Promise((resolve) => {
            req.onsuccess = resolve;
            req.onerror = resolve;
        });
    });

    it('simultaneous competing acquisition: exactly one returns true', async () => {
        const results = await Promise.all([
            store.tryAcquire('owner-1', Date.now(), 5000),
            store.tryAcquire('owner-2', Date.now(), 5000)
        ]);
        expect(results).toContain(true);
        expect(results).toContain(false);
    });

    it('active owner: second owner cannot acquire before expiry', async () => {
        const now = Date.now();
        await store.tryAcquire('owner-1', now, 5000);
        const second = await store.tryAcquire('owner-2', now, 5000);
        expect(second).toBe(false);
    });

    it('non-owner cannot renew ownership', async () => {
        const now = Date.now();
        await store.tryAcquire('owner-1', now, 5000);
        const renewed = await store.renew('owner-2', now, 5000);
        expect(renewed).toBe(false);
    });

    it('non-owner release cannot remove owner lease', async () => {
        const now = Date.now();
        await store.tryAcquire('owner-1', now, 5000);
        await store.release('owner-2');
        const second = await store.tryAcquire('owner-3', now, 5000);
        expect(second).toBe(false);
    });

    it('owner release: second owner can acquire', async () => {
        const now = Date.now();
        await store.tryAcquire('owner-1', now, 5000);
        await store.release('owner-1');
        const second = await store.tryAcquire('owner-2', now, 5000);
        expect(second).toBe(true);
    });

    it('expired lease: another owner can acquire', async () => {
        const now = Date.now();
        await store.tryAcquire('owner-1', now, -100);
        const second = await store.tryAcquire('owner-2', now, 5000);
        expect(second).toBe(true);
    });

    it('persisted value contains only ownerId and expiresAt', async () => {
        await store.tryAcquire('owner-1', 1000, 5000);
        
        const db = await new Promise<IDBDatabase>((resolve) => {
            const req = indexedDB.open('godforge-auth-coordination-v1', 1);
            req.onsuccess = () => resolve(req.result);
        });

        const tx = db.transaction('refresh-leases', 'readonly');
        const req = tx.objectStore('refresh-leases').get('godforge-auth-refresh');
        
        const data = await new Promise((resolve) => {
            req.onsuccess = () => resolve(req.result);
        });

        expect(data).toEqual({ ownerId: 'owner-1', expiresAt: 6000 });
        db.close();
    });
});

describe('AuthRefreshCoordinator', () => {
    beforeEach(() => {
        vi.stubGlobal('BroadcastChannel', FakeBroadcastChannel);
        Object.defineProperty(navigator, 'locks', { configurable: true, value: undefined });
        localStorage.clear();
    });

    afterEach(() => {
        vi.useRealTimers();
        vi.unstubAllGlobals();
        FakeBroadcastChannel.channels.clear();
    });

    it('deduplicates ten refresh callers in one tab', async () => {
        const transport = vi.fn(async () => auth('token-1'));
        const coordinator = new AuthRefreshCoordinator(transport, new MemoryRefreshLeaseStore());

        const results = await Promise.all(Array.from({ length: 10 }, () => coordinator.refreshAccessToken()));

        expect(transport).toHaveBeenCalledTimes(1);
        expect(results.every(result => result.accessToken === 'token-1')).toBe(true);
        coordinator.dispose();
    });

    it('coordinates simultaneous initialization across five tabs with one rotation', async () => {
        let calls = 0;
        const transport = vi.fn(async () => {
            calls += 1;
            await new Promise(resolve => setTimeout(resolve, 10));
            return auth(`token-${calls}`);
        });
        const leases = new MemoryRefreshLeaseStore();
        const coordinators = Array.from({ length: 5 }, () => new AuthRefreshCoordinator(transport, leases));

        const results = await Promise.all(coordinators.map(coordinator => coordinator.refreshAccessToken()));

        expect(transport).toHaveBeenCalledTimes(1);
        expect(results.every(result => result.accessToken === 'token-1')).toBe(true);
        coordinators.forEach(coordinator => coordinator.dispose());
    });

    it('shares one leader failure across waiting tabs without a retry storm', async () => {
        const transport = vi.fn(async () => {
            await new Promise(resolve => setTimeout(resolve, 10));
            throw new Error('network unavailable');
        });
        const leases = new MemoryRefreshLeaseStore();
        const coordinators = Array.from({ length: 5 }, () => new AuthRefreshCoordinator(transport, leases));

        const results = await Promise.allSettled(coordinators.map(coordinator => coordinator.refreshAccessToken()));

        expect(transport).toHaveBeenCalledTimes(1);
        expect(results.every(result => result.status === 'rejected')).toBe(true);
        expect(results.filter(result => result.status === 'rejected').slice(1).every(result =>
            result.status === 'rejected' && result.reason instanceof AuthRefreshPeerFailedError)).toBe(true);
        coordinators.forEach(coordinator => coordinator.dispose());
    });

    it('recovers after an expired leader lease without deadlocking', async () => {
        const leases = new MemoryRefreshLeaseStore();
        leases.seed('crashed-tab', Date.now() + 100);
        const transport = vi.fn(async () => auth('recovered'));
        const coordinator = new AuthRefreshCoordinator(transport, leases);

        await expect(coordinator.refreshAccessToken()).resolves.toMatchObject({ accessToken: 'recovered' });
        expect(transport).toHaveBeenCalledTimes(1);
        coordinator.dispose();
    });

    it('renews a live leader lease while a slow refresh is in progress', async () => {
        vi.useFakeTimers();
        const transport = vi.fn(async () => {
            await new Promise(resolve => setTimeout(resolve, 8_000));
            return auth('slow-token');
        });
        const leases = new MemoryRefreshLeaseStore();
        const first = new AuthRefreshCoordinator(transport, leases);
        const second = new AuthRefreshCoordinator(transport, leases);

        const results = Promise.all([first.refreshAccessToken(), second.refreshAccessToken()]);
        await vi.advanceTimersByTimeAsync(8_100);

        await expect(results).resolves.toEqual([expect.objectContaining({ accessToken: 'slow-token' }), expect.objectContaining({ accessToken: 'slow-token' })]);
        expect(transport).toHaveBeenCalledTimes(1);
        expect(leases.renewCalls).toBeGreaterThan(0);
        first.dispose();
        second.dispose();
    });

    it('fails closed instead of refreshing without an atomic fallback lock', async () => {
        vi.stubGlobal('indexedDB', undefined);
        const transport = vi.fn(async () => auth('must-not-run'));
        const coordinator = new AuthRefreshCoordinator(transport);

        await expect(coordinator.refreshAccessToken()).rejects.toBeInstanceOf(AuthRefreshCoordinationUnavailableError);
        expect(transport).not.toHaveBeenCalled();
        coordinator.dispose();
    });

    it('propagates logout and rejects a refresh result that completed after invalidation', async () => {
        let release!: (value: AuthResponseDto) => void;
        const leases = new MemoryRefreshLeaseStore();
        const leader = new AuthRefreshCoordinator(() => new Promise(resolve => { release = resolve; }), leases);
        const otherTab = new AuthRefreshCoordinator(async () => auth('unused'), leases);
        const received = vi.fn();
        otherTab.subscribe(received);

        const pending = leader.refreshAccessToken();
        otherTab.publishCleared('logout');
        await new Promise(resolve => setTimeout(resolve, 0));
        release(auth('stale-token'));

        await expect(pending).rejects.toBeInstanceOf(AuthRefreshInvalidatedError);
        expect(received).toHaveBeenCalledWith(expect.objectContaining({ kind: 'cleared', reason: 'logout' }));
        leader.dispose();
        otherTab.dispose();
    });

    it('rejects a stale refresh even before the asynchronous clear broadcast is delivered', async () => {
        let queuedBroadcast: (() => void) | undefined;
        vi.spyOn(FakeBroadcastChannel.prototype, 'postMessage').mockImplementation(function (this: FakeBroadcastChannel, data: unknown) {
            queuedBroadcast = () => FakeBroadcastChannel.channels.get('godforge-auth-v1')?.forEach(channel => {
                if (channel !== this) (channel as unknown as { listener: ((event: MessageEvent) => void) | null }).listener?.({ data } as MessageEvent);
            });
        });
        let release!: (value: AuthResponseDto) => void;
        const leases = new MemoryRefreshLeaseStore();
        const leader = new AuthRefreshCoordinator(() => new Promise(resolve => { release = resolve; }), leases);
        const otherTab = new AuthRefreshCoordinator(async () => auth('unused'), leases);

        const pending = leader.refreshAccessToken();
        otherTab.publishCleared('logout');
        await new Promise(resolve => setTimeout(resolve, 50));
        release(auth('stale-token'));

        await expect(pending).rejects.toBeInstanceOf(AuthRefreshInvalidatedError);
        queuedBroadcast?.();
        leader.dispose();
        otherTab.dispose();
    });
});
