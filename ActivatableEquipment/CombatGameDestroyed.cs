using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(CombatGameState))]
  [HarmonyPatch("OnCombatGameDestroyed")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatGameState_OnCombatGameDestroyedMap {
    public static bool Prefix(CombatGameState __instance) {
      RepairHelper.Clear();
      HashSet<string> clearComponents = new HashSet<string>();
      foreach (var avfx in ComponentVFXHelper.componentsVFXObjects) {
        clearComponents.Add(avfx.Key);
      }
      foreach (var compGUID in clearComponents) {
        if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(compGUID)) {
          ComponentVFXHelper.componentsVFXObjects[compGUID].Clean();
        }
      }
      ComponentVFXHelper.componentsVFXObjects.Clear();
      CAEAuraHelper.ClearBubbles();
      CombatHUDWeaponPanelExHelper.Clear();
      CombatHUDEquipmentPanel.Clear();
      ActivatableComponent.Clear();
      CombatHUDEquipmentSlotEx.Clear();
      CombatHUDEquipmentPanel.Clear();
      AbstractActor_InitAbstractActor.Clear();
      C3Helper.Clear();
      CustomStatisticEffectHelper.Clear();
      //DamageHelpers.DamageHelper.Clear();
      return true;
    }
  }
}