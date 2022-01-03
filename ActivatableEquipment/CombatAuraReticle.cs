using BattleTech.UI;
using Harmony;
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
    private static FieldInfo fOwner;
    public static bool Prepare() {
      fOwner = typeof(CombatAuraReticle).GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic);
      return true;
    }
    public static AbstractActor owner(this CombatAuraReticle reticle) {
      return (AbstractActor)fOwner.GetValue(reticle);
    }
    public static bool IsVisibleToPlayer(this AbstractActor unit) {
      if (unit == null || (UnityEngine.Object)unit.GameRep == (UnityEngine.Object)null || unit.Combat == null)
        return false;
      if (!unit.team.IsFriendly(unit.Combat.LocalPlayerTeam) && unit.Combat.LocalPlayerTeam.VisibilityToTarget((ICombatant)unit) != VisibilityLevel.BlipGhost)
        return unit.Combat.LocalPlayerTeam.VisibilityToTarget((ICombatant)unit) == VisibilityLevel.LOSFull;
      return true;
    }
    public static bool isAuraVisible(this CombatAuraReticle __instance, AuraBubble aura, AbstractActor ___owner, CombatHUD ___HUD, bool spinning) {
      if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.HideAll) { return false; };
      if ((___owner.IsVisibleToPlayer() == false) || (___owner.IsOperational == false)) {
         return false;
      }
      if (aura != null) {
        if (aura.Def.isSpining != spinning) { return false; }
        if (aura.isMainSensors) {
          if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.ShowAll) { return true; }
          if (aura.Def.NotShowOnSelected) { return false; };
          if (aura.Def.HideOnNotSelected) {
            if (___HUD.SelectedActor != null) {
              if (___HUD.SelectedActor.GUID == ___owner.GUID) {
                return true;
              }
            }
            return false;
          } else {
            return true;
          }
        }
        if (aura.source != null) {
          if (aura.Def.isSpining != spinning) { return false; }
          Weapon weapon = aura.source as Weapon;
          if ((weapon == null) || (aura.Def.Id != "AMS")) {
            if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.ShowAll) { return true; }
            if (aura.Def.NotShowOnSelected) { return false; };
            if (aura.Def.HideOnNotSelected) {
              if (___HUD.SelectedActor != null) {
                if (___HUD.SelectedActor.GUID == ___owner.GUID) {
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
              if (___HUD.SelectedActor != null) {
                if (___HUD.SelectedActor.GUID == ___owner.GUID) {
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
    public static bool Prefix(CombatAuraReticle __instance, ref ButtonState __result, ref AbstractActor ___owner, ref CombatHUD ___HUD) {
      try {
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { __result = ButtonState.Disabled;  return false; }
        __result = __instance.isAuraVisible(aura,___owner, ___HUD, false)? ButtonState.Enabled : ButtonState.Disabled;
        return false;
      }catch(Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
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
    public static PropertyInfo pAuraRangeScaledObject;
    public static bool Prepare() {
      pAuraRangeScaledObject = typeof(CombatAuraReticle).GetProperty("auraRangeScaledObject", BindingFlags.NonPublic | BindingFlags.Instance);
      return true;
    }
    public static GameObject auraRangeScaledObject(this CombatAuraReticle instance) {
      return (GameObject)pAuraRangeScaledObject.GetValue(instance);
    }
    public static bool Prefix(CombatAuraReticle __instance, ButtonState auraProjectionState, ref AbstractActor ___owner, ref float ___currentAuraRange) {
      try {
        GameObject auraRangeScaledObject = __instance.auraRangeScaledObject();
        if (auraProjectionState == ButtonState.Disabled) {
          auraRangeScaledObject.SetActive(false);
          return false;
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
          if (!Mathf.Approximately(___currentAuraRange, b)) {
            auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          }
          ___currentAuraRange = b;
          return false;
        }
      }catch(Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshActiveProbeRange")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshActiveProbeRange {
    public static PropertyInfo pactiveProbeRangeScaledObject;
    public static bool Prepare() {
      pactiveProbeRangeScaledObject = typeof(CombatAuraReticle).GetProperty("activeProbeRangeScaledObject", BindingFlags.NonPublic | BindingFlags.Instance);
      return true;
    }
    public static GameObject activeProbeRangeScaledObject(this CombatAuraReticle instance) {
      return (GameObject)pactiveProbeRangeScaledObject.GetValue(instance);
    }
    public static bool Prefix(CombatAuraReticle __instance, bool showActiveProbe, ref AbstractActor ___owner, ref float ___currentAPRange) {
      try {
        GameObject activeProbeRangeScaledObject = __instance.activeProbeRangeScaledObject();
        if (showActiveProbe == false) {
          activeProbeRangeScaledObject.SetActive(false);
          return false;
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
          if (!Mathf.Approximately(___currentAPRange, b)) {
            activeProbeRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
          }
          ___currentAPRange = b;
          return false;
        }
      } catch (Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("RefreshActiveProbeState")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_RefreshActiveProbeState {
    public static Vector3 dbgPos = Vector3.zero;
    public static bool Prefix(CombatAuraReticle __instance, ref AbstractActor ___owner, ref float ___currentAuraRange, ref CombatHUD ___HUD, ref Transform ___thisTransform, ref bool __result) {
      try {
        //if (CombatHUD_Update_HideReticlesHotKey.hideReticles == AuraShowState.HideAll) { __result = false; return false; };
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { return true; };
        __result = __instance.isAuraVisible(aura,___owner,___HUD,true);
        return false;
      }catch(Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
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
    public static bool Prefix(CombatAuraReticle __instance, bool isBright, ref AbstractActor ___owner, ref CombatHUD ___HUD, ref bool ___currentAPIsBright) {
      try {
        AuraBubble aura = __instance.AuraBubble();
        if (aura == null) { return true; }
        if (isBright == false) { return false; }
        isBright = ___HUD.SelectedActor != null && ___HUD.SelectionHandler.ActiveState is SelectionStateMoveBase;
        if (isBright) {
          __instance.activeProbeDecal.DecalMaterial = __instance.activeProbeMatBright;
          //this.apSpinAnim.DORestartById("spin");
        } else {
          __instance.activeProbeDecal.DecalMaterial = __instance.activeProbeMatDim;
          //this.apSpinAnim.DOPause();
        }
        if(___currentAPIsBright != aura.Def.isSpining) {
          ___currentAPIsBright = aura.Def.isSpining;
          if (___currentAPIsBright) {
            __instance.apSpinAnim.DORestartById("spin");
          } else {
            __instance.apSpinAnim.DOPause();
          }
        }
        return false;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("UpdatePosition")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_UpdatePosition {
    public static Vector3 dbgPos = Vector3.zero;
    public static void Postfix(CombatAuraReticle __instance, ref AbstractActor ___owner, ref float ___currentAuraRange, ref CombatHUD ___HUD, ref Transform ___thisTransform) {
      //if(___HUD.SelectedActor != null && ___HUD.SelectionHandler.ActiveState is SelectionStateMoveBase && ___owner.GUID == ___HUD.SelectedActor.GUID) {
      //AuraPreviewRecord preview = 
      //}
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