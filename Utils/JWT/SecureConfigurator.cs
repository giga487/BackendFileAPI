namespace Utils.JWTAuthentication
{
    public class JWTSecureConfiguration
    {
        public string MyKey { get; set; } = "pippo";
        public string Issuer { get; set; } = "topolino";
        public string Audience { get; set; } = "pluto";
        public string Subject { get; set; } = "paperino";
    }
}
