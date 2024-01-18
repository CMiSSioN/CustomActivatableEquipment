using BattleTech.UI;
using HarmonyLib;
using CustomActivatableEquipment;
using System.Reflection;
using BattleTech;
using UnityEngine;
using CustAmmoCategories;
using System;
using System.Collections.Generic;

namespace CustomActivatablePatches {

  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("DesiredAuraProjectionState")]
  [HarmonyPatch(MethodType.Getter)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_DesiredAuraProjectionState {
    public static bool IsVisibleToPlayer(this AbstractActor unit) {
      if (unit == null || (UnityEngine.Object)unit.GameRep == (UnityEngine.Object)null || unit.Combat == null)
        return false;
      if (!unit.team.IsFriendly(unit.Combat.LocalPlayerTeam) && unit.Combat.LocalPlayerTeam.VisibilityToTarget((ICombatant)unit) != VisibilityLevel.BlipGhost)
        return unit.Combat.LocalPlayerTeam.VisibilityToTarget((ICombatant)unit) == VisibilityLevel.LOSFull;
      return true;
    }
    public static bool isAuraVisible(this CombatAuraReticle __instance, AuraBubble aura, bool spinning) {
      if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.HideAll) { return false; };
      if ((__instance.owner.IsVisibleToPlayer() == false) || (__instance.owner.IsOperational == false)) {
         return false;
      }
      if (aura != null) {
        if (aura.Def.isSpining != spinning) { return false; }
        if (aura.isMainSensors) {
          if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.ShowAll) { return true; }
          if (aura.Def.NotShowOnSelected) { return false; };
          if (aura.Def.HideOnNotSelected) {
            if (__instance.HUD.SelectedActor != null) {
              if (__instance.HUD.SelectedActor.GUID == __instance.owner.GUID) {
                return true;
              }
            }
            return false;
          } else {
            return true;
          }
        }
        if ((aura.source != null)||(aura.pilot != null)) {
          if (aura.Def.isSpining != spinning) { return false; }
          Weapon weapon = aura.source as Weapon;
          if ((weapon == null) || (aura.Def.Id != "AMS")) {
            if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.ShowAll) { return true; }
            if (aura.Def.NotShowOnSelected) { return false; };
            if (aura.Def.HideOnNotSelected) {
              if (__instance.HUD.SelectedActor != null) {
                if (__instance.HUD.SelectedActor.GUID == __instance.owner.GUID) {
                  return true;
                }
              }
              return false;
            } else {
              return true;
            }
          } else {
            if (weapon.isAMS() == false) {
              return false;
            }
            if (weapon.IsEnabled == false) {
              return false;
            }
            if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.ShowAll) { return true; }
            if (aura.Def.NotShowOnSelected) { return false; };
            if (aura.Def.HideOnNotSelected) {
              if (__instance.HUD.SelectedActor != null) {
                if (__instance.HUD.SelectedActor.GUID == __instance.owner.GUID) {
                  return true;
                }
              }
              return false;
            } else {
              return true;
            }
          }
        } else {
          return false;
        }
      }
      return false;
    }
    public static bool Prefix(CombatAuraReticle __instance, ref ButtonState __result) {
      try {
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { __result = ButtonState.Disabled;  return false; }
        __result = __instance.isAuraVisible(aura, false)? ButtonState.Enabled : ButtonState.Disabled;
        return false;
      }catch(Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        UIManager.logger.LogException(e);
      }
      return true;
      //__instance.GameRep.PlayVFXAt(__instance.GameRep.thisTransform, Vector3.zero, "vfxPrfPrtl_ECM_loop", true, Vector3.zero, false, -1f);
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshAuraRange")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshAuraRange {
    public static void Prefix(ref bool __runOriginal, CombatAuraReticle __instance, ButtonState auraProjectionState) {
      try {
        if (!__runOriginal) { return; }
        GameObject auraRangeScaledObject = __instance.auraRangeScaledObject;
        if (auraProjectionState == ButtonState.Disabled) {
          auraRangeScaledObject.SetActive(false);
          __runOriginal = false; return;
        }
        //AuraBubble mainSensorsBubble = __instance.MainSensors();
        //Log.LogWrite("CombatAuraReticle.RefreshAuraRange " + (mainSensorsBubble == null ? "null" : mainSensorsBubble.collider.radius.ToString()) + "\n");
        //if (mainSensorsBubble != null) {
          //auraRangeScaledObject.SetActive(true);
          //float b = mainSensorsBubble.collider.radius;
          //if (!Mathf.Approximately(___currentAuraRange, b)) {
            //auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          //}
          //___currentAuraRange = b;
          //return false;
        //}
        AuraBubble auraBubble = __instance.AuraBubble();
        if (auraBubble != null) {
          auraRangeScaledObject.SetActive(true);
          float b = auraBubble.collider.radius;
          if (!Mathf.Approximately(__instance.currentAuraRange, b)) {
            auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          }
          __instance.currentAuraRange = b;
          __runOriginal = false; return;
        }
      }catch(Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
        UIManager.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshActiveProbeRange")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshActiveProbeRange {
    public static void Prefix(ref bool __runOriginal, CombatAuraReticle __instance, bool showActiveProbe) {
      try {
        if (!__runOriginal) { return; }
        GameObject activeProbeRangeScaledObject = __instance.activeProbeRangeScaledObject;
        if (showActiveProbe == false) {
          activeProbeRangeScaledObject.SetActive(false);
          __runOriginal = false; return;
        }
        //AuraBubble mainSensorsBubble = __instance.MainSensors();
        //Log.LogWrite("CombatAuraReticle.RefreshAuraRange " + (mainSensorsBubble == null ? "null" : mainSensorsBubble.collider.radius.ToString()) + "\n");
        //if (mainSensorsBubble != null) {
        //auraRangeScaledObject.SetActive(true);
        //float b = mainSensorsBubble.collider.radius;
        //if (!Mathf.Approximately(___currentAuraRange, b)) {
        //auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
        //}
        //___currentAuraRange = b;
        //return false;
        //}
        AuraBubble auraBubble = __instance.AuraBubble();
        if (auraBubble != null) {
          activeProbeRangeScaledObject.SetActive(true);
          float b = auraBubble.collider.radius;
          if (!Mathf.Approximately(__instance.currentAPRange, b)) {
            activeProbeRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          }
          __instance.currentAPRange = b;
          __runOriginal = false; return;
        }
      } catch (Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
        UIManager.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshActiveProbeState")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshActiveProbeState {
    public static Vector3 dbgPos = Vector3.zero;
    public static void Prefix(ref bool __runOriginal, CombatAuraReticle __instance, ref bool __result) {
      try {
        if (!__runOriginal) { return; }
        //if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.HideAll) { __result = false; return false; };
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { return; };
        __result = __instance.isAuraVisible(aura,true);
        __runOriginal = false; return;
      }catch(Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        UIManager.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshActiveProbeColor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshActiveProbeColor {
    private static Dictionary<CombatAuraReticle, bool> SpinSave = new Dictionary<CombatAuraReticle, bool>();
    private static bool isSpining(this CombatAuraReticle reticle) {
      if (SpinSave.TryGetValue(reticle, out bool result)) { return result; }
      SpinSave.Add(reticle, false);
      return false;
    }
    private static void isSpining(this CombatAuraReticle reticle, bool value) {
      if (SpinSave.ContainsKey(reticle)) { SpinSave[reticle] = value; } else { SpinSave.Add(reticle, value); };
    }
    public static void Prefix(ref bool __runOriginal, CombatAuraReticle __instance, bool isBright) {
      try {
        if (!__runOriginal) { return; }
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { return; }
        if (isBright == false) { __runOriginal = false; return; }
        isBright = __instance.HUD.SelectedActor != null && __instance.HUD.SelectionHandler.ActiveState is SelectionStateMoveBase;
        if (isBright) {
          __instance.activeProbeDecal.DecalMaterial = __instance.activeProbeMatBright;
          //this.apSpinAnim.DORestartById("spin");
        } else {
          __instance.activeProbeDecal.DecalMaterial = __instance.activeProbeMatDim;
          //this.apSpinAnim.DOPause();
        }
        if(__instance.currentAPIsBright != aura.Def.isSpining) {
          __instance.currentAPIsBright = aura.Def.isSpining;
          if (__instance.currentAPIsBright) {
            __instance.apSpinAnim.DORestartById("spin");
          } else {
            __instance.apSpinAnim.DOPause();
          }
        }
        __runOriginal = false; return;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        UIManager.logger.LogException(e);
      }
      return;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("UpdatePosition")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_UpdatePosition {
    public static Vector3 dbgPos = Vector3.zero;
    public static void Postfix(CombatAuraReticle __instance) {
    }
  }
  public enum AuraShowState { Default, ShowAll, HideAll }
  [HarmonyPatch(typeof(CombatHUD))]
  [HarmonyPatch("Update")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatHUD_Update_HideReticlesHotKey {
    public static bool keypressed = false;
    private static AuraShowState f_hideReticles = AuraShowState.Default;
    public static AuraShowState hideReticles { get { return CACCombatState.IsInDeployManualState? AuraShowState.HideAll : f_hideReticles; } set { f_hideReticles = value; } }
    public static void Postfix(CombatHUD __instance) {
      bool key = Input.GetKey(KeyCode.A);
      bool mod = Input.GetKey(KeyCode.LeftControl);
      bool res = key && mod;
      if (keypressed != res) {
        keypressed = res;
        if (keypressed) {
          switch (f_hideReticles) {
            case AuraShowState.Default: f_hideReticles = AuraShowState.HideAll; break;
            case AuraShowState.HideAll: f_hideReticles = AuraShowState.ShowAll; break;
            case AuraShowState.ShowAll: f_hideReticles = AuraShowState.Default; break;
          }
        };
      }
    }
  }
  //public 
}