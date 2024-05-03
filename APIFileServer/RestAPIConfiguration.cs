using JWTAuthentication;
using Utils.FileHelper;
using Utils.JWTAuthentication;

namespace APIFileServer
{
    public class RestAPIConfiguration
    {
        public int HTTPPort { get; private set; } = 5000;
        public int HTTPSPort { get; private set; } = 5001;
        public int RestTImeOutMS { get; private set; } = 10000;
        public string SharedFilePath { get; private set; } = string.Empty;
        public bool OpenFileProvider { get; private set; } = false;
        public string Host { get; private set; } = string.Empty;
        public string UrlFileProvider { get; private set; } = string.Empty;
        public List<string> SharedFiles { get; private set; } = new List<string>();
        public FileList? FileList { get; private set; } = null;
        public JWTSecureConfiguration? JWTConfig {get; private set;} = null;
        public string ChunksMainFolder { get; private set; } = null;
        public bool ChunksIsOK { get; private set; } = true;
        public int MaxChunkSize { get; private set; } = 50 * 1024;
        public RestAPIConfiguration(ConfigurationManager config)
        {
            var sharedFileConf = config.GetSection("SharedFile");
            SharedFilePath = sharedFileConf.GetValue<string>("ClientFilesPath") ?? string.Empty;

            if (SharedFilePath == string.Empty)
            {
                throw new ArgumentException("No file path to share");
            }
            else
            {
                SharedFiles = Directory.EnumerateFiles(SharedFilePath, searchPattern: "*", SearchOption.AllDirectories).ToList();

            }

            OpenFileProvider = sharedFileConf.GetValue<bool>("OpenFileProvider");
            Host = sharedFileConf.GetValue<string>("Host") ?? string.Empty;

            if (Host == string.Empty)
            {
                Host = "Localhost";
            }

            UrlFileProvider = sharedFileConf.GetValue<string>("URLFileProvider") ?? string.Empty;

            if (UrlFileProvider == string.Empty)
            {
                throw new ArgumentException($"No file path name in configuration: \'URLFileProvider\'");
            }

            ChunksMainFolder = sharedFileConf.GetValue<string>("ChunksMainFolder") ?? string.Empty;

            if (ChunksMainFolder == string.Empty)
            {
                ChunksIsOK = false;

            }
            else
            {
                MaxChunkSize = sharedFileConf.GetValue<int>("MaxChunkSize");
            }

            var jwtConfig = config.GetSection("JWTSecureData");

            if (jwtConfig is null)
                throw new ArgumentException(message: "No jwt token secure data");

            JWTConfig = new JWTSecureConfiguration();

            JWTConfig.MyKey = jwtConfig.GetValue<string>("MyKey") ?? string.Empty;
            JWTConfig.Issuer = jwtConfig.GetValue<string>("Issuer") ?? string.Empty;
            JWTConfig.Audience = jwtConfig.GetValue<string>("Audience") ?? string.Empty;
            JWTConfig.Subject = jwtConfig.GetValue<string>("Subject") ?? string.Empty;
        }

        public async Task<RestAPIConfiguration> CreateFileListAsync()
        {
            FileList = await new FileList(SharedFilePath).AddFilesAsync();

            return this;
        }

        public RestAPIConfiguration CreateFileList()
        {
            FileList = new FileList(SharedFilePath).AddFiles();

            return this;
        }

        public RestAPIConfiguration MakeChunksFiles()
        {
            //Task.Run(() =>
            //{
                FileList?.MakeChunks(ChunksMainFolder, MaxChunkSize);
            //});

            return this;
        }
    }
}
