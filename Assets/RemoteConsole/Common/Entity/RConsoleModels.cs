using System;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace RConsole.Common
{
    // 二进制解析基类：提供二进制读取工具
    public abstract class BinaryModelBase
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

    // 与 proto 对应的简单模型（在未启用 protobuf 运行时的情况下用于占位）
    [Serializable]
    public class LogRecordModel : BinaryModelBase
    {
        public long timestamp;
        public LogType level; // 0:Log,1:Warning,2:Error,3:Exception,4:Assert
        public string tag;
        public string message;
        public string stackTrace;
        public int threadId;

        [DoNotSerialize] public ClientInfoModel clientInfoModel;

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write((byte)EnvelopeKind.LogRecord);
                bw.Write(timestamp);
                bw.Write((int)level);
                WriteString(bw, tag);
                WriteString(bw, message);
                WriteString(bw, stackTrace);
                bw.Write(threadId);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            timestamp = br.ReadInt64();
            level = (LogType)br.ReadInt32();
            tag = ReadString(br);
            message = ReadString(br);
            stackTrace = ReadString(br);
            threadId = br.ReadInt32();
        }
    }

    [Serializable]
    public class ClientInfoModel : BinaryModelBase
    {
        public string deviceName;
        public string deviceModel;
        public string deviceId;
        public string platform;
        public string appName;
        public string appVersion;
        public string sessionId;

        [DoNotSerialize] public string connectID;
        [DoNotSerialize] public string address;
        [DoNotSerialize] public DateTime connectedAt;
        [DoNotSerialize] public bool isFiltered;

        public override byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write((byte)EnvelopeKind.Handshake);
                WriteString(bw, deviceName);
                WriteString(bw, deviceModel);
                WriteString(bw, deviceId);
                WriteString(bw, platform);
                WriteString(bw, appName);
                WriteString(bw, appVersion);
                WriteString(bw, sessionId);
                return ms.ToArray();
            }
        }

        public override void FromBinary(BinaryReader br)
        {
            deviceName = ReadString(br);
            deviceModel = ReadString(br);
            deviceId = ReadString(br);
            platform = ReadString(br);
            appName = ReadString(br);
            appVersion = ReadString(br);
            sessionId = ReadString(br);
        }
    }

    [Serializable]
    public class EnvelopeModel : BinaryModelBase
    {
        [FormerlySerializedAs("handshake")] public ClientInfoModel clientInfo;
        public LogRecordModel log;

        public static EnvelopeModel FromLog(LogRecordModel log)
        {
            return new EnvelopeModel { log = log };
        }

        public static EnvelopeModel FromHandshake(ClientInfoModel hs)
        {
            return new EnvelopeModel { clientInfo = hs };
        }

        public override void FromBinary(BinaryReader br)
        {
            var kind = (EnvelopeKind)br.ReadByte();
            if (kind == EnvelopeKind.Handshake)
            {
                clientInfo = new ClientInfoModel();
                clientInfo.FromBinary(br);
                // var env = FromHandshake(model);
            }

            if (kind == EnvelopeKind.LogRecord)
            {
                log = new LogRecordModel();
                log.FromBinary(br);
                // var env = FromLog(model);
            }
        }

        public static EnvelopeModel FromData(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                var model = new EnvelopeModel();
                model.FromBinary(br);
                return model;
            }

            return null;
        }

        public override byte[] ToBinary()
        {
            if (clientInfo != null)
            {
                return clientInfo.ToBinary();
            }
            else if (log != null)
            {
                return log.ToBinary();
            }

            return null;
        }
    }
}