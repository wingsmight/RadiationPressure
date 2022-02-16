﻿using System;
using System.Collections;

namespace Battlehub.RTEditor
{
    public class ArrayEditor : IListEditor
    {
        protected override void SetInputField(IList value)
        {
            if (value == null)
            {
                value = (Array)Activator.CreateInstance(PropertyType, 0);
                SetValue(value);
            }

            base.SetInputField(value);
        }

        protected override IList Resize(IList list, int size)
        {
            Array newArray = (Array)Activator.CreateInstance(PropertyType, size);
            Array arr = (Array)list;
            if (arr != null && arr.Length > 0)
            {
                for (int i = 0; i < newArray.Length; ++i)
                {
                    newArray.SetValue(arr.GetValue(Math.Min(i, arr.Length - 1)), i);
                }
            }
            return newArray;
        }
    }
}

   