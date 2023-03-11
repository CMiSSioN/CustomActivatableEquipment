using HarmonyLib;
using HBS.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using IRBTModUtils;
using Newtonsoft.Json.Linq;

namespace CustomActivatableEquipment {
  //[HarmonyPatch(typeof(JSONSerializationUtility))]
  //[HarmonyPatch("RehydrateObjectFromDictionary")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(object), typeof(Dictionary<string, object>), typeof(string), typeof(HBS.Stopwatch), typeof(HBS.Stopwatch), typeof(JSONSerializationUtility.RehydrationFilteringMode), typeof(Func<string, bool>[]) })]
  //public static class JSONSerializationUtility_RehydrateObjectFromDictionary {
  //  public static readonly string DEBUG_OUTOPUT_FLAG = "JSONSerializationUtility.DEBUG_OUTPUT";
  //  public static bool Prefix(object target, Dictionary<string, object> values, string classStructure, HBS.Stopwatch convertTime, HBS.Stopwatch reflectTime, JSONSerializationUtility.RehydrationFilteringMode filteringMode, Func<string, bool>[] predicates) {
  //    try {
  //      if (target is JObject jtarget) {
  //        Log.Debug?.WL(0, $"JSONSerializationUtility.RehydrateObjectFromDictionary target:{target.GetType().ToString()} classStructure:{classStructure}");
  //        foreach (var value in values) {
  //          Log.Debug?.WL(1, $"{value.Key}");
  //          JObject subtarget = null;
  //          Dictionary<string, object> subvalues = value.Value as Dictionary<string, object>;
  //          if (subvalues != null) {
  //            Log.Debug?.WL(2, $"dictionary");
  //            subtarget = new JObject();
  //            JSONSerializationUtility.RehydrateObjectFromDictionary(subtarget, subvalues, filteringMode, predicates);
  //            jtarget.Add(value.Key, subtarget);
  //          } else {
  //            Log.Debug?.WL(2, $"not a dictionary {value.Value}");
  //            jtarget.Add(value.Key, JValue.FromObject(value.Value));
  //          }
  //        }
  //        return false;
  //      }
  //      //if (Thread.CurrentThread.isFlagSet(DEBUG_OUTOPUT_FLAG) == false) { return; }
  //      //
  //    } catch (Exception e) {
  //      Log.Error?.TWL(0, e.ToString(), true);
  //    }
  //    return true;
  //  }
  //}
}