using APIFileServer.source;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System;
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

                    if (_memoryCache.Get(objToSend.Filename, out byte[] memoryBuffer))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(memoryBuffer))
                        {
                            if (memoryStream.Length == 0)
                            {
                                Console.WriteLine($"Request of {objToSend.Filename} -> failed");
                            }

                            if (new FileExtensionContentTypeProvider().TryGetContentType(objToSend.Filename, out string contentType))
                            {
                                // set the position to return the file from
                                return File(memoryBuffer, contentType);
                            }
                            else
                            {
                                return File(memoryBuffer, "application/octet-stream");
                            }
                        }
                    }
                    
                    //using (var stream = new FileStream(objToSend.Filename, FileMode.Open))
                    //{
                    //    memoryStream = new MemoryStream();
                    //    await stream.CopyToAsync(memoryStream);
                    //    _memoryCache.AddMemory(objToSend.Filename, memoryStream.GetBuffer());

                    //    FileInfo f = new FileInfo(objToSend.Filename);

                    //    var upload = FileStreamUpload(f.Name, memoryStream);
                    //    return upload;
                    //}

                    /*
                    if (objToSend != null)
                    {
                        if (!_memoryCache.Get(objToSend.Filename, out byte[] memoryBuffer))
                        {
                            using (var stream = new FileStream(objToSend.Filename, FileMode.Open))
                            {
                                memoryStream = new MemoryStream();
                                await stream.CopyToAsync(memoryStream);
                                _memoryCache.AddMemory(objToSend.Filename, memoryStream);
                            }
                        }
                        else
                        {
                            memoryStream = new MemoryStream(memoryBuffer);
                        }

                        FileInfo f = new FileInfo(objToSend.Filename);
                        
                        using(memoryStream)
                        {
                            if (memoryStream.Length == 0)
                            {
                                Console.WriteLine($"Request of {objToSend.Filename} -> failed");
                            }

                            return FileStreamUpload(f.Name, memoryStream);
                            //if (new FileExtensionContentTypeProvider().TryGetContentType(objToSend.Filename, out string contentType))
                            //{
                            //    // set the position to return the file from
                            //    memoryStream.Position = 0;

                            //    return new FileStreamResult(memoryStream, contentType)
                            //    {
                            //        FileDownloadName = objToSend.Filename
                            //    };
                            //}
                            //else
                            //{
                            //    memoryStream.Position = 0;

                            //    //Console.WriteLine($"Request of {objToSend.Filename}");
                            //    return new FileStreamResult(memoryStream, "application/octet-stream")
                            //    {
                            //        FileDownloadName = objToSend.Filename
                            //    };

                            //}
                        }
                    }
                    */
                }
                catch(Exception ex)
                {
                    return new BadRequestResult();
                }

                return new BadRequestResult();
            }
            else
                return new BadRequestResult();
        }

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

                //if (new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string contentType))
                //{
                //    // set the position to return the file from
                //    memoryStream.Position = 0;

                //    return new FileStreamResult(memoryStream, contentType)
                //    {
                //        FileDownloadName = fileName
                //    };
                //}
                //else
                //{
                //    memoryStream.Position = 0;

                //    //Console.WriteLine($"Request of {objToSend.Filename}");
                //    return new FileStreamResult(memoryStream, "application/octet-stream")
                //    {
                //        FileDownloadName = fileName
                //    };

                //}

                //return new BadRequestResult();
            }
            else
                return new BadRequestResult();
        }
    }
}
