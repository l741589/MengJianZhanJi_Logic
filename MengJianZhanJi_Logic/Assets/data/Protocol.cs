using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.data {
    [ProtoContract]
    public enum Types {
        [ProtoEnum]
        Null,
        [ProtoEnum]
        GameStart,
        [ProtoEnum]
        PickRole,
        [ProtoEnum]
        ChangeStage,
        [ProtoEnum]
        Action,
        [ProtoEnum]
        SyncStatus,
        [ProtoEnum]
        TruncateMessage,
    }

}