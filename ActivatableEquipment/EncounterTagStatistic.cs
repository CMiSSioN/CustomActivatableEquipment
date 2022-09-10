using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(EffectManager))]
  [HarmonyPatch("CreateEffect")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(EffectData), typeof(string), typeof(int), typeof(ICombatant), typeof(ICombatant), typeof(WeaponHitInfo), typeof(int), typeof(bool) })]
  [HarmonyPriority(Priority.First)]
  public static class EffectManager_CreateEffect {
    public static void TestStatistic(this StatCollection stats, EffectData effectData) {
      if (stats.ContainsStatistic(effectData.statisticData.statName)) { return; }
      if (effectData.statisticData.statName.StartsWith(StatisticEffect_OnEffectBegin.ADD_ENCOUNTER_FLAG_STAT_ID) == false) { return; }
      Variant variant = new Variant(System.Type.GetType(effectData.statisticData.modType));
      switch (effectData.statisticData.operation) {
        case StatCollection.StatOperation.Set: {
          if (variant.CheckType(Variant.boolType)) {
            stats.AddStatistic<bool>(effectData.statisticData.statName, false);
          } else
          if (variant.CheckType(Variant.intType)) {
            stats.AddStatistic<int>(effectData.statisticData.statName, 0);
          } else
          if (variant.CheckType(Variant.floatType)) {
            stats.AddStatistic<float>(effectData.statisticData.statName, 0.0f);
          } else
          if (variant.CheckType(Variant.stringType)) {
            stats.AddStatistic<string>(effectData.statisticData.statName, string.Empty);
          }
        }
        break;
        case StatCollection.StatOperation.Int_Add:
        case StatCollection.StatOperation.Int_Subtract:
        case StatCollection.StatOperation.Int_Multiply:
        case StatCollection.StatOperation.Int_Divide:
        case StatCollection.StatOperation.Int_Divide_Denom:
        case StatCollection.StatOperation.Int_Mod:
        case StatCollection.StatOperation.Int_Multiply_Float:
        case StatCollection.StatOperation.Int_Divide_Float:
        case StatCollection.StatOperation.Int_Divide_Denom_Float:
        case StatCollection.StatOperation.Bitflag_SetBit:
        case StatCollection.StatOperation.Bitflag_FlipBit:
        case StatCollection.StatOperation.Bitflag_Combine: {
          stats.AddStatistic<int>(effectData.statisticData.statName, 0);
        }; break;
        case StatCollection.StatOperation.Float_Add:
        case StatCollection.StatOperation.Float_Subtract:
        case StatCollection.StatOperation.Float_Multiply:
        case StatCollection.StatOperation.Float_Divide:
        case StatCollection.StatOperation.Float_Divide_Denom:
        case StatCollection.StatOperation.Float_Multiply_Int:
        case StatCollection.StatOperation.Float_Divide_Int:
        case StatCollection.StatOperation.Float_Divide_Denom_Int: {
          stats.AddStatistic<float>(effectData.statisticData.statName, 0.0f);
        }; break;
        case StatCollection.StatOperation.String_Append:
        case StatCollection.StatOperation.String_Prepend: {
          stats.AddStatistic<string>(effectData.statisticData.statName, string.Empty);
        }; break;
      }
    }
    public static void Prefix(EffectManager __instance, EffectData effectData, ICombatant target) {
      try {
        if (effectData.effectType != EffectType.StatisticEffect) { return; }
        List<StatCollection> targetStatCollections = Traverse.Create(__instance).Method("GetTargetStatCollections", effectData, target).GetValue<List<StatCollection>>();
        for (int index = 0; index < targetStatCollections.Count; ++index) {
          targetStatCollections[index].TestStatistic(effectData);
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("InitAbstractActor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class AbstractActor_InitAbstractActor {
    private static Dictionary<StatCollection, AbstractActor> actor_by_stat_collection = new Dictionary<StatCollection, AbstractActor>();
    public static void Clear() {
      actor_by_stat_collection.Clear();
    }
    public static AbstractActor actor(this StatCollection stats) {
      if (actor_by_stat_collection.TryGetValue(stats, out var result)) { return result; } else { return null; }
    }
    public static void Prefix(AbstractActor __instance) {
      try {
        actor_by_stat_collection[__instance.StatCollection] = __instance;
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("OnEffectBegin")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatisticEffect_OnEffectBegin {
    public static readonly string ADD_ENCOUNTER_FLAG_STAT_ID = "ADD_ENCOUNTER_TAG_";
    public static void RecalcEncounterFlag(this StatisticEffect __instance) {
      if (__instance.Target == null) { return; }
      if (__instance.EffectData.statisticData.statName.StartsWith(ADD_ENCOUNTER_FLAG_STAT_ID) == false) {
        return;
      }
      string encounter_flag_name = __instance.EffectData.statisticData.statName.Substring(ADD_ENCOUNTER_FLAG_STAT_ID.Length);
      Log.Debug?.WL(1, "encounter flag name:"+ encounter_flag_name);
      Statistic encStat = __instance.Target.StatCollection.GetStatistic(__instance.EffectData.statisticData.statName);
      if (encStat == null) { return; }
      if (encStat.Value<float>() > Core.Epsilon) {
        Log.Debug?.TWL(0, "Add encounter flag by statistic " + __instance.Target.PilotableActorDef.ChassisID + " " + encounter_flag_name);
        __instance.Target.EncounterTags.Add(encounter_flag_name);
      } else {
        Log.Debug?.TWL(0, "Remove encounter flag by statistic " + __instance.Target.PilotableActorDef.ChassisID + " " + encounter_flag_name);
        __instance.Target.EncounterTags.Remove(encounter_flag_name);
      }
      __instance.Target.UpdateAuras(false);
      if (Core.Settings._C3NetworkEncounterTags.Contains(encounter_flag_name)) {
        C3Helper.Clear();
      }
    }
    public static void Postfix(StatisticEffect __instance) {
      try {
        //Log.Debug?.TWL(0, "StatisticEffect.OnEffectBegin " + (__instance.Target == null ? "null" : __instance.Target.PilotableActorDef.ChassisID) + " " + __instance.EffectData.statisticData.statName);
        __instance.RecalcEncounterFlag();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("OnEffectPhaseBegin")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatisticEffect_OnEffectPhaseBegin {
    public static void Postfix(StatisticEffect __instance) {
      try {
        //Log.Debug?.TWL(0, "StatisticEffect.OnEffectPhaseBegin " + (__instance.Target == null ? "null" : __instance.Target.PilotableActorDef.ChassisID)+" "+__instance.EffectData.statisticData.statName);
        __instance.RecalcEncounterFlag();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("OnEffectTakeDamage")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatisticEffect_OnEffectTakeDamage {
    public static void Postfix(StatisticEffect __instance) {
      try {
        //Log.Debug?.TWL(0, "StatisticEffect.OnEffectTakeDamage " + (__instance.Target == null ? "null" : __instance.Target.PilotableActorDef.ChassisID) + " " + __instance.EffectData.statisticData.statName);
        __instance.RecalcEncounterFlag();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("OnEffectEnd")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatisticEffect_OnEffectEnd {
    public static void Postfix(StatisticEffect __instance) {
      try {
        //Log.Debug?.TWL(0, "StatisticEffect.OnEffectEnd " + (__instance.Target == null ? "null" : __instance.Target.PilotableActorDef.ChassisID) + " " + __instance.EffectData.statisticData.statName);
        __instance.RecalcEncounterFlag();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("OnEffectActivationEnd")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatisticEffect_OnEffectActivationEnd {
    public static void Postfix(StatisticEffect __instance) {
      try {
        //Log.Debug?.TWL(0, "StatisticEffect.OnEffectActivationEnd " +(__instance.Target==null?"null":__instance.Target.PilotableActorDef.ChassisID) + " " + __instance.EffectData.statisticData.statName);
        __instance.RecalcEncounterFlag();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatCollection))]
  [HarmonyPatch("ModifyStatistic")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class StatCollection_ModifyStatistic {
    public static void Prefix(StatCollection __instance, string statName, StatCollection.StatOperation op, Variant variant) {
      try {
        Dictionary<string, Statistic> stats = Traverse.Create(__instance).Field<Dictionary<string, Statistic>>("stats").Value;
        if (stats.ContainsKey(statName)) { return; }
        Log.Debug?.TWL(0, "StatCollection.ModifyStatistic "+ statName+" is absent. Adding");
        switch (op) {
          case StatCollection.StatOperation.Set: {
            if (variant.CheckType(Variant.boolType)) {
              __instance.AddStatistic<bool>(statName, false);
            } else
            if (variant.CheckType(Variant.intType)) {
              __instance.AddStatistic<int>(statName, 0);
            } else
            if (variant.CheckType(Variant.floatType)) {
              __instance.AddStatistic<float>(statName, 0.0f);
            } else
            if (variant.CheckType(Variant.stringType)) {
              __instance.AddStatistic<string>(statName, string.Empty);
            }
          }
          break;
          case StatCollection.StatOperation.Int_Add:
          case StatCollection.StatOperation.Int_Subtract:
          case StatCollection.StatOperation.Int_Multiply:
          case StatCollection.StatOperation.Int_Divide:
          case StatCollection.StatOperation.Int_Divide_Denom:
          case StatCollection.StatOperation.Int_Mod:
          case StatCollection.StatOperation.Int_Multiply_Float:
          case StatCollection.StatOperation.Int_Divide_Float:
          case StatCollection.StatOperation.Int_Divide_Denom_Float: 
          case StatCollection.StatOperation.Bitflag_SetBit:
          case StatCollection.StatOperation.Bitflag_FlipBit:
          case StatCollection.StatOperation.Bitflag_Combine: { 
            __instance.AddStatistic<int>(statName, 0);
          }; break;          
          case StatCollection.StatOperation.Float_Add:
          case StatCollection.StatOperation.Float_Subtract:
          case StatCollection.StatOperation.Float_Multiply:
          case StatCollection.StatOperation.Float_Divide:
          case StatCollection.StatOperation.Float_Divide_Denom:
          case StatCollection.StatOperation.Float_Multiply_Int:
          case StatCollection.StatOperation.Float_Divide_Int:
          case StatCollection.StatOperation.Float_Divide_Denom_Int: {
            __instance.AddStatistic<float>(statName, 0.0f);
          }; break;
          case StatCollection.StatOperation.String_Append:
          case StatCollection.StatOperation.String_Prepend: {
            __instance.AddStatistic<string>(statName, string.Empty);
          }; break;
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

}