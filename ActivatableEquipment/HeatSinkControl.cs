using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using HarmonyLib;
using HBS;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomActivatableEquipment {
  public class HeatSinkPanelListItem: MonoBehaviour, IPointerClickHandler {
    public LanceMechEquipmentListItem item = null;
    public MechComponent component = null;
    public CombatHUDHeatSinkPanel parent = null;
    public MechDef activeMech = null;
    public void SetData(CombatHUDHeatSinkPanel parent, MechDef mechDef, LanceMechEquipmentListItem item, MechComponent mechComponent) {
      this.parent = parent;
      this.component = mechComponent;
      this.item = item;
      this.activeMech = mechDef;
      UIColor bgColor = MechComponentRef.GetUIColor(component.mechComponentRef);
      if(component.DamageLevel == ComponentDamageLevel.Destroyed) { bgColor = UIColor.Disabled; }
      item.SetData(component.mechComponentRef.Def.Description.UIName, component.DamageLevel, bgColor: bgColor);
      item.SetComponentRef(component.mechComponentRef, mechDef);
      item.SetTooltipData(component.mechComponentRef.Def);
      if((mechComponent.GetHeatSinkState() == false) && (component.DamageLevel != ComponentDamageLevel.Destroyed)) {
        item.itemTextColor.SetUIColor(UIColor.DarkGray);
      }
    }
    public void OnPointerClick(PointerEventData eventData) {
      Log.Debug?.TWL(0, $"HeatSinkPanelListItem.OnPointerClick {component.defId}");
      if(component.DamageLevel == ComponentDamageLevel.Destroyed) { return; }
      if(component.GetHeatSinkState() == true){
        if(parent.HUD.SelectedActor is Mech mech) { if(mech.AdjustedHeatsinkCapacity == 0) { return; } }
      }
      component.ToggleHeatSinkState();
      if(component.GetHeatSinkState() == false) {
        item.itemTextColor.SetUIColor(UIColor.DarkGray);
      } else {
        UIColor bgColor = MechComponentRef.GetUIColor(component.mechComponentRef);
        if(component.DamageLevel == ComponentDamageLevel.Destroyed) { bgColor = UIColor.Disabled; }
        item.SetData(component.mechComponentRef.Def.Description.UIName, component.DamageLevel, bgColor: bgColor);
        item.SetTooltipData(component.mechComponentRef.Def);
      }
      this.parent.RefreshText();
    }
  }
  public static class CombatHUDHeatSinkPanelHelper {
    public static readonly string HEAT_SINK_STATE_STATISTIC_NAME = "CAE_HEATSINK_STATE";
    public static bool GetHeatSinkState(this MechComponent component) {
      return component.statCollection.GetOrCreateStatisic<bool>(HEAT_SINK_STATE_STATISTIC_NAME, true).Value<bool>();
    }
    public static void ToggleHeatSinkState(this MechComponent component) {
      bool curState = component.GetHeatSinkState();
      if(curState == true) {
        component.statCollection.GetOrCreateStatisic<bool>(HEAT_SINK_STATE_STATISTIC_NAME, true).SetValue<bool>(false);
        component.CancelCreatedEffects(false);
      } else {
        component.statCollection.GetOrCreateStatisic<bool>(HEAT_SINK_STATE_STATISTIC_NAME, true).SetValue<bool>(true);
        component.RestartPassiveEffects(false);
      }
    }
  }
  public class CombatHUDHeatSinkPanel: MonoBehaviour {
    public static readonly string PREFAB_NAME = "uixPrfPanl_LC_HeatSinkPanelItem";
    private static CombatHUDHeatSinkPanel instance = null;
    public GenericPopup popup = null;
    public LanceMechEquipmentList itemsList = null;
    public CombatHUD HUD = null;
    public List<HeatSinkPanelListItem> allComponents = new List<HeatSinkPanelListItem>();
    public GameObject container = null;
    public GameObject itemsListHolder = null;
    public void Close() {
      try {
        if(this.popup == null) { return; }
        this.itemsListHolder.transform.SetParent(this.container.transform);
        this.popup = null;
      } catch(Exception e) {
        Log.Error?.TWL(0,e.ToString());
        UIManager.logger.LogException(e);
      }
      HUD = null;
    }
    public static void ClearEndCombat() {
      try {
        if(instance == null) { instance = UIManager.Instance.PopupRoot.gameObject.GetComponentInChildren<CombatHUDHeatSinkPanel>(true); }
        if(instance == null) { return; }
        if(instance.popup != null) {
          var popup = instance.popup;
          instance.Close();
          popup.Pool();
        }
        instance.Clear();
        GameObject.Destroy(instance);
        instance = null;
      }catch(Exception e) {
        Log.Error?.TWL(0, e.ToString());
        UIManager.logger.LogException(e);
      }
    }
    public void RefreshText() {
      StringBuilder stringBuilder = new StringBuilder();
      if(this.HUD.SelectedActor is Mech mech) {
        stringBuilder.AppendLine($"__/CAE.HEAT_CONTROL.CUR/__:{mech.CurrentHeat}/{mech.OverheatLevel}/{mech.MaxHeat}");
        //stringBuilder.AppendLine($"Overheat heat level:{mech.OverheatLevel}");
        //stringBuilder.AppendLine($"Shutdown heat level:{mech.MaxHeat}");
        stringBuilder.AppendLine($"__/CAE.HEAT_CONTROL.CAP/__:{mech.AdjustedHeatsinkCapacity}");
        stringBuilder.AppendLine($"__/CAE.HEAT_CONTROL.HELP/__");
      }
      popup._contentText.SetText(stringBuilder.ToString());
    }
    public void Show(CombatHUD HUD) {
      if(HUD == null) { return; }
      this.HUD = HUD;
      if(HUD.SelectedActor == null) { return; }
      if(itemsListHolder == null) { return; }
      if(itemsList == null) { return; }
      if(this.popup != null) { return; }
      this.popup = GenericPopupBuilder.Create("__/CAE.HEAT_PANEL.TOOLTIP.NAME/__", "PLACEHOLDER").AddButton("CLOSE", () => { this.Close(); }, true).Render();
      this.itemsListHolder.transform.SetParent(popup._contentText.gameObject.transform.parent);
      this.itemsListHolder.transform.SetSiblingIndex(popup._contentText.gameObject.transform.GetSiblingIndex() + 1);
      this.InitItemsList();
      this.RefreshText();
    }
    public void Clear() {
      List<HeatSinkPanelListItem> listItems = new List<HeatSinkPanelListItem>(this.allComponents);
      itemsList.allComponents.Clear();
      this.allComponents.Clear();
      foreach(var go in listItems) {
        itemsList.dataManager.PoolGameObject(PREFAB_NAME, go.gameObject);
      }
      listItems.Clear();
    }
    public void InitItemsList() {
      itemsList.activeMech = (HUD.SelectedActor as Mech).MechDef;
      itemsList.dataManager = HUD.Combat.DataManager;
      this.Clear();
      if(itemsList.activeMech == null)
        return;
      this.ItemsListSetLoadout();
    }
    public void ItemsListSetLoadout() {
      this.ItemsListSetLoadout(itemsList.headLabel, itemsList.headColor, itemsList.headLayout, ChassisLocations.Head);
      this.ItemsListSetLoadout(itemsList.centerTorsoLabel, itemsList.centerTorsoColor, itemsList.centerTorsoLayout, ChassisLocations.CenterTorso);
      this.ItemsListSetLoadout(itemsList.leftTorsoLabel, itemsList.leftTorsoColor, itemsList.leftTorsoLayout, ChassisLocations.LeftTorso);
      this.ItemsListSetLoadout(itemsList.rightTorsoLabel, itemsList.rightTorsoColor, itemsList.rightTorsoLayout, ChassisLocations.RightTorso);
      this.ItemsListSetLoadout(itemsList.leftArmLabel, itemsList.leftArmColor, itemsList.leftArmLayout, ChassisLocations.LeftArm);
      this.ItemsListSetLoadout(itemsList.rightArmLabel, itemsList.rightArmColor, itemsList.rightArmLayout, ChassisLocations.RightArm);
      this.ItemsListSetLoadout(itemsList.leftLegLabel, itemsList.leftLegColor, itemsList.leftLegLayout, ChassisLocations.LeftLeg);
      this.ItemsListSetLoadout(itemsList.rightLegLabel, itemsList.rightLegColor, itemsList.rightLegLayout, ChassisLocations.RightLeg);
    }
    private HeatSinkPanelListItem CreateItem() {
      GameObject gameObject = itemsList.dataManager.PooledInstantiate(PREFAB_NAME, BattleTechResourceType.UIModulePrefabs);
      HeatSinkPanelListItem result = null;
      if(gameObject != null) {
        result = gameObject.GetComponent<HeatSinkPanelListItem>();
        if(result != null) { return result; }
      }
      if(gameObject == null) { gameObject = itemsList.dataManager.PooledInstantiate("uixPrfPanl_LC_MechLoadoutItem", BattleTechResourceType.UIModulePrefabs); }
      if(gameObject == null) { return result; }
      result = gameObject.AddComponent<HeatSinkPanelListItem>();
      result.item = gameObject.GetComponent<LanceMechEquipmentListItem>();
      return result;
    }
    private void ItemsListSetLoadout(LocalizableText headerLabel, UIColorRefTracker headerColor,Transform layoutParent,ChassisLocations location) {
      Mech unit = this.HUD.SelectedActor as Mech;
      if(unit.isHasHeat() == false) { layoutParent.gameObject.SetActive(false); return; }
      var locDef = itemsList.activeMech.Chassis.GetLocationDef(location);
      if((locDef.MaxArmor == 0f) && (locDef.InternalStructure <= 1f)) { layoutParent.gameObject.SetActive(false); return; }
      string locName = itemsList.activeMech.Chassis.GetAbbreviatedChassisLocation(location);
      if(string.IsNullOrEmpty(locName)) { layoutParent.gameObject.SetActive(false); return; }
      headerLabel.SetText(locName);
      float currStructure = unit.GetCurrentStructure(location);
      float fullStructure = itemsList.activeMech.Chassis.GetLocationDef(location).InternalStructure;
      if(currStructure <= 0f) {
        headerColor.SetUIColor(UIColor.Red);
      } else if(currStructure < fullStructure) {
        headerColor.SetUIColor(UIColor.Gold);
      } else {
        headerColor.SetUIColor(UIColor.White);
      }
      bool hasComponent = false;
      foreach(var mechComponent in unit.allComponents) {
        MechComponentRef componentRef = mechComponent.mechComponentRef;
        if(mechComponent.componentType != ComponentType.HeatSink) { continue; }
        if(mechComponent.componentDef.ComponentTags.ContainsAny(Core.Settings.controlHeatSinkSkipTags)) { continue; }
        if(componentRef.MountedLocation == location) {
          HeatSinkPanelListItem item = this.CreateItem();
          LanceMechEquipmentListItem component = item.item;
          item.SetData(this,unit.MechDef, component, mechComponent);
          item.gameObject.transform.SetParent(layoutParent, false);
          itemsList.allComponents.Add(component.gameObject);
          this.allComponents.Add(item);
          hasComponent = true;
        }
      }
      if(hasComponent == false) { layoutParent.gameObject.SetActive(false); return; }
      layoutParent.gameObject.SetActive(true);
    }

    public static CombatHUDHeatSinkPanel Create() {
      try {
        GameObject mechBayMechInfoWidget = UIManager.Instance.dataManager.PooledInstantiate("uixPrfPanl_SIM_mechBayUnitInfo-Widget", BattleTechResourceType.UIModulePrefabs);
        if(mechBayMechInfoWidget == null) { return null; };
        LanceMechEquipmentList itemsList = mechBayMechInfoWidget.GetComponentInChildren<LanceMechEquipmentList>(true);
        if(itemsList == null) { UIManager.Instance.dataManager.PoolGameObject("uixPrfPanl_SIM_mechBayUnitInfo-Widget", mechBayMechInfoWidget); return null; };
        GameObject itemsListGo = GameObject.Instantiate(itemsList.gameObject.transform.parent.gameObject);
        itemsList = itemsListGo.GetComponentInChildren<LanceMechEquipmentList>(true);
        itemsListGo.name = "layout_loadout";
        UIManager.Instance.dataManager.PoolGameObject("uixPrfPanl_SIM_mechBayUnitInfo-Widget", mechBayMechInfoWidget);
        GameObject CombatHUDHeatSinkPanelGO = new GameObject("CombatHUDHeatSinkPanel");
        CombatHUDHeatSinkPanelGO.transform.SetParent(LazySingletonBehavior<UIManager>.Instance.PopupRoot.transform);
        GameObject CombatHUDHeatSinkPanelContainer = new GameObject("container");
        CombatHUDHeatSinkPanelContainer.transform.SetParent(CombatHUDHeatSinkPanelGO.transform);
        CombatHUDHeatSinkPanelContainer.SetActive(false);
        CombatHUDHeatSinkPanel result = CombatHUDHeatSinkPanelGO.AddComponent<CombatHUDHeatSinkPanel>();
        //GenericPopup popup = GenericPopupBuilder.Create("HEAT SINK CONTROL", "PLACEHOLDER").AddButton("CLOSE", () => { CombatHUDHeatSinkPanel.instance?.Close(); }, false).Render();
        //result = popup.gameObject.AddComponent<CombatHUDHeatSinkPanel>();
        //result.popup = popup;
        //result.gameObject.name = "CombatHUDHeatSinkPanel";
        result.itemsListHolder = itemsListGo;
        result.container = CombatHUDHeatSinkPanelContainer;
        itemsListGo.transform.SetParent(CombatHUDHeatSinkPanelContainer.transform);
        //itemsListGo.transform.SetSiblingIndex(popup._contentText.gameObject.transform.GetSiblingIndex() + 1);
        itemsListGo.transform.localScale = Vector3.one;
        var fitter = itemsListGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        result.itemsList = itemsList;
        HashSet<GameObject> toDelete = new HashSet<GameObject>();
        foreach(var tr in result.itemsList.headLayout.transform.parent.gameObject.GetComponentsInChildren<Transform>(true)) {
          if(tr.parent != itemsList.headLayout.transform.parent) { continue; }
          if(tr == itemsList.headLayout.transform) { continue; }
          toDelete.Add(tr.gameObject);
        }
        foreach(var go in toDelete) { GameObject.DestroyImmediate(go); }
        List<ChassisLocations> createLocation = new List<ChassisLocations>() {
          ChassisLocations.CenterTorso, ChassisLocations.RightTorso, ChassisLocations.LeftTorso, ChassisLocations.RightArm, ChassisLocations.LeftArm,
          ChassisLocations.RightLeg, ChassisLocations.LeftLeg
        };
        foreach(var location in createLocation) {
          var locationGo = GameObject.Instantiate(result.itemsList.headLayout.gameObject);
          string locName = "Loc";
          switch(location) {
            case ChassisLocations.CenterTorso: {
              locName = "CT";
              result.itemsList.centerTorsoLayout = locationGo.transform;
              result.itemsList.centerTorsoLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.centerTorsoColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.centerTorsoLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.centerTorsoLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.LeftTorso: {
              locName = "LT";
              result.itemsList.leftTorsoLayout = locationGo.transform;
              result.itemsList.leftTorsoLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.leftTorsoColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.leftTorsoLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.leftTorsoLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.RightTorso: {
              locName = "CT";
              result.itemsList.rightTorsoLayout = locationGo.transform;
              result.itemsList.rightTorsoLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.rightTorsoColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.rightTorsoLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.rightTorsoLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.LeftArm: {
              locName = "CT";
              result.itemsList.leftArmLayout = locationGo.transform;
              result.itemsList.leftArmLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.leftArmColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.leftArmLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.leftArmLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.RightArm: {
              locName = "CT";
              result.itemsList.rightArmLayout = locationGo.transform;
              result.itemsList.rightArmLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.rightArmColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.rightArmLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.rightArmLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.LeftLeg: {
              locName = "CT";
              result.itemsList.leftLegLayout = locationGo.transform;
              result.itemsList.leftLegLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.leftLegColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.leftLegLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.leftLegLayout.gameObject.name = $"{locName}";
            }; break;
            case ChassisLocations.RightLeg: {
              locName = "CT";
              result.itemsList.rightLegLayout = locationGo.transform;
              result.itemsList.rightLegLabel = locationGo.GetComponentInChildren<LocalizableText>(true);
              result.itemsList.rightLegColor = locationGo.GetComponentInChildren<UIColorRefTracker>(true);
              result.itemsList.rightLegLabel.gameObject.name = $"{locName}-txt";
              result.itemsList.rightLegLayout.gameObject.name = $"{locName}";
            }; break;
          }
          locationGo.transform.SetParent(result.itemsList.headLayout.gameObject.transform.parent);
          locationGo.transform.localScale = Vector3.one;
        }
        result.Close();
        return result;
      } catch(Exception e) {
        UIManager.logger.LogException(e);
        Log.Error?.TWL(0, e.ToString());
      }
      return null;
    }
    public static CombatHUDHeatSinkPanel Instance {
      get {
        if(instance == null) { instance = UIManager.Instance.PopupRoot.gameObject.GetComponentInChildren<CombatHUDHeatSinkPanel>(true); }
        if(instance == null) { instance = CombatHUDHeatSinkPanel.Create(); }
        return instance;
      }
    }
  }
  public class CombatHUDHeatSinkControl :MonoBehaviour {
    public CombatHUDActionButton HeatSinkBtn { get; set; }
    public RectTransform EjectButton { get; set; }
    public RectTransform HeatSinkButton { get; set; }
    public CombatHUDMechTray parent { get; set; }
    public bool ui_inited = false;
    public static readonly string BUTTON_ID = "HEAT_SINK_CONTROL_BUTTON";
    public static readonly string BUTTON_NAME = "__/CAE.HEATSINK_CONTROL.BUTTON/__";
    public static readonly string BUTTON_ICON = "uixSvgIcon_ability_coolantVent";
    public static void RequestResources(LoadRequest loadRequest) {
      Log.Debug?.TWL(0, "CombatHUDHeatSinkControl.RequestResources");
      try {
        if(string.IsNullOrEmpty(BUTTON_ICON)) { return; }
        if(loadRequest.dataManager.Exists(BattleTechResourceType.SVGAsset, BUTTON_ICON)) { return; };
        if(loadRequest.dataManager.ResourceLocator.EntryByID(BUTTON_ICON, BattleTechResourceType.SVGAsset) != null) {
          Log.Debug?.WL(2, "exist in manifest but not loaded");
          loadRequest.AddBlindLoadRequest(BattleTechResourceType.SVGAsset, BUTTON_ICON);
        } else {
          Log.Debug?.WL(2, "not exist in manifest");
        }
      }catch(Exception e) {
        UIManager.logger.LogException(e);
      }
    }
    public void Update() {
      if(ui_inited) { return; }
      if(EjectButton == null) { return; }
      if(HeatSinkBtn == null) { return; }
      if(HeatSinkButton == null) { return; }
      try {
        Vector3 pos = EjectButton.localPosition;
        pos.y -= (EjectButton.sizeDelta.y);
        HeatSinkButton.sizeDelta = EjectButton.sizeDelta;
        HeatSinkButton.localPosition = pos;
        ui_inited = true;
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
    public void ResetMechButtons(AbstractActor actor) {
      if(HeatSinkBtn == null) { return; }
      if(actor == null) { HeatSinkBtn.DisableButton(); return; }
      if(actor.IsShutDown) { HeatSinkBtn.DisableButton(); return; }
      HeatSinkBtn.InitButton(SelectionType.None, null, CustomSvgCache.get(BUTTON_ICON, actor.Combat.DataManager), BUTTON_ID, BUTTON_NAME, actor);
      if(actor.HasActivatedThisRound || !actor.IsAvailableThisPhase || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved) {
        HeatSinkBtn.DisableButton();
      } else {
        HeatSinkBtn.ResetButtonIfNotActive(actor);
      }
    }
    public void Init(CombatHUDMechTray parent, CombatGameState Combat, CombatHUD HUD) {
      this.parent = parent;
      this.HeatSinkBtn?.Init(parent.HUD.Combat, parent.HUD, BTInput.Instance.Key_None(), true);
      ui_inited = false;
    }
    public static CombatHUDHeatSinkControl Instantine(CombatHUDMechTray mechTray) {
      CombatHUDHeatSinkControl result = mechTray.gameObject.AddComponent<CombatHUDHeatSinkControl>();
      try {
        Log.Debug?.TWL(0, "CombatHUDHeatSinkControl.Instantine");
        result.EjectButton = mechTray.gameObject.FindObject<RectTransform>("EjectButton", false);
        if(result.EjectButton == null) { return result; }
        GameObject HeatSinkBtnGO = GameObject.Instantiate(result.EjectButton.gameObject);
        CombatHUDActionButton ejectBtn = result.EjectButton.gameObject.GetComponentInChildren<CombatHUDActionButton>(true);
        if(ejectBtn != null) { ejectBtn.Text.gameObject.SetActive(false); }
        result.HeatSinkButton = HeatSinkBtnGO.GetComponent<RectTransform>();
        HeatSinkBtnGO.transform.SetParent(result.EjectButton.transform.parent);
        HeatSinkBtnGO.transform.SetSiblingIndex(result.EjectButton.transform.GetSiblingIndex() + 1);
        HeatSinkBtnGO.transform.localScale = Vector3.one;
        HeatSinkBtnGO.name = "HeatSinkCtrlButton";
        Transform heatsinkButton_Holder = HeatSinkBtnGO.FindObject<Transform>("ejectButton_Holder", false);
        HBSTooltip tooltip = HeatSinkBtnGO.GetComponentInChildren<HBSTooltip>(true);
        tooltip.defaultStateData.SetString("HEAT CONTROL");
        heatsinkButton_Holder.gameObject.name = "heatSinkButton_Holder";
        result.HeatSinkBtn = heatsinkButton_Holder.gameObject.GetComponentInChildren<CombatHUDActionButton>(true);
        result.HeatSinkBtn.Text.gameObject.SetActive(false);
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        CombatHUD.uiLogger.LogException(e);
      }
      return result;
    }
  }
  [HarmonyPatch(typeof(CombatHUDMechTray))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  public static class CombatHUDMechTray_Init {
    public static void Postfix(CombatHUDMechTray __instance, CombatHUD HUD) {
      CombatHUDHeatSinkControl hctrl = __instance.gameObject.GetComponent<CombatHUDHeatSinkControl>();
      if(hctrl == null) { hctrl = CombatHUDHeatSinkControl.Instantine(__instance); }
      hctrl.Init(__instance, HUD.Combat, HUD);
    }
  }
  [HarmonyPatch(typeof(CombatHUDMechwarriorTray))]
  [HarmonyPatch("ResetMechwarriorButtons")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AbstractActor) })]
  public static class CombatHUDMechwarriorTray_ResetMechwarriorButtons {
    public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor) {
      CombatHUDHeatSinkControl hctrl = __instance.HUD.MechTray.gameObject.GetComponent<CombatHUDHeatSinkControl>();
      if(hctrl == null) { return; }
      hctrl.ResetMechButtons(actor);
    }
  }
  [HarmonyPatch(typeof(CombatHUDTooltipHoverElement))]
  [HarmonyPatch("OnPointerClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(PointerEventData) })]
  public static class CombatHUDTooltipHoverElement_OnPointerClick {
    public static Exception Finalizer(CombatHUDTooltipHoverElement __instance, Exception __exception) {
      if(__exception != null) {
        string name = string.Empty;
        Transform tr = __instance.transform;
        while(tr != null) { name = string.IsNullOrEmpty(name)?tr.name:$"{tr.name}.{name}"; tr = tr.parent; }
        UIManager.logger.LogError($"{name}");
        UIManager.logger.LogException(__exception);
      }
      return null;
    }
  }
  [HarmonyPatch(typeof(CombatHUDTooltipHoverElement))]
  [HarmonyPatch("OnPointerEnter")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(PointerEventData) })]
  public static class CombatHUDTooltipHoverElement_OnPointerEnter {
    public static Exception Finalizer(CombatHUDTooltipHoverElement __instance, Exception __exception) {
      if(__exception != null) {
        string name = string.Empty;
        Transform tr = __instance.transform;
        while(tr != null) { name = string.IsNullOrEmpty(name) ? tr.name : $"{tr.name}.{name}"; tr = tr.parent; }
        UIManager.logger.LogError($"{name}");
        UIManager.logger.LogException(__exception);
      }
      return null;
    }
  }
  [HarmonyPatch(typeof(CombatHUDActionButton))]
  [HarmonyPatch("ExecuteClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDActionButton_ExecuteClick {
    public static void Prefix(ref bool __runOriginal, CombatHUDActionButton __instance) {
      Log.Debug?.WL(0, "CombatHUDActionButton.ExecuteClick " + __instance.GUID);
      try {
        if(__instance.GUID == CombatHUDHeatSinkControl.BUTTON_ID) {
          if(__instance.HUD.SelectedActor.isHasHeat()) {
            try {
              CombatHUDHeatSinkPanel.Instance?.Show(__instance.HUD);
            }catch(Exception e) {
              Log.Error?.TWL(0,e.ToString());
              UIManager.logger.LogException(e);
            }
          }
          __runOriginal = false;
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDActionButton))]
  [HarmonyPatch("InitHoverInfo")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(SelectionType), typeof(Ability), typeof(AbstractActor) })]
  public static class CombatHUDActionButton_InitHoverInfo {
    public static void Postfix(CombatHUDActionButton __instance, SelectionType SelectionType, Ability Ability, AbstractActor actor) {
      Log.Debug?.WL(0, "CombatHUDActionButton.InitHoverInfo " + __instance.GUID);
      try {
        if(__instance.GUID != CombatHUDHeatSinkControl.BUTTON_ID) { return; };
        if(__instance.hoverToolTip != null) {
          __instance.hoverToolTip.BasicString = new Localize.Text("__/CAE.HEAT_PANEL.TOOLTIP.NAME/__");
          __instance.hoverToolTip.BuffStrings.Clear();
          __instance.hoverToolTip.DebuffStrings.Clear();
          __instance.hoverToolTip.BuffStrings.Add(new Localize.Text("__/CAE.HEAT_PANEL.TOOLTIP.DESCRIPTION/__"));
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }

}