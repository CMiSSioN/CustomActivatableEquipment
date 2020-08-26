using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomComponents;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomActivatableEquipment {
  public class AbilityLine {
    public Ability ability { get; set; }
    public string name { get; set; }
    public string state { get; set; }
    public bool error { get; set; }
    public int length() { return name.Length + state.Length; }
    public string ToString(bool selected) {
      StringBuilder sline = new StringBuilder();
      sline.Append("<size=90%>");
      if (selected) { sline.Append("<mark=#FFFFFF20>"); };
      if (selected == false) { sline.Append("<color=#7F7F7FFF>"); } else { sline.Append("<color=#FFFFFFFF>"); };
      sline.Append("<uppercase>");
      sline.Append("<mspace=2.5em>" + this.name + "</mspace>");
      sline.Append("<mspace=2.5em>" + this.state + "</mspace>");
      sline.Append("</uppercase>");
      sline.Append("</color>");
      if (selected) { sline.Append("</mark>"); };
      sline.Append("</size>");
      return sline.ToString();
    }
    public AbilityLine() {
      name = string.Empty;
      state = string.Empty;
      error = false;
    }
  }
  public class ComponentLine {
    public MechComponent component { get; set; }
    public string name { get; set; }
    public string state { get; set; }
    public string heat { get; set; }
    public string fail { get; set; }
    public float failchance { get; set; }
    public bool manual { get; set; }
    public bool active { get; set; }
    public bool activatable { get; set; }
    public bool error { get; set; }
    public int length() { return name.Length + state.Length + heat.Length + fail.Length; }
    public string ToString(bool selected) {
      StringBuilder sline = new StringBuilder();
      if (selected) { sline.Append("<mark=#FFFFFF20>"); };
      if (this.active == false) {
        if (selected == false) { sline.Append("<color=#7F7F7FFF>"); } else { sline.Append("<color=#FFFFFFFF>"); };
      } else {
        if (this.failchance > 0.5f) { sline.Append("<color=\"red\">"); } else
        if (this.failchance > 0.3f) { sline.Append("<color=\"orange\">"); } else { sline.Append("<color=#3FFF3FFF>"); };
      }
      sline.Append("<uppercase>");
      sline.Append("<mspace=2.5em>" + this.name + "</mspace>");
      sline.Append("<mspace=2.5em>" + this.state + "</mspace>");
      sline.Append("<mspace=2.5em>" + this.heat + "</mspace>");
      sline.Append("<mspace=2.5em>" + this.fail + "</mspace>");
      sline.Append("</uppercase>");
      sline.Append("</color>");
      if (selected) { sline.Append("</mark>"); };
      return sline.ToString();
    }
    public ComponentLine() {
      name = string.Empty;
      state = string.Empty;
      heat = string.Empty;
      fail = string.Empty;
      failchance = 0f;
      manual = false;
      active = false;
    }
  }
  public class ActivatbleMenuTriggers : EventTrigger {
    public ComponentLine dataline { get; set; }
    private LocalizableText line { get; set; }
    public override void OnPointerEnter(PointerEventData data) {
      Log.Debug?.Write("OnPointerEnter called." + data.position + "\n");
      line.SetText(dataline.ToString(true));
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.Debug?.Write("OnPointerExit called." + data.position + "\n");
      line.SetText(dataline.ToString(false));
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.Debug?.Write("OnPointerClick called." + data.position + "\n");
      if (dataline.error) { return; }
      if (dataline.manual == false) { return; }
      if (dataline.component != null) {
        Log.Debug?.Write("Toggle activatable " + dataline.component.defId + "\n");
        ActivatableComponent.toggleComponentActivation(dataline.component);
      }
      if (ActivatebleDialogHelper.popup != null) {
        Log.Debug?.Write("Aborting popup\n");
        ActivatebleDialogHelper.popup.Pool(false);
      }
      //line.SetText(dataline.ToString(false));
    }
    public void Init(LocalizableText line, ComponentLine data) {
      this.line = line;
      this.dataline = data;
    }
    public void Deactivate() {
      Log.Debug?.Write("ActivatbleMenuTriggers.Deactivate\n");
    }
  }
  public class AbilityMenuTriggers : EventTrigger {
    public AbilityLine dataline { get; set; }
    private LocalizableText line { get; set; }
    private CombatHUD HUD { get; set; }
    public override void OnPointerEnter(PointerEventData data) {
      Log.Debug?.Write("OnPointerEnter called." + data.position + "\n");
      line.SetText(dataline.ToString(true));
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.Debug?.Write("OnPointerExit called." + data.position + "\n");
      line.SetText(dataline.ToString(false));
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.Debug?.Write("OnPointerClick called." + data.position + "\n");
      if (dataline.error) { return; }
      if (dataline.ability != null) {
        Log.Debug?.Write("Toggle ability " + dataline.ability.Def.Description.Id + "\n");
        List<CombatHUDEquipmentSlot> equipmentSlots = CombatHUDWeaponPanel_RefreshDisplayedEquipment.EquipmentSlots;
        Log.WL(1, "EquipmentSlots:"+ equipmentSlots.Count);
        equipmentSlots[1].InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(dataline.ability.Def.Targeting, false), dataline.ability, dataline.ability.Def.AbilityIcon, dataline.ability.Def.Description.Id, dataline.ability.Def.Description.Name, HUD.SelectedActor);
        equipmentSlots[1].gameObject.SetActive(false);
        equipmentSlots[1].ResetButtonIfNotActive(HUD.SelectedActor);
        equipmentSlots[1].OnClick();
      }
      if (ActivatebleDialogHelper.popup != null) {
        Log.Debug?.Write("Aborting popup\n");
        ActivatebleDialogHelper.popup.Pool(false);
      }
      //line.SetText(dataline.ToString(false));
    }
    public void Init(LocalizableText line, AbilityLine data, CombatHUD HUD) {
      this.line = line;
      this.dataline = data;
      this.HUD = HUD;
    }
    public void Deactivate() {
      Log.Debug?.Write("ActivatbleMenuTriggers.Deactivate\n");
    }
  }

  public static class ActivatebleDialogHelper {
    public static GenericPopup popup = null;
    public static LocalizableText AddLine(VerticalLayoutGroup layout, LocalizableText src, string text, TMPro.TextAlignmentOptions aligin) {
      GameObject aline = new GameObject();
      LocalizableText atextline = aline.AddComponent<LocalizableText>();
      atextline.fontSizeMax = src.fontSizeMax;
      atextline.fontSizeMin = src.fontSizeMin;
      atextline.fontSize = src.fontSize;
      atextline.SetText(text);
      atextline.alignment = aligin;
      atextline.enableWordWrapping = false;
      aline.transform.SetParent(layout.transform);
      return atextline;
    }
    public static void AddLine(VerticalLayoutGroup layout, CombatHUD HUD, LocalizableText src, AbilityLine dataline, bool error) {
      //LocalizableText upline = AddLine(layout, src, new string('_', dataline.length()), TMPro.TextAlignmentOptions.TopLeft);
      LocalizableText atextline = AddLine(layout, src, dataline.ToString(false), TMPro.TextAlignmentOptions.TopLeft);
      //LocalizableText btline = AddLine(layout, src, new string('_', dataline.length()), TMPro.TextAlignmentOptions.TopLeft);
      if (error) { dataline.error = true; };
      if ((error == false) && (dataline.error == false)) {
        AbilityMenuTriggers trigger = atextline.gameObject.GetComponent<AbilityMenuTriggers>();
        if (trigger == null) {
          trigger = atextline.gameObject.AddComponent<AbilityMenuTriggers>();
        }
        trigger.Init(atextline, dataline, HUD);
      }
    }
    public static void AddLine(VerticalLayoutGroup layout, CombatHUD HUD, LocalizableText src, ComponentLine dataline, bool error) {
      //LocalizableText upline = AddLine(layout, src, new string('_', dataline.length()), TMPro.TextAlignmentOptions.TopLeft);
      LocalizableText atextline = AddLine(layout, src, dataline.ToString(false), TMPro.TextAlignmentOptions.TopLeft);
      //LocalizableText btline = AddLine(layout, src, new string('_', dataline.length()), TMPro.TextAlignmentOptions.TopLeft);
      if (error) { dataline.error = true; };
      if ((error == false) && (dataline.error == false)) {
        ActivatbleMenuTriggers trigger = atextline.gameObject.GetComponent<ActivatbleMenuTriggers>();
        if (trigger == null) {
          trigger = atextline.gameObject.AddComponent<ActivatbleMenuTriggers>();
        }
        trigger.Init(atextline, dataline);
      }
    }
    private static bool weaponPanelState = true;
    public static void ResetEquipmentState(this CombatHUDWeaponPanel panel) { weaponPanelState = true; }
    private static Dictionary<string, object> WPStates = new Dictionary<string, object>();
    public static void Init() {
      Type states = typeof(CombatHUDWeaponPanel).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).FieldType;
      Log.Debug?.TWL(0, "WPState:" + (states == null ? "null" : states.ToString()));
      if (states != null) {
        var WPStateValues = Enum.GetValues(states);
        foreach (var WPStateValue in WPStateValues) {
          Log.WL(1, WPStateValue.ToString());
          WPStates.Add(WPStateValue.ToString(),WPStateValue);
        }
      }
    }
    public static void ShowComponents(CombatHUD HUD) {
      Log.Debug?.TWL(0, "ShowComponents");
      //if (HUD.SelectedActor == null) { return; };
      typeof(CombatHUDWeaponPanel).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(HUD.WeaponPanel, new object[] { weaponPanelState?WPStates["Unloading"] : WPStates["Loading"] });
      weaponPanelState = !weaponPanelState;
    }
    public static void CreateDialog(this AbstractActor unit, CombatHUD HUD) {
      Dictionary<MechComponent, ComponentLine> actComps = new Dictionary<MechComponent, ComponentLine>();
      Dictionary<MechComponent, List<AbilityLine>> ablComps = new Dictionary<MechComponent, List<AbilityLine>>();
      //Core.currentActiveComponentsDlg.Clear();
      /*foreach (MechComponent component in unit.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        ComponentLine line = new ComponentLine();
        line.component = component;
        line.name = component.UIName.ToString();
        if (component.IsFunctional == false) {
          line.state += " __/CAE.NonFunctional/__";
          line.error = true;
        }else
        if (activatable != null) {
          Log.LogWrite(component.defId + ":" + component.parent.GUID + ":" + component.getCCGUID() + " is activatable\n");
          line.error = false;
          if (ActivatableComponent.isOutOfCharges(component)) {
            line.state += " __/CAE.OutOfCharges/__";
            line.error = true;
          } else
          if (activatable.ChargesCount == -1) {
            line.state += " __/CAE.OPERATIONAL/__";
            line.error = true;
          } else
          if (activatable.ChargesCount > 0) {
            line.state += " __/CAE.CHARGES/__:" + ActivatableComponent.getChargesCount(component);
          }
          if (ActivatableComponent.isComponentActivated(component)) {
            line.active = true;
            line.state += (" " + activatable.ActivationMessage);
            if (activatable.CanBeactivatedManualy == false) {
              if (component.parent is Mech) {
                float neededHeat = (activatable.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activatable.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activatable.AutoDeactivateOnHeat;
                line.heat = ("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
              }
            }
          } else {
            line.active = false;
            line.state += (" " + activatable.DeactivationMessage + " ");
            if (activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
              if (component.parent is Mech) {
                float neededHeat = (activatable.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activatable.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activatable.AutoActivateOnHeat;
                line.heat = ("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
              }
            }
          }
          line.manual = activatable.CanBeactivatedManualy;
          line.failchance = ActivatableComponent.getEffectiveComponentFailChance(component);
          line.fail = "__/CAE.FAIL/__:" + Math.Round(line.failchance * 100f) + "%";
          if (line.active || (activatable.AutoActivateOnHeat > Core.Epsilon) || line.manual) {
            actComps.Add(component, line);
          }
        } else {
          Log.LogWrite(component.defId + ":" + component.parent.GUID + ":" + component.getCCGUID() + " is not activatable\n");
        }
      }
      foreach (Ability ability in unit.ComponentAbilities) {
        AbilityLine alinedata = new AbilityLine();
        alinedata.ability = ability;
        alinedata.name = ability.parentComponent.UIName + ":" + ability.Def.Description.Name;
        //SelectionType prefSelType = CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false);
        alinedata.state = "STATE:";
        if (ability.Def.ActivationTime == AbilityDef.ActivationTiming.Passive) {
          alinedata.error = true;
          alinedata.state += "PASSIVE";
        } else if ((ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByMovement) && (unit.HasMovedThisRound)) {
          alinedata.error = true;
          alinedata.state = "MOVED";
        } else if ((ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring) && (unit.HasFiredThisRound)) {
          alinedata.error = true;
          alinedata.state += "FIRED";
        } else if (ability.IsAvailable == false) {
          alinedata.error = true;
          alinedata.state += "NOT AVAIBLE";
        } else if (ability.IsActive) {
          alinedata.error = false;
          alinedata.state += "ACTIVE";
        } else {
          alinedata.error = false;
          alinedata.state += "NOT ACTIVE";
        }
        if (ablComps.ContainsKey(ability.parentComponent) == false) { ablComps.Add(ability.parentComponent, new List<AbilityLine>()); }
        ablComps[ability.parentComponent].Add(alinedata);
        //ablComps.Add(alinedata);
      }

      if ((actComps.Count == 0)&&(unit.ComponentAbilities.Count == 0)) { return; }
      int length_name = 0;
      int length_state = 0;
      int length_heat = 0;
      int length_fail = 0;
      foreach (var line in actComps) {
        if (line.Value.name.Length > length_name) { length_name = line.Value.name.Length; }
        if (line.Value.state.Length > length_state) { length_state = line.Value.state.Length; }
        if (line.Value.heat.Length > length_heat) { length_heat = line.Value.heat.Length; }
        if (line.Value.fail.Length > length_fail) { length_fail = line.Value.fail.Length; }
      }
      foreach (var line in actComps) {
        if (line.Value.name.Length < (length_name + 1)) { line.Value.name += new string(' ', length_name - line.Value.name.Length + 1); }
        if (line.Value.state.Length < (length_state + 1)) { line.Value.state += new string(' ', length_state - line.Value.state.Length + 1); }
        if (line.Value.heat.Length < (length_heat + 1)) { line.Value.heat += new string(' ', length_heat - line.Value.heat.Length + 1); }
        if (line.Value.fail.Length < (length_fail + 1)) { line.Value.fail += new string(' ', length_fail - line.Value.fail.Length + 1); }
      }
      int all_length = length_name + length_state + length_heat + length_fail;
      GenericPopupBuilder pb = GenericPopupBuilder.Create("__/CAE.Components/__", "                                                                           ");
      pb.AddButton("X", (Action)(() => { }), true);
      pb.OnClose = (Action)(() => {
        Log.LogWrite("Menu closed\n");
        LocalizableText content = (LocalizableText)typeof(GenericPopup).GetField("_contentText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
        content.transform.parent.gameObject.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
        List<GameObject> childs = new List<GameObject>();
        for (int i = 0; i < content.gameObject.transform.childCount; ++i) {
          childs.Add(content.gameObject.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < childs.Count; ++i) {
          Log.LogWrite(" destroy:" + childs[i].name + ":" + childs[i].GetInstanceID() + "\n");
          GameObject.Destroy(childs[i]);
          childs[i] = null;
        }
      });

      popup = pb.IsNestedPopupWithBuiltInFader().SetAlwaysOnTop().CancelOnEscape().Render();
      LocalizableText textline = (LocalizableText)typeof(GenericPopup).GetField("_contentText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
      textline.transform.parent.gameObject.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
      VerticalLayoutGroup layout = textline.gameObject.GetComponent<VerticalLayoutGroup>();
      if (layout == null) {
        layout = textline.gameObject.AddComponent<VerticalLayoutGroup>();
      }
      layout.spacing = 10f;
      layout.padding.left = 0;
      layout.padding.right = 0;
      layout.padding.bottom = 0;
      layout.padding.top = 0;
      bool error = (HUD.SelectedTarget != null) || (unit.IsAvailableThisPhase == false) || (unit.HasMovedThisRound);
      for (int index = 0; index < actComps.Count; ++index) {
        AddLine(layout, HUD, textline, actComps[index].Value, error);
      }
      if (HUD.SelectedTarget != null) {
        AddLine(layout, textline, "__/CAE.SelectedTargetForbidden/__", TMPro.TextAlignmentOptions.Center);
      }
      if (unit.IsAvailableThisPhase == false) {
        AddLine(layout, textline, "__/CAE.NotAvaibleThisPhase/__", TMPro.TextAlignmentOptions.Center);
      }
      if (unit.HasMovedThisRound) {
        AddLine(layout, textline, "Can't activate/deactivate equipment after move", TMPro.TextAlignmentOptions.Center);
      }
      if (unit.ComponentAbilities.Count > 0) {
        AddLine(layout, textline, "COMPONENT'S ABILITIES:", TMPro.TextAlignmentOptions.Center);
        error = (unit.HasActivatedThisRound || !unit.IsAvailableThisPhase || unit.MovingToPosition != null || unit.Combat.StackManager.IsAnyOrderActive && unit.Combat.TurnDirector.IsInterleaved);
        //SelectionState activeState = HUD.SelectionHandler.ActiveState;
        List<AbilityLine> alines = new List<AbilityLine>();
        length_name = 0;
        length_state = 0;
        foreach (var line in alines) {
          if (line.name.Length > length_name) { length_name = line.name.Length; }
          if (line.state.Length > length_state) { length_state = line.state.Length; }
        }
        foreach (var line in alines) {
          if (line.name.Length < (length_name + 1)) { line.name += new string(' ', length_name - line.name.Length + 1); }
          if (line.state.Length < (length_state + 1)) { line.state += new string(' ', length_state - line.state.Length + 1); }
        }
        foreach (AbilityLine alinedata in alines) {
          AddLine(layout, HUD, textline, alinedata, error);
        }
        if (unit.IsAvailableThisPhase == false) {
          AddLine(layout, textline, "__/CAE.NotAvaibleThisPhase/__", TMPro.TextAlignmentOptions.Center);
        }
        if (unit.HasActivatedThisRound) {
          AddLine(layout, textline, "Can't apply abilities after activation", TMPro.TextAlignmentOptions.Center);
        }
        if (unit.MovingToPosition != null) {
          AddLine(layout, textline, "Can't apply abilities while moving", TMPro.TextAlignmentOptions.Center);
        }
        if (unit.Combat.StackManager.IsAnyOrderActive && unit.Combat.TurnDirector.IsInterleaved) {
          AddLine(layout, textline, "Can't apply abilities when other units moving", TMPro.TextAlignmentOptions.Center);
        }
      }*/
    }
  }
  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("ResetDisplayedWeapons")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDWeaponPanel_ResetDisplayedWeapons {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance) {
      __instance.ResetEquipmentState();
    }
  }
}