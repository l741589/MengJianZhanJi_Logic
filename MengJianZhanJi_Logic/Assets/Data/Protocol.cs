using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Data {
    [ProtoContract]
    public enum Types {
        [ProtoEnum]
        GameStart,
        [ProtoEnum]
        PickRole,
        [ProtoEnum]
        InitHandCards,
        [ProtoEnum]
        ChangeStage,
        [ProtoEnum]
        DrawCard,
        [ProtoEnum]
        AskForAction,
        [ProtoEnum]
        DispAction,
        [ProtoEnum]
        SyncStatus,
        [ProtoEnum]
        AskForCard,
    }

}