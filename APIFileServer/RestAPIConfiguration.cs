using APIFileServer.source;
//using JWTAuthentication;
using Utils.FileHelper;
using Utils.JWTAuthentication;
using Serilog;
using Newtonsoft.Json;
using System.Security.Principal;
using System.Text.Unicode;

namespace APIFileServer
{
    public class RestAPIConfiguration
    {
        Serilog.ILogger? _logger;
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
        public string ChunksMainFolder { get; private set; } = string.Empty;
        public bool ChunksIsOK { get; private set; } = true;
        public int MaxChunkSize { get; private set; } = 50 * 1024;
        public string PhysicalFileRoot { get; private set; } = string.Empty;
        public int MaxCacheRam { get; private set; } = 500*1024*1024;
        public bool CompressedChunks { get; private set; } = false;
        public bool JWTIsEnabled { get; private set; } = true;
        public string MD5File { get; private set; } = string.Empty;
        public bool CheckIfOldFilesAreTheSame { get; set; } = true; //is not so implemented, the effort is right?
        public bool FilesAreChanged { get; set; } = true;
        public RestAPIConfiguration(ConfigurationManager config, Serilog.ILogger? logger)
        {
            _logger = logger;

            var sharedFileConf = config.GetSection("SharedFile");
            SharedFilePath = sharedFileConf.GetValue<string>("ClientFilesPath") ?? string.Empty;

            if (SharedFilePath == string.Empty)
            {
                _logger?.Error("No file path to share");
                throw new ArgumentException("No file path to share");
            }
            else
            {
                SharedFiles = Directory.EnumerateFiles(SharedFilePath, searchPattern: "*", SearchOption.AllDirectories).ToList();
                foreach (string fileToShare in SharedFiles)
                {
                    _logger?.Information($"FILE SHARED: {fileToShare}");
                }

            }

            try
            {
                MD5File = sharedFileConf.GetValue<string>("MD5FileList") ?? string.Empty;
            }
            catch
            {
                CheckIfOldFilesAreTheSame = false;
            }

            OpenFileProvider = sharedFileConf.GetValue<bool>("OpenFileProvider");
            Host = sharedFileConf.GetValue<string>("Host") ?? string.Empty;

            _logger?.Information($"HOST: {Host}");

            if (Host == string.Empty)
            {
                Host = "Localhost";
            }

            UrlFileProvider = sharedFileConf.GetValue<string>("URLFileProvider") ?? string.Empty;
            _logger?.Information($"URLFileProvider: {Host}");

            if (UrlFileProvider == string.Empty)
            {
                _logger?.Error($"No file path name in configuration: \'URLFileProvider\'");
                throw new ArgumentException($"No file path name in configuration: \'URLFileProvider\'");
            }


            PhysicalFileRoot = sharedFileConf.GetValue<string>("PhysicalProvider") ?? string.Empty;
            ChunksMainFolder = sharedFileConf.GetValue<string>("ChunksMainFolder") ?? string.Empty;
            CompressedChunks = sharedFileConf.GetValue<bool>("Compressed");

            _logger?.Information($"Physical Provider: {PhysicalFileRoot}");
            _logger?.Information($"Chunks Folder: {ChunksMainFolder}");
            _logger?.Information($"FILE TO COMPRESS: {CompressedChunks}");

            if (ChunksMainFolder == string.Empty)
            {
                ChunksIsOK = false;

            }
            else
            {
                MaxChunkSize = sharedFileConf.GetValue<int>("MaxChunkSize");
                _logger?.Information($"MAX CHUNK SIZE: {MaxChunkSize}");
            }

            MaxCacheRam = sharedFileConf.GetValue<int>("MaxCacheRam");
            _logger?.Information($"MAX RAM USABLE: {MaxCacheRam}");

            var jwtConfig = config.GetSection("JWTSecureData");

            if (jwtConfig is null)
                throw new ArgumentException(message: "No jwt token secure data");

            JWTConfig = new JWTSecureConfiguration();
            JWTIsEnabled = jwtConfig.GetValue<bool>("IsEnabled");

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
            if (CheckIfOldFilesAreTheSame)
            {
                FileList = new FileList(SharedFilePath).AddFiles();

                //var filelistLoaded = JsonConvert.DeserializeObject<FileList>(MD5File);
                //_logger?.Information($"Checking are the same files");

                string json = JsonConvert.SerializeObject(FileList, Formatting.Indented);
                File.WriteAllTextAsync(MD5File, json);
            }

            return this;
        }

        public RestAPIConfiguration MakeChunksFiles()
        {

            if (FilesAreChanged)
            {
                FileList?.MakeChunksFiles(ChunksMainFolder, MaxChunkSize, CompressedChunks);
                //});

                int amountOfChunks = FileList?.GetAmountOfFile() ?? 0;

                _logger?.Information($"CHUNK CREATED: {amountOfChunks}");
            }
            else
            {
                _logger?.Warning($"CHUNK NOT CREATED, no changes");
            }

            return this;
        }

        public RestAPIConfiguration? FillCache(RestAPIFileCache memoryCache)
        {
            if(FileList == null)
            {
                return null;
            }

            foreach(var keyValFileShared in FileList.FilesDict)
            {
                var fileShared = keyValFileShared.Value;

                if(fileShared != null)
                {
                    for(int i = 0; i < fileShared.Chunks.ChunksList.Count; i++)
                    {
                        ApiFileInfo objToSend = fileShared.Chunks.ChunksList.ElementAt(i);

                        using (var stream = new FileStream(objToSend.Filename, FileMode.Open))
                        {
                            var memoryStream = new MemoryStream();
                            if ((int)stream.Length > 0)
                            {
                                stream.CopyTo(memoryStream, (int)stream.Length);
                                memoryCache.AddMemory(objToSend.Filename, memoryStream.GetBuffer());
                            }
                            else
                            {
                                _logger?.Information($"{objToSend.Filename} has L: {(int)stream.Length}");
                            }
                        }
                    }
                }
            }

            return this;
        }
    }
}
