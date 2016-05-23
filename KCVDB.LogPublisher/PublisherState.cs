using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace KCVDB.LogFilePublisher
{
    [ProtoContract]
    sealed class PublisherState
    {
        public Dictionary<Guid, SessionInfo> SessionInfos;

        public Dictionary<int, int> MemberIds;

        [ProtoMember(2)]
        private IEnumerable<SessionItem> sessionItems
        {
            get
            {
                return this.SessionInfos
                    ?.OrderBy(item => item.Key)
                    .Select(item => new SessionItem { PrivateSessionId = item.Key, PublicSessionId = item.Value.PublicSessionId, LineHash = item.Value.LineHash.ToByteArray() });
            }
            set
            {
                this.SessionInfos = value.ToDictionary(item => item.PrivateSessionId, item => new SessionInfo { PublicSessionId = item.PublicSessionId, LineHash = new Hash(item.LineHash) });
            }
        }

        [ProtoMember(3)]
        private IEnumerable<MemberItem> memberItems
        {
            get
            {
                return this.MemberIds
                    ?.OrderBy(item => item.Key)
                    .Select(item => new MemberItem { PrivateMemberId = item.Key, PublicMemberId = item.Value });
            }
            set
            {
                this.MemberIds = value.ToDictionary(item => item.PrivateMemberId, item => item.PublicMemberId);
            }
        }

        [ProtoContract]
        private struct SessionItem
        {
            [ProtoMember(1)]
            public Guid PrivateSessionId;

            [ProtoMember(2)]
            public Guid PublicSessionId;

            [ProtoMember(3)]
            public byte[] LineHash;
        }

        [ProtoContract]
        private struct MemberItem
        {
            [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
            public int PrivateMemberId;

            [ProtoMember(2, DataFormat = DataFormat.FixedSize)]
            public int PublicMemberId;
        }
    }

    [ProtoContract]
    sealed class SessionInfo
    {
        [ProtoMember(1)]
        public Guid PublicSessionId;

        [ProtoMember(2)]
        public Hash LineHash;
    }
}
