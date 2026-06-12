export interface ApiResponse<T> {
    ResultCode: number;
    ResultMsg?: string;
    ResultData?: T;
}
