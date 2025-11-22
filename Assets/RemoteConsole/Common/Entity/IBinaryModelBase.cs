
using System.IO;
using System.Text;

namespace RConsole.Common
{
    // 二进制解析基类：提供二进制读取工具
    public abstract class IBinaryModelBase
    {
        
        public abstract byte[] ToBinary();

        public abstract void FromBinary(BinaryReader br);

        protected string ReadString(BinaryReader br)
        {
            var len = br.ReadInt32();
            if (len < 0) return null;
            if (len == 0) return string.Empty;
            var bytes = br.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }


        protected void WriteString(BinaryWriter bw, string s)
        {
            if (s == null)
            {
                bw.Write(-1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(bytes.Length);
            if (bytes.Length > 0)
                bw.Write(bytes);
        }
    }

}