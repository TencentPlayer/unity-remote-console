using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RConsole.Common
{
    /// <summary>
    /// 响应查看界面数据
    /// </summary>
    public class FileModel : IBinaryModelBase
    {
        // public static int CurentId = 0;
        public FileModel()
        {
        }

        public FileModel(string path)
        {
            Path = path;
        }

        public FileModel(int id, string path)
        {
            Id = id;
            Path = path;
        }

        /// <summary>
        /// 随机生成不重复的ID
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// 根路径
        /// </summary>
        public string RootPath { get; set; } = Application.persistentDataPath;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 是否是目录
        /// </summary>
        public bool IsDirectory { get; set; } = false;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long Length { get; set; } = 0;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public long LastWriteTime { get; set; } = 0;

        /// <summary>
        /// 文件MD5
        /// </summary>
        public string MD5 { get; set; } = string.Empty;

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<FileModel> Children { get; set; } = new List<FileModel>();


        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(RootPath);
                bw.Write(Name);
                bw.Write(Path);
                bw.Write(IsDirectory);
                bw.Write(Length);
                bw.Write(LastWriteTime);
                bw.Write(MD5);
                bw.Write(Children.Count);
                for (int i = 0; i < Children.Count; i++)
                {
                    bw.Write(Children[i].ToBinary());
                }

                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            RootPath = br.ReadString();
            Name = br.ReadString();
            Path = br.ReadString();
            IsDirectory = br.ReadBoolean();
            Length = br.ReadInt64();
            LastWriteTime = br.ReadInt64();
            MD5 = br.ReadString();
            int childCount = br.ReadInt32();
            for (int i = 0; i < childCount; i++)
            {
                var child = new FileModel();
                child.FromBinary(br);
                Children.Add(child);
            }
        }
    }
}