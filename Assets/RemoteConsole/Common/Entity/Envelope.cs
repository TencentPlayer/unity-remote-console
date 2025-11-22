using System;
using System.IO;
using System.Text;

namespace RConsole.Common
{

    [Serializable]
    public class Envelope : IBinaryModelBase
    {

        /// <summary>
        /// 消息ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 消息类型
        /// </summary>
        public EnvelopeKind Kind;
        
        /// <summary>
        /// 对应模块子命令
        /// </summary>
        public byte SubCommandId;

        /// <summary>
        /// 
        /// </summary>
        public IBinaryModelBase Model = null;

        public Envelope(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                FromBinary(br);
            }
        }

        public Envelope(EnvelopeKind kind, byte subCommandId, IBinaryModelBase model)
        {
            // 随机生成一个不重复的 id
            Id = Guid.NewGuid().GetHashCode();
            Kind = kind;
            SubCommandId = subCommandId;
            Model = model;
        }
        
        public override void FromBinary(BinaryReader br)
        {
            Id = br.ReadInt32();
            Kind = (EnvelopeKind)br.ReadByte();
            SubCommandId = br.ReadByte();
            Model = EnvelopeFactory.Create(Kind);
            Model.FromBinary(br);
        }

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(Id);
                bw.Write((byte)Kind);
                bw.Write(SubCommandId);
                var bytes = Model.ToBinary();
                bw.Write(bytes);
                return ms.ToArray();
            }
        }
    }
}