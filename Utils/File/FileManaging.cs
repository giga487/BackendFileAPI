using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Utils.FileHelper.FileHelper;

namespace Utils.FileHelper
{
    public partial class FileHelper
    {
        public class ChunkFile
        {
            public List<ApiFileInfo> ChunksList { get; set; } = new List<ApiFileInfo>();
            public string FileName { get; private set; } = null;
            public string Where { get; private set; } = null;
            public int MaxLengthByte { get; private set; } = 500;
            public ApiFileInfo OriginalFileInfo { get; private set; } = null;
            public FileInfo FileInfo { get; private set; } = null;
            public bool Completed { get; private set; } = false;
            public ChunkFile(string originalFile, string whereToPutThese, int maxLengthByte)
            {
                if(!File.Exists(originalFile))
                {
                    throw new FileNotFoundException(message:$"{originalFile} not exist");
                }

                FileInfo = new FileInfo(originalFile);
                string md5 = FileHelper.Md5Result(originalFile);
                OriginalFileInfo = new ApiFileInfo(FileInfo.FullName, md5, FileInfo.Length);

                Where = whereToPutThese;
                FileName = originalFile;
                MaxLengthByte = maxLengthByte;

                CreateChunks();
            }

            public List<ApiFileInfo> CreateChunks()
            {
                ChunksList = CreateChunks(FileName, Where, MaxLengthByte, true);
                Completed = true;

                return ChunksList;
            }

            public static List<ApiFileInfo> CreateChunks(string filename, string whereToPut, int maxLengthByte, bool compression = false)
            {
                List<ApiFileInfo> chunks = new List<ApiFileInfo>();

                long fileSize;
                FileInfo f = new FileInfo(filename);

                byte[] buffer;
                if (compression)
                {
                    buffer = FileHelper.Compress(filename).Result;
                    fileSize = buffer.LongLength;
                }
                else
                {
                    fileSize = f.Length;
                }

                string fileBaseName = string.Empty;
                string modifiedName = string.Empty;

                if (f.Extension != "")
                {
                    fileBaseName = f.Name.Replace(f.Extension, "");
                    modifiedName = $"{fileBaseName}_{f.Extension.Replace(".", "")}";
                }
                else
                {
                    modifiedName = $"{f.Name}_folder";
                }

                string newFolder = Path.Combine(whereToPut, modifiedName);

                if(!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }

                if (fileSize > maxLengthByte)
                {
                    using (Stream input = File.OpenRead(filename))
                    {
                        int index = 0;
                        int remaining = (int)input.Length;

                        while (input.Position < input.Length)
                        {
                            string newfile = Path.Combine(newFolder, $"{fileBaseName}_{index}{f.Extension}");

                            int sizeToCopy = Math.Min(remaining, maxLengthByte);
                            buffer = new byte[sizeToCopy];

                            var bytesRead = input.Read(buffer, 0, sizeToCopy);
                            string md5New = Md5Result(buffer);

                            if (!File.Exists(newfile)) //forse esiste gia il chunk
                            {
                                using (Stream output = File.Create(newfile))
                                {
                                    //while (remaining > 0 && bytesRead > 0)
                                    //{
                                        output.Write(buffer, 0, bytesRead);
                                        remaining -= bytesRead;
                                    //}
                                }
                            }
                            else
                            {
                                string md5Old = Md5Result(newfile);

                                if (md5Old != md5New)
                                {
                                    using (Stream output = File.Create(newfile))
                                    {
                                        while (remaining > 0 && bytesRead > 0)
                                        {
                                            output.Write(buffer, 0, bytesRead);
                                            remaining -= bytesRead;
                                        }
                                    }
                                }
                                else
                                {

                                }
                            }

                            index++;

                            ApiFileInfo info = new ApiFileInfo(newfile, md5New, buffer.Length);
                            chunks.Add(info);
                        }
                    }
                }
                else
                {
                    FileInfo fInfo = new FileInfo(filename);
                    string md5new = Md5Result(filename);
                    ApiFileInfo info = new ApiFileInfo(filename, md5new, fInfo.Length);

                    chunks.Add(info);
                }

                return chunks;
            }
        }
    }
}
