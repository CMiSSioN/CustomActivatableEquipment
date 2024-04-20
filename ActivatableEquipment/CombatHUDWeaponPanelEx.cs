using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using CustomAmmoCategoriesPathes;
using CustomComponents;
using CustomComponents.ExtendedDetails;
using HarmonyLib;
using HBS;
using Newtonsoft.Json;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomActivatableEquipment {
  /*public class BonusDescriptionContent {
    public string template { get; set; }
    public string elements { get; set; }
    public BonusDescriptionContent() { template = string.Empty; elements = string.Empty; }

  }
  public class ExtendedDescription {
    public List<BonusDescriptionContent> Traits { get; set; }
    public Dictionary<UnitType, List<BonusDescriptionContent>> CriticalEffects { get; set; }
    public string Description { get; set; }
    public ExtendedDescription() {
      CriticalEffects = new Dictionary<UnitType, List<BonusDescriptionContent>>();
      Traits = new List<BonusDescriptionContent>();
      Description = string.Empty;
    }
  }
  public static class ExtendedDescriptionHelper {
    //public static MethodInfo GetExtendedDescription { get; private set; } = null;
    private delegate string GetExtendedDescriptionInvoker(string defId);
    private static GetExtendedDescriptionInvoker GetExtendedDescriptionDelegate = null;
    public static void DetectMechEngineer() {
      Log.Debug?.TWL(0, "DetectMechEngineer");
      Assembly[] asseblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly assembly in asseblies) {
        Log.WL(1, assembly.FullName);
        if (assembly.FullName.Contains("MechEngineer")) {
          Type helper = assembly.GetType("MechEngineer.Features.OverrideDescriptions.DescriptionsHelper");
          if(helper != null) {
            Log.WL(2, "helper class found");
            MethodInfo method = helper.GetMethod("GetExtendedDescription", BindingFlags.Static | BindingFlags.Public);
            if(method != null) {
              Log.WL(3, "method found");
              var dm = new DynamicMethod("CAEGetExtendedDescription", typeof(string), new Type[] { typeof(string) }, helper);
              var gen = dm.GetILGenerator();
              gen.Emit(OpCodes.Ldarg_0);
              gen.Emit(OpCodes.Call, method);
              gen.Emit(OpCodes.Ret);
              GetExtendedDescriptionDelegate = (GetExtendedDescriptionInvoker)dm.CreateDelegate(typeof(GetExtendedDescriptionInvoker));
            }
          }
          break;
        }
      }
    }
    public static ExtendedDescription GetExtendedDescription(this MechComponent component) {
      if (GetExtendedDescriptionDelegate == null) { return null; }
      string serialData = GetExtendedDescriptionDelegate(component.defId);
      Log.Debug?.TWL(0, "GetExtendedDescription: " + component.defId + ":" + serialData);
      if (string.IsNullOrEmpty(serialData)) { return null; }
      ExtendedDescription obj = JsonConvert.DeserializeObject<ExtendedDescription>(serialData);
      Log.WL(0,JsonConvert.SerializeObject(obj,Formatting.Indented));
      return obj;
    }
  }*/
  public class WeaponSlotsLabelsToggle : EventTrigger {
    private bool hovered = false;
    private bool inited = false;
    public LocalizableText text_WeaponText { get; set; }
    public LocalizableText text_Ammo { get; set; }
    public LocalizableText text_Damage { get; set; }
    public LocalizableText text_HitChance { get; set; }
    public void Init(LocalizableText wt, LocalizableText at, LocalizableText dt, LocalizableText ht) {
      text_WeaponText = wt;
      text_Ammo = at;
      text_Damage = dt;
      text_HitChance = ht;
    }
    public override void OnPointerEnter(PointerEventData data) {
      Log.Debug?.Write("WeaponSlotsLabelsToggle.OnPointerEnter called." + data.position + "\n");
      if (text_WeaponText != null) { text_WeaponText.color = Color.white; };
      if (text_Ammo != null) { text_Ammo.color = Color.white; };
      if (text_Damage != null) { text_Damage.color = Color.white; };
      if (text_HitChance != null) { text_HitChance.color = Color.white; };
      hovered = true;
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.Debug?.Write("WeaponSlotsLabelsToggle.OnPointerExit called." + data.position + "\n");
      if (text_WeaponText != null) { text_WeaponText.color = Color.grey; };
      if (text_Ammo != null) { text_Ammo.color = Color.grey; };
      if (text_Damage != null) { text_Damage.color = Color.grey; };
      if (text_HitChance != null) { text_HitChance.color = Color.grey; };
      hovered = false;
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.Debug?.Write("WeaponSlotsLabelsToggle.OnPointerClick called." + data.position + "\n");
      if (this.hovered) if (CombatHUDWeaponPanelExHelper.panelEx != null) CombatHUDWeaponPanelExHelper.panelEx.Toggle();
      //line.SetText(dataline.ToString(false));
    }
  }
  public class ComponentsPanelHeaderAligner : MonoBehaviour {
    public CombatHUDEquipmentPanel panelEx;
    public RectTransform text_WeaponText;
    public RectTransform text_State;
    public RectTransform text_Charges;
    public RectTransform text_FailChance;
    private bool ui_inited = false;
    public void ResetUI() {
      ui_inited = false;
    }
    public void Init(CombatHUDEquipmentPanel panelEx) {
      this.panelEx = panelEx;
      text_WeaponText = panelEx.label_WeaponText.gameObject.GetComponent<RectTransform>();
      text_State = panelEx.label_State.gameObject.GetComponent<RectTransform>();
      text_Charges = panelEx.label_Charges.gameObject.GetComponent<RectTransform>();
      text_FailChance = panelEx.label_FailChance.gameObject.GetComponent<RectTransform>();
      ui_inited = false;
    }
    public void Update() {
      if (ui_inited) { return; }
      if (panelEx == null) { return; }
      if (panelEx.slots.Count == 0) { return; }
      CombatHUDEquipmentSlotEx slot = panelEx.slots[0];
      if (panelEx.slots[0].ui_inited == false) { return; }
      Log.Debug?.TWL(0, "ComponentsPanelHeaderAligner.Update ui init");
      if (text_WeaponText != null) text_WeaponText.SetPosX(slot.nameText.transform.position.x);
      if (text_State != null) text_State.SetPosX(slot.stateText.transform.position.x);
      if (text_Charges != null) text_Charges.SetPosX(slot.chargesText.transform.position.x);
      if (text_FailChance != null) text_FailChance.SetPosX(slot.failText.transform.position.x + 20f);
      if (text_WeaponText != null) Log.Debug?.WL(1, "text_WeaponText x:" + text_WeaponText.transform.position.x);
      if (text_State != null) Log.Debug?.WL(1, "text_State x:" + text_State.transform.position.x);
      if (text_Charges != null) Log.Debug?.WL(1, "text_Charges x:" + text_Charges.transform.position.x);
      if (text_FailChance != null) Log.Debug?.WL(1, "text_FailChance x:" + text_FailChance.transform.position.x);
      ui_inited = true;
    }
  }
  public class CombatHUDEquipmentPanel : EventTrigger {
    public static CombatHUDEquipmentPanel Instance { get; set; } = null;
    public List<CombatHUDEquipmentSlotEx> slots;
    private Dictionary<CombatHUDSidePanelHoverElement, CombatHUDEquipmentSlotEx> clickReceivers = new Dictionary<CombatHUDSidePanelHoverElement, CombatHUDEquipmentSlotEx>();
    public CombatHUD HUD { get; private set; }
    public CombatHUDWeaponPanel weaponPanel { get; private set; }
    public LocalizableText label_WeaponText { get; set; }
    public LocalizableText label_State { get; set; }
    public LocalizableText label_Charges { get; set; }
    public LocalizableText label_FailChance { get; set; }
    private List<CombatHUDEquipmentSlotEx> operatinalSlots { get; set; } = new List<CombatHUDEquipmentSlotEx>();
    public float TimeToLoad = 0.3f;
    public float TimeToUnload = 0.15f;
    private float timeInCurrentState;
    private WPState state { get; set; }
    public void Hide() {
      foreach (CombatHUDEquipmentSlotEx slot in operatinalSlots) {
        slot.Hide();
      }
    }
    public void Show() {
      foreach (CombatHUDEquipmentSlotEx slot in operatinalSlots) {
        slot.RealState = true;
        slot.RefreshComponent();
      }
    }
    public void Toggle() {
      switch (state) {
        case WPState.Loaded:
        case WPState.Loading:
          timeInCurrentState = 0f;
          state = WPState.Unloading;
          break;
        case WPState.Off:
        case WPState.Unloading:
          timeInCurrentState = 0f;
          state = WPState.Loading;
          break;
      }
    }
    public void ProcessOnPointerClick(CombatHUDSidePanelHoverElement hover) {
      if (clickReceivers.ContainsKey(hover)) { clickReceivers[hover].OnPointerClick(null); }
    }
    public override void OnPointerEnter(PointerEventData data) {
      Log.Debug?.Write("CombatHUDEquipmentPanel.OnPointerEnter called." + data.position + "\n");
      if (label_WeaponText != null) { label_WeaponText.color = Color.white; };
      if (label_State != null) { label_State.color = Color.white; };
      if (label_Charges != null) { label_Charges.color = Color.white; };
      if (label_FailChance != null) { label_FailChance.color = Color.white; };
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.Debug?.Write("CombatHUDEquipmentPanel.OnPointerExit called." + data.position + "\n");
      if (label_WeaponText != null) { label_WeaponText.color = Color.grey; };
      if (label_State != null) { label_State.color = Color.grey; };
      if (label_Charges != null) { label_Charges.color = Color.grey; };
      if (label_FailChance != null) { label_FailChance.color = Color.grey; };
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.Debug?.Write("CombatHUDEquipmentPanel.OnPointerClick called." + data.position + "\n");
      Toggle();
      //if (CombatHUDWeaponPanelExHelper.panelEx != null) CombatHUDWeaponPanelExHelper.panelEx.Toggle();
      //line.SetText(dataline.ToString(false));
    }
    private static void DestroyRec(GameObject obj) {
      foreach (Transform child in obj.transform) {
        CombatHUDEquipmentPanel.DestroyRec(child.gameObject);
      }
      GameObject.Destroy(obj);
    }
    public static void Clear() {
      if (CombatHUDEquipmentPanel.Instance != null) {
        GameObject tmp = CombatHUDEquipmentPanel.Instance.gameObject;
        CombatHUDEquipmentPanel.Instance = null;
        CombatHUDEquipmentPanel.DestroyRec(tmp);
      }
    }
    public static void Init(CombatHUD HUD, CombatHUDWeaponPanel weaponPanel) {
      if (Instance != null) { CombatHUDEquipmentPanel.Clear(); };
      Transform labels = weaponPanel.gameObject.transform.Find("wp_Labels");
      if (labels != null) {
        GameObject labels_ex = GameObject.Instantiate(labels.gameObject);
        WeaponSlotsLabelsToggle toggle = labels_ex.gameObject.GetComponent<WeaponSlotsLabelsToggle>();
        if (toggle != null) { GameObject.Destroy(toggle); }
        Instance = labels_ex.AddComponent<CombatHUDEquipmentPanel>();
        Instance.HUD = HUD;
        Instance.weaponPanel = weaponPanel;
        Instance.slots = new List<CombatHUDEquipmentSlotEx>();
        labels_ex.transform.SetParent(weaponPanel.transform);
        labels_ex.transform.localScale = new Vector3(1f, 1f, 1f);
        labels_ex.SetActive(true);
        Instance.label_WeaponText = labels_ex.transform.Find("text_WeaponText").gameObject.GetComponent<LocalizableText>();
        Instance.label_State = labels_ex.transform.Find("text_Ammo").gameObject.GetComponent<LocalizableText>();
        Instance.label_Charges = labels_ex.transform.Find("text_Damage").gameObject.GetComponent<LocalizableText>();
        Instance.label_FailChance = labels_ex.transform.Find("text_HitChance").gameObject.GetComponent<LocalizableText>();
        GameObject text_Mode = labels_ex.transform.Find("text_Mode").gameObject;
        if (text_Mode != null) { GameObject.Destroy(text_Mode); }
        GameObject text_AmmoName = labels_ex.transform.Find("text_AmmoName").gameObject;
        if (text_AmmoName != null) { GameObject.Destroy(text_AmmoName); }
        Instance.timeInCurrentState = 0f;
        Instance.state = WPState.Off;
        Instance.label_WeaponText.SetText("__/CAE.COMPONENT_LABEL/__");
        Instance.label_State.SetText("__/CAE.COMPONENT_STATE/__");
        Instance.label_Charges.SetText("__/CAE.COMPONENT_CHARGES/__");
        Instance.label_FailChance.SetText("__/CAE.COMPONENT_FAIL/__");
        ComponentsPanelHeaderAligner headerAligner = Instance.GetComponent<ComponentsPanelHeaderAligner>();
        if (headerAligner == null) { headerAligner = Instance.gameObject.AddComponent<ComponentsPanelHeaderAligner>(); }
        headerAligner.Init(Instance);
      }
    }
    public void InitDisplayedEquipment(AbstractActor unit) {
      operatinalSlots.Clear();
      if (unit == null) { return; }
      Log.Debug?.TWL(0, "CombatHUDEquipmentPanel.InitDisplayedEquipment unit:" + new Localize.Text(unit.DisplayName).ToString() + " pilot:" + unit.GetPilot().pilotDef.Description.Id);
      HashSet<MechComponent> acomps = new HashSet<MechComponent>();
      foreach (Ability ability in unit.ComponentAbilities) {
        acomps.Add(ability.parentComponent);
      }
      List<MechComponent> ncomps = new List<MechComponent>();
      Dictionary<string, List<MechComponent>> stackComps = new Dictionary<string, List<MechComponent>>();
      foreach (MechComponent component in unit.allComponents) {
        Log.Debug?.WL(1, "component:" + component.defId);
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (acomps.Contains(component)) { Log.Debug?.WL(2, "have ability"); ncomps.Add(component); continue; };
        if (activatable == null) { Log.Debug?.WL(2, "not activatable"); continue; }
        if (activatable.HideInUI) { Log.Debug?.WL(2, "should be hidden in UI"); continue; }
        if(component.IsFunctional == false){ Log.Debug?.WL(2, "not functional"); }
        if (activatable.CanBeactivatedManualy) {
          Log.Debug?.WL(2, "can be activated manualy"); ncomps.Add(component);
        } else {
          if((activatable.AutoActivateOnHeat > Core.Epsilon)||(activatable.AutoActivateOnOverheatLevel > Core.Epsilon)) {
            if(stackComps.TryGetValue(component.defId, out List<MechComponent> actComps) == false) {
              actComps = new List<MechComponent>();
              stackComps.Add(component.defId, actComps);
            }
            actComps.Add(component);
          }
        }
        //if (ActivatableComponent.isComponentActivated(component)) { Log.Debug?.WL(2, "activated"); ncomps.Add(component); continue; };
        //if (activatable.CanBeactivatedManualy) { Log.Debug?.WL(2, "can be activated manualy"); ncomps.Add(component); continue; };
        //if (activatable.AutoActivateOnHeat > Core.Epsilon) { Log.Debug?.WL(2, "activate by heat"); ncomps.Add(component); continue; }
      }
      int desiredSlots = ncomps.Count + stackComps.Count;
      for (int index = 0; index < desiredSlots; ++index) {
        if (index >= slots.Count) {
          CombatHUDEquipmentSlotEx nslot = CombatHUDEquipmentSlotEx.Init(HUD, weaponPanel, this);
          if (nslot != null) clickReceivers.Add(nslot.hoverSidePanel, nslot);
        }
        if ((index < slots.Count)&&(index < ncomps.Count)) {
          operatinalSlots.Add(slots[index]);
          slots[index].Init(ncomps[index], 1);
          slots[index].RealState = true;
        }
      }
      int slot_index = ncomps.Count;
      foreach (var stackSlot in stackComps) {
        if ((slot_index < slots.Count)) {
          operatinalSlots.Add(slots[slot_index]);
          slots[slot_index].Init(stackSlot.Value[0], stackSlot.Value.Count);
          slots[slot_index].RealState = true;
        }
        ++slot_index;
      }
      for (int index = desiredSlots; index < slots.Count; ++index) {
        slots[index].Hide();
      }
      CombatHUDEquipmentSlotEx.Clear();
      timeInCurrentState = 0f;
      state = WPState.Loading;
      RefreshDisplayedEquipment(unit);
    }
    public void RefreshDisplayedEquipment(AbstractActor unit) {
      //Log.Debug?.TWL(0, "CombatHUDEquipmentPanel.RefreshDisplayedEquipment");
      if (unit == null) { return; }
      foreach (CombatHUDEquipmentSlotEx slot in slots) {
        slot.RefreshComponent();
      }
    }
    public void ShowWeaponsUpTo(int count) {
      if (count > this.operatinalSlots.Count) { count = this.operatinalSlots.Count; }
      for (int index = 0; index < count; ++index) {
        this.operatinalSlots[index].RealState = true;
        this.operatinalSlots[index].Show();
      }
      for (int index = count; index < this.operatinalSlots.Count; ++index) {
        this.operatinalSlots[index].Hide();
      }
    }
    private void SetState(WPState newState) {
      if (this.state == newState)
        return;
      this.state = newState;
      this.timeInCurrentState = 0.0f;
      switch (newState) {
        case WPState.Off:
          this.ShowWeaponsUpTo(0);
          break;
        case WPState.Loaded:
          this.ShowWeaponsUpTo(this.operatinalSlots.Count);
          break;
      }
    }
    private void Update() {
      this.timeInCurrentState += Time.unscaledDeltaTime;
      switch (this.state) {
        case WPState.Loading:
          if ((double)this.timeInCurrentState > (double)this.TimeToLoad) {
            this.SetState(WPState.Loaded);
            break;
          }
          break;
        case WPState.Unloading:
          if ((double)this.timeInCurrentState > (double)this.TimeToUnload) {
            this.SetState(WPState.Off);
            break;
          }
          break;
      }
      switch (this.state) {
        case WPState.Loading:
          this.ShowWeaponsUpTo((int)((double)this.operatinalSlots.Count * (double)(this.timeInCurrentState / this.TimeToLoad)));
          break;
        case WPState.Unloading:
          this.ShowWeaponsUpTo((int)((double)this.operatinalSlots.Count * (1.0 - (double)this.timeInCurrentState / (double)this.TimeToLoad)));
          break;
      }
    }
  }
  public class CombatHUDEqupmentSlotHeat: MonoBehaviour {
    public CombatHUDEquipmentSlotEx parent { get; set; } = null;
    public RectTransform parent_rect { get; set; } = null;
    public RectTransform background_rect { get; set; } = null;
    public SVGImage background_image { get; set; } = null;
    public RectTransform heatbar_rect { get; set; } = null;
    public SVGImage heatbar_image { get; set; } = null;
    public RectTransform activate_rect { get; set; } = null;
    public SVGImage activate_image { get; set; } = null;
    public RectTransform deactivate_rect { get; set; } = null;
    public SVGImage deactivate_image { get; set; } = null;
    public void Init(CombatHUDEquipmentSlotEx parent) {
      this.parent = parent;
      parent_rect = parent.mainImage.gameObject.GetComponent<RectTransform>();

      GameObject heatbar_go = GameObject.Instantiate(parent.mainImage.gameObject.transform.Find("check_Image").gameObject);
      heatbar_go.name = "heat_bar";
      heatbar_go.GetComponent<LayoutElement>().ignoreLayout = true;
      heatbar_go.transform.SetParent(this.gameObject.transform);
      heatbar_image = heatbar_go.GetComponent<SVGImage>();
      CustomSvgCache.setIcon(heatbar_image, "activatable_heat", parent.HUD.Combat.DataManager);
      heatbar_rect = heatbar_go.GetComponent<RectTransform>();
      heatbar_rect.sizeDelta = new Vector2(parent_rect.sizeDelta.x, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize);
      heatbar_go.transform.localScale = Vector3.one;
      heatbar_go.transform.localPosition = new Vector3(0f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize - parent_rect.sizeDelta.y, 0f);
      heatbar_go.transform.localRotation = Quaternion.identity;
      heatbar_rect.pivot = new Vector2(0f, 1f);

      GameObject activate_go = GameObject.Instantiate(parent.mainImage.gameObject.transform.Find("check_Image").gameObject);
      activate_go.name = "activate_bar";
      activate_go.GetComponent<LayoutElement>().ignoreLayout = true;
      activate_go.transform.SetParent(this.gameObject.transform);
      activate_image = activate_go.GetComponent<SVGImage>();
      CustomSvgCache.setIcon(activate_image, "activatable_heat", parent.HUD.Combat.DataManager);
      activate_rect = activate_go.GetComponent<RectTransform>();
      activate_rect.sizeDelta = new Vector2(parent_rect.sizeDelta.x / 100f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize);
      activate_go.transform.localScale = Vector3.one;
      activate_go.transform.localPosition = new Vector3(0f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize - parent_rect.sizeDelta.y, 0f);
      activate_go.transform.localRotation = Quaternion.identity;
      activate_rect.pivot = new Vector2(0.5f, 1f);

      GameObject deactivate_go = GameObject.Instantiate(parent.mainImage.gameObject.transform.Find("check_Image").gameObject);
      deactivate_go.name = "activate_bar";
      deactivate_go.GetComponent<LayoutElement>().ignoreLayout = true;
      deactivate_go.transform.SetParent(this.gameObject.transform);
      deactivate_image = deactivate_go.GetComponent<SVGImage>();
      CustomSvgCache.setIcon(deactivate_image, "activatable_heat", parent.HUD.Combat.DataManager);
      deactivate_rect = deactivate_go.GetComponent<RectTransform>();
      deactivate_rect.sizeDelta = new Vector2(parent_rect.sizeDelta.x / 100f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize);
      deactivate_go.transform.localScale = Vector3.one;
      deactivate_go.transform.localPosition = new Vector3(0f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize - parent_rect.sizeDelta.y, 0f);
      deactivate_go.transform.localRotation = Quaternion.identity;
      deactivate_rect.pivot = new Vector2(0.5f, 1f);

      GameObject background_go = GameObject.Instantiate(parent.mainImage.gameObject.transform.Find("check_Image").gameObject);
      background_go.name = "heat_background";
      background_go.GetComponent<LayoutElement>().ignoreLayout = true;
      background_go.transform.SetParent(this.gameObject.transform);
      background_image = background_go.GetComponent<SVGImage>();
      CustomSvgCache.setIcon(background_image, "activatable_heat_border", parent.HUD.Combat.DataManager);
      background_rect = background_go.GetComponent<RectTransform>();
      background_rect.sizeDelta = new Vector2(parent_rect.sizeDelta.x, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize);
      background_go.transform.localScale = Vector3.one;
      background_go.transform.localPosition = new Vector3(0f, parent_rect.sizeDelta.y * Core.Settings.componentHeatBarSize - parent_rect.sizeDelta.y, 0f);
      background_go.transform.localRotation = Quaternion.identity;
      background_rect.pivot = new Vector2(0f, 1f);
    }
    public void UIInit() {
      Vector2 size = background_rect.sizeDelta;
      size.x = parent_rect.sizeDelta.x;
      background_rect.sizeDelta = size;
      background_image.color = Core.Settings.componentHeatBarBorderColor.color;
      activate_image.color = Core.Settings.componentHeatBarActivateColor.color;
      deactivate_image.color = Core.Settings.componentHeatBarDeactivateColor.color;
    }

    public void UpdateHeatBounds() {
      float activateSize = 1f;
      float deactivateSize = 1f;
      if (this.parent.component.parent is Mech mech) {
        if (this.parent.activeDef.AutoActivateOnOverheatLevel > Core.Epsilon) {
          activateSize = this.parent.activeDef.AutoActivateOnOverheatLevel * mech.OverheatLevel / mech.MaxHeat;
        } else if (this.parent.activeDef.AutoActivateOnHeat > Core.Epsilon) {
          activateSize = this.parent.activeDef.AutoActivateOnHeat / mech.MaxHeat;
        }
        if (this.parent.activeDef.AutoDeactivateOverheatLevel > Core.Epsilon) {
          deactivateSize = this.parent.activeDef.AutoDeactivateOverheatLevel * mech.OverheatLevel / mech.MaxHeat;
        } else if (this.parent.activeDef.AutoDeactivateOnHeat > Core.Epsilon) {
          deactivateSize = this.parent.activeDef.AutoDeactivateOnHeat / mech.MaxHeat;
        }
        Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.UIInit " + this.parent.component.defId);
        Log.Debug?.WL(1, "AutoActivateOnOverheatLevel:" + this.parent.activeDef.AutoActivateOnOverheatLevel);
        Log.Debug?.WL(1, "AutoDeactivateOverheatLevel:" + this.parent.activeDef.AutoDeactivateOverheatLevel);
        Log.Debug?.WL(1, "AutoActivateOnHeat:" + this.parent.activeDef.AutoActivateOnHeat);
        Log.Debug?.WL(1, "activateSize:" + activateSize);
        Log.Debug?.WL(1, "deactivateSize:" + deactivateSize);
        Log.Debug?.WL(1, "OverheatLevel:" + mech.OverheatLevel);
        Log.Debug?.WL(1, "MaxHeat:" + mech.MaxHeat);
      }
      Vector2 size = background_rect.sizeDelta;
      activate_rect.localPosition = new Vector3(size.x * activateSize, activate_rect.localPosition.y, 0f);
      deactivate_rect.localPosition = new Vector3(size.x * deactivateSize, deactivate_rect.localPosition.y, 0f);
    }
    public CombatHUDHeatMeter HeatMeter { get; set; }
    public void Update() {
      Vector2 size = background_rect.sizeDelta;
      if (parent.component.parent is Mech mech) {
        if (mech.isHasHeat()) {
          size.x = parent_rect.sizeDelta.x * ((float)CombatHUDHeatDisplay.GetProjectedHeat(mech,this.parent.HUD) / (float)mech.MaxHeat);
          if (size.x < 0f) { size.x = 0f; }
          heatbar_rect.sizeDelta = size;
          //Color color = this.parent.HUD.MechTray.ActorInfo.HeatDisplay.PredictiveBarHighFlash.color;
          //color.a = 0.5f;
          heatbar_image.color = this.parent.HUD.MechTray.ActorInfo.HeatDisplay.PredictiveBarHighFlash.color;
        }
      }
    }
  }
  public class CombatHUDEquipmentSlotEx : EventTrigger {
    public int ComponentsCount { get; set; } = 1;
    public CombatHUDWeaponPanelEx panelEx { get; set; }
    public CombatHUD HUD { get; private set; }
    public CombatHUDWeaponPanel weaponPanel { get; private set; }
    public CombatHUDEquipmentPanel equipPanel { get; private set; }
    public CombatHUDEqupmentSlotHeat heatMeter { get; private set; }
    public MechComponent component { get; private set; }
    public ActivatableComponent activeDef { get; private set; }
    public LocalizableText nameText { get; set; }
    public LocalizableText stateText { get; set; }
    public LocalizableText chargesText { get; set; }
    public LocalizableText failText { get; set; }
    public SVGImage mainImage { get; set; }
    public SVGImage checkImage { get; set; }
    public CombatHUDSidePanelHoverElement hoverSidePanel { get; set; }
    public List<CombatHUDEquipmentSlot> buttons { get; set; }
    public List<Ability> abilities { get; set; }
    public bool RealState { get; set; }
    private bool hovered { get; set; }
    public bool ui_inited { get; private set; }
    public bool useProjectedHeat { get; private set; } = false;
    private bool isNeedFlashing() {
      if (component == null) { return false; }
      if (activeDef == null) { return false; }
      if (activeDef.ChargesCount != 0) { return false; }
      if (component.isActive() == false) { return false; }
      if(activeDef.CanBeactivatedManualy == false) {
        if(activeDef.FailRoundsStart > 0) {
          int activeRounds = ActivatableComponent.getComponentActiveRounds(component);
          if(activeRounds < activeDef.FailRoundsStart) { return false; }
        }
      }
      if (component.FailChance() < Core.Settings.equipmentFlashFailChance) { return false; }
      return true;
    }
    private bool background_flashing { get; set; }
    private float flashSpeedCurrent;
    private float flashT;
    private UILookAndColorConstants LookAndColorConstants { get; set; }
    private void ShowTextColor(Color color, Color failChanceColor) {
      this.nameText.color = color;
      this.stateText.color = color;
      this.failText.color = failChanceColor;
      this.chargesText.color = color;
    }
    private void RefreshHighlighted() {
      background_flashing = false;
      flashSpeedCurrent = 2f;
      flashT = 0f;
      if (this.component.IsFunctional == false) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor, this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor);
        return;
      } else
      if ((activeDef == null) || (HUD.SelectedTarget != null) || ActivatableComponent.isOutOfCharges(component) || (component.parent.IsAvailableThisPhase == false) || (component.parent.HasMovedThisRound)) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor, this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor);
        return;
      }
      this.HUD.PlayAudioEvent(AudioEventList_ui.ui_weapon_hover);
      this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.HighlightedBGColor;
      this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.HighlightedTextColor, this.LookAndColorConstants.WeaponSlotColors.HighlightedTextColor);
    }
    private Color GetFailTextColor(float failChance) {
      if (failChance <= 15f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorA; }
      if (failChance <= 30f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorB; }
      if (failChance <= 50f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorC; }
      return this.LookAndColorConstants.WeaponSlotColors.qualityColorD;
    }
    private void RefreshNonHighlighted() {
      if (this.component.IsFunctional == false) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor, this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor);
        return;
      } else
      if ((activeDef == null) || (HUD.SelectedTarget != null) || ActivatableComponent.isOutOfCharges(component) || (component.parent.IsAvailableThisPhase == false) || (component.parent.HasMovedThisRound)) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor, this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor);
        return;
      }
      background_flashing = isNeedFlashing();
      if (background_flashing == false) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      }
      this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.AvailableTextColor, this.GetFailTextColor(this.failChance));
    }
    public void UIInit() {
      if (panelEx == null) { return; }
      if (panelEx.isUIInted == false) { return; }
      if (panelEx.panelBackground == null) { return; }
      if (panelEx.slotsEx[0].isUIInited == false) { return; }
      if (panelEx.slotsEx[0].modeHover.ui_inited == false) { return; }
      if (panelEx.slotsEx[0].ammoHover.ui_inited == false) { return; }
      RectTransform tr = this.nameText.transform.parent.GetComponent<RectTransform>();
      if (tr == null) { return; }
      float width = tr.worldWidth();
      float destWidth = panelEx.panelBackground.worldWidth() - 50f;
      Vector2 size = tr.sizeDelta;
      float widthMod = destWidth / width;
      size.x = size.x * (widthMod);
      tr.sizeDelta = size;
      this.heatMeter.UIInit();
      //float activateSize = 1f;
      //float deactivateSize = 1f;
      //if (this.component.parent is Mech mech) {
      //  if (this.activeDef.AutoActivateOnOverheatLevel > Core.Epsilon) {
      //    activateSize = this.activeDef.AutoActivateOnOverheatLevel * mech.OverheatLevel / mech.MaxHeat;
      //  } else if (this.activeDef.AutoActivateOnHeat > Core.Epsilon) {
      //    activateSize = this.activeDef.AutoActivateOnHeat / mech.MaxHeat;
      //  }
      //  if (this.activeDef.AutoDeactivateOverheatLevel > Core.Epsilon) {
      //    deactivateSize = this.activeDef.AutoDeactivateOverheatLevel * mech.OverheatLevel / mech.MaxHeat;
      //  } else if (this.activeDef.AutoDeactivateOnHeat > Core.Epsilon) {
      //    deactivateSize = this.activeDef.AutoDeactivateOnHeat / mech.MaxHeat;
      //  }
      //  Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.UIInit " + this.component.defId);
      //  Log.Debug?.WL(1, "AutoActivateOnOverheatLevel:"+ this.activeDef.AutoActivateOnOverheatLevel);
      //  Log.Debug?.WL(1, "AutoDeactivateOverheatLevel:" + this.activeDef.AutoDeactivateOverheatLevel);
      //  Log.Debug?.WL(1, "AutoActivateOnHeat:" + this.activeDef.AutoActivateOnHeat);
      //  Log.Debug?.WL(1, "activateSize:" + activateSize);
      //  Log.Debug?.WL(1, "deactivateSize:" + deactivateSize);
      //  Log.Debug?.WL(1, "OverheatLevel:" + mech.OverheatLevel);
      //  Log.Debug?.WL(1, "MaxHeat:" + mech.MaxHeat);
      //}
      //heatBackground.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, size.y/2f);
      //heatActivateBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(activateSize * size.x, size.y/2f);
      //heatDeactivateBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(deactivateSize * size.x, size.y/2f);
      //heatBackground.GetComponent<SVGImage>().color = new Color(1f, 1f, 1f, 0.5f);
      //Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.UIInit color:"+ heatBackground.GetComponent<SVGImage>().color);
      ui_inited = true;
    }
    //private RectTransform heatBackgroundRT = null;
    //private RectTransform backgroundRT = null;
    public void Update() {
      if (ui_inited == false) { UIInit(); }
      if (background_flashing) {
        flashT += Time.deltaTime * flashSpeedCurrent;
        if (flashT > 1f) { flashT = 1f; flashSpeedCurrent = -2f; }
        if (flashT < 0f) { flashT = 0f; flashSpeedCurrent = 2f; }
        this.mainImage.color = Color.Lerp(this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor, Color.red, flashT);
      }
      if (T < 0f) { T = 0f; }
      if (T > 0f) { T -= Time.deltaTime; }
      //if (heatBackgroundRT == null) {
      //  heatBackgroundRT = heatBackground.GetComponent<RectTransform>();
      //}
      //if (backgroundRT == null) {
      //  backgroundRT = this.mainImage.gameObject.GetComponent<RectTransform>();
      //}
      //if (this.component.parent is Mech mech) {
      //  int heat = CombatHUDHeatDisplay.GetProjectedHeat(mech, this.HUD);
      //  float heatSize = (float)heat / (float)mech.MaxHeat;
      //  heatBackgroundRT.sizeDelta = new Vector2(backgroundRT.sizeDelta.x * heatSize, backgroundRT.sizeDelta.y / 2f);
      //}
    }
    public override void OnPointerEnter(PointerEventData eventData) {
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerEnter " + component.defId);
      hovered = true;
      this.RefreshHighlighted();
    }
    public override void OnPointerExit(PointerEventData eventData) {
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerExit " + component.defId);
      hovered = false;
      this.RefreshNonHighlighted();
    }
    public float T = 0f;
    public static float DEMULTIPICATOR = 3f;
    public override void OnPointerClick(PointerEventData eventData) {
      Log.Debug?.TWL(0, $"CombatHUDEquipmentSlotEx.OnPointerClick demultipicator:{T}");
      if (T > 0f) { return; }
      T = DEMULTIPICATOR;
      try {
        if (activeDef == null) { return; }
        if (component == null) { return; }
        if (activeDef.CanBeactivatedManualy == false) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} can't be activated manually", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        if (HUD.SelectedTarget != null) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} can't be activated while target is selected", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        if (HUD.SelectionHandler.ActiveState is SelectionStateJump) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} can't be activated while jumping", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        if (component.IsFunctional == false) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} is not functional", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) {
          return;
        }
        if (activatable.CanActivateAfterMove == false) {
          if (component.parent.HasMovedThisRound) {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("unit already moved"), FloatieMessage.MessageNature.Neutral));
            return;
          }
        }
        if (activatable.CanActivateAfterFire == false) {
          if (component.parent.HasFiredThisRound) {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("unit already fired"), FloatieMessage.MessageNature.Neutral));
            return;
          }
        }
        if (ActivatableComponent.isOutOfCharges(component)) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} out of charges", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        if (component.parent.IsAvailableThisPhase == false) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("unit is not available this phase"), FloatieMessage.MessageNature.Neutral));
          return;
        }
        if (component.parent.BlockComponentsActivation()) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("components activations is blocked"), FloatieMessage.MessageNature.Neutral));
          return;
        }
        int activatedRound = ActivatableComponent.getComponentActivedRound(component);
        int currentRound = component.parent.Combat.TurnDirector.CurrentRound;
        if (activeDef.ActivateOncePerRound && (activatedRound != currentRound) && (activatedRound >= 0)) {
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, new Localize.Text("{0} can only be activated once per round", activeDef.ButtonName), FloatieMessage.MessageNature.Neutral));
          return;
        }
        Log.Debug?.WL(1, $"Toggle activatable {component.defId}");
        ActivatableComponent.toggleComponentActivation(this.component);
        equipPanel.RefreshDisplayedEquipment(component.parent);
        Log.Debug?.TWL(0, $"CombatHUDEquipmentSlotEx.OnPointerClick success");
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
    public static Dictionary<MechComponent, string> stateCache = new Dictionary<MechComponent, string>();
    public static Dictionary<MechComponent, float> failCache = new Dictionary<MechComponent, float>();
    public static void Clear() { stateCache.Clear(); failCache.Clear(); }
    public static void ClearCache(MechComponent component) { stateCache.Remove(component); failCache.Remove(component); }
    public static float FailChance(MechComponent component) {
      if (failCache.TryGetValue(component, out float fail)) { return fail; }
      fail = ActivatableComponent.getEffectiveComponentFailChance(component);
      failCache.Add(component, fail);
      return fail;
    }
    public static string GetState(MechComponent component, ActivatableComponent activDef) {
      if (stateCache.TryGetValue(component, out string state)) { return state; }
      if (ActivatableComponent.isOutOfCharges(component)) {
        state = "__/CAE.OutOfCharges/__";
      } else {
        if (activDef.ChargesCount != 0) {
          state = "__/CAE.OPERATIONAL/__";
        } else {
          if (ActivatableComponent.isComponentActivated(component)) {
            state = activDef.ActivationMessage;
          } else {
            state = activDef.DeactivationMessage;
          }
        }
      }
      stateCache.Add(component, state);
      return state;
    }
    public static string InterpolateUIName(MechComponent component) {
      return component.UIName.ToString().Replace("{location}", component.parent.GetAbbreviatedChassisLocation(component.Location));
      //return component.UIName.ToString();
    }

    public float failChance { get; private set; }
    public float shownFailChance { get; private set; } = float.NaN;
    public void updateFailChance() {
      if ((this.useProjectedHeat == false)||(this.HUD.MechTray.HeatMeterHolder.activeInHierarchy == false)) {
        if (this.shownFailChance != failChance) {
          this.shownFailChance = failChance;
          this.failText.SetText(string.Format("{0}%", this.failChance));
        }
        return;
      }
    }
    //public bool TestPredictedHeat(out bool isActivated) {
    //  isActivated = ActivatableComponent.isComponentActivated(this.component);
    //  if (useProjectedHeat == false) { return false; }
    //  if (this.component.parent is Mech mech) {
    //    int projectedHeat = CombatHUDHeatDisplay.GetProjectedHeat(mech, this.HUD);
    //  }
    //}
    public void RefreshComponent() {
      this.Show();
      if (hovered == false) { RefreshNonHighlighted(); } else { RefreshHighlighted(); };
      nameText.SetText(InterpolateUIName(component) + (this.ComponentsCount>1?("x"+ComponentsCount.ToString()):""));
      if ((component.IsFunctional == false) || (activeDef == null)) {
        chargesText.SetText("--");
        stateText.SetText("--");
        failText.SetText("--");
      } else {
        if (activeDef.ChargesCount == -1) { chargesText.SetText("UNL"); } else
        if (activeDef.ChargesCount == 0) { chargesText.SetText("--"); } else {
          chargesText.SetText(ActivatableComponent.getChargesCount(component).ToString());
        }
        string state = CombatHUDEquipmentSlotEx.GetState(component, activeDef);
        stateText.SetText(state);
        this.failChance = Mathf.Round(CombatHUDEquipmentSlotEx.FailChance(component) * 100.0f);
        if(activeDef.CanBeactivatedManualy == false) { 
          int activeRounds = ActivatableComponent.getComponentActiveRounds(component);
          if(activeRounds < activeDef.FailRoundsStart) {
            this.failChance = 0f;
          }
        }
        this.failText.SetText(string.Format("{0}%", this.failChance));
      }
      AbstractActor actor = component.parent;
      bool forceInactive = actor.HasActivatedThisRound || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved;
      for (int index = 0; index < abilities.Count; ++index) {
        CombatHUDEquipmentSlotEx.ResetAbilityButton(actor, (CombatHUDActionButton)buttons[index], abilities[index], forceInactive);
      }
    }
    public void Show() {
      if (RealState) {
        //Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.Show "+component.defId+" abilities:"+abilities.Count);
        if(this.gameObject.activeInHierarchy == false) {
          background_flashing = isNeedFlashing();
          flashSpeedCurrent = 2f;
          flashT = 0f;
        }
        this.gameObject.SetActive(true);
        //this.gameObject.transform.parent.gameObject.SetActive(true);
        for (int index = 0; index < abilities.Count; ++index) {
          buttons[index].transform.parent.gameObject.SetActive(true);
          buttons[index].gameObject.SetActive(true);
        }
      }
    }
    public void Hide() {
      RealState = false;
      //this.abilities.Clear();
      this.gameObject.SetActive(false);
      //this.gameObject.transform.parent.gameObject.SetActive(false);
      foreach (CombatHUDEquipmentSlot button in buttons) {
        button.transform.parent.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
      }
      background_flashing = false;
      flashSpeedCurrent = 2f;
      flashT = 0f;
    }
    public static void ResetAbilityButton(AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive) {
      if (ability == null)
        return;
      if (forceInactive)
        button.DisableButton();
      else if (button.IsAbilityActivated)
        button.ResetButtonIfNotActive(actor);
      else if (!ability.IsAvailable) {
        button.DisableButton();
      } else {
        bool flag1 = false;
        bool flag2 = false;
        bool flag3 = ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
        if (actor.HasActivatedThisRound || !actor.IsAvailableThisPhase || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved)
          button.DisableButton();
        else if (actor.IsShutDown) {
          if (!flag1)
            button.DisableButton();
          else
            button.ResetButtonIfNotActive(actor);
        } else if (actor.IsProne) {
          if (!flag2)
            button.DisableButton();
          else
            button.ResetButtonIfNotActive(actor);
        } else if ((actor.HasFiredThisRound || !actor.Combat.TurnDirector.IsInterleaved) && ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring)
          button.DisableButton();
        else if (actor.HasMovedThisRound) {
          if (flag3)
            button.ResetButtonIfNotActive(actor);
          else
            button.DisableButton();
        } else
          button.ResetButtonIfNotActive(actor);
      }
    }
    public void Init(MechComponent component, int count) {
      ComponentsCount = count;
      useProjectedHeat = false;
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if(activatable != null) {
        if (activatable.CanBeactivatedManualy == false) {
          if(activatable.AutoActivateOnHeat > Core.Epsilon || activatable.AutoActivateOnOverheatLevel > Core.Epsilon) {
            useProjectedHeat = true;
          }
        }
      }
      if (component.parent.isHasHeat() == false) { useProjectedHeat = false; }
      this.heatMeter.gameObject.SetActive(useProjectedHeat);
      //this.heatBackground.SetActive(useProjectedHeat);
      //this.heatActivateBorder.SetActive(useProjectedHeat);
      //this.heatDeactivateBorder.SetActive(useProjectedHeat);
      hovered = false;
      ui_inited = false;
      this.component = component;
      this.activeDef = component.componentDef.GetComponent<ActivatableComponent>();
      this.hoverSidePanel.Title = new Localize.Text(component.Description.UIName + (ComponentsCount > 1 ? " x" + ComponentsCount.ToString() : ""));
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.Init "+component.defId);
      ExtendedDetails extDescr = component.componentDef.GetComponent<ExtendedDetails>();
      if (extDescr == null) {
        this.hoverSidePanel.Description = new Localize.Text(component.Description.Details);
        Log.Debug?.WL(1, "no extended description:"+ this.hoverSidePanel.Description);
      } else {
        StringBuilder description = new StringBuilder();
        Log.Debug?.WL(1, "extended description:" + description);
        foreach (ExtendedDetail detail in extDescr.GetDetails()) {
          if (detail.UnitType != UnitType.UNDEFINED) { if (detail.UnitType != component.parent.UnitType) { continue; } };
          Log.Debug?.WL(2, "detail:" + detail.Identifier + ":"+detail.Text);
          string addtext = new Localize.Text(detail.Text).ToString();
          addtext = addtext.Replace("\n\n","\n");
          description.Append("<size=80%>"+addtext+"</size>");
        }
        this.hoverSidePanel.Description = new Localize.Text(description.ToString());
      }
      float offHeat = 0f;
      float onHeat = 0f;
      if (activeDef != null) {
        offHeat = (activeDef.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activeDef.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activeDef.AutoDeactivateOnHeat;
        onHeat = (activeDef.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activeDef.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activeDef.AutoActivateOnHeat;
      }
      if (onHeat > Core.Epsilon) {
        this.hoverSidePanel.Description.Append("\nAUTO ACTIVE ON:" + onHeat);
      }
      if (offHeat > Core.Epsilon) {
        this.hoverSidePanel.Description.Append("\nAUTO DEACTIVE ON:" + offHeat);
      }
      int index = 0;
      AbstractActor actor = component.parent;
      abilities.Clear();
      CombatHUDEquipmentSlotEx.ClearCache(component);
      //HashSet<CombatHUDEquipmentSlot> enabledButtons = new HashSet<CombatHUDEquipmentSlot>();
      foreach (Ability ability in component.parent.ComponentAbilities) {
        if (ability.parentComponent == component) {
          if (buttons.Count <= index) {
            this.InitNewAbilitySlot();
          }
          if (buttons.Count > index) {
            buttons[index].gameObject.SetActive(true);
            buttons[index].gameObject.transform.parent.gameObject.SetActive(true);
            buttons[index].Init(component.parent.Combat, HUD, BTInput.Instance.Key_None(), true);
            buttons[index].InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false), ability, ability.Def.AbilityIcon, ability.Def.Description.Id, ability.Def.Description.Name, actor);
            bool forceInactive = actor.HasActivatedThisRound || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved;
            CombatHUDEquipmentSlotEx.ResetAbilityButton(actor, (CombatHUDActionButton)buttons[index], ability, forceInactive);
            ++index;
            abilities.Add(ability);
          }
        }
      }
      for (int t = abilities.Count; t < buttons.Count; ++t) { buttons[t].gameObject.SetActive(false); buttons[t].gameObject.transform.parent.gameObject.SetActive(false); };

      if (useProjectedHeat) {
        this.heatMeter.UpdateHeatBounds();
      }
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.Init " + component.defId + " abilities:" + this.abilities.Count);
    }
    public static CombatHUDEquipmentSlotEx Init(CombatHUD HUD, CombatHUDWeaponPanel weaponPanel, CombatHUDEquipmentPanel equipPanel) {
      Transform slot = weaponPanel.gameObject.transform.Find("wp_Slot1");
      GameObject slot_ex = null;
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.static Init");
      if (slot != null) {
        Log.Debug?.WL(1, "wp_Slot1 found");
        slot_ex = GameObject.Instantiate(slot.gameObject);
        CombatHUDWeaponSlot hudslot = slot_ex.GetComponentInChildren<CombatHUDWeaponSlot>(true);
        if(hudslot != null) GameObject.Destroy(hudslot);
        WeaponDamageHover dmghover = slot_ex.GetComponentInChildren<WeaponDamageHover>(true);
        if(dmghover!= null) GameObject.Destroy(dmghover);
        WeaponNameHover namehover = slot_ex.GetComponentInChildren<WeaponNameHover>(true);
        if (namehover != null) GameObject.Destroy(namehover);
        WeaponAmmoCounterHover ammohover = slot_ex.GetComponentInChildren<WeaponAmmoCounterHover>(true);
        if (ammohover != null) GameObject.Destroy(ammohover);
        WeaponHitChanceHover hithover = slot_ex.GetComponentInChildren<WeaponHitChanceHover>(true);
        if (hithover != null) GameObject.Destroy(hithover);
        CombatHUDWeaponSlotEx weaponSlotEx = slot_ex.GetComponentInChildren<CombatHUDWeaponSlotEx>(true);
        if (weaponSlotEx != null) { GameObject.Destroy(weaponSlotEx); };
        WeaponModeHover modeHover = slot_ex.GetComponentInChildren<WeaponModeHover>(true);
        if (modeHover != null) { GameObject.Destroy(modeHover); };
        WeaponAmmoHover ammoNameHover = slot_ex.GetComponentInChildren<WeaponAmmoHover>(true);
        if (ammoNameHover != null) { GameObject.Destroy(ammoNameHover); };
        slot_ex.transform.SetParent(weaponPanel.transform);
        slot_ex.SetActive(false);
        slot_ex.transform.localScale = new Vector3(1f, 1f, 1f);
        Log.Debug?.TWL(0, "found wp_Slot1 parent:" + slot_ex.transform.parent.name);
      } else {
        Log.Debug?.WL(1, "wp_Slot1 not found");
        return null;
      }
      CombatHUDEquipmentSlotEx result = null;
      Transform ui = slot_ex.transform.Find("uixPrfBttn_weaponSlot-MANAGED");
      if (ui != null) {
        result = slot_ex.AddComponent<CombatHUDEquipmentSlotEx>();
        result.HUD = HUD;
        result.weaponPanel = weaponPanel;
        result.equipPanel = equipPanel;
        result.panelEx = weaponPanel.gameObject.GetComponent<CombatHUDWeaponPanelEx>();
        result.buttons = new List<CombatHUDEquipmentSlot>();
        result.abilities = new List<Ability>();
        result.LookAndColorConstants = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants;
        result.nameText = ui.Find("weapon_Text").GetComponent<LocalizableText>();
        result.stateText = ui.Find("ammo_Text").GetComponent<LocalizableText>();
        result.failText = ui.Find("hitChance_Text").GetComponent<LocalizableText>();
        result.chargesText = ui.Find("damage_Text").GetComponent<LocalizableText>();

        GameObject button_down = ui.gameObject.FindRecursive("weapon_button_down");
        if (button_down != null) { GameObject.Destroy(button_down); }
        GameObject button_up = ui.gameObject.FindRecursive("weapon_button_up");
        if (button_up != null) { GameObject.Destroy(button_up); }
        GameObject mode_text_back = ui.gameObject.FindRecursive("mode_text_background");
        if (mode_text_back != null) { GameObject.Destroy(mode_text_back); }
        GameObject ammo_text_back = ui.gameObject.FindRecursive("ammo_name_text_background");
        if (ammo_text_back != null) { GameObject.Destroy(ammo_text_back); }
        GameObject modText = ui.gameObject.FindRecursive("mode_text");
        if (modText != null) { modText.GetComponent<LocalizableText>().SetText("  "); }
        GameObject ammoText = ui.gameObject.FindRecursive("ammo_name_text");
        if (ammoText != null) { ammoText.GetComponent<LocalizableText>().SetText("  "); }

        result.mainImage = ui.gameObject.GetComponent<SVGImage>();
        result.checkImage = ui.Find("check_Image").gameObject.GetComponent<SVGImage>();
        Log.Debug?.TWL(0, "check_Image:" + ui.Find("check_Image").gameObject.GetComponent<SVGImage>().color);
        GameObject.Destroy(ui.gameObject.GetComponent<CombatHUDTooltipHoverElement>());
        result.hoverSidePanel = ui.gameObject.AddComponent<CombatHUDSidePanelHoverElement>();
        result.hoverSidePanel.Init(HUD);
        result.hoverSidePanel.Title = new Localize.Text("__/CAE.COMPONENT/__");
        result.hoverSidePanel.Description = new Localize.Text("__/CAE.COMPONENT_DESCRIPTION/__");

        GameObject heatmetergo = new GameObject("heatmeter");
        heatmetergo.transform.SetParent(result.mainImage.gameObject.transform);
        heatmetergo.transform.SetAsFirstSibling();
        heatmetergo.transform.localScale = Vector3.one;
        heatmetergo.transform.localPosition = Vector3.zero;
        heatmetergo.transform.localRotation = Quaternion.identity;
        result.heatMeter = heatmetergo.AddComponent<CombatHUDEqupmentSlotHeat>();
        result.heatMeter.Init(result);
        //GameObject.Destroy(ui.Find("check_Image").gameObject);
        //GameObject.Destroy(slot_ex.transform.Find("flag_multiTarget_Diamond (1)").gameObject);
      }
      result.equipPanel.slots.Add(result);
      return result;
    }
    public void InitNewAbilitySlot() {
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.InitNewAbilitySlot "+ this.component.defId);
      try {
        Transform slot = weaponPanel.gameObject.transform.Find("uixPrfPanl_ElectronicWarfareToggles");
        if (slot != null) {
          GameObject slot_ex = GameObject.Instantiate(slot.gameObject);
          slot_ex.transform.SetParent(weaponPanel.transform);
          slot_ex.transform.localScale = new Vector3(1f, 0.9f, 1f);
          slot_ex.SetActive(false);
          HorizontalLayoutGroup layout = slot_ex.GetComponent<HorizontalLayoutGroup>();
          RectOffset tempPadding = new RectOffset(
                  layout.padding.left,
                  layout.padding.right,
                  layout.padding.top,
                  layout.padding.bottom);
          tempPadding.left += 50;
          layout.padding = tempPadding;
          Log.Debug?.TWL(0, "found uixPrfPanl_ElectronicWarfareToggles parent:" + slot_ex.transform.parent.name);
          CombatHUDEquipmentSlot eqslot = slot_ex.transform.Find("equipmentButton_1").GetComponent<CombatHUDEquipmentSlot>();
          if (eqslot != null) {
            buttons.Add(eqslot);
            Log.Debug?.WL(1, "found CombatHUDEquipmentSlot parent:" + eqslot.transform.parent.name);
          }
        }
      }catch(Exception e) {
        Log.Debug?.TWL(0,e.ToString());
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
  public static class CombatHUDWeaponPanel_Init {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance, CombatGameState Combat, CombatHUD HUD) {
      CombatHUDWeaponPanelExHelper.HUD = HUD;
      CombatHUDWeaponPanelExHelper.panelEx = __instance.gameObject.GetComponent<CombatHUDEquipWeaponPanelEx>();
      if (CombatHUDWeaponPanelExHelper.panelEx == null) {
        CombatHUDWeaponPanelExHelper.panelEx = __instance.gameObject.AddComponent<CombatHUDEquipWeaponPanelEx>();
        CombatHUDWeaponPanelExHelper.panelEx.Init(__instance, HUD);
        Transform labels = __instance.gameObject.transform.Find("wp_Labels");
        if (labels != null) {
          WeaponSlotsLabelsToggle toggle = labels.gameObject.GetComponent<WeaponSlotsLabelsToggle>();
          if (toggle == null) { toggle = labels.gameObject.AddComponent<WeaponSlotsLabelsToggle>(); }
          LocalizableText text_WeaponText = labels.gameObject.transform.Find("text_WeaponText").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_Ammo = labels.gameObject.transform.Find("text_Ammo").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_Damage = labels.gameObject.transform.Find("text_Damage").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_HitChance = labels.gameObject.transform.Find("text_HitChance").gameObject.GetComponent<LocalizableText>();
          toggle.Init(text_WeaponText, text_Ammo, text_Damage, text_HitChance);
        }
      }
      if (CombatHUDEquipmentPanel.Instance != null) { CombatHUDEquipmentPanel.Clear(); };
      CombatHUDEquipmentPanel.Init(HUD, __instance);
    }
  }

  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("DisplayedActor")]
  [HarmonyPatch(MethodType.Setter)]
  public static class CombatHUDWeaponPanel_SetState {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance) {
      if (CombatHUDWeaponPanelExHelper.panelEx != null) {
        CombatHUDWeaponPanelExHelper.panelEx.Force(__instance.DisplayedActor != null);
      }
    }
  }

  public static class CombatHUDWeaponPanelExHelper {
    public static CombatHUD HUD = null;
    public static CombatHUDEquipWeaponPanelEx panelEx = null;
    public static void Clear() {
      HUD = null;
      if (panelEx != null) { GameObject.Destroy(panelEx); panelEx = null; }
    }
  }
  public enum WPState {
    None,
    Off,
    Loading,
    Loaded,
    Unloading
  }
  public class CombatHUDEquipWeaponPanelEx : MonoBehaviour {
    private CombatHUDWeaponPanel panel;
    private CombatHUD HUD;
    private List<CombatHUDWeaponSlot> WeaponSlots;
    private List<CombatHUDEquipmentSlot> EquipmentSlots;
    private CombatHUDWeaponSlot meleeSlot;
    private CombatHUDWeaponSlot dfaSlot;
    public float TimeToLoad = 0.3f;
    public float TimeToUnload = 0.15f;
    private float timeInCurrentState;
    private WPState state;
    private int numWeaponsDisplayed {
      get {
        if (this.panel == null) { Log.Debug?.TWL(0, "CombatHUDWeaponPanel is null!"); return 0; }
        return this.panel.numWeaponsDisplayed;
      }
    }
    public void Awake() {
      //panel = null;
    }
    public void Init(CombatHUDWeaponPanel weaponPanel, CombatHUD HUD) {
      this.panel = weaponPanel;
      this.HUD = HUD;
      WeaponSlots = weaponPanel.WeaponSlots;
      EquipmentSlots = weaponPanel.EquipmentSlots;
      meleeSlot = weaponPanel.meleeSlot;
      dfaSlot = weaponPanel.dfaSlot;
      this.state = WPState.Off;
      this.timeInCurrentState = 0.0f;
    }
    public void Show(bool show) {
      if (show) { this.SetState(WPState.Loading); } else { this.SetState(WPState.Unloading); };
    }

    public void Toggle() {
      switch (this.state) {
        case WPState.Unloading:
        case WPState.Off:
          SetState(WPState.Loading);
          break;
        case WPState.Loaded:
        case WPState.Loading:
          SetState(WPState.Unloading);
          break;
      }
    }
    public void Force(bool show) {
      this.state = show ? WPState.Loaded : WPState.Off;
    }
    public void ShowWeaponsUpTo(int count) {
      this.panel.ShowWeaponsUpTo(count);
    }
    private void SetState(WPState newState) {
      if (this.state == newState)
        return;
      this.state = newState;
      this.timeInCurrentState = 0.0f;
      switch (newState) {
        case WPState.Off:
          this.ShowWeaponsUpTo(0);
          break;
        case WPState.Loaded:
          this.ShowWeaponsUpTo(this.panel.DisplayedActor.Weapons.Count);
          break;
      }
    }
    private void Update() {
      this.timeInCurrentState += Time.unscaledDeltaTime;
      switch (this.state) {
        case WPState.Loading:
          if ((double)this.timeInCurrentState > (double)this.TimeToLoad) {
            this.SetState(WPState.Loaded);
            break;
          }
          break;
        case WPState.Unloading:
          if ((double)this.timeInCurrentState > (double)this.TimeToUnload) {
            this.SetState(WPState.Off);
            break;
          }
          break;
      }
      switch (this.state) {
        case WPState.Loading:
          this.ShowWeaponsUpTo((int)((double)this.panel.DisplayedActor.Weapons.Count * (double)(this.timeInCurrentState / this.TimeToLoad)));
          break;
        case WPState.Unloading:
          this.ShowWeaponsUpTo((int)((double)this.numWeaponsDisplayed * (1.0 - (double)this.timeInCurrentState / (double)this.TimeToLoad)));
          break;
      }
    }
  }
}
