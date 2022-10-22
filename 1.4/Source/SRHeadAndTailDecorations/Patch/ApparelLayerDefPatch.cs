// ******************************************************************
//       /\ /|       @file       ApparelLayerDefPatch.cs
//       \ V/        @brief      服装层级定义补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 17:48:18
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SR.ModRimWorld.HeadAndTailDecorations
{
    public static class ApparelLayerDefPatch
    {
        [HarmonyPatch(typeof(ApparelLayerDef))]
        [HarmonyPatch("IsUtilityLayer", MethodType.Getter)]
        [UsedImplicitly]
        private static class IsUtilityLayerPatch
        {
            /// <summary>
            /// 将尾巴层级与腰带层级做相同的处理
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="__result"></param>
            /// <returns></returns>
            [HarmonyPrefix]
            [UsedImplicitly]
            private static bool Prefix(ApparelLayerDef __instance, ref bool __result)
            {
                __result = __instance == ApparelLayerDefOf.AFUTailDecoration
                           || __instance == RimWorld.ApparelLayerDefOf.Belt;
                return false;
            }
        }
    }
}
