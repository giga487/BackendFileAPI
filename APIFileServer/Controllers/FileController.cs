using APIFileServer.source;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Net.Mime;
using Utils.FileHelper;

namespace APIFileServer.Controllers
{
    [Route("api/[controller]/[action]/")]
    public class FileController : Controller
    {
        private IFileProvider? _fileProvider { get; set; } = null;
        private FileList? _files { get; set; } = null;
        private RestAPIFileCache _memoryCache { get; set; } = null;

        public FileController(IFileProvider? fileProvider, FileList list, RestAPIFileCache cache)
        {
            _fileProvider = fileProvider;
            _files = list;
            _memoryCache = cache;
        }

        //https://localhost:7006/api/File/List
        [HttpGet]
        [Authorize]
        public IActionResult List()
        {
            if (_fileProvider is PhysicalFileProvider physicalFileProvider)
            {
                var list = _files?.MinimalApiDict.Values.ToList();

                if (list?.Count == 0)
                {
                    return new BadRequestResult();
                }
                else
                    return Ok(list);
            }

            return new BadRequestResult();
        }


        //http://localhost:5009/MD5?filename=test.txt
        //[HttpGet("[controller]/[action]/{filename}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MD5(string? filename)
        {
            string md5 = string.Empty;

            if (_fileProvider is PhysicalFileProvider physicalFileProvider && filename is not null)
            {
                string path = physicalFileProvider.Root;

                md5 = await FileHelper.Md5ResultAsync(filename, path);

                if (string.IsNullOrEmpty(md5))
                {
                    return new BadRequestResult();
                }
                else
                    return Ok(md5);
            }

            return new BadRequestResult();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CacheRemainingSize()
        {
            if (_memoryCache is not null)
            {
                return Ok(_memoryCache.MaxMemory - _memoryCache.MemorySize);
            }
            else
                return new BadRequestResult();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CacheItems()
        {
            List<string> cacheItems = new List<string>();
            if (_memoryCache is not null)
            {
                return Ok(_memoryCache.Items);
            }
            else
                return new BadRequestResult();
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadFileByChunks(string fileName, int id)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == null || _files is null)
            {
                return Content("File Name is Empty...");
            }

            // get the filePath
            if (_fileProvider is PhysicalFileProvider filProviderPhysical)
            {
                string path = filProviderPhysical.Root;

                if (!_files.FilesDict.TryGetValue(fileName, out FileInfoToShare? file) && file?.FileInfo != null)
                {
                    return new BadRequestResult();
                }

                if (file is null || file.FileInfo is null || !file.Chunks.Completed)
                    return new BadRequestResult();

                try
                {
                    ApiFileInfo objToSend = file.Chunks.ChunksList.ElementAt(id);

                    if (!_memoryCache.Get(objToSend.Filename, out byte[]? memoryBuffer))
                    {
                        using (var stream = new FileStream(objToSend.Filename, FileMode.Open))
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream, (int)stream.Length);
                                if (_memoryCache.AddMemory(objToSend.Filename, memoryStream.GetBuffer()))
                                {
                                    Console.WriteLine($"inserted in cache {objToSend.Filename}");
                                    _memoryCache.Get(objToSend.Filename, out memoryBuffer);
                                }
                                else
                                {
                                    Console.WriteLine($"---- taken by hdd {objToSend.Filename}");
                                    memoryBuffer = memoryStream.GetBuffer(); //if the data is not written in cache
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"taken by cache {objToSend.Filename}");
                        _memoryCache.Get(objToSend.Filename, out memoryBuffer);
                    }

                    if (memoryBuffer != null)
                    {
                        if (new FileExtensionContentTypeProvider().TryGetContentType(objToSend.Filename, out string? contentType))
                        {
                            return File(memoryBuffer, contentType);
                        }
                        else
                        {
                            return File(memoryBuffer, "application/octet-stream");
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new BadRequestResult();
                }

                return new BadRequestResult();
            }
            else
                return new BadRequestResult();
        }
        [Authorize]
        private FileStreamResult? FileStreamUpload(string filename, MemoryStream memoryStream)
        {
            try
            {
                if (new FileExtensionContentTypeProvider().TryGetContentType(filename, out string contentType))
                {
                    // set the position to return the file from
                    memoryStream.Position = 0;

                    return new FileStreamResult(memoryStream, contentType)
                    {
                        FileDownloadName = filename
                    };
                }
                else
                {
                    memoryStream.Position = 0;

                    //Console.WriteLine($"Request of {objToSend.Filename}");
                    return new FileStreamResult(memoryStream, "application/octet-stream")
                    {
                        FileDownloadName = filename
                    };

                }
            }
            catch
            {
                return null;
            }
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == null || _files is null)
            {
                return Content("File Name is Empty...");
            }

            // get the filePath
            if (_fileProvider is PhysicalFileProvider filProviderPhysical)
            {
                string path = filProviderPhysical.Root;

                if(!_files.FilesDict.TryGetValue(fileName, out FileInfoToShare? file) && file?.FileInfo != null)
                {
                    return new BadRequestResult();
                }

                if(file is null || file.FileInfo is null)
                    return new BadRequestResult();

                // create a memory stream
                var memoryStream = new MemoryStream();

                using (var stream = new FileStream(file.FileInfo.FullName, FileMode.Open))
                {
                    await stream.CopyToAsync(memoryStream);
                }

                return FileStreamUpload(fileName, memoryStream);
            }
            else
                return new BadRequestResult();
        }
    }
}
