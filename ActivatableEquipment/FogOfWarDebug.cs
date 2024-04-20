using BattleTech;
using FogOfWar;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CustomActivatableEquipment {
  //[HarmonyPatch(typeof(FogOfWarView))]
  //[HarmonyPatch("Update")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class FogOfWarView_Update {
  //  public static bool SUPRESS = false;
  //  public static void Prefix(ref bool __runOriginal, FogOfWarView __instance) {
  //    if (__runOriginal == false) { return; }
  //    if (SUPRESS) { __runOriginal = false; }
  //  }
  //}
  //[HarmonyPatch(typeof(FogOfWarView))]
  //[HarmonyPatch("Update")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class FogOfWarView_Update {
  //  public static bool LOG = false;
  //  public static Stopwatch sw = new Stopwatch();
  //  public static void Prefix(ref bool __runOriginal, FogOfWarView __instance) {
  //    if (LOG) {
  //      sw.Restart();
  //      Log.Debug.TWL(0, $"FogOfWarView.Update prefix");
  //    }
  //  }
  //  public static void Postfix(ref bool __runOriginal, FogOfWarView __instance) {
  //    if (LOG) {
  //      sw.Stop();
  //      Log.Debug.TWL(0, $"FogOfWarView.Update postfix elapsed:{sw.ElapsedMilliseconds}ms");
  //    }
  //  }
  //}
  //[HarmonyPatch(typeof(FogOfWarSystem))]
  //[HarmonyPatch("UpdateAllViewers")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class FogOfWarSystem_UpdateViewers {
  //  public static Stopwatch sw = new Stopwatch();
  //  public static void Prefix(ref bool __runOriginal, FogOfWarSystem __instance) {
  //    if (FogOfWarView_Update.LOG) {
  //      sw.Restart();
  //      Log.Debug.TWL(0, $"FogOfWarSystem.UpdateAllViewers prefix");
  //    }
  //  }
  //  public static void Postfix(ref bool __runOriginal, FogOfWarView __instance) {
  //    if (FogOfWarView_Update.LOG) {
  //      sw.Stop();
  //      Log.Debug.TWL(0, $"FogOfWarSystem.UpdateAllViewers postfix elapsed:{sw.ElapsedMilliseconds}ms");
  //    }
  //  }
  //}
}