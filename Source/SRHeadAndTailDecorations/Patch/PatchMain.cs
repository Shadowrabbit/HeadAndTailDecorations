// ******************************************************************
//       /\ /|       @file       PatchMain.cs
//       \ V/        @brief      patch管理器
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-01-30 21:05:24
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System.Reflection;
using HarmonyLib;
using Verse;

namespace SR.HeadAndTailDecorations
{
    [StaticConstructorOnStartup]
    public static class PatchMain
    {
        static PatchMain()
        {
            var instance = new Harmony("SR.SRHeadAndTailDecorations");
            instance.PatchAll(Assembly.GetExecutingAssembly()); //对所有特性标签的方法进行patch
        }
    }
}