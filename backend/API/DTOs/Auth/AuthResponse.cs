public record AuthResponse
{
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required UserResponse User { get; set; }

}