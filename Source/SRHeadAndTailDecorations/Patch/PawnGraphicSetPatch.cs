// ******************************************************************
//       /\ /|       @file       PawnGraphicSetPatch.cs
//       \ V/        @brief      生物图形集补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 15:42:24
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SR.HeadAndTailDecorations
{
    public class PawnGraphicSetPatch
    {
        [HarmonyPatch(typeof(PawnGraphicSet))]
        [HarmonyPatch("MatsBodyBaseAt")]
        class MatsBodyBaseAtPatch
        {
            [HarmonyPrefix]
            static bool Prefix(PawnGraphicSet __instance, ref List<Material> __result, Rot4 facing,
                RotDrawMode bodyCondition = RotDrawMode.Fresh)
            {
                var num = facing.AsInt + 1000 * (int) bodyCondition;
                var cachedMatsBodyBaseHash = Traverse.Create(__instance).Field<int>("cachedMatsBodyBaseHash").Value;
                var cachedMatsBodyBase = Traverse.Create(__instance).Field<List<Material>>("cachedMatsBodyBase").Value;
                if (num != cachedMatsBodyBaseHash)
                {
                    cachedMatsBodyBase.Clear();
                    Traverse.Create(__instance).Field<int>("cachedMatsBodyBaseHash").Value = num;
                    switch (bodyCondition)
                    {
                        case RotDrawMode.Fresh:
                            cachedMatsBodyBase.Add(__instance.nakedGraphic.MatAt(facing));
                            break;
                        case RotDrawMode.Rotting:
                            cachedMatsBodyBase.Add(__instance.rottingGraphic.MatAt(facing));
                            break;
                        case RotDrawMode.Dessicated:
                            break;
                        default:
                            if (__instance.dessicatedGraphic != null)
                            {
                                if (bodyCondition == RotDrawMode.Dessicated)
                                {
                                    cachedMatsBodyBase.Add(__instance.dessicatedGraphic.MatAt(facing));
                                }

                                break;
                            }

                            goto case RotDrawMode.Rotting;
                    }

                    for (var index = 0; index < __instance.apparelGraphics.Count; ++index)
                    {
                        if ((__instance.apparelGraphics[index].sourceApparel.def.apparel.shellRenderedBehindHead
                             || __instance.apparelGraphics[index].sourceApparel.def.apparel.LastLayer !=
                             RimWorld.ApparelLayerDefOf.Shell)
                            && !PawnRenderer.RenderAsPack(__instance.apparelGraphics[index].sourceApparel)
                            && __instance.apparelGraphics[index].sourceApparel.def.apparel.LastLayer !=
                            RimWorld.ApparelLayerDefOf.Overhead
                            && __instance.apparelGraphics[index].sourceApparel.def.apparel.LastLayer !=
                            ApparelLayerDefOf.AFUHeadDecoration)
                        {
                            cachedMatsBodyBase.Add(__instance.apparelGraphics[index].graphic.MatAt(facing));
                        }
                    }
                }

                __result = cachedMatsBodyBase;
                return false;
            }
        }
    }
}