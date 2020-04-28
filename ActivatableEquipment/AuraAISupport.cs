using BattleTech;
using CustomActivatableEquipment;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustAmmoCategoriesPatches {
  public static class AreAnyHostilesInWeaponRangeNode_Tick {
    public static bool Prefix(BehaviorNode __instance, ref BehaviorTreeResults __result, ref BehaviorTree ___tree, ref AbstractActor ___unit) {
      try {
        List<AbstractActor> allAlliesOf = ___unit.Combat.GetAllAlliesOf(___unit);
        AuraBubble sensors = ___unit.sensorAura();
        for (int index1 = 0; index1 < ___tree.enemyUnits.Count; ++index1) {
          ICombatant enemyUnit = ___tree.enemyUnits[index1];
          AbstractActor abstractActor = enemyUnit as AbstractActor;
          float magnitude = (enemyUnit.CurrentPosition - ___unit.CurrentPosition).magnitude;
          if (AIUtil.UnitHasVisibilityToTargetFromPosition(___unit, enemyUnit, ___unit.CurrentPosition, allAlliesOf)) {
            if (___unit.CanEngageTarget(enemyUnit) || ___unit.CanDFATargetFromPosition(enemyUnit, ___unit.CurrentPosition)) {
              __result = new BehaviorTreeResults(BehaviorNodeState.Success);
              return false;
            }
            if ((double)magnitude <= (double)___unit.MaxWalkDistance) {
              __result = new BehaviorTreeResults(BehaviorNodeState.Success);
              return false;
            }
            if (abstractActor != null && abstractActor.IsGhosted) {
              float num = Mathf.Lerp(___unit.MaxWalkDistance, ___unit.MaxSprintDistance, ___unit.BehaviorTree.GetBehaviorVariableValue(BehaviorVariableName.Float_SignalInWeapRngWhenEnemyGhostedWithinMoveDistance).FloatVal);
              float range = sensors.collider.radius;
              if ((double)Vector3.Distance(___unit.CurrentPosition, abstractActor.CurrentPosition) - (double)range >= (double)num) {
                continue;
              }
            }
            for (int index2 = 0; index2 < ___unit.Weapons.Count; ++index2) {
              Weapon weapon = ___unit.Weapons[index2];
              if (weapon.CanFire && (double)weapon.MaxRange >= (double)magnitude) {
                __result = new BehaviorTreeResults(BehaviorNodeState.Success);
                return false;
              }
            }
          }
        }
        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
        return false;
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n");
        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
        return false;
      }
    }
  }
  [HarmonyPatch(typeof(AITeam))]
  [HarmonyPatch("GetUnitThatCanReachECM")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(List<AbstractActor>) })]
  public static class AITeam_GetUnitThatCanReachECM {
    private static MethodInfo mCanEntireEnemyTeamBeGhosted;
    private static MethodInfo mGetBehaviorVariableValue;
    private delegate bool CanEntireEnemyTeamBeGhostedDelegate(AITeam instance);
    private delegate BehaviorVariableValue GetBehaviorVariableValueDelegate(BehaviorTree instance, BehaviorVariableName name);
    private static CanEntireEnemyTeamBeGhostedDelegate CanEntireEnemyTeamBeGhostedInvoker = null;
    private static GetBehaviorVariableValueDelegate GetBehaviorVariableValueInvoker = null;
    public static bool Prepare() {
      mCanEntireEnemyTeamBeGhosted = typeof(AITeam).GetMethod("CanEntireEnemyTeamBeGhosted", BindingFlags.NonPublic | BindingFlags.Instance);
      if(mCanEntireEnemyTeamBeGhosted == null) {
        Log.LogWrite("Can't find AITeam.CanEntireEnemyTeamBeGhosted\n",true);
        return false;
      }
      mGetBehaviorVariableValue = typeof(BehaviorTree).GetMethod("GetBehaviorVariableValue", BindingFlags.NonPublic | BindingFlags.Instance);
      if (mGetBehaviorVariableValue == null) {
        Log.LogWrite("Can't find BehaviorTree.GetBehaviorVariableValue\n",true);
        return false;
      }
      var dm = new DynamicMethod("CAECanEntireEnemyTeamBeGhosted", typeof(bool), new Type[] { typeof(AITeam) }, typeof(AITeam));
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Call, mCanEntireEnemyTeamBeGhosted);
      gen.Emit(OpCodes.Ret);
      CanEntireEnemyTeamBeGhostedInvoker = (CanEntireEnemyTeamBeGhostedDelegate)dm.CreateDelegate(typeof(CanEntireEnemyTeamBeGhostedDelegate));
      var dm1 = new DynamicMethod("CAEGetBehaviorVariableValue", typeof(BehaviorVariableValue), new Type[] { typeof(BehaviorTree), typeof(BehaviorVariableName) }, typeof(BehaviorTree));
      var gen1 = dm1.GetILGenerator();
      gen1.Emit(OpCodes.Ldarg_0);
      gen1.Emit(OpCodes.Ldarg_1);
      gen1.Emit(OpCodes.Call, mGetBehaviorVariableValue);
      gen1.Emit(OpCodes.Ret);
      GetBehaviorVariableValueInvoker = (GetBehaviorVariableValueDelegate)dm1.CreateDelegate(typeof(GetBehaviorVariableValueDelegate));
      return true;
    }
    public static bool CanEntireEnemyTeamBeGhosted(this AITeam instance) {
      return CanEntireEnemyTeamBeGhostedInvoker(instance);
    }
    public static BehaviorVariableValue GetBehaviorVariableValue(this BehaviorTree instance, BehaviorVariableName name) {
      return GetBehaviorVariableValueInvoker(instance,name);
    }
    public static bool Prefix(AITeam __instance, List<AbstractActor> unusedUnits, ref AbstractActor __result) {
      try {
        if (!__instance.CanEntireEnemyTeamBeGhosted()) {
          __result = null;
          return false;
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
        return false;
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
        __result = null;
        return false;
      }
    }
  }

}