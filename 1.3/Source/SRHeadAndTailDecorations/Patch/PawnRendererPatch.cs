// ******************************************************************
//       /\ /|       @file       PawnRendererPatch.cs
//       \ V/        @brief      生物渲染补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 13:12:10
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace SR.ModRimWorld.HeadAndTailDecorations
{
    public static class PawnRendererPatch
    {
        [HarmonyPatch(typeof(PawnRenderer))]
        [HarmonyPatch("DrawHeadHair")]
        [HarmonyPatch(new[]
        {
            typeof(Vector3), typeof(Vector3), typeof(float), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode),
            typeof(PawnRenderFlags)
        })]
        [UsedImplicitly]
        private static class DrawHeadHairPatch
        {
            [HarmonyPrefix]
            [UsedImplicitly]
            private static bool Prefix(PawnRenderer __instance,
                Vector3 rootLoc,
                Vector3 headOffset,
                float angle,
                Rot4 bodyFacing,
                Rot4 headFacing,
                RotDrawMode bodyDrawType,
                PawnRenderFlags flags)
            {
                if (Traverse.Create(__instance).Method("ShellFullyCoversHead", flags).GetValue<bool>())
                    return false;
                var pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
                var vector3 = rootLoc + headOffset;
                vector3.y += 0.02895753f;
                var apparelGraphics = __instance.graphics.apparelGraphics;
                var quat = Quaternion.AngleAxis(angle, Vector3.up);
                var flag1 = false;
                var flag2 = bodyFacing == Rot4.North || pawn.style == null ||
                            pawn.style.beardDef == BeardDefOf.NoBeard;
                if (flags.FlagSet(PawnRenderFlags.Headgear) &&
                    (!flags.FlagSet(PawnRenderFlags.Portrait) || !Prefs.HatsOnlyOnMap ||
                     flags.FlagSet(PawnRenderFlags.StylingStation)))
                {
                    var mesh = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                    for (var index = 0; index < apparelGraphics.Count; ++index)
                    {
                        if (apparelGraphics[index].sourceApparel.def.apparel.LastLayer !=
                            RimWorld.ApparelLayerDefOf.Overhead
                            && apparelGraphics[index].sourceApparel.def.apparel.LastLayer !=
                            ApparelLayerDefOf.AFUHeadDecoration) continue;
                        if (apparelGraphics[index].sourceApparel.def.apparel.bodyPartGroups
                            .Contains(BodyPartGroupDefOf.FullHead))
                            flag2 = true;
                        if (!apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace)
                        {
                            flag1 = true;
                            var original = apparelGraphics[index].graphic.MatAt(bodyFacing);
                            var mat = flags.FlagSet(PawnRenderFlags.Cache)
                                ? original
                                : Traverse.Create(__instance).Method("OverrideMaterialIfNeeded", original, pawn,
                                    flags.FlagSet(PawnRenderFlags.Portrait)).GetValue<Material>();
                            GenDraw.DrawMeshNowOrLater(mesh, vector3, quat, mat,
                                flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                        else
                        {
                            var original = apparelGraphics[index].graphic.MatAt(bodyFacing);
                            var mat = flags.FlagSet(PawnRenderFlags.Cache)
                                ? original
                                : Traverse.Create(__instance).Method("OverrideMaterialIfNeeded", original, pawn,
                                    flags.FlagSet(PawnRenderFlags.Portrait)).GetValue<Material>();

                            var loc = rootLoc + headOffset;
                            if (apparelGraphics[index].sourceApparel.def.apparel.hatRenderedBehindHead)
                                loc.y += 0.02216602f;
                            else
                                loc.y += !(bodyFacing == Rot4.North) ||
                                         apparelGraphics[index].sourceApparel.def.apparel.hatRenderedAboveBody
                                    ? 0.03185328f
                                    : 0.002895753f;
                            GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                    }
                }

                if (ModsConfig.IdeologyActive && __instance.graphics.faceTattooGraphic != null &&
                    bodyDrawType != RotDrawMode.Dessicated &&
                    (bodyFacing != Rot4.North || pawn.style.FaceTattoo.visibleNorth))
                {
                    var loc = vector3;
                    loc.y -= 0.001447876f;
                    GenDraw.DrawMeshNowOrLater(__instance.graphics.HairMeshSet.MeshAt(headFacing), loc, quat,
                        __instance.graphics.faceTattooGraphic.MatAt(headFacing),
                        flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                if (!flag2 && bodyDrawType != RotDrawMode.Dessicated && !flags.FlagSet(PawnRenderFlags.HeadStump) &&
                    pawn.style?.beardDef != null)
                {
                    var loc =
                        Traverse.Create(__instance).Method("OffsetBeardLocationForCrownType", pawn.story.crownType,
                                headFacing, vector3)
                            .GetValue<Vector3>() + pawn.style.beardDef.GetOffset(pawn.story.crownType, headFacing);
                    var mesh = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                    var mat = __instance.graphics.BeardMatAt(headFacing, flags.FlagSet(PawnRenderFlags.Portrait),
                        flags.FlagSet(PawnRenderFlags.Cache));
                    if (mat != null)
                        GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                if (flag1 || bodyDrawType == RotDrawMode.Dessicated || flags.FlagSet(PawnRenderFlags.HeadStump))
                    return false;
                var mesh1 = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                var mat1 = __instance.graphics.HairMatAt(headFacing, flags.FlagSet(PawnRenderFlags.Portrait),
                    flags.FlagSet(PawnRenderFlags.Cache));
                if (!(mat1 != null))
                    return false;
                GenDraw.DrawMeshNowOrLater(mesh1, vector3, quat, mat1, flags.FlagSet(PawnRenderFlags.DrawNow));
                return false;
            }
        }
    }
}