// ******************************************************************
//       /\ /|       @file       ApparelLayerDefOf.cs
//       \ V/        @brief      
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-30 21:34:54
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using RimWorld;
using Verse;

namespace SR.HeadAndTailDecorations
{
    [DefOf]
    public static class ApparelLayerDefOf
    {
        public static ApparelLayerDef AFUHeadDecoration;
        public static ApparelLayerDef AFUTailDecoration;

        static ApparelLayerDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (ApparelLayerDefOf));
    }
}