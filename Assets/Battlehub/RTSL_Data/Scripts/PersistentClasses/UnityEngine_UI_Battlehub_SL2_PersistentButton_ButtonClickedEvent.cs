using System.Collections.Generic;
using ProtoBuf;
using Battlehub.RTSL;
using UnityEngine.UI;
using UnityEngine.UI.Battlehub.SL2;
using UnityEngine.Events.Battlehub.SL2;

using UnityObject = UnityEngine.Object;
namespace UnityEngine.UI.Battlehub.SL2
{
    [ProtoContract]
    public partial class PersistentButtonNestedButtonClickedEvent<TID> : PersistentUnityEvent<TID>
    {
        
        public static implicit operator Button.ButtonClickedEvent(PersistentButtonNestedButtonClickedEvent<TID> surrogate)
        {
            if(surrogate == null) return default(Button.ButtonClickedEvent);
            return (Button.ButtonClickedEvent)surrogate.WriteTo(new Button.ButtonClickedEvent());
        }
        
        public static implicit operator PersistentButtonNestedButtonClickedEvent<TID>(Button.ButtonClickedEvent obj)
        {
            PersistentButtonNestedButtonClickedEvent<TID> surrogate = new PersistentButtonNestedButtonClickedEvent<TID>();
            surrogate.ReadFrom(obj);
            return surrogate;
        }
    }
}

