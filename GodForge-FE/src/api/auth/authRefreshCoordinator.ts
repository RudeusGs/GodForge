import axios from 'axios';
import type { ApiResponse } from '../api.models';
import type { AuthResponseDto } from './auth.models';

const CHANNEL_NAME = 'godforge-auth-v1';
const LOCK_NAME = 'godforge-auth-refresh';
const LEASE_DB_NAME = 'godforge-auth-coordination-v1';
const LEASE_STORE_NAME = 'refresh-leases';
const EVENT_EPOCH_KEY = 'godforge_auth_event_epoch';
const CLEAR_EPOCH_KEY = 'godforge_auth_clear_epoch';
const LEASE_DURATION_MS = 35_000;
const LEASE_HEARTBEAT_MS = 2_500;
const WAIT_TIMEOUT_MS = 45_000;

export type AuthEvent = {
    id: string;
    epoch: number;
    issuedAt: number;
    kind: 'authenticated' | 'cleared' | 'refresh-failed';
    auth?: AuthResponseDto;
    reason?: string;
};

type RefreshTransport = () => Promise<AuthResponseDto>;
type AuthEventListener = (event: AuthEvent) => void;

type RefreshLease = {
    ownerId: string;
    expiresAt: number;
};

export interface RefreshLeaseStore {
    tryAcquire(ownerId: string, now: number, durationMs: number): Promise<boolean>;
    renew(ownerId: string, now: number, durationMs: number): Promise<boolean>;
    release(ownerId: string): Promise<void>;
}

export class AuthRefreshInvalidatedError extends Error {
    constructor() {
        super('Authentication was cleared while refresh was in progress.');
        this.name = 'AuthRefreshInvalidatedError';
    }
}

export class AuthRefreshPeerFailedError extends Error {
    constructor() {
        super('Another tab failed to refresh authentication.');
        this.name = 'AuthRefreshPeerFailedError';
    }
}

export class AuthRefreshCoordinationUnavailableError extends Error {
    constructor(options?: ErrorOptions) {
        super('Cross-tab authentication refresh coordination is unavailable.', options);
        this.name = 'AuthRefreshCoordinationUnavailableError';
    }
}

export function isDefinitiveAuthInvalidation(error: unknown): boolean {
    if (error instanceof AuthRefreshInvalidatedError) {
        return true;
    }
    if (axios.isAxiosError(error) && error.response?.status === 401) {
        return true;
    }
    return false;
}

export class IndexedDbRefreshLeaseStore implements RefreshLeaseStore {
    private databasePromise: Promise<IDBDatabase> | null = null;

    async tryAcquire(ownerId: string, now: number, durationMs: number): Promise<boolean> {
        return this.mutateLease((current, store) => {
            if (current && current.expiresAt > now && current.ownerId !== ownerId) {
                return false;
            }

            store.put({ ownerId, expiresAt: now + durationMs } satisfies RefreshLease, LOCK_NAME);
            return true;
        });
    }

    async renew(ownerId: string, now: number, durationMs: number): Promise<boolean> {
        return this.mutateLease((current, store) => {
            if (current?.ownerId !== ownerId) {
                return false;
            }

            store.put({ ownerId, expiresAt: now + durationMs } satisfies RefreshLease, LOCK_NAME);
            return true;
        });
    }

    async release(ownerId: string): Promise<void> {
        await this.mutateLease((current, store) => {
            if (current?.ownerId === ownerId) {
                store.delete(LOCK_NAME);
            }
            return undefined;
        });
    }

    private async mutateLease<T>(mutate: (current: RefreshLease | null, store: IDBObjectStore) => T): Promise<T> {
        const database = await this.getDatabase();
        return new Promise<T>((resolve, reject) => {
            const transaction = database.transaction(LEASE_STORE_NAME, 'readwrite');
            const store = transaction.objectStore(LEASE_STORE_NAME);
            const request = store.get(LOCK_NAME);
            let result: T;
            let operationError: unknown;

            request.onsuccess = () => {
                try {
                    result = mutate((request.result as RefreshLease | undefined) ?? null, store);
                } catch (error) {
                    operationError = error;
                    transaction.abort();
                }
            };
            request.onerror = () => {
                operationError = request.error;
            };
            transaction.oncomplete = () => resolve(result!);
            transaction.onabort = () => reject(operationError ?? transaction.error ?? new Error('Authentication lease transaction was aborted.'));
            transaction.onerror = () => {
                operationError ??= transaction.error;
            };
        });
    }

