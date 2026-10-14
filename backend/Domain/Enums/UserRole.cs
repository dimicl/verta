public enum UserRole
{
    Owner,
    /// <summary>Full workspace access — all boards.</summary>
    Member,
    /// <summary>
    /// Board-scoped member (same edit rights as Member, only invited boards).
    /// Exposed as Member in API responses; kept for access scoping only.
    /// </summary>
    Guest
}