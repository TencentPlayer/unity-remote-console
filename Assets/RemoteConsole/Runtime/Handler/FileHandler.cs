using System;
using System.IO;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{
    public class FileHandler : IHandler
    {
        public override void OnEnable()
        {
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CFile, (byte)SubFile.FetchDirectory, OnFetchDirectory);
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CFile, (byte)SubFile.MD5, OnFetchMD5);
            RCCapability.Instance.WebSocket.On(EnvelopeKind.S2CFile, (byte)SubFile.Download, OnDownload);
        }

        public override void OnDisable()
        {
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CFile, (byte)SubFile.FetchDirectory, OnFetchDirectory);
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CFile, (byte)SubFile.MD5, OnFetchMD5);
            RCCapability.Instance.WebSocket.Off(EnvelopeKind.S2CFile, (byte)SubFile.Download, OnDownload);
        }

        private IBinaryModelBase OnFetchDirectory(IBinaryModelBase model)
        {
            var req = (FileModel)model;
            var path = req.Path;
            path = path.Replace(RCCapability.Instance.PathRoot, "");
            path = RCCapability.Instance.PathRoot + path;
            var root = BuildTree(path);
            root.Id = req.Id;
            return root;
        }

        private IBinaryModelBase OnFetchMD5(IBinaryModelBase model)
        {
            var req = (FileModel)model;
            var path = req.Path;
            path = path.Replace(RCCapability.Instance.PathRoot, "");
            path = RCCapability.Instance.PathRoot + path;
            var md5 = SafeGetFileMD5(path);
            req.MD5 = md5;
            return req;
        }

        private IBinaryModelBase OnDownload(IBinaryModelBase model)
        {
            var req = (FileModel)model;
            var path = req.Path;
            path = path.Replace(RCCapability.Instance.PathRoot, "");
            path = RCCapability.Instance.PathRoot + path;
            try
            {
                if (File.Exists(path))
                {
                    req.Data = File.ReadAllBytes(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"读取文件 {path} 失败: {e}");
            }
            return req;
        }

        private string SafeGetFileMD5(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var hash = md5.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"获取文件 {path} MD5 失败: {e}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 根据当前路径，获取当前路径以及当前路径下的所有文件和目录，以及其文件信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private FileModel BuildTree(string path)
        {
            var resp = new FileModel
            {
                Name = Path.GetFileName(path),
                Path = path,
                IsDirectory = true,
                Length = 0,
                LastWriteTime = SafeGetDirWriteTimeUtcMillis(path)
            };
            resp.Name = string.IsNullOrEmpty(resp.Name) ? "/" : resp.Name;
            try
            {
                if (Directory.Exists(path))
                {
                    var dirs = Directory.GetDirectories(path);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        var d = dirs[i];
                        var dirModel = new FileModel()
                        {
                            Name = Path.GetFileName(d),
                            Path = d,
                            IsDirectory = true,
                            Length = 0,
                            LastWriteTime = SafeGetDirWriteTimeUtcMillis(d)
                        };
                        resp.Children.Add(dirModel);
                    }

                    var files = Directory.GetFiles(path);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var f = files[i];
                        var fi = SafeGetFileInfo(f);
                        var fileModel = new FileModel
                        {
                            Name = Path.GetFileName(f),
                            Path = f,
                            IsDirectory = false,
                            Length = fi?.Length ?? 0,
                            LastWriteTime = fi != null ? ToUnixMillis(fi.LastWriteTimeUtc) : 0
                        };
                        resp.Children.Add(fileModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FileBrowser] 构建目录树失败: {ex.Message}");
            }

            return resp;
        }

        private static long SafeGetDirWriteTimeUtcMillis(string dir)
        {
            try
            {
                return ToUnixMillis(Directory.GetLastWriteTimeUtc(dir));
            }
            catch
            {
                return 0;
            }
        }

        private static FileInfo SafeGetFileInfo(string path)
        {
            try
            {
                return new FileInfo(path);
            }
            catch
            {
                return null;
            }
        }

        private static long ToUnixMillis(DateTime dtUtc)
        {
            return new DateTimeOffset(dtUtc).ToUnixTimeMilliseconds();
        }
    }
}