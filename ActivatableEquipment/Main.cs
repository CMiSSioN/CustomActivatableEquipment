﻿using CustomComponents;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using InControl;
using Random = UnityEngine.Random;
using Localize;
using CustomActivatableEquipment;
using CustomAmmoCategoriesPatches;
using CustAmmoCategoriesPatches;
using CustomActivatableEquipment.DamageHelpers;

namespace CustomActivatablePatches {
  public static class InjurePilot_Check {
    public static MethodBase PatchMethod() { return AccessTools.Method(typeof(Pilot), "InjurePilot"); }
    public static MethodInfo PrefixMethod() { return AccessTools.Method(typeof(InjurePilot_Check), nameof(Prefix)); }
    public static MethodInfo PostfixMethod() { return AccessTools.Method(typeof(InjurePilot_Check), nameof(Postfix)); }
    public static bool propagationCheck { get; set; } = false;
    public static void Prefix(Pilot __instance, ref Pilot __state) {
      try {
        if (propagationCheck == false) { return; }
        if (__instance == null) { return; }
        __state = __instance;
        Log.Debug?.TWL(0, $"Pilot.InjurePilot prefix {__instance.Callsign} propagate normally");
      }catch(Exception e) {
        Log.Error?.TWL(0,e.ToString(),true);
      }
    }
    public static void Postfix(Pilot __instance, ref Pilot __state) {
      try {
        if (propagationCheck == false) { return; }
        if (__instance == null) { return; }
        if(__state == null) {
          Log.Debug?.TWL(0, $"Pilot.InjurePilot postfix {__instance.Callsign}");
          Log.Debug?.WL(1, $"Someone prevented InjurePilot from executing. CAE is not responsible to this check who else is prefixing it");
        } else {
          Log.Debug?.TWL(0, $"Pilot.InjurePilot postfix {__instance.Callsign} check success. Pilot.InjurePilot is called successfully");

        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(Pilot))]
  [HarmonyPatch("InjuryReasonDescription")]
  [HarmonyPatch(MethodType.Getter)]
  [HarmonyPatch(new Type[] { })]
  public static class Pilot_InjuryReasonDescription {
    public static void Postfix(Pilot __instance, ref string __result) {
      try {
        Log.Debug?.TWL(0, $"Pilot.InjuryReasonDescription {(int)__instance.InjuryReason} {__result}");
        if (Core.Settings.AdditionalInjuryReasonsTable.TryGetValue((int)__instance.InjuryReason, out var reason)) {
          Log.Debug?.WL(1, $"replace to {reason}");
          __result = reason;
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

  [HarmonyPatch(typeof(CombatHUDButtonBase))]
  [HarmonyPatch("OnClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDActionButton_ExecuteClick {

    public static bool Prefix(CombatHUDActionButton __instance) {
      CustomActivatableEquipment.Log.Debug?.Write("CombatHUDActionButton.ExecuteClick '" + __instance.GUID + "'/'" + CombatHUD.ButtonID_Move + "' " + (__instance.GUID == CombatHUD.ButtonID_Move) + "\n");
      CombatHUD HUD = (CombatHUD)typeof(CombatHUDActionButton).GetProperty("HUD", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null);
      /*if (__instance.GUID == CombatHUD.ButtonID_Move) {
        CustomActivatableEquipment.Log.LogWrite(" button is move\n");
        bool modifyers = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        if (modifyers) {
          CustomActivatableEquipment.Log.LogWrite(" ctrl is pressed\n");
          if (HUD.SelectedActor != null) {
            CustomActivatableEquipment.Log.LogWrite(" actor is selected\n");
            if (HUD.SelectedActor is Mech) {
              CustomActivatableEquipment.Log.LogWrite(" mech is selected\n");
              CustomActivatableEquipment.Core.ShowEquipmentDlg(HUD.SelectedActor as Mech, HUD);
            }
          }
          return false;
        }
      } else*/
      if (__instance.GUID == CombatHUD.ButtonID_DoneWithMech) {
        CustomActivatableEquipment.Log.Debug?.Write(" button is brase\n");
        bool modifyers = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        if (modifyers) {
          CustomActivatableEquipment.Log.Debug?.Write(" ctrl is pressed\n");
          if (HUD.SelectedActor != null) {
            CustomActivatableEquipment.Log.Debug?.Write(" actor is selected\n");
            if (HUD.SelectedActor is Mech) {
              CustomActivatableEquipment.Log.Debug?.Write(" mech is selected\n");
              CustomActivatableEquipment.Core.ShowHeatDlg(HUD.SelectedActor as Mech);
            }
          }
          return false;
        }
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(ActorMovementSequence))]
  [HarmonyPatch("CompleteOrders")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class ActorMovementSequence_CompleteOrders {
    public static void Postfix(ActorMovementSequence __instance) {
      CustomActivatableEquipment.Log.Debug?.Write("ActorMovementSequence.CompleteOrders " + __instance.owningActor.GUID + ":" + __instance.owningActor.DisplayName + "\n");
      if (__instance.meleeType == MeleeAttackType.NotSet) {
        foreach (MechComponent component in __instance.owningActor.allComponents) {
          if (component.IsFunctional == false) { continue; };
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          if (activatable.FailCheckOnActivationEnd) { continue; }
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            int aRounds = CustomActivatableEquipment.ActivatableComponent.getComponentActiveRounds(component);
            CustomActivatableEquipment.Log.Debug?.Write("Component:" + component.defId + " is active for " + aRounds + "\n");
            if (CustomActivatableEquipment.ActivatableComponent.rollFail(component, false) == false) {
              CustomActivatableEquipment.Log.Debug?.WL(1,$"Component fail. Deactivate {activatable.ShutdownOnFail}");
              if(activatable.ShutdownOnFail) CustomActivatableEquipment.ActivatableComponent.deactivateComponent(component);
            }
          }
        }
        //__instance.owningActor.Combat.commitDamage();
      }
    }
  }
  [HarmonyPatch(typeof(SelectionState))]
  [HarmonyPatch("CanDeselect")]
  [HarmonyPatch(MethodType.Getter)]
  [HarmonyPatch(new Type[] { })]
  public static class SelectionState_CanDeselect
  {
    public static void Postfix(SelectionState __instance, ref bool __result)
    {
      Log.Debug?.TWL(0,"SelectionState.CanDeselect " + __instance.SelectedActor.DisplayName + ":" + __result);
      Log.Debug?.WL(1, "HasActivatedThisRound:" + __instance.SelectedActor.HasActivatedThisRound);
      Log.Debug?.WL(1, "HasBegunActivation:" + __instance.SelectedActor.HasBegunActivation);
      Log.Debug?.WL(1, "HasMovedThisRound:" + __instance.SelectedActor.HasMovedThisRound);
    }
  }
  [HarmonyPatch(typeof(MechMeleeSequence))]
  [HarmonyPatch("CompleteOrders")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechMeleeSequence_CompleteOrders {
    public static void Postfix(MechMeleeSequence __instance) {
      Log.Debug?.Write("MechMeleeSequence.CompleteOrders " + __instance.OwningMech.GUID + ":" + __instance.OwningMech.DisplayName + "\n");
      foreach (MechComponent component in __instance.OwningMech.allComponents) {
        try {
          if (component.IsFunctional == false) { continue; };
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          if (activatable.FailCheckOnActivationEnd) { continue; }
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            int aRounds = CustomActivatableEquipment.ActivatableComponent.getComponentActiveRounds(component);
            CustomActivatableEquipment.Log.Debug?.Write("Component:" + component.defId + " is active for " + aRounds + "\n");
            if (CustomActivatableEquipment.ActivatableComponent.rollFail(component, false) == false) {
              CustomActivatableEquipment.Log.Debug?.WL(1,$"Component fail. Deactivate {activatable.ShutdownOnFail}");
              if(activatable.ShutdownOnFail) CustomActivatableEquipment.ActivatableComponent.deactivateComponent(component);
            }
          }
        }catch(Exception e) {
          Log.Error?.TWL(0,e.ToString(),true);
        }
      }
      //__instance.owningActor.Combat.commitDamage();
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("OnActivationEnd")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(string), typeof(int) })]
  public static class AbstractActor_OnActivationEnd_FailRoll {
    public static void Postfix(AbstractActor __instance) {
      Log.Debug?.TWL(0,"AbstractActor.OnActivationEnd fail roll " + __instance.PilotableActorDef.ChassisID);
      try {
        foreach (MechComponent component in __instance.allComponents) {
          if (component.IsFunctional == false) { continue; };
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          if (activatable.FailCheckOnActivationEnd == false) { continue; }
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            int aRounds = CustomActivatableEquipment.ActivatableComponent.getComponentActiveRounds(component);
            CustomActivatableEquipment.Log.Debug?.WL(1,"Component:" + component.defId + " is active for " + aRounds);
            if (CustomActivatableEquipment.ActivatableComponent.rollFail(component, false) == false) {
              CustomActivatableEquipment.Log.Debug?.WL(1, $"Component fail. Deactivate {activatable.ShutdownOnFail}");
              if (activatable.ShutdownOnFail) CustomActivatableEquipment.ActivatableComponent.deactivateComponent(component);
            }
          }
        }
      }catch(Exception e) {
        Log.Error?.TWL(0,e.ToString(),true);
      }
    }
  }

  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("GetHeatSinkDissipation")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class Mech_GetHeatSinkDissipation {
    public static void Postfix(Mech __instance, ref float __result) {
      //Log.LogWrite("Mech.GetHeatSinkDissipation:" + __instance.DisplayName + ":"+__instance.GUID+"\n");
      for (int index = 0; index < __instance.miscComponents.Count; ++index) {
        MechComponent miscComponent = __instance.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) { continue; }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) {
          //Log.LogWrite("  Offline heat sink found:" + miscComponent.getCCGUID() + ". Heat dissipation was:"+__result);
          if (__result > componentDef.DissipationCapacity) { __result -= componentDef.DissipationCapacity; } else { __result = 0f; };
          //Log.LogWrite(" become:" + __result+"\n");
        };
      }
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("ApplyHeatSinks")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class Mech_ApplyHeatSinks {

    public static void Postfix(Mech __instance, int stackID) {
      Log.Debug?.Write("Mech.ApplyHeatSinks:" + __instance.DisplayName + ":" + __instance.GUID + "\n");
      foreach (MechComponent component in __instance.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        //if(activatable.CanBeactivatedManualy) {continue;}
        float OverheatLevel = (float)__instance.CurrentHeat / (float)__instance.OverheatLevel;
        if (ActivatableComponent.isComponentActivated(component)) {
          Log.Debug?.Write(" " + component.defId + " active " + __instance.CurrentHeat + "/" + activatable.AutoDeactivateOnHeat + "\n");
          Log.Debug?.Write(" " + component.defId + " active " + OverheatLevel + "/" + activatable.AutoDeactivateOverheatLevel + "\n");
          if (activatable.AutoDeactivateOverheatLevel <= CustomActivatableEquipment.Core.Epsilon) {
            if (activatable.AutoDeactivateOnHeat > Core.Epsilon) {
              if (__instance.CurrentHeat < activatable.AutoDeactivateOnHeat) {
                ActivatableComponent.deactivateComponent(component);
              }
            }
          } else {
            if (OverheatLevel < activatable.AutoDeactivateOverheatLevel) {
              ActivatableComponent.deactivateComponent(component);
            }
          }
        } else {
          int activatedRound = ActivatableComponent.getComponentActivedRound(component);
          int currentRound = __instance.Combat.TurnDirector.CurrentRound;
          Log.Debug?.Write(" " + component.defId + " not active " + __instance.CurrentHeat + "/" + activatable.AutoActivateOnHeat + " oncePerRound:"+ activatable.ActivateOncePerRound+ " actRound:"+ activatedRound + " currentRound:"+currentRound+"\n");
          if (activatable.ActivateOncePerRound && (activatedRound == currentRound)) { return; }
          if (activatable.AutoActivateOnOverheatLevel <= CustomActivatableEquipment.Core.Epsilon) {
            if (activatable.AutoActivateOnHeat > Core.Epsilon) {
              if (__instance.CurrentHeat >= activatable.AutoActivateOnHeat) {
                ActivatableComponent.activateComponent(component, true, false);
              }
            }
          } else {
            if (OverheatLevel >= activatable.AutoActivateOnOverheatLevel) {
              ActivatableComponent.activateComponent(component, true, false);
            }
          }
        }
      }
    }
  }
  [HarmonyPatch(typeof(TurnDirector))]
  [HarmonyPatch("EndCurrentRound")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class TurnDirector_EndCurrentRound {
    public static void CollectDangerComponents(this AbstractActor actor) {
      StringBuilder result = new StringBuilder();
      foreach (MechComponent component in actor.allComponents) {
        CombatHUDEquipmentSlotEx.ClearCache(component);
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        if (activatable.CanBeactivatedManualy == false) { continue; };
        if (ActivatableComponent.isComponentActivated(component) == false) { continue; }
        int actRounds = ActivatableComponent.getComponentActiveRounds(component);
        if (actRounds < activatable.FailRoundsStart) { continue; }
        float failChance = ActivatableComponent.getEffectiveComponentFailChance(component);
        if (failChance < Core.Settings.ToolTipWarningFailChance) { continue; };
        if (result.Length > 0) { result.Append("\n"); };
        if (failChance >= Core.Settings.ToolTipAlertFailChance) { result.Append("<color=#FF0000FF>"); } else { result.Append("<color=#FFA500FF>"); }
        result.Append(component.Description.UIName+ " __/CAE.FAIL/__:");
        result.Append(Mathf.RoundToInt(failChance*100f)+"%</color>");
      }
      MoveStatusPreview_DisplayPreviewStatus.setAdditionalStringMoving(actor, "__/CAE.COMPONENTS/__", result.ToString());
    }
    public static void Postfix(TurnDirector __instance) {
      CustomActivatableEquipment.Log.Debug?.Write("TurnDirector.EndCurrentRound\n");
      foreach (var mech in __instance.Combat.AllActors) {
        CustomActivatableEquipment.Log.Debug?.Write(" Actor:" + mech.DisplayName + ":" + mech.GUID + "\n");
        foreach (MechComponent component in mech.allComponents) {
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          float curFailChance = ActivatableComponent.getComponentFailChance(component);
          CustomActivatableEquipment.Log.Debug?.Write("  " + component.defId + " activatable\n");
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
            curFailChance += activatable.FailChancePerTurn;
            int actRounds = ActivatableComponent.getComponentActiveRounds(component);
            ++actRounds;
            ActivatableComponent.setComponentActiveRounds(component, actRounds);
            CustomActivatableEquipment.Log.Debug?.Write("  active for " + actRounds + "\n");
          } else {
            curFailChance -= activatable.FailChancePerTurn;
            if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
          }
          ActivatableComponent.setComponentFailChance(component, curFailChance);
          CustomActivatableEquipment.Log.Debug?.Write("  new fail chance " + curFailChance + "\n");
        }
        mech.CollectDangerComponents();
      }
    }
  }
  /*[HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("InitStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class Mech_InitStats {
    public static void ActiveDefaultComponents(this AbstractActor unit) {
      foreach(MechComponent component in unit.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        if(activatable.ActiveByDefault == true) {
          ActivatableComponent.activateComponent(component, true);
        }
      }
    }
    public static void Postfix(Mech __instance) {
      __instance.ActiveDefaultComponents();
    }
  }
  [HarmonyPatch(typeof(Vehicle))]
  [HarmonyPatch("InitStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class Vehicle_InitStats {
    public static void Postfix(Mech __instance) {
      __instance.ActiveDefaultComponents();
    }
  }
  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("InitStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class Turret_InitStats {
    public static void Postfix(Mech __instance) {
      __instance.ActiveDefaultComponents();
    }
  }*/
}

namespace CustomActivatableEquipment {
  public class Log {
    //private static string m_assemblyFile;
    private static string m_logfile;
    private static StreamWriter m_fs = null;
    private static readonly Mutex mutex = new Mutex();
    private static Log m_log = new Log();
    public static string BaseDirectory;
    public static void InitLog() {
      //Log.m_assemblyFile = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
      Log.m_logfile = Path.Combine(BaseDirectory, "ActivatableComponents.log");
      //Log.m_logfile = Path.Combine(Log.m_logfile, "CustomAmmoCategories.log");
      File.Delete(Log.m_logfile);
      m_fs = new StreamWriter(m_logfile);
      m_fs.AutoFlush = true;
    }
    public void W(string line, bool isCritical = false) {
      m_log.Write(line, isCritical);
    }
    public void WL(string line, bool isCritical = false) {
      line += "\n"; W(line, isCritical);
    }
    public void W(int initiation, string line, bool isCritical = false) {
      string init = new string(' ', initiation);
      line = init + line; W(line, isCritical);
    }
    public void WL(int initiation, string line, bool isCritical = false) {
      string init = new string(' ', initiation);
      line = init + line; WL(line, isCritical);
    }
    public void TW(int initiation, string line, bool isCritical = false) {
      string init = new string(' ', initiation);
      line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
      W(line, isCritical);
    }
    public void TWL(int initiation, string line, bool isCritical = false) {
      string init = new string(' ', initiation);
      line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
      WL(line, isCritical);
    }
    public void TWriteCritical(int initiation, string line) {
      string init = new string(' ', initiation);
      line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
      WL(line, true);
    }
    public static Log Debug{
        get
        {
            if (Core.Settings.debug) {
                return m_log;
            }
            return null;
        }
    }
    public static Log Error {
      get {
        return m_log;
      }
    }
    public static void WriteCritical(string line) {
        m_log.Write(line, true);
    }
    public void Write(string line, bool isCritical = false) {
      try {
        if ((Core.Settings.debug) || (isCritical)) {
          if (Log.mutex.WaitOne(1000)) {
            m_fs.Write(line);
            Log.mutex.ReleaseMutex();
          }
        }
      } catch (Exception) {
        //i'm sertanly don't know what to do
      }
    }
  }
}

namespace CustomActivatableEquipment {
  using CustAmmoCategories;
  using CustomActivatablePatches;
  using HBS.Util;
  using IRBTModUtils;
  using Newtonsoft.Json.Linq;

  public class AoEExplosion {
    public static Dictionary<ICombatant, Dictionary<string, List<EffectData>>> ExposionStatusEffects = new Dictionary<ICombatant, Dictionary<string, List<EffectData>>>();
    public string ExplosionMessage { get; set; } = string.Empty;
    public float Range { get; set; }
    public float Damage { get; set; }
    public float Heat { get; set; }
    public float Stability { get; set; }
    public float Chance { get; set; }
    public string VFX { get; set; }
    public float VFXScaleX { get; set; }
    public float VFXScaleY { get; set; }
    public float VFXScaleZ { get; set; }
    public float VFXOffsetX { get; set; }
    public float VFXOffsetY { get; set; }
    public float VFXOffsetZ { get; set; }
    public float FireTerrainChance { get; set; }
    public float FireTerrainStrength { get; set; }
    public float FireDurationWithoutForest { get; set; }
    public float FireTerrainCellRadius { get; set; }
    public string TempDesignMask { get; set; }
    public float TempDesignMaskTurns { get; set; }
    public float TempDesignMaskCellRadius { get; set; }
    public string LongVFX { get; set; }
    public float LongVFXScaleX { get; set; }
    public float LongVFXScaleY { get; set; }
    public float LongVFXScaleZ { get; set; }
    public float LongVFXOffsetX { get; set; }
    public float LongVFXOffsetY { get; set; }
    public float LongVFXOffsetZ { get; set; }
    public string ExplodeSound { get; set; }
    public string VFXActorStat { get; set; }
    public string VFXScaleXActorStat { get; set; }
    public string VFXScaleYActorStat { get; set; }
    public string VFXScaleZActorStat { get; set; }
    public string VFXOffsetXActorStat { get; set; }
    public string VFXOffsetYActorStat { get; set; }
    public string VFXOffsetZActorStat { get; set; }
    public string RangeActorStat { get; set; }
    public string DamageActorStat { get; set; }
    public string HeatActorStat { get; set; }
    public string StabilityActorStat { get; set; }
    public string ChanceActorStat { get; set; }
    public string FireTerrainChanceActorStat { get; set; }
    public string FireTerrainStrengthActorStat { get; set; }
    public string FireDurationWithoutForestActorStat { get; set; }
    public string FireTerrainCellRadiusActorStat { get; set; }
    public string TempDesignMaskActorStat { get; set; }
    public string TempDesignMaskTurnsActorStat { get; set; }
    public string TempDesignMaskCellRadiusActorStat { get; set; }
    public string LongVFXActorStat { get; set; }
    public string LongVFXScaleXActorStat { get; set; }
    public string LongVFXScaleYActorStat { get; set; }
    public string LongVFXScaleZActorStat { get; set; }
    public string LongVFXOffsetXActorStat { get; set; }
    public string LongVFXOffsetYActorStat { get; set; }
    public string LongVFXOffsetZActorStat { get; set; }
    public string ExplodeSoundActorStat { get; set; }
    public bool AmmoCountScale { get; set; }
    public string AddSelfDamageTag { get; set; }
    public string AddOtherDamageTag { get; set; }
    public EffectData[] statusEffects { get; set; }
    public string statusEffectsCollectionName { get; set; }
    public string statusEffectsCollection { get; set; }
    public string statusEffectsCollectionActorStat { get; set; }
    public AoEExplosion() {
      Range = -1f;
      Damage = -1f;
      Heat = -1f;
      Stability = -1f;
      Chance = -1f;
      VFX = null;
      FireTerrainChance = -1f;
      FireTerrainStrength = -1;
      FireDurationWithoutForest = -1;
      FireTerrainCellRadius = -1;
      VFXScaleX = -1f;
      VFXScaleY = -1f;
      VFXScaleZ = -1f;
      VFXOffsetX = -1f;
      VFXOffsetY = -1f;
      VFXOffsetZ = -1f;
      TempDesignMask = null;
      TempDesignMaskTurns = -1;
      TempDesignMaskCellRadius = -1;
      LongVFX = null;
      LongVFXScaleX = -1f;
      LongVFXScaleY = -1f;
      LongVFXScaleZ = -1f;
      LongVFXOffsetX = -1f;
      LongVFXOffsetY = -1f;
      LongVFXOffsetZ = -1f;
      AmmoCountScale = false;
      AddSelfDamageTag = string.Empty;
      AddOtherDamageTag = string.Empty;
      ExplodeSound = string.Empty;
      ExplodeSoundActorStat = string.Empty;
      statusEffects = new EffectData[0];
    }
  }
  public enum DamageActivationType { Threshhold, Single, Level }
  [CustomComponent("ActivatableComponent")]
  public partial class ActivatableComponent : SimpleCustomComponent {
    public static string CAEComponentActiveStatName = "CAEComnonentActive";
    public static string CAEComponentActiveRounds = "CAEComnonentActiveRounds";
    public static string CAEComponentActivedRound = "CAEComnonentActivedRound";
    public static string CAEComponentFailChance = "CAEFailChance";
    public static string CAEComponentChargesCount = "CAEChargesCount";
    public string ButtonName { get; set; }
    public float FailFlatChance { get; set; }
    public int FailRoundsStart { get; set; }
    public float FailChancePerTurn { get; set; }
    public float FailISDamage { get; set; }
    public float FailArmorDamage { get; set; }
    public float FailStabDamage { get; set; }
    public int FailPilotingBase { get; set; }
    public float FailPilotingMult { get; set; }
    public bool FailCrit { get; set; }
    public bool SelfCrit { get; set; }
    public float MechTonnageWeightMult { get; set; }
    public float MechTonnageSlotsMult { get; set; }
    public float EngineTonnageWeightMult { get; set; }
    public float EngineTonnageSlotsMult { get; set; }
    public float AutoActivateOnHeat { get; set; }
    public float AutoDeactivateOnHeat { get; set; }
    public float AutoActivateOnIncomingHeat { get; set; }
    public DamageActivationType incomingHeatActivationType { get; set; }
    public float AutoActivateOnArmorDamage { get; set; }
    public float AutoActivateOnStructureDamage { get; set; }
    public bool ActivateOnDamageToInstalledLocation { get; set; }
    public DamageActivationType damageActivationType { get; set; }
    public ArmorLocation[] ActivateOnDamageToMechLocations { get; set; }
    public VehicleChassisLocations[] ActivateOnDamageToVehicleLocations { get; set; }
    public float AutoActivateOnOverheatLevel { get; set; }
    public float AutoDeactivateOverheatLevel { get; set; }
    public string ActivationMessage { get; set; }
    public string DeactivationMessage { get; set; }
    public bool ActivationIsBuff { get; set; }
    public bool NoUniqueCheck { get; set; }
    public int ChargesCount { get; set; }
    public ChassisLocations[] FailDamageLocations { get; set; }
    public VehicleChassisLocations[] FailDamageVehicleLocations { get; set; }
    public bool FailCheckOnActivationEnd { get; set; } = false;
    public bool FailDamageToInstalledLocation { get; set; }
    public bool FailCritToInstalledLocation { get; set; }
    public bool FailCritComponents { get; set; }
    public ChassisLocations[] FailCritLocations { get; set; }
    public VehicleChassisLocations[] FailCritVehicleLocations { get; set; }
    public string[] FailCritExcludeComponentsTags { get; set; }
    public string[] FailCritOnlyComponentsTags { get; set; }
    public EffectData[] statusEffects { get; set; }
    public EffectData[] offlineStatusEffects { get; set; }
    public VFXInfo presistantVFX { get; set; }
    public VFXInfo activateVFX { get; set; }
    public VFXInfo destroyedVFX { get; set; }
    public AoEExplosion Explosion { get; set; }
    public bool CanNotBeActivatedManualy { get; set; }
    public bool ExplodeOnFail { get; set; }
    public bool AlwaysFail { get; set; }
    public bool ExplodeOnDamage { get; set; }
    public float FailChancePerActivation { get; set; }
    public bool ActiveByDefault { get; set; }
    public bool ExplodeOnSuccess { get; set; }
    public bool EjectOnFail { get; set; }
    public bool EjectOnSuccess { get; set; }
    public bool EjectOnActivationTry { get; set; }
    public bool InjuryOnFail { get; set; } = false;
    public bool InjuryOnSuccess { get; set; } = false;
    public bool InjuryOnActivationTry { get; set; } = false;
    public InjuryReason InjuryReason { get; set; } = InjuryReason.ComponentExplosion;
    public int InjuryReasonInt { get; set; } = -1;
    public bool KillPilotOnFail { get; set; } = false;
    public bool KillPilotOnSuccess { get; set; } = false;
    public bool KillPilotOnActivationTry { get; set; } = false;
    public DamageType KillPilotDamageType { get; set; } = DamageType.ComponentExplosion;
    public string CheckPilotStatusFromAttack_reason { get; set; } = "Component fail";
    public bool DonNotCancelEffectOnDestruction { get; set; }
    public string activateSound { set { FActivateSound = new CustAmmoCategories.CustomAudioSource(value); } }
    public string deactivateSound { set { FDeactivateSound = new CustAmmoCategories.CustomAudioSource(value); } }
    public string destroySound { set { FDestroySound = new CustAmmoCategories.CustomAudioSource(value); } }
    public bool presistantVFXOutOfLOSHide { get; set; }
    public bool activateVFXOutOfLOSHide { get; set; }
    public RepairRecord Repair { get; set; }
    [JsonIgnore]
    private CustAmmoCategories.CustomAudioSource FActivateSound;
    [JsonIgnore]
    private CustAmmoCategories.CustomAudioSource FDeactivateSound;
    [JsonIgnore]
    private CustAmmoCategories.CustomAudioSource FDestroySound;
    public bool SafeActivation { get; set; }
    public bool CanActivateAfterMove { get; set; }
    public bool CanActivateAfterFire { get; set; }
    public bool ActivateOncePerRound { get; set; }
    public List<string> PassiveEncounterTags { get; set; }
    public List<string> OnlineEncounterTags { get; set; }
    public List<string> OfflineEncounterTags { get; set; }
    public bool SwitchOffOnFall { get; set; }
    public bool HideInUI { get; set; }
    public bool ShutdownOnFail { get; set; } = true;
    public ActivatableComponent() {
      ButtonName = "NotSet";
      FailFlatChance = 0f;
      FailStabDamage = 0f;
      FailChancePerTurn = 0f;
      FailISDamage = 0f;
      FailRoundsStart = 0;
      SelfCrit = false;
      MechTonnageWeightMult = 0f;
      MechTonnageSlotsMult = 0f;
      EngineTonnageWeightMult = 0f;
      EngineTonnageSlotsMult = 0f;
      FailPilotingBase = 5;
      FailPilotingMult = 0f;
      AutoActivateOnHeat = 0f;
      AutoDeactivateOnHeat = 0f;
      AutoActivateOnIncomingHeat = 0f;
      AutoActivateOnArmorDamage = 0f;
      AutoActivateOnStructureDamage = 0f;
      ActivateOnDamageToInstalledLocation = false;
      incomingHeatActivationType = DamageActivationType.Threshhold;
      ActivateOnDamageToMechLocations = new ArmorLocation[0];
      ActivateOnDamageToVehicleLocations = new VehicleChassisLocations[0];
      FailDamageLocations = new ChassisLocations[0];
      statusEffects = new EffectData[0];
      offlineStatusEffects = new EffectData[0];
      ActivationMessage = "ON";
      DeactivationMessage = "OFF";
      ActivationIsBuff = true;
      NoUniqueCheck = false;
      ChargesCount = 0;
      presistantVFX = new VFXInfo();
      activateVFX = new VFXInfo();
      destroyedVFX = new VFXInfo();
      Explosion = new AoEExplosion();
      CanNotBeActivatedManualy = true;
      ExplodeOnFail = false;
      ExplodeOnDamage = false;
      AlwaysFail = false;
      FailChancePerActivation = 0f;
      ActiveByDefault = false;
      ExplodeOnSuccess = false;
      EjectOnFail = false;
      EjectOnSuccess = false;
      EjectOnActivationTry = false;
      DonNotCancelEffectOnDestruction = false;
      FActivateSound = null;
      FDeactivateSound = null;
      FDestroySound = null;
      Repair = new RepairRecord();
      FallEffects = null;
      Linkage = new LinkageRecord();
      presistantVFXOutOfLOSHide = false;
      activateVFXOutOfLOSHide = false;
      incomingHeatActivationType = DamageActivationType.Threshhold;
      SafeActivation = false;
      CanActivateAfterMove = false;
      CanActivateAfterFire = true;
      ActivateOncePerRound = false;
      PassiveEncounterTags = new List<string>();
      OnlineEncounterTags = new List<string>();
      OfflineEncounterTags = new List<string>();
      SwitchOffOnFall = false;
      HideInUI = false;
      FailCritExcludeComponentsTags = new string[0];
      FailCritOnlyComponentsTags = new string[0];
      FailDamageVehicleLocations = new VehicleChassisLocations[0];
      FailDamageLocations = new ChassisLocations[0];
      FailCritComponents = false;
      FailCritLocations = new ChassisLocations[0];
      FailCritVehicleLocations = new VehicleChassisLocations[0];
      FailDamageToInstalledLocation = false;
    }
    public void playActivateSound(AkGameObj soundObject) {
      if (FActivateSound != null) {
        Log.Debug?.Write("playing activate sound\n");
        FActivateSound.play(soundObject);
      }
    }
    public void playDeactivateSound(AkGameObj soundObject) {
      if (FDeactivateSound != null) {
        Log.Debug?.Write("playing deactivate sound\n");
        FDeactivateSound.play(soundObject);
      }
    }
    public void playDestroySound(AkGameObj soundObject) {
      if (FDestroySound != null) {
        Log.Debug?.Write("playing destroy sound\n");
        FDestroySound.play(soundObject);
      }
    }
    private static Dictionary<MechComponent, int> chargesCache = new Dictionary<MechComponent, int>();
    public static void Clear() { chargesCache.Clear(); }
    private static void ClearChargesCache(MechComponent component) { chargesCache.Remove(component); }
    private static void CacheCharges(MechComponent component, int value) { if (chargesCache.ContainsKey(component)) { chargesCache[component] = value; } else { chargesCache.Add(component, value); }; }
    public static bool isOutOfCharges(MechComponent component) {
      if (component == null) { return false; };
      if (chargesCache.TryGetValue(component, out int charges)) { return charges == 0; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { CacheCharges(component, -1); return false; }
      if (activatable.ChargesCount == 0) { CacheCharges(component, -1); return false; };
      if (activatable.ChargesCount == -1) { CacheCharges(component, -1); return false; };
      if (Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentChargesCount, activatable.ChargesCount);
        CacheCharges(component, activatable.ChargesCount);
        return false;
      }
      charges = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentChargesCount).Value<int>();
      if (charges > 0) { CacheCharges(component, charges); return false; };
      CacheCharges(component, 0);
      return true;
    }
    public static int getChargesCount(MechComponent component) {
      if (component == null) { return -1; };
      if (chargesCache.TryGetValue(component, out int charges)) { return charges; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { CacheCharges(component, -1); return -1; }
      if (activatable.ChargesCount == -1) { CacheCharges(component, -1); return -1; };
      if (activatable.ChargesCount <= 0) { CacheCharges(component, 0); return 0; };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentChargesCount, activatable.ChargesCount);
        CacheCharges(component, activatable.ChargesCount);
        return activatable.ChargesCount;
      }
      charges = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentChargesCount).Value<int>();
      CacheCharges(component, charges);
      return charges;
    }
    public static void setChargesCount(MechComponent component, int charges) {
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentChargesCount, charges);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentChargesCount, charges);
      }
      CacheCharges(component, charges);
    }
    public bool CanBeactivatedManualy {
      get {
        //if (this.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) { return false; }
        //if (this.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) { return false; }
        return !this.CanNotBeActivatedManualy;
      }
    }
    public static void critComponent(Mech mech, MechComponent componentInSlot, ChassisLocations location, ref WeaponHitInfo hitInfo) {
      Weapon weapon1 = componentInSlot as Weapon;
      AmmunitionBox ammoBox = componentInSlot as AmmunitionBox;
      Jumpjet jumpjet = componentInSlot as Jumpjet;
      HeatSinkDef componentDef = componentInSlot.componentDef as HeatSinkDef;
      bool flag = weapon1 != null;
      if ((UnityEngine.Object)mech.GameRep != (UnityEngine.Object)null) {
        WwiseManager.SetSwitch<AudioSwitch_weapon_type>(AudioSwitch_weapon_type.laser_medium, mech.GameRep.audioObject);
        WwiseManager.SetSwitch<AudioSwitch_surface_type>(AudioSwitch_surface_type.mech_critical_hit, mech.GameRep.audioObject);
        int num1 = (int)WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_weapon, mech.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        int num2 = (int)WwiseManager.PostEvent<AudioEventList_explosion>(AudioEventList_explosion.explosion_small, mech.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        if (mech.team.LocalPlayerControlsTeam)
          AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_friendly ", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
        else if (!mech.team.IsFriendly(mech.Combat.LocalPlayerTeam))
          AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_enemy", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
        if (jumpjet == null && componentDef == null && (ammoBox == null && componentInSlot.DamageLevel > ComponentDamageLevel.Functional))
          mech.GameRep.PlayComponentCritVFX((int)location);
        if (ammoBox != null && componentInSlot.DamageLevel > ComponentDamageLevel.Functional)
          mech.GameRep.PlayVFX((int)location, (string)mech.Combat.Constants.VFXNames.componentDestruction_AmmoExplosion, true, Vector3.zero, true, -1f);
      }
      ComponentDamageLevel damageLevel = componentInSlot.DamageLevel;
      switch (damageLevel) {
        case ComponentDamageLevel.Functional:
          if (flag) {
            damageLevel = ComponentDamageLevel.Penalized;
            mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} CRIT", new object[1]
            {
                    (object) componentInSlot.UIName
            }), FloatieMessage.MessageNature.CriticalHit, true)));
            goto case ComponentDamageLevel.Destroyed;
          } else {
            damageLevel = ComponentDamageLevel.Destroyed;
            mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} DESTROYED", new object[1]
            {
                    (object) componentInSlot.UIName
            }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
            goto case ComponentDamageLevel.Destroyed;
          }
        case ComponentDamageLevel.Destroyed:
          componentInSlot.DamageComponent(hitInfo, damageLevel, true);
          break;
        default:
          damageLevel = ComponentDamageLevel.Destroyed;
          mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} DESTROYED", new object[1]
          {
                  (object) componentInSlot.UIName
          }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
          goto case ComponentDamageLevel.Destroyed;
      }
    }

    public static void CritLocation(Mech mech, ChassisLocations location, ref WeaponHitInfo hitInfo) {
      Log.Debug?.Write("CritLocation " + mech.DisplayName + ":" + location + "\n");
      int maxSlots = mech.MechDef.GetChassisLocationDef(location).InventorySlots;
      Log.Debug?.Write(" slots in location:" + maxSlots + "\n");
      List<int> slotsWithComponents = new List<int>();
      for (int slotIndex = 0; slotIndex < maxSlots; ++slotIndex) {
        MechComponent testInSlot = mech.GetComponentInSlot(location, slotIndex);
        if (testInSlot != null) {
          Log.Debug?.Write(" slots:" + slotIndex + ":" + testInSlot.defId + "\n");
          slotsWithComponents.Add(slotIndex);
        } else {
          Log.Debug?.Write(" slots:" + slotIndex + ":empty\n");
        }
      }
      int slotRoll = (int)(((float)slotsWithComponents.Count) * Random.Range(0f, 1f));
      Log.Debug?.Write(" slotRoll:" + slotRoll + "\n");
      MechComponent componentInSlot = mech.GetComponentInSlot(location, slotRoll);
      if (componentInSlot != null) {
        Log.Debug?.Write(" critComponent:" + componentInSlot.defId + "\n");
        ActivatableComponent.critComponent(mech, componentInSlot, location, ref hitInfo);
      } else {
        Log.Debug?.Write(" crit to empty slot. possibly only if no components in location\n");
      }
    }
    public static bool rollFail(MechComponent component, bool isInital = false, bool testRoll = false) {
      Log.Debug?.TWL(0,"rollFail " + component.defId);
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return false; }
      if ((ActivatableComponent.isComponentActivated(component) == false) && (isInital == false)) {
        Log.Debug?.Write(" not activated\n");
        return false;
      };
      int actRounds = ActivatableComponent.getComponentActiveRounds(component);
      if ((isInital == false) && (actRounds < activatable.FailRoundsStart)) {
        Log.Debug?.Write(" check not needed\n");
        return true;
      }
      if (!(component.parent is AbstractActor)) {
        Log.Debug?.Write(" owner is not AbstractActor\n");
        return true;
      }
      AbstractActor owner = component.parent as AbstractActor;
      float chance = ActivatableComponent.getEffectiveComponentFailChance(component);
      Log.Debug?.Write(" chance:" + chance + "\n");
      float roll = Random.Range(0f, 1f);
      if (activatable.AlwaysFail) {
        roll = -1f;
        Log.Debug?.Write(" always fail\n");
      }
      if (testRoll) { if (roll < chance) { return false; } else { return true; }; };
      if (component.isAIRollPassed()) {
        component.setAIRollPassed(false);
        if(roll >= 0f) { roll = chance + 0.1f; };
      }
      Log.Debug?.Write(" roll:" + roll + "\n");
      if (roll < chance) {
        if (activatable.EjectOnFail) { component.parent.EjectPilot(component.parent.GUID, -1, DeathMethod.PilotEjection, false); };
        ICustomMech custMech = component.parent as ICustomMech;
        if ((custMech != null)) { if (custMech.isSquad) { goto skip_pilot_processing; } }
        Log.Debug?.WL(1, $"InjuryOnFail:{activatable.InjuryOnFail}");
        if (activatable.InjuryOnFail) {
          Log.Debug?.WL(2, $"SetNeedsInjury {(activatable.InjuryReasonInt >= 0 ? activatable.InjuryReasonInt : (int)activatable.InjuryReason)}");
          component.parent.GetPilot()?.SetNeedsInjury((InjuryReason)(activatable.InjuryReasonInt >= 0? activatable.InjuryReasonInt : (int)activatable.InjuryReason));
        }
        Log.Debug?.WL(1, $"KillPilotOnFail:{activatable.KillPilotOnFail}");
        if (activatable.KillPilotOnFail) {
          Log.Debug?.WL(2, $"KillPilot {activatable.KillPilotDamageType}");
          component.parent.GetPilot()?.KillPilot(component.parent.Combat.Constants, component.parent.GUID, 0, activatable.KillPilotDamageType, (Weapon)null, component.parent);
          component.parent.FlagForDeath(component.UIName+" fail", DeathMethod.PilotKilled, DamageType.ComponentExplosion, -1, -1, component.parent.GUID, false);
        }
      skip_pilot_processing:
        //var fakeHit = new WeaponHitInfo(-1, -1, -1, -1, component.parent.GUID, component.parent.GUID, -1, null, null, null, null, null, null, null, null, null, null, null);
        //if (activatable.FailISDamage >= 1f) {
        owner.StructureDamage(activatable, component);
        owner.ArmorDamage(activatable, component);
          //foreach (ChassisLocations location in activatable.FailDamageLocations) {
          //  Log.Debug?.Write(" apply inner structure damage:" + location + "\n");
          //  owner.ApplyStructureStatDamage(location, activatable.FailISDamage, fakeHit);
          //  if (owner.IsLocationDestroyed(location)) {
          //    owner.NukeStructureLocation(fakeHit, (int)location, location, Vector3.zero, DamageType.OverheatSelf);
          //  }
          //}
          //component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, "STRUCTURE DAMAGE", FloatieMessage.MessageNature.CriticalHit));
        //}
        if (activatable.FailCritComponents) {
          owner.CritComponentInLocations(activatable, component);
        }
        if (activatable.SelfCrit) {
          Log.Debug?.Write(" apply crit to self\n");
          component.CritComponent();
          //ActivatableComponent.critComponent(owner, component, MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)component.Location), ref fakeHit);
        }
        if (activatable.FailStabDamage > Core.Epsilon) {
          if (owner is Mech mech) {
            if (mech.isHasStability()) {
              mech.AddAbsoluteInstability(activatable.FailStabDamage, StabilityChangeSource.Effect, owner.GUID);
            }
          }
        }
        Log.Debug?.Write(" owner status. Death:" + owner.IsFlaggedForDeath + " Knockdown:" + owner.IsFlaggedForKnockdown + "\n");
        bool needToDone = false;
        Log.Debug?.WL(1, $"checking pilot status from attack need injury:{component.parent.GetPilot()?.NeedsInjury}");
        InjurePilot_Check.propagationCheck = true;
        owner.CheckPilotStatusFromAttack(activatable.CheckPilotStatusFromAttack_reason,-1,-1);
        InjurePilot_Check.propagationCheck = false;
        Log.Debug?.WL(1, $"checked pilot status from attack need injury:{component.parent.GetPilot()?.NeedsInjury}");
        if (owner.IsFlaggedForDeath || owner.IsFlaggedForKnockdown) {
          Log.Debug?.Write(" need done with actor\n");
          needToDone = true;
          owner.HasFiredThisRound = true;
          owner.HasMovedThisRound = true;
        }
        owner.HandleDeath(owner.GUID);
        if (owner.IsDead == false) {
          owner.HandleKnockdown(-1, owner.GUID, Vector2.one, (SequenceFinished)null);
        }
        if (needToDone && owner.IsAvailableThisPhase) {
          Log.Debug?.Write(" done with actor\n");
          owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage(owner.DoneWithActor()));
        }
        if (activatable.ExplodeOnFail) {
          component.AoEExplodeComponent();
        }
        return false;
      }
      return true;
    }


    public static bool canBeDamageActivated(MechComponent component)
    {
        if(isComponentActivated(component))
        {
                Log.Debug?.Write($"{component.Name} already active or not activateable - cannot be activated\n");
                return false;
        }
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable.AutoActivateOnIncomingHeat > 0f||
            activatable.AutoActivateOnArmorDamage > 0f ||
            activatable.AutoActivateOnStructureDamage > 0f
            //activatable.AutoActivateOnAnyDamage > 0f
            )
        {
                Log.Debug?.Write($"{component.Name} - can be damage activated\n");
                return true;
        }
         return false;
    }


    internal static bool ActivateOnIncomingHeat(MechComponent component, int heatDamage)
    {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) { return true; }
            if (activatable.AutoActivateOnIncomingHeat!=0 && activatable.AutoActivateOnIncomingHeat<=heatDamage)
            {
                Log.Debug?.Write($"{component.Name} ActivateOnIncomingHeat {activatable.AutoActivateOnIncomingHeat:F3} <= {heatDamage} \n");
                activateComponent(component, true, false);
                return true;
            }
            return false;
    }

    internal static bool ActivateOnDamage(MechComponent component, float armorDamage,float structureDamage, ChassisLocations loc)
    {
            /*ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) { return false; }
            if (activatable.AutoActivateOnAnyDamage != 0 && activatable.AutoActivateOnAnyDamage <= (armorDamage+structureDamage) && shouldAutoActivateForDamageToLocation(component,loc) )
            {
                Log.LogWrite($"{component.Name} AutoActivateOnAnyDamage {activatable.AutoActivateOnAnyDamage:F3} <= {armorDamage + structureDamage} \n");
                activateComponent(component, true, false);
                return true;
            }
            if (activatable.AutoActivateOnArmorDamage != 0 && activatable.AutoActivateOnArmorDamage <= armorDamage && shouldAutoActivateForDamageToLocation(component, loc))
            {
                Log.LogWrite($"{component.Name} AutoActivateOnArmorDamage {activatable.AutoActivateOnArmorDamage:F3} <= {armorDamage} \n");
                activateComponent(component, true, false);
                return true;
            }
            if (activatable.AutoActivateOnStructureDamage != 0 && activatable.AutoActivateOnStructureDamage <= structureDamage && shouldAutoActivateForDamageToLocation(component, loc))
            {
                Log.LogWrite($"{component.Name} AutoActivateOnStructureDamage {activatable.AutoActivateOnStructureDamage:F3} <= {structureDamage} \n");
                activateComponent(component, true, false);
                return true;
            }*/
            return false;
        }

        private static bool shouldAutoActivateForDamageToLocation(MechComponent component, ChassisLocations loc)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (component.IsFunctional == false) { return false; }
            if (activatable == null) { return false; }
            if (loc==ChassisLocations.MainBody)
            {
                return false;//unsupported
            }
            if(component.parent is Mech mech)
            {
                //These checks mean that if a location is 1 hit destroyed damage triggered activation wont happen
                //This allows overall damage checks i.e. (damage right leg+damage left leg>VAL) , as well multi location checks ( damage right leg>VAL OR damage left leg >VAL) checks.
                if (loc== ChassisLocations.Torso && (mech.IsLocationDestroyed(ChassisLocations.CenterTorso) || mech.IsLocationDestroyed(ChassisLocations.LeftTorso) || mech.IsLocationDestroyed(ChassisLocations.RightTorso)))
                {//not sure what isLocationDestroyed performs when checking complex locations - AND or OR , so implementing
                    Log.Debug?.Write($"shouldAutoActivate Skip cause Location (?Partialy?) Destroyed {loc.ToString()}\n");
                    return false;
                }
                if (loc == ChassisLocations.Arms && (mech.IsLocationDestroyed(ChassisLocations.LeftArm) || mech.IsLocationDestroyed(ChassisLocations.RightArm)) )
                {//not sure what isLocationDestroyed performs when checking complex locations - AND or OR , so implementing
                    Log.Debug?.Write($"shouldAutoActivate Skip cause Location (?Partialy?) Destroyed {loc.ToString()}\n");
                    return false;
                }
                if (loc == ChassisLocations.Legs && (mech.IsLocationDestroyed(ChassisLocations.LeftLeg) || mech.IsLocationDestroyed(ChassisLocations.RightLeg)) )
                {//not sure what isLocationDestroyed performs when checking complex locations - AND or OR , so implementing
                    Log.Debug?.Write($"shouldAutoActivate Skip cause Location (?Partialy?) Destroyed {loc.ToString()}\n");
                    return false;
                }
                if (loc == ChassisLocations.All && (/*mech.IsLocationDestroyed(ChassisLocations.CenterTorso) ||*/ mech.IsLocationDestroyed(ChassisLocations.LeftTorso) || mech.IsLocationDestroyed(ChassisLocations.RightTorso) || mech.IsLocationDestroyed(ChassisLocations.LeftArm) || mech.IsLocationDestroyed(ChassisLocations.RightArm) || mech.IsLocationDestroyed(ChassisLocations.LeftLeg) || mech.IsLocationDestroyed(ChassisLocations.RightLeg)))
                {//not sure what isLocationDestroyed performs when checking complex locations - AND or OR , so implementing
                    Log.Debug?.Write($"shouldAutoActivate Skip cause Location (?Partialy?) Destroyed {loc.ToString()}\n");
                    return false;
                }
                if ( !(loc == ChassisLocations.Torso || loc == ChassisLocations.Arms || loc == ChassisLocations.Legs || loc == ChassisLocations.All || loc == ChassisLocations.MainBody) && mech.IsLocationDestroyed(loc))
                {
                    Log.Debug?.Write($"shouldAutoActivate Skip cause Location Destroyed {loc.ToString()}\n");
                    return false;
                }
            }
            else
            {
                Log.Debug?.Write($"Not a mech, somethings broken\n");
            }
            /*if((activatable.ActivateOnDamageToLocations.Length==0 || activatable.ActivateOnDamageToLocations.Contains(ChassisLocations.None)) && (int)loc==component.Location){
                Log.LogWrite($"shouldAutoActivate {component.Name} install location matches damage location {loc.ToString()}\n");
                return true;
            }
            if (activatable.ActivateOnDamageToLocations.Contains(loc))
            {
                Log.LogWrite($"shouldAutoActivate {component.Name} auto activate location matches damage location {loc.ToString()}\n");
                return true;
            }*/
            return false;
        }

        public static bool isComponentActivated(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if(component.IsFunctional == false) { return false; }
      if (activatable == null) { return true; }
      if (Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        return false;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentActiveStatName).Value<bool>();
    }
    public static bool isComponentActivatable(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return false; }
      return true;
    }
    public static int getComponentActiveRounds(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        return 0;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentActiveRounds).Value<int>();
    }
    public static int getComponentActivedRound(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return -1; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActivedRound) == false) {
        return -1;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentActivedRound).Value<int>();
    }
    public static void setComponentActiveRounds(MechComponent component, int aRounds) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, aRounds);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, aRounds);
      }
    }
    public static float getComponentFailChance(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == false) {
        return 0;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentFailChance).Value<float>();
    }
    public static float getEffectiveComponentFailChance(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0f; }
      float FailChance = 0f;
      //if ((activatable.CanBeactivatedManualy == false) && (ActivatableComponent.isComponentActivated(component) == false)) {
      //  return 0f;
      //}
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == true) {
        FailChance = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentFailChance).Value<float>();
      }
      if (FailChance < activatable.FailFlatChance) { FailChance = activatable.FailFlatChance; };
      FailChance += (activatable.FailPilotingBase - component.parent.SkillPiloting) * activatable.FailPilotingMult;
      if (FailChance < 0f) { FailChance = 0f; };
      return FailChance;
    }
    public static void setComponentFailChance(MechComponent component, float chance) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == false) {
        component.StatCollection.AddStatistic<float>(ActivatableComponent.CAEComponentFailChance, chance);
      } else {
        component.StatCollection.Set<float>(ActivatableComponent.CAEComponentFailChance, chance);
      }
    }
    public static void activateComponent(MechComponent component, bool autoActivate, bool isInital) {
      CombatHUDEquipmentSlotEx.ClearCache(component);
      Log.Debug?.Write("activateComponent " + component.defId + "\n");
      if (component.IsFunctional == false) {
        Log.Debug?.Write(" not functional\n");
        return;
      };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        Log.Debug?.Write(" not activatable\n");
        return;
      }
      if (ActivatableComponent.isComponentActivated(component) == true) {
        Log.Debug?.Write(" already activated\n");
        return;
      };
      if (ActivatableComponent.isOutOfCharges(component)) {
        Log.Debug?.Write(" out of charges\n");
        return;
      }
      if (activatable.EjectOnActivationTry) {
        Log.Debug?.Write(" eject on activation try\n");
        component.parent.EjectPilot(component.parent.GUID, -1, DeathMethod.PilotEjection, false);
      };
      ICustomMech custMech = component.parent as ICustomMech;
      if ((custMech != null)) { if (custMech.isSquad) { goto skip_pilot_processing_try; } }
      if (activatable.InjuryOnActivationTry) {
        component.parent.GetPilot()?.SetNeedsInjury((InjuryReason)(activatable.InjuryReasonInt >= 0 ? activatable.InjuryReasonInt : (int)activatable.InjuryReason));
        component.parent.CheckPilotStatusFromAttack(activatable.CheckPilotStatusFromAttack_reason, -1, -1);
        component.parent.HandleDeath(component.parent.GUID);
      }
      if (activatable.KillPilotOnActivationTry) {
        component.parent.GetPilot()?.KillPilot(component.parent.Combat.Constants, component.parent.GUID, 0, activatable.KillPilotDamageType, (Weapon)null, component.parent);
        component.parent.FlagForDeath(component.UIName + " success", DeathMethod.PilotKilled, DamageType.ComponentExplosion, -1, -1, component.parent.GUID, false);
        component.parent.HandleDeath(component.parent.GUID);
      }
      skip_pilot_processing_try:
      if (autoActivate == false) {
        if (ActivatableComponent.rollFail(component, true) == false) {
          Log.Debug?.Write(" fail to activate\n");
          component.playDeactivateSound();
          return;
        }
      } else {
        Log.Debug?.Write(" auto activation. no fail roll needed\n");
      }
      if (activatable.ChargesCount != 0) {
        if (activatable.ChargesCount > 0) {
          int charges = ActivatableComponent.getChargesCount(component);
          if (charges > 0) {
            --charges;
            ActivatableComponent.setChargesCount(component, charges);
            Log.Debug?.Write(" remains charges:" + charges + "\n");
          } else {
            Log.Debug?.Write(" out of charges\n");
            return;
          }
        } else {
          Log.Debug?.Write(" infinate charges\n");
        }
      } else {
        if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
          component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, true);
        } else {
          component.StatCollection.Set<bool>(ActivatableComponent.CAEComponentActiveStatName, true);
        }
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if (activatable.FailChancePerActivation > Core.Epsilon) {
        float curFailChance = ActivatableComponent.getComponentFailChance(component);
        curFailChance += activatable.FailChancePerActivation;
        if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
        ActivatableComponent.setComponentFailChance(component, curFailChance);
      }
      if (activatable.EjectOnSuccess) {
        Log.Debug?.Write(" eject on activation success\n");
        component.parent.EjectPilot(component.parent.GUID, -1, DeathMethod.PilotEjection, false);
      };
      if ((custMech != null)) { if (custMech.isSquad) { goto skip_pilot_processing_success; } }
      if (activatable.InjuryOnSuccess) {
        component.parent.GetPilot()?.SetNeedsInjury((InjuryReason)(activatable.InjuryReasonInt >= 0 ? activatable.InjuryReasonInt : (int)activatable.InjuryReason));
        component.parent.CheckPilotStatusFromAttack(activatable.CheckPilotStatusFromAttack_reason, -1, -1);
        component.parent.HandleDeath(component.parent.GUID);
      }
      if (activatable.KillPilotOnSuccess) {
        component.parent.GetPilot()?.KillPilot(component.parent.Combat.Constants, component.parent.GUID, 0, activatable.KillPilotDamageType, (Weapon)null, component.parent);
        component.parent.FlagForDeath(component.UIName + " success", DeathMethod.PilotKilled, DamageType.ComponentExplosion, -1, -1, component.parent.GUID, false);
        component.parent.HandleDeath(component.parent.GUID);
      }
    skip_pilot_processing_success:

      if (activatable.Repair.repairTrigger.OnActivation) { activatable.Repair.Repair(component); }
      activatable.removeOfflineEffects(component);
      activatable.applyOnlineEffects(component, isInital);
      component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID,
        component.Description.UIName + " " + activatable.ActivationMessage,
        (activatable.ActivationIsBuff) ? FloatieMessage.MessageNature.Buff : FloatieMessage.MessageNature.Debuff
      ));
      ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
      if (activeVFX != null) { activeVFX.SpawnSelf(component.parent.Combat); }
      try {
        if (activatable.activateVFXOutOfLOSHide == true) {
          if (component.parent.GameRep.VisibleToPlayer == false) { activeVFX.Hide(); }
        }
      } catch (Exception) {
      }
      component.playActivateSound();
      component.UpdateAuras(false);
      component.parent?.bodyAura()?.RetriggerEnter(false, false, true);
      component.parent?.bodyAura()?.RetriggerExit();
      //component.parent.bodyAura()?.ReapplyAllEffects();
      CAEAuraHelper.ClearAuraPreviewCache();
      if (activatable.ExplodeOnSuccess) { component.AoEExplodeComponent(); }
      component.LinkageActivate(isInital);
    }

    public void applyOnlineEffects(MechComponent component, bool isInital) {
      if (this.statusEffects == null) {
        Log.Debug?.Write(" no activatable effects\n");
      } else {
        Log.Debug?.Write(" activatable effects count: " + this.statusEffects.Length + "\n");
        Log.Debug?.Write(" sprint:" + component.parent.MaxSprintDistance + "\n");
        Log.Debug?.Write(" walk:" + component.parent.MaxWalkDistance + "\n");
        Thread.CurrentThread.pushToStack<MechComponent>("EFFECT_SOURCE", component);
        for (int index = 0; index < this.statusEffects.Length; ++index) {
          EffectData statusEffect = this.statusEffects[index];
          if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive) {
            string effectID = string.Format("ActivatableEffect_{0}_{1}", (object)component.parent.GUID, (object)component.uid);
            if (statusEffect.targetingData.specialRules == AbilityDef.SpecialRules.Aura && !component.parent.AuraComponents.Contains(component)) {
              component.parent.AuraComponents.Add(component);
            } else if (statusEffect.targetingData.effectTargetType == EffectTargetType.Creator) {
              typeof(MechComponent).GetMethod("ApplyPassiveEffectToTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, new object[4] {
                (object)statusEffect,(object)component.parent,(object)((ICombatant)component.parent),(object)effectID
              });
              component.createdEffectIDs.Add(effectID);
              Log.Debug?.Write("Activate effect " + effectID + ":" + statusEffect.Description.Id + "\n");
            }
          }
        }
        Thread.CurrentThread.popFromStack<MechComponent>("EFFECT_SOURCE");
        PrintActorStatistic(component.parent);
        component.parent.ResetPathing(false);
        //if (isInital == false) {
        //  Log.LogWrite(" Updating auras\n");
        //  AuraCache.UpdateAurasToActor(component.parent.Combat.AllActors, component.parent, component.parent.CurrentPosition, EffectTriggerType.TurnUpdate, true);
        //  AuraCache.RefreshECMStates(component.parent.Combat.AllActors, EffectTriggerType.TurnUpdate);
        //}
        Log.Debug?.Write(" sprint:" + component.parent.MaxSprintDistance + "\n");
        Log.Debug?.Write(" walk:" + component.parent.MaxWalkDistance + "\n");
      }
    }
    public void PrintActorStatistic(AbstractActor actor) {
      Log.Debug?.TWL(0,actor.DisplayName+" statistic");
      foreach(var stat in actor.StatCollection) {
        Log.Debug?.WL(1, stat.Key + ":" + stat.Value.CurrentValue);
      }
    }
    public void applyOfflineEffects(MechComponent component,bool isInital) {
      if (this.offlineStatusEffects == null) {
        Log.Debug?.Write(" no offline effects\n");
      } else {
        Log.Debug?.Write(" offline effects count: " + this.offlineStatusEffects.Length + "\n");
        Log.Debug?.Write(" sprint:" + component.parent.MaxSprintDistance + "\n");
        Log.Debug?.Write(" walk:" + component.parent.MaxWalkDistance + "\n");
        Thread.CurrentThread.pushToStack<MechComponent>("EFFECT_SOURCE", component);
        for (int index = 0; index < this.offlineStatusEffects.Length; ++index) {
          EffectData statusEffect = this.offlineStatusEffects[index];
          if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive) {
            string effectID = string.Format("OfflineActivatableEffect_{0}_{1}", (object)component.parent.GUID, (object)component.uid);
            if (statusEffect.targetingData.specialRules == AbilityDef.SpecialRules.Aura && !component.parent.AuraComponents.Contains(component)) {
              component.parent.AuraComponents.Add(component);
            } else if (statusEffect.targetingData.effectTargetType == EffectTargetType.Creator) {
              typeof(MechComponent).GetMethod("ApplyPassiveEffectToTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, new object[4] {
                (object)statusEffect,(object)component.parent,(object)((ICombatant)component.parent),(object)effectID
              });
              component.createdEffectIDs.Add(effectID);
              Log.Debug?.Write("Activate offline effect " + effectID + ":" + statusEffect.Description.Id + "\n");
            }
          }
        }
        PrintActorStatistic(component.parent);
        Thread.CurrentThread.popFromStack<MechComponent>("EFFECT_SOURCE");
        component.parent.ResetPathing(false);
        //if (isInital == false) {
        //  Log.LogWrite(" Updating auras\n");
        //  AuraCache.UpdateAurasToActor(component.parent.Combat.AllActors, component.parent, component.parent.CurrentPosition, EffectTriggerType.TurnUpdate, true);
        //  AuraCache.RefreshECMStates(component.parent.Combat.AllActors, EffectTriggerType.TurnUpdate);
        //}
        Log.Debug?.Write(" sprint:" + component.parent.MaxSprintDistance + "\n");
        Log.Debug?.Write(" walk:" + component.parent.MaxWalkDistance + "\n");
      }
    }
    public void removeOnlineEffects(MechComponent component) {
      List<string> actEffectsIDs = new List<string>();
      foreach (string effID in component.createdEffectIDs) {
        if (effID.StartsWith("ActivatableEffect_")) {
          actEffectsIDs.Add(effID);
        }
      }
      foreach (string effId in actEffectsIDs) {
        List<Effect> allEffectsWithId = component.parent.Combat.EffectManager.GetAllEffectsWithID(effId);
        for (int index2 = 0; index2 < allEffectsWithId.Count; ++index2) {
          if (allEffectsWithId[index2].EffectData.targetingData.specialRules != AbilityDef.SpecialRules.Aura) {
            Log.Debug?.Write("Removing effect " + effId + ":" + allEffectsWithId[index2].EffectData.Description.Id + "\n");
            component.parent.CancelEffect(allEffectsWithId[index2], false);
          } else {
            Log.Debug?.Write("Aura effect " + effId + ":" + allEffectsWithId[index2].EffectData.Description.Id + " will be removed at aura cache update\n");
          }
        }
        component.createdEffectIDs.Remove(effId);
      }
      PrintActorStatistic(component.parent);
    }
    public void removeOfflineEffects(MechComponent component) {
      List<string> actEffectsIDs = new List<string>();
      foreach (string effID in component.createdEffectIDs) {
        if (effID.StartsWith("OfflineActivatableEffect_")) {
          actEffectsIDs.Add(effID);
        }
      }
      foreach (string effId in actEffectsIDs) {
        List<Effect> allEffectsWithId = component.parent.Combat.EffectManager.GetAllEffectsWithID(effId);
        for (int index2 = 0; index2 < allEffectsWithId.Count; ++index2) {
          Log.Debug?.Write("Removing effect " + effId + ":" + allEffectsWithId[index2].EffectData.Description.Id + "\n");
          if (allEffectsWithId[index2].EffectData.targetingData.specialRules != AbilityDef.SpecialRules.Aura) {
            component.parent.CancelEffect(allEffectsWithId[index2], false);
          } else {
            Log.Debug?.Write("Aura effect " + effId + ":" + allEffectsWithId[index2].EffectData.Description.Id + " will be removed at aura cache update\n");
          }
        }
        component.createdEffectIDs.Remove(effId);
      }
      PrintActorStatistic(component.parent);
    }
    public static void shutdownComponent(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      } else {
        component.StatCollection.Set<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      activatable.removeOnlineEffects(component);
      activatable.removeOfflineEffects(component);
      component.UpdateAuras(false);
      component.parent?.bodyAura()?.RetriggerEnter(false, false, true);
      component.parent?.bodyAura()?.RetriggerExit();
      ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
      if (activeVFX != null) { activeVFX.CleanupSelf(); }
      Log.Debug?.Write(component.defId+" shutdown\n");
    }
    public static void startupComponent(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      Log.Debug?.Write(component.defId + " restart\n");
      if (activatable.ActiveByDefault == true) {
        ActivatableComponent.activateComponent(component, true, true);
      } else {
        activatable.applyOfflineEffects(component, true);
      }
      component.UpdateAuras(false);
    }
    public static void deactivateComponent(MechComponent component) {
      CombatHUDEquipmentSlotEx.ClearCache(component);
      if (component.IsFunctional == false) { return; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (ActivatableComponent.isComponentActivated(component) == false) { return; };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      } else {
        component.StatCollection.Set<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if (activatable.statusEffects == null) {
        return;
      }
      activatable.removeOnlineEffects(component);
      activatable.applyOfflineEffects(component, false);
      component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID,
        component.Description.UIName + " " + activatable.DeactivationMessage,
        (activatable.ActivationIsBuff) ? FloatieMessage.MessageNature.Debuff : FloatieMessage.MessageNature.Buff
      ));
      ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
      if (activeVFX != null) { activeVFX.CleanupSelf(); }
      component.playDeactivateSound();
      component.UpdateAuras(false);
      component.parent?.bodyAura()?.RetriggerEnter(false, false, true);
      component.parent?.bodyAura()?.RetriggerExit();
      CAEAuraHelper.ClearAuraPreviewCache();
      component.LinkageDectivate(false);
    }
    public static void toggleComponentActivation(MechComponent component) {
      CombatHUDEquipmentSlotEx.ClearCache(component);
      Log.Debug?.Write("toggleComponentActivation " + component.defId + "\n");
      if (component.IsFunctional == false) {
        Log.Debug?.Write(" not functional\n");
        return;
      };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        Log.Debug?.Write(" not activatable\n");
        return;
      }
      if(activatable.SafeActivation == false) { component.parent.OnActivationBegin(component.parent.GUID, -1); };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if (ActivatableComponent.isComponentActivated(component) == false) {
        Log.Debug?.Write(" activating\n");
        ActivatableComponent.activateComponent(component,false,false);
        if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActivedRound) == false) {
          component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActivedRound, component.parent.Combat.TurnDirector.CurrentRound);
        } else {
          component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActivedRound, component.parent.Combat.TurnDirector.CurrentRound);
        }
      } else {
        ActivatableComponent.deactivateComponent(component);
      }
    }

    //public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab) {
    //  Log.Debug?.Write("PreValidateDrop\n");
    //  if (this.MechTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon) {
    //    float self_tonnage = (float)Math.Ceiling((double)this.Def.Tonnage);
    //    float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.MechTonnageWeightMult));
    //    float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.MechTonnageWeightMult));
    //    Log.Debug?.Write(" checking on tonnage. mech : " + downLimit + " - " + upLimit + "\n");
    //    if ((mechlab.MechLab.activeMechDef.Chassis.Tonnage <= downLimit) || (mechlab.MechLab.activeMechDef.Chassis.Tonnage > upLimit)) {
    //      string result = "This component is not sutable for this chassis. Tonnage must be " + (downLimit + 1f) + "-" + upLimit;
    //      return result;
    //    }
    //  }
    //  if (this.MechTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon) {
    //    float self_tonnage = this.Def.InventorySize;
    //    float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.MechTonnageSlotsMult));
    //    float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.MechTonnageSlotsMult));
    //    Log.Debug?.Write(" checking on tonnage. mech : " + downLimit + " - " + upLimit + "\n");
    //    if ((mechlab.MechLab.activeMechDef.Chassis.Tonnage <= downLimit) || (mechlab.MechLab.activeMechDef.Chassis.Tonnage > upLimit)) {
    //      string result = "This component is not sutable for this chassis. Tonnage must be " + (downLimit + 1f) + "-" + upLimit;
    //      return result;
    //    }
    //  }
    //  if ((this.EngineTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon) || (this.EngineTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon)) {
    //    Log.Debug?.Write(" checking on engine weight\n");
    //    List<MechComponentRef> components = mechlab.MechLab.activeMechInventory;
    //    float engineTonnage = 0f;
    //    foreach (var comp in components) {
    //      if (comp.Def.IsCategory("EnginePart")) {
    //        engineTonnage += comp.Def.Tonnage;
    //      };
    //    }
    //    if (engineTonnage < CustomActivatableEquipment.Core.Epsilon) {
    //      return string.Empty;
    //    }
    //    if (this.EngineTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon) {
    //      float self_tonnage = (float)Math.Ceiling((double)this.Def.Tonnage);
    //      float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.EngineTonnageWeightMult));
    //      float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.EngineTonnageWeightMult));
    //      Log.Debug?.Write(" checking on tonnage. engine : " + downLimit + " - " + upLimit + "\n");
    //      if ((engineTonnage <= downLimit) || (engineTonnage > upLimit)) {
    //        string result = "This component is not sutable for this chassis. Engine tonnage must be " + (downLimit + 1f) + "-" + upLimit;
    //        return result;
    //      }
    //    }
    //    if (this.EngineTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon) {
    //      float self_tonnage = this.Def.InventorySize;
    //      float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.EngineTonnageSlotsMult));
    //      float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.EngineTonnageSlotsMult));
    //      Log.Debug?.Write(" checking on tonnage. engine : " + downLimit + " - " + upLimit + "\n");
    //      if ((engineTonnage <= downLimit) || (engineTonnage > upLimit)) {
    //        string result = "This component is not sutable for this chassis. Engine tonnage must be " + (downLimit + 1f) + "-" + upLimit;
    //        return result;
    //      }
    //    }
    //  }
    //  if (this.NoUniqueCheck == false) {
    //    foreach (var comp in mechlab.MechLab.activeMechInventory) {
    //      ActivatableComponent activatable = comp.Def.GetComponent<ActivatableComponent>();
    //      if (activatable != null) {
    //        if (activatable.ButtonName == this.ButtonName) {
    //          string result = "This mech already have component of the same type";
    //          return result;
    //        }
    //      }
    //    }
    //  }
    //  return string.Empty;
    //  //mechlab.MechLab.activeMechDef.
    //}
  }
  public enum AuraUpdateFix {
    None,
    Never,
    Time,
    Position
  };
  public class ComponentToggle {
    public MechComponent component;
    public ActivatableComponent activatable;
    public ComponentToggle(MechComponent c, ActivatableComponent a) {
      this.component = c;
      this.activatable = a;
    }
    public void toggle() {
      Log.Debug?.Write("Toggle activatable " + component.defId + "\n");
      ActivatableComponent.toggleComponentActivation(this.component);
    }
  }

  public class HeatSinkToggle {
    public Mech mech;
    public GenericPopup popup;
    public HeatSinkToggle(Mech m) {
      this.mech = m;
    }
    public void AddHeatSink() {
      if (mech == null) {
        Log.WriteCritical("WARNING! Alter mech heatsink state without mech selected");
        return;
      }
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) { continue; }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName) == false) { continue; };
        miscComponent.RemoveTag(CustomActivatableEquipment.Core.HeatSinkOfflineTagName);
        this.popup.TextContent = HeatSinkToggle.MechHeatSinksInfo(this.mech);
        break;
      }
    }
    public void RemoveHeatSink() {
      if (mech == null) {
        Log.WriteCritical("WARNING! Alter mech heatsink state without mech selected");
        return;
      }
      Log.Debug?.Write("Deactivating heatsink:" + mech.DisplayName + ":" + mech.GUID + "\n");
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) { continue; }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) { continue; };
        Log.Debug?.Write("  Active heat sinc found:" + miscComponent.getCCGUID() + "\n");
        miscComponent.AddTag(CustomActivatableEquipment.Core.HeatSinkOfflineTagName);
        Log.Debug?.Write("  Add tag:" + miscComponent.ComponentTags().ToString() + "\n");
        this.popup.TextContent = HeatSinkToggle.MechHeatSinksInfo(this.mech);
        break;
      }
    }
    public static string MechHeatSinksInfo(Mech mech) {
      int AvaibleHeatSinks = 0;
      int DestroyedHeatSinks = 0;
      int OfflineHeatSinks = 0;
      float OnlineHeatDissipation = 0f;
      if (mech == null) {
        Log.WriteCritical("WARNING! trying to get mech heat info without mech selected");
        return "No mech selected";
      }
      if (mech.miscComponents == null) {
        Log.WriteCritical("WARNING! trying to get mech heat info without misc components on mech");
        return "No heatsinks found";
      }
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent == null) {
          Log.WriteCritical("WARNING! empty component");
          continue;
        }
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (miscComponent == null) {
          Log.WriteCritical("WARNING! Component without def");
          continue;
        }
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        ++AvaibleHeatSinks;
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) {
          ++DestroyedHeatSinks; continue;
        }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) {
          ++OfflineHeatSinks; continue;
        }
        OnlineHeatDissipation += componentDef.DissipationCapacity;
      }
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("Current heat level:" + mech.CurrentHeat + "\n");
      stringBuilder.Append("Overheat heat level:" + mech.OverheatLevel + "\n");
      stringBuilder.Append("Shutdown heat level:" + mech.MaxHeat + "\n");
      stringBuilder.Append("All chassis dedicated heat sinks count:" + AvaibleHeatSinks + "\n");
      stringBuilder.Append("Destroyed dedicated heat sinks count:" + DestroyedHeatSinks + "\n");
      stringBuilder.Append("Offline dedicated heat sinks count:" + OfflineHeatSinks + "\n");
      stringBuilder.Append("Online dedicated heat sinks count:" + (AvaibleHeatSinks - DestroyedHeatSinks - OfflineHeatSinks) + "\n");
      /*stringBuilder.Append("Online dedicated heat sinks dissipation:" + (OnlineHeatDissipation) + "\n");
      int tot
      stringBuilder.Append("Temporary heat sinks dissipation:" + mech.HeatSinkCapacity + "\n");
      float num = 1f;
      if (mech.occupiedDesignMask != null && !Mathf.Approximately(mech.occupiedDesignMask.heatSinkMultiplier, 1f)) {
        num *= mech.occupiedDesignMask.heatSinkMultiplier;
        stringBuilder.Append("Mech position heat sink multiplier:" + (mech.occupiedDesignMask.heatSinkMultiplier) + "\n");
      }
      if (mech.Combat.MapMetaData.biomeDesignMask != null && !Mathf.Approximately(mech.Combat.MapMetaData.biomeDesignMask.heatSinkMultiplier, 1f)) {
        num *= mech.Combat.MapMetaData.biomeDesignMask.heatSinkMultiplier;
        stringBuilder.Append("Biome heat sink multiplier:" + (mech.occupiedDesignMask.heatSinkMultiplier) + "\n");
      }
      stringBuilder.Append("Global combat heat sink multiplyer:" + (mech.Combat.Constants.Heat.GlobalHeatSinkMultiplier) + "\n");

      OnlineHeatDissipation = (float)Math.Round(((double)OnlineHeatDissipation * ((double)num * (double)mech.Combat.Constants.Heat.GlobalHeatSinkMultiplier)));*/
      stringBuilder.Append("Effective total heat dissipation:" + (mech.AdjustedHeatsinkCapacity) + "\n");
      Log.Debug?.Write(stringBuilder.ToString());
      return stringBuilder.ToString();
    }
  }
  public static partial class Core {
    public static float Epsilon = 0.01f;
    //public static Dictionary<string, GameObject> AdditinalFXObjects = new Dictionary<string, GameObject>();
    public static HarmonyInstance harmony { get; set; } = null;
    //public static List<ActivatableComponent> currentActiveComponentsDlg = new List<ActivatableComponent>();
    public static readonly string HeatSinkOfflineTagName = "offline";
    //public static Dictionary<string,List<ComponentToggle>>
    private static FieldInfo f_statCollection_stats = null;
    public static bool checkExistance(StatCollection statCollection, string statName) {
      if (f_statCollection_stats == null) { f_statCollection_stats = typeof(StatCollection).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance); }
      return ((Dictionary<string, Statistic>)f_statCollection_stats.GetValue(statCollection)).ContainsKey(statName);
    }
    public static void ShowHeatDlg(Mech mech) {
      if (mech == null) {
        GenericPopupBuilder popup = GenericPopupBuilder.Create("Heat sinks", "No mech selected");
        popup.AddButton("Done", (Action)null, true, (PlayerAction)null);
        popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
      } else {
        GenericPopupBuilder popup = GenericPopupBuilder.Create("Heat sinks", HeatSinkToggle.MechHeatSinksInfo(mech));
        HeatSinkToggle HTT = new HeatSinkToggle(mech);
        popup.AddButton("Done", (Action)null, true, (PlayerAction)null);
        popup.AddButton("-1", new Action(HTT.RemoveHeatSink), false, (PlayerAction)null);
        popup.AddButton("+1", new Action(HTT.AddHeatSink), false, (PlayerAction)null);
        HTT.popup = popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
      }
    }
    public static void ShowEquipmentDlg(Mech mech, CombatHUD HUD) {
      ActivatebleDialogHelper.CreateDialog(mech, HUD);
      return;
      /*List<string> activatables = new List<string>();
      List<ComponentToggle> actComps = new List<ComponentToggle>();
      //Core.currentActiveComponentsDlg.Clear();
      foreach (MechComponent component in mech.allComponents) {
        //if (component.IsFunctional == false) { return; };
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable != null) {
          //if (activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
          //Log.LogWrite(" but can't be activated manualy activatable\n");
          //} else {
          if (activatable.CanNotBeActivatedManualy == true) {
            if (activatable.AutoActivateOnHeat <= Core.Epsilon) { continue; };
          }
          Log.LogWrite(component.defId + ":" + component.parent.GUID + ":" + component.getCCGUID() + " is activatable\n");
          activatables.Add(activatable.ButtonName);
          actComps.Add(new ComponentToggle(component, activatable));
          //}
          //Core.currentActiveComponentsDlg.Add(activatable);
        } else {
          Log.LogWrite(component.defId + ":" + component.parent.GUID + ":" + component.getCCGUID() + " is not activatable\n");
        }
      }
      StringBuilder text = new StringBuilder();
      //text.Append("Active components(" + activatables.Count + "):");
      for (int index = 0; index < activatables.Count; ++index) {
        MechComponent component = actComps[index].component;
        text.Append("\n" + component.UIName);
        text.Append(" __/CAE.STATE/__:");
        if (component.IsFunctional == false) {
          text.Append(" __/CAE.NonFunctional/__");
          continue;
        }
        if (ActivatableComponent.isOutOfCharges(component)) {
          text.Append(" __/CAE.OutOfCharges/__");
          continue;
        }
        if (actComps[index].activatable.ChargesCount == -1) {
          text.Append(" __/CAE.OPERATIONAL/__");
          continue;
        }
        if (actComps[index].activatable.ChargesCount > 0) {
          text.Append(" __/CAE.CHARGES/__:" + ActivatableComponent.getChargesCount(component));
        }
        if (ActivatableComponent.isComponentActivated(component)) {
          text.Append(" " + actComps[index].activatable.ActivationMessage + " ");
          if (actComps[index].activatable.CanBeactivatedManualy == false) {
            if (component.parent is Mech) {
              float neededHeat = (actComps[index].activatable.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? actComps[index].activatable.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : actComps[index].activatable.AutoDeactivateOnHeat;
              text.Append("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        } else {
          text.Append(" " + actComps[index].activatable.DeactivationMessage + " ");
          if (actComps[index].activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
            if (component.parent is Mech) {
              float neededHeat = (actComps[index].activatable.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? actComps[index].activatable.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : actComps[index].activatable.AutoActivateOnHeat;
              text.Append("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        }
        float failChance = ActivatableComponent.getEffectiveComponentFailChance(component);
        //ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        //if (failChance < activatable.FailFlatChance) { failChance = activatable.FailFlatChance; };
        text.Append(" __/CAE.FAIL/__:" + Math.Round(failChance * 100f) + "%");
      }
      if (HUD.SelectedTarget != null) {
        text.Append("__/CAE.SelectedTargetForbidden/__");
      }
      if (mech.IsAvailableThisPhase == false) {
        text.Append("__/CAE.NotAvaibleThisPhase/__");
      }
      if (mech.HasMovedThisRound) {
        text.Append("\nCan't activate/deactivate equipment after move");
      }
      GenericPopupBuilder popup = GenericPopupBuilder.Create("__/CAE.Components/__", text.ToString());

      popup.AddButton("__/CAE.Done/__", (Action)null, true, (PlayerAction)null);
      if ((HUD.SelectedTarget == null)&&(mech.IsAvailableThisPhase)&&(mech.HasMovedThisRound == false)) {
        for (int index = 0; index < activatables.Count; ++index) {
          if (actComps[index].component.IsFunctional == false) { continue; };
          if (actComps[index].activatable.CanBeactivatedManualy) {
            if (ActivatableComponent.isOutOfCharges(actComps[index].component) == false) {
              popup.AddButton(activatables[index], new Action(actComps[index].toggle), true, (PlayerAction)null);
            }
          }
        }
      }
      Log.LogWrite("Rendering popup:" + text.ToString() + "\n");
      popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
      //ComponentsMenu menu = new ComponentsMenu(mech);
      //menu.Render();*/
    }

    public static Settings Settings = new Settings();
    public static Settings GlobalSettings = new Settings();
    public static void FinishedLoading(List<string> loadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources) {
      Log.Debug?.TWriteCritical(0, "FinishedLoading");
      try {
        foreach (var customResource in customResources) {
          Log.Debug?.WL(1, "customResource:" + customResource.Key);
          if (customResource.Key == nameof(WeaponAddonDef)) {
            foreach (var resource in customResource.Value) {
              Log.Debug?.WL(2, "resource:" + resource.Key + "=" + resource.Value.FilePath);
              resource.Value.RegisterWeaponAddon(resource.Key);
            }
          }
        }
        CustomSettings.ModsLocalSettingsHelper.RegisterLocalSettings("ActivatebleEquipment", "Activatable Equipment", LocalSettingsHelper.ResetSettings, LocalSettingsHelper.ReadSettings);
        WeaponDefModesCollectHelper.RegisterCallback("ActivatebleEquipment", WeaponAddonDefHelper.GatherModes);
        C3Helper.Init();
        Core.harmony.Patch(InjurePilot_Check.PatchMethod(),new HarmonyMethod(InjurePilot_Check.PrefixMethod()), new HarmonyMethod(InjurePilot_Check.PostfixMethod()));
        //ExtendedDescriptionHelper.DetectMechEngineer();
      } catch (Exception e) {
        Log.Debug?.TWriteCritical(0, e.ToString());
      }
    }
    public class testJsonClass {
      public string field1 { get; set; }
      public JObject jobject { get; set; } = new JObject();
    };

    public static void Init(string directory, string settingsJson) {
      CustomActivatableEquipment.Log.BaseDirectory = directory;
      CustomActivatableEquipment.Log.InitLog();
      Core.Settings = JsonConvert.DeserializeObject<CustomActivatableEquipment.Settings>(settingsJson);
      Core.GlobalSettings = JsonConvert.DeserializeObject<CustomActivatableEquipment.Settings>(settingsJson);
      var settingsObject = (JObject)JsonConvert.DeserializeObject(settingsJson);
        var sensorAura = settingsObject["sensorsAura"];
        var statusEffects = (JArray)sensorAura?["statusEffects"];
        var firstStatusEffect = statusEffects?[0];
        var description = firstStatusEffect?["Description"];

        if (description != null)
        {
            var id = description["Id"].ToString();
            var name = description["Name"].ToString();
            var details = description["Details"].ToString();
            var icon = description["Icon"].ToString();
            Core.Settings.sensorsAura.statusEffects.First().Description =
                new BaseDescriptionDef(id, name, details, icon);
        }

        CustomActivatableEquipment.Log.Debug?.TWL(0,"Initing... " + directory + " version: " + Assembly.GetExecutingAssembly().GetName().Version + "\n"
                                              + "Settings = [" + JsonConvert.SerializeObject(Core.Settings, Formatting.Indented, new JsonSerializerSettings()
                                                                                                                                     {
                                                                                                                                         ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                                                                                                                     }) + "]", true);
      Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NoCollision"), LayerMask.NameToLayer("NoCollision"), false); //makes not ignore collision between "NoCollision" layer
      Log.Debug?.W("Layer Name;");
      for (int layer1 = 8; layer1 < 32; ++layer1) {
        Log.Debug?.W(LayerMask.LayerToName(layer1) + "(" + layer1 + ");");
      }
      Log.Debug?.WL("");
      for (int layer1 = 8; layer1 < 32; ++layer1) {
        Log.Debug?.W(LayerMask.LayerToName(layer1)+"("+layer1+");");
        for (int layer2 = 8; layer2 < 32; ++layer2) {
          Log.Debug?.W((Physics.GetIgnoreLayerCollision(layer1, layer2) ? "":"X") +";");
        }
        Log.Debug?.WL("");
      }
      Core.Settings.sensorsAura.MinefieldDetector = true;
      /*try {
        string apath = Path.Combine(directory, "assets");
        Log.LogWrite("additional assets:" + Core.Settings.AdditionalAssets.Count + "\n");
        foreach (string assetName in Core.Settings.AdditionalAssets) {
          string path = Path.Combine(apath, assetName);
          if (File.Exists(path)) {
            var assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle != null) {
              Log.LogWrite("asset " + path + ":" + assetBundle.name + " loaded\n");
              UnityEngine.GameObject[] objects = assetBundle.LoadAllAssets<GameObject>();
              Log.LogWrite("FX objects:\n");
              foreach (var obj in objects) {
                Log.LogWrite(" " + obj.name + "\n");
                AdditinalFXObjects.Add(obj.name, obj);
              }
            } else {
              Log.LogWrite("fail to load:" + path + "\n");
            }
          } else {
            Log.LogWrite("not exists:" + path + "\n");
          }
        }
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n");
      }*/
      try {
        CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
        harmony = HarmonyInstance.Create("io.mission.activatablecomponents");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        harmony.Patch(
        typeof(Weapon).Assembly.GetType("AreAnyHostilesInWeaponRangeNode").GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic), 
        new HarmonyMethod(typeof(AreAnyHostilesInWeaponRangeNode_Tick).GetMethod("Prefix")));
        ActivatebleDialogHelper.Init();
        CustomMechHelper.RegisterInitGameRepPrefix(Mech_InitGameRep_ECMRemove.Prefix);
        CustomMechHelper.RegisterInitGameRepPostfix(Mech_InitGameRep_ECMRemove.Postfix);

        //string testJson = "{ \"field1\":\"value\", \"jobject\":{ \"jobjectfield\": { \"jobjectvalue\":\"jobjectsubvalue\" } }}";
        //testJsonClass testClassObj = new testJsonClass();
        //Thread.CurrentThread.SetFlag(JSONSerializationUtility_RehydrateObjectFromDictionary.DEBUG_OUTOPUT_FLAG);
        //JSONSerializationUtility.FromJSON<testJsonClass>(testClassObj, testJson);
        //Thread.CurrentThread.ClearFlag(JSONSerializationUtility_RehydrateObjectFromDictionary.DEBUG_OUTOPUT_FLAG);
        //Log.Debug?.TWL(0, "JSON test result:");
        //Log.Debug?.WL(0, JsonConvert.SerializeObject(testClassObj, Formatting.Indented));
        //testClassObj = JsonConvert.DeserializeObject<testJsonClass>(testJson);
        //Log.Debug?.TWL(0, "JSON test result:");
        //Log.Debug?.WL(0, JsonConvert.SerializeObject(testClassObj, Formatting.Indented));
      } catch (Exception e) {
        CustomActivatableEquipment.Log.Debug?.Write(e.ToString() + "\n");
      }
    }
  }
}
