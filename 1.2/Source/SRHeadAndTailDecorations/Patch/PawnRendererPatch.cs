// ******************************************************************
//       /\ /|       @file       PawnRendererPatch.cs
//       \ V/        @brief      生物渲染补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-31 13:12:10
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SR.HeadAndTailDecorations
{
    public class PawnRendererPatch
    {
        [HarmonyPatch(typeof(PawnRenderer))]
        [HarmonyPatch("RenderPawnInternal")]
        [HarmonyPatch(new[]
        {
            typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool),
            typeof(bool), typeof(bool)
        })]
        class RenderPawnInternalPatch
        {
            [HarmonyPrefix]
            static bool Prefix(PawnRenderer __instance, Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing,
                Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait, bool headStump, bool invisible)
            {
                var pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
                if (!__instance.graphics.AllResolved)
                    __instance.graphics.ResolveAllGraphics();
                Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
                Mesh mesh1 = null;
                if (renderBody)
                {
                    Vector3 loc = rootLoc;
                    loc.y += 0.009183673f;
                    if (bodyDrawType == RotDrawMode.Dessicated && !pawn.RaceProps.Humanlike &&
                        (__instance.graphics.dessicatedGraphic != null && !portrait))
                    {
                        __instance.graphics.dessicatedGraphic.Draw(loc, bodyFacing, pawn, angle);
                    }
                    else
                    {
                        mesh1 = !pawn.RaceProps.Humanlike
                            ? __instance.graphics.nakedGraphic.MeshAt(bodyFacing)
                            : MeshPool.humanlikeBodySet.MeshAt(bodyFacing);
                        List<Material> materialList = __instance.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
                        for (int index = 0; index < materialList.Count; ++index)
                        {
                            var mat = Traverse.Create(__instance).Method("OverrideMaterialIfNeeded_NewTemp",
                                    materialList[index], pawn, portrait)
                                .GetValue<Material>();
                            GenDraw.DrawMeshNowOrLater(mesh1, loc, quaternion, mat, portrait);
                            loc.y += 3f / 980f;
                        }

                        if (bodyDrawType == RotDrawMode.Fresh)
                        {
                            Vector3 drawLoc = rootLoc;
                            drawLoc.y += 0.01836735f;
                            var woundOverlays = Traverse.Create(__instance).Field<PawnWoundDrawer>("woundOverlays")
                                .Value;
                            woundOverlays.RenderOverBody(drawLoc, mesh1, quaternion, portrait);
                        }
                    }
                }

                Vector3 vector3_1 = rootLoc;
                Vector3 vector3_2 = rootLoc;
                if (bodyFacing != Rot4.North)
                {
                    vector3_2.y += 0.0244898f;
                    vector3_1.y += 0.02142857f;
                }
                else
                {
                    vector3_2.y += 0.02142857f;
                    vector3_1.y += 0.0244898f;
                }

                Vector3 vector = rootLoc;
                vector.y += bodyFacing == Rot4.South ? 3f / 490f : 0.02755102f;
                List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;
                if (__instance.graphics.headGraphic != null)
                {
                    Vector3 vector3_3 = quaternion * __instance.BaseHeadOffsetAt(headFacing);
                    Material mat1 =
                        __instance.graphics.HeadMatAt_NewTemp(headFacing, bodyDrawType, headStump, portrait);
                    if (mat1 != null)
                        GenDraw.DrawMeshNowOrLater(MeshPool.humanlikeHeadSet.MeshAt(headFacing), vector3_2 + vector3_3,
                            quaternion, mat1, portrait);
                    Vector3 loc1 = rootLoc + vector3_3;
                    loc1.y += 0.03061225f;
                    bool flag = false;
                    if (!portrait || !Prefs.HatsOnlyOnMap)
                    {
                        Mesh mesh2 = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                        for (int index = 0; index < apparelGraphics.Count; ++index)
                        {
                            if (
                                apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                                RimWorld.ApparelLayerDefOf.Overhead
                                //帽子绘制的时候把头部装饰也绘制一下
                                || apparelGraphics[index].sourceApparel.def.apparel.LastLayer ==
                                ApparelLayerDefOf.AFUHeadDecoration
                            )
                            {
                                if (!apparelGraphics[index].sourceApparel.def.apparel.hatRenderedFrontOfFace)
                                {
                                    flag = true;
                                    var mat2 = Traverse.Create(__instance).Method("OverrideMaterialIfNeeded_NewTemp",
                                            apparelGraphics[index].graphic.MatAt(bodyFacing), pawn, portrait)
                                        .GetValue<Material>();
                                    GenDraw.DrawMeshNowOrLater(mesh2, loc1, quaternion, mat2, portrait);
                                }
                                else
                                {
                                    var mat2 = Traverse.Create(__instance).Method(
                                            "OverrideMaterialIfNeeded_NewTemp",
                                            apparelGraphics[index].graphic.MatAt(bodyFacing), pawn, portrait)
                                        .GetValue<Material>();
                                    Vector3 loc2 = rootLoc + vector3_3;
                                    loc2.y += bodyFacing == Rot4.North ? 3f / 980f : 0.03367347f;
                                    GenDraw.DrawMeshNowOrLater(mesh2, loc2, quaternion, mat2, portrait);
                                }
                            }
                        }
                    }

                    if (!flag && bodyDrawType != RotDrawMode.Dessicated && !headStump)
                    {
                        Mesh mesh2 = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                        Material material = __instance.graphics.HairMatAt_NewTemp(headFacing, portrait);
                        Vector3 loc2 = loc1;
                        Quaternion quat = quaternion;
                        Material mat2 = material;
                        int num = portrait ? 1 : 0;
                        GenDraw.DrawMeshNowOrLater(mesh2, loc2, quat, mat2, num != 0);
                    }
                }

                if (renderBody)
                {
                    for (int index = 0; index < apparelGraphics.Count; ++index)
                    {
                        ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[index];
                        if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer ==
                            RimWorld.ApparelLayerDefOf.Shell &&
                            !apparelGraphicRecord.sourceApparel.def.apparel.shellRenderedBehindHead)
                        {
                            var mat = Traverse.Create(__instance).Method("OverrideMaterialIfNeeded_NewTemp",
                                    apparelGraphicRecord.graphic.MatAt(bodyFacing), pawn, portrait)
                                .GetValue<Material>();
                            GenDraw.DrawMeshNowOrLater(mesh1, vector3_1, quaternion, mat, portrait);
                        }

                        if (PawnRenderer.RenderAsPack(apparelGraphicRecord.sourceApparel))
                        {
                            var mat = Traverse.Create(__instance).Method("OverrideMaterialIfNeeded_NewTemp",
                                    apparelGraphicRecord.graphic.MatAt(bodyFacing), pawn, portrait)
                                .GetValue<Material>();
                            if (apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData != null)
                            {
                                Vector2 vector2_1 =
                                    apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltOffsetAt(
                                        bodyFacing, pawn.story.bodyType);
                                Vector2 vector2_2 =
                                    apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltScaleAt(
                                        pawn.story.bodyType);
                                Matrix4x4 matrix = Matrix4x4.Translate(vector) * Matrix4x4.Rotate(quaternion) *
                                                   Matrix4x4.Translate(new Vector3(vector2_1.x, 0.0f, vector2_1.y)) *
                                                   Matrix4x4.Scale(new Vector3(vector2_2.x, 1f, vector2_2.y));
                                GenDraw.DrawMeshNowOrLater_NewTemp(mesh1, matrix, mat, portrait);
                            }
                            else
                                GenDraw.DrawMeshNowOrLater(mesh1, vector3_1, quaternion, mat, portrait);
                        }
                    }
                }

                if (!portrait && pawn.RaceProps.Animal &&
                    (pawn.inventory != null && pawn.inventory.innerContainer.Count > 0) &&
                    __instance.graphics.packGraphic != null)
                    Graphics.DrawMesh(mesh1, vector3_1, quaternion, __instance.graphics.packGraphic.MatAt(bodyFacing),
                        0);
                if (portrait)
                    return false;
                Traverse.Create(__instance).Method("DrawEquipment", rootLoc).GetValue();
                if (pawn.apparel != null)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    for (int index = 0; index < wornApparel.Count; ++index)
                        wornApparel[index].DrawWornExtras();
                }

                Vector3 bodyLoc = rootLoc;
                bodyLoc.y += 0.03979592f;
                var statusOverlays = Traverse.Create(__instance).Field<PawnHeadOverlays>("statusOverlays").Value;
                statusOverlays.RenderStatusOverlays(bodyLoc, quaternion,
                    MeshPool.humanlikeHeadSet.MeshAt(headFacing));
                return false;
            }
        }
    }
}