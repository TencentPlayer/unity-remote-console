namespace RConsole.Common
{

    // Envelope 的二进制类型标识
    public enum EnvelopeKind : byte
    {
        C2SHandshake = 1,
        C2SLog = 2,
        C2SLookin = 3,
        S2CLookin = 4,
        C2SFile = 5,
        S2CFile = 6,
    }

    public enum SubLookIn : byte
    {
        LookIn = 1,
    }

    public enum SubHandshake : byte
    {
        Handshake = 1,
    }

    public enum SubLog : byte
    {
        Log = 1,
    }

    public enum SubFile : byte
    {
        FetchDirectory = 1,
        MD5 = 2,
        Download = 3,
    }

}