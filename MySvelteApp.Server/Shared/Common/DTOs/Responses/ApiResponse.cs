namespace MySvelteApp.Server.Shared.Common.DTOs.Responses;

public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
    public bool Success { get; set; } = true;
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

