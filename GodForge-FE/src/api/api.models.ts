export interface ApiMeta {
    correlationId?: string;
    page?: number;
    pageSize?: number;
    totalCount?: number;
}

export interface ApiResponse<T = unknown> {
    data: T;
    meta?: ApiMeta;
}
