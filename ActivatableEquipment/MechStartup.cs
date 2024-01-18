using BattleTech;
using CustomActivatableEquipment;
using CustomComponents;
using HarmonyLib;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("InitEffectStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class AbstractActor_InitStats {
    public static readonly string StoodUpRollModStatName = "CAEStoodUpRollMod";
    public static readonly string ArmAbsenceStoodUpModStatName = "CAEArmAbsenceStoodUpMod";
    public static readonly string UnsafeFailChanceStatName = "CAEUnsafeFailChance";
    public static readonly string UnsafeFailChanceModStatName = "CAEUnsafeFailChanceMod";
    public static readonly string AIUnsafeFailChanceModStatName = "CAEAIUnsafeFailChanceMod";
    public static float StoodUpRollMod(this AbstractActor unit) {
      if (Core.checkExistance(unit.StatCollection, StoodUpRollModStatName) == false) {
        unit.StatCollection.AddStatistic<float>(StoodUpRollModStatName, 0f);
      }
      return unit.StatCollection.GetStatistic(StoodUpRollModStatName).Value<float>();
    }
    public static float ArmAbsenceStoodUpMod(this AbstractActor unit) {
      if (Core.checkExistance(unit.StatCollection, ArmAbsenceStoodUpModStatName) == false) {
        unit.StatCollection.AddStatistic<float>(ArmAbsenceStoodUpModStatName, Core.Settings.DefaultArmsAbsenceStoodUpMod);
      }
      return unit.StatCollection.GetStatistic(ArmAbsenceStoodUpModStatName).Value<float>();
    }
    public static float UnsafeFailChanceMod(this AbstractActor unit) {
      if (Core.checkExistance(unit.StatCollection, UnsafeFailChanceModStatName) == false) {
        unit.StatCollection.AddStatistic<float>(UnsafeFailChanceModStatName, 1f);
      }
      return unit.StatCollection.GetStatistic(UnsafeFailChanceModStatName).Value<float>();
    }
    public static float UnsafeFailChance(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0f; };
      return component.StatCollection.GetOrCreateStatisic<float>(UnsafeFailChanceStatName, activatable.UnsafeFailChance).Value<float>();
    }
    public static float AIUnsafeFailChanceMod(this AbstractActor unit) {
      if (Core.checkExistance(unit.StatCollection, AIUnsafeFailChanceModStatName) == false) {
        unit.StatCollection.AddStatistic<float>(AIUnsafeFailChanceModStatName, Core.Settings.DefaultAIUnsafeFailChanceMod);
      }
      return unit.StatCollection.GetStatistic(AIUnsafeFailChanceModStatName).Value<float>();
    }
    public static void Postfix(AbstractActor __instance) {
      try {
        AbstractActor_InitEffectStatsAuras.Postfix(__instance);
        AbstractActor_InitEffectStatsHeadHit.Postfix(__instance);
        Log.Debug?.WL(0,$"AbstractActor.InitEffectStats {__instance.DisplayName}:{__instance.GUID}");
        if (Core.checkExistance(__instance.StatCollection, StoodUpRollModStatName) == false) {
          __instance.StatCollection.AddStatistic<float>(StoodUpRollModStatName, 0f);
        }
        if (Core.checkExistance(__instance.StatCollection, ArmAbsenceStoodUpModStatName) == false) {
          __instance.StatCollection.AddStatistic<float>(ArmAbsenceStoodUpModStatName, Core.Settings.DefaultArmsAbsenceStoodUpMod);
        }
        if (Core.checkExistance(__instance.StatCollection, UnsafeFailChanceModStatName) == false) {
          __instance.StatCollection.AddStatistic<float>(UnsafeFailChanceModStatName, 1f);
        }
        if (Core.checkExistance(__instance.StatCollection, AIUnsafeFailChanceModStatName) == false) {
          __instance.StatCollection.AddStatistic<float>(AIUnsafeFailChanceModStatName, Core.Settings.DefaultAIUnsafeFailChanceMod);
        }
        Log.Debug?.WL(1,$"StoodUpRollMod {__instance.StoodUpRollMod()}");
        Log.Debug?.WL(1,$"ArmAbsenceStoodUpMod {__instance.ArmAbsenceStoodUpMod()}");
      } catch (Exception e) {
        Log.Error?.TWL(0,e.ToString(),true);
      }
    }
  }
  [HarmonyPatch(typeof(MechStandInvocation))]
  [HarmonyPatch("Invoke")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState) })]
  public static class MechStandInvocation_Invoke {
    public static float StoodUpRoll(this Mech mech) {
      Log.Debug?.Write(mech.DisplayName+ ".StoodUpRoll\n");
      float result = (float)mech.pilot.Piloting*Core.Settings.StoodUpPilotingRollCoeff;
      Log.Debug?.Write(" piloting:"+result+"="+ mech.pilot.Piloting + "x"+ Core.Settings.StoodUpPilotingRollCoeff + "\n");
      Log.Debug?.Write(" chassis:" + result + " + " + mech.StoodUpRollMod()+" = ");
      result += mech.StoodUpRollMod();
      Log.Debug?.Write(result + "\n");
      float absentArms = 0f;
      if (mech.IsLocationDestroyed(ChassisLocations.LeftArm)) { absentArms += 1f; }
      if (mech.IsLocationDestroyed(ChassisLocations.RightArm)) { absentArms += 1f; }
      Log.Debug?.Write(" destroyed arms:" + result + " + " + absentArms + "x" + mech.ArmAbsenceStoodUpMod() + " = ");
      result += (absentArms * mech.ArmAbsenceStoodUpMod());
      Log.Debug?.Write(result + "\n");
      float absentLegs = 0f;
      if (mech.IsLocationDestroyed(ChassisLocations.LeftLeg)) { absentLegs += 1f; }
      if (mech.IsLocationDestroyed(ChassisLocations.RightLeg)) { absentLegs += 1f; }
      Log.Debug?.Write(" destroyed legs:" + result + " + " + absentLegs + "x" + Core.Settings.LegAbsenceStoodUpMod + " = ");
      result += (absentLegs * Core.Settings.LegAbsenceStoodUpMod);
      Log.Debug?.Write(result + "\n");
      return result;
    }
    public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState, ref bool __result) {
      if (Core.Settings.StoodUpPilotingRoll == false) { return true; }
      try {
        __result = true;
        Mech actorByGuid = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
        if (actorByGuid == null) {
          Log.Debug?.Write("MechStartupInvocation.Invoke failed! Unable to Mech!\n");
          return true;
        }
        Log.Debug?.Write("Mech stoodup roll\n");
        float limit = actorByGuid.StoodUpRoll();
        float roll = Random.Range(0f, 1f);
        Log.Debug?.Write(" roll = "+roll+" against "+limit+"\n");
        if (roll < limit) {
          Log.Debug?.Write(" success\n");
          return true;
        } else {
          Log.Debug?.Write(" fail to stand up\n");
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(__instance.MechGUID, __instance.MechGUID, "__/CAE.StandUpFail/__", FloatieMessage.MessageNature.Buff));
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage(actorByGuid.DoneNoAnimation()));
        }
        return false;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(MechStartupInvocation))]
  [HarmonyPatch("Invoke")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState) })]
  public static class MechStartupInvocation_Invoke {
    public static IStackSequence DoneNoAnimation(this Mech mech) {
      mech.OnActivationBegin(mech.GUID, -1);
      return mech.GetDoneWithActorOrders();
    }
    public static void Prefix(ref bool __runOriginal, MechStartupInvocation __instance, CombatGameState combatGameState, ref bool __result) {
      if (Core.Settings.StartupByHeatControl == false) { return; }
      try {
        __result = true;
        Mech actorByGuid = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
        if (actorByGuid == null) {
          Log.Debug?.Write("MechStartupInvocation.Invoke failed! Unable to Mech!\n");
          return;
        }
        if (actorByGuid.CurrentHeatAsRatio >= Core.Settings.StartupMinHeatRatio) {
          //actorByGuid.Unused
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(__instance.MechGUID, __instance.MechGUID, "__/CAE.ReactroTooHot/__", FloatieMessage.MessageNature.Buff));
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage(actorByGuid.DoneNoAnimation()));
        } else {
          return;
        }
        __runOriginal = false; return;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        CombatGameState.gameInfoLogger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("CancelCreatedEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool) })]
  public static class MechComponent_CancelCreatedEffects {
    public static void Prefix(MechComponent __instance, bool performAuraRefresh) {
      try {
        Log.Debug?.Write("MechComponent.CancelCreatedEffects "+__instance.defId+"\n");
        ActivatableComponent.shutdownComponent(__instance);
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        AbstractActor.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("GenerateFallSequence")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int),typeof(string),typeof(Vector2), typeof(SequenceFinished) })]
  public static class MechComponent_GenerateFallSequence {
    public static void Prefix(Mech __instance, int previousStackID, string sourceID, Vector2 attackDirection, SequenceFinished fallSequenceCompletedCallback) {
      try {
        Log.Debug?.TWL(0,"Mech.GenerateFallSequence");
        foreach (MechComponent component in __instance.allComponents) {
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          if (activatable.SwitchOffOnFall == false) { continue; }
          if (ActivatableComponent.isComponentActivated(component)) {
            ActivatableComponent.deactivateComponent(component);
          }
        }
      } catch (Exception e) {
        Log.Debug?.TWL(0,e.ToString());
        AbstractActor.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("RestartPassiveEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool) })]
  public static class MechComponent_RestartCreatedEffects {
    public static void Prefix(MechComponent __instance, bool performAuraRefresh) {
      try {
        Log.Debug?.Write("MechComponent.RestartPassiveEffects " + __instance.defId + "\n");
        ActivatableComponent.startupComponent(__instance);
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        AbstractActor.logger.LogException(e);
      }
      return;
    }
  }
}