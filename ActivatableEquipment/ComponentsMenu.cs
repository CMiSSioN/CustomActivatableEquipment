using BattleTech;
using BattleTech.UI;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomActivatableEquipment {
  public class ComponentsMenu {
    public GenericPopup popup;
    public List<MechComponent> components;
    public Dictionary<MechComponent, string> componentsStates;
    public int SelectedComponent;
    public ComponentsMenu(AbstractActor unit) {
      SelectedComponent = 0;
      components = new List<MechComponent>();
      componentsStates = new Dictionary<MechComponent, string>();
      popup = null;
      foreach (MechComponent component in unit.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable != null) { components.Add(component); };
      }
    }
    public void Render() {
      GenericPopupBuilder builder = GenericPopupBuilder.Create("__/CAE.Components/__", this.BuildText());
      builder.AddButton("X", null, true);
      builder.AddButton("+", new Action(this.Left), false);
      builder.AddButton("<-", new Action(this.Up), false);
      builder.AddButton("-", new Action(this.Right), false);
      builder.AddButton("->", new Action(this.Down), false);
      builder.AddButton("Ок", null, true);
      popup = builder.CancelOnEscape().Render();
    }
    public void Left() {

    }
    public void Right() {

    }
    public string BuildText(){
      StringBuilder builder = new StringBuilder();
      for(int index = 0; index < components.Count; ++index) {
        if (index != 0) { builder.Append("\n"); };
        if (index == SelectedComponent) { builder.Append("->"); }
        MechComponent component = components[index];
        builder.Append(component.UIName);
        if (component.IsFunctional == false) {
          builder.Append(" !__/CAE.NonFunctional/__!");continue;
        }
        if (component.DamageLevel >= ComponentDamageLevel.Penalized) {
          builder.Append(" !__/CAE.Damaged/__!");
        }
        if (ActivatableComponent.isOutOfCharges(components[index])) {
          builder.Append(" !__/CAE.OutOfCharges/__!");
          continue;
        }
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable.ChargesCount != 0) {
          if (activatable.ChargesCount > 0) {
            builder.Append(" __/CAE.CHARGES/__:" + ActivatableComponent.getChargesCount(component));
          }
        }
        if (ActivatableComponent.isComponentActivated(component)) {
          builder.Append(" " + activatable.ActivationMessage + " ");
          if (activatable.CanBeactivatedManualy == false) {
            if (component.parent is Mech) {
              float neededHeat = (activatable.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activatable.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activatable.AutoDeactivateOnHeat;
              builder.Append("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        } else {
          builder.Append(" " + activatable.DeactivationMessage + " ");
          if (activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
            if (component.parent is Mech) {
              float neededHeat = (activatable.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activatable.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activatable.AutoActivateOnHeat;
              builder.Append("__/CAE.HEAT/__:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        }
        float failChance = ActivatableComponent.getEffectiveComponentFailChance(component);
        if (failChance > Core.Epsilon) {
          builder.Append(" __/CAE.FAIL/__:" + Math.Round(failChance * 100f) + "%");
        }
      }
      return builder.ToString();
    }
    public void Up() {
      if(SelectedComponent > 0) {
        SelectedComponent -= 1;
      } else {
        SelectedComponent = this.components.Count - 1;
      }
      popup.TextContent = this.BuildText();
    }
    public void Down() {
      if (SelectedComponent < this.components.Count - 1) {
        SelectedComponent += 1;
      } else {
        SelectedComponent = 0;
      }
      popup.TextContent = this.BuildText();
    }
  }
}