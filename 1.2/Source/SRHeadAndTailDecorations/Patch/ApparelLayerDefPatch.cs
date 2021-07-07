// ******************************************************************
//       /\ /|       @file       ApparelLayerDefPatch.cs
//       \ V/        @brief      服装层级定义补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 17:48:18
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using HarmonyLib;
using Verse;

namespace SR.HeadAndTailDecorations
{
    public class ApparelLayerDefPatch
    {
        [HarmonyPatch(typeof(ApparelLayerDef))]
        [HarmonyPatch("IsUtilityLayer", MethodType.Getter)]
        class IsUtilityLayerPatch
        {
            [HarmonyPrefix]
            static bool Prefix(ApparelLayerDef __instance, ref bool __result)
            {
                __result = __instance == ApparelLayerDefOf.AFUTailDecoration
                           || __instance == RimWorld.ApparelLayerDefOf.Belt;
                return false;
            }
        }
    }
}