using BattleTech.UI;
using Harmony;
using CustomActivatableEquipment;
using System.Reflection;
using BattleTech;
using UnityEngine;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(CombatAuraReticle))]
  [HarmonyPatch("DesiredAuraProjectionState")]
  [HarmonyPatch(MethodType.Getter)]
  [HarmonyPriority(Priority.Last)]
  public static class CombatAuraReticle_DesiredAuraProjectionState {
    private static FieldInfo fOwner;
    public static bool Prepare() {
      fOwner = typeof(CombatAuraReticle).GetField("owner",BindingFlags.Instance|BindingFlags.NonPublic);
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
    public static bool Prefix(CombatAuraReticle __instance,ref ButtonState __result, ref AbstractActor ___owner) {
      if ((___owner.IsVisibleToPlayer() == false) || (___owner.IsOperational == false)) {
        __result = ButtonState.Disabled; return false;
      }
      AuraBubble mainSensorsBubble = __instance.MainSensors();
      //Log.LogWrite("CombatAuraReticle.DesiredAuraProjectionState " + (mainSensorsBubble == null ? "null" : mainSensorsBubble.collider.radius.ToString()) + "\n");
      if (mainSensorsBubble != null) {
        __result = ButtonState.Enabled; return false;
      }
      AuraBubble aura = __instance.AuraBubble();
      if(aura != null) {
        __result = ButtonState.Enabled; return false;
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
      GameObject auraRangeScaledObject = __instance.auraRangeScaledObject();
      if (auraProjectionState == ButtonState.Disabled) {
        auraRangeScaledObject.SetActive(false);
        return false;
      }
      AuraBubble mainSensorsBubble = __instance.MainSensors();
      //Log.LogWrite("CombatAuraReticle.RefreshAuraRange " + (mainSensorsBubble == null ? "null" : mainSensorsBubble.collider.radius.ToString()) + "\n");
      if (mainSensorsBubble != null) {
        auraRangeScaledObject.SetActive(true);
        float b = mainSensorsBubble.collider.radius;
        if (!Mathf.Approximately(___currentAuraRange, b)) {
          auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
        }
        ___currentAuraRange = b;
        return false;
      }
      AuraBubble auraBubble = __instance.AuraBubble();
      if(auraBubble != null) {
        auraRangeScaledObject.SetActive(true);
        float b = auraBubble.collider.radius;
        if (!Mathf.Approximately(___currentAuraRange, b)) {
          auraRangeScaledObject.transform.localScale = new Vector3(b * 2f, 1f, b * 2f);
        }
        ___currentAuraRange = b;
        return false;
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
    public static void Postfix(CombatAuraReticle __instance, ref AbstractActor ___owner, ref float ___currentAuraRange,ref CombatHUD ___HUD,ref Transform ___thisTransform) {
      //if(___HUD.SelectedActor != null && ___HUD.SelectionHandler.ActiveState is SelectionStateMoveBase && ___owner.GUID == ___HUD.SelectedActor.GUID) {
        //AuraPreviewRecord preview = 
      //}
    }
  }
  //public 
}