// ******************************************************************
//       /\ /|       @file       PawnRendererPatch.cs
//       \ V/        @brief      生物渲染补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 13:12:10
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System;
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
            typeof(PawnRenderFlags), typeof(bool)
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
                PawnRenderFlags flags, bool bodyDrawn)
            {
                if (Traverse.Create(__instance).Method("ShellFullyCoversHead", flags).GetValue<bool>() & bodyDrawn)
                    return false;
                var onHeadLoc = rootLoc + headOffset;
                onHeadLoc.y += 0.02895753f;
                var apparelGraphics = __instance.graphics.apparelGraphics;
                var geneGraphics = __instance.graphics.geneGraphics;
                var quat = Quaternion.AngleAxis(angle, Vector3.up);
                var pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
                var num = pawn.DevelopmentalStage.Baby() || bodyDrawType == RotDrawMode.Dessicated
                    ? 1
                    : (flags.FlagSet(PawnRenderFlags.HeadStump) ? 1 : 0);
                var flag1 = num == 0 || pawn.story?.hairDef == null || pawn.story.hairDef.noGraphic;
                var flag2 = num == 0 && bodyFacing != Rot4.North && pawn.DevelopmentalStage.Adult() &&
                            (pawn.style?.beardDef ?? BeardDefOf.NoBeard) != BeardDefOf.NoBeard;
                var allFaceCovered = false;
                var drawEyes = true;
                var middleFaceCovered = false;
                var flag3 = pawn.CurrentBed() != null && !pawn.CurrentBed().def.building.bed_showSleeperBody;
                var flag4 = !flags.FlagSet(PawnRenderFlags.Portrait) & flag3;
                var flag5 = flags.FlagSet(PawnRenderFlags.Headgear) && (!flags.FlagSet(PawnRenderFlags.Portrait) ||
                                                                        !Prefs.HatsOnlyOnMap ||
                                                                        flags.FlagSet(PawnRenderFlags.StylingStation));
                var leftEyeCached = Traverse.Create(__instance).Field<BodyPartRecord>("leftEyeCached").Value;
                var rightEyeCached = Traverse.Create(__instance).Field<BodyPartRecord>("rightEyeCached").Value;
                if (leftEyeCached == null)
                    leftEyeCached =
                        pawn.def.race.body.AllParts.FirstOrDefault<BodyPartRecord>(
                            (Predicate<BodyPartRecord>) (p => p.woundAnchorTag == "LeftEye"));
                if (rightEyeCached == null)
                    rightEyeCached =
                        pawn.def.race.body.AllParts.FirstOrDefault<BodyPartRecord>(
                            (Predicate<BodyPartRecord>) (p => p.woundAnchorTag == "RightEye"));
                var hasLeftEye = leftEyeCached != null && !pawn.health.hediffSet.PartIsMissing(leftEyeCached);
                var hasRightEye = rightEyeCached != null && !pawn.health.hediffSet.PartIsMissing(rightEyeCached);
                if (flag5)
                {
                    for (int index = 0; index < apparelGraphics.Count; ++index)
                    {
                        if ((!flag4 || apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace) &&
                            (apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             RimWorld.ApparelLayerDefOf.Overhead ||
                             apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             ApparelLayerDefOf.AFUHeadDecoration ||
                             apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             RimWorld.ApparelLayerDefOf.EyeCover))
                        {
                            if (apparelGraphics[index].sourceApparel.def.apparel.bodyPartGroups
                                .Contains(BodyPartGroupDefOf.FullHead))
                            {
                                flag2 = false;
                                allFaceCovered = true;
                                if (!apparelGraphics[index].sourceApparel.def.apparel.forceEyesVisibleForRotations
                                    .Contains(headFacing.AsInt))
                                    drawEyes = false;
                            }

                            if (!apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace &&
                                !apparelGraphics[index].sourceApparel.def.apparel.forceRenderUnderHair)
                                flag1 = false;
                            if (apparelGraphics[index].sourceApparel.def.apparel.coversHeadMiddle)
                                middleFaceCovered = true;
                        }
                    }
                }

                TryDrawGenes(GeneDrawLayer.PostSkin);
                if (ModsConfig.IdeologyActive && __instance.graphics.faceTattooGraphic != null &&
                    (bodyDrawType != RotDrawMode.Dessicated && !flags.FlagSet(PawnRenderFlags.HeadStump)) &&
                    (bodyFacing != Rot4.North || pawn.style.FaceTattoo.visibleNorth))
                {
                    Vector3 loc = rootLoc + headOffset;
                    loc.y += 0.02316602f;
                    if (bodyFacing == Rot4.North)
                        loc.y -= 1f / 1000f;
                    else
                        loc.y += 1f / 1000f;
                    GenDraw.DrawMeshNowOrLater(__instance.graphics.HairMeshSet.MeshAt(headFacing), loc, quat,
                        __instance.graphics.faceTattooGraphic.MatAt(headFacing),
                        flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                TryDrawGenes(GeneDrawLayer.PostTattoo);
                if (headFacing != Rot4.North && !allFaceCovered | drawEyes)
                {
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def.eyeGraphicSouth != null && hediff.def.eyeGraphicEast != null)
                        {
                            GraphicData graphicData = headFacing.IsHorizontal
                                ? hediff.def.eyeGraphicEast
                                : hediff.def.eyeGraphicSouth;
                            bool drawLeft = hediff.Part.woundAnchorTag == "LeftEye";
                            DrawExtraEyeGraphic(graphicData.Graphic,
                                hediff.def.eyeGraphicScale *
                                pawn.ageTracker.CurLifeStage.eyeSizeFactor.GetValueOrDefault(1f), 0.0014f,
                                drawLeft, !drawLeft);
                        }
                    }
                }

                if (flag2)
                {
                    var loc = Traverse.Create(__instance).Method("OffsetBeardLocationForHead",
                            pawn.style.beardDef, pawn.story.headType, headFacing, rootLoc + headOffset)
                        .GetValue<Vector3>();
                    Mesh mesh = __instance.graphics.BeardMeshSet.MeshAt(headFacing);
                    Material mat = __instance.graphics.BeardMatAt(headFacing, flags.FlagSet(PawnRenderFlags.Portrait),
                        flags.FlagSet(PawnRenderFlags.Cache));
                    if ((UnityEngine.Object) mat != (UnityEngine.Object) null)
                        GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                if (flag5)
                {
                    for (int index = 0; index < apparelGraphics.Count; ++index)
                    {
                        if ((!flag4 || apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace) &&
                            apparelGraphics[index].sourceApparel.def.apparel.forceRenderUnderHair)
                            DrawApparel(apparelGraphics[index]);
                    }
                }

                if (flag1)
                {
                    Mesh mesh = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                    Material mat = __instance.graphics.HairMatAt(headFacing, flags.FlagSet(PawnRenderFlags.Portrait),
                        flags.FlagSet(PawnRenderFlags.Cache));
                    if ((UnityEngine.Object) mat != (UnityEngine.Object) null)
                        GenDraw.DrawMeshNowOrLater(mesh, onHeadLoc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                TryDrawGenes(GeneDrawLayer.PostHair);
                if (flag5)
                {
                    for (int index = 0; index < apparelGraphics.Count; ++index)
                    {
                        if ((!flag4 || apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace) &&
                            (apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             RimWorld.ApparelLayerDefOf.Overhead ||
                             apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             ApparelLayerDefOf.AFUHeadDecoration ||
                             apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                             RimWorld.ApparelLayerDefOf.EyeCover
                            ) && !apparelGraphics[index].sourceApparel.def.apparel.forceRenderUnderHair)
                            DrawApparel(apparelGraphics[index]);
                    }
                }

                TryDrawGenes(GeneDrawLayer.PostHeadgear);

                void DrawApparel(ApparelGraphicRecord apparelRecord)
                {
                    Mesh mesh = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                    if (!apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                    {
                        Material original = apparelRecord.graphic.MatAt(bodyFacing);
                        Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                            ? original
                            : Traverse.Create(__instance).Method("OverrideMaterialIfNeeded", original, pawn,
                                    flags.FlagSet(PawnRenderFlags.Portrait))
                                .GetValue<Material>();
                        GenDraw.DrawMeshNowOrLater(mesh, onHeadLoc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }
                    else
                    {
                        Material original = apparelRecord.graphic.MatAt(bodyFacing);
                        Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                            ? original
                            : Traverse.Create(__instance).Method("OverrideMaterialIfNeeded", original, pawn,
                                    flags.FlagSet(PawnRenderFlags.Portrait))
                                .GetValue<Material>();
                        Vector3 loc = rootLoc + headOffset;
                        if (apparelRecord.sourceApparel.def.apparel.hatRenderedBehindHead)
                            loc.y += 0.02216602f;
                        else
                            loc.y += !(bodyFacing == Rot4.North) ||
                                     apparelRecord.sourceApparel.def.apparel.hatRenderedAboveBody
                                ? 0.03185328f
                                : 0.002895753f;
                        GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }
                }

                void TryDrawGenes(GeneDrawLayer layer)
                {
                    if (!ModLister.BiotechInstalled || flags.FlagSet(PawnRenderFlags.HeadStump))
                        return;
                    for (int index = 0; index < geneGraphics.Count; ++index)
                    {
                        if (geneGraphics[index].sourceGene.def.CanDrawNow(bodyFacing, layer))
                        {
                            if (geneGraphics[index].sourceGene.def.graphicData.drawOnEyes)
                                DrawGeneEyes(geneGraphics[index]);
                            else
                                DrawGene(geneGraphics[index], layer);
                        }
                    }
                }

                void DrawGene(GeneGraphicRecord geneRecord, GeneDrawLayer layer)
                {
                    if (bodyDrawType == RotDrawMode.Dessicated &&
                        !geneRecord.sourceGene.def.graphicData.drawWhileDessicated ||
                        geneRecord.sourceGene.def.graphicData.drawLoc == GeneDrawLoc.HeadMiddle & allFaceCovered &&
                        !geneRecord.sourceGene.def.graphicData.drawIfFaceCovered ||
                        geneRecord.sourceGene.def.graphicData.drawLoc == GeneDrawLoc.HeadMiddle & middleFaceCovered &&
                        !geneRecord.sourceGene.def.graphicData.drawIfFaceCovered)
                        return;
                    Vector3 loc = Traverse.Create(__instance).Method("HeadGeneDrawLocation", geneRecord.sourceGene.def,
                            pawn.story.headType,
                            headFacing, rootLoc + headOffset, layer)
                        .GetValue<Vector3>();
                    Material original =
                        (bodyDrawType == RotDrawMode.Rotting ? geneRecord.rottingGraphic : geneRecord.graphic).MatAt(
                            headFacing);
                    Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                        ? original
                        : Traverse.Create(__instance).Method("OverrideMaterialIfNeeded", original, pawn,
                                flags.FlagSet(PawnRenderFlags.Portrait))
                            .GetValue<Material>();
                    GenDraw.DrawMeshNowOrLater(__instance.graphics.HairMeshSet.MeshAt(headFacing), loc, quat, mat,
                        flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                void DrawGeneEyes(GeneGraphicRecord geneRecord)
                {
                    if (headFacing == Rot4.North ||
                        bodyDrawType == RotDrawMode.Dessicated &&
                        !geneRecord.sourceGene.def.graphicData.drawWhileDessicated ||
                        geneRecord.sourceGene.def.graphicData.drawLoc == GeneDrawLoc.HeadMiddle & allFaceCovered &&
                        !geneRecord.sourceGene.def.graphicData.drawIfFaceCovered && !drawEyes)
                        return;
                    DrawExtraEyeGraphic(
                        bodyDrawType == RotDrawMode.Rotting ? geneRecord.rottingGraphic : geneRecord.graphic,
                        geneRecord.sourceGene.def.graphicData.drawScale *
                        pawn.ageTracker.CurLifeStage.eyeSizeFactor.GetValueOrDefault(1f), 0.0012f, hasLeftEye,
                        hasRightEye);
                }

                void DrawExtraEyeGraphic(
                    Graphic graphic,
                    float scale,
                    float yOffset,
                    bool drawLeft,
                    bool drawRight)
                {
                    bool narrowCrown = pawn.story.headType.narrow;
                    Vector3? eyeOffsetEastWest = pawn.story.headType.eyeOffsetEastWest;
                    Vector3 vector3_1 = rootLoc + headOffset + new Vector3(0.0f, 0.02606177f + yOffset, 0.0f) +
                                        quat * new Vector3(0.0f, 0.0f, -0.25f);
                    BodyTypeDef.WoundAnchor woundAnchor1 =
                        pawn.story.bodyType.woundAnchors.FirstOrDefault<BodyTypeDef.WoundAnchor>(
                            (Predicate<BodyTypeDef.WoundAnchor>) (a =>
                            {
                                if (a.tag == "LeftEye")
                                {
                                    Rot4? rotation = a.rotation;
                                    Rot4 rot4 = headFacing;
                                    if ((rotation.HasValue
                                        ? (rotation.HasValue ? (rotation.GetValueOrDefault() == rot4 ? 1 : 0) : 1)
                                        : 0) != 0)
                                        return headFacing == Rot4.South ||
                                               a.narrowCrown.GetValueOrDefault() == narrowCrown;
                                }

                                return false;
                            }));
                    BodyTypeDef.WoundAnchor woundAnchor2 =
                        pawn.story.bodyType.woundAnchors.FirstOrDefault<BodyTypeDef.WoundAnchor>(
                            (Predicate<BodyTypeDef.WoundAnchor>) (a =>
                            {
                                if (a.tag == "RightEye")
                                {
                                    Rot4? rotation = a.rotation;
                                    Rot4 rot4 = headFacing;
                                    if ((rotation.HasValue
                                        ? (rotation.HasValue ? (rotation.GetValueOrDefault() == rot4 ? 1 : 0) : 1)
                                        : 0) != 0)
                                        return headFacing == Rot4.South ||
                                               a.narrowCrown.GetValueOrDefault() == narrowCrown;
                                }

                                return false;
                            }));
                    Material mat = graphic.MatAt(headFacing);
                    if (headFacing == Rot4.South)
                    {
                        if (woundAnchor1 == null || woundAnchor2 == null)
                            return;
                        if (drawLeft)
                            GenDraw.DrawMeshNowOrLater(MeshPool.GridPlaneFlip(Vector2.one * scale),
                                Matrix4x4.TRS(vector3_1 + quat * woundAnchor1.offset, quat, Vector3.one), mat,
                                flags.FlagSet(PawnRenderFlags.DrawNow));
                        if (drawRight)
                            GenDraw.DrawMeshNowOrLater(MeshPool.GridPlane(Vector2.one * scale),
                                Matrix4x4.TRS(vector3_1 + quat * woundAnchor2.offset, quat, Vector3.one), mat,
                                flags.FlagSet(PawnRenderFlags.DrawNow));
                    }

                    if (headFacing == Rot4.East & drawRight)
                    {
                        if (woundAnchor2 == null)
                            return;
                        Vector3 vector3_2 = eyeOffsetEastWest ?? woundAnchor2.offset;
                        GenDraw.DrawMeshNowOrLater(MeshPool.GridPlane(Vector2.one * scale),
                            Matrix4x4.TRS(vector3_1 + quat * vector3_2, quat, Vector3.one), mat,
                            flags.FlagSet(PawnRenderFlags.DrawNow));
                    }

                    if (!(headFacing == Rot4.West & drawLeft) || woundAnchor1 == null)
                        return;
                    Vector3 vector3_3 = woundAnchor1.offset;
                    if (eyeOffsetEastWest.HasValue)
                        vector3_3 = eyeOffsetEastWest.Value.ScaledBy(new Vector3(-1f, 1f, 1f));
                    GenDraw.DrawMeshNowOrLater(MeshPool.GridPlaneFlip(Vector2.one * scale),
                        Matrix4x4.TRS(vector3_1 + quat * vector3_3, quat, Vector3.one), mat,
                        flags.FlagSet(PawnRenderFlags.DrawNow));
                }

                return false;
            }
        }
    }
}