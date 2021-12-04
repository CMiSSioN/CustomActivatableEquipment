using BattleTech;
using Harmony;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CustomActivatableEquipment {
  //public static class CustomStatisticEffectHelper{
  //  private static ConcurrentDictionary<StatisticEffectData, CustomStatisticEffectData> customData = new ConcurrentDictionary<StatisticEffectData, CustomStatisticEffectData>();
  //  public static void Register(this StatisticEffectData __instance) {
  //    CustomStatisticEffectData result = new CustomStatisticEffectData();
  //    customData.AddOrUpdate(__instance, result, (k, v) => { return result; });
  //  }
  //  public static CustomStatisticEffectData custom(this StatisticEffectData __instance) {
  //    if (customData.TryGetValue(__instance, out CustomStatisticEffectData result) == false) {
  //      result = new CustomStatisticEffectData();
  //      customData.AddOrUpdate(__instance, result, (k,v)=> { return result; });
  //    }
  //    return result;
  //  }
  //  public static void PreSave(this StatisticEffectData __instance) {

  //  }
  //  public static void PostSave(this StatisticEffectData __instance) {

  //  }
  //  public static void PreLoad(this StatisticEffectData __instance) {

  //  }
  //  public static void PostLoad(this StatisticEffectData __instance) {

  //  }
  //}
  //[HarmonyPatch(typeof(StatisticEffectData))]
  //[HarmonyPatch(MethodType.Constructor)]
  //[HarmonyPatch(new Type[] { })]
  //public static class AttackDirector_OnAttackCompleteTA {
  //  public static void Postfix(StatisticEffectData __instance) {
  //    Log.Debug?.TWL(0, "StatisticEffectData.Constructor");
  //    __instance.Register();
  //  }
  //}

  //public class CustomStatisticEffectData {
  //  public string statName;
  //}
}