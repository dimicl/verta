public class User 
{
    public Guid Id { get; set; }
    
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public UserRole Role { get; set; }
    public string Status { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
}