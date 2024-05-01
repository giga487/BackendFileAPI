namespace JWTAuthentication
{
    public class SecureConfigurator
    {
        public string MyKey { get; set; } = "Yh2k7QSu4l8CZg5p6X3Pna9L0Miy4D3Bvt0JVr87UcOj69Kqw5R2Nmf4FWs03Hdx";
        public string Issuer { get; set; } = "JWTAuthenticationServer";
        public string Audience { get; set; } = "JWTServicePostmanClient";
        public string Subject { get; set; } = "JWTServiceAccessToken";
    }
}
