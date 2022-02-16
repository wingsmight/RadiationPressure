using System.Collections.Generic;
using ProtoBuf;
using Battlehub.RTSL;
using Battlehub.RTCommon;
using Battlehub.RTCommon.Battlehub.SL2;
using UnityEngine.Battlehub.SL2;
using UnityEngine;
using System;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTCommon.Battlehub.SL2
{
    [ProtoContract]
    public partial class PersistentExposeToEditor<TID> : PersistentMonoBehaviour<TID>
    {
        [ProtoMember(257)]
        public PersistentExposeToEditorUnityEvent<TID> Unselected;

        [ProtoMember(258)]
        public TID BoundsObject;

        [ProtoMember(259)]
        public BoundsType BoundsType;

        [ProtoMember(260)]
        public PersistentBounds<TID> CustomBounds;

        [ProtoMember(262)]
        public bool CanSnap;

        [ProtoMember(263)]
        public bool AddColliders;

        [ProtoMember(272)]
        public TID[] Colliders;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            ExposeToEditor uo = (ExposeToEditor)obj;
            Unselected = uo.Unselected;
            BoundsObject = ToID(uo.BoundsObject);
            BoundsType = uo.BoundsType;
            CustomBounds = uo.CustomBounds;
            CanSnap = uo.CanSnap;
            AddColliders = uo.AddColliders;
            Colliders = ToID(uo.Colliders);
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            ExposeToEditor uo = (ExposeToEditor)obj;
            uo.Unselected = Unselected;
            uo.BoundsObject = FromID(BoundsObject, uo.BoundsObject);
            uo.BoundsType = BoundsType;
            uo.CustomBounds = CustomBounds;
            uo.CanSnap = CanSnap;
            uo.AddColliders = AddColliders;
            uo.Colliders = FromID(Colliders, uo.Colliders);
            return uo;
        }

        protected override void GetDepsImpl(GetDepsContext<TID> context)
        {
            base.GetDepsImpl(context);
            AddSurrogateDeps(Unselected, context);
            AddDep(BoundsObject, context);
            AddDep(Colliders, context);
        }

        protected override void GetDepsFromImpl(object obj, GetDepsFromContext context)
        {
            base.GetDepsFromImpl(obj, context);
            ExposeToEditor uo = (ExposeToEditor)obj;
            AddSurrogateDeps(uo.Unselected, v_ => (PersistentExposeToEditorUnityEvent<TID>)v_, context);
            AddDep(uo.BoundsObject, context);
            AddDep(uo.Colliders, context);
        }
    }
}

