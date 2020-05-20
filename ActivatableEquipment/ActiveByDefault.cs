using BattleTech;
using BattleTech.UI;
using CustomActivatableEquipment;
using CustomComponents;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(UnitSpawnPointGameLogic))]
  [HarmonyPatch("initializeActor")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Team), typeof(Lance) })]
  public static class AbstractActor_Init {
    public static void Postfix(UnitSpawnPointGameLogic __instance, AbstractActor actor, Team team, Lance lance) {
      CustomActivatableEquipment.Log.LogWrite("UnitSpawnPointGameLogic.initializeActor " + ":" + new Text(__instance.DisplayName).ToString() + "\n");
      actor.AddToActiveDefaultQueue();
    }
  }
  [HarmonyPatch(typeof(CombatHUD))]
  [HarmonyPatch("Update")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUD_Update {
    private static Queue<AbstractActor> spawnedActors = new Queue<AbstractActor>();
    public static void AddToActiveDefaultQueue(this AbstractActor unit) {
      spawnedActors.Enqueue(unit);
    }
    public static void ActiveDefaultComponents(this AbstractActor unit) {
      foreach (MechComponent component in unit.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        if (activatable.ActiveByDefault == true) {
          ActivatableComponent.activateComponent(component, true, true);
        } else {
          activatable.applyOfflineEffects(component, true);
        }
      }
    }
    public static void Postfix(CombatHUD __instance) {
      try {
        if(spawnedActors.Count > 0) {
          AbstractActor unit = spawnedActors.Dequeue();
          unit.ActiveDefaultComponents();
        }
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
}
