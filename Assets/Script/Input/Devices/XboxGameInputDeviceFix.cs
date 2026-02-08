using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace YARG.Input.Devices
{
    /// <summary>
    /// Fixes PlasticBand Xbox GameInput device registration on UWP builds.
    /// PlasticBand's GameInputLayoutFinder.RegisterLayout uses [Conditional("UNITY_STANDALONE_WIN")]
    /// which compiles out all Xbox device registrations on UWP (UNITY_WSA).
    /// This script manually registers the Xbox GameInput device layouts using reflection.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal static class XboxGameInputDeviceFix
    {
#if UNITY_EDITOR
        static XboxGameInputDeviceFix()
        {
            Register();
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        internal static void Register()
        {
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN || UNITY_WSA
            try
            {
                // Find the PlasticBand assembly
                Assembly plasticBandAsm = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "PlasticBand")
                    {
                        plasticBandAsm = asm;
                        break;
                    }
                }

                if (plasticBandAsm == null)
                {
                    Debug.LogWarning("[XboxGameInputFix] PlasticBand assembly not found");
                    return;
                }

                // Find InputSystem.RegisterLayout<T>(string, InputDeviceMatcher?) generic method
                MethodInfo registerLayoutGeneric = null;
                foreach (var method in typeof(InputSystem).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.Name == "RegisterLayout" && method.IsGenericMethod)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length == 2 &&
                            parameters[0].ParameterType == typeof(string) &&
                            parameters[1].ParameterType == typeof(InputDeviceMatcher?))
                        {
                            registerLayoutGeneric = method;
                            break;
                        }
                    }
                }

                if (registerLayoutGeneric == null)
                {
                    Debug.LogWarning("[XboxGameInputFix] InputSystem.RegisterLayout<T> method not found");
                    return;
                }

                // Xbox One Riffmaster Guitar (PDP)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneRiffmasterGuitar", 0x0E6F, 0x0248);

                // Xbox One Rock Band Guitar (Mad Catz)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneRockBandGuitar", 0x0738, 0x4161);

                // Xbox One Rock Band Guitar (PDP)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneRockBandGuitar", 0x0E6F, 0x0170);

                // Xbox One Four Lane Drumkit (Mad Catz)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneFourLaneDrumkit", 0x0738, 0x4262);

                // Xbox One Four Lane Drumkit (PDP)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneFourLaneDrumkit", 0x0E6F, 0x0171);

                // Xbox One Six Fret Guitar (Activision)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneSixFretGuitar", 0x1430, 0x079B);

                // Xbox One Wired Legacy Adapter (PDP)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneWiredLegacyAdapter", 0x0E6F, 0x0175);

                // Xbox One Wireless Legacy Adapter (Mad Catz)
                RegisterGameInputDevice(plasticBandAsm, registerLayoutGeneric,
                    "PlasticBand.Devices.XboxOneWirelessLegacyAdapter", 0x0738, 0x4164);

                Debug.Log("[XboxGameInputFix] Xbox GameInput device layouts registered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XboxGameInputFix] Failed to register Xbox GameInput devices: {ex}");
            }
#endif
        }

        private static void RegisterGameInputDevice(Assembly asm, MethodInfo registerLayoutGeneric,
            string typeName, int vendorId, int productId)
        {
            var deviceType = asm.GetType(typeName);
            if (deviceType == null)
            {
                Debug.LogWarning($"[XboxGameInputFix] Type {typeName} not found in PlasticBand assembly");
                return;
            }

            var matcher = new InputDeviceMatcher()
                .WithInterface("GameInput")
                .WithCapability("vendorId", vendorId)
                .WithCapability("productId", productId);

            try
            {
                var specificMethod = registerLayoutGeneric.MakeGenericMethod(deviceType);
                specificMethod.Invoke(null, new object[] { null, (InputDeviceMatcher?)matcher });
                Debug.Log($"[XboxGameInputFix] Registered {typeName} (VID:0x{vendorId:X4} PID:0x{productId:X4})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[XboxGameInputFix] Failed to register {typeName}: {ex.Message}");
            }
        }
    }
}
