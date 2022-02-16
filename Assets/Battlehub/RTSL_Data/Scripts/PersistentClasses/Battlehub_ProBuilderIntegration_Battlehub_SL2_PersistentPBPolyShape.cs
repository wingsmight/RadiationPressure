using System.Collections.Generic;
using ProtoBuf;
using Battlehub.RTSL;
using Battlehub.ProBuilderIntegration;
using Battlehub.ProBuilderIntegration.Battlehub.SL2;
using UnityEngine.Battlehub.SL2;
using System;
using UnityEngine;

using UnityObject = UnityEngine.Object;
namespace Battlehub.ProBuilderIntegration.Battlehub.SL2
{
    [ProtoContract]
    public partial class PersistentPBPolyShape<TID> : PersistentMonoBehaviour<TID>
    {
        [ProtoMember(257)]
        public int Stage;

        [ProtoMember(258)]
        public List<PersistentVector3<TID>> Positions;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            PBPolyShape uo = (PBPolyShape)obj;
            Stage = uo.Stage;
            Positions = Assign(uo.Positions, v_ => (PersistentVector3<TID>)v_);
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            PBPolyShape uo = (PBPolyShape)obj;
            uo.Stage = Stage;
            uo.Positions = Assign(Positions, v_ => (Vector3)v_);
            return uo;
        }
    }
}

