//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: A linear mapping value that is used by other components
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class nDMapping : MonoBehaviour
	{
		public Dictionary<string, float> values;
        nDMapping(string[] dimensions)
        {
			foreach (string d in dimensions)
			{
                if (!values.TryAdd(d, 0.0f)) {
                    throw new System.ArgumentException(d + " is a duplicate key");
                }
            }
        }
	}
}