    private getDatabase(): Promise<IDBDatabase> {
        if (this.databasePromise) {
            return this.databasePromise;
        }
        if (typeof indexedDB === 'undefined') {
            return Promise.reject(new Error('IndexedDB is unavailable.'));
        }

        this.databasePromise = new Promise<IDBDatabase>((resolve, reject) => {
            const request = indexedDB.open(LEASE_DB_NAME, 1);
            request.onupgradeneeded = () => {
                const database = request.result;
                if (!database.objectStoreNames.contains(LEASE_STORE_NAME)) {
                    database.createObjectStore(LEASE_STORE_NAME);
                }
            };
            request.onsuccess = () => {
                const database = request.result;
                database.onversionchange = () => {
                    database.close();
                    this.databasePromise = null;
                };
                resolve(database);
            };
            request.onerror = () => {
                this.databasePromise = null;
                reject(request.error ?? new Error('Unable to open the authentication coordination database.'));
            };
        });

        return this.databasePromise;
    }
}

function eventKindRank(kind: AuthEvent['kind']): number {
    if (kind === 'cleared') return 2;
    if (kind === 'authenticated') return 1;
    return 0;
}

function compareEvents(left: AuthEvent | null, right: AuthEvent): number {
    if (!left) return -1;
    if (left.epoch !== right.epoch) return left.epoch - right.epoch;
    if (left.issuedAt !== right.issuedAt) return left.issuedAt - right.issuedAt;
    if (left.kind !== right.kind) return eventKindRank(left.kind) - eventKindRank(right.kind);
    return left.id.localeCompare(right.id);
}

export class AuthRefreshCoordinator {
    private readonly ownerId = crypto.randomUUID();
    private readonly listeners = new Set<AuthEventListener>();
    private readonly channel: BroadcastChannel | null;
    private latestEvent: AuthEvent | null = null;
    private refreshPromise: Promise<AuthResponseDto> | null = null;
    private readonly transport: RefreshTransport;
    private readonly leaseStore: RefreshLeaseStore;

    constructor(transport: RefreshTransport, leaseStore: RefreshLeaseStore = new IndexedDbRefreshLeaseStore()) {
        this.transport = transport;
        this.leaseStore = leaseStore;
        this.channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
        this.channel?.addEventListener('message', this.receiveMessage);
    }

    subscribe(listener: AuthEventListener): () => void {
        this.listeners.add(listener);
        return () => this.listeners.delete(listener);
    }

    publishAuthenticated(auth: AuthResponseDto): void {
        this.publish({
            id: crypto.randomUUID(),
            epoch: this.nextEventEpoch(),
            issuedAt: Date.now(),
            kind: 'authenticated',
            auth,
        });
    }

    publishCleared(reason: string): void {
        const event = {
            id: crypto.randomUUID(),
            epoch: this.nextEventEpoch(),
            issuedAt: Date.now(),
            kind: 'cleared',
            reason,
        } satisfies AuthEvent;
        this.persistClearEpoch(event.epoch);
        this.publish(event);
    }

    refreshAccessToken(): Promise<AuthResponseDto> {
        if (this.refreshPromise) return this.refreshPromise;

        const eventAtStart = this.latestEvent;
        const clearEpochAtStart = this.readClearEpoch();
        this.refreshPromise = this.withCrossTabLock(async () => {
            const coordinatedResult = this.resultPublishedAfter(eventAtStart);
            if (coordinatedResult) return coordinatedResult;

            let auth: AuthResponseDto;
            try {
                auth = await this.transport();
            } catch (error) {
                this.publishRefreshFailed();
                throw error;
            }
            if (this.wasClearedAfter(eventAtStart) || this.readClearEpoch() > clearEpochAtStart) {
                throw new AuthRefreshInvalidatedError();
            }
            this.publishAuthenticated(auth);
            return auth;
        }).finally(() => {
            this.refreshPromise = null;
        });

        return this.refreshPromise;
    }

    dispose(): void {
        this.channel?.removeEventListener('message', this.receiveMessage);
        this.channel?.close();
        this.listeners.clear();
    }

