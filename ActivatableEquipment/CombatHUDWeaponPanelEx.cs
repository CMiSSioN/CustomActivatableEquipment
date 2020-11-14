using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomAmmoCategoriesPatches;
using CustomAmmoCategoriesPathes;
using CustomComponents;
using CustomComponents.ExtendedDetails;
using Harmony;
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
      foreach (MechComponent component in unit.allComponents) {
        Log.Debug?.WL(1, "component:" + component.defId);
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (acomps.Contains(component)) { Log.Debug?.WL(2, "have ability"); ncomps.Add(component); continue; };
        if (activatable == null) { Log.Debug?.WL(2, "not activatable"); continue; }
        if (ActivatableComponent.isComponentActivated(component)) { Log.Debug?.WL(2, "activated"); ncomps.Add(component); continue; };
        if (activatable.CanBeactivatedManualy) { Log.Debug?.WL(2, "can be activated manualy"); ncomps.Add(component); continue; };
        if (activatable.AutoActivateOnHeat > Core.Epsilon) { Log.Debug?.WL(2, "activate by heat"); ncomps.Add(component); continue; }
      }
      for (int index = 0; index < ncomps.Count; ++index) {
        if (index >= slots.Count) {
          CombatHUDEquipmentSlotEx nslot = CombatHUDEquipmentSlotEx.Init(HUD, weaponPanel, this);
          if (nslot != null) clickReceivers.Add(nslot.hoverSidePanel, nslot);
        }
        if (index < slots.Count) {
          operatinalSlots.Add(slots[index]);
          slots[index].Init(ncomps[index]);
          slots[index].RealState = true;
        }
      }
      for (int index = ncomps.Count; index < slots.Count; ++index) {
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

  public class CombatHUDEquipmentSlotEx : EventTrigger {
    public CombatHUDWeaponPanelEx panelEx { get; set; }
    public CombatHUD HUD { get; private set; }
    public CombatHUDWeaponPanel weaponPanel { get; private set; }
    public CombatHUDEquipmentPanel equipPanel { get; private set; }
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
    private bool isNeedFlashing() {
      if (component == null) { return false; }
      if (activeDef == null) { return false; }
      if (activeDef.ChargesCount != 0) { return false; }
      if (component.isActive() == false) { return false; }
      if (component.FailChance() < Core.Settings.equipmentFlashFailChance) { return false; }
      return true;
    }
    private bool flashing { get; set; }
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
      flashing = false;
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
      if (failChance <= 0.15f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorA; }
      if (failChance <= 0.30f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorB; }
      if (failChance <= 0.50f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorC; }
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
      flashing = isNeedFlashing();
      if (flashing == false) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      }
      this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.AvailableTextColor, this.GetFailTextColor(CombatHUDEquipmentSlotEx.FailChance(component)));
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
      ui_inited = true;
    }
    public void Update() {
      if (ui_inited == false) { UIInit(); }
      if (flashing == false) { return; }
      flashT += Time.deltaTime * flashSpeedCurrent;
      if (flashT > 1f) { flashT = 1f; flashSpeedCurrent = -2f; }
      if (flashT < 0f) { flashT = 0f; flashSpeedCurrent = 2f; }
      this.mainImage.color = Color.Lerp(this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor,Color.red, flashT);
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
    public override void OnPointerClick(PointerEventData eventData) {
      Log.Debug?.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerClick");
      if (activeDef == null) { return; }
      if (component == null) { return; }
      if (activeDef.CanBeactivatedManualy == false) { return; }
      if (HUD.SelectedTarget != null) { return; }
      if (component.IsFunctional == false) { return; }
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (activatable.CanActivateAfterMove == false) {
        if (component.parent.HasMovedThisRound) { return; }
      }
      if (ActivatableComponent.isOutOfCharges(component)) { return; }
      if (component.parent.IsAvailableThisPhase == false) { return; }
      Log.Debug?.Write("Toggle activatable " + component.defId + "\n");
      ActivatableComponent.toggleComponentActivation(this.component);
      equipPanel.RefreshDisplayedEquipment(component.parent);
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
    public void RefreshComponent() {
      this.Show();
      if (hovered == false) { RefreshNonHighlighted(); } else { RefreshHighlighted(); };
      nameText.SetText(component.UIName);
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
        //float failChance = Mathf.Round(CombatHUDEquipmentSlotEx.FailChance(component)*100.0f);
        failText.SetText("{0}%", Mathf.Round(CombatHUDEquipmentSlotEx.FailChance(component) * 100.0f));
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
          flashing = isNeedFlashing();
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
      flashing = false;
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
    public void Init(MechComponent component) {
      hovered = false;
      ui_inited = false;
      this.component = component;
      this.activeDef = component.componentDef.GetComponent<ActivatableComponent>();
      this.hoverSidePanel.Title = new Localize.Text(component.Description.UIName);
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
    private PropertyInfo p_numWeaponsDisplayed;
    private int numWeaponsDisplayed {
      get {
        if (this.panel == null) { Log.Debug?.TWL(0, "CombatHUDWeaponPanel is null!"); return 0; }
        return (int)p_numWeaponsDisplayed.GetValue(this.panel);
      }
    }
    public void Awake() {
      //panel = null;
    }
    public void Init(CombatHUDWeaponPanel weaponPanel, CombatHUD HUD) {
      this.panel = weaponPanel;
      this.HUD = HUD;
      WeaponSlots = (List<CombatHUDWeaponSlot>)typeof(CombatHUDWeaponPanel).GetField("WeaponSlots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      EquipmentSlots = (List<CombatHUDEquipmentSlot>)typeof(CombatHUDWeaponPanel).GetField("EquipmentSlots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      meleeSlot = (CombatHUDWeaponSlot)typeof(CombatHUDWeaponPanel).GetField("meleeSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      dfaSlot = (CombatHUDWeaponSlot)typeof(CombatHUDWeaponPanel).GetField("dfaSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      p_numWeaponsDisplayed = typeof(CombatHUDWeaponPanel).GetProperty("numWeaponsDisplayed", BindingFlags.Instance | BindingFlags.NonPublic);
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
      typeof(CombatHUDWeaponPanel).GetMethod("ShowWeaponsUpTo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this.panel, new object[] { count });
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
