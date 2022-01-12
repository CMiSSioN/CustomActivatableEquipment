using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(TurnDirector))]
  [HarmonyPatch("BeginNewPhase")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class TurnDirector_BeginNewPhase {
    private static HashSet<AbstractActor> activatedActors = new HashSet<AbstractActor>();
    public static bool isActivated(this AbstractActor unit) { return activatedActors.Contains(unit); }
    public static void ActiveDefaultComponents(this AbstractActor unit) {
      Log.Debug?.WL(1, "ActiveDefaultComponents:" + unit.PilotableActorDef.Description.Id);
      foreach (MechComponent component in unit.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        Log.Debug?.WL(2, "component:" + component.Description.Id+ " ActiveByDefault:" + activatable.ActiveByDefault);
        if (activatable.ActiveByDefault == true) {
          ActivatableComponent.activateComponent(component, true, true);
        } else {
          activatable.applyOfflineEffects(component, true);
        }
      }
      activatedActors.Add(unit);
    }
    public static void Postfix(TurnDirector __instance, int newPhase) {
      if (CACCombatState.IsInDeployManualState) { return; }
      Log.Debug?.TWL(0,"BeginNewPhase round:"+__instance.CurrentRound+" phase:"+newPhase);
      foreach(AbstractActor unit in __instance.Combat.AllActors) {
        if (unit.isActivated()) { continue; }
        unit.ActiveDefaultComponents();
        unit.UpdateAurasWithSensors();
      }
    }
  }
  //[HarmonyPatch(typeof(UnitSpawnPointGameLogic))]
  //[HarmonyPatch("initializeActor")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Team), typeof(Lance) })]
  //public static class AbstractActor_Init {
  //  public static void Prefix(UnitSpawnPointGameLogic __instance, AbstractActor actor, Team team, Lance lance) {
  //    CustomActivatableEquipment.Log.Debug?.Write("UnitSpawnPointGameLogic.initializeActor " + new Text(actor.DisplayName).ToString() + ":" + new Text(__instance.DisplayName).ToString() + "\n");
  //    actor.AddToActiveDefaultQueue();
  //  }
  //}
  //[HarmonyPatch(typeof(CombatHUD))]
  //[HarmonyPatch("Update")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { })]
  //public static class CombatHUD_Update {
  //  private static Queue<AbstractActor> spawnedActors = new Queue<AbstractActor>();
  //  public static void AddToActiveDefaultQueue(this AbstractActor unit) {
  //    spawnedActors.Enqueue(unit);
  //  }
  //  public static void ActiveDefaultComponents(this AbstractActor unit) {
  //    foreach (MechComponent component in unit.allComponents) {
  //      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
  //      if (activatable == null) { continue; }
  //      if (activatable.ActiveByDefault == true) {
  //        ActivatableComponent.activateComponent(component, true, true);
  //      } else {
  //        activatable.applyOfflineEffects(component, true);
  //      }
  //    }
  //  }
  //  public static void Postfix(CombatHUD __instance) {
  //    try {
  //      if(spawnedActors.Count > 0) {
  //        AbstractActor unit = spawnedActors.Dequeue();
  //        unit.ActiveDefaultComponents();
  //      }
  //    } catch (Exception e) {
  //      Log.WriteCritical(e.ToString() + "\n");
  //    }
  //  }
  //}
}
