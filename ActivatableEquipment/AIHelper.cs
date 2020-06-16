using BattleTech;
using CustomActivatableEquipment;
using Harmony;
using System;
using CustomComponents;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using CustomActivatableEquipment.DamageHelpers;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("OnNewRound")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class AbstractActor_OnNewRound {
    public static bool Prefix(AbstractActor __instance) {
      Log.LogWrite("AbstractActor.OnNewRound(" + __instance.DisplayName + ":" + __instance.GUID + ")\n");
      try {
        __instance.Combat.commitDamage();
      }catch(Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
        if (__instance.IsDead) { return true; };
      CAEAIHelper.AIActivatableProc(__instance);
      __instance.CommitCAEDamageData();
      return true;
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("OnActivationEnd")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(string), typeof(int) })]
  public static class AbstractActor_OnActivationEnd {
    public static bool Prefix(AbstractActor __instance) {
      Log.LogWrite("AbstractActor.OnActivationEnd(" + __instance.DisplayName + ":" + __instance.GUID + ")\n");
      CAEAIHelper.AIActivatableProc(__instance);
      return true;
    }
  }

}

namespace CustomActivatableEquipment {
  public static class CAEAIHelper {
    public static readonly string AIRollPassStatName = "CAEAIFailRollPassed";
    public static bool isAIRollPassed(this MechComponent component) {
      if(Core.checkExistance(component.StatCollection, AIRollPassStatName) == false) {
        component.StatCollection.AddStatistic<bool>(AIRollPassStatName,false);
      }
      return component.StatCollection.GetStatistic(AIRollPassStatName).Value<bool>();
    }
    public static bool setAIRollPassed(this MechComponent component,bool val) {
      if (Core.checkExistance(component.StatCollection, AIRollPassStatName) == false) {
        component.StatCollection.AddStatistic<bool>(AIRollPassStatName, false);
      }
      return component.StatCollection.Set<bool>(AIRollPassStatName, val);
    }
    public static bool AICheatRoll(this AbstractActor actor) {
      if (actor.isAIUnit() == false) { return false; }
      float roll = Random.Range(0f, 1f);
      if (roll < Core.Settings.AIActivatableCheating) { return true; }
      return false;
    }
    public static bool isAIUnit(this AbstractActor actor) {
      return actor.TeamId != actor.Combat.LocalPlayerTeamGuid;
    }
    public static void AIActivatableProc(AbstractActor unit) {
      try {
        if (unit.IsDead) { return; }
        if (unit.TeamId == unit.Combat.LocalPlayerTeamGuid) {
          Log.LogWrite(" not AI\n");
          return;
        }
        HashSet<MechComponent> extreamlyUsefull = new HashSet<MechComponent>();
        HashSet<MechComponent> Usefull = new HashSet<MechComponent>();
        var AITime = System.Diagnostics.Stopwatch.StartNew();
        // the code that you want to measure comes here
        var elapsedMs = AITime.ElapsedMilliseconds;
        int isVisibleTargets = 0;
        float offenceCoeff = 0f;
        float deffenceCoeff = 0f;
        unit.TargetsCalc(ref isVisibleTargets, ref offenceCoeff, ref deffenceCoeff);
        Mech mech = unit as Mech;
        float heatCoeff = 0f;
        float overheatCoeff = 0f;
        if (mech != null) {
          heatCoeff = mech.CurrentHeat / mech.OverheatLevel;
          overheatCoeff = mech.CurrentHeatAsRatio;
        }
        Log.LogWrite("AI activatable coeffs:" + unit.DisplayName + ":" + unit.GUID + "\n");
        Log.LogWrite(" visible targets:" + isVisibleTargets + "\n");
        Log.LogWrite(" offenceCoeff:" + offenceCoeff + "\n");
        Log.LogWrite(" deffenceCoeff:" + deffenceCoeff + "\n");
        Log.LogWrite(" heatCoeff:" + heatCoeff + "\n");
        Log.LogWrite(" overheatCoeff:" + overheatCoeff + "\n");
        bool isAnyLocationExposed = unit.IsAnyStructureExposed;
        Log.LogWrite(" isAnyLocationExposed:" + isAnyLocationExposed + "\n");
        Log.LogWrite("Components:\n");
        foreach (MechComponent component in unit.allComponents) {
          Log.LogWrite(" " + component.defId + "\n");
          if (component.CanBeActivated() == false) {
            Log.LogWrite("  can't be activated\n");
            continue;
          };
          if (component.isSensors()) {
            Log.LogWrite("  sensors\n");
            if (isVisibleTargets == 0) {
              if (isAnyLocationExposed == false) {
                Log.LogWrite("  no visible targets. Sensors are extreamly usefull to detect them\n");
                extreamlyUsefull.Add(component);
              }
            } else
            if (isVisibleTargets < 2) {
              if (isAnyLocationExposed == false) {
                Log.LogWrite("  low visible visible targets count. Sensors are usefull to detect additional targets\n");
                Usefull.Add(component);
              }
            }
          }
          if (component.isCool()) {
            if (heatCoeff > Core.Settings.AIHeatCoeffCoeff) {
              Log.LogWrite("  cooling component and i'm heated. Usefull\n");
              Usefull.Add(component);
            }
            if (overheatCoeff > Core.Settings.AIOverheatCoeffCoeff) {
              Log.LogWrite("  cooling component and i'm close to overheat. Very usefull\n");
              extreamlyUsefull.Add(component);
            }
          }
          if (component.isSpeed()) {
            if (isAnyLocationExposed) {
              Log.LogWrite("  speed and i'm damaged. Need to flee. Very usefull\n");
              extreamlyUsefull.Add(component);
            } else
            if (isVisibleTargets == 0) {
              Log.LogWrite("  speed and no visible targets. Need to find usefull\n");
              Usefull.Add(component);
            }
          }
          if (component.isOffence()) {
            if ((isVisibleTargets > 1) && (isAnyLocationExposed)) {
              Log.LogWrite("  offence and i see target and damaged. Very usefull\n");
              extreamlyUsefull.Add(component);
            } else if (offenceCoeff > Core.Settings.AIOffenceUsefullCoeff) {
              Log.LogWrite("  offence and i can make much damage. Very usefull\n");
              extreamlyUsefull.Add(component);
            } else if (offenceCoeff > Core.Epsilon) {
              Log.LogWrite("  offence and i can make some damage. Usefull\n");
              Usefull.Add(component);
            }
          }
          if (component.isDefence()) {
            if ((isVisibleTargets > 1) && (isAnyLocationExposed)) {
              Log.LogWrite("  defence and i see target and damaged. Very usefull\n");
              extreamlyUsefull.Add(component);
            } else if (deffenceCoeff > Core.Settings.AIDefenceUsefullCoeff) {
              Log.LogWrite("  defence and i can suffer much damage. Very usefull\n");
              extreamlyUsefull.Add(component);
            } else if (deffenceCoeff > Core.Epsilon) {
              Log.LogWrite("  defence and i suffer make some damage. Usefull\n");
              Usefull.Add(component);
            }
          }
        }
        HashSet<MechComponent> stepDownComponents = new HashSet<MechComponent>();
        foreach (MechComponent component in extreamlyUsefull) {
          if (component.isHeat()) {
            if (heatCoeff > Core.Settings.AIHeatCoeffCoeff) {
              Log.LogWrite(component.defId + " very usefull. But i'm heated\n");
              stepDownComponents.Add(component);
            } else
            if (overheatCoeff > Core.Settings.AIOverheatCoeffCoeff) {
              Log.LogWrite(component.defId + " very usefull. But i'm overheated\n");
              stepDownComponents.Add(component);
            }
          }
        }
        foreach (MechComponent component in stepDownComponents) {
          extreamlyUsefull.Remove(component);
          Usefull.Add(component);
        }
        Log.LogWrite("Activation:\n");
        foreach (MechComponent component in unit.allComponents) {
          Log.LogWrite(" " + component.defId + "\n");
          bool needToBeActivated = false;
          if (extreamlyUsefull.Contains(component)) {
            Log.LogWrite("  very usefull\n");
            if (component.isFailDanger() == false) {
              Log.LogWrite("  not danger\n");
              needToBeActivated = true;
            } else {
              Log.LogWrite("  FailChance:" + component.FailChance() + "\n");
              if ((component.isDamaged() == false)||(isAnyLocationExposed == true)) {
                if (component.FailChance() < Core.Settings.AIComponentExtreamlyUsefulModifyer) {
                  needToBeActivated = true;
                  Log.LogWrite("  not big danger. Can be activated\n");
                }
              } else {
                Log.LogWrite("  component is damaged and have no exposed locations. Danger level increase\n");
                if (component.FailChance() < Core.Settings.AIComponentUsefullModifyer) {
                  needToBeActivated = true;
                  Log.LogWrite("  not big danger. Can be activated\n");
                }

              }
            }
          }
          if (Usefull.Contains(component)) {
            Log.LogWrite("  usefull\n");
            if (component.isFailDanger() == false) {
              Log.LogWrite("  not danger\n");
              needToBeActivated = true;
            } else {
              Log.LogWrite("  FailChance:" + component.FailChance() + "\n");
              if ((component.isDamaged() == false) || (isAnyLocationExposed == true)) {
                if (component.FailChance() < Core.Settings.AIComponentUsefullModifyer) {
                  needToBeActivated = true;
                  Log.LogWrite("  not big danger. Can be activated\n");
                }
              } else {
                Log.LogWrite("  component is damaged and have no exposed locations. Danger level increase will not activate.\n");
              }
            }
          }
          if (needToBeActivated) {
            if (ActivatableComponent.isComponentActivated(component)) {
              Log.LogWrite("  already active\n");
              if (unit.AICheatRoll()) {
                bool isSuccess = ActivatableComponent.rollFail(component, false, true);
                if (isSuccess == false) {
                  Log.LogWrite("  deactivating due to high possible fail\n");
                  ActivatableComponent.deactivateComponent(component);
                } else {
                  component.setAIRollPassed(true);
                }
              }
            } else {
              Log.LogWrite("  activating\n");
              if (unit.AICheatRoll()) {
                bool isSuccess = ActivatableComponent.rollFail(component, false, true);
                if (isSuccess == false) {
                  Log.LogWrite("  not activate due to high possible fail\n");
                } else {
                  component.setAIRollPassed(true);
                  ActivatableComponent.activateComponent(component, false, false);
                }
              } else {
                ActivatableComponent.activateComponent(component, false, false);
              }
            }
          } else {
            if (ActivatableComponent.isComponentActivated(component)) {
              Log.LogWrite("  not active\n");
            } else {
              Log.LogWrite("  deactivating\n");
              ActivatableComponent.deactivateComponent(component);
            }
          }
        }
        AITime.Stop();
        Log.LogWrite("AI activatable time:" + AITime.ElapsedMilliseconds + " msec\n");
      } catch (Exception e) {
        Log.LogWrite("AI activatable exception:" + e.ToString() + "\n", true);
      }

    }
    public static void TargetsCalc(this AbstractActor unit, ref int isVisibleTargets, ref float offenceCoeff, ref float deffenceCoeff) {
      isVisibleTargets = 0;
      float offenceCoeffUp = 0f;
      float offenceCoeffDown = 0f;
      float defenceCoeffUp = 0f;
      float defenceCoeffDown = 0f;
      foreach (var target in unit.GetVisibleEnemyUnits()) {
        isVisibleTargets += 1;
        float distance = Vector3.Distance(unit.CurrentPosition, target.CurrentPosition);
        foreach (Weapon weapon in unit.Weapons) {
          float toHit = 0f;
          if (weapon.CanFire == false) { continue; }
          if (weapon.WillFireAtTargetFromPosition(target, unit.CurrentPosition) == true) {
            toHit = weapon.GetToHitFromPosition(target, 1, unit.CurrentPosition, target.CurrentPosition, true, target.IsEvasive, false);
          }
          if (toHit < Core.Epsilon) { continue; };
          offenceCoeffUp += (toHit * weapon.DamagePerShot * weapon.ShotsWhenFired);
        }
        foreach (Weapon weapon in target.Weapons) {
          float toHit = 0f;
          if (weapon.CanFire == false) { continue; }
          if (weapon.WillFireAtTargetFromPosition(unit, target.CurrentPosition) == true) {
            toHit = weapon.GetToHitFromPosition(unit, 1, target.CurrentPosition, unit.CurrentPosition, true, unit.IsEvasive, false);
          }
          if (toHit < Core.Epsilon) { continue; };
          defenceCoeffUp += (toHit * weapon.DamagePerShot * weapon.ShotsWhenFired);
        }
        offenceCoeffDown += (target.SummaryArmorCurrent);
      }
      defenceCoeffDown = unit.SummaryArmorCurrent;
      if (isVisibleTargets == 0) {
        offenceCoeff = 0f;
        deffenceCoeff = 0f;
      } else {
        if (offenceCoeffDown > Core.Epsilon) { offenceCoeff = offenceCoeffUp / offenceCoeffDown; } else { offenceCoeff = 1f; }
        if (defenceCoeffDown > Core.Epsilon) { deffenceCoeff = defenceCoeffUp / defenceCoeffDown; } else { deffenceCoeff = 1f; }
      }
    }
    public static int ChargesCount(this MechComponent component) {
      return ActivatableComponent.getChargesCount(component);
    }
    public static bool CanBeActivated(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (component.DamageLevel >= ComponentDamageLevel.Destroyed) { return false; }
      if (activatable == null) { return false; };
      if (activatable.CanBeactivatedManualy == false) { return false; }
      if (activatable.ChargesCount == -1) { return true; };
      if (activatable.ChargesCount > 0) { if (activatable.ChargesCount <= component.ChargesCount()) { return false; }; };
      return true;
    }
    public static bool isUsefullByHeat(this MechComponent component, float heatCoeff, float overheatCoeff) {
      if (component.isHeat()) {
        if (heatCoeff > Core.Settings.AIHeatCoeffCoeff) { return false; }
        if (overheatCoeff > Core.Settings.AIOverheatCoeffCoeff) { return false; }
      }
      return true;
    }
    public static bool isDamaged(this MechComponent component) {
      return ((component.DamageLevel > ComponentDamageLevel.Functional) && (component.DamageLevel < ComponentDamageLevel.Destroyed));
    }
    public static bool isOffence(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_offence");
    }
    public static bool isDefence(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_defence");
    }
    public static bool isExplode(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_explode");
    }
    public static bool isHeat(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_heat");
    }
    public static bool isCool(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_cool");
    }
    public static bool isSpeed(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_speed");
    }
    public static bool isSensors(this MechComponent component) {
      return component.ComponentTags().Contains("cae_ai_sensors");
    }
    public static float FailChance(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0f; };
      if (activatable.AlwaysFail) { return 1f; };
      return ActivatableComponent.getEffectiveComponentFailChance(component);
    }
    public static bool isFailDanger(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      Mech mech = component.parent as Mech;
      if (activatable.FailCrit && component.isExplode()) { return true; };
      if (mech == null) { return false; }
      foreach (var Location in activatable.FailDamageLocations) {
        if (mech != null) {
          foreach (var lcomp in mech.GetComponentsForLocation(Location, ComponentType.AmmunitionBox)) {
            return true;
          }
        }
        if(component.parent.StructureForLocation((int)Location) < activatable.FailISDamage) {
          return true;
        }
      }
      return false;
    }
  }
}