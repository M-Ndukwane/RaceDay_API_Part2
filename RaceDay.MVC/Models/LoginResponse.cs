namespace RaceDay.MVC.Models
{
    public class LoginResponse
    {
        public string Message { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public LoginUser User { get; set; } = new();
    }

    public class LoginUser
    {
        public int UsersID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
