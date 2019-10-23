using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("ResetForFullRebuild")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_ResetForFullRebuild_Disable {
    public static bool Prefix(AuraCache __instance) { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("HasAlreadyChecked")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_HasAlreadyChecked_Disable {
    public static bool Prefix(AuraCache __instance,ref bool __result) { __result = true; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAllAuras")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_UpdateAllAuras_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAurasToActor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_UpdateAurasToActor_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAuras")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_UpdateAuras_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(Ability), typeof(float), typeof(EffectTriggerType), typeof(bool) })]
  public static class AuraCache_UpdateAura_Ability_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(MechComponent), typeof(float), typeof(EffectTriggerType), typeof(bool) })]
  public static class AuraCache_UpdateAura_Component_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("ShouldAffectThisActor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AuraCache_ShouldAffectThisActor_Disable {
    public static bool Prefix(AuraCache __instance, ref bool __result) { __result = false; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("AuraConditionsPassed")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Ability), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_AuraConditionsPassed_Ability_Disable {
    public static bool Prefix(AuraCache __instance, ref bool __result) { __result = false; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("AuraConditionsPassed")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_AuraConditionsPassed_Component_Disable {
    public static bool Prefix(AuraCache __instance, ref bool __result) { __result = false; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("AddEffectIfNotPresent")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_AddEffectIfNotPresent_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("RemoveEffectIfPresent")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_RemoveEffectIfPresent_Disable {
    public static bool Prefix() { return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("IsAffectedByAuraFrom")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_IsAffectedByAuraFrom_Disable {
    public static bool Prefix(AuraCache __instance, ref bool __result) { __result = false; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("IsAffectedByAnyAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_IsAffectedByAnyAura_Disable {
    public static bool Prefix(AuraCache __instance, ref bool __result) { __result = false; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewAurasAffectingMe")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_PreviewAurasAffectingMe_Disable {
    public static bool Prefix(AuraCache __instance, ref Dictionary<string, List<EffectData>> __result) { __result = new Dictionary<string, List<EffectData>>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewAurasFromActorAffectingMe")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_PreviewAurasFromActorAffectingMe_Disable {
    public static bool Prefix(AuraCache __instance, ref List<EffectData> __result) { __result = new List<EffectData>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewMyAurasAffectingOthers")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_PreviewMyAurasAffectingOthers_Disable {
    public static bool Prefix(AuraCache __instance, ref Dictionary<string, List<EffectData>> __result) { __result = new Dictionary<string, List<EffectData>>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewMyAurasAffecting")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_PreviewMyAurasAffecting_Disable {
    public static bool Prefix(AuraCache __instance, ref List<EffectData> __result) { __result = new  List<EffectData>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewMyGhostSpottedCount")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_PreviewMyGhostSpottedCount_Disable {
    public static bool Prefix(AuraCache __instance, ref int __result) { __result = 0; return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Ability), typeof(float) })]
  public static class AuraCache_PreviewAuraA_Disable {
    public static bool Prefix(AuraCache __instance, ref List<EffectData> __result) { __result = new List<EffectData>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(float) })]
  public static class AuraCache_PreviewAuraC_Disable {
    public static bool Prefix(AuraCache __instance, ref List<EffectData> __result) { __result = new List<EffectData>(); return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("RefreshECMStates")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class AuraCache_RefreshECMStates_Disable {
    public static bool Prefix(AuraCache __instance) {return false; }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("GetBestECMState")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  public static class AuraCache_GetBestECMState_Disable {
    public static bool Prefix(AuraCache __instance, ref AbstractActor __result) { __result = null; return false; }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class Mech_InitGameRep_ECMREmove {
    public static void Prefix(Mech __instance) {
      __instance.AuraComponents.Clear();
    }
    public static void Postfix(Mech __instance) {
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_opponent_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECMcarrierAura_loop");
      //__instance.GameRep.PlayVFXAt(__instance.GameRep.thisTransform, Vector3.zero, "vfxPrfPrtl_ECM_loop", true, Vector3.zero, false, -1f);
    }
  }
  [HarmonyPatch(typeof(Vehicle))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  public static class Vehicle_InitGameRep_ECMREmove {
    public static void Postfix(Vehicle __instance) {
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_opponent_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECMcarrierAura_loop");
    }
  }
  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  public static class Turret_InitGameRep_ECMREmove {
    public static void Postfix(Mech __instance) {
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECM_opponent_loop");
      __instance.GameRep.StopManualPersistentVFX("vfxPrfPrtl_ECMcarrierAura_loop");
    }
  }
}