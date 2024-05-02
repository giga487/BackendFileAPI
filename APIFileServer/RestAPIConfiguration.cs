using Utils.FileHelper;

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
    }
}
