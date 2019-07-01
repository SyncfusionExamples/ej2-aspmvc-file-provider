using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Syncfusion.EJ2.FileManager.Base;
using System.Web.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Drawing.Drawing2D;
using System.IO.Compression;

namespace Syncfusion.EJ2.FileManager.PhysicalFileProvider
{
    public class PhysicalFileProvider : PhysicalFileProviderBase
    {
        protected string contentRootPath;
        protected string[] allowedExtention = new string[] { "*" };
        AccessDetails AccessDetails = new AccessDetails();
        protected string rootName;

        public PhysicalFileProvider()
        {
        }
        // Set the current root folder
        public void RootFolder(string name)
        {
            this.contentRootPath = name;
        }
        // Set the allowed extesnsion
        public void AllowedExtension(string[] AlowedExtention = null)
        {
            this.allowedExtention = AlowedExtention == null ? this.allowedExtention : allowedExtention;
        }
        // Sets the access rules for the files and folders
        public void SetRules(AccessDetails details)
        {
            this.AccessDetails = details;
            var root = new DirectoryInfo(this.contentRootPath);
            this.rootName = root.Name;
        }
        // Read the files and folders
        public virtual FileManagerResponse GetFiles(string path, bool showHiddenItems, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse readResponse = new FileManagerResponse();
            try
            {
                if (path == null)
                {
                    path = string.Empty;
                }
                String fullPath = (contentRootPath + path);
                var directory = new DirectoryInfo(fullPath);
                var extensions = this.allowedExtention;
                FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
                cwd.Name = directory.Name;
                cwd.Size = 0;
                cwd.IsFile = false;
                cwd.DateModified = directory.LastWriteTime;
                cwd.DateCreated = directory.CreationTime;
                cwd.HasChild = directory.GetDirectories().Length > 0 ? true : false;
                cwd.Type = directory.Extension;
                cwd.FilterPath = GetRelativePath(this.contentRootPath, directory.Parent.FullName + "\\");
                cwd.Permission = GetPathPermission(path);
                readResponse.CWD = cwd;
                if (!cwd.Permission.Read)
                {
                    readResponse.Files = null;
                    throw new UnauthorizedAccessException("'" + this.rootName + path + "' is not accessible. Access is denied.");
                }
                readResponse.Files = ReadDirectories(directory, extensions, showHiddenItems, data);
                readResponse.Files = readResponse.Files.Concat(ReadFiles(directory, extensions, showHiddenItems, data));
                return readResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                readResponse.Error = er;
                return readResponse;
            }
        }
        // Read each file
        public virtual IEnumerable<FileManagerDirectoryContent> ReadFiles(DirectoryInfo directory, string[] extensions, bool showHiddenItems, params FileManagerDirectoryContent[] data)
        {
            try
            {
                FileManagerResponse readFiles = new FileManagerResponse();
                if (!showHiddenItems)
                {
                    var files = extensions.SelectMany(directory.GetFiles).Where(f => (f.Attributes & FileAttributes.Hidden) == 0)
                            .Select(file => new FileManagerDirectoryContent
                            {
                                Name = file.Name,
                                IsFile = true,
                                Size = file.Length,
                                DateModified = file.LastWriteTime,
                                DateCreated = file.CreationTime,
                                HasChild = false,
                                Type = file.Extension,
                                FilterPath = GetRelativePath(this.contentRootPath, directory.FullName),
                                Permission = GetPermission(directory.FullName, file.Name, true)
                            });
                    readFiles.Files = (IEnumerable<FileManagerDirectoryContent>)files;
                }
                else
                {
                    var files = extensions.SelectMany(directory.GetFiles)
                            .Select(file => new FileManagerDirectoryContent
                            {
                                Name = file.Name,
                                IsFile = true,
                                Size = file.Length,
                                DateModified = file.LastWriteTime,
                                DateCreated = file.CreationTime,
                                HasChild = false,
                                Type = file.Extension,
                                FilterPath = GetRelativePath(this.contentRootPath, directory.FullName),
                                Permission = GetPermission(directory.FullName, file.Name, true)
                            });
                    readFiles.Files = (IEnumerable<FileManagerDirectoryContent>)files;
                }
                return readFiles.Files;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        // Sets the relative path
        public static string GetRelativePath(string rootPath, string fullPath)
        {
            if (!String.IsNullOrEmpty(rootPath) && !String.IsNullOrEmpty(fullPath))
            {
                var rootDirectory = new DirectoryInfo(rootPath);
                if (rootDirectory.FullName.Substring(rootDirectory.FullName.Length - 1) == "\\")
                {
                    if (fullPath.Contains(rootDirectory.FullName))
                    {
                        return fullPath.Substring(rootPath.Length - 1);
                    }
                }
                else if (fullPath.Contains(rootDirectory.FullName + "\\"))
                {
                    return "\\" + fullPath.Substring(rootPath.Length + 1);
                }
            }
            return String.Empty;
        }

        // Read each Directory
        public virtual IEnumerable<FileManagerDirectoryContent> ReadDirectories(DirectoryInfo directory, string[] extensions, bool showHiddenItems, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse readDirectory = new FileManagerResponse();
            try
            {
                if (!showHiddenItems)
                {
                    var directories = directory.GetDirectories().Where(f => (f.Attributes & FileAttributes.Hidden) == 0)
                            .Select(subDirectory => new FileManagerDirectoryContent
                            {
                                Name = subDirectory.Name,
                                Size = 0,
                                IsFile = false,
                                DateModified = subDirectory.LastWriteTime,
                                DateCreated = subDirectory.CreationTime,
                                HasChild = subDirectory.GetDirectories().Length > 0 ? true : false,
                                Type = subDirectory.Extension,
                                FilterPath = GetRelativePath(this.contentRootPath, directory.FullName),
                                Permission = GetPermission(directory.FullName, subDirectory.Name, false)
                            });
                    readDirectory.Files = (IEnumerable<FileManagerDirectoryContent>)directories;
                }
                else
                {
                    var directories = directory.GetDirectories().Select(subDirectory => new FileManagerDirectoryContent
                    {
                        Name = subDirectory.Name,
                        Size = 0,
                        IsFile = false,
                        DateModified = subDirectory.LastWriteTime,
                        DateCreated = subDirectory.CreationTime,
                        HasChild = subDirectory.GetDirectories().Length > 0 ? true : false,
                        Type = subDirectory.Extension,
                        FilterPath = GetRelativePath(this.contentRootPath, directory.FullName),
                        Permission = GetPermission(directory.FullName, subDirectory.Name, false)
                    });
                    readDirectory.Files = (IEnumerable<FileManagerDirectoryContent>)directories;
                }
                return readDirectory.Files;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        // Create a NewFolder
        public virtual FileManagerResponse Create(string path, string name, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse createResponse = new FileManagerResponse();
            try
            {
                AccessPermission PathPermission = GetPathPermission(path);
                if (!PathPermission.Read || !PathPermission.EditContents)
                    throw new UnauthorizedAccessException("'" + this.rootName + path + "' is not accessible. Access is denied.");

                var newDirectoryPath = Path.Combine(contentRootPath + path, name);

                var directoryExist = Directory.Exists(newDirectoryPath);

                if (directoryExist)
                {
                    var exist = new DirectoryInfo(newDirectoryPath);
                    ErrorDetails er = new ErrorDetails();
                    er.Code = "400";
                    er.Message = "A file or folder with the name " + exist.Name.ToString() + " already exists.";
                    createResponse.Error = er;

                    return createResponse;
                }

                string physicalPath = GetPath(path);
                Directory.CreateDirectory(newDirectoryPath);
                var directory = new DirectoryInfo(newDirectoryPath);
                FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent();
                CreateData.Name = directory.Name;
                CreateData.IsFile = false;
                CreateData.Size = 0;
                CreateData.DateModified = directory.LastWriteTime;
                CreateData.DateCreated = directory.CreationTime;
                CreateData.HasChild = directory.GetDirectories().Length > 0 ? true : false;
                CreateData.Type = directory.Extension;
                CreateData.Permission = GetPermission(physicalPath, directory.Name, false);
                var newData = new FileManagerDirectoryContent[] { CreateData };
                createResponse.Files = newData;
                return createResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                createResponse.Error = er;
                return createResponse;
            }
        }
        // Gets the details of the selected file(s) or folder(s)
        public virtual FileManagerResponse Details(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse getDetailResponse = new FileManagerResponse();
            FileDetails detailFiles = new FileDetails();
            try
            {
                if (names.Length == 0 || names.Length == 1)
                {
                    if (path == null) { path = string.Empty; };
                    var fullPath = "";
                    if (names.Length == 0)
                    {
                        fullPath = (contentRootPath + path.Substring(0, path.Length - 1));
                    }
                    else if (names[0] == null || names[0] == "")
                    {
                        fullPath = (contentRootPath + path);
                    }
                    else
                    {
                        fullPath = Path.Combine(contentRootPath + path, names[0]);
                    }
                    string physicalPath = GetPath(path);
                    var directory = new DirectoryInfo(fullPath);
                    FileInfo info = new FileInfo(fullPath);
                    FileDetails fileDetails = new FileDetails();
                    var baseDirectory = new DirectoryInfo(this.contentRootPath);
                    fileDetails.Name = info.Name == "" ? directory.Name : info.Name;
                    fileDetails.IsFile = (File.GetAttributes(fullPath) & FileAttributes.Directory) != FileAttributes.Directory;
                    fileDetails.Size = (File.GetAttributes(fullPath) & FileAttributes.Directory) != FileAttributes.Directory ? byteConversion(info.Length).ToString() : byteConversion(new DirectoryInfo(fullPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => (file.Length))).ToString();
                    fileDetails.Created = info.CreationTime;
                    fileDetails.Location = GetRelativePath(baseDirectory.Parent.FullName, info.FullName);
                    fileDetails.Modified = info.LastWriteTime;
                    fileDetails.Permission = GetPermission(physicalPath, fileDetails.Name, fileDetails.IsFile);
                    detailFiles = fileDetails;
                }
                else
                {
                    FileDetails fileDetails = new FileDetails();
                    fileDetails.Size = "0";
                    for (int i = 0; i < names.Length; i++)
                    {
                        var fullPath = "";
                        if (names[i] == null)
                        {
                            fullPath = (contentRootPath + path);
                        }
                        else
                        {
                            fullPath = Path.Combine(contentRootPath + path, names[i]);
                        }
                        var baseDirectory = new DirectoryInfo(this.contentRootPath);
                        FileInfo info = new FileInfo(fullPath);
                        fileDetails.Name = string.Join(", ", names);
                        fileDetails.Size = (long.Parse(fileDetails.Size) + ((info.Attributes.ToString() != "Directory") ? info.Length : new DirectoryInfo(fullPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length))).ToString();
                        fileDetails.Location = GetRelativePath(baseDirectory.Parent.FullName, info.Directory.FullName);
                    }
                    fileDetails.Size = byteConversion(long.Parse(fileDetails.Size)).ToString();
                    fileDetails.MultipleFiles = true;
                    detailFiles = fileDetails;
                }
                getDetailResponse.Details = detailFiles;
                return getDetailResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                getDetailResponse.Error = er;
                return getDetailResponse;
            }
        }
        // Deletes the file(s) or folder(s)
        public virtual FileManagerResponse Delete(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse DeleteResponse = new FileManagerResponse();
            FileManagerDirectoryContent[] removedFiles = new FileManagerDirectoryContent[names.Length];
            try
            {
                string physicalPath = GetPath(path);
                for (int i = 0; i < names.Length; i++)
                {
                    bool IsFile = !IsDirectory(physicalPath, names[i]);
                    AccessPermission permission = GetPermission(physicalPath, names[i], IsFile);
                    if (!permission.Read || !permission.Edit)
                        throw new UnauthorizedAccessException("'" + this.rootName + path + names[i] + "' is not accessible. Access is denied.");
                }
                for (int i = 0; i < names.Length; i++)
                {
                    var fullPath = Path.Combine((contentRootPath + path), names[i]);
                    var directory = new DirectoryInfo(fullPath);
                    if (!string.IsNullOrEmpty(names[i]))
                    {
                        FileAttributes attr = File.GetAttributes(fullPath);
                        removedFiles[i] = GetFileDetails(fullPath);
                        //detect whether its a directory or file
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            DeleteDirectory(fullPath);
                        }
                        else
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                    else
                    {
                        throw new ArgumentNullException("name should not be null");
                    }
                }
                DeleteResponse.Files = removedFiles;
                return DeleteResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                DeleteResponse.Error = er;
                return DeleteResponse;
            }
        }
        // Renames the file(s) or folder(s)
        public virtual FileManagerResponse Rename(string path, string name, string newName, bool replace = false, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse renameResponse = new FileManagerResponse();
            try
            {
                string physicalPath = GetPath(path);
                bool IsFile = !IsDirectory(physicalPath, name);
                AccessPermission permission = GetPermission(physicalPath, name, IsFile);
                if (!permission.Read || !permission.Edit)
                    throw new UnauthorizedAccessException("'" + this.rootName + path + name + "' is not accessible. Access is denied.");

                var tempPath = (contentRootPath + path);
                var oldPath = Path.Combine(tempPath, name);
                var newPath = Path.Combine(tempPath, newName);
                FileAttributes attr = File.GetAttributes(oldPath);

                FileInfo info = new FileInfo(oldPath);
                var isFile = (File.GetAttributes(oldPath) & FileAttributes.Directory) != FileAttributes.Directory;
                if (isFile)
                {
                    info.MoveTo(newPath);
                }
                else
                {
                    var directoryExist = Directory.Exists(newPath);
                    if (directoryExist && !oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var exist = new DirectoryInfo(newPath);
                        ErrorDetails er = new ErrorDetails();
                        er.Code = "400";
                        er.Message = "Cannot rename " + exist.Name.ToString() + " to " + newName + ": destination already exists.";
                        renameResponse.Error = er;

                        return renameResponse;
                    }
                    else if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    {
                        tempPath = Path.Combine(tempPath + "Syncfusion_TempFolder");
                        Directory.Move(oldPath, tempPath);
                        Directory.Move(tempPath, newPath);
                    }
                    else
                    {
                        Directory.Move(oldPath, newPath);
                    }
                }
                var addedData = new[]{
                        GetFileDetails(newPath)
                    };
                renameResponse.Files = addedData;
                return renameResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                renameResponse.Error = er;
                return renameResponse;
            }
        }
        // Copies the file(s) or folder(s)
        public virtual FileManagerResponse Copy(string path, string targetPath, string[] names, string[] renamedItemNames, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse copyResponse = new FileManagerResponse();
            try
            {
                if (renamedItemNames == null)
                {
                    renamedItemNames = new string[0];
                }
                string physicalPath = GetPath(path);
                for (int i = 0; i < names.Length; i++)
                {
                    bool IsFile = !IsDirectory(physicalPath, names[i]);
                    AccessPermission permission = GetPermission(physicalPath, names[i], IsFile);
                    if (!permission.Read || !permission.Copy)
                        throw new UnauthorizedAccessException("'" + this.rootName + path + names[i] + "' is not accessible. Access is denied.");
                }
                AccessPermission PathPermission = GetPathPermission(targetPath);
                if (!PathPermission.Read || !PathPermission.EditContents)
                    throw new UnauthorizedAccessException("'" + this.rootName + targetPath + "' is not accessible. Access is denied.");

                var existFiles = new List<string>();
                var missingFiles = new List<string>();
                var copiedFiles = new List<FileManagerDirectoryContent>();
                var tempPath = path;
                for (int i = 0; i < names.Length; i++)
                {
                    var fullname = names[i];
                    int name = names[i].LastIndexOf("/");
                    if (name >= 0)
                    {
                        path = tempPath + names[i].Substring(0, name + 1);
                        names[i] = names[i].Substring(name + 1);
                    }
                    else
                    {
                        path = tempPath;
                    }
                    var itemPath = Path.Combine(contentRootPath + path, names[i]);
                    if (Directory.Exists(itemPath) || File.Exists(itemPath))
                    {
                        FileAttributes fileAttributes = File.GetAttributes(itemPath);
                        if (fileAttributes == FileAttributes.Directory)
                        {
                            var directoryName = names[i];
                            var oldPath = Path.Combine(contentRootPath + path, directoryName);
                            var newPath = Path.Combine(contentRootPath + targetPath, directoryName);
                            var exist = Directory.Exists(newPath);
                            if (exist)
                            {
                                int index = -1;
                                if (renamedItemNames.Length > 0)
                                {
                                    index = Array.FindIndex(renamedItemNames, row => row.Contains(directoryName));
                                }
                                if ((newPath == oldPath) || (index != -1))
                                {
                                    newPath = DirectoryRename(newPath);
                                    DirectoryCopy(oldPath, newPath);
                                    var detail = GetFileDetails(newPath);
                                    detail.PreviousName = names[i];
                                    copiedFiles.Add(detail);
                                }
                                else
                                {
                                    existFiles.Add(fullname);
                                }
                            }
                            else
                            {
                                DirectoryCopy(oldPath, newPath);
                                var detail = GetFileDetails(newPath);
                                detail.PreviousName = names[i];
                                copiedFiles.Add(detail);
                            }
                        }
                        else
                        {
                            var fileName = names[i];
                            var newFilePath = Path.Combine(targetPath, fileName);
                            var oldPath = Path.Combine(contentRootPath + path, fileName);
                            var newPath = Path.Combine(contentRootPath + targetPath, fileName);
                            var fileExist = System.IO.File.Exists(newPath);

                            if (fileExist)
                            {
                                int index = -1;
                                if (renamedItemNames.Length > 0)
                                {
                                    index = Array.FindIndex(renamedItemNames, row => row.Contains(fileName));
                                }
                                if ((newPath == oldPath) || (index != -1))
                                {
                                    newPath = FileRename(newPath, fileName);
                                    File.Copy(oldPath, newPath);
                                    var detail = GetFileDetails(newPath);
                                    detail.PreviousName = names[i];
                                    copiedFiles.Add(detail);
                                }
                                else
                                {
                                    existFiles.Add(fullname);
                                }
                            }
                            else
                            {
                                if (renamedItemNames.Length > 0)
                                {
                                    File.Delete(newPath);
                                }
                                File.Copy(oldPath, newPath);
                                var detail = GetFileDetails(newPath);
                                detail.PreviousName = names[i];
                                copiedFiles.Add(detail);
                            }
                        }
                    }
                    else
                    {
                        missingFiles.Add(names[i]);
                    }
                }
                copyResponse.Files = copiedFiles;
                if (existFiles.Count > 0)
                {
                    ErrorDetails er = new ErrorDetails();
                    er.FileExists = existFiles;
                    er.Code = "400";
                    er.Message = "File Already Exists";
                    copyResponse.Error = er;
                }
                if (missingFiles.Count == 0)
                {
                    return copyResponse;
                }
                else
                {
                    string namelist = missingFiles[0];
                    for (int k = 1; k < missingFiles.Count; k++)
                    {
                        namelist = namelist + ", " + missingFiles[k];
                    }
                    throw new FileNotFoundException(namelist + " not found in given location.");
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                er.FileExists = copyResponse.Error?.FileExists;
                copyResponse.Error = er;
                return copyResponse;
            }
        }
        // Moves the file(s) or folder(s)
        public virtual FileManagerResponse Move(string path, string targetPath, string[] names, string[] renamedItemNames, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse moveResponse = new FileManagerResponse();
            try
            {
                if (renamedItemNames == null)
                {
                    renamedItemNames = new string[0];
                }
                string physicalPath = GetPath(path);
                for (int i = 0; i < names.Length; i++)
                {
                    bool IsFile = !IsDirectory(physicalPath, names[i]);
                    AccessPermission permission = GetPermission(physicalPath, names[i], IsFile);
                    if (!permission.Read || !permission.Edit)
                        throw new UnauthorizedAccessException("'" + this.rootName + path + names[i] + "' is not accessible. Access is denied.");
                }
                AccessPermission PathPermission = GetPathPermission(targetPath);
                if (!PathPermission.Read || !PathPermission.EditContents)
                    throw new UnauthorizedAccessException("'" + this.rootName + targetPath + "' is not accessible. Access is denied.");

                var existFiles = new List<string>();
                var missingFiles = new List<string>();
                var movedFiles = new List<FileManagerDirectoryContent>();
                var tempPath = path;
                for (int i = 0; i < names.Length; i++)
                {
                    var fullName = names[i];
                    int name = names[i].LastIndexOf("/");
                    if (name >= 0)
                    {
                        path = tempPath + names[i].Substring(0, name + 1);
                        names[i] = names[i].Substring(name + 1);
                    }
                    else
                    {
                        path = tempPath;
                    }
                    var itemPath = Path.Combine(contentRootPath + path, names[i]);
                    if (Directory.Exists(itemPath) || File.Exists(itemPath))
                    {
                        FileAttributes fileAttributes = File.GetAttributes(itemPath);
                        if (fileAttributes == FileAttributes.Directory)
                        {
                            var directoryName = names[i];
                            var oldPath = Path.Combine(contentRootPath + path, directoryName);
                            var newPath = Path.Combine(contentRootPath + targetPath, directoryName);
                            var exist = Directory.Exists(newPath);
                            if (exist)
                            {
                                int index = -1;
                                if (renamedItemNames.Length > 0)
                                {
                                    index = Array.FindIndex(renamedItemNames, row => row.Contains(directoryName));
                                }
                                if ((newPath == oldPath) || (index != -1))
                                {
                                    newPath = DirectoryRename(newPath);
                                    Directory.Move(oldPath, newPath);
                                    var detail = GetFileDetails(newPath);
                                    detail.PreviousName = names[i];
                                    movedFiles.Add(detail);
                                }
                                else
                                {
                                    existFiles.Add(fullName);
                                }
                            }
                            else
                            {
                                Directory.Move(oldPath, newPath);
                                var detail = GetFileDetails(newPath);
                                detail.PreviousName = names[i];
                                movedFiles.Add(detail);
                            }
                        }
                        else
                        {
                            var fileName = names[i];
                            var newFilePath = Path.Combine(targetPath, fileName);
                            var oldPath = Path.Combine(contentRootPath + path, fileName);
                            var newPath = Path.Combine(contentRootPath + targetPath, fileName);
                            var fileExist = File.Exists(newPath);

                            if (fileExist)
                            {
                                int index = -1;
                                if (renamedItemNames.Length > 0)
                                {
                                    index = Array.FindIndex(renamedItemNames, row => row.Contains(fileName));
                                }
                                if ((newPath == oldPath) || (index != -1))
                                {
                                    newPath = FileRename(newPath, fileName);
                                    File.Move(oldPath, newPath);
                                    var detail = GetFileDetails(newPath);
                                    detail.PreviousName = names[i];
                                    movedFiles.Add(detail);
                                }
                                else
                                {
                                    existFiles.Add(fullName);
                                }
                            }
                            else
                            {
                                File.Move(oldPath, newPath);
                                var detail = GetFileDetails(newPath);
                                detail.PreviousName = names[i];
                                movedFiles.Add(detail);
                            }
                        }
                    }
                    else
                    {
                        missingFiles.Add(names[i]);
                    }
                }
                moveResponse.Files = movedFiles;
                if (existFiles.Count > 0)
                {
                    ErrorDetails er = new ErrorDetails();
                    er.FileExists = existFiles;
                    er.Code = "400";
                    er.Message = "File Already Exists";
                    moveResponse.Error = er;
                }
                if (missingFiles.Count == 0)
                {
                    return moveResponse;
                }
                else
                {
                    string namelist = missingFiles[0];
                    for (int k = 1; k < missingFiles.Count; k++)
                    {
                        namelist = namelist + ", " + missingFiles[k];
                    }
                    throw new FileNotFoundException(namelist + " not found in given location.");
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails
                {
                    Message = e.Message.ToString(),
                    Code = e.Message.ToString().Contains("Access is denied") ? "401" : "417",
                    FileExists = moveResponse.Error?.FileExists
                };
                moveResponse.Error = er;
                return moveResponse;
            }
        }
        // Search for the file(s) or folder(s)
        public virtual FileManagerResponse Search(string path, string searchString, bool showHiddenItems = false, bool caseSensitive = false, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse searchResponse = new FileManagerResponse();
            try
            {
                if (path == null) { path = string.Empty; };
                var searchWord = searchString;
                var searchPath = (this.contentRootPath + path);
                var directory = new DirectoryInfo((this.contentRootPath + path));
                FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
                cwd.Name = directory.Name;
                cwd.Size = 0;
                cwd.IsFile = false;
                cwd.DateModified = directory.LastWriteTime;
                cwd.DateCreated = directory.CreationTime;
                cwd.HasChild = directory.GetDirectories().Length > 0 ? true : false;
                cwd.Type = directory.Extension;
                cwd.FilterPath = GetRelativePath(this.contentRootPath, directory.Parent.FullName + "\\");
                cwd.Permission = GetPathPermission(path);
                if (!cwd.Permission.Read)
                    throw new UnauthorizedAccessException("'" + this.rootName + path + "' is not accessible. Access is denied.");
                searchResponse.CWD = cwd;

                List<FileManagerDirectoryContent> foundedFiles = new List<FileManagerDirectoryContent>();
                var extensions = this.allowedExtention;
                var searchDirectory = new DirectoryInfo(searchPath);
                if (showHiddenItems)
                {
                    var filteredFileList = searchDirectory.GetFiles(searchString, SearchOption.AllDirectories).
                        Where(item => new Regex(WildcardToRegex(searchString), (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase)).IsMatch(item.Name));
                    var filteredDirectoryList = searchDirectory.GetDirectories(searchString, SearchOption.AllDirectories).
                        Where(item => new Regex(WildcardToRegex(searchString), (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase)).IsMatch(item.Name));
                    foreach (FileInfo file in filteredFileList)
                    {
                        foundedFiles.Add(GetFileDetails(Path.Combine(this.contentRootPath, file.DirectoryName, file.Name)));
                    }
                    foreach (DirectoryInfo dir in filteredDirectoryList)
                    {
                        foundedFiles.Add(GetFileDetails(Path.Combine(this.contentRootPath, dir.FullName)));
                    }
                }
                else
                {
                    var filteredFileList = searchDirectory.GetFiles(searchString, SearchOption.AllDirectories).
                        Where(item => new Regex(WildcardToRegex(searchString), (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase)).IsMatch(item.Name) && (item.Attributes & FileAttributes.Hidden) == 0);
                    var filteredDirectoryList = searchDirectory.GetDirectories(searchString, SearchOption.AllDirectories).
                        Where(item => new Regex(WildcardToRegex(searchString), (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase)).IsMatch(item.Name) && (item.Attributes & FileAttributes.Hidden) == 0);
                    foreach (FileInfo file in filteredFileList)
                    {
                        foundedFiles.Add(GetFileDetails(Path.Combine(this.contentRootPath, file.DirectoryName, file.Name)));
                    }
                    foreach (DirectoryInfo dir in filteredDirectoryList)
                    {
                        foundedFiles.Add(GetFileDetails(Path.Combine(this.contentRootPath, dir.FullName)));
                    }
                }
                searchResponse.Files = (IEnumerable<FileManagerDirectoryContent>)foundedFiles;
                return searchResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                searchResponse.Error = er;
                return searchResponse;
            }
        }
        // Converts the byte value to appropriate file size.
        public String byteConversion(long fileSize)
        {
            try
            {
                string[] index = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (fileSize == 0)
                {
                    return "0 " + index[0];
                }

                long bytes = Math.Abs(fileSize);
                int loc = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, loc), 1);
                return (Math.Sign(fileSize) * num).ToString() + " " + index[loc];
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public virtual string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".")
                       + "$";
        }
        // Returns the image file
        public virtual FileStreamResult GetImage(string path, string id, bool allowCompress, ImageSize size = null, params FileManagerDirectoryContent[] data)
        {
            path = path.Substring(1, path.Length - 1);
            try
            {
                AccessPermission PathPermission = GetFilePermission(path);
                if (!PathPermission.Read)
                    return null;
                String fullPath = Path.Combine(contentRootPath, path);
                if (allowCompress)
                {
                    size = new ImageSize { Height = 14, Width = 16 };
                    CompressImage(fullPath, size);
                }
                FileStream fileStreamInput = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                FileStreamResult fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                return fileStreamResult;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Compress the image.
        public virtual void CompressImage(string path, ImageSize targetSize)
        {
            using (var image = Image.FromStream(System.IO.File.OpenRead(path)))
            {
                var originalSize = new ImageSize { Height = image.Height, Width = image.Width };
                var size = FindRatio(originalSize, targetSize);
                using (var thumbnail = new Bitmap(size.Width, size.Height))
                {
                    using (var graphics = Graphics.FromImage(thumbnail))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.PixelOffsetMode = PixelOffsetMode.Default;
                        graphics.InterpolationMode = InterpolationMode.Bicubic;
                        graphics.DrawImage(image, 0, 0, thumbnail.Width, thumbnail.Height);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        thumbnail.Save(memoryStream, ImageFormat.Png);
                        HttpResponse response = HttpContext.Current.Response;
                        response.Buffer = true;
                        response.Clear();
                        response.ContentType = "image/png";
                        response.BinaryWrite(memoryStream.ToArray());
                        response.Flush();
                        response.End();
                    }
                }
            }
        }
        // Returns the compressed image size
        public virtual ImageSize FindRatio(ImageSize originalSize, ImageSize targetSize)
        {
            var aspectRatio = (float)originalSize.Width / (float)originalSize.Height;
            var width = targetSize.Width;
            var height = targetSize.Height;

            if (originalSize.Width > targetSize.Width || originalSize.Height > targetSize.Height)
            {
                if (aspectRatio > 1)
                {
                    height = (int)(targetSize.Height / aspectRatio);
                }
                else
                {
                    width = (int)(targetSize.Width * aspectRatio);
                }
            }
            else
            {
                width = originalSize.Width;
                height = originalSize.Height;
            }

            return new ImageSize
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1)
            };
        }
        // Uploads the file(s) to the fil system
        public virtual FileManagerResponse Upload(string path, IList<System.Web.HttpPostedFileBase> uploadFiles, string action, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse uploadResponse = new FileManagerResponse();
            try
            {
                AccessPermission PathPermission = GetPathPermission(path);
                if (!PathPermission.Read || !PathPermission.Upload)
                    throw new UnauthorizedAccessException("'" + this.rootName + path + "' is not accessible. Access is denied.");

                var existFiles = new List<string>();
                foreach (var file in uploadFiles)
                {
                    if (uploadFiles != null)
                    {
                        var name = System.IO.Path.GetFileName(file.FileName);
                        var fullName = Path.Combine((this.contentRootPath + path), name);
                        if (action == "save")
                        {
                            if (!System.IO.File.Exists(fullName))
                            {
                                file.SaveAs(fullName);
                            }
                            else
                            {
                                existFiles.Add(fullName);
                            }
                        }
                        else if (action == "remove")
                        {
                            if (System.IO.File.Exists(fullName))
                            {
                                System.IO.File.Delete(fullName);
                            }
                            else
                            {
                                ErrorDetails er = new ErrorDetails();
                                er.Code = "404";
                                er.Message = "File not found.";
                                uploadResponse.Error = er;
                            }
                        }
                        else if (action == "replace")
                        {
                            if (System.IO.File.Exists(fullName))
                            {
                                System.IO.File.Delete(fullName);
                            }
                            file.SaveAs(fullName);
                        }
                        else if (action == "keepboth")
                        {
                            var newName = fullName;
                            int index = newName.LastIndexOf(".");
                            if (index >= 0)
                                newName = newName.Substring(0, index);
                            int fileCount = 0;
                            while (System.IO.File.Exists(newName + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(name) : Path.GetExtension(name))))
                            {
                                fileCount++;
                            }
                            newName = newName + (fileCount > 0 ? "(" + fileCount.ToString() + ")" : "") + Path.GetExtension(name);
                            file.SaveAs(newName);
                        }
                    }
                }
                if (existFiles.Count != 0)
                {
                    ErrorDetails er = new ErrorDetails();
                    er.Code = "400";
                    er.Message = "File already exists.";
                    er.FileExists = existFiles;
                    uploadResponse.Error = er;
                }
                return uploadResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                uploadResponse.Error = er;
                return uploadResponse;
            }
        }
        // Downloads the file(s) or folder(s)
        public virtual FileStreamResult Download(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            try
            {
                string physicalPath = GetPath(path);
                String fullPath;
                int count = 0;
                for (var i = 0; i < names.Length; i++)
                {
                    bool IsFile = !IsDirectory(physicalPath, names[i]);
                    AccessPermission FilePermission = GetPermission(physicalPath, names[i], IsFile);
                    if (!FilePermission.Read || !FilePermission.Download)
                        throw new UnauthorizedAccessException("'" + this.rootName + path + names[i] + "' is not accessible. Access is denied.");

                    fullPath = Path.Combine(contentRootPath + path, names[i]);
                    FileAttributes fileAttributes = File.GetAttributes(fullPath);
                    if (fileAttributes != FileAttributes.Directory)
                    {
                        count++;
                    }
                }
                if (count == names.Length)
                {
                    return DownloadFile(path, names);
                }
                else
                {
                    return DownloadFolder(path, names, count);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private FileStreamResult fileStreamResult;
        // Downloads the file(s)
        public virtual FileStreamResult DownloadFile(string path, string[] names = null)
        {
            try
            {
                path = Path.GetDirectoryName(path);
                var tempPath = Path.Combine(Path.GetTempPath(), "temp.zip");
                String fullPath;
                if (names == null || names.Length == 0)
                {
                    fullPath = (contentRootPath + path);
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    FileStream fileStreamInput = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                }
                else if (names.Length == 1)
                {
                    fullPath = Path.Combine(contentRootPath + path, names[0]);
                    FileStream fileStreamInput = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = names[0];
                }
                else if (names.Length > 1)
                {
                    string fileName = Guid.NewGuid().ToString() + "temp.zip";
                    string newFileName = fileName.Substring(36);
                    tempPath = Path.Combine(Path.GetTempPath(), newFileName);
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    string currentDirectory;
                    ZipArchiveEntry zipEntry;
                    ZipArchive archive;
                    for (int i = 0; i < names.Count(); i++)
                    {
                        fullPath = Path.Combine((contentRootPath + path), names[i]);
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            try
                            {
                                using (archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
                                {
                                    currentDirectory = Path.Combine((contentRootPath + path), names[i]);
                                    zipEntry = archive.CreateEntryFromFile(Path.Combine(this.contentRootPath, currentDirectory), names[i]);
                                }
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            throw new ArgumentNullException("name should not be null");
                        }
                    }
                    try
                    {
                        FileStream fileStreamInput = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Delete);
                        fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                        fileStreamResult.FileDownloadName = "files.zip";
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                return fileStreamResult;
            }
            catch (Exception)
            {
                return null;
            }
        }
        // Downloads the folder(s)
        protected FileStreamResult DownloadFolder(string path, string[] names, int count)
        {
            try
            {
                path = Path.GetDirectoryName(path);
                FileStreamResult fileStreamResult;
                // create a temp.Zip file intially 
                var tempPath = Path.Combine(Path.GetTempPath(), "temp.zip");
                String fullPath;
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                if (names.Length == 1)
                {
                    var directoryName = new DirectoryInfo(contentRootPath);
                    if (directoryName.Name != names[0])
                    {
                        fullPath = Path.Combine(contentRootPath + path, names[0]);
                    }
                    else
                    {
                        fullPath = Path.Combine(contentRootPath + path);
                    }

                    ZipFile.CreateFromDirectory(fullPath, tempPath);
                    FileStream fileStreamInput = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Delete);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = names[0] + ".zip";
                }
                else
                {
                    string currentDirectory;
                    ZipArchiveEntry zipEntry;
                    ZipArchive archive;
                    using (archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
                    {
                        for (var i = 0; i < names.Length; i++)
                        {
                            currentDirectory = Path.Combine((contentRootPath + path), names[i]);
                            FileAttributes fileAttributes = File.GetAttributes(currentDirectory);
                            if (fileAttributes == FileAttributes.Directory)
                            {
                                var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories);
                                if (files.Length == 0)
                                {
                                    zipEntry = archive.CreateEntry(names[i] + "/");
                                }
                                else
                                {
                                    foreach (var filePath in files)
                                    {
                                    zipEntry = archive.CreateEntryFromFile(filePath, names[i] + filePath.Substring(currentDirectory.Length));
                                    }
                                }
                                foreach (var filePath in Directory.GetDirectories(currentDirectory, "*", SearchOption.AllDirectories))
                                {
                                    if (Directory.GetFiles(filePath).Length == 0)
                                    {
                                            zipEntry = archive.CreateEntryFromFile(Path.Combine(this.contentRootPath, filePath), filePath.Substring(path.Length));
                                    }
                                }
                            }
                            else
                            {
                                    zipEntry = archive.CreateEntryFromFile(Path.Combine(this.contentRootPath, currentDirectory), names[i]);

                            }
                        }
                    }
                    FileStream fileStreamInput = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Delete);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "application/force-download");
                    fileStreamResult.FileDownloadName = "folders.zip";
                }
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                return fileStreamResult;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Renames a Directory
        private string DirectoryRename(string newPath)
        {
            int directoryCount = 0;
            while (System.IO.Directory.Exists(newPath + (directoryCount > 0 ? "(" + directoryCount.ToString() + ")" : "")))
            {
                directoryCount++;
            }
            newPath = newPath + (directoryCount > 0 ? "(" + directoryCount.ToString() + ")" : "");
            return newPath;
        }
        // Renames a File
        private string FileRename(string newPath, string fileName)
        {
            int name = newPath.LastIndexOf(".");
            if (name >= 0)
            {
                newPath = newPath.Substring(0, name);
            }
            int fileCount = 0;
            while (System.IO.File.Exists(newPath + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(fileName) : Path.GetExtension(fileName))))
            {
                fileCount++;
            }
            newPath = newPath + (fileCount > 0 ? "(" + fileCount.ToString() + ")" : "") + Path.GetExtension(fileName);
            return newPath;
        }

        // Copies the directory
        private void DirectoryCopy(string sourceDirName, string destDirName)
        {
            try
            {
                // Gets the subdirectories for the specified directory.
                var dir = new DirectoryInfo(sourceDirName);

                var dirs = dir.GetDirectories();
                // If the destination directory doesn't exist, creates it.
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                // Gets the files in the directory and copy them to the new location.
                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    var oldPath = Path.Combine(sourceDirName, file.Name);
                    var temppath = Path.Combine(destDirName, file.Name);
                    File.Copy(oldPath, temppath);
                }
                foreach (var direc in dirs)
                {
                    var oldPath = Path.Combine(sourceDirName, direc.Name);
                    var temppath = Path.Combine(destDirName, direc.Name);
                    DirectoryCopy(oldPath, temppath);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        // Deletes the directory
        public virtual void DeleteDirectory(string path)
        {
            try
            {
                string[] files = Directory.GetFiles(path);
                string[] dirs = Directory.GetDirectories(path);
                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                foreach (string dir in dirs)
                {
                    DeleteDirectory(dir);
                }
                Directory.Delete(path, true);
            }
            catch (IOException e)
            {
                throw e;
            }
        }
        // Gets the file details
        public virtual FileManagerDirectoryContent GetFileDetails(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                FileAttributes attr = File.GetAttributes(path);
                FileInfo detailPath = new FileInfo(info.FullName);
                var folderLength = 0;
                var isFile = ((attr & FileAttributes.Directory) == FileAttributes.Directory) ? false : true;
                if (!isFile)
                {
                    folderLength = detailPath.Directory.GetDirectories().Length;
                }
                var filterPath = GetRelativePath(this.contentRootPath, info.DirectoryName + "\\");
                return new FileManagerDirectoryContent
                {
                    Name = info.Name,
                    Size = isFile ? info.Length : 0,
                    IsFile = isFile,
                    DateModified = info.LastWriteTime,
                    DateCreated = info.CreationTime,
                    Type = info.Extension,
                    HasChild = isFile ? false : (info.Directory.GetDirectories().Length > 0 ? true : false),
                    FilterPath = filterPath,
                    Permission = GetPermission(GetPath(filterPath), info.Name, isFile)
                };
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        // Gets Access permission
        public virtual AccessPermission GetPermission(string location, string name, bool isFile)
        {
            AccessPermission FilePermission = new AccessPermission();
            if (isFile)
            {
                if (this.AccessDetails.FileRules == null)
                {
                    return FilePermission;
                }
                string nameExtension = Path.GetExtension(name).ToLower();
                string fileName = Path.GetFileNameWithoutExtension(name);
                string currentPath = GetFilePath(location + name);
                foreach (FileRule fileRule in AccessDetails.FileRules)
                {
                    if (!string.IsNullOrEmpty(fileRule.Path) && (fileRule.Role == null || fileRule.Role == AccessDetails.Role))
                    {
                        if (fileRule.Path.IndexOf("*.*") > -1)
                        {
                            string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf("*.*"));
                            if (currentPath.IndexOf(GetPath(parentPath)) == 0 || parentPath == "")
                            {
                                FilePermission = UpdateFileRules(FilePermission, fileRule);
                            }
                        }
                        else if (fileRule.Path.IndexOf("*.") > -1)
                        {
                            var pathExtension = Path.GetExtension(fileRule.Path).ToLower();
                            string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf("*."));
                            if ((GetPath(parentPath) == currentPath || parentPath == "") && nameExtension == pathExtension)
                            {
                                FilePermission = UpdateFileRules(FilePermission, fileRule);
                            }
                        }
                        else if (fileRule.Path.IndexOf(".*") > -1)
                        {
                            string pathName = Path.GetFileNameWithoutExtension(fileRule.Path);
                            string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf(pathName + ".*"));
                            if ((GetPath(parentPath) == currentPath || parentPath == "") && fileName == pathName)
                            {
                                FilePermission = UpdateFileRules(FilePermission, fileRule);
                            }
                        }
                        else if (GetPath(fileRule.Path) == GetValidPath(location + name))
                        {
                            FilePermission = UpdateFileRules(FilePermission, fileRule);
                        }
                    }
                }
                return FilePermission;
            }
            else
            {
                if (this.AccessDetails.FolderRules == null)
                {
                    return FilePermission;
                }
                foreach (FolderRule folderRule in AccessDetails.FolderRules)
                {
                    if (folderRule.Path != null && (folderRule.Role == null || folderRule.Role == AccessDetails.Role))
                    {
                        if (folderRule.Path.IndexOf("*") > -1)
                        {
                            string parentPath = folderRule.Path.Substring(0, folderRule.Path.IndexOf("*"));
                            if (GetValidPath(location + name).IndexOf(GetPath(parentPath)) == 0 || parentPath == "")
                            {
                                FilePermission = UpdateFolderRules(FilePermission, folderRule);
                            }
                        }
                        else if (GetPath(folderRule.Path) == GetValidPath(location + name) || GetPath(folderRule.Path) == GetValidPath(location + name + "\\"))
                        {
                            FilePermission = UpdateFolderRules(FilePermission, folderRule);
                        }
                        else if (GetValidPath(location + name).IndexOf(GetPath(folderRule.Path)) == 0)
                        {
                            FilePermission.Edit = HasPermission(folderRule.EditContents);
                            FilePermission.EditContents = HasPermission(folderRule.EditContents);
                        }
                    }
                }
                return FilePermission;
            }
        }
        // Gets the directory path
        public virtual string GetPath(string path)
        {
            String fullPath = (this.contentRootPath + path);
            var directory = new DirectoryInfo(fullPath);
            return directory.FullName;
        }
        // Gets the valid directory path
        public virtual string GetValidPath(string path)
        {
            var directory = new DirectoryInfo(path);
            return directory.FullName;
        }
        // Gets the file path
        public virtual string GetFilePath(string path)
        {
            return Path.GetDirectoryName(path) + "\\";
        }
        // Gets the folder details
        public virtual string[] GetFolderDetails(string path)
        {
            string[] str_array = path.Split('/'), fileDetails = new string[2];
            string parentPath = "";
            for (var i = 0; i < str_array.Length - 2; i++)
            {
                parentPath += str_array[i] + "/";
            }
            fileDetails[0] = parentPath;
            fileDetails[1] = str_array[str_array.Length - 2];
            return fileDetails;
        }
        // Gets the permission for accessing the folders
        public virtual AccessPermission GetPathPermission(string path)
        {
            string[] fileDetails = GetFolderDetails(path);
            return GetPermission(GetPath(fileDetails[0]), fileDetails[1], false);
        }
        // Gets the permission for accessing the files
        public virtual AccessPermission GetFilePermission(string path)
        {
            string parentPath = path.Substring(0, path.LastIndexOf("/") + 1);
            string fileName = Path.GetFileName(path);
            return GetPermission(GetPath(parentPath), fileName, true);
        }
        // Checks wheter the item is a directory 
        public virtual bool IsDirectory(string path, string fileName)
        {
            FileInfo info = new FileInfo(path + fileName);
            return (info.Attributes.ToString() != "Directory") ? false : true;
        }
        // Checks the access permission
        public virtual bool HasPermission(Permission rule)
        {
            return rule == Permission.Allow ? true : false;
        }
        // Updates the file rules
        public virtual AccessPermission UpdateFileRules(AccessPermission filePermission, FileRule fileRule)
        {
            filePermission.Copy = HasPermission(fileRule.Copy);
            filePermission.Download = HasPermission(fileRule.Download);
            filePermission.Edit = HasPermission(fileRule.Edit);
            filePermission.Read = HasPermission(fileRule.Read);
            return filePermission;
        }
        // Updates the folder rules
        public virtual AccessPermission UpdateFolderRules(AccessPermission folderPermission, FolderRule folderRule)
        {
            folderPermission.Copy = HasPermission(folderRule.Copy);
            folderPermission.Download = HasPermission(folderRule.Download);
            folderPermission.Edit = HasPermission(folderRule.Edit);
            folderPermission.EditContents = HasPermission(folderRule.EditContents);
            folderPermission.Read = HasPermission(folderRule.Read);
            folderPermission.Upload = HasPermission(folderRule.Upload);
            return folderPermission;
        }
        public string ToCamelCase(FileManagerResponse userData)
        {
            return JsonConvert.SerializeObject(userData, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }
    }
}
