namespace JWTAuthentication
{
    public class UserInfo
    {
        public int UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class UserList
    {
        public List<UserInfo> Users = new List<UserInfo>()
        {
            new UserInfo(){UserName = "Gigi", Email = "gigi@gmail.com", CreatedDate = DateTime.Now, DisplayName = "Gigi"},
            new UserInfo(){UserName = "Gigi1", Email = "gig2@gmail.com", CreatedDate = DateTime.Now, DisplayName = "Gigi" },
            new UserInfo(){UserName = "Gigi3", Email = "gigi3@gmail.com", CreatedDate = DateTime.Now, DisplayName = "Gigi" },
            new UserInfo(){UserName = "Gigi2", Email = "gigi4@gmail.com", CreatedDate = DateTime.Now , DisplayName = "Gigi"},
            new UserInfo(){UserName = "Gigi4", Email = "gigi5@gmail.com", CreatedDate = DateTime.Now, DisplayName = "Gigi" },
            new UserInfo(){UserName = "Gigi5", Email = "gigi6@gmail.com", CreatedDate = DateTime.Now, DisplayName = "Gigi" },
        };
    }
}
