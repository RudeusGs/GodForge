export interface ApiMeta {
    correlationId?: string;
    page?: number;
    pageSize?: number;
    totalItems?: number;
    totalPages?: number;
}

export interface ApiResponse<T = unknown> {
    data: T;
    meta?: ApiMeta;
}