    private readonly receiveMessage = (message: MessageEvent<AuthEvent>): void => {
        const event = message.data;
        if (!event || !['authenticated', 'cleared', 'refresh-failed'].includes(event.kind)) return;
        this.accept(event);
    };

    private publishRefreshFailed(): void {
        this.publish({
            id: crypto.randomUUID(),
            epoch: this.nextEventEpoch(),
            issuedAt: Date.now(),
            kind: 'refresh-failed',
            reason: 'refresh-failed',
        });
    }

    private publish(event: AuthEvent): void {
        this.accept(event);
        this.channel?.postMessage(event);
    }

    private accept(event: AuthEvent): void {
        if (compareEvents(this.latestEvent, event) >= 0) return;
        this.latestEvent = event;
        this.listeners.forEach(listener => listener(event));
    }

    private resultPublishedAfter(eventAtStart: AuthEvent | null): AuthResponseDto | null {
        if (this.latestEvent === eventAtStart) return null;
        if (this.latestEvent?.kind === 'cleared') throw new AuthRefreshInvalidatedError();
        if (this.latestEvent?.kind === 'refresh-failed') throw new AuthRefreshPeerFailedError();
        return this.latestEvent?.auth ?? null;
    }

    private wasClearedAfter(eventAtStart: AuthEvent | null): boolean {
        return this.latestEvent !== eventAtStart && this.latestEvent?.kind === 'cleared';
    }

    private async withCrossTabLock<T>(action: () => Promise<T>): Promise<T> {
        if (typeof navigator !== 'undefined' && navigator.locks) {
            return navigator.locks.request(LOCK_NAME, { mode: 'exclusive' }, action);
        }

        return this.withAtomicLease(action);
    }

    private async withAtomicLease<T>(action: () => Promise<T>): Promise<T> {
        const deadline = Date.now() + WAIT_TIMEOUT_MS;
        while (Date.now() < deadline) {
            let acquired: boolean;
            try {
                acquired = await this.leaseStore.tryAcquire(this.ownerId, Date.now(), LEASE_DURATION_MS);
            } catch (error) {
                throw new AuthRefreshCoordinationUnavailableError({ cause: error });
            }

            if (acquired) {
                const heartbeat = window.setInterval(() => {
                    void this.leaseStore.renew(this.ownerId, Date.now(), LEASE_DURATION_MS).catch(() => {
                        // The long lease remains valid beyond the refresh transport timeout. If the
                        // coordination store is unavailable, waiters fail closed instead of bypassing it.
                    });
                }, LEASE_HEARTBEAT_MS);
                try {
                    return await action();
                } finally {
                    window.clearInterval(heartbeat);
                    try {
                        await this.leaseStore.release(this.ownerId);
                    } catch {
                        // The bounded lease expires even if cleanup cannot reach IndexedDB.
                    }
                }
            }

            await new Promise(resolve => window.setTimeout(resolve, 50));
        }

        throw new Error('Timed out waiting for the authentication refresh coordinator.');
    }

    private nextEventEpoch(): number {
        if (typeof localStorage === 'undefined') return Date.now();
        try {
            const next = Math.max(Date.now(), Number(localStorage.getItem(EVENT_EPOCH_KEY) ?? 0) + 1);
            localStorage.setItem(EVENT_EPOCH_KEY, String(next));
            return next;
        } catch {
            return Date.now();
        }
    }

    private readClearEpoch(): number {
        if (typeof localStorage === 'undefined') return 0;
        try {
            return Number(localStorage.getItem(CLEAR_EPOCH_KEY) ?? 0);
        } catch {
            return 0;
        }
    }

    private persistClearEpoch(epoch: number): void {
        if (typeof localStorage === 'undefined') return;
        try {
            localStorage.setItem(CLEAR_EPOCH_KEY, String(epoch));
        } catch {
            // BroadcastChannel still propagates the invalidation when storage is unavailable.
        }
    }
}

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5072/api/v1';

export const authRefreshCoordinator = new AuthRefreshCoordinator(async () => {
    const response = await axios.post<ApiResponse<AuthResponseDto>>(
        `${baseURL}/auth/refresh`,
        undefined,
        { timeout: 30_000, withCredentials: true },
    );
    return response.data.data;
});
