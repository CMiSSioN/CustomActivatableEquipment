using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("RefreshDisplayedEquipment")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDWeaponPanel_RefreshDisplayedEquipment {
    public static List<CombatHUDEquipmentSlot> EquipmentSlots = null;
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance, List<CombatHUDEquipmentSlot> ___EquipmentSlots, AbstractActor ___displayedActor) {
      EquipmentSlots = ___EquipmentSlots;
      for (int index = 1; index < EquipmentSlots.Count; ++index) {
        EquipmentSlots[index].DisableButton();
        EquipmentSlots[index].gameObject.SetActive(false);
      }
      if(EquipmentSlots.Count > 0){
        EquipmentSlots[0].gameObject.transform.parent.gameObject.SetActive(false);
        EquipmentSlots[0].gameObject.SetActive(false);
        EquipmentSlots[0].InitButton(SelectionType.ActiveProbe, null, null, "ACTIVE_COMPONENTS_MENU", "Active components menu", ___displayedActor);
        EquipmentSlots[0].Text.SetText("COMPONENTS", (object[])Array.Empty<object>());
        EquipmentSlots[0].ResetButtonIfNotActive(___displayedActor);
      }
      if (CombatHUDEquipmentPanel.Instance != null) { CombatHUDEquipmentPanel.Instance.RefreshDisplayedEquipment(__instance.DisplayedActor); };
    }
  }
  [HarmonyPatch(typeof(TurnDirector))]
  [HarmonyPatch("BeginNewRound")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class TurnDirector_BeginNewRound {
    //public static bool Prepare() { return false; }
    public static void Postfix(TurnDirector __instance) {
      foreach(AbstractActor unit in __instance.Combat.AllActors) {
        if (unit.IsShutDown) { continue; };
        if (unit.IsDead) { continue; }
        if (CACCombatState.IsInDeployManualState) { continue; }
        unit.UpdateAurasWithSensors();
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDSidePanelHoverElement))]
  [HarmonyPatch("OnPointerClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(PointerEventData) })]
  public static class CombatHUDSidePanelHoverElement_OnPointerClick {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDSidePanelHoverElement __instance) {
      Log.Debug?.TWL(0, "CombatHUDSidePanelHoverElement.OnPointerClick " + __instance.Title.ToString());
      if (CombatHUDEquipmentPanel.Instance != null) { CombatHUDEquipmentPanel.Instance.ProcessOnPointerClick(__instance); };
    }
  }
  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("DisplayedActor")]
  [HarmonyPatch(MethodType.Setter)]
  public static class CombatHUDWeaponPanel_DisplayedActor {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance, AbstractActor value) {
      if (CombatHUDEquipmentPanel.Instance != null) { CombatHUDEquipmentPanel.Instance.InitDisplayedEquipment(value); };
    }
  }
  [HarmonyPatch(typeof(CombatHUDActionButton))]
  [HarmonyPatch("TryActivate")]
  [HarmonyPatch(MethodType.Normal)]
  public static class CombatHUDEquipmentSlot_IsActive {
    private static PropertyInfo p_HUD = typeof(CombatHUDButtonBase).GetProperty("HUD", BindingFlags.Instance | BindingFlags.NonPublic);
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDActionButton __instance, ref bool __result) {
      CombatHUDEquipmentSlot slot = __instance as CombatHUDEquipmentSlot;
      if (slot == null) { return; }
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlot.TryActivate GUID:" + __instance.GUID+" selection type:"+ (SelectionType)typeof(CombatHUDEquipmentSlot).GetProperty("SelectionType",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(slot) +" result:"+__result);
      if (__result) {
        CombatHUD HUD = (CombatHUD)p_HUD.GetValue(slot);
        HUD.AttackModeSelector.FireButton.FireText.SetText(slot.Ability.Def.Description.Name);
      }
    }
  }
  [HarmonyPatch(typeof(CombatSelectionHandler))]
  [HarmonyPatch("ProcessPressedButtons")]
  [HarmonyPatch(MethodType.Normal)]
  public static class CombatHUDEquipmentSlot_ProcessPressedButtons {
    //public static bool Prepare() { return false; }
    public static void Prefix(CombatSelectionHandler __instance) {
      Log.Debug?.TWL(0, "CombatSelectionHandler.ProcessPressedButtons ActiveState:" + (__instance.ActiveState == null?"null":__instance.ActiveState.ToString()));
      foreach(string btn in __instance.PressedButtons) {
        Log.Debug?.WL(1, btn);
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDAttackModeSelector))]
  [HarmonyPatch("ShowFireButton")]
  [HarmonyPatch(MethodType.Normal)]
  public static class CombatHUDAttackModeSelector_ShowFireButton {
    private static string RenameFireButton = string.Empty;
    public static void RenameFireButtonOnce(string val) {
      CombatHUDAttackModeSelector_ShowFireButton.RenameFireButton = val;
    }
    public static void Postfix(CombatHUDAttackModeSelector __instance, CombatHUDFireButton.FireMode mode, string additionalDetails, bool showHeatWarnings) {
      Log.Debug?.TWL(0, "CombatHUDAttackModeSelector.ShowFireButton mode:"+mode);
      Log.Debug?.WL(0, Environment.StackTrace);
      if (string.IsNullOrEmpty(RenameFireButton) == false) {
        RenameFireButton = string.Empty;
        __instance.FireButton.FireText.SetText(RenameFireButton);
      }
    }
  }
  /*[HarmonyPatch(typeof(CombatHUDEquipmentSlot))]
  [HarmonyPatch("ExecuteClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDEquipmentSlot_ExecuteClick {
    private static PropertyInfo p_HUD = typeof(CombatHUDButtonBase).GetProperty("HUD", BindingFlags.Instance | BindingFlags.NonPublic);
    public static CombatHUD HUD(this CombatHUDButtonBase button) {
      return (CombatHUD)p_HUD.GetValue(button);
    }
    //public static bool Prepare() { return false; }
    public static bool Prefix(CombatHUDEquipmentSlot __instance) {
      int num1 = (int)WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_action_generic, WwiseManager.GlobalAudioObject, (AkCallbackManager.EventCallback)null, (object)null);
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlot.ExecuteClick");
      if (CombatHUDWeaponPanel_RefreshDisplayedEquipment.EquipmentSlots != null) {
        if (CombatHUDWeaponPanel_RefreshDisplayedEquipment.EquipmentSlots.Count > 0) {
          if (CombatHUDWeaponPanel_RefreshDisplayedEquipment.EquipmentSlots[0] == __instance) {
            //ActivatebleDialogHelper.CreateDialog(__instance.HUD().SelectedActor, __instance.HUD());
            //ActivatebleDialogHelper.ShowComponents(__instance.HUD());
            
            return false;
          }
        }
      }
      return true;
    }
  }*/

}