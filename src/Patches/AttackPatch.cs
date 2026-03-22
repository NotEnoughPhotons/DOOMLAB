using System;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;

namespace NEP.DOOMLAB.Patches
{
    [HarmonyPatch(typeof(ImpactProperties), nameof(ImpactProperties.ReceiveAttack))]
    internal static class AttackPatch
    {
        public static Action<Attack> OnAttackReceived;
        public static unsafe void Postfix(ImpactProperties __instance, Attack attack)
        {
            OnAttackReceived?.Invoke(attack);
        }
    }
}