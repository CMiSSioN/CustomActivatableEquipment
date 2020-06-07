using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomActivatableEquipment {
  public static class StatisticHelper {
    public static Statistic GetOrCreateStatisic<StatisticType>(this StatCollection collection, string statName, StatisticType defaultValue) {
      Statistic statistic = collection.GetStatistic(statName);
      if (statistic == null) {
        statistic = collection.AddStatistic<StatisticType>(statName, defaultValue);
      }
      return statistic;
    }
    public static void SetOrCreateStatisic<StatisticType>(this StatCollection collection, string statName, StatisticType value) {
      Statistic statistic = collection.GetStatistic(statName);
      if (statistic == null) {
        statistic = collection.AddStatistic<StatisticType>(statName, value);
      } else {
        statistic.SetValue(value);
      }      
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("InitEffectStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class AbstractActor_InitEffectStatsHeadHit {
    public static void Postfix(AbstractActor __instance) {
      __instance.StatCollection.AddStatistic<bool>(Core.Settings.unaffectedByHeadHitStatName, false);
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("ApplyHeadStructureEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(ChassisLocations), typeof(LocationDamageLevel), typeof(LocationDamageLevel), typeof(WeaponHitInfo) })]
  public static class Mech_ApplyHeadStructureEffects {
    public static bool Prefix(Mech __instance, ChassisLocations location, LocationDamageLevel oldDamageLevel, LocationDamageLevel newDamageLevel, WeaponHitInfo hitInfo) {
      if (__instance.StatCollection.GetOrCreateStatisic<bool>(Core.Settings.unaffectedByHeadHitStatName, false).Value<bool>()) {
        return false;
      }
      return true;
    }
  }
}