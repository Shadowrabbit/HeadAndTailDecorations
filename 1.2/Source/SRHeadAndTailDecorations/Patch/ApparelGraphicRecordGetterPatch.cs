// ******************************************************************
//       /\ /|       @file       ApparelGraphicRecordGetterPatch.cs
//       \ V/        @brief      服装图形记录获取补丁
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-30 21:06:58
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using HarmonyLib;
using RimWorld;
using Verse;

namespace SR.HeadAndTailDecorations
{
    public class ApparelGraphicRecordGetterPatch
    {
        [HarmonyPatch(typeof(ApparelGraphicRecordGetter))]
        [HarmonyPatch("TryGetGraphicApparel")]
        class TryGetGraphicApparelPatch
        {
            [HarmonyPrefix]
            static bool Prefix(ref bool __result, Apparel apparel,
                BodyTypeDef bodyType,
                out ApparelGraphicRecord rec)
            {
                if (bodyType == null)
                {
                    Log.Error("Getting apparel graphic with undefined body type.");
                    bodyType = BodyTypeDefOf.Male;
                }

                if (apparel.def.apparel.wornGraphicPath.NullOrEmpty())
                {
                    rec = new ApparelGraphicRecord(null, null);
                    __result = false;
                    return false;
                }

                var path =
                    apparel.def.apparel.LastLayer == RimWorld.ApparelLayerDefOf.Overhead ||
                    PawnRenderer.RenderAsPack(apparel) ||
                    //此处为添加内容 自定义的两个层级不设置体型
                    apparel.def.apparel.LastLayer == ApparelLayerDefOf.AFUHeadDecoration ||
                    apparel.def.apparel.LastLayer == ApparelLayerDefOf.AFUTailDecoration ||
                    apparel.def.apparel.wornGraphicPath == BaseContent.PlaceholderImagePath
                        ? apparel.def.apparel.wornGraphicPath
                        : apparel.def.apparel.wornGraphicPath + "_" + bodyType.defName;
                var shader = ShaderDatabase.Cutout;
                if (apparel.def.apparel.useWornGraphicMask)
                    shader = ShaderDatabase.CutoutComplex;
                var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize,
                    apparel.DrawColor);
                rec = new ApparelGraphicRecord(graphic, apparel);
                __result = true;
                return false; //false将不执行原方法
            }
        }
    }
}