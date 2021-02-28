using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Reflection;
using UnityEngine.XR;
using Photon.Pun;
using Photon;
using UnityEngine.UI;
using System.IO;

namespace WristMenu
{
    [BepInPlugin("org.jeydevv.monkeytag.wristmenu", "Monke Wrist Menu!", "1.0.0.0")]
    public class MyMenuPatcher : BaseUnityPlugin
    {
        public void Awake()
        {
            var harmony = new Harmony("com.jeydevv.monkeytag.wristmenu");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GorillaLocomotion.Player))]
    [HarmonyPatch("Update", MethodType.Normal)]
    class MenuPatch
    {
        static string[] buttons = new string[] {"Toggle Super Monke V2", "Toggle Tag Gun", "Toggle Speed Boost", "Tag All"};
        static bool[] buttonsActive = new bool[] {false, false, false, false};
        static bool gripDown;
        static GameObject menu = null;
        static GameObject canvasObj = null;
        static GameObject referance = null;
        public static int framePressCooldown = 0;
        static GameObject pointer = null;
        static bool gravityToggled = false;
        static bool flying = false;
        static int btnCooldown = 0;
        static void Prefix(GorillaLocomotion.Player __instance)
        {

            List<InputDevice> list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Left | UnityEngine.XR.InputDeviceCharacteristics.Controller, list);
            list[0].TryGetFeatureValue(CommonUsages.gripButton, out gripDown);

            if (gripDown && menu == null)
            {
                Draw();
                if (referance == null)
                {
                    referance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject.Destroy(referance.GetComponent<MeshRenderer>());
                    referance.transform.parent = __instance.rightHandTransform;
                    referance.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                    referance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
            }
            else if (!gripDown && menu != null)
            {
                GameObject.Destroy(menu);
                menu = null;
                GameObject.Destroy(referance);
                referance = null;
            }

            if (gripDown && menu != null)
            {
                menu.transform.position = __instance.leftHandTransform.position;
                menu.transform.rotation = __instance.leftHandTransform.rotation;
            }

            if (buttonsActive[0])
            {
                bool primaryDown = false;
                bool secondaryDown = false;
                list = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller, list);
                list[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryDown);
                list[0].TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryDown);

                if (primaryDown)
                {
                    __instance.transform.position += (__instance.headCollider.transform.forward * Time.deltaTime) * 12f;
                    __instance.GetComponent<Rigidbody>().velocity = Vector3.zero;
                    if (!flying)
                    {
                        flying = true;
                    }
                }
                else if (flying)
                {
                    __instance.GetComponent<Rigidbody>().velocity = (__instance.headCollider.transform.forward * Time.deltaTime) * 12f;
                    flying = false;
                }

                if (secondaryDown)
                {
                    if (!gravityToggled && __instance.bodyCollider.attachedRigidbody.useGravity == true)
                    {
                        __instance.bodyCollider.attachedRigidbody.useGravity = false;
                        gravityToggled = true;
                    }
                    else if (!gravityToggled && __instance.bodyCollider.attachedRigidbody.useGravity == false)
                    {
                        __instance.bodyCollider.attachedRigidbody.useGravity = true;
                        gravityToggled = true;
                    }
                }
                else
                {
                    gravityToggled = false;
                }
            }

            if (buttonsActive[1])
            {
                bool flag = false;
                bool flag2 = false;
                list = new List<InputDevice>();
                InputDevices.GetDevices(list);
                InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller, list);
                list[0].TryGetFeatureValue(CommonUsages.triggerButton, out flag);
                list[0].TryGetFeatureValue(CommonUsages.gripButton, out flag2);
                if (flag2)
                {
                    RaycastHit hitInfo;
                    Physics.Raycast(__instance.rightHandTransform.position - __instance.rightHandTransform.up, -__instance.rightHandTransform.up, out hitInfo);
                    if (pointer == null)
                    {
                        pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        GameObject.Destroy(pointer.GetComponent<Rigidbody>());
                        GameObject.Destroy(pointer.GetComponent<SphereCollider>());
                        pointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    }
                    pointer.transform.position = hitInfo.point;

                    Photon.Realtime.Player player;
                    bool taggable = GorillaTagger.Instance.TryToTag(hitInfo, true, out player);
                    if (flag && !taggable)
                    {
                        pointer.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                    }
                    else if (!flag && taggable)
                    {
                        pointer.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
                    }
                    else if (flag && taggable)
                    {
                        pointer.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
                        foreach (var ply in PhotonNetwork.PlayerList)
                        {
                            PhotonView.Get(GorillaTagManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, new object[]
                            {
                        ply,
                        player
                            });
                        }
                    }
                }
                else
                {
                    GameObject.Destroy(pointer);
                    pointer = null;
                }
            }

            if (buttonsActive[2])
            {
                __instance.maxJumpSpeed = 999f;
                __instance.jumpMultiplier = 1.45f;
            }
            else
            {
                __instance.maxJumpSpeed = 999f;
                __instance.jumpMultiplier = 1.1f;
            }

            if (buttonsActive[3] == true)
            {
                if (btnCooldown == 0)
                {
                    btnCooldown = Time.frameCount + 30;
                    foreach (var ply1 in PhotonNetwork.PlayerList)
                    {
                        foreach (var ply2 in PhotonNetwork.PlayerList)
                        {
                            PhotonView.Get(GorillaTagManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, new object[]
                            {
                            ply1,
                            ply2
                            });
                        }
                    }
                    GameObject.Destroy(menu);
                    menu = null;
                    Draw();
                }
            }

            if (btnCooldown > 0)
            {
                if (Time.frameCount > btnCooldown)
                {
                    btnCooldown = 0;
                    buttonsActive[3] = false;
                    GameObject.Destroy(menu);
                    menu = null;
                    Draw();
                }
            }
        }

        static void AddButton(float offset, string text)
        {
            GameObject newBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(newBtn.GetComponent<Rigidbody>());
            newBtn.GetComponent<BoxCollider>().isTrigger = true;
            newBtn.transform.parent = menu.transform;
            newBtn.transform.rotation = Quaternion.identity;
            newBtn.transform.localScale = new Vector3(0.09f, 0.8f, 0.08f);
            newBtn.transform.localPosition = new Vector3(0.56f, 0f, 0.28f - offset);
            newBtn.AddComponent<BtnCollider>().relatedText = text;

            int index = -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (text == buttons[i])
                {
                    index = i;
                    break;
                }
            }

            if (!buttonsActive[index])
            {
                newBtn.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            }
            else
            {
                newBtn.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
            }

            GameObject titleObj = new GameObject();
            titleObj.transform.parent = canvasObj.transform;
            Text title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            title.text = text;
            title.fontSize = 1;
            title.alignment = TextAnchor.MiddleCenter;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 0;
            RectTransform titleTransform = title.GetComponent<RectTransform>();
            titleTransform.localPosition = Vector3.zero;
            titleTransform.sizeDelta = new Vector2(0.2f, 0.05f);
            titleTransform.localPosition = new Vector3(0.064f, 0f, 0.111f - (offset / 2.55f));
            titleTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
        }

        public static void Draw()
        {
            menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(menu.GetComponent<Rigidbody>());
            GameObject.Destroy(menu.GetComponent<BoxCollider>());
            GameObject.Destroy(menu.GetComponent<Renderer>());
            menu.transform.localScale = new Vector3(0.1f, 0.3f, 0.4f);

            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(background.GetComponent<Rigidbody>());
            GameObject.Destroy(background.GetComponent<BoxCollider>());
            background.transform.parent = menu.transform;
            background.transform.rotation = Quaternion.identity;
            background.transform.localScale = new Vector3(0.1f, 1f, 1f);
            background.GetComponent<Renderer>().material.SetColor("_Color", Color.black);
            background.transform.position = new Vector3(0.05f, 0f, 0f);

            canvasObj = new GameObject();
            canvasObj.transform.parent = menu.transform;
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            CanvasScaler canvasScale = canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScale.dynamicPixelsPerUnit = 1000;

            GameObject titleObj = new GameObject();
            titleObj.transform.parent = canvasObj.transform;
            Text title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            title.text = "Monke Mod Menu";
            title.fontSize = 1;
            title.alignment = TextAnchor.MiddleCenter;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 0;
            RectTransform titleTransform = title.GetComponent<RectTransform>();
            titleTransform.localPosition = Vector3.zero;
            titleTransform.sizeDelta = new Vector2(0.28f, 0.05f);
            titleTransform.position = new Vector3(0.06f, 0f, 0.175f);
            titleTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            for (int i = 0; i < buttons.Length; i++)
            {
                AddButton(i * 0.15f, buttons[i]);
            }
        }

        public static void Toggle(string relatedText)
        {
            int index = -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (relatedText == buttons[i])
                {
                    index = i;
                    break;
                }
            }

            if (index != buttons.Length - 1)
            {
                if (buttonsActive[index])
                {
                    buttonsActive[index] = false;
                }
                else
                {
                    for (int i = 0; i < buttons.Length - 1; i++)
                    {
                        buttonsActive[i] = false;
                    }
                    buttonsActive[index] = true;
                }
            }
            else
            {
                buttonsActive[buttons.Length - 1] = true;
            }

            GameObject.Destroy(menu);
            menu = null;
            Draw();
        }
    }
    
    class BtnCollider : MonoBehaviour
    {
        public string relatedText;

        private void OnTriggerEnter(Collider collider)
        {
            if (Time.frameCount >= MenuPatch.framePressCooldown + 30)
            {
                MenuPatch.Toggle(relatedText);
                MenuPatch.framePressCooldown = Time.frameCount;
            }
        }
    }
}