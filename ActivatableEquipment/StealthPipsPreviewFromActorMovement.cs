using BattleTech;
using BattleTech.UI;
using CustomActivatableEquipment;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("StealthPipsPreviewFromActorMovement")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AbstractActor_StealthPipsPreviewFromActorMovement_Prefix {
    private static int CacheTurn = -1;
    private static int CachePhase = -1;
    private static Dictionary<AbstractActor, Dictionary<AbstractActor, int>> stealthCache = new Dictionary<AbstractActor, Dictionary<AbstractActor, int>>();
    private static Dictionary<AbstractActor, Dictionary<AbstractActor, Dictionary<Vector3, int>>> stealthCacheEx = new Dictionary<AbstractActor, Dictionary<AbstractActor, Dictionary<Vector3, int>>>();
    public static void ClearCache(int turn, int phase) {
      stealthCacheEx = new Dictionary<AbstractActor, Dictionary<AbstractActor, Dictionary<Vector3, int>>>();
      stealthCache = new Dictionary<AbstractActor, Dictionary<AbstractActor, int>>();
      CacheTurn = turn;
      CachePhase = phase;
    }
    public static bool isInCache(this AbstractActor instance, AbstractActor movingActor, Vector3 previewPos) {
      if (stealthCache.ContainsKey(instance) == false) { return false; }
      if (stealthCache[instance].ContainsKey(movingActor) == false) { return false; }
      return true;
    }
    public static bool isInCacheEx(this AbstractActor instance, AbstractActor movingActor, Vector3 previewPos) {
      if (stealthCacheEx.ContainsKey(instance) == false) { return false; }
      if (stealthCacheEx[instance].ContainsKey(movingActor) == false) { return false; }
      if (stealthCacheEx[instance][movingActor].ContainsKey(previewPos) == false) { return false; }
      return true;
    }
    public static int getFromCache(this AbstractActor instance, AbstractActor movingActor, Vector3 previewPos) {
      return stealthCache[instance][movingActor];
    }
    public static void setToCache(this AbstractActor instance, AbstractActor movingActor, Vector3 previewPos, int result) {
      if (stealthCache.ContainsKey(instance) == false) { stealthCache.Add(instance, new Dictionary<AbstractActor, int>()); };
      if (stealthCache[instance].ContainsKey(movingActor) == false) { stealthCache[instance].Add(movingActor, result); } else { 
        stealthCache[instance][movingActor] = result;
      }
    }
    public static bool Prefix(AbstractActor __instance, AbstractActor movingActor, Vector3 previewPos, ref int __result) {
      __result = 0;
      AuraPreviewRecord preview = movingActor.getPreviewCache(previewPos);
      __result = preview.getStealthPipsPreview(__instance);
      return false;
      //if (SelectionStateMove_ProcessMousePos.isNeedCache == false) { return true; }
      /*if ((CacheTurn != __instance.Combat.TurnDirector.CurrentRound) || (CachePhase != __instance.Combat.TurnDirector.CurrentPhase)) {
        ClearCache(__instance.Combat.TurnDirector.CurrentRound, __instance.Combat.TurnDirector.CurrentPhase);
      }
      if (__instance.isInCache(movingActor, previewPos)) {
        __result = __instance.getFromCache(movingActor, previewPos);
        return false;
      }
      return true;*/
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("StealthPipsPreviewFromActorMovement")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3) })]
  public static class AbstractActor_StealthPipsPreviewFromActorMovement_Postfix {
    public static void Postfix(AbstractActor __instance, AbstractActor movingActor, Vector3 previewPos, ref int __result) {
      //__instance.setToCache(movingActor, previewPos, __result);
    }
  }

  [HarmonyPatch(typeof(CombatHUDNumFlagHex))]
  [HarmonyPatch("UpdateStealthState")]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDNumFlagHex_UpdateStealthState {
      public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator ilGenerator) {
          return codeInstructions.MethodReplacer(
              typeof(CombatHUDStealthBarPips).GetMethod(nameof(CombatHUDStealthBarPips.UpdateStealth)),
              typeof(CombatHUDNumFlagHex_UpdateStealthState).GetMethod(nameof(UpdateStealNoop), AccessTools.all));
      }

      private static void UpdateStealNoop(CombatHUDStealthBarPips StealthDisplay, float current, float projected, bool force) {
            // no op.
      }
  }

  [HarmonyPatch(typeof(CombatHUDStatusPanel))]
  [HarmonyPatch("PreviewDesignMasks")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(List<WayPoint>) })]
  [HarmonyPriority(Priority.First)]
  public static class CombatHUDStatusPanel_PreviewDesignMasks {
    public static bool Prefix(CombatHUDStatusPanel __instance,List<WayPoint> waypoints) {
      if (waypoints.Count == 0) { return false; }
      return true;
    }
  }
  [HarmonyPatch(typeof(SelectionStateMoveBase))]
  [HarmonyPatch("ProcessMousePos")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Vector3) })]
  [HarmonyPriority(Priority.First)]
  public static class SelectionStateMoveBase_ProcessMousePos {
    public static Vector3 prevResultDestination = Vector3.zero;
    public static bool Prefix(SelectionStateMoveBase __instance, Vector3 worldPos) {
      /*SelectionStateJump jump = __instance as SelectionStateJump;
      if (jump != null) { return true; };
      if (__instance.SelectedActor == null) { return true; }
      if (__instance.SelectedActor.Pathing == null) { return true; }
      if (__instance.HasDestination) { return true; };
      Vector3 resultDestination = __instance.SelectedActor.Pathing.ResultDestination;
      if(prevResultDestination != resultDestination) {
        prevResultDestination = resultDestination;
        return true;
      }
      return false;*/
      return true;
    }
  }
  [HarmonyPatch(typeof(SelectionStateMove))]
  [HarmonyPatch("ProcessMousePos")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Vector3) })]
  [HarmonyPriority(Priority.First)]
  public static class SelectionStateMove_ProcessMousePos {
    public static bool isNeedCache = false;
    public static bool Prefix(SelectionStateMoveBase __instance, Vector3 worldPos) {
      //SelectionStateMove_ProcessMousePos.isNeedCache = true;
      return true;
    }
    public static void Postfix(SelectionStateMoveBase __instance, Vector3 worldPos) {
      //SelectionStateMove_ProcessMousePos.isNeedCache = false;
    }
  }
  [HarmonyPatch(typeof(CombatMovementReticle))]
  [HarmonyPatch("UpdateReticle")]
  [HarmonyPatch(MethodType.Normal)]
#if BT1_8
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
#else
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool) })]
#endif
  [HarmonyPriority(Priority.First)]
  public static class CombatMovementReticle_UpdateReticle {
    public static Vector3 prevResultDestination = Vector3.zero;
    public static MethodInfo mUpdateStatusPreview;
    public static MethodInfo mHideStatusPreview;
    public static bool Prepare() {
      mUpdateStatusPreview = typeof(CombatMovementReticle).GetMethod("UpdateStatusPreview", BindingFlags.Instance | BindingFlags.NonPublic);
      mHideStatusPreview = typeof(CombatMovementReticle).GetMethod("HideStatusPreview", BindingFlags.Instance | BindingFlags.NonPublic);
      return true;
    }
    public static void UpdateStatusPreview(this CombatMovementReticle instance, AbstractActor actor, Vector3 worldPos, MoveType moveType) {
      mUpdateStatusPreview.Invoke(instance, new object[] { actor, worldPos, moveType });
    }
    public static void HideStatusPreview(this CombatMovementReticle instance) {
      mHideStatusPreview.Invoke(instance, new object[] { });
    }
    public static bool Prefix(CombatMovementReticle __instance, AbstractActor actor, Vector3 mousePos, bool isJump, bool isMelee, bool isTargetLocked) {
      //if (isJump) { return true; }
      return true;
      /*if (actor == null || actor.Pathing == null || (isJump && actor.JumpPathing == null)) {
        return true;
      };
      Vector3 resultDestination = actor.Pathing.ResultDestination;
      if (resultDestination == prevResultDestination) {
        if (!isMelee) {
          __instance.UpdateStatusPreview(actor, resultDestination + actor.HighestLOSPosition, actor.Pathing.MoveType);
        } else {
          __instance.HideStatusPreview();
        }
        return false;
      } else {
        prevResultDestination = resultDestination;
        return true;
      }*/
    }

  }
}