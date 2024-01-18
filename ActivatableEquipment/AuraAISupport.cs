using BattleTech;
using CustomActivatableEquipment;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustAmmoCategoriesPatches {
  public static class AreAnyHostilesInWeaponRangeNode_Tick {
    public static void Prefix(ref bool __runOriginal, BehaviorNode __instance, ref BehaviorTreeResults __result) {
      try {
        if (!__runOriginal) { return; }
        List<AbstractActor> allAlliesOf = __instance.unit.Combat.GetAllAlliesOf(__instance.unit);
        AuraBubble sensors = __instance.unit.sensorAura();
        for (int index1 = 0; index1 < __instance.tree.enemyUnits.Count; ++index1) {
          ICombatant enemyUnit = __instance.tree.enemyUnits[index1];
          AbstractActor abstractActor = enemyUnit as AbstractActor;
          float magnitude = (enemyUnit.CurrentPosition - __instance.unit.CurrentPosition).magnitude;
          if (AIUtil.UnitHasVisibilityToTargetFromPosition(__instance.unit, enemyUnit, __instance.unit.CurrentPosition, allAlliesOf)) {
            if (__instance.unit.CanEngageTarget(enemyUnit) || __instance.unit.CanDFATargetFromPosition(enemyUnit, __instance.unit.CurrentPosition)) {
              __result = new BehaviorTreeResults(BehaviorNodeState.Success);
              __runOriginal = false; return;
            }
            if (magnitude <= __instance.unit.MaxWalkDistance) {
              __result = new BehaviorTreeResults(BehaviorNodeState.Success);
              __runOriginal = false; return;
            }
            if (abstractActor != null && abstractActor.IsGhosted) {
              float num = Mathf.Lerp(__instance.unit.MaxWalkDistance, __instance.unit.MaxSprintDistance, __instance.unit.BehaviorTree.GetBehaviorVariableValue(BehaviorVariableName.Float_SignalInWeapRngWhenEnemyGhostedWithinMoveDistance).FloatVal);
              float range = sensors.collider.radius;
              if ((double)Vector3.Distance(__instance.unit.CurrentPosition, abstractActor.CurrentPosition) - (double)range >= (double)num) {
                continue;
              }
            }
            for (int index2 = 0; index2 < __instance.unit.Weapons.Count; ++index2) {
              Weapon weapon = __instance.unit.Weapons[index2];
              if (weapon.CanFire && weapon.MaxRange >= magnitude) {
                __result = new BehaviorTreeResults(BehaviorNodeState.Success);
                __runOriginal = false; return;
              }
            }
          }
        }
        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
        __runOriginal = false; return;
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
        AbstractActor.logger.LogException(e);
        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
        __runOriginal = false; return;
      }
    }
  }
  [HarmonyPatch(typeof(AITeam))]
  [HarmonyPatch("GetUnitThatCanReachECM")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(List<AbstractActor>) })]
  public static class AITeam_GetUnitThatCanReachECM {
    public static bool Prepare() { return true; }
    public static void Prefix(ref bool __runOriginal, AITeam __instance, List<AbstractActor> unusedUnits, ref AbstractActor __result) {
      try {
        if (!__runOriginal) { return; }
        if (!__instance.CanEntireEnemyTeamBeGhosted()) {
          __result = null;
          __runOriginal = false; return;
        }
        AbstractActor result = (AbstractActor)null;
        float minDistance = float.MaxValue;
        for (int index1 = 0; index1 < unusedUnits.Count; ++index1) {
          AbstractActor unusedUnit = unusedUnits[index1];
          List<AbstractActor> enemies = AIUtil.HostilesToUnit(unusedUnit);
          AuraBubble sensors = unusedUnit.sensorAura();
          if (sensors == null) { continue; };
          for (int index2 = 0; index2 < enemies.Count; ++index2) {
            AbstractActor enemy = enemies[index2];
            if (enemy.HasECMAbilityInstalled) {
              float floatVal = unusedUnit.BehaviorTree.GetBehaviorVariableValue(BehaviorVariableName.Float_SignalInWeapRngWhenEnemyGhostedWithinMoveDistance).FloatVal;
              float maxMoveDist = Mathf.Lerp(unusedUnit.MaxWalkDistance, unusedUnit.MaxSprintDistance, floatVal);
              float range = sensors.collider.radius;
              float needMoveDist = Vector3.Distance(unusedUnit.CurrentPosition, enemy.CurrentPosition) - range;
              if ((double)needMoveDist <= (double)maxMoveDist && (double)needMoveDist < (double)minDistance) {
                result = unusedUnit;
                minDistance = needMoveDist;
              }
            }
          }
        }
        __result = result;
        __runOriginal = false; return;
      } catch (Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
        AbstractActor.logger.LogException(e);
        __result = null;
        __runOriginal = false; return;
      }
    }
  }

}