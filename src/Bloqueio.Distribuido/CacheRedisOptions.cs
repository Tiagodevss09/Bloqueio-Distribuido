namespace Bloqueio.Distribuido
{
    public sealed class CacheRedisOptions
    {
        public string Host { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string Format { get; set; } = string.Empty;

        public string GetConnectionString()
        {
            return string.Format(Format, Host, Password);
        }
    }
}
