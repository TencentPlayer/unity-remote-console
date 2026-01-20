using System.Collections.Generic;

namespace RConsole.Common
{
    public class EnvelopResolver
    {
        private static readonly List<Resolver> Resolvers = new List<Resolver>();

        public static void Initialize()
        {
        }

        public static void OnEnable()
        {
            Resolvers.Clear();
            Register(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, () => new ClientModel(), null);
            Register(EnvelopeKind.C2SLog, (byte)SubLog.Log, () => new LogModel(), null);
            Register(EnvelopeKind.S2CLookIn, (byte)SubLookIn.LookIn, () => new StringModel(),
                () => new LookInViewModel());
            Register(EnvelopeKind.S2CFile, (byte)SubFile.FetchDirectory, () => new FileModel(),
                () => new FileModel());
            Register(EnvelopeKind.S2CFile, (byte)SubFile.MD5, () => new FileModel(), () => new FileModel());
            Register(EnvelopeKind.S2CFile, (byte)SubFile.Download, () => new FileModel(), () => new FileModel());
        }

        public static void OnDisable()
        {
            Resolvers.Clear();
        }

        public static void Register(EnvelopeKind kind, byte subKind, DataModelDelegate request,
            DataModelDelegate response)
        {
            Resolvers.Add(new Resolver(kind, subKind, request, response));
        }

        public static IBinaryModelBase GetResponse(EnvelopeKind kind, byte subKind)
        {
            var dns = Resolvers.Find(x => x.Kind == kind && x.SubKind == subKind);
            return dns?.Response?.Invoke();
        }

        public static IBinaryModelBase GetRequest(EnvelopeKind kind, byte subKind)
        {
            var dns = Resolvers.Find(x => x.Kind == kind && x.SubKind == subKind);
            return dns?.Request?.Invoke();
        }

        public static void Clear()
        {
            Resolvers.Clear();
        }
    }


    public delegate IBinaryModelBase DataModelDelegate();

    public class Resolver
    {
        public string Key = string.Empty;

        public EnvelopeKind Kind;

        public byte SubKind;

        public DataModelDelegate Request;

        public DataModelDelegate Response;

        public Resolver(EnvelopeKind kind, byte subKind, DataModelDelegate request, DataModelDelegate response)
        {
            Key = $"{kind}_{subKind}";
            Kind = kind;
            SubKind = subKind;
            Request = request;
            Response = response;
        }
    }
}