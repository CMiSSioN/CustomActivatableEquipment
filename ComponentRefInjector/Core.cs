using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace ComponentDefInjector {
  public class Settings {
    public bool debugLog { get; set; } = true;
  }
  internal static class Injector {
    public static Settings settings { get; set; } = new Settings();
    public static string AssemblyDirectory {
      get {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        UriBuilder uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        return Path.GetDirectoryName(path);
      }
    }
    internal static MethodReference HBS_Util_Serialization_StorageSpaceString { get; set; } = null;
    internal static MethodReference HBS_Util_SerializationStream_PutString { get; set; } = null;
    internal static MethodReference HBS_Util_SerializationStream_GetString { get; set; } = null;
    internal static MethodReference HBS_Util_SerializationStream_PutBool { get; set; } = null;
    internal static MethodReference HBS_Util_SerializationStream_GetBool { get; set; } = null;
    internal static FieldReference HBS_Util_Serialization_STORAGE_SPACE_BOOL { get; set; } = null;
    public static void InjectSize(TypeDefinition BaseComponentRef, params FieldDefinition[] fields) {
      MethodDefinition sizeMethod = BaseComponentRef.Methods.First(x => x.Name == "Size");
      if (sizeMethod == null) {
        Log.Err?.WL(1, "can't find BattleTech.BaseComponentRef.Size method", true);
        return;
      }
      var get_SimGameUID = game.MainModule.ImportReference(BaseComponentRef.Properties.First(x => x.Name == "SimGameUID").GetMethod);
      ILProcessor body = sizeMethod.Body.GetILProcessor();
      int ti = -1;
      for (var i = 0; i < sizeMethod.Body.Instructions.Count; i++) {
        var instruction = sizeMethod.Body.Instructions[i];
        if ((instruction.OpCode == OpCodes.Call) && (instruction.Operand == get_SimGameUID)) { ti = i; }
      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find get_SimGameUID call", true);
        return;
      }
      if (sizeMethod.Body.Instructions[ti + 1].OpCode == OpCodes.Ldstr) {
        Log.Err?.WL(1, "already patched?!", true);
        return;
      }
      var instructions = new List<Instruction>() {
        body.Create(OpCodes.Ldstr, "_V2"),
        body.Create(OpCodes.Call, game.MainModule.ImportReference(typeof(string).GetMethod("Concat",new Type[] { typeof(string), typeof(string) }))),
      };
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(sizeMethod.Body.Instructions[ti], instruction); }
      ti = -1;
      for (var i = 0; i < sizeMethod.Body.Instructions.Count; i++) {
        var instruction = sizeMethod.Body.Instructions[i];
        if (instruction.OpCode == OpCodes.Ret) { ti = i; }

      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find Ret opcode", true);
        return;
      }
      instructions = new List<Instruction>();
      foreach (var field in fields) {
        instructions.AddRange(new List<Instruction>() {
            body.Create(OpCodes.Ldarg_0),
            body.Create(OpCodes.Ldfld, field),
            body.Create(OpCodes.Call, HBS_Util_Serialization_StorageSpaceString),
            body.Create(OpCodes.Add)
          }
        );
      }
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(sizeMethod.Body.Instructions[ti - 1], instruction); }

      Log.M?.TWL(0, $"InjectSize success");
      for (var i = 0; i < sizeMethod.Body.Instructions.Count; i++) {
        var instruction = sizeMethod.Body.Instructions[i];
        Log.M?.WL(1, instruction.OpCode + ":" + (instruction.Operand == null ? "null" : instruction.Operand.ToString()));
      }
      Log.M?.WL(0, $"method end", true);
    }
    public static void InjectSave(TypeDefinition BaseComponentRef, params FieldDefinition[] fields) {
      MethodDefinition saveMethod = BaseComponentRef.Methods.First(x => x.Name == "Save");
      if (saveMethod == null) {
        Log.Err?.WL(1, "can't find BattleTech.BaseComponentRef.Size method", true);
        return;
      }
      var get_SimGameUID = game.MainModule.ImportReference(BaseComponentRef.Properties.First(x => x.Name == "SimGameUID").GetMethod);
      ILProcessor body = saveMethod.Body.GetILProcessor();
      int ti = -1;
      for (var i = 0; i < saveMethod.Body.Instructions.Count; i++) {
        var instruction = saveMethod.Body.Instructions[i];
        if ((instruction.OpCode == OpCodes.Call) && (instruction.Operand == get_SimGameUID)) { ti = i; }
      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find get_SimGameUID call", true);
        return;
      }
      if (saveMethod.Body.Instructions[ti + 1].OpCode == OpCodes.Ldstr) {
        Log.Err?.WL(1, "already patched?!", true);
        return;
      }
      var instructions = new List<Instruction>() {
        body.Create(OpCodes.Ldstr, "_V2"),
        body.Create(OpCodes.Call, game.MainModule.ImportReference(typeof(string).GetMethod("Concat",new Type[] { typeof(string), typeof(string) }))),
      };
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(saveMethod.Body.Instructions[ti], instruction); }

      ti = -1;
      for (var i = 0; i < saveMethod.Body.Instructions.Count; i++) {
        var instruction = saveMethod.Body.Instructions[i];
        if (instruction.OpCode == OpCodes.Ret) { ti = i; }
      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find return opcode", true);
        return;
      }
      instructions = new List<Instruction>();
      foreach (var field in fields) {
        instructions.AddRange(new List<Instruction>(){
          body.Create(OpCodes.Ldarg_1),
          body.Create(OpCodes.Ldarg_0),
          body.Create(OpCodes.Ldfld, field),
          body.Create(OpCodes.Callvirt, HBS_Util_SerializationStream_PutString),
        });
      }
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(saveMethod.Body.Instructions[ti - 1], instruction); }
      Log.M?.TWL(0, $"InjectSave success");
      for (var i = 0; i < saveMethod.Body.Instructions.Count; i++) {
        var instruction = saveMethod.Body.Instructions[i];
        Log.M?.WL(1, instruction.OpCode + ":" + (instruction.Operand == null ? "null" : instruction.Operand.ToString()));
      }
      Log.M?.WL(0, $"method end", true);
    }
    public static void InjectLoad(TypeDefinition BaseComponentRef, params FieldDefinition[] fields) {
      MethodDefinition loadMethod = BaseComponentRef.Methods.First(x => x.Name == "Load");
      if (loadMethod == null) {
        Log.Err?.WL(1, "can't find BattleTech.BaseComponentRef.Size method", true);
        return;
      }
      int ti = -1;
      ILProcessor body = loadMethod.Body.GetILProcessor();
      for (var i = 0; i < loadMethod.Body.Instructions.Count; i++) {
        var instruction = loadMethod.Body.Instructions[i];
        if (instruction.OpCode == OpCodes.Ret) { ti = i; }

      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find return opcode", true);
        return;
      }
      Instruction ret = loadMethod.Body.Instructions[ti];
      var get_SimGameUID = game.MainModule.ImportReference(BaseComponentRef.Properties.First(x => x.Name == "SimGameUID").GetMethod);
      var set_SimGameUID = game.MainModule.ImportReference(BaseComponentRef.Properties.First(x => x.Name == "SimGameUID").SetMethod);
      var instructions = new List<Instruction>() {
        body.Create(OpCodes.Ldarg_0),
        body.Create(OpCodes.Call, get_SimGameUID),
        body.Create(OpCodes.Ldstr, "_V2"),
        body.Create(OpCodes.Callvirt, game.MainModule.ImportReference(typeof(string).GetMethod("EndsWith",new Type[] { typeof(string) }))),
        body.Create(OpCodes.Brfalse_S, ret),
        body.Create(OpCodes.Ldarg_0),
        body.Create(OpCodes.Ldarg_0),
        body.Create(OpCodes.Call, get_SimGameUID),
        body.Create(OpCodes.Ldc_I4_0),
        body.Create(OpCodes.Ldarg_0),
        body.Create(OpCodes.Call, get_SimGameUID),
        body.Create(OpCodes.Callvirt, game.MainModule.ImportReference(typeof(string).GetProperty("Length").GetGetMethod())),
        body.Create(OpCodes.Ldstr, "_V2"),
        body.Create(OpCodes.Callvirt, game.MainModule.ImportReference(typeof(string).GetProperty("Length").GetGetMethod())),
        body.Create(OpCodes.Sub),
        body.Create(OpCodes.Callvirt, game.MainModule.ImportReference(typeof(string).GetMethod("Substring",new Type[] { typeof(Int32), typeof(Int32) }))),
        body.Create(OpCodes.Call, set_SimGameUID)
      };
      foreach (var field in fields) {
        instructions.AddRange(new List<Instruction>() {
          body.Create(OpCodes.Ldarg_0),
          body.Create(OpCodes.Ldarg_1),
          body.Create(OpCodes.Callvirt, HBS_Util_SerializationStream_GetString),
          body.Create(OpCodes.Stfld, field)
        });
      }
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(loadMethod.Body.Instructions[ti-1], instruction); }


      Log.M?.TWL(0, $"InjectLoad success");
      for (var i = 0; i < loadMethod.Body.Instructions.Count; i++) {
        var instruction = loadMethod.Body.Instructions[i];
        Log.M?.WL(1, instruction.OpCode + ":" + (instruction.Operand == null ? "null" : instruction.Operand.ToString()));
      }
      Log.M?.WL(0, $"method end", true);
    }
    public static void InjectConstructor(TypeDefinition BaseComponentRef, params FieldDefinition[] fields) {
      MethodDefinition trgMethod = BaseComponentRef.Methods.First(x => { return (x.Name == ".ctor") && (x.Parameters.Count == 2); });
      if (trgMethod == null) {
        Log.Err?.WL(1, "can't find BattleTech.BaseComponentRef..ctor method", true);
        return;
      }
      var get_SimGameUID = game.MainModule.ImportReference(BaseComponentRef.Properties.First(x => x.Name == "SimGameUID").GetMethod);
      ILProcessor body = trgMethod.Body.GetILProcessor();
      int ti = -1;
      for (var i = 0; i < trgMethod.Body.Instructions.Count; i++) {
        var instruction = trgMethod.Body.Instructions[i];
        if (instruction.OpCode == OpCodes.Call) { ti = i; break; }
      }
      if (ti == -1) {
        Log.Err?.WL(1, "can't find OpCodes.Ldarg_0 opcode", true);
        return;
      }
      var instructions = new List<Instruction>();
      foreach (var field in fields) {
        instructions.AddRange(new List<Instruction>(){
          body.Create(OpCodes.Ldarg_0),
          body.Create(OpCodes.Ldarg_1),
          body.Create(OpCodes.Ldfld, field),
          body.Create(OpCodes.Stfld, field)
        });
      }
      instructions.Reverse();
      foreach (var instruction in instructions) { body.InsertAfter(trgMethod.Body.Instructions[ti], instruction); }
      Log.M?.TWL(0, $"InjectSave success");
      for (var i = 0; i < trgMethod.Body.Instructions.Count; i++) {
        var instruction = trgMethod.Body.Instructions[i];
        Log.M?.WL(1, instruction.OpCode + ":" + (instruction.Operand == null ? "null" : instruction.Operand.ToString()));
      }
      Log.M?.WL(0, $"method end", true);
    }
    internal static AssemblyDefinition game { get; set; } = null;
    public static void Inject(IAssemblyResolver resolver) {
      Log.BaseDirectory = AssemblyDirectory;
      Log.InitLog();
      Log.Err?.TWL(0, $"ComponentDefInjector initing {Assembly.GetExecutingAssembly().GetName().Version}", true);
      try {
        game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
        if (game == null) {
          Log.Err?.WL(1, "can't resolve main game assembly", true);
          return;
        }
        HBS_Util_Serialization_StorageSpaceString = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.Serialization").Methods.First(x => x.Name == "StorageSpaceString"));
        HBS_Util_SerializationStream_PutString = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.SerializationStream").Methods.First(x => x.Name == "PutString"));
        HBS_Util_SerializationStream_GetString = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.SerializationStream").Methods.First(x => x.Name == "GetString"));
        HBS_Util_SerializationStream_PutBool = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.SerializationStream").Methods.First(x => x.Name == "PutBool"));
        HBS_Util_SerializationStream_GetBool = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.SerializationStream").Methods.First(x => x.Name == "GetBool"));
        HBS_Util_Serialization_STORAGE_SPACE_BOOL = game.MainModule.ImportReference(game.MainModule.GetType("HBS.Util.Serialization").Fields.First(x => x.Name == "STORAGE_SPACE_BOOL"));

        TypeDefinition BaseComponentRef = game.MainModule.GetType("BattleTech.BaseComponentRef");
        if (BaseComponentRef == null) {
          Log.Err?.WL(1, "can't resolve BattleTech.BaseComponentRef type", true);
          return;
        }
        Log.M?.WL(1, "fields before:");
        foreach (var field in BaseComponentRef.Fields) {
          Log.M?.WL(2, $"{field.Name}");
        }
        FieldDefinition prefabNameFieldDef = BaseComponentRef.Fields.First(x => x.Name == "prefabName");
        if (prefabNameFieldDef == null) {
          Log.Err?.WL(1, "can't find BattleTech.BaseComponentRef.SimGameUID field", true);
          return;
        }
        List<CustomAttribute> statName_attrs = prefabNameFieldDef.HasCustomAttributes ? prefabNameFieldDef.CustomAttributes.ToList() : new List<CustomAttribute>();

        FieldDefinition LocalGUIDFieldDef = new FieldDefinition("LocalGUID", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(string)));
        FieldDefinition TargetComponentGUIDFieldDef = new FieldDefinition("TargetComponentGUID", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(string)));
        //FieldDefinition ApplyFirstOnlyFieldDef = new FieldDefinition("ApplyFirstOnly", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(typeof(bool)));
        Log.M?.WL(1, $"BattleTech.BaseComponentRef.prefabName custom attributes {statName_attrs.Count}:");
        foreach (var attr in statName_attrs) {
          LocalGUIDFieldDef.CustomAttributes.Add(attr);
          TargetComponentGUIDFieldDef.CustomAttributes.Add(attr);
          Log.M?.WL(2, $"{attr.AttributeType.Name}");
        }
        BaseComponentRef.Fields.Add(LocalGUIDFieldDef);
        BaseComponentRef.Fields.Add(TargetComponentGUIDFieldDef);
        Log.M?.WL(1, "fields after:");
        foreach (var field in BaseComponentRef.Fields) {
          Log.M?.WL(2, $"{field.Name}");
        }
        Log.M?.WL(1, "field added successfully", true);

        InjectSize(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
        InjectSave(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
        InjectLoad(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);
        InjectConstructor(BaseComponentRef, LocalGUIDFieldDef, TargetComponentGUIDFieldDef);

        FieldDefinition componentRef = new FieldDefinition("componentRef", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechComponentRef")));
        FieldDefinition mechDef = new FieldDefinition("mechDef", Mono.Cecil.FieldAttributes.Public, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechDef")));
        var LanceMechEquipmentListItem = game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem");
        LanceMechEquipmentListItem.Fields.Add(componentRef);
        LanceMechEquipmentListItem.Fields.Add(mechDef);
        MethodDefinition SetComponentRef = new MethodDefinition("SetComponentRef",Mono.Cecil.MethodAttributes.Public, game.MainModule.TypeSystem.Void);
        SetComponentRef.Parameters.Add(new ParameterDefinition("componentRef", Mono.Cecil.ParameterAttributes.None, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechComponentRef"))));
        SetComponentRef.Parameters.Add(new ParameterDefinition("mechDef", Mono.Cecil.ParameterAttributes.None, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.MechDef"))));
        LanceMechEquipmentListItem.Methods.Add(SetComponentRef);
        var body = SetComponentRef.Body.GetILProcessor();
        body.Emit(OpCodes.Ldarg_0);
        body.Emit(OpCodes.Ldarg_1);
        body.Emit(OpCodes.Stfld, componentRef);
        body.Emit(OpCodes.Ldarg_0);
        body.Emit(OpCodes.Ldarg_2);
        body.Emit(OpCodes.Stfld, mechDef);
        body.Emit(OpCodes.Ret);
        var SetLoadout = game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentList").Methods.First(x => { return (x.Name == "SetLoadout") && (x.Parameters.Count == 4);  });
        int ti = -1;
        var SetData = game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem").Methods.First(x => { return (x.Name == "SetData") && (x.Parameters.Count == 4); }));
        for (int i=0;i < SetLoadout.Body.Instructions.Count; ++i) {
          var instruction = SetLoadout.Body.Instructions[i];
          if (instruction.OpCode == OpCodes.Callvirt && instruction.Operand == SetData) { ti = i; break; }
        }
        if(ti != -1) {
          body = SetLoadout.Body.GetILProcessor();
          List<Instruction> instructions = new List<Instruction>() {
            body.Create(OpCodes.Dup),
            body.Create(OpCodes.Ldloc_S,SetLoadout.Body.Variables[6]),
            body.Create(OpCodes.Ldarg_0),
            body.Create(OpCodes.Ldfld, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentList").Fields.First(x=>x.Name=="activeMech"))),
            body.Create(OpCodes.Call, game.MainModule.ImportReference(game.MainModule.GetType("BattleTech.UI.LanceMechEquipmentListItem").Methods.First(x=>x.Name=="SetComponentRef"))),
          };
          instructions.Reverse();
          foreach(var instruction in instructions)
            body.InsertAfter(SetLoadout.Body.Instructions[ti], instruction);
        }
      } catch (Exception e) {
        Log.Err?.TWL(0, e.ToString(), true);
      }
    }
  }
}
