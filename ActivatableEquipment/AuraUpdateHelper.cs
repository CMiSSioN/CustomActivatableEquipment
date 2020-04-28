using BattleTech;
using BattleTech.UI;
using CustomComponents;
using FogOfWar;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace CustomActivatableEquipment {
  public partial class ActivatableComponent {
    [JsonIgnore]
    private EffectData[] FallEffects;
    public void FixStatusEffects() {
      if (this.statusEffects != null) {
        List<EffectData> result = new List<EffectData>();
      }
    }
    [JsonIgnore]
    public EffectData[] allEffects {
      get {
        if (FallEffects != null) { return FallEffects; }
        List<EffectData> result = new List<EffectData>();
        result.AddRange(this.Def.statusEffects);
        //Log.LogWrite("ActivatableComponent.AllEffects\n");
        //foreach(EffectData ef in result) {

        //}
        result.AddRange(this.statusEffects);
        result.AddRange(this.offlineStatusEffects);
        return result.ToArray();
      }
    }
    [JsonIgnore]
    private HashSet<EffectData> FonlineEffects;
    [JsonIgnore]
    public HashSet<EffectData> onlineEffects {
      get {
        if (FonlineEffects != null) { return FonlineEffects; }
        FonlineEffects = new HashSet<EffectData>();
        foreach (EffectData effect in this.statusEffects) { FonlineEffects.Add(effect); }
        return FonlineEffects;
      }
    }
    [JsonIgnore]
    private HashSet<EffectData> FofflineEffects;
    [JsonIgnore]
    public HashSet<EffectData> offlineEffects {
      get {
        if (FofflineEffects != null) { return FofflineEffects; }
        FofflineEffects = new HashSet<EffectData>();
        foreach (EffectData effect in this.offlineStatusEffects) { FofflineEffects.Add(effect); }
        return FofflineEffects;
      }
    }
    [JsonIgnore]
    private HashSet<EffectData> FbaseEffects;
    [JsonIgnore]
    public HashSet<EffectData> baseEffects {
      get {
        if (FbaseEffects != null) { return FbaseEffects; }
        FbaseEffects = new HashSet<EffectData>();
        foreach (EffectData effect in this.Def.statusEffects) { FbaseEffects.Add(effect); }
        return FbaseEffects;
      }
    }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAuras")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(EffectTriggerType), typeof(bool) })]
  public static class AuraCache_UpdateAurasComponents {
    public static MethodInfo FOwner;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n", true);
        return false;
      }
      return true;
    }
    public static bool Prefix(AuraCache __instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, float distSquared, EffectTriggerType triggerSource, bool forceUpdate) {
      //AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      //Log.LogWrite("AuraCache.UpdateAuras prefix owner:" + Owner.DisplayName + ":" + Owner.GUID + " from: " + fromActor.DisplayName + ":" + fromActor.GUID + " AuraComponents:" + fromActor.AuraComponents.Count + " forceUpdate:" + forceUpdate + "\n");
      return false;
    }
  }
  /*[HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshAuraRange")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(ButtonState) })]
  public static class CombatAuraReticle_RefreshAuraRange {
    public static FieldInfo Fowner;
    public static FieldInfo FcurrentAuraRange;
    public static MethodInfo FauraRangeScaledObject;
    public static bool Prepare() {
      Fowner = typeof(CombatAuraReticle).GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic);
      FcurrentAuraRange = typeof(CombatAuraReticle).GetField("currentAuraRange", BindingFlags.Instance | BindingFlags.NonPublic);
      FauraRangeScaledObject = typeof(CombatAuraReticle).GetProperty("auraRangeScaledObject", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      if (Fowner == null) {
        Log.LogWrite("ERROR!:Can't find owner field\n", true);
        return false;
      }
      if (FcurrentAuraRange == null) {
        Log.LogWrite("ERROR!:Can't find currentAuraRange field\n", true);
        return false;
      }
      if (FauraRangeScaledObject == null) {
        Log.LogWrite("ERROR!:Can't find auraRangeScaledObject property\n", true);
        return false;
      }
      return true;
    }
    public static bool Prefix(CombatAuraReticle __instance, ButtonState auraProjectionState) {
      GameObject auraRangeScaledObject = (GameObject)FauraRangeScaledObject.Invoke(__instance,new object[0] { });
      AbstractActor owner = (AbstractActor)Fowner.GetValue(__instance);
      if (auraProjectionState == ButtonState.Disabled) {
        auraRangeScaledObject.SetActive(false);
      } else {
        auraRangeScaledObject.SetActive(true);
        float b = 0.0f;
        if (owner.AuraAbilities.Count > 0) {
          b = owner.AuraAbilities[0].Def.EffectData[0].targetingData.range;
        } else if (owner.AuraComponents.Count > 0) {
          foreach(MechComponent component in owner.AuraComponents) {
            foreach(EffectData effect in component.componentDef.statusEffects) {
              if (effect.targetingData.specialRules != AbilityDef.SpecialRules.Aura) { continue; }
              if (b < effect.targetingData.range) { b = effect.targetingData.range; }
            }
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) {continue;}
            if (ActivatableComponent.isComponentActivated(component)) {
              foreach (EffectData effect in activatable.statusEffects) {
                if (effect.targetingData.specialRules != AbilityDef.SpecialRules.Aura) { continue; }
                if (b < effect.targetingData.range) { b = effect.targetingData.range; }
              }
            } else {
              foreach (EffectData effect in activatable.offlineStatusEffects) {
                if (effect.targetingData.specialRules != AbilityDef.SpecialRules.Aura) { continue; }
                if (b < effect.targetingData.range) { b = effect.targetingData.range; }
              }

            }
          }
        }
        if (!Mathf.Approximately((float)FcurrentAuraRange.GetValue(__instance), b)) {
          //Log.LogWrite("Updating currentAuraRange"+b+"\n");
          auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          FcurrentAuraRange.SetValue(__instance, b);
          //__instance.currentAuraRange = b;
        }
      }
      return false;
    }
  }*/
  /*[HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("CreateEffect")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(EffectData), typeof(Ability), typeof(string), typeof(int), typeof(AbstractActor), typeof(bool) })]
  public static class AbstractActor_CreateEffect {
    public static bool Prefix(AbstractActor __instance, EffectData effect, Ability fromAbility, string effectId, int stackItemUID, AbstractActor creator, bool skipLogging,ref bool __result) {
      Log.LogWrite("AbstractActor.CreateEffect prefix");
      try {
        Log.LogWrite(" this:"
          + (__instance != null ? (__instance.DisplayName + ":" + __instance.GUID) : "null")
          + " effect: " + (effect != null ? effect.ToString() : "null")
          + " fromAbility:" + (fromAbility != null ? fromAbility.ToString() : "null")
          + " creator:" + (creator != null ? (creator.ToString()) : "null") + "\n"
        );
        Log.LogWrite(" creating effect\n");
        List<Effect> effect1 = __instance.Combat.EffectManager.CreateEffect(effect, effectId, stackItemUID, (ICombatant)creator, (ICombatant)__instance, new WeaponHitInfo(), 0, skipLogging);
        Log.LogWrite(" created effect\n");
        if (effect.targetingData.forcePathRebuild && __instance.Pathing != null) {
          Log.LogWrite(" reset pathing\n");
          __instance.ResetPathing(false);
          if (__instance == creator)
            __instance.Combat.PathingManager.AddNewBlockingPath(__instance.Pathing);
        }
        if (effect.targetingData.forceVisRebuild) {
          Log.LogWrite(" changing fow\n");
          FogOfWarSystem fowSystem = FogOfWarSystem.Instance;
          if (fowSystem != null) {
            FogOfWarSystem.Instance.UpdateViewer(__instance);
          } else {
            Log.LogWrite(" can't get fow\n");
          }
          if (__instance.VisibilityCache != null) {
            __instance.VisibilityCache.UpdateCacheReciprocal(__instance.Combat.GetAllImporantCombatants());
          } else {
            Log.LogWrite(" can't get VisibilityCache\n");
          }
          Log.LogWrite(" changed fow\n");
        }
        if (effect1.Count > 0) {
          Log.LogWrite(" adding mark\n");
          typeof(AbstractActor).GetMethod("ProcessAddedMark", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[2] { typeof(Effect), typeof(Ability) }, null).Invoke(
            __instance, new object[2] { effect1[0], fromAbility }
          );
          //__instance.ProcessAddedMark(effect1[0], fromAbility);
        }
        Log.LogWrite(" checking stealth\n");
        int StealthPipsPrevious = (int)typeof(AbstractActor).GetField("StealthPipsPrevious", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
        if (__instance.StealthPipsCurrent != StealthPipsPrevious) {
          __instance.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new StealthChangedMessage(__instance.GUID, __instance.StealthPipsCurrent));
        }
        __result = effect1.Count > 0;
        return false;
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\nWTF?!!\n");
        return false;
      }
    }
  }*/
  /*[HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("AddEffectIfNotPresent")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(string), typeof(EffectData), typeof(List<string>), typeof(EffectTriggerType) })]
  public static class AuraCache_AddEffectIfNotPresent {
    public static MethodInfo FOwner;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n", true);
        return false;
      }
      return true;
    }
    public static bool Prefix(AuraCache __instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, string effectCreatorId, EffectData effect, ref List<string> existingEffectIDs, EffectTriggerType triggerSource, ref bool __result) {
      AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      try {
        //string effectId = AuraCache.GetEffectID(fromActor, effectCreatorId, effect, Owner);
        string effectId = string.Format("{0}-{1}-{2}-{3}", (object)fromActor.GUID, (object)effectCreatorId, (object)effect.Description.Id, (object)Owner.GUID);
        Log.LogWrite("AuraCache.AddEffectIfNotPresent\n");
        Log.LogWrite(" owner:"+Owner.DisplayName+"\n");
        Log.LogWrite(" effectId:" + effectId + "\n");
        Log.LogWrite(" fromActor:" + fromActor.DisplayName + "\n");
        Log.LogWrite(" effect:" + effect.Description.Id + "\n");
        bool effect1 = Owner.CreateEffect(effect, (Ability)null, effectId, -1, fromActor, triggerSource == EffectTriggerType.Preview);
        if (effect1) {
          if (existingEffectIDs != null) {
            existingEffectIDs.Add(effectId);
          }
          Owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AuraAddedMessage(fromActor.GUID, Owner.GUID, effectId, effect));
        }
        __result = effect1;
      } catch (Exception e) {
        Log.LogWrite(e.ToString()+"\n");
      }
      return false;
      //Log.LogWrite("AuraCache.ShouldAffectThisActor postfix owner:" + Owner.DisplayName + ":" + Owner.GUID + " from: " + fromActor.DisplayName + ":" + fromActor.GUID + " Effect:" + effect.Description.Id + " result:" + __result + "\n");
    }
  }*/
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("ShouldAffectThisActor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(EffectData), typeof(EffectTriggerType) })]
  public static class AuraCache_ShouldAffectThisActor {
    public static MethodInfo FOwner;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n", true);
        return false;
      }
      return true;
    }
    public static void Postfix(AuraCache __instance, AbstractActor fromActor, EffectData effect, EffectTriggerType triggerSource, ref bool __result) {
      //AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      //Log.LogWrite("AuraCache.ShouldAffectThisActor postfix owner:" + Owner.DisplayName + ":" + Owner.GUID + " from: " + fromActor.DisplayName + ":" + fromActor.GUID + " Effect:" + effect.Description.Id + " result:" + __result + "\n");
    }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("UpdateAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(MechComponent), typeof(float), typeof(EffectTriggerType), typeof(bool) })]
  public static class AuraCache_UpdateAura {
    public static MethodInfo FOwner;
    public static MethodInfo mAddEffectIfNotPresent;
    public static MethodInfo mRemoveEffectIfPresent;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      mAddEffectIfNotPresent = typeof(AuraCache).GetMethod("AddEffectIfNotPresent", BindingFlags.Instance | BindingFlags.NonPublic);
      mRemoveEffectIfPresent = typeof(AuraCache).GetMethod("RemoveEffectIfPresent", BindingFlags.Instance | BindingFlags.NonPublic);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n", true);
        return false;
      }
      return true;
    }
    /*public static void AddEffectIfNotPresent(this AuraCache instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, string effectCreatorId, EffectData effect, ref List<string> existingEffectIDs, EffectTriggerType triggerSource) {
      object[] args = new object[7] { fromActor, movingActor, movingActorPos, effectCreatorId, effect, existingEffectIDs, triggerSource};
      mAddEffectIfNotPresent.Invoke(instance, args);
      existingEffectIDs = (List<string>)args[5];
    }
    public static void RemoveEffectIfPresent(this AuraCache instance, AbstractActor fromActor, string effectCreatorId, EffectData effect, List<Effect> existingEffects, EffectTriggerType triggerSource) {
      object[] args = new object[5] { fromActor, effectCreatorId, effect, existingEffects, triggerSource };
      mRemoveEffectIfPresent.Invoke(instance, args);
    }*/
    public static bool Prefix(AuraCache __instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, MechComponent auraComponent, float distSquared, EffectTriggerType triggerSource, bool skipECMCheck) {
      /*AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      List<Effect> all = Owner.Combat.EffectManager.GetAllEffectsCreatedBy(fromActor.GUID).FindAll((Predicate<Effect>)(x => x.targetID == Owner.GUID));
      for (int index = 0; index < auraComponent.componentDef.statusEffects.Length; ++index) {
        if (__instance.ShouldAffectThisActor(fromActor, auraComponent.componentDef.statusEffects[index], triggerSource)) {
          if (!skipECMCheck && (auraComponent.componentDef.statusEffects[index].targetingData.auraEffectType == AuraEffectType.ECM_GHOST || auraComponent.componentDef.statusEffects[index].targetingData.auraEffectType == AuraEffectType.ECM_GENERAL))
            Owner.Combat.FlagECMStateNeedsRefreshing();
          else if (__instance.AuraConditionsPassed(fromActor, auraComponent, auraComponent.componentDef.statusEffects[index], distSquared, triggerSource))
            __instance.AddEffectIfNotPresent(fromActor, movingActor, movingActorPos, auraComponent.componentDef.Description.Id, auraComponent.componentDef.statusEffects[index], ref auraComponent.createdEffectIDs, triggerSource);
          else
            __instance.RemoveEffectIfPresent(fromActor, auraComponent.componentDef.Description.Id, auraComponent.componentDef.statusEffects[index], all, triggerSource);
        }
      }*/
      return false;
      //Log.LogWrite("AuraCache.UpdateAura prefix owner:" + Owner.DisplayName + ":" + Owner.GUID + " from: " + fromActor.DisplayName + ":" + fromActor.GUID + " component:" + auraComponent.defId + "\n");
    }
    /*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
      Log.LogWrite("AuraCache.UpdateAura transpliter\n");
      List<CodeInstruction> result = instructions.ToList();
      MethodInfo get_statusEffects = AccessTools.Property(typeof(MechComponentDef), "statusEffects").GetGetMethod(false);
      if (get_statusEffects != null) {
        Log.LogWrite(" source method found\n");
      } else {
        return result;
      }
      MethodInfo replacementMethod = AccessTools.Method(typeof(AuraCache_UpdateAura), nameof(get_statusEffects));
      if (replacementMethod != null) {
        Log.LogWrite("target method found\n");
      } else {
        return result;
      }
      do {
        int methodCallIndex = result.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && instruction.operand == get_statusEffects);
        if (methodCallIndex >= 0) {
          Log.LogWrite("methodCallIndex found " + methodCallIndex + "\n");
        } else {
          break;
        }
        result[methodCallIndex].operand = replacementMethod;
        for (int thisIndex = methodCallIndex - 1; thisIndex > 0; --thisIndex) {
          Log.LogWrite(" result[" + thisIndex + "].opcode = " + result[thisIndex].opcode + "\n");
          if (result[thisIndex].opcode == OpCodes.Callvirt) {
            result[thisIndex].opcode = OpCodes.Nop;
            result[thisIndex].operand = null;
            Log.LogWrite(" def opcode changed to component\n");
            break;
          }
        }
      } while (true);
      Log.LogWrite("result:\n");
      for(int t = 0; t < result.Count; ++t) {
        Log.LogWrite(" "+t + "\t" + result[t].opcode + "\t" + (result[t].operand == null ? "null" : result[t].operand) + "\n");
      }
      return result;
    }*/
    public static EffectData[] get_statusEffects(this MechComponent component) {
      //if (component == null) { return new EffectData[0] { }; };
      //Log.LogWrite("AuraCache_UpdateAura.get_statusEffects(" + component.defId + ")\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        //Log.LogWrite(" not activatable:" + component.componentDef.statusEffects.Length + "\n");
        return component.componentDef.statusEffects;
      } else {
        //Log.LogWrite(" activatable:"+activatable.allEffects.Length+"\n");
        return activatable.allEffects;
      }
    }
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("PreviewAura")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(float) })]
  public static class AuraCache_PreviewAura {
    public static MethodInfo FOwner;
    private static MethodInfo mShouldAffectThisActor;
    private static MethodInfo mAuraConditionsPassed;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      mShouldAffectThisActor = typeof(AuraCache).GetMethod("ShouldAffectThisActor", BindingFlags.Instance | BindingFlags.NonPublic);
      mAuraConditionsPassed = typeof(AuraCache).GetMethod("AuraConditionsPassed", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(AbstractActor) , typeof(MechComponent) , typeof(EffectData) , typeof(float) , typeof(EffectTriggerType)  },null);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n", true);
        return false;
      }
      return true;
    }
    public static bool isActive(this MechComponent component) {
      return ActivatableComponent.isComponentActivated(component);
    }
    /*public static bool ShouldAffectThisActor(this AuraCache instance, AbstractActor fromActor, EffectData effect, EffectTriggerType triggerSource) {
      return (bool)mShouldAffectThisActor.Invoke(instance, new object[] { fromActor, effect, triggerSource });
    }
    public static bool AuraConditionsPassed(this AuraCache instance, AbstractActor fromActor, MechComponent auraComponent, EffectData effectData, float distSquared, EffectTriggerType triggerSource) {
      return (bool)mAuraConditionsPassed.Invoke(instance, new object[] {  fromActor, auraComponent,  effectData,  distSquared,  triggerSource });
    }*/
    public static bool Prefix(AuraCache __instance, AbstractActor fromActor, MechComponent auraComponent, float distSquared, ref List<EffectData> __result) {
      /*AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      List<EffectData> effectDataList = new List<EffectData>();
      for (int index = 0; index < auraComponent.componentDef.statusEffects.Length; ++index) {
        if (__instance.ShouldAffectThisActor(fromActor, auraComponent.componentDef.statusEffects[index], EffectTriggerType.Preview) && __instance.AuraConditionsPassed(fromActor, auraComponent, auraComponent.componentDef.statusEffects[index], distSquared, EffectTriggerType.Preview))
          effectDataList.Add(auraComponent.componentDef.statusEffects[index]);
      }
      ActivatableComponent activatable = auraComponent.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) {
        if (auraComponent.isActive()) {
          for (int index = 0; index < activatable.statusEffects.Length; ++index) {
            if (__instance.ShouldAffectThisActor(fromActor, activatable.statusEffects[index], EffectTriggerType.Preview) && __instance.AuraConditionsPassed(fromActor, auraComponent, activatable.statusEffects[index], distSquared, EffectTriggerType.Preview))
              effectDataList.Add(activatable.statusEffects[index]);
          }
        } else {
          for (int index = 0; index < activatable.offlineStatusEffects.Length; ++index) {
            if (__instance.ShouldAffectThisActor(fromActor, activatable.offlineStatusEffects[index], EffectTriggerType.Preview) && __instance.AuraConditionsPassed(fromActor, auraComponent, activatable.offlineStatusEffects[index], distSquared, EffectTriggerType.Preview))
              effectDataList.Add(activatable.offlineStatusEffects[index]);
          }
        }
      }
      __result = effectDataList;*/
      return false;
      //Log.LogWrite("AuraCache.PreviewAura prefix owner:" + Owner.DisplayName + ":" + Owner.GUID + " from: " + fromActor.DisplayName + ":" + fromActor.GUID + " component:" + auraComponent.defId + "\n");
    }
    /*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
      Log.LogWrite("AuraCache.PreviewAura transpliter\n");
      List<CodeInstruction> result = instructions.ToList();
      MethodInfo get_statusEffects = AccessTools.Property(typeof(MechComponentDef), "statusEffects").GetGetMethod(false);
      if (get_statusEffects != null) {
        Log.LogWrite(" source method found\n");
      } else {
        return result;
      }
      MethodInfo replacementMethod = AccessTools.Method(typeof(AuraCache_UpdateAura), nameof(get_statusEffects));
      if (replacementMethod != null) {
        Log.LogWrite("target method found\n");
      } else {
        return result;
      }
      do {
        int methodCallIndex = result.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && instruction.operand == get_statusEffects);
        if (methodCallIndex >= 0) {
          Log.LogWrite("methodCallIndex found " + methodCallIndex + "\n");
        } else {
          break;
        }
        result[methodCallIndex].operand = replacementMethod;
        for (int thisIndex = methodCallIndex - 1; thisIndex > 0; --thisIndex) {
          Log.LogWrite(" result[" + thisIndex + "].opcode = " + result[thisIndex].opcode + "\n");
          if (result[thisIndex].opcode == OpCodes.Callvirt) {
            result[thisIndex].opcode = OpCodes.Nop;
            result[thisIndex].operand = null;
            Log.LogWrite(" def opcode changed to component\n");
            break;
          }
        }
      } while (true);
      Log.LogWrite("result:\n");
      for (int t = 0; t < result.Count; ++t) {
        Log.LogWrite(" " + t + "\t" + result[t].opcode + "\t" + (result[t].operand == null ? "null" : result[t].operand) + "\n");
      }
      return result;
    }*/
  }
  [HarmonyPatch(typeof(AuraCache))]
  [HarmonyPatch("AuraConditionsPassed")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
  public static class AuraCache_AuraConditionsPassed {
    public static MethodInfo FOwner;
    public static bool Prepare() {
      FOwner = typeof(AuraCache).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
      if (FOwner == null) {
        Log.LogWrite("ERROR!:Can't find Owner property\n",true);
        return false;
      }
      return true;
    }
    public static void Postfix(AuraCache __instance, AbstractActor fromActor, MechComponent auraComponent, EffectData effectData, float distSquared, EffectTriggerType triggerSource, ref bool __result) {
      /*AbstractActor Owner = (AbstractActor)FOwner.Invoke(__instance, new object[0] { });
      //Log.LogWrite("AuraCache.AuraConditionsPassed prefix owner:" + Owner.DisplayName + ":"+Owner.GUID+" from: "+fromActor.DisplayName+":"+fromActor.GUID+" component:"+auraComponent.defId+" effect:"+effectData.Description.Id+" res:"+__result+"\n");
      if (__result == false) { return; }
      ActivatableComponent activatable = auraComponent.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (activatable.baseEffects.Contains(effectData)) {
        //Log.LogWrite(" base effect\n");
        return;
      };
      bool componentActive = ActivatableComponent.isComponentActivated(auraComponent);
      if (activatable.onlineEffects.Contains(effectData) && (componentActive == false)) {
        //Log.LogWrite(" online effect but not actiavted\n");
        __result = false;
      } else
      if (activatable.offlineEffects.Contains(effectData) && (componentActive == true)) {
        //Log.LogWrite(" offline effect but actiavted\n");
        __result = false;
      };*/
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("OnPositionUpdate")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Vector3), typeof(Quaternion), typeof(int), typeof(bool), typeof(List<DesignMaskDef>), typeof(bool) })]
  public static class AbstractActor_OnPositionUpdate {
    private class AuraUpdatePositionRecord {
      //private static readonly float TIMEDELTA = 1f;
      //private static readonly float DISTANCEDELTA = 20f;
      public float t { get; set; }
      public Vector3 p { get; set; }
      public AuraUpdatePositionRecord(AbstractActor unit) {
        t = 0f;
        p = unit.CurrentPosition;
      }
      public bool Update(AbstractActor unit) {
        if(Core.Settings.auraUpdateFix == AuraUpdateFix.Time) {
          this.t += Time.deltaTime;
          if(this.t >= Core.Settings.auraUpdateMinTimeDelta) {
            this.t = 0f;
            return true;
          }
        }else
        if(Core.Settings.auraUpdateFix == AuraUpdateFix.Position) {
          float distance = Vector3.Distance(p,unit.CurrentPosition);
          if(distance >= Core.Settings.auraUpdateMinPosDelta) {
            p = unit.CurrentPosition;
            return true;
          }
        }
        return false;
      }
    }
    private static Dictionary<AbstractActor, AuraUpdatePositionRecord> auraUpdateData = new Dictionary<AbstractActor, AuraUpdatePositionRecord>();
    public static void RemoveAuraUpdateData(this AbstractActor unit) {
      if (auraUpdateData.ContainsKey(unit)) { auraUpdateData.Remove(unit); };
    }
    public static bool IsNeedUpdateAura(this AbstractActor unit) {
      if (Core.Settings.auraUpdateFix == AuraUpdateFix.None) { return true; }
      if (Core.Settings.auraUpdateFix == AuraUpdateFix.Never) { return false; }
      if (auraUpdateData.ContainsKey(unit) == false) {
        auraUpdateData.Add(unit, new AuraUpdatePositionRecord(unit));
        return false;
      }
      return auraUpdateData[unit].Update(unit);
    }
    /*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
      Log.LogWrite("AbstractActor.OnPositionUpdate transpliter\n",true);
      MethodInfo UpdateAurasToActor = AccessTools.Method(typeof(AuraCache), "UpdateAurasToActor");
      if (UpdateAurasToActor != null) {
        Log.LogWrite(" source method found\n",true);
      } else {
        return instructions;
      }
      MethodInfo replacementMethod = AccessTools.Method(typeof(AbstractActor_OnPositionUpdate), nameof(UpdateAurasToActor));
      if (replacementMethod != null) {
        Log.LogWrite("target method found\n",true);
      } else {
        return instructions;
      }
      return Transpilers.MethodReplacer(instructions, UpdateAurasToActor, replacementMethod);
    }
    public static void UpdateAurasToActor(List<AbstractActor> actors, AbstractActor movingActor, Vector3 movingActorPosition, EffectTriggerType triggerSource, bool forceUpdate) {
      //switch (Core.Settings.auraUpdateFix) {
        //case AuraUpdateFix.None: AuraCache.UpdateAurasToActor(actors, movingActor, movingActorPosition, triggerSource, forceUpdate); return;
      //}
      if (movingActor.IsNeedUpdateAura()) {
        AuraCache.UpdateAurasToActor(actors, movingActor, movingActorPosition, triggerSource, forceUpdate);
      }
      //Log.LogWrite("AbstractActor.OnPositionUpdate.UpdateAurasToActor\n");
    }*/
  }
  [HarmonyPatch(typeof(ActorMovementSequence))]
  [HarmonyPatch("CompleteMove")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class ActorMovementSequence_CompleteMoveAura {
    public static void Postfix(ActorMovementSequence __instance) {
      /*if (__instance.owningActor == null) { return; }
      switch (Core.Settings.auraUpdateFix) {
        case AuraUpdateFix.Never:
        case AuraUpdateFix.Position:
        case AuraUpdateFix.Time:
          __instance.owningActor.RemoveAuraUpdateData();
          AuraCache.UpdateAurasToActor(__instance.owningActor.Combat.AllActors, __instance.owningActor, __instance.owningActor.CurrentPosition, EffectTriggerType.Passive, false);
          break;
      }*/
    }
  }
  [HarmonyPatch(typeof(MechJumpSequence))]
  [HarmonyPatch("CompleteJump")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechJumpSequence_CompleteJumpAura {
    public static void Postfix(MechJumpSequence __instance) {
      /*if (__instance.OwningMech == null) { return;  }
      switch (Core.Settings.auraUpdateFix) {
        case AuraUpdateFix.Never:
        case AuraUpdateFix.Position:
        case AuraUpdateFix.Time:
          __instance.OwningMech.RemoveAuraUpdateData();
          AuraCache.UpdateAurasToActor(__instance.OwningMech.Combat.AllActors, __instance.OwningMech, __instance.owningActor.CurrentPosition, EffectTriggerType.Passive, false);
          break;
      }*/
    }
  }

  public static class AuraUpdateHelper {
    public static void AurasActivateOnline(this ActivatableComponent activatable, MechComponent component) {

    }
  }
}
