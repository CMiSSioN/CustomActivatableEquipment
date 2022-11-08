using BattleTech;
using Harmony;
using HBS.Collections;
using IRBTModUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CustomActivatableEquipment {
  public static class CustomStatisticEffectHelper {
    private static Dictionary<StatCollection, MechComponent> statCollectionsRegistry = new Dictionary<StatCollection, MechComponent>();
    public static void RegisterStatCollection(this MechComponent component) {
      statCollectionsRegistry[component.StatCollection] = component;
    }
    public static MechComponent getComponent(this StatCollection statCollection) {
      if (statCollectionsRegistry.TryGetValue(statCollection, out var result)) { return result; }
      return null;
    }
    public static void Clear() { statCollectionsRegistry.Clear(); }
  }
  //[HarmonyPatch(typeof(MechComponent))]
  //[HarmonyPatch("InitStats")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { })]
  //public static class MechComponent_InitStats {
  //  public static void Postfix(MechComponent __instance) {
  //    try { 
  //      __instance.RegisterStatCollection();
  //    } catch (Exception e) {
  //      Log.Error?.TWL(0, e.ToString(), true);
  //    }
  //  }
  //}
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("InitPassiveSelfEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechComponent_InitPassiveSelfEffects {
    public static void Prefix(MechComponent __instance) {
      try {
        Thread.CurrentThread.pushToStack<MechComponent>("EFFECT_SOURCE", __instance);
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
    public static void Postfix(MechComponent __instance) {
      try {
        Thread.CurrentThread.popFromStack<MechComponent>("EFFECT_SOURCE");
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(EffectManager))]
  [HarmonyPatch("EffectComplete")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Effect) })]
  public static class EffectManager_EffectComplete {
    public static void Postfix(EffectManager __instance, Effect e) {
      try {
        if (EffectManager_GetTargetStatCollections.StatisticEffectData_Location == null) { return; }
        string SourceLocation = EffectManager_GetTargetStatCollections.StatisticEffectData_Location.GetValue(e.EffectData.statisticData) as string;
        if (SourceLocation != "{onlyone}") { return; }
        if (e is StatisticEffect statEffect) {
          StatCollection statCollection = Traverse.Create(statEffect).Property<StatCollection>("statCollection").Value;
          if (statCollection == null) { return; }
          statCollection.RemoveStatistic(e.EffectData.Description.Id+"_only_one_tracker");
        }
      } catch (Exception ex) {
        Log.Error?.TWL(0, ex.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(StatisticEffect))]
  [HarmonyPatch("initStatisiticEffect")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(ICombatant),typeof(EffectData),typeof(StatCollection) })]
  public static class StatisticEffect_initStatisiticEffect {
    public static void Postfix(StatisticEffect __instance, ICombatant target, EffectData effectData, StatCollection targetStatCollection) {
      try {
        if (EffectManager_GetTargetStatCollections.StatisticEffectData_Location == null) { return; }
        string SourceLocation = EffectManager_GetTargetStatCollections.StatisticEffectData_Location.GetValue(effectData.statisticData) as string;
        if ((SourceLocation == "{onlyone}") && (targetStatCollection != null)) {
          targetStatCollection.GetOrCreateStatisic<bool>(effectData.Description.Id + "_only_one_tracker", false).SetValue<bool>(true);
        }
        AbstractActor unit = targetStatCollection.actor();
        MechComponent sourceComponent = Thread.CurrentThread.peekFromStack<MechComponent>("EFFECT_SOURCE");
        Log.Debug?.TWL(0,$"StatisticEffect.initStatisiticEffect target:{(unit==null?"null":unit.PilotableActorDef.ChassisID)} sourceComponent:{(sourceComponent==null?"null":sourceComponent.defId)}");
        if ((unit != null)&&(string.IsNullOrEmpty(SourceLocation) == false)) {
          if (sourceComponent != null) {
            int sourceLocation = -1;
            if (SourceLocation == "{current}") {
              if (sourceComponent != null) {
                sourceLocation = sourceComponent.Location;
              }
            } else {
              if (Enum.TryParse<ChassisLocations>(SourceLocation, out var cloc)) {
                sourceLocation = (int)cloc;
              } else if (Enum.TryParse<VehicleChassisLocations>(SourceLocation, out var vloc)) {
                sourceLocation = (int)vloc.FakeVehicleLocation();
              } else if (Enum.TryParse<BuildingLocation>(SourceLocation, out var bloc)) {
                sourceLocation = 1;
              }
            }
            Statistic stat = targetStatCollection.GetStatistic(effectData.statisticData.statName);
            if ((sourceLocation >= 0) && (stat != null)) {
              string locstatname = sourceLocation.ToString() + "." + effectData.statisticData.statName;
              if (targetStatCollection.GetStatistic(locstatname) == null) {
                MethodInfo AddStatistic = targetStatCollection.GetType().GetMethods().First(x => {return x.Name == "AddStatistic" && (x.GetParameters().Length == 2); });
                object defValue = stat.GetType().GetMethod("DefaultValue").MakeGenericMethod(stat.ValueType()).Invoke(stat, new object[] { });
                AddStatistic.MakeGenericMethod(stat.ValueType()).Invoke(targetStatCollection, new object[] { locstatname, defValue });
              }
              Log.Debug?.WL(1, $"location stat name:{locstatname}");
              __instance.ModVariant.statName = locstatname;
            }
          }
        }
      } catch (Exception ex) {
        Log.Error?.TWL(0, ex.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(EffectManager))]
  [HarmonyPatch("GetTargetStatCollections")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(EffectData), typeof(ICombatant) })]
  public static class EffectManager_GetTargetStatCollections {
    public static FieldInfo StatisticEffectData_Location;
    public static FieldInfo StatisticEffectData_ShouldHaveTags;
    public static FieldInfo StatisticEffectData_ShouldNotHaveTags;
    private static bool Prepare_already_run = false;
    public static bool Prepare() {
      if (Prepare_already_run) { return true; }
      Prepare_already_run = true;
      StatisticEffectData_Location = AccessTools.Field(typeof(StatisticEffectData), "Location");
      Log.Debug?.TWL(0, $"EffectManager.GetTargetStatCollections Prepare StatisticEffectData.Location {(StatisticEffectData_Location==null?"not found":"found")}");
      StatisticEffectData_ShouldHaveTags = AccessTools.Field(typeof(StatisticEffectData), "ShouldHaveTags");
      Log.Debug?.TWL(0, $"EffectManager.GetTargetStatCollections Prepare StatisticEffectData.ShouldHaveTags {(StatisticEffectData_ShouldHaveTags == null ? "not found" : "found")}");
      StatisticEffectData_ShouldNotHaveTags = AccessTools.Field(typeof(StatisticEffectData), "ShouldNotHaveTags");
      Log.Debug?.TWL(0, $"EffectManager.GetTargetStatCollections Prepare StatisticEffectData.ShouldNotHaveTags {(StatisticEffectData_ShouldNotHaveTags == null ? "not found" : "found")}");
      //Log.Debug?.WL(0, Environment.StackTrace);
      return StatisticEffectData_Location != null;
    }
    public static HashSet<string> ShouldHaveTags(this StatisticEffectData statisticEffectData) {
      if (StatisticEffectData_ShouldHaveTags == null) { return new HashSet<string>(); }
      string shouldHaveTags = StatisticEffectData_ShouldHaveTags.GetValue(statisticEffectData) as string;
      if (string.IsNullOrEmpty(shouldHaveTags)) { return new HashSet<string>(); }
      return shouldHaveTags.Split(',').ToHashSet();
    }
    public static HashSet<string> ShouldNotHaveTags(this StatisticEffectData statisticEffectData) {
      if (StatisticEffectData_ShouldNotHaveTags == null) { return new HashSet<string>(); }
      string shouldNotHaveTags = StatisticEffectData_ShouldNotHaveTags.GetValue(statisticEffectData) as string;
      if (string.IsNullOrEmpty(shouldNotHaveTags)) { return new HashSet<string>(); }
      return shouldNotHaveTags.Split(',').ToHashSet();
    }
    public static void Postfix(EffectManager __instance, EffectData effectData, ICombatant target, ref List<StatCollection> __result) {
      try {
        if (StatisticEffectData_Location == null) {
          return;
        }
        string SourceLocation = StatisticEffectData_Location.GetValue(effectData.statisticData) as string;
        Log.Debug?.TWL(0, $"EffectManager.GetTargetStatCollections {effectData.Description.Id} sourceLocation:{SourceLocation} collections:{__result.Count}");
        if (string.IsNullOrEmpty(SourceLocation)) { return; }
        int sourceLocation = -1;
        bool isAbove = false;
        bool isOnlyOne = false;
        bool isTarget = false;
        if (SourceLocation == "{above}") {
          isAbove = true;
          SourceLocation = "{current}";
        }
        if(SourceLocation == "{onlyone}") {
          isOnlyOne = true;
          SourceLocation = "{current}";
        }
        if (SourceLocation == "{target}") {
          isTarget = true;
          SourceLocation = "{current}";
        }
        MechComponent sourceComponent = Thread.CurrentThread.peekFromStack<MechComponent>("EFFECT_SOURCE");
        if (SourceLocation == "{current}") {
          if (sourceComponent == null) { return; }
          sourceLocation = sourceComponent.Location;
        } else {
          if(Enum.TryParse<ChassisLocations>(SourceLocation, out var cloc)) {
            sourceLocation = (int)cloc;
          } else if(Enum.TryParse<VehicleChassisLocations>(SourceLocation, out var vloc)) {
            sourceLocation = (int)vloc.FakeVehicleLocation();
          } else if(Enum.TryParse<BuildingLocation>(SourceLocation, out var bloc)) {
            sourceLocation = 1;
          }
        }
        if (sourceLocation < 0) { return; }
        HashSet<StatCollection> result = new HashSet<StatCollection>();
        TagSet ShouldNotHaveTags = new TagSet(effectData.statisticData.ShouldNotHaveTags());
        TagSet ShouldHaveTags = new TagSet(effectData.statisticData.ShouldHaveTags());
        MechComponent aboveComponent = null;
        foreach (StatCollection statCollection in __result) {
          MechComponent targetComponent = statCollection.getComponent();
          if (targetComponent == null) { result.Add(statCollection); continue; }
          int targetLocation = targetComponent.Location;
          if (targetComponent.vehicleComponentRef != null) {
            targetLocation = (int)targetComponent.vehicleComponentRef.MountedLocation.FakeVehicleLocation();
          }
          Log.Debug?.WL(1, $"component {targetComponent.defId} UID:{targetComponent.uid} location:{targetLocation} effect location:{sourceLocation}");
          if (ShouldNotHaveTags.Count > 0) {
            if (targetComponent.componentDef.ComponentTags.ContainsAny(ShouldNotHaveTags)) { continue; }
          }
          if (ShouldHaveTags.Count > 0) {
            if (targetComponent.componentDef.ComponentTags.ContainsAll(ShouldHaveTags) == false) { continue; }
          }
          if (isTarget) {
            if (string.IsNullOrEmpty(targetComponent.baseComponentRef.LocalGUID()) == false) {
              if(targetComponent.baseComponentRef.LocalGUID() == sourceComponent.baseComponentRef.TargetComponentGUID()) {
                result.Add(statCollection);
              }
            }
            continue;
          }
          if (targetLocation != sourceLocation) { continue; }
          if ((targetComponent.uid.CompareTo(sourceComponent.uid) < 0)) {
            if ((aboveComponent == null)||(targetComponent.uid.CompareTo(aboveComponent.uid) > 0)) { aboveComponent = targetComponent; }
          }
          if (isAbove) { continue; }
          if (isOnlyOne) { if (statCollection.GetStatistic(effectData.Description.Id+"_only_one_tracker") != null) {
              Log.Debug?.WL(2, $"already applied");
              continue;
            }
          }
          result.Add(statCollection);
          if (isOnlyOne) { break; }
        }
        if (aboveComponent != null) {
          Log.Debug?.WL(1, $"above component {aboveComponent.defId} UID:{aboveComponent.uid} sourceComponent.UID:{sourceComponent.uid}");
          result.Add(aboveComponent.StatCollection);
        }
        __result = result.ToList();
        Log.Debug?.WL(1, $"filtered:{__result.Count}");
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

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