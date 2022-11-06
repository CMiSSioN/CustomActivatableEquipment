﻿using BattleTech;
using BattleTech.Data;
using BattleTech.Save.Test;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using CustAmmoCategories;
using CustomComponents;
using CustomSettings;
using Harmony;
using HBS;
using HBS.Collections;
using IRBTModUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomActivatableEquipment {
  public class MechLabItemButton : EventTrigger {
    public SVGImage svg { get; set; } = null;
    public Image img { get; set; } = null;
    public MLComponentRefTracker parent { get; set; } = null;
    public void Update() {
      try {
        if (parent != null && parent.listItem != null) {
          if (parent.iconParent == null) {
            SVGImage icon = Traverse.Create(parent.listItem).Field<SVGImage>("icon").Value;
            parent.iconParent = icon.transform.parent.gameObject.GetComponent<RectTransform>();
          }
          parent.iconParent.anchoredPosition = new Vector2(-2f, parent.iconParent.anchoredPosition.y);
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString());
      }
    }
    public override void OnPointerEnter(PointerEventData eventData) {
      Log.Debug?.TWL(0, "MechLabItemButton.OnPointerEnter " + parent.componentRef.ComponentDefID);
      svg.color = UIManager.Instance.UIColorRefs.orange;
    }
    public override void OnPointerExit(PointerEventData eventData) {
      Log.Debug?.TWL(0, "MechLabItemButton.OnPointerExit " + parent.componentRef.ComponentDefID);
      svg.color = UIManager.Instance.UIColorRefs.white;
    }
    public override void OnPointerClick(PointerEventData eventData) {
      Log.Debug?.TWL(0, "MechLabItemButton.OnPointerClick " + parent.componentRef.ComponentDefID);
      TargetsPopupSupervisor.Instance.OnTargetsButtonClick(parent.componentRef, parent.mechLabPanel.activeMechDef, parent.mechLabPanel.activeMechInventory);
    }
  }
  public class MLComponentRefTracker : MonoBehaviour {
    private MechComponentRef f_componentRef = null;
    public MechComponentRef componentRef {
      get {
        return f_componentRef;
      }
      set {
        f_componentRef = value;
      }
    }
    public MechDef mechDef { get; set; }
    public MechLabItemButton settingsButton { get; set; }
    public MechLabItemSlotElement listItem { get; set; }
    //private bool uiInited = false;
    public RectTransform iconParent = null;
    public MechLabPanel mechLabPanel { get; set; } = null;
    public void Clear() {
      mechDef = null;
      componentRef = null;
    }
    public void Init(MechLabItemSlotElement mlelement, MechLabPanel mechLabPanel) {
      this.listItem = mlelement;
      settingsButton = listItem.GetComponentInChildren<MechLabItemButton>(true);
      this.mechLabPanel = mechLabPanel;
      if (settingsButton == null) {
        SVGImage icon = Traverse.Create(mlelement).Field<SVGImage>("icon").Value;
        //RectTransform rectTransform = icon.transform.parent.gameObject.GetComponent<RectTransform>();
        GameObject svgGO = GameObject.Instantiate(icon.gameObject);
        svgGO.name = "settingsIcon";
        svgGO.transform.SetParent(icon.gameObject.transform.parent);
        svgGO.transform.localScale = Vector3.one;
        svgGO.transform.SetSiblingIndex(icon.gameObject.transform.GetSiblingIndex() + 1);
        SVGImage svg = svgGO.GetComponent<SVGImage>();
        CustomSvgCache.setIcon(svg, "gears", UIManager.Instance.dataManager);

        GameObject itemGO = new GameObject("settingsImg");
        LayoutElement layoutElement = itemGO.AddComponent<LayoutElement>();
        //layoutElement.ignoreLayout = true;
        itemGO.transform.SetParent(svg.rectTransform);
        Image img = itemGO.AddComponent<Image>();
        img.rectTransform.pivot = Vector2.zero;
        img.rectTransform.sizeDelta = svg.rectTransform.sizeDelta;
        img.color = Color.clear;
        img.rectTransform.anchoredPosition = Vector2.zero;
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.zero;
        settingsButton = itemGO.AddComponent<MechLabItemButton>();
        settingsButton.svg = svg;
        settingsButton.img = img;
        settingsButton = listItem.GetComponentInChildren<MechLabItemButton>(true);
      }
      settingsButton.parent = this;
      this.componentRef = Traverse.Create(mlelement).Field<MechComponentRef>("componentRef").Value;
      //MechLabPanel mechLabPanel = dropParent.gameObject.GetComponent<MechLabPanel>();
      //if (mechLabPanel == null) { mechLabPanel = dropParent.gameObject.GetComponentInParent<MechLabPanel>(); }
      this.mechDef = mechLabPanel.activeMechDef;
      settingsButton.svg.gameObject.SetActive(this.componentRef.Def.isHasTarget());
      //this.componentRef.Def.GetWeaponAddon().hasTarget;
      //this.uiInited = false;
    }
  }

  [HarmonyPatch(typeof(MechLabLocationWidget))]
  [HarmonyPatch("OnAddItem")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
  public static class MechLabLocationWidget_OnAddItem {
    private static bool? PrepareCalled = new bool?();
    public static bool Prepare() {
      if (PrepareCalled.HasValue) { return PrepareCalled.Value; }
      PrepareCalled = true;
      FieldInfo LocalGUID = typeof(BattleTech.BaseComponentRef).GetField("LocalGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.LocalGUID {(LocalGUID == null ? "not found" : "found")}");
      if (LocalGUID != null) {
      } else { PrepareCalled = false; return false; }
      FieldInfo TargetComponentGUID = typeof(BattleTech.BaseComponentRef).GetField("TargetComponentGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.TargetComponentGUID {(TargetComponentGUID == null ? "not found" : "found")}");
      if (TargetComponentGUID != null) {
      } else { PrepareCalled = false; return false; }
      return true;
    }
    public static void Postfix(MechLabLocationWidget __instance, IMechLabDraggableItem item, bool validate) {
      try {
        MechLabItemSlotElement mechlabItem = item as MechLabItemSlotElement;
        MLComponentRefTracker refTracker = mechlabItem.gameObject.GetComponent<MLComponentRefTracker>();
        if (refTracker == null) { refTracker = mechlabItem.gameObject.AddComponent<MLComponentRefTracker>(); }
        var mechLabPanel = __instance.GetComponentInParent<MechLabPanel>();
        refTracker.Init(mechlabItem, __instance.GetComponentInParent<MechLabPanel>());
        if ((mechlabItem.ComponentRef.Def.isHasTarget())&&(mechlabItem.ComponentRef.Def.isAutoTarget())) {
          TargetsPopupSupervisor.ResolveAddonsOnInventory(mechLabPanel.activeMechInventory);
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechLabLocationWidget))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(LocationLoadoutDef) })]
  public static class MechLabLocationWidget_SetData {
    private static bool? PrepareCalled = new bool?();
    public static bool Prepare() {
      if (PrepareCalled.HasValue) { return PrepareCalled.Value; }
      PrepareCalled = true;
      FieldInfo LocalGUID = typeof(BattleTech.BaseComponentRef).GetField("LocalGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.LocalGUID {(LocalGUID == null ? "not found" : "found")}");
      if (LocalGUID != null) {
      } else { PrepareCalled = false; return false; }
      FieldInfo TargetComponentGUID = typeof(BattleTech.BaseComponentRef).GetField("TargetComponentGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.TargetComponentGUID {(TargetComponentGUID == null ? "not found" : "found")}");
      if (TargetComponentGUID != null) {
      } else { PrepareCalled = false; return false; }
      return true;
    }
    public static void Postfix(MechLabLocationWidget __instance, LocationLoadoutDef loadout, ref List<MechLabItemSlotElement> ___localInventory) {
      try {
        var mechLabPanel = __instance.GetComponentInParent<MechLabPanel>();
        foreach (MechLabItemSlotElement mechlabItem in ___localInventory) {
          MLComponentRefTracker refTracker = mechlabItem.gameObject.GetComponent<MLComponentRefTracker>();
          if (refTracker == null) { refTracker = mechlabItem.gameObject.AddComponent<MLComponentRefTracker>(); }
          refTracker.Init(mechlabItem, __instance.GetComponentInParent<MechLabPanel>());
        }
        TargetsPopupSupervisor.ResolveAddonsOnInventory(mechLabPanel.activeMechInventory);
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechLabLocationWidget))]
  [HarmonyPatch("OnRemoveItem")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
  public static class MechLabLocationWidget_OnRemoveItem {
    private static bool? PrepareCalled = new bool?();
    public static bool Prepare() {
      if (PrepareCalled.HasValue) { return PrepareCalled.Value; }
      PrepareCalled = true;
      FieldInfo LocalGUID = typeof(BattleTech.BaseComponentRef).GetField("LocalGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.LocalGUID {(LocalGUID == null ? "not found" : "found")}");
      if (LocalGUID != null) {
      } else { PrepareCalled = false; return false; }
      FieldInfo TargetComponentGUID = typeof(BattleTech.BaseComponentRef).GetField("TargetComponentGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.TargetComponentGUID {(TargetComponentGUID == null ? "not found" : "found")}");
      if (TargetComponentGUID != null) {
      } else { PrepareCalled = false; return false; }
      return true;
    }
    public static void Postfix(MechLabLocationWidget __instance, IMechLabDraggableItem item, bool validate) {
      try {
        MechLabItemSlotElement mechlabItem = item as MechLabItemSlotElement;
        MLComponentRefTracker refTracker = mechlabItem.gameObject.GetComponent<MLComponentRefTracker>();
        if ((refTracker != null)&&(refTracker.settingsButton != null)&&(refTracker.settingsButton.svg != null)) {
          refTracker.settingsButton.svg.gameObject.SetActive(false);
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechLabItemSlotElement))]
  [HarmonyPatch("ClearData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechComponent_ClearData {
    private static bool? PrepareCalled = new bool?();
    public static bool Prepare() {
      if (PrepareCalled.HasValue) { return PrepareCalled.Value; }
      PrepareCalled = true;
      FieldInfo LocalGUID = typeof(BattleTech.BaseComponentRef).GetField("LocalGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.LocalGUID {(LocalGUID == null ? "not found" : "found")}");
      if (LocalGUID != null) {
      } else { PrepareCalled = false; return false; }
      FieldInfo TargetComponentGUID = typeof(BattleTech.BaseComponentRef).GetField("TargetComponentGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.TargetComponentGUID {(TargetComponentGUID == null ? "not found" : "found")}");
      if (TargetComponentGUID != null) {
      } else { PrepareCalled = false; return false; }
      return true;
    }
    public static void Postfix(MechLabItemSlotElement __instance) {
      try {
        MLComponentRefTracker refTracker = __instance.gameObject.GetComponent<MLComponentRefTracker>();
        if (refTracker == null) { refTracker = __instance.gameObject.AddComponent<MLComponentRefTracker>(); }
        refTracker.Clear();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

  [CustomComponent("AddonReference")]
  public class AddonReference : SimpleCustomComponent {
    public bool autoTarget { get; set; } = false;
    public bool installedLocationOnly { get; set; } = false;
    public string WeaponAddonId { get; set; } = string.Empty;
    public string[] WeaponAddonIds { get; set; } = new string[0];
  }
  public class WeaponAddonDef {
    [JsonIgnore]
    public MechComponentDef parent { get; } = null;
    public string Id { get; set; } = string.Empty;
    [JsonIgnore]
    private TagSet f_tags = null;
    [JsonIgnore]
    public TagSet targetTags {
      get {
        if (f_tags == null) { f_tags = new TagSet(targetComponentTags); }
        return f_tags;
      }
    }
    public HashSet<string> targetComponentTags { get; set; } = new HashSet<string>();
    public string addonType { get; set; } = string.Empty;
    [JsonIgnore]
    public string safeAddonType {
      get {
        return string.IsNullOrEmpty(addonType) ? this.Id : this.addonType;
      }
    }
    [JsonIgnore]
    public List<WeaponMode> modes { get; set; } = new List<WeaponMode>();
    public WeaponAddonDef() {

    }
    public WeaponAddonDef(string id) {
      this.Id = id;
    }
    private static List<PropertyInfo> properties = new List<PropertyInfo>();
    public static void InitProperties() {
      if (properties.Count != 0) { return; }
      foreach (var prop in typeof(WeaponAddonDef).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
        bool skip = false;
        foreach (var attr in prop.GetCustomAttributes(true)) { if ((attr as JsonIgnoreAttribute) != null) { skip = true; break; } }
        if (skip) { continue; }
        properties.Add(prop);
      }
    }
    public void fromJSON(JToken json) {
      InitProperties();
      foreach (var prop in properties) {
        if (json[prop.Name] == null) { continue; }
        prop.SetValue(this, json[prop.Name].ToObject(prop.PropertyType));
      }
      if(json[nameof(modes)] is JArray jmodes) {
        foreach(var jmode in jmodes) {
          WeaponMode mode = new WeaponMode();
          mode.fromJSON(jmode);
          this.modes.Add(mode);
        }
      }
    }
  }
  public static class WeaponAddonDefHelper {
    private static Dictionary<string, WeaponAddonDef> loadedAddons = new Dictionary<string, WeaponAddonDef>();
    private static Dictionary<string, HashSet<WeaponAddonDef>> upgradeAddons = new Dictionary<string, HashSet<WeaponAddonDef>>();
    private static Dictionary<string, VersionManifestEntry> existingAddons = new Dictionary<string, VersionManifestEntry>();
    private static BaseComponentRefRegistry baseComponentRefRegistry = new BaseComponentRefRegistry();
    public static void InitweaponsAddons(this AbstractActor unit) {
      Log.Debug?.TWL(0,$"InitweaponsAddons {unit.PilotableActorDef.ChassisID}");
      Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>();
      foreach(Weapon weapon in unit.Weapons) {
        if (string.IsNullOrEmpty(weapon.baseComponentRef.LocalGUID())) { continue; }
        weapons[weapon.baseComponentRef.LocalGUID()] = weapon;
      }
      foreach(MechComponent component in unit.allComponents) {
        if (component.baseComponentRef.Def.isHasTarget() == false) { continue; }
        if (string.IsNullOrEmpty(component.baseComponentRef.TargetComponentGUID())) { continue; }
        if(weapons.TryGetValue(component.baseComponentRef.TargetComponentGUID(), out var weapon)) {
          foreach (WeaponMode mode in component.GatherModesFor(weapon)) {
            Log.Debug?.WL(1, $"add mode {mode.Id} to {weapon.defId} guid:{weapon.baseComponentRef.LocalGUID()}");
            weapon.info().AddMode(mode, mode.isBaseMode);
          }
        }
      }
    }
    public static List<WeaponMode> GatherModesFor(this MechComponent component, Weapon weapon) {
      List<WeaponMode> result = new List<WeaponMode>();
      AddonReference addonReference = component.componentDef.GetComponent<AddonReference>();
      if (addonReference == null) { return result; }
      if (addonReference.installedLocationOnly && component.Location != weapon.Location) { return result; }
      foreach (var addon in component.componentDef.GetWeaponAddons()) {
        if (weapon.baseComponentRef.CanBeTarget(addon)) { result.AddRange(addon.modes); }
      }
      return result;
    }
    public static List<WeaponMode> GatherModes(this BaseComponentRef componentRef, List<BaseComponentRef> inventory) {
      List<WeaponMode> result = new List<WeaponMode>();
      try {
        if (string.IsNullOrEmpty(componentRef.LocalGUID())) { return result; }
        //TargetsPopupSupervisor.ResolveAddonsOnInventory(inventory);
        foreach(var addonSrc in inventory) {
          if (addonSrc.Def.isHasTarget() == false) { continue; }
          if (addonSrc.TargetComponentGUID() != componentRef.LocalGUID()) { continue; }
          if (componentRef.CanBeTarget(addonSrc) == false) { continue; }
          foreach (var addon in componentRef.GetAddonsFromSource(addonSrc)) {
            result.AddRange(addon.modes);
          }
        }
      }catch(Exception e) {
        Log.Error?.TWL(0,e.ToString(),true);
      }
      return result;
    }
    private class BaseComponentRefData {
      public string LocalGUID = string.Empty;
      public string TargetGUID = string.Empty;
    }
    private class BaseComponentRefRegistry {
      public Dictionary<string, BaseComponentRefData> additionalDataRegistry = new Dictionary<string, BaseComponentRefData>();
    }
    public static void WriteAdditionaldataRegistry(this BaseComponentRef componentRef) {
      if (string.IsNullOrEmpty(componentRef.SimGameUID)) { return; }
      if (baseComponentRefRegistry.additionalDataRegistry.TryGetValue(componentRef.SimGameUID, out var data) == false) {
        data = new BaseComponentRefData();
        baseComponentRefRegistry.additionalDataRegistry.Add(componentRef.SimGameUID, data);
      }
      data.LocalGUID = componentRef.LocalGUID(false);
      data.TargetGUID = componentRef.TargetComponentGUID(false);
    }
    public static void ReadAdditionaldataRegistry(this BaseComponentRef componentRef) {
      if (string.IsNullOrEmpty(componentRef.SimGameUID)) { return; }
      if (baseComponentRefRegistry.additionalDataRegistry.TryGetValue(componentRef.SimGameUID, out var data)) {
        componentRef.LocalGUID(data.LocalGUID, false);
        componentRef.TargetComponentGUID(data.TargetGUID, false);
      }
    }
    private static void IterateWorkQueue(this WorkOrderEntry entry, ref HashSet<string> existingSimGameIds) {
      if(entry is WorkOrderEntry_InstallComponent install) {
        existingSimGameIds.Add(install.ComponentSimGameUID);
      }
      if (entry is WorkOrderEntry_RepairComponent repair) {
        existingSimGameIds.Add(repair.ComponentSimGameUID);
      }
      foreach (var subitem in entry.SubEntries) {
        subitem.IterateWorkQueue(ref existingSimGameIds);
      }
    }
    public static void SanitizeAdditionalComponentsData(this SimGameState sim) {
      HashSet<string> existingSimGameIds = new HashSet<string>();
      foreach(var mech in sim.ActiveMechs) {
        foreach(var component in mech.Value.Inventory) {
          if (component == null) { continue; }
          if (string.IsNullOrEmpty(component.SimGameUID)) { continue; }
          existingSimGameIds.Add(component.SimGameUID);
        }
      }
      foreach (var mech in sim.ReadyingMechs) {
        foreach (var component in mech.Value.Inventory) {
          if (component == null) { continue; }
          if (string.IsNullOrEmpty(component.SimGameUID)) { continue; }
          existingSimGameIds.Add(component.SimGameUID);
        }
      }
      foreach(var item in sim.MechLabQueue) {
        item.IterateWorkQueue(ref existingSimGameIds);
      }
      HashSet<string> toDelete = new HashSet<string>();
      foreach(var reg in baseComponentRefRegistry.additionalDataRegistry) {
        if (existingSimGameIds.Contains(reg.Key)) { continue; }
        toDelete.Add(reg.Key);
      }
      foreach (string id in toDelete) { baseComponentRefRegistry.additionalDataRegistry.Remove(id); }
    }
    private static readonly string ADDITIONAL_COMPONENTS_DATA = "ADDITIONAL_COMPONENTS_DATA";
    public static void ComponentsAdditionaDataSave(this SimGameState sim) {
      sim.CompanyStats.GetOrCreateStatisic<string>(ADDITIONAL_COMPONENTS_DATA, "{}").SetValue<string>(JsonConvert.SerializeObject(baseComponentRefRegistry));
    }
    public static void ComponentsAdditionaDataLoad(this SimGameState sim) {
      baseComponentRefRegistry = JsonConvert.DeserializeObject< BaseComponentRefRegistry >(sim.CompanyStats.GetOrCreateStatisic<string>(ADDITIONAL_COMPONENTS_DATA, "{}").Value<string>());
    }
    public static HashSet<WeaponAddonDef> GetWeaponAddons(this MechComponent component) {
      return GetWeaponAddons(component.componentDef);
    }
    public static void RegisterWeaponAddon(this VersionManifestEntry entry, string id) { existingAddons[id] = entry; }
    public static HashSet<WeaponAddonDef> GetWeaponAddons(this MechComponentDef component) {
      if (upgradeAddons.TryGetValue(component.Description.Id, out var result)) { return result; }
      result = new HashSet<WeaponAddonDef>();
      Log.Debug?.TWL(0, $"GetWeaponAddons {component.Description.Id}");
      HashSet<string> addonsToLoad = new HashSet<string>();
      AddonReference addonRef = component.GetComponent<AddonReference>();
      if (addonRef != null) {
        if(string.IsNullOrEmpty(addonRef.WeaponAddonId) == false)addonsToLoad.Add(addonRef.WeaponAddonId);
        if(addonRef.WeaponAddonIds != null) {
          foreach (string addonId in addonRef.WeaponAddonIds) {
            if (string.IsNullOrEmpty(addonId)) { continue; }
            addonsToLoad.Add(addonId);
          }
        }
      }
      Log.Debug?.WL(1, $"addonsToLoad:{addonsToLoad.Count}");
      foreach (string addonId in addonsToLoad) {
        Log.Debug?.WL(2, $"{addonId}");
        if (loadedAddons.TryGetValue(addonId, out var addon)) {
          result.Add(addon); continue;
        }
        if (existingAddons.TryGetValue(addonId, out var addonfile)) {
          try {
            JObject json = JObject.Parse(File.ReadAllText(addonfile.FilePath));
            addon = new WeaponAddonDef(addonId);
            addon.fromJSON(json);
            loadedAddons.Add(addonId, addon);
            result.Add(addon);
          } catch (Exception e) {
            Log.Error?.TWL(0, $"Error reading addon file {addonfile.FilePath}");
            Log.Error?.WL(0, e.ToString());
          }
        }
      }
      Log.Debug?.WL(1, $"result:{result.Count}");
      upgradeAddons.Add(component.Description.Id, result);
      return result;
    }
    public static bool isHasTarget(this MechComponentDef componentDef) {
      return componentDef.GetWeaponAddons().Count > 0;
    }
    public static bool isAutoTarget(this MechComponentDef componentDef) {
      AddonReference addonRef = componentDef.GetComponent<AddonReference>();
      if (addonRef == null) { return false; }
      return addonRef.autoTarget;
    }
    public static bool CanBeTarget(this MechComponentDef componentDef, WeaponAddonDef addon) {
      if (componentDef.ComponentType != ComponentType.Weapon) { return false; }
      if (addon.targetComponentTags.Count == 0) { return true; }
      if (componentDef.ComponentTags.ContainsAll(addon.targetTags)) { return true; }
      return false;
    }
    public static bool CanBeTarget(this BaseComponentRef componentRef, WeaponAddonDef addon) {
      return componentRef.Def.CanBeTarget(addon);
    }
    public static bool CanBeTarget(this BaseComponentRef componentRef, BaseComponentRef addonSrc) {
      AddonReference addonRef = addonSrc.Def.GetComponent<AddonReference>();
      if (addonRef == null) { return false; }
      MechComponentRef mechComponentRef = componentRef as MechComponentRef;
      MechComponentRef srcComponentRef = addonSrc as MechComponentRef;
      if (addonRef.installedLocationOnly) {
        if ((mechComponentRef) != null && (srcComponentRef) != null) {
          if (mechComponentRef.MountedLocation != srcComponentRef.MountedLocation) { return false; }
        }
      }
      foreach (WeaponAddonDef addon in addonSrc.Def.GetWeaponAddons()) {
        if (componentRef.CanBeTarget(addon)) { return true; }
      }
      return false;
    }
    public static HashSet<WeaponAddonDef> GetAddonsFromSource(this BaseComponentRef componentRef, BaseComponentRef addonSrc) {
      HashSet<WeaponAddonDef> result = new HashSet<WeaponAddonDef>();
      AddonReference addonRef = addonSrc.Def.GetComponent<AddonReference>();
      if (addonRef == null) { return result; }
      MechComponentRef mechComponentRef = componentRef as MechComponentRef;
      MechComponentRef srcComponentRef = addonSrc as MechComponentRef;
      if (addonRef.installedLocationOnly) {
        if ((mechComponentRef) != null && (srcComponentRef) != null) {
          if (mechComponentRef.MountedLocation != srcComponentRef.MountedLocation) { return result; }
        }
      }
      foreach (WeaponAddonDef addon in addonSrc.Def.GetWeaponAddons()) {
        if (componentRef.CanBeTarget(addon)) { result.Add(addon); }
      }
      return result;
    }
  }
  public class TargetsControl : MonoBehaviour {
    public RectTransform listParent { get; set; } = null;
    public LocalizableText componentCountText { get; set; } = null;
    public List<TargetUIItem> weaponsList { get; set; } = new List<TargetUIItem>();
    //public TargetUIItem placeholderItem { get; set; } = null;
    public TargetsPopupSupervisor parent { get; set; } = null;
    public void AddWeaponUIItem(TargetDataElement dataElement) {
      GameObject gameObject = UIManager.Instance.dataManager.PooledInstantiate("uixPrfPanl_LC_TargetItem", BattleTechResourceType.UIModulePrefabs);
      if (gameObject == null) {
        gameObject = UIManager.Instance.dataManager.PooledInstantiate("uixPrfPanl_LC_MechLoadoutItem", BattleTechResourceType.UIModulePrefabs);
        gameObject.name = "uixPrfPanl_LC_TargetItem(Clone)";
      }
      gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
      TargetUIItem targetItem = gameObject.GetComponent<TargetUIItem>();
      if (null == targetItem) { targetItem = gameObject.AddComponent<TargetUIItem>(); }
      targetItem.layoutElement = gameObject.GetComponent<LayoutElement>();
      if (null == targetItem.layoutElement) { targetItem.layoutElement = gameObject.AddComponent<LayoutElement>(); }
      targetItem.layoutElement.ignoreLayout = false;
      targetItem.data = dataElement;
      targetItem.parent = this;
      LanceMechEquipmentListItem component = gameObject.GetComponent<LanceMechEquipmentListItem>();
      targetItem.ui = component;
      if (dataElement != null) {
        UIColor textColor = UIColor.White;
        UIColor bgColor = MechComponentRef.GetUIColor(dataElement.componentRef);
        if (dataElement.componentRef.DamageLevel == ComponentDamageLevel.Destroyed) {
          bgColor = UIColor.Disabled;
        }
        string text = new Localize.Text("{0} {1}", dataElement.componentRef.Def.Description.UIName, Mech.GetAbbreviatedChassisLocation(dataElement.componentRef.MountedLocation)).ToString();
        component.SetData(text, ComponentDamageLevel.Functional, textColor, bgColor);
        Traverse.Create(component).Field<UIColorRefTracker>("itemTextColor").Value.SetUIColor(textColor);
        dataElement.debugDetails = $"L:{dataElement.componentRef.LocalGUID()}\nT:{parent.componentRef.TargetComponentGUID()}";
        component.SetTooltipData(dataElement.componentRef.Def);
        Traverse.Create(component).Field<HBSTooltip>("EquipmentTooltip").Value.SetDefaultStateData(dataElement.debugDescription.GetTooltipStateData());
      }
      gameObject.transform.SetParent(listParent, false);
      if (dataElement != null) {
        targetItem.gameObject.SetActive(true);
        weaponsList.Add(targetItem);
      }
    }
    public void UpdateColors() {
      foreach(TargetUIItem weapon in weaponsList) {
        UIColor textColor = UIColor.White;
        if ((string.IsNullOrEmpty(parent.componentRef.TargetComponentGUID()) == false) && (parent.componentRef.TargetComponentGUID() == weapon.data.componentRef.LocalGUID())) {
          textColor = UIColor.Gold;
        }
        Traverse.Create(weapon.ui).Field<UIColorRefTracker>("itemTextColor").Value.SetUIColor(textColor);
        weapon.data.debugDetails = $"L:{weapon.data.componentRef.LocalGUID()}\nT:{parent.componentRef.TargetComponentGUID()}";
        Traverse.Create(weapon.ui).Field<HBSTooltip>("EquipmentTooltip").Value.SetDefaultStateData(weapon.data.debugDescription.GetTooltipStateData());
      }
    }
    public void Clear() {
      for (int index = this.weaponsList.Count - 1; index >= 0; --index) {
        LanceMechEquipmentListItem labItemSlotElement = this.weaponsList[index].ui;
        labItemSlotElement.gameObject.transform.SetParent((Transform)null, false);
        TargetUIItem weaponsOrderItem = this.weaponsList[index];
        if (weaponsOrderItem != null) {
          weaponsOrderItem.parent = null;
          weaponsOrderItem.data = null;
        }
        UIManager.Instance.dataManager.PoolGameObject("uixPrfPanl_LC_TargetItem", labItemSlotElement.gameObject);
      }
      this.weaponsList.Clear();
    }

  }
  public class TargetDataElement {
    public MechComponentRef componentRef { get; set; } = null;
    public BaseDescriptionDef debugDescription { get; set; } = null;
    public string debugDetails {
      set {
        Traverse.Create(debugDescription).Property<string>("Details").Value = value;
      }
    }
    public TargetDataElement(MechComponentRef compRef, MechDef mechDef) {
      componentRef = compRef;
      debugDescription = new BaseDescriptionDef("debug_guids", "GUIDS", string.Empty, string.Empty);
      debugDetails = $"LocalGUID:{compRef.LocalGUID()}\nSelectedTarget:";
    }
  }

  public class TargetUIItem : MonoBehaviour, IPointerClickHandler {
    public TargetDataElement data { get; set; } = null;
    public TargetsControl parent { get; set; } = null;
    public LayoutElement layoutElement { get; set; } = null;
    public LanceMechEquipmentListItem ui { get; set; } = null;
    private RectTransform f_rect = null;
    public RectTransform rect {
      get {
        if (f_rect == null) { f_rect = GetComponent<RectTransform>(); }
        return f_rect;
      }
    }
    public void OnPointerClick(PointerEventData eventData) {
      if(parent.parent.isHasAnyAddonType(this.data.componentRef, this.data.componentRef.GetAddonsFromSource(parent.parent.componentRef))) {
        parent.parent.PlaceError("this weapon already have addon of this type");
        return;
      }
      parent.parent.ClearError();
      if(string.IsNullOrEmpty(parent.parent.componentRef.TargetComponentGUID()) == false) {
        if(parent.parent.componetByGuid.TryGetValue(parent.parent.componentRef.TargetComponentGUID(), out var prevTargetRef)) {
          parent.parent.removeAddon(prevTargetRef, parent.parent.componentRef);
        }
      }
      parent.parent.placeAddon(this.data.componentRef, parent.parent.componentRef);
      parent.UpdateColors();
    }
  }

  public class TargetsPopupSupervisor : MonoBehaviour {
    public enum PopupState { Main };
    public GenericPopup popup { get; set; } = null;
    public PopupState state { get; set; } = PopupState.Main;
    public TargetsControl targetsControl { get; set; } = null;
    public HBSButton backButton { get; set; } = null;
    public HBSButton saveButton { get; set; } = null;
    public MechComponentRef componentRef { get; set; } = null;
    public HBSTooltip helpTooltip { get; set; } = null;
    public MechDef mechDef { get; set; } = null;
    public List<MechComponentRef> inventory { get; set; } = new List<MechComponentRef>();
    public Dictionary<string, HashSet<string>> placedAddons = new Dictionary<string, HashSet<string>>();
    public Dictionary<string, MechComponentRef> componetByGuid = new Dictionary<string, MechComponentRef>();
    public void PlaceError(string errorStr) {
      targetsControl.componentCountText.SetText("<color=red>__/CAE.TARGET.ERROR/__</color>");
      helpTooltip.SetDefaultStateData(new BaseDescriptionDef("TargetsHelpTooltipID", "__/CAE.TRG.ERORR/__", errorStr, string.Empty).GetTooltipStateData());
    }
    public void ClearError() {
      targetsControl.componentCountText.SetText("__/CAE.TARGET.HELP/__");
      helpTooltip.SetDefaultStateData(new BaseDescriptionDef("TargetsHelpTooltipID", "__/CAE.TRG.USAGE/__", "__/CAE.TRG.USAGE.TARGETS.DETAILS/__", string.Empty).GetTooltipStateData());
    }
    public static void ResolveAddonsOnInventory(List<MechComponentRef> inventory) {
      Dictionary<MechComponentRef, HashSet<string>> placedAddons = new Dictionary<MechComponentRef, HashSet<string>>();
      Dictionary<string, MechComponentRef> componetByGuid = new Dictionary<string, MechComponentRef>();
      Log.Debug?.TWL(0, "ResolveAddonsOnInventory");
      foreach (var invItem in inventory) {
        if (invItem == null) { continue; }
        string LocalGUID = invItem.LocalGUID();
        if (string.IsNullOrEmpty(LocalGUID)) { LocalGUID = Guid.NewGuid().ToString(); }
        invItem.LocalGUID(LocalGUID);
        Log.Debug?.WL(1, $"{invItem.ComponentDefID} SimGameUID:{invItem.SimGameUID} LocalGUID:{invItem.LocalGUID()} TargetComponentGUID:{invItem.TargetComponentGUID()} MountedLocation:{invItem.MountedLocation}");
        componetByGuid.Add(LocalGUID, invItem);
      }
      foreach (var invItem in inventory) {
        if (string.IsNullOrEmpty(invItem.TargetComponentGUID())) { continue; }
        if (invItem.Def.isHasTarget() == false) { continue; }
        if (componetByGuid.TryGetValue(invItem.TargetComponentGUID(), out var targetRef)) {
          if (placedAddons.TryGetValue(targetRef, out var targetAddons) == false) {
            targetAddons = new HashSet<string>();
          }
          foreach (var addon in targetRef.GetAddonsFromSource(invItem)) {
            targetAddons.Add(addon.safeAddonType);
          }
          placedAddons[targetRef] = targetAddons;
        } else {
          invItem.TargetComponentGUID(string.Empty);
        }
      }
      foreach(var invItem in inventory) {
        if (invItem.Def.isHasTarget() == false) { continue; }
        if (invItem.Def.isAutoTarget() == false) { continue; }
        if (string.IsNullOrEmpty(invItem.TargetComponentGUID()) == false) { continue; }
        foreach (var targetItem in inventory) {
          if (targetItem.CanBeTarget(invItem) == false) { continue; }
          HashSet<WeaponAddonDef> addons = targetItem.GetAddonsFromSource(invItem);
          if (placedAddons.TryGetValue(targetItem, out var targetAddons)) {
            bool alreadyHasAddon = false;
            foreach(var addon in addons) {
              if (targetAddons.Contains(addon.safeAddonType)) { alreadyHasAddon = true; break; }
            }
            if (alreadyHasAddon) { continue; }
          } else {
            targetAddons = new HashSet<string>();
          }
          foreach (var addon in addons) {
            targetAddons.Add(addon.safeAddonType);
          }          
          placedAddons[targetItem] = targetAddons;
          invItem.TargetComponentGUID(targetItem.LocalGUID());
          break;
        }
      }
    }
    public void GatherAddonsInfo() {
      componetByGuid.Clear();
      placedAddons.Clear();
      Log.Debug?.TWL(0, "GatherAddonsInfo");
      foreach (var invItem in inventory) {
        if (invItem == null) { continue; }
        string LocalGUID = invItem.LocalGUID();
        if (string.IsNullOrEmpty(LocalGUID)) { LocalGUID = Guid.NewGuid().ToString(); }
        invItem.LocalGUID(LocalGUID);
        Log.Debug?.WL(1, $"{invItem.ComponentDefID} SimGameUID:{invItem.SimGameUID} LocalGUID:{invItem.LocalGUID()} TargetComponentGUID:{invItem.TargetComponentGUID()} MountedLocation:{invItem.MountedLocation}");
        componetByGuid.Add(LocalGUID, invItem);
      }
      foreach (var invItem in inventory) {
        if (string.IsNullOrEmpty(invItem.TargetComponentGUID())) { continue; }
        if (invItem.Def.isHasTarget() == false) { continue; }
        if (componetByGuid.TryGetValue(invItem.TargetComponentGUID(), out var targetRef)) {
          if (this.placedAddons.TryGetValue(targetRef.LocalGUID(), out var targetAddons) == false) {
            targetAddons = new HashSet<string>();
          }
          foreach (var addon in targetRef.GetAddonsFromSource(invItem)) {
            targetAddons.Add(addon.safeAddonType);
          }
          this.placedAddons[targetRef.LocalGUID()] = targetAddons;
        } else {
          invItem.TargetComponentGUID(string.Empty);
        }
      }
      foreach (var placedAddon in this.placedAddons) {
        Log.Debug?.WL(1, $"{placedAddon.Key}");
        foreach(var addonType in placedAddon.Value) {
          Log.Debug?.WL(2, $"{addonType}");
        }
      }
    }
    public bool isHasAnyAddonType(MechComponentRef testingRef, HashSet<WeaponAddonDef> addons) {
      Log.Debug?.TW(0,$"isHasAnyAddonType {testingRef.LocalGUID()}");
      foreach (var addon in addons) { Log.Debug?.W(1,addon.safeAddonType); }; Log.Debug?.WL(0,"");
      if (string.IsNullOrEmpty(testingRef.LocalGUID())) {
        return false;
      }
      if (placedAddons.TryGetValue(testingRef.LocalGUID(), out var targetAddons) == false) { return false; }
      foreach (var addon in addons) { if (targetAddons.Contains(addon.safeAddonType)) { return true; } }
      return false;
    }
    public bool isHasAddon(MechComponentRef testingRef, MechComponentRef addonSource) {
      if (string.IsNullOrEmpty(testingRef.LocalGUID())) { return false; }
      if (string.IsNullOrEmpty(addonSource.TargetComponentGUID())) { return false; }
      return testingRef.LocalGUID() == addonSource.TargetComponentGUID();
    }
    public void removeAddon(MechComponentRef testingRef, MechComponentRef addonSource) {
      if (string.IsNullOrEmpty(testingRef.LocalGUID())) { return; }
      if (string.IsNullOrEmpty(addonSource.TargetComponentGUID())) { return; }
      addonSource.TargetComponentGUID(string.Empty);
      if (placedAddons.TryGetValue(testingRef.LocalGUID(), out var addons)) {
        foreach (var addon in testingRef.GetAddonsFromSource(addonSource)) {
          addons.Remove(addon.safeAddonType);
        }
      }
    }
    public void placeAddon(MechComponentRef targetRef, MechComponentRef addonSource) {
      HashSet<WeaponAddonDef> addAddons = targetRef.GetAddonsFromSource(addonSource); ;
      if (string.IsNullOrEmpty(targetRef.LocalGUID())) {
        targetRef.LocalGUID(Guid.NewGuid().ToString());
        if (string.IsNullOrEmpty(targetRef.LocalGUID()) == false) {
          componetByGuid.Add(targetRef.LocalGUID(), targetRef);
        }
      }
      if (string.IsNullOrEmpty(targetRef.LocalGUID()) == false) {
        if(placedAddons.TryGetValue(targetRef.LocalGUID(), out var haveAddons) == false) {
          haveAddons = new HashSet<string>();
        }
        foreach (var addon in addAddons) {
          haveAddons.Add(addon.safeAddonType);
        }
        placedAddons[targetRef.LocalGUID()] = haveAddons;
        addonSource.TargetComponentGUID(targetRef.LocalGUID());
      }
    }
    private static TargetsPopupSupervisor f_Instance = null;
    public static TargetsPopupSupervisor Instance {
      get {
        if(f_Instance == null) {
          f_Instance = UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<TargetsPopupSupervisor>(true);
          if (f_Instance == null) {
            GameObject go = new GameObject("TargetsPopupSupervisor");
            go.transform.SetParent(UIManager.Instance.UIRoot.gameObject.transform);
            f_Instance = go.AddComponent<TargetsPopupSupervisor>();
            f_Instance.instantine();
          }
        }
        return f_Instance;
      }
    }
    public void instantine() {
      if (targetsControl != null) { return; }
      try {
        GameObject uixPrfPanl_ML_main_Widget = LazySingletonBehavior<UIManager>.Instance.dataManager.PooledInstantiate("uixPrfPanl_ML_main-Widget", BattleTechResourceType.UIModulePrefabs);
        if (uixPrfPanl_ML_main_Widget == null) {
          Log.Debug?.TWL(0, "uixPrfPanl_ML_main-Widget not found");
          return;
        }
        Log.Debug?.TWL(0, "uixPrfPanl_ML_main-Widget found");
        MechLabDismountWidget dismountWidget = uixPrfPanl_ML_main_Widget.GetComponentInChildren<MechLabDismountWidget>(true);
        if (dismountWidget == null) {
          Log.Debug?.TWL(0, "MechLabDismountWidget not found");
          return;
        }
        Log.Debug?.TWL(0, "MechLabDismountWidget found");

        {
          GameObject controlGO = GameObject.Instantiate(dismountWidget.gameObject);
          controlGO.name = "ui_weaponsOrder";
          controlGO.transform.localScale = Vector3.one;
          MechLabDismountWidget localWidget = controlGO.GetComponent<MechLabDismountWidget>();
          controlGO.FindObject<LocalizableText>("txt_label").SetText("__/CAE.TRG.TARGETS/__");
          controlGO.FindObject<LocalizableText>("txt_instr").gameObject.SetActive(false);
          targetsControl = controlGO.AddComponent<TargetsControl>();
          targetsControl.listParent = Traverse.Create(localWidget).Field<RectTransform>("listParent").Value;
          targetsControl.componentCountText = Traverse.Create(localWidget).Field<LocalizableText>("componentCountText").Value;
          GameObject.Destroy(localWidget);
          targetsControl.componentCountText.SetText("__/CAE.TARGET.HELP/__");
          helpTooltip = targetsControl.componentCountText.gameObject.AddComponent<HBSTooltip>();
          helpTooltip.SetDefaultStateData(new BaseDescriptionDef("TargetsHelpTooltipID", "__/CAE.TRG.USAGE/__", "__/CAE.TRG.USAGE.TARGETS.DETAILS/__", string.Empty).GetTooltipStateData());
          VerticalLayoutGroup verticalLayoutGroup = targetsControl.listParent.gameObject.GetComponent<VerticalLayoutGroup>();
          verticalLayoutGroup.spacing = 22f;
          targetsControl.gameObject.SetActive(false);
          targetsControl.parent = this;
          targetsControl.gameObject.transform.SetParent(this.transform);
        }

        LazySingletonBehavior<UIManager>.Instance.dataManager.PoolGameObject("uixPrfPanl_ML_main-Widget", uixPrfPanl_ML_main_Widget);
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
    public void OnBack() {
      if (state == PopupState.Main) { OnClose(); }
    }
    public void OnTargetsButtonClick(MechComponentRef componentRef, MechDef mechDef, List<MechComponentRef> inventory) {
      if (mechDef == null) { return; }
      GenericPopupBuilder builder = GenericPopupBuilder.Create("__/CAE.TRG.LIST/__", "PLACEHOLDER");
      builder.AddButton("__/CAE.TRG.CLOSE/__", new Action(this.OnBack), false);
      popup = builder.CancelOnEscape().Render();
      backButton = Traverse.Create(popup).Field<List<HBSButton>>("buttons").Value[0];
      this.mechDef = mechDef;
      this.inventory = (inventory == null) ? this.mechDef.Inventory.ToList() : inventory;
      this.componentRef = componentRef;
      this.OnShow();
    }
    public void OnClose() {
      if (targetsControl != null) {
        targetsControl.gameObject.SetActive(false);
        targetsControl.gameObject.transform.SetParent(this.transform);
        targetsControl.Clear();
        this.ClearError();
      }
      this.componetByGuid.Clear();
      this.placedAddons.Clear();
      //this.mechDef = null;
      this.componentRef = null;
      if (popup != null) {
        Traverse.Create(popup).Field<LocalizableText>("_contentText").Value.gameObject.SetActive(true);
        popup.Pool();
        popup = null;
      }
    }
    public void OnShow() {
      if ((targetsControl != null) && (popup != null)) {
        try {
          LocalizableText _contentText = Traverse.Create(popup).Field<LocalizableText>("_contentText").Value;
          _contentText.gameObject.SetActive(false);
          {
            RectTransform controlRT = targetsControl.gameObject.GetComponent<RectTransform>();
            controlRT.pivot = _contentText.rectTransform.pivot;
            controlRT.sizeDelta = new Vector2(_contentText.rectTransform.sizeDelta.x, controlRT.sizeDelta.y);

            targetsControl.gameObject.SetActive(true);
            targetsControl.gameObject.transform.SetParent(_contentText.gameObject.transform.parent);
            targetsControl.gameObject.transform.SetSiblingIndex(_contentText.transform.GetSiblingIndex() + 1);
            LayoutElement layoutElement = targetsControl.gameObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
          }
          GameObject contentTextGO = GameObject.Instantiate(_contentText.gameObject);
          contentTextGO.SetActive(false);
          contentTextGO.transform.SetParent(_contentText.gameObject.transform.parent);
          contentTextGO.transform.SetSiblingIndex(targetsControl.transform.GetSiblingIndex() + 1);
          contentTextGO.transform.localScale = Vector3.one;
          GameObject.Destroy(contentTextGO.GetComponent<LocalizableText>());
          HorizontalLayoutGroup group = contentTextGO.AddComponent<HorizontalLayoutGroup>();
          group.spacing = 8f;
          group.padding = new RectOffset(10, 10, 0, 0);
          group.childAlignment = TextAnchor.UpperCenter;
          group.childControlHeight = false;
          group.childControlWidth = false;
          group.childForceExpandHeight = false;
          group.childForceExpandWidth = false;

          //if (mechDef == null) { return; }
          if (componentRef == null) { return; }
          if (componentRef.Def.isHasTarget() == false) { return; }
          this.GatherAddonsInfo();

          List<MechComponentRef> possibleTargets = new List<MechComponentRef>();
          for (int index = 0; index < inventory.Count; ++index) {
            if (inventory[index].CanBeTarget(componentRef) == false) { continue; }
            if ((string.IsNullOrEmpty(inventory[index].LocalGUID()) == false) && (string.IsNullOrEmpty(componentRef.TargetComponentGUID()) == false)) {
              if (componentRef.TargetComponentGUID() == inventory[index].LocalGUID()) {
                possibleTargets.Add(inventory[index]);
                continue;
              }
            }
            possibleTargets.Add(inventory[index]);
            if (componentRef.Def.isAutoTarget() && string.IsNullOrEmpty(componentRef.TargetComponentGUID())) {
              HashSet<WeaponAddonDef> addons = inventory[index].GetAddonsFromSource(componentRef);              
              if (this.isHasAnyAddonType(inventory[index], addons) == false) {
                this.placeAddon(inventory[index], componentRef);
              }
            }
          }
          Thread.CurrentThread.pushActorDef(mechDef);
          for (int index = 0; index < possibleTargets.Count; ++index) {
            targetsControl.AddWeaponUIItem(new TargetDataElement(possibleTargets[index], mechDef));
          }
          Thread.CurrentThread.clearActorDef();
          targetsControl.UpdateColors();
        } catch (Exception e) {
          Log.Error?.TWL(0, e.ToString(), true);
        }
      }
    }
  }
  public class LanceMechEquipmentListItemButton: EventTrigger {
    public SVGImage svg { get; set; } = null;
    public Image img { get; set; } = null;
    public ComponentRefTracker parent { get; set; } = null;
    public override void OnPointerEnter(PointerEventData eventData) {
      Log.Debug?.TWL(0, "LanceMechEquipmentListItemButton.OnPointerEnter " + parent.componentRef.ComponentDefID);
      svg.color = UIManager.Instance.UIColorRefs.orange;
    }
    public override void OnPointerExit(PointerEventData eventData) {
      Log.Debug?.TWL(0, "LanceMechEquipmentListItemButton.OnPointerExit " + parent.componentRef.ComponentDefID);
      svg.color = UIManager.Instance.UIColorRefs.white;
    }
    public override void OnPointerClick(PointerEventData eventData) {
      Log.Debug?.TWL(0, "LanceMechEquipmentListItemButton.OnPointerClick "+ parent.componentRef.ComponentDefID);
      TargetsPopupSupervisor.Instance.OnTargetsButtonClick(parent.componentRef, parent.parent.mechDef, parent.parent.inventory);
    }
  }
  public class ComponentRefTrackersList: MonoBehaviour {
    public MechDef mechDef { get; set; } = null;
    public List<MechComponentRef> inventory { get; set; } = new List<MechComponentRef>();

  }
  public class ComponentRefTracker: MonoBehaviour {
    private MechComponentRef f_componentRef = null;
    public MechComponentRef componentRef {
      get {
        return f_componentRef;
      }
      set {
        f_componentRef = value;
      }
    }
    public ComponentRefTrackersList parent { get; set; } = null;
    public LanceMechEquipmentListItemButton settingsButton { get; set; }
    public LanceMechEquipmentListItem listItem { get; set; }
    private bool uiInited = false;
    public void Update() {
      if(uiInited == false) {
        RectTransform rectTransform = this.GetComponent<RectTransform>();
        settingsButton.img.rectTransform.anchoredPosition = new Vector2(rectTransform.sizeDelta.x, 1f);
        uiInited = true;
      }
    }
    public void Init() {
      RectTransform rectTransform = this.GetComponent<RectTransform>();
      settingsButton = listItem.GetComponentInChildren<LanceMechEquipmentListItemButton>();
      if (settingsButton == null) {
        GameObject itemGO = new GameObject("settingsImg");
        itemGO.transform.SetParent(listItem.gameObject.transform);
        Image img = itemGO.AddComponent<Image>();
        img.rectTransform.pivot = new Vector2(1f,0);
        img.rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.y+2f, rectTransform.sizeDelta.y-2f);
        img.color = Color.clear;
        img.rectTransform.anchoredPosition = new Vector2(rectTransform.sizeDelta.x, 1f);
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.zero;
        GameObject svgGO = new GameObject("icon");
        svgGO.transform.SetParent(itemGO.transform);
        svgGO.transform.localPosition = Vector3.zero;
        svgGO.transform.localScale = Vector3.one;
        SVGImage svg = svgGO.AddComponent<SVGImage>();
        svg.rectTransform.pivot = Vector2.zero;
        svg.rectTransform.sizeDelta = img.rectTransform.sizeDelta;
        svg.rectTransform.anchorMin = Vector2.zero;
        svg.rectTransform.anchorMax = Vector2.zero;
        CustomSvgCache.setIcon(svg, "gears", UIManager.Instance.dataManager);
        settingsButton = itemGO.AddComponent<LanceMechEquipmentListItemButton>();
        settingsButton.svg = svg;
        settingsButton.img = img;
      }
      settingsButton = listItem.GetComponentInChildren<LanceMechEquipmentListItemButton>();
      settingsButton.parent = this;
      if ((this.componentRef != null)&&(this.componentRef.Def != null)) {
        settingsButton.img.gameObject.SetActive(this.componentRef.Def.isHasTarget());
      } else {
        settingsButton.img.gameObject.SetActive(false);
      }
    }
  }
  [HarmonyPatch(typeof(LanceMechEquipmentList))]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch("SetLoadout")]
  [HarmonyPatch(new Type[] { })]
  public static class LanceMechEquipmentList_SetLoadout {
    private delegate string d_Field_get(BattleTech.BaseComponentRef src);
    private delegate void d_Field_set(BattleTech.BaseComponentRef src, string value);
    private delegate MechComponentRef d_MechComponentRef_get(BattleTech.UI.LanceMechEquipmentListItem src);
    private delegate void d_MechComponentRef_set(BattleTech.UI.LanceMechEquipmentListItem src, MechComponentRef value);
    private delegate MechDef d_MechDef_get(BattleTech.UI.LanceMechEquipmentListItem src);
    private delegate void d_MechDef_set(BattleTech.UI.LanceMechEquipmentListItem src, MechDef value);
    private static d_Field_get i_LocalGUID_get = null;
    private static d_Field_set i_LocalGUID_set = null;
    private static d_Field_get i_TargetComponentGUID_get = null;
    private static d_Field_set i_TargetComponentGUID_set = null;
    private static d_MechComponentRef_get i_MechComponentRef_get = null;
    private static d_MechComponentRef_set i_MechComponentRef_set = null;
    private static d_MechDef_get i_MechDef_get = null;
    private static d_MechDef_set i_MechDef_set = null;
    public static string LocalGUID(this BattleTech.BaseComponentRef src, bool cached = true) {
      if (i_LocalGUID_get == null) { return string.Empty; }
      if (cached) { src.ReadAdditionaldataRegistry();  }
      return i_LocalGUID_get(src);
    }
    public static void LocalGUID(this BattleTech.BaseComponentRef src, string value, bool update = true) {
      if (i_LocalGUID_set == null) { return; }
      i_LocalGUID_set(src, value);
      if (update) { src.WriteAdditionaldataRegistry(); }
    }
    public static string TargetComponentGUID(this BattleTech.BaseComponentRef src, bool cached = true) {
      if (i_TargetComponentGUID_get == null) { return string.Empty; }
      if (cached) { src.ReadAdditionaldataRegistry(); }
      return i_TargetComponentGUID_get(src);
    }
    public static void TargetComponentGUID(this BattleTech.BaseComponentRef src, string value, bool update = true) {
      if (i_TargetComponentGUID_set == null) { return; }
      i_TargetComponentGUID_set(src, value);
      if (update) { src.WriteAdditionaldataRegistry(); }
    }
    public static MechComponentRef componentRef(this LanceMechEquipmentListItem src) {
      if (i_MechComponentRef_get == null) { return null; }
      return i_MechComponentRef_get(src);
    }
    public static void componentRef(this LanceMechEquipmentListItem src, MechComponentRef value) {
      if (i_MechComponentRef_set == null) { return; }
      i_MechComponentRef_set(src, value);
    }
    public static MechDef mechDef(this LanceMechEquipmentListItem src) {
      if (i_MechDef_get == null) { return null; }
      return i_MechDef_get(src);
    }
    public static void mechDef(this LanceMechEquipmentListItem src, MechDef value) {
      if (i_MechDef_set == null) { return; }
      i_MechDef_set(src, value);
    }
    private static bool? PrepareCalled = new bool?();
    public static bool Prepare() {
      if (PrepareCalled.HasValue) { return PrepareCalled.Value; }
      PrepareCalled = true;
      FieldInfo LocalGUID = typeof(BattleTech.BaseComponentRef).GetField("LocalGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.LocalGUID {(LocalGUID == null ? "not found" : "found")}");
      if (LocalGUID != null) {
        {
          var dm = new DynamicMethod("get_LocalGUID", typeof(string), new Type[] { typeof(BattleTech.BaseComponentRef) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldfld, LocalGUID);
          gen.Emit(OpCodes.Ret);
          i_LocalGUID_get = (d_Field_get)dm.CreateDelegate(typeof(d_Field_get));
        }
        {
          var dm = new DynamicMethod("set_LocalGUID", null, new Type[] { typeof(BattleTech.BaseComponentRef), typeof(string) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldarg_1);
          gen.Emit(OpCodes.Stfld, LocalGUID);
          gen.Emit(OpCodes.Ret);
          i_LocalGUID_set = (d_Field_set)dm.CreateDelegate(typeof(d_Field_set));
        }
      } else { PrepareCalled = false; return false; }
      FieldInfo TargetComponentGUID = typeof(BattleTech.BaseComponentRef).GetField("TargetComponentGUID", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"BaseComponentRef.TargetComponentGUID {(TargetComponentGUID == null ? "not found" : "found")}");
      if (TargetComponentGUID != null) {
        {
          var dm = new DynamicMethod("get_TargetComponentGUID", typeof(string), new Type[] { typeof(BattleTech.BaseComponentRef) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldfld, TargetComponentGUID);
          gen.Emit(OpCodes.Ret);
          i_TargetComponentGUID_get = (d_Field_get)dm.CreateDelegate(typeof(d_Field_get));
        }
        {
          var dm = new DynamicMethod("set_TargetComponentGUID", null, new Type[] { typeof(BattleTech.BaseComponentRef), typeof(string) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldarg_1);
          gen.Emit(OpCodes.Stfld, TargetComponentGUID);
          gen.Emit(OpCodes.Ret);
          i_TargetComponentGUID_set = (d_Field_set)dm.CreateDelegate(typeof(d_Field_set));
        }
      } else { PrepareCalled = false; return false; }
      FieldInfo componentRef = typeof(LanceMechEquipmentListItem).GetField("componentRef", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"LanceMechEquipmentListItem.componentRef {(componentRef == null ? "not found" : "found")}");
      if (componentRef != null) {
        {
          var dm = new DynamicMethod("get_componentRef", typeof(MechComponentRef), new Type[] { typeof(LanceMechEquipmentListItem) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldfld, componentRef);
          gen.Emit(OpCodes.Ret);
          i_MechComponentRef_get = (d_MechComponentRef_get)dm.CreateDelegate(typeof(d_MechComponentRef_get));
        }
        {
          var dm = new DynamicMethod("set_componentRef", null, new Type[] { typeof(LanceMechEquipmentListItem), typeof(MechComponentRef) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldarg_1);
          gen.Emit(OpCodes.Stfld, componentRef);
          gen.Emit(OpCodes.Ret);
          i_MechComponentRef_set = (d_MechComponentRef_set)dm.CreateDelegate(typeof(d_MechComponentRef_set));
        }
      } else { PrepareCalled = false; return false; }
      FieldInfo mechDef = typeof(LanceMechEquipmentListItem).GetField("mechDef", BindingFlags.Public | BindingFlags.Instance);
      Log.Debug?.WL(1, $"LanceMechEquipmentListItem.mechDef {(componentRef == null ? "not found" : "found")}");
      if (componentRef != null) {
        {
          var dm = new DynamicMethod("get_mechDef", typeof(MechDef), new Type[] { typeof(LanceMechEquipmentListItem) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldfld, componentRef);
          gen.Emit(OpCodes.Ret);
          i_MechDef_get = (d_MechDef_get)dm.CreateDelegate(typeof(d_MechDef_get));
        }
        {
          var dm = new DynamicMethod("set_mechDef", null, new Type[] { typeof(LanceMechEquipmentListItem), typeof(MechDef) });
          var gen = dm.GetILGenerator();
          gen.Emit(OpCodes.Ldarg_0);
          gen.Emit(OpCodes.Ldarg_1);
          gen.Emit(OpCodes.Stfld, componentRef);
          gen.Emit(OpCodes.Ret);
          i_MechDef_set = (d_MechDef_set)dm.CreateDelegate(typeof(d_MechDef_set));
        }
      } else { PrepareCalled = false; return false; }
      return true;
    }
    public static void Postfix(LanceMechEquipmentList __instance, List<GameObject> ___allComponents, MechDef ___activeMech) {
      try {
        Log.Debug?.TWL(0,$"LanceMechEquipmentList.SetLoadout {___allComponents.Count}");
        ComponentRefTrackersList componentRefTrackersList = __instance.gameObject.GetComponent<ComponentRefTrackersList>();
        if (componentRefTrackersList == null) { componentRefTrackersList = __instance.gameObject.AddComponent<ComponentRefTrackersList>(); }
        componentRefTrackersList.mechDef = ___activeMech;
        componentRefTrackersList.inventory.Clear();
        foreach (var go in ___allComponents) {
          LanceMechEquipmentListItem component = go.GetComponent<LanceMechEquipmentListItem>();
          if (component == null) { continue; }
          ComponentRefTracker refTracker = go.GetComponent<ComponentRefTracker>();
          if (refTracker == null) { refTracker = go.AddComponent<ComponentRefTracker>(); }
          refTracker.listItem = component;
          Log.Debug?.WL(1, $"{(component.componentRef()==null?"null": component.componentRef().ComponentDefID )}");
          refTracker.componentRef = component.componentRef();
          refTracker.parent = componentRefTrackersList;
          componentRefTrackersList.inventory.Add(refTracker.componentRef);
          //refTracker.mechDef = component.mechDef();
          refTracker.Init();
        }
        TargetsPopupSupervisor.ResolveAddonsOnInventory(componentRefTrackersList.inventory);
      } catch(Exception e) {
        Log.Error?.TWL(0,e.ToString());
      }
    }
  }
  [HarmonyPatch(MethodType.Normal)]
  public static class LanceMechEquipmentListItem_SetComponentRef {
    public static bool Prepare() {
      return AccessTools.Method(typeof(LanceMechEquipmentListItem), "SetComponentRef") != null;
    }
    public static MethodBase TargetMethod() {
      return AccessTools.Method(typeof(LanceMechEquipmentListItem), "SetComponentRef");
    }
    public static void Postfix(LanceMechEquipmentListItem __instance, MechComponentRef componentRef, MechDef mechDef) {
      try {
        string LocalGUID = componentRef == null? "null": componentRef.LocalGUID();
        string SimgameUID = componentRef == null ? "null" : componentRef.SimGameUID;
        string TargetGUID = componentRef == null ? "null" : componentRef.TargetComponentGUID();
        Log.Debug?.TWL(0, $"LanceMechEquipmentListItem.SetComponentRef {(componentRef==null?"null":componentRef.ComponentDefID)} LocalGUID:{LocalGUID} SimGameUID:{SimgameUID} TargetGUID:{TargetGUID}");
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
  public static class SimGameState_Dehydrate {
    static void Prefix(SimGameState __instance) {
      Log.Debug?.TWL(0, "SimGameState.Dehydrate");
      try {
        __instance.SanitizeAdditionalComponentsData();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
      try {
        __instance.ComponentsAdditionaDataSave();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
  public static class SimGameState_Rehydrate {
    static void Postfix(SimGameState __instance) {
      Log.Debug?.TWL(0, "SimGameState.Rehydrate");
      try {
        __instance.ComponentsAdditionaDataLoad();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch("AssignAmmoToWeapons")]
  [HarmonyPatch(new Type[] { })]
  public static class AbstractActor_AssignAmmoToWeapons {
    public static void Postfix(AbstractActor __instance) {
      try {
        Log.Debug?.TWL(0, "AbstractActor.AssignAmmoToWeapons " + __instance.PilotableActorDef.Description.Id);
        __instance.InitweaponsAddons();
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

}