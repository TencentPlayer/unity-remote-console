using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RConsole.Common
{
    /// <summary>
    /// 响应查看界面数据
    /// </summary>
    public class LookInViewModel : IBinaryModelBase
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// 节点矩形区域
        /// </summary>
        public Rect Rect { get; set; } = new Rect();

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<LookInViewModel> Children { get; set; } = new List<LookInViewModel>();

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Name);
                bw.Write(Path);
                bw.Write(IsActive);
                bw.Write(Rect.x);
                bw.Write(Rect.y);
                bw.Write(Rect.width);
                bw.Write(Rect.height);
                bw.Write(Children.Count);
                foreach (var child in Children)
                {
                    bw.Write(child.ToBinary());
                }

                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            Name = br.ReadString();
            Path = br.ReadString();
            IsActive = br.ReadBoolean();
            Rect = new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var childCount = br.ReadInt32();
            Children.Clear();
            for (int i = 0; i < childCount; i++)
            {
                var child = new LookInViewModel();
                child.FromBinary(br);
                Children.Add(child);
            }
        }
    }
}