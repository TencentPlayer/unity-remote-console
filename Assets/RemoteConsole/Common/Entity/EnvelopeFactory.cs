

using System;

namespace RConsole.Common
{
    public class EnvelopeFactory
    {
        public static IBinaryModelBase Create(EnvelopeKind kind)
        {
            return kind switch
            {
                EnvelopeKind.C2SHandshake => new ClientModel(),
                EnvelopeKind.C2SLog => new LogModel(),
                EnvelopeKind.S2CLookin => new LookInViewModel(),
                EnvelopeKind.S2CFile => new FileModel(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}