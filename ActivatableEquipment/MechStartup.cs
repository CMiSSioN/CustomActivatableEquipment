using BattleTech;
using CustomActivatableEquipment;
using Harmony;
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
    public static void Postfix(AbstractActor __instance) {
      Log.Debug?.Write("AbstractActor.InitEffectStats " + __instance.DisplayName + ":" + __instance.GUID + "\n");
      if (Core.checkExistance(__instance.StatCollection, StoodUpRollModStatName) == false) {
        __instance.StatCollection.AddStatistic<float>(StoodUpRollModStatName, 0f);
      }
      if (Core.checkExistance(__instance.StatCollection, ArmAbsenceStoodUpModStatName) == false) {
        __instance.StatCollection.AddStatistic<float>(ArmAbsenceStoodUpModStatName, Core.Settings.DefaultArmsAbsenceStoodUpMod);
      }
      Log.Debug?.Write(" StoodUpRollMod " + __instance.StoodUpRollMod()+"\n");
      Log.Debug?.Write(" ArmAbsenceStoodUpMod " + __instance.ArmAbsenceStoodUpMod()+"\n");
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
    public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState, ref bool __result) {
      if (Core.Settings.StartupByHeatControl == false) { return true; }
      try {
        __result = true;
        Mech actorByGuid = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
        if (actorByGuid == null) {
          Log.Debug?.Write("MechStartupInvocation.Invoke failed! Unable to Mech!\n");
          return true;
        }
        if (actorByGuid.CurrentHeatAsRatio >= Core.Settings.StartupMinHeatRatio) {
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(__instance.MechGUID, __instance.MechGUID, "__/CAE.ReactroTooHot/__", FloatieMessage.MessageNature.Buff));
          combatGameState.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage(actorByGuid.DoneNoAnimation()));
        } else {
          return true;
        }
        return false;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("CancelCreatedEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool) })]
  public static class MechComponent_CancelCreatedEffects {
    public static bool Prefix(MechComponent __instance, bool performAuraRefresh) {
      try {
        Log.Debug?.Write("MechComponent.CancelCreatedEffects "+__instance.defId+"\n");
        ActivatableComponent.shutdownComponent(__instance);
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("RestartPassiveEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool) })]
  public static class MechComponent_RestartCreatedEffects {
    public static bool Prefix(MechComponent __instance, bool performAuraRefresh) {
      try {
        Log.Debug?.Write("MechComponent.RestartPassiveEffects " + __instance.defId + "\n");
        ActivatableComponent.startupComponent(__instance);
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
    }
  }
}