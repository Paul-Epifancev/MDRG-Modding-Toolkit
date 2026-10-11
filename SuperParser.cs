using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SuperParser.MainModClass), "SuperParser", "6.5.1", "Paul.Epifancev #warlockforbugs (Discord: anon.jun)")]
[assembly: MelonGame(null, null)]

namespace SuperParser
{
    public class ManagerData
    {
        public string goName;
        public string path;
        public string revealedId;
    }

    public class MainModClass : MelonMod
    {
        private string rootFolder;
        private string f1Png;
        private string f2Nodes;
        private string f3Live2D;
        private string f4Managers;
        private string f5Views;
        private string f6Merged;
        private string f7SpriteInfo;
        private string f8Win32;
        private string f9Architecture;
        private string f10Popup;
        private string f11Inventory;
        private string f12GameState;
        private string f13ItemManager;
        private string f14MissingItems;
        private string f15Cuddle;
        private bool isProcessing;
        private List<ManagerData> mgrCache = new List<ManagerData>();
        private IntPtr gameAssemblyBase = IntPtr.Zero;
        private Il2CppSystem.Type _gameVariablesType = null;
        private Il2CppSystem.Type _itemType = null;
        private Il2CppSystem.Type _itemManagerType = null;
        private Il2CppSystem.Type _gameItemContainerType = null;
        private Il2CppSystem.Reflection.FieldInfo _prefabField = null;
        private Il2CppSystem.Reflection.PropertyInfo _gameIdProperty = null;
        private Il2CppSystem.Reflection.MethodInfo _createInstanceMethod = null;
        private Il2CppSystem.Reflection.MethodInfo _removeItemMethod = null;
        private Il2CppSystem.Reflection.MethodInfo _getAllItemsMethod = null;

        private static readonly string[] VerifiedRoomNodeNames = new string[]
        {
            "back", "Bed", "couch", "Cupboard", "PC/PC desk", "PC", "PC/Screen light", "PC/PC rgb",
            "TV", "TV/TV light", "Window", "Light", "Morning Light", "Light Multiply",
            "Bathroom Door/Bathroom Door Light", "Door", "Bathroom Door", "GhostBackground",
            "BatBanner", "SpiderPlush", "mat"
        };

        private static readonly string[] VerifiedRoomNodePaths = new string[]
        {
            "/Room/back", "/Room/Bed", "/Room/couch", "/Room/Cupboard", "/Room/PC/PC desk",
            "/Room/PC/PC", "/Room/PC/Screen light", "/Room/PC/PC rgb", "/Room/TV/TV",
            "/Room/TV/TV light", "/Room/Window", "/Room/Light", "/Room/Morning Light",
            "/Room/Light Multiply", "/Room/Bathroom Door/Bathroom Door Light", "/Room/Door",
            "/Room/Bathroom Door/Door", "/Room/GhostBackground", "/Room/BatBanner",
            "/Room/SpiderPlush", "/Room/mat"
        };

        private static readonly Dictionary<string, string> VerifiedSceneToManagerId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "/Views/Room Scene", "Room" },
            { "/Views/Changing Room Scene", "" },
            { "/Views/Shop Scene", "Shop" },
            { "/Views/Shower Scene", "shower" },
            { "/Views/Missionary Scene", "room_missionary" },
            { "/Views/Clothier Scene", "Clothier" }
        };

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public override void OnInitializeMelon()
        {
            try
            {
                gameAssemblyBase = GetModuleHandle("GameAssembly.dll");
                string baseMods = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
                rootFolder = Path.Combine(baseMods, "SuperParser_Report");
                f1Png = Path.Combine(rootFolder, "1_PNG_VRAM_Dump");
                f2Nodes = Path.Combine(rootFolder, "2_Scene_Nodes");
                f3Live2D = Path.Combine(rootFolder, "3_Live2D_Full");
                f4Managers = Path.Combine(rootFolder, "4_Managers");
                f5Views = Path.Combine(rootFolder, "5_Views_Scenes");
                f6Merged = Path.Combine(rootFolder, "6_Merged_Interlock");
                f7SpriteInfo = Path.Combine(rootFolder, "7_Sprite_Info");
                f8Win32 = Path.Combine(rootFolder, "8_Win32_Interceptor");
                f9Architecture = Path.Combine(rootFolder, "9_Architecture_Map");
                f10Popup = Path.Combine(rootFolder, "10_Popup_ClonePopup");
                f11Inventory = Path.Combine(rootFolder, "11_Inventory");
                f12GameState = Path.Combine(rootFolder, "12_Game_State");
                f13ItemManager = Path.Combine(rootFolder, "13_ItemManager_Debug");
                f14MissingItems = Path.Combine(rootFolder, "14_Missing_Items_Deep_Dive");
                f15Cuddle = Path.Combine(rootFolder, "15_Cuddle_Scene");

                string[] dirs = { f1Png, f2Nodes, f3Live2D, f4Managers, f5Views, f6Merged, f7SpriteInfo, f8Win32, f9Architecture, f10Popup, f11Inventory, f12GameState, f13ItemManager, f14MissingItems, f15Cuddle };
                foreach (string d in dirs)
                {
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }
                InitializeIl2CppInventoryTypes();
            }
            catch (Exception e)
            {
                MelonLogger.Error("Init failed: " + e.Message);
            }
            MelonLogger.Msg("[SuperParser v6.5.1] Ready. 15 Folders configured. Press F5 to dump.");
        }

        private void InitializeIl2CppInventoryTypes()
        {
            try
            {
                var assemblies = Il2CppSystem.AppDomain.CurrentDomain.GetAssemblies();
                MelonLogger.Msg($"[Init] Found {assemblies.Length} IL2CPP assemblies");
                foreach (var asm in assemblies)
                {
                    try
                    {
                        if (_gameVariablesType == null)
                        {
                            _gameVariablesType = asm.GetType("GameVariables");
                            if (_gameVariablesType != null) MelonLogger.Msg($"[Init] Found GameVariables: {_gameVariablesType.FullName}");
                        }
                        if (_itemType == null)
                        {
                            _itemType = asm.GetType("Item");
                            if (_itemType != null) MelonLogger.Msg($"[Init] Found Item: {_itemType.FullName}");
                        }
                        if (_gameItemContainerType == null)
                        {
                            _gameItemContainerType = asm.GetType("GameItemContainer");
                            if (_gameItemContainerType != null) MelonLogger.Msg($"[Init] Found GameItemContainer: {_gameItemContainerType.FullName}");
                        }
                    }
                    catch { }
                }
                if (_itemType != null)
                {
                    try
                    {
                        var nested = _itemType.GetNestedTypes(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic);
                        foreach (var nt in nested)
                        {
                            if (nt.Name == "ItemManager")
                            {
                                _itemManagerType = nt;
                                MelonLogger.Msg($"[Init] Found ItemManager as nested type: {nt.FullName}");
                                break;
                            }
                        }
                    }
                    catch { }
                }
                if (_itemType != null)
                {
                    _prefabField = _itemType.GetField("_prefab", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                    _gameIdProperty = _itemType.GetProperty("GameId", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                }
                if (_itemManagerType != null)
                {
                    _createInstanceMethod = _itemManagerType.GetMethod("CreateInstance", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Static);
                    _getAllItemsMethod = _itemManagerType.GetMethod("AllItemsReadonlyCollection", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    var methods = _itemManagerType.GetMethods(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    foreach (var m in methods)
                    {
                        if (m.Name == "RemoveItem")
                        {
                            var pars = m.GetParameters();
                            if (pars.Length == 1) { _removeItemMethod = m; break; }
                        }
                    }
                }
                MelonLogger.Msg("[Init] IL2CPP Inventory Types initialized.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Init] Failed: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5) && !isProcessing)
            {
                isProcessing = true;
                MelonLogger.Msg("[F5] Harvesting data across all 15 vectors...");
                try
                {
                    mgrCache = ExtractManagers();
                    string scene = DetectScene();
                    MelonLogger.Msg("Active scene: " + scene);
                    DumpArchitectureMap();
                    DumpManagers();
                    DumpSceneNodes(scene);
                    DumpViewsAndScenes(scene);
                    DumpLive2DFull(scene);
                    DumpSpriteInfo(scene);
                    DumpMergedInterlock(scene);
                    DumpWin32Interceptor(scene);
                    DumpPopupClonePopup(scene);
                    DumpInventoryDeep(scene);
                    DumpGameState(scene);
                    DumpItemManagerDebug();
                    DumpMissingItemsDeepDive();
                    DumpPngVram();
                    DumpCuddleAndLuaMaster();
                    MelonLogger.Msg("[DONE] All 15 folders updated successfully.");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Pipeline crashed: " + ex.Message);
                }
                finally
                {
                    isProcessing = false;
                }
            }
        }

        private GameObject FindNodeByPath(string targetPath)
        {
            GameObject go = GameObject.Find(targetPath);
            if (go != null) return go;

            go = GameObject.Find("/Room Scene" + targetPath);
            if (go != null) return go;

            GameObject roomRoot = GameObject.Find("/Room Scene/Room") ?? GameObject.Find("/Room") ?? GameObject.Find("Room");
            if (roomRoot != null)
            {
                string relativePath = targetPath.StartsWith("/Room/", StringComparison.OrdinalIgnoreCase) ? targetPath.Substring(6) : targetPath;
                Transform tr = roomRoot.transform.Find(relativePath);
                if (tr != null) return tr.gameObject;
            }

            var allRenderers = UnityEngine.Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.gameObject == null) continue;
                string absPath = GetAbsPath(sr.gameObject);
                if (absPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return sr.gameObject;
                }
            }

            return null;
        }

        // =====================================================================
        // FOLDER 6: MERGED INTERLOCK — v6.5.1 (ИСПРАВЛЕН ДЛЯ НЕАКТИВНЫХ НОД)
        // =====================================================================
        private void DumpMergedInterlock(string scene)
        {
            int totalLinks = 0;
            int roomNodesFound = 0;
            int roomNodesSkipped = 0;
            List<string> skippedNodes = new List<string>();

            string idx = Path.Combine(f6Merged, "00_INDEX.txt");
            StringBuilder isb = new StringBuilder();
            isb.AppendLine("=========================================================================");
            isb.AppendLine("MERGED INTERLOCK INDEX (FOLDER 6) — v6.5.1");
            isb.AppendLine("=========================================================================");
            isb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            isb.AppendLine($"Active scene: {scene}");
            isb.AppendLine($"Verified Room Nodes count: {VerifiedRoomNodeNames.Length}");
            isb.AppendLine("=========================================================================\n");

            isb.AppendLine("[BLOCK 1] ROOM SCENE — 21 VERIFIED NODES");
            isb.AppendLine("-------------------------------------------------------------------------");

            GameObject roomParent = GameObject.Find("/Room Scene/Room")
                                 ?? GameObject.Find("/Room")
                                 ?? GameObject.Find("Room");

            if (roomParent != null)
            {
                for (int i = 0; i < VerifiedRoomNodeNames.Length; i++)
                {
                    string relativePath = VerifiedRoomNodeNames[i];
                    string fullPath = VerifiedRoomNodePaths[i];

                    Transform child = roomParent.transform.Find(relativePath);
                    if (child != null)
                    {
                        GameObject go = child.gameObject;
                        // ВАРИАНТ А: Обрабатываем даже неактивные ноды
                        ProcessInterlockNodeWithInactive(go, "Room", "Room Scene (Player Bedroom)", fullPath, ref totalLinks, isb);
                        roomNodesFound++;
                    }
                    else
                    {
                        GameObject go = GameObject.Find(fullPath);
                        if (go != null)
                        {
                            ProcessInterlockNodeWithInactive(go, "Room", "Room Scene (Player Bedroom)", fullPath, ref totalLinks, isb);
                            roomNodesFound++;
                        }
                        else
                        {
                            roomNodesSkipped++;
                            skippedNodes.Add($"{fullPath} (GameObject not found)");
                        }
                    }
                }
            }
            else
            {
                isb.AppendLine("  [ERROR] Room parent object not found in hierarchy!");
                for (int i = 0; i < VerifiedRoomNodeNames.Length; i++)
                {
                    roomNodesSkipped++;
                    skippedNodes.Add($"{VerifiedRoomNodePaths[i]} (Room parent not found)");
                }
            }

            isb.AppendLine($"\n  Room nodes found: {roomNodesFound}/{VerifiedRoomNodeNames.Length}");
            isb.AppendLine($"  Room nodes skipped: {roomNodesSkipped}\n");

            isb.AppendLine("[BLOCK 2] OTHER MANAGED SCENES");
            isb.AppendLine("-------------------------------------------------------------------------");
            foreach (var kvp in VerifiedSceneToManagerId)
            {
                if (kvp.Key == "/Views/Room Scene") continue;
                var sceneGo = GameObject.Find(kvp.Key);
                if (sceneGo != null && sceneGo.activeInHierarchy)
                {
                    string contextName = kvp.Key == "/Views/Changing Room Scene" ? "Changing Room (Lady Annalie Fitting Room)" : kvp.Key.Replace("/Views/", "");
                    var children = sceneGo.GetComponentsInChildren<Transform>();
                    int sceneCount = 0;
                    foreach (var child in children)
                    {
                        if (child.gameObject.activeInHierarchy)
                        {
                            if (ProcessInterlockNode(child.gameObject, kvp.Value, contextName, ref totalLinks, isb))
                            {
                                sceneCount++;
                            }
                        }
                    }
                    isb.AppendLine($"  {contextName}: {sceneCount} nodes processed\n");
                }
            }

            isb.AppendLine("[BLOCK 3] DIRECT INJECTION SCENES (No Manager)");
            isb.AppendLine("-------------------------------------------------------------------------");
            string[] directInjectionScenes = { "/Views/Cuddle Scene", "/Views/Scene 2", "/Views/Scene 1", "/Views/Scene 3", "/Views/Outside Scene", "/Views/PC Scene", "/Views/Developer Room Scene", "/Views/Menu Scene" };
            foreach (var diScene in directInjectionScenes)
            {
                var sceneGo = GameObject.Find(diScene);
                if (sceneGo != null && sceneGo.activeInHierarchy)
                {
                    isb.AppendLine($"  ▶ {diScene.Replace("/Views/", "")}");
                    var children = sceneGo.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        if (child.gameObject.activeInHierarchy && child.gameObject.GetComponent<SpriteRenderer>() != null)
                        {
                            ProcessDirectInjectionNode(child.gameObject, diScene.Replace("/Views/", ""), ref totalLinks, isb);
                        }
                    }
                }
            }

            isb.AppendLine("\n[BLOCK 4] DIAGNOSTICS — SKIPPED ROOM NODES");
            isb.AppendLine("-------------------------------------------------------------------------");
            if (skippedNodes.Count > 0)
            {
                foreach (var skipped in skippedNodes)
                {
                    isb.AppendLine($"  ⚠ {skipped}");
                }
            }
            else
            {
                isb.AppendLine("  ✓ All 21 room nodes processed successfully.");
            }

            isb.AppendLine($"\n=========================================================================");
            isb.AppendLine($"TOTAL VERIFIED LINKS GENERATED: {totalLinks}");
            isb.AppendLine($"=========================================================================");
            File.WriteAllText(idx, isb.ToString(), Encoding.UTF8);
            MelonLogger.Msg($"[OK] Folder 6: {totalLinks} interlock links. Room: {roomNodesFound}/{VerifiedRoomNodeNames.Length}.");
        }

        // НОВЫЙ МЕТОД: Обрабатывает даже неактивные ноды (ВАРИАНТ А)
        private void ProcessInterlockNodeWithInactive(GameObject go, string mgrId, string sceneContext, string expectedPath, ref int totalLinks, StringBuilder indexSb)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                var rawRenderer = go.GetComponentInChildren(Il2CppType.Of<SpriteRenderer>());
                if (rawRenderer != null) sr = rawRenderer.TryCast<SpriteRenderer>();
            }

            // Если есть активный SpriteRenderer со спрайтом
            if (sr != null && sr.sprite != null && sr.sprite.texture != null)
            {
                totalLinks++;
                string safe = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
                string fp = Path.Combine(f6Merged, $"Node_{sceneContext.Replace(" ", "_")}_{safe}_{totalLinks}.txt");
                StringBuilder msb = new StringBuilder();
                msb.AppendLine($"CONTEXT SCENE: {sceneContext}");
                msb.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
                msb.AppendLine($"Instance ID: {go.GetInstanceID()}");
                msb.AppendLine($"REVEALED MGR ID: \"{mgrId}\"");
                msb.AppendLine($"Node Name: \"{go.name}\"");
                msb.AppendLine($"ActiveInHierarchy: {go.activeInHierarchy}");
                msb.AppendLine($"Active Sprite: {sr.sprite.name}");
                msb.AppendLine($"Resolution: {sr.sprite.texture.width}x{sr.sprite.texture.height}");
                msb.AppendLine($"PPU: {sr.sprite.pixelsPerUnit}");
                msb.AppendLine($"Pivot: ({sr.sprite.pivot.x:F2}, {sr.sprite.pivot.y:F2})");
                msb.AppendLine($"Texture Source: {sr.sprite.texture.name}");
                msb.AppendLine("=========================================================================");
                msb.AppendLine("USE IN LUA SCRIPT:");
                msb.AppendLine($"  local mgr = TextureOverriderManager.GetTextureOverriderManager(\"{mgrId}\")");
                msb.AppendLine($"  mgr:SetSprite(CurrentModGuid, \"{GetAbsPath(go)}\", mySprite)");
                File.WriteAllText(fp, msb.ToString(), Encoding.UTF8);
                indexSb.AppendLine($"  ├─ Link {totalLinks}: {GetAbsPath(go)} (Mgr: {mgrId})");
                return;
            }

            // Если есть UI Image
            var uiImage = go.GetComponent<UnityEngine.UI.Image>();
            if (uiImage == null)
            {
                var uiRaw = go.GetComponentInChildren(Il2CppType.Of<UnityEngine.UI.Image>());
                if (uiRaw != null) uiImage = uiRaw.TryCast<UnityEngine.UI.Image>();
            }

            if (uiImage != null && uiImage.sprite != null && uiImage.sprite.texture != null)
            {
                totalLinks++;
                string safe = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
                string fp = Path.Combine(f6Merged, $"Node_UI_{sceneContext.Replace(" ", "_")}_{safe}_{totalLinks}.txt");
                StringBuilder msb = new StringBuilder();
                msb.AppendLine($"[UI ELEMENT — NOT MANAGED BY TextureOverriderManager]");
                msb.AppendLine($"CONTEXT SCENE: {sceneContext}");
                msb.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
                msb.AppendLine($"Instance ID: {go.GetInstanceID()}");
                msb.AppendLine($"Node Name: \"{go.name}\"");
                msb.AppendLine($"UI Image Sprite: {uiImage.sprite.name}");
                msb.AppendLine($"Resolution: {uiImage.sprite.texture.width}x{uiImage.sprite.texture.height}");
                msb.AppendLine($"NOTE: Requires direct C# DLL injection (not Lua API)");
                File.WriteAllText(fp, msb.ToString(), Encoding.UTF8);
                indexSb.AppendLine($"  ├─ UI Link {totalLinks}: {GetAbsPath(go)} (UI Image, needs C#)");
                return;
            }

            // ВАРИАНТ А: Нода найдена, но нет активного SpriteRenderer/Image
            totalLinks++;
            string safeInactive = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
            string fpInactive = Path.Combine(f6Merged, $"Node_INACTIVE_{sceneContext.Replace(" ", "_")}_{safeInactive}_{totalLinks}.txt");
            StringBuilder msbInactive = new StringBuilder();
            msbInactive.AppendLine($"[INACTIVE NODE — WAITING FOR STATE TRIGGER]");
            msbInactive.AppendLine($"CONTEXT SCENE: {sceneContext}");
            msbInactive.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
            msbInactive.AppendLine($"Instance ID: {go.GetInstanceID()}");
            msbInactive.AppendLine($"Node Name: \"{go.name}\"");
            msbInactive.AppendLine($"ActiveInHierarchy: {go.activeInHierarchy}");
            msbInactive.AppendLine($"Expected Path: {expectedPath}");
            msbInactive.AppendLine($"REVEALED MGR ID: \"{mgrId}\"");
            msbInactive.AppendLine($"=========================================================================");
            msbInactive.AppendLine($"STATUS: Node exists in hierarchy but has no active SpriteRenderer or UI Image.");
            msbInactive.AppendLine($"This is NORMAL behavior — the node will activate when triggered by game state.");
            msbInactive.AppendLine($"Example: Bathroom Door Light activates only when Jun is showering.");
            msbInactive.AppendLine($"=========================================================================");
            msbInactive.AppendLine("USE IN LUA SCRIPT (when node becomes active):");
            msbInactive.AppendLine($"  local mgr = TextureOverriderManager.GetTextureOverriderManager(\"{mgrId}\")");
            msbInactive.AppendLine($"  mgr:SetSprite(CurrentModGuid, \"{GetAbsPath(go)}\", mySprite)");
            File.WriteAllText(fpInactive, msbInactive.ToString(), Encoding.UTF8);
            indexSb.AppendLine($"  ├─ Link {totalLinks}: {GetAbsPath(go)} (Mgr: {mgrId}) [INACTIVE - waiting for state trigger]");
        }

        private bool ProcessInterlockNode(GameObject go, string mgrId, string sceneContext, ref int totalLinks, StringBuilder indexSb)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                var rawRenderer = go.GetComponentInChildren(Il2CppType.Of<SpriteRenderer>());
                if (rawRenderer != null) sr = rawRenderer.TryCast<SpriteRenderer>();
            }

            if (sr != null && sr.sprite != null && sr.sprite.texture != null)
            {
                totalLinks++;
                string safe = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
                string fp = Path.Combine(f6Merged, $"Node_{sceneContext.Replace(" ", "_")}_{safe}_{totalLinks}.txt");
                StringBuilder msb = new StringBuilder();
                msb.AppendLine($"CONTEXT SCENE: {sceneContext}");
                msb.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
                msb.AppendLine($"Instance ID: {go.GetInstanceID()}");
                msb.AppendLine($"REVEALED MGR ID: \"{mgrId}\"");
                msb.AppendLine($"Node Name: \"{go.name}\"");
                msb.AppendLine($"ActiveInHierarchy: {go.activeInHierarchy}");
                msb.AppendLine($"Active Sprite: {sr.sprite.name}");
                msb.AppendLine($"Resolution: {sr.sprite.texture.width}x{sr.sprite.texture.height}");
                msb.AppendLine($"PPU: {sr.sprite.pixelsPerUnit}");
                msb.AppendLine($"Pivot: ({sr.sprite.pivot.x:F2}, {sr.sprite.pivot.y:F2})");
                msb.AppendLine($"Texture Source: {sr.sprite.texture.name}");
                msb.AppendLine("=========================================================================");
                msb.AppendLine("USE IN LUA SCRIPT:");
                msb.AppendLine($"  local mgr = TextureOverriderManager.GetTextureOverriderManager(\"{mgrId}\")");
                msb.AppendLine($"  mgr:SetSprite(CurrentModGuid, \"{GetAbsPath(go)}\", mySprite)");
                File.WriteAllText(fp, msb.ToString(), Encoding.UTF8);
                indexSb.AppendLine($"  ├─ Link {totalLinks}: {GetAbsPath(go)} (Mgr: {mgrId})");
                return true;
            }

            var uiImage = go.GetComponent<UnityEngine.UI.Image>();
            if (uiImage == null)
            {
                var uiRaw = go.GetComponentInChildren(Il2CppType.Of<UnityEngine.UI.Image>());
                if (uiRaw != null) uiImage = uiRaw.TryCast<UnityEngine.UI.Image>();
            }

            if (uiImage != null && uiImage.sprite != null && uiImage.sprite.texture != null)
            {
                totalLinks++;
                string safe = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
                string fp = Path.Combine(f6Merged, $"Node_UI_{sceneContext.Replace(" ", "_")}_{safe}_{totalLinks}.txt");
                StringBuilder msb = new StringBuilder();
                msb.AppendLine($"[UI ELEMENT — NOT MANAGED BY TextureOverriderManager]");
                msb.AppendLine($"CONTEXT SCENE: {sceneContext}");
                msb.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
                msb.AppendLine($"Instance ID: {go.GetInstanceID()}");
                msb.AppendLine($"Node Name: \"{go.name}\"");
                msb.AppendLine($"UI Image Sprite: {uiImage.sprite.name}");
                msb.AppendLine($"Resolution: {uiImage.sprite.texture.width}x{uiImage.sprite.texture.height}");
                msb.AppendLine($"NOTE: Requires direct C# DLL injection (not Lua API)");
                File.WriteAllText(fp, msb.ToString(), Encoding.UTF8);
                indexSb.AppendLine($"  ├─ UI Link {totalLinks}: {GetAbsPath(go)} (UI Image, needs C#)");
                return true;
            }

            return false;
        }

        private void ProcessDirectInjectionNode(GameObject go, string sceneContext, ref int totalLinks, StringBuilder indexSb)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null && sr.sprite.texture != null)
            {
                totalLinks++;
                string safe = go.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
                string fp = Path.Combine(f6Merged, $"DIRECT_INJECT_{sceneContext}_{safe}_{totalLinks}.txt");
                StringBuilder msb = new StringBuilder();
                msb.AppendLine($"[DIRECT INJECTION ONLY] - NO MANAGER AVAILABLE");
                msb.AppendLine($"CONTEXT SCENE: {sceneContext}");
                msb.AppendLine($"Absolute Hierarchy Path: {GetAbsPath(go)}");
                msb.AppendLine($"Instance ID: {go.GetInstanceID()}");
                msb.AppendLine($"Node Name: \"{go.name}\"");
                msb.AppendLine($"Active Sprite: {sr.sprite.name}");
                msb.AppendLine($"Resolution: {sr.sprite.texture.width}x{sr.sprite.texture.height}");
                msb.AppendLine($"PPU: {sr.sprite.pixelsPerUnit}");
                msb.AppendLine($"Pivot: ({sr.sprite.pivot.x:F2}, {sr.sprite.pivot.y:F2})");
                msb.AppendLine($"Texture Source: {sr.sprite.texture.name}");
                File.WriteAllText(fp, msb.ToString(), Encoding.UTF8);
                indexSb.AppendLine($"  ├─ Direct Inject {totalLinks}: {GetAbsPath(go)}");
            }
        }

        // =====================================================================
        // FOLDER 3: LIVE2D FULL — v6.5.1 (ИСПРАВЛЕН ДЛЯ HIT AREAS И BUTTONLIST)
        // =====================================================================
        private void DumpLive2DFull(string activeSceneName)
        {
            string path = Path.Combine(f3Live2D, "Active_Scene_Live2D_Mechanics_Passport.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("LIVE2D CONTROLLER, BUTTONLIST & INTERACTION PROFILER (FOLDER 3)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"ACTIVE SCENE CONTEXT: {activeSceneName}");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");

            // ЧАСТЬ 1: ButtonList парсер (УЛУЧШЕННЫЙ ПОИСК)
            sb.AppendLine("[PART 1] DYNAMIC BUTTONLIST WRAPPERS");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                int btnCount = 0;
                for (int i = 0; i < allObjects.Length; i++)
                {
                    GameObject go = allObjects[i];
                    if (go == null) continue;
                    var components = go.GetComponents<Component>();
                    for (int c = 0; c < components.Length; c++)
                    {
                        var comp = components[c];
                        if (comp == null) continue;
                        string compName = comp.GetIl2CppType().Name;
                        // УЛУЧШЕНИЕ: Ищем больше вариантов имён компонентов
                        if (compName == "ButtonList" || compName == "DynamicButtonList" || compName.Contains("Button"))
                        {
                            try
                            {
                                var buttonsField = comp.GetType().GetField("_buttons", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (buttonsField != null)
                                {
                                    var buttonsList = buttonsField.GetValue(comp) as System.Collections.IEnumerable;
                                    if (buttonsList != null)
                                    {
                                        foreach (var wrapper in buttonsList)
                                        {
                                            if (wrapper == null) continue;
                                            string textResult = "[Dynamic Delegate Closed]";
                                            var textFuncProp = wrapper.GetType().GetProperty("TextFunc") ?? wrapper.GetType().GetProperty("<TextFunc>k__BackingField");
                                            if (textFuncProp != null)
                                            {
                                                var del = textFuncProp.GetValue(wrapper) as Delegate;
                                                if (del != null) textResult = del.DynamicInvoke() as string;
                                            }
                                            long trueAddressPointer = go.Pointer.ToInt64();
                                            btnCount++;
                                            sb.AppendLine($"[🚨 DYNAMIC_BUTTONLIST_WRAPPER_DETECTED] #{btnCount}");
                                            sb.AppendLine($"  ├─ Simulated Button Node: \"{go.name}\"");
                                            sb.AppendLine($"  ├─ Extracted TextFunc() : \"{textResult}\"");
                                            sb.AppendLine($"  └─ Action Pointer ID    : 0x{trueAddressPointer:X16}\n");
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                sb.AppendLine($"Total ButtonList wrappers detected: {btnCount}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            // ЧАСТЬ 2: ВСЕ CubismParameter — БЕЗ фильтров по именам
            sb.AppendLine("[PART 2] ALL CUBISM PARAMETERS (NO NAME FILTERS)");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
                int paramCount = 0;
                int activeParamCount = 0;
                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    var paramComp = go.GetComponent("CubismParameter");
                    if (paramComp != null)
                    {
                        paramCount++;
                        float liveVal = 0f, minVal = 0f, maxVal = 0f, defVal = 0f;
                        try
                        {
                            var type = paramComp.GetType();
                            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                            var fVal = type.GetField("Value", flags) ?? type.GetField("_value", flags);
                            if (fVal != null) liveVal = Convert.ToSingle(fVal.GetValue(paramComp));
                            else { var pV = type.GetProperty("Value"); if (pV != null) liveVal = Convert.ToSingle(pV.GetValue(paramComp, null)); }
                            
                            var fMin = type.GetField("MinimumValue", flags) ?? type.GetField("_minimumValue", flags);
                            if (fMin != null) minVal = Convert.ToSingle(fMin.GetValue(paramComp));
                            else { var pMin = type.GetProperty("MinimumValue"); if (pMin != null) minVal = Convert.ToSingle(pMin.GetValue(paramComp, null)); }
                            
                            var fMax = type.GetField("MaximumValue", flags) ?? type.GetField("_maximumValue", flags);
                            if (fMax != null) maxVal = Convert.ToSingle(fMax.GetValue(paramComp));
                            else { var pMax = type.GetProperty("MaximumValue"); if (pMax != null) maxVal = Convert.ToSingle(pMax.GetValue(paramComp, null)); }
                            
                            var fDef = type.GetField("DefaultValue", flags) ?? type.GetField("_defaultValue", flags);
                            if (fDef != null) defVal = Convert.ToSingle(fDef.GetValue(paramComp));
                            else { var pDef = type.GetProperty("DefaultValue"); if (pDef != null) defVal = Convert.ToSingle(pDef.GetValue(paramComp, null)); }
                        }
                        catch { }

                        bool isActive = Math.Abs(liveVal - defVal) > 0.0001f;
                        if (isActive) activeParamCount++;

                        sb.AppendLine($"▶ GameObject: \"{go.name}\"");
                        sb.AppendLine($"  ├─ Path: {GetAbsPath(go)}");
                        sb.AppendLine($"  ├─ Type: {paramComp.GetIl2CppType().FullName}");
                        sb.AppendLine($"  ├─ Live Value : {liveVal:F4} {(isActive ? "[ACTIVE - differs from default]" : "")}");
                        sb.AppendLine($"  ├─ Min Range  : {minVal:F2}");
                        sb.AppendLine($"  ├─ Max Range  : {maxVal:F2}");
                        sb.AppendLine($"  └─ Default    : {defVal:F2}\n");
                    }
                }
                sb.AppendLine($"Total CubismParameters intercepted: {paramCount}");
                sb.AppendLine($"Active parameters (live != default): {activeParamCount}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            // ЧАСТЬ 3: CubismPart парсер
            sb.AppendLine("[PART 3] CUBISM PARTS HIERARCHY");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
                int partCount = 0;
                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    var partComp = go.GetComponent("CubismPart");
                    if (partComp != null)
                    {
                        partCount++;
                        sb.AppendLine($"▶ Part: \"{go.name}\" | Path: {GetAbsPath(go)}");
                    }
                }
                sb.AppendLine($"\nTotal CubismParts found: {partCount}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            // ЧАСТЬ 4: INTERACTION ZONES (ИСПРАВЛЕНО: Используем go.name как ID)
            sb.AppendLine("[PART 4] INTERACTION ZONES & HIT AREAS (CubismRaycastable)");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
                int hitAreaCount = 0;
                int controllerCount = 0;

                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    var comp = go.GetComponent("Live2DInteractionController") 
                            ?? go.GetComponent("ModelBrain") 
                            ?? go.GetComponent("Live2DShowerController");
                    if (comp != null)
                    {
                        controllerCount++;
                        sb.AppendLine($"[🧠 CONTROLLER] \"{go.name}\" | Path: {GetAbsPath(go)} | Type: {comp.GetIl2CppType().Name}");
                    }
                }
                sb.AppendLine($"\nTotal Interaction Controllers found: {controllerCount}\n");

                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    var raycastable = go.GetComponent("CubismRaycastable");
                    if (raycastable != null)
                    {
                        hitAreaCount++;
                        // ВАРИАНТ Г: Используем имя GameObject как идентификатор
                        string hitAreaId = go.name;
                        
                        sb.AppendLine($"[🎯 HIT AREA #{hitAreaCount}] \"{go.name}\"");
                        sb.AppendLine($"  ├─ Path: {GetAbsPath(go)}");
                        sb.AppendLine($"  ├─ Raycastable ID: {hitAreaId}");
                        sb.AppendLine($"  └─ Active: {go.activeInHierarchy}");
                    }
                }
                sb.AppendLine($"\nTotal HitAreas (CubismRaycastable) found: {hitAreaCount}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 3: Live2D extended dump complete (ButtonList + Parameters + Parts + Interaction).");
        }

        // =====================================================================
        // ОСТАЛЬНЫЕ МЕТОДЫ (без изменений)
        // =====================================================================
        private void DumpCuddleAndLuaMaster()
        {
            string path = Path.Combine(f15Cuddle, "Cuddle_And_Lua_API_Master_Report.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("CUDDLE SCENE & LUA API MASTER REPORT (FOLDER 15)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");

            sb.AppendLine("[PART 1] CUDDLE SCENE HIERARCHY & COMPONENTS");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
                int count = 0;
                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    string pathStr = GetAbsPath(go);
                    if (pathStr.Contains("Cuddle", StringComparison.OrdinalIgnoreCase) || go.name.Contains("Cuddle", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        sb.AppendLine($"▶ GameObject: \"{go.name}\" | Path: {pathStr} | Active: {go.activeInHierarchy}");
                        var comps = go.GetComponents<UnityEngine.Component>();
                        foreach (var c in comps)
                        {
                            if (c == null) continue;
                            string compName = c.GetIl2CppType().Name;
                            sb.AppendLine($"  ├─ Component: {compName}");
                            if (c is UnityEngine.SpriteRenderer sr)
                            {
                                if (sr.sprite != null)
                                {
                                    sb.AppendLine($"     └─ Sprite: {sr.sprite.name} | Tex: {sr.sprite.texture.name} ({sr.sprite.texture.width}x{sr.sprite.texture.height})");
                                    sb.AppendLine($"     └─ Color: RGBA({sr.color.r:F2}, {sr.color.g:F2}, {sr.color.b:F2}, {sr.color.a:F2})");
                                }
                            }
                            else if (c is UnityEngine.UI.Image img)
                            {
                                if (img.sprite != null) sb.AppendLine($"     └─ UI.Image Sprite: {img.sprite.name}");
                            }
                            else if (c is UnityEngine.UI.RawImage raw)
                            {
                                if (raw.texture != null) sb.AppendLine($"     └─ UI.RawImage Texture: {raw.texture.name}");
                            }
                        }
                        sb.AppendLine();
                    }
                }
                sb.AppendLine($"Total Cuddle-related GameObjects: {count}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            sb.AppendLine("[PART 2] FUNGUS VARIABLES INTERCEPTION (Sprite/Texture Check)");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo2 = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
                int fungusCount = 0;
                foreach (var go in allGo2)
                {
                    if (go == null || !go.activeInHierarchy) continue;
                    if (!go.name.Contains("Cuddle", StringComparison.OrdinalIgnoreCase)) continue;
                    var comps = go.GetComponents<UnityEngine.Component>();
                    foreach (var c in comps)
                    {
                        if (c == null) continue;
                        string typeName = c.GetIl2CppType().FullName;
                        if (typeName.Contains("Fungus") && (typeName.Contains("SpriteVariable") || typeName.Contains("TextureVariable") || typeName.Contains("Variable")))
                        {
                            fungusCount++;
                            sb.AppendLine($"▶ Found Fungus Variable on: \"{go.name}\" ({typeName})");
                            try
                            {
                                var il2cppType = c.GetIl2CppType();
                                var valProp = il2cppType.GetProperty("Value", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                                if (valProp != null)
                                {
                                    var val = valProp.GetValue(c, null);
                                    sb.AppendLine($"  └─ Value: {val?.ToString() ?? "NULL"}");
                                }
                            }
                            catch { }
                        }
                    }
                }
                sb.AppendLine($"\nTotal Fungus Variables found in Cuddle context: {fungusCount}\n");
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            sb.AppendLine("[PART 3] ViewSingleton INSPECTION");
            sb.AppendLine("-------------------------------------------------------------------------");
            try
            {
                var allGo3 = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
                foreach (var go in allGo3)
                {
                    if (go == null) continue;
                    if (go.name == "ViewSingleton")
                    {
                        sb.AppendLine($"▶ Found ViewSingleton at: {GetAbsPath(go)}");
                        var viewSingletonComp = go.GetComponent("ViewSingleton");
                        if (viewSingletonComp != null)
                        {
                            var il2cppType = viewSingletonComp.GetIl2CppType();
                            var cuddleField = il2cppType.GetField("cuddleView", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                            if (cuddleField != null)
                            {
                                object val = cuddleField.GetValue(viewSingletonComp);
                                if (val != null)
                                {
                                    sb.AppendLine($"  [OK] cuddleView found! Type: {val.GetType().FullName}");
                                    var valType = val.GetType();
                                    var subFields = valType.GetFields((BindingFlags)(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance));
                                    foreach (var sf in subFields)
                                    {
                                        if (sf.Name == "isWrapped" || sf.Name == "pooledPtr" || sf.Name == "klass") continue;
                                        try
                                        {
                                            object sfVal = sf.GetValue(val);
                                            sb.AppendLine($"    - {sf.FieldType.Name} {sf.Name} = {sfVal?.ToString() ?? "NULL"}");
                                        }
                                        catch { }
                                    }
                                }
                                else { sb.AppendLine($"  [!] cuddleView is NULL"); }
                            }
                            else { sb.AppendLine($"  [!] cuddleView field not found in ViewSingleton."); }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}\n"); }

            sb.AppendLine("\n[PART 4] LUA SANDBOX ALPHABET PARSER (Exposed API)");
            sb.AppendLine("-------------------------------------------------------------------------");
            string[] targetTypes = { "ViewSingleton", "ModUtilities", "TextureOverriderManager", "UiOverlay", "GameVariables" };
            foreach (var typeName in targetTypes)
            {
                sb.AppendLine($"\n▶ TARGET: {typeName}");
                try
                {
                    var assemblies = Il2CppSystem.AppDomain.CurrentDomain.GetAssemblies();
                    Il2CppSystem.Type foundType = null;
                    foreach (var asm in assemblies)
                    {
                        try { foundType = asm.GetType(typeName); if (foundType != null) break; } catch { }
                    }
                    if (foundType == null) { sb.AppendLine("  [NOT FOUND IN IL2CPP]"); continue; }
                    var members = new List<string>();
                    var methods = foundType.GetMethods(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        try
                        {
                            var pars = m.GetParameters();
                            string parStr = "";
                            foreach (var p in pars) parStr += (parStr.Length > 0 ? ", " : "") + p.ParameterType.Name + " " + p.Name;
                            string staticStr = m.IsStatic ? "[STATIC] " : "";
                            members.Add($"  {staticStr}[METHOD] {m.ReturnType.Name} {m.Name}({parStr})");
                        }
                        catch { }
                    }
                    var fields = foundType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                    foreach (var f in fields)
                    {
                        try
                        {
                            string staticStr = f.IsStatic ? "[STATIC] " : "";
                            members.Add($"  {staticStr}[FIELD] {f.FieldType.Name} {f.Name}");
                        }
                        catch { }
                    }
                    var props = foundType.GetProperties(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                    foreach (var p in props)
                    {
                        try { members.Add($"  [PROPERTY] {p.PropertyType.Name} {p.Name}"); } catch { }
                    }
                    members.Sort();
                    sb.AppendLine($"  Total exposed members: {members.Count}");
                    foreach (var member in members) sb.AppendLine(member);
                }
                catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}"); }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 15: Cuddle & Lua API Master Report complete.");
        }

        private void DumpMissingItemsDeepDive()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("MISSING ITEMS DEEP DIVE v6.5.1 (FOLDER 14)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            int totalItemsFound = 0;
            int missingItemsCount = 0;
            sb.AppendLine("[STEP 1] GameVariables.Current...");
            object gvInstance = null;
            try
            {
                var currentProp = _gameVariablesType?.GetProperty("Current", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Static);
                if (currentProp != null)
                {
                    gvInstance = currentProp.GetValue(null, null);
                    sb.AppendLine($"  [OK] Pointer: {GetPointer(gvInstance)}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"  [EXCEPTION] {ex.Message}"); }
            sb.AppendLine("\n[STEP 2] itemManager from GameVariables...");
            object itemManagerInstance = null;
            if (gvInstance != null && _gameVariablesType != null)
            {
                try
                {
                    var imField = _gameVariablesType.GetField("itemManager", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    if (imField != null)
                    {
                        itemManagerInstance = imField.GetValue((Il2CppSystem.Object)gvInstance);
                        sb.AppendLine($"  [OK] Pointer: {GetPointer(itemManagerInstance)}");
                    }
                }
                catch (Exception ex) { sb.AppendLine($"  [EXCEPTION] {ex.Message}"); }
            }
            sb.AppendLine("\n[STEP 3] FULL DUMP of ItemManager fields via IL2CPP type...");
            if (itemManagerInstance != null && _itemManagerType != null)
            {
                try
                {
                    var fields = _itemManagerType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                    sb.AppendLine($"  Found {fields.Length} fields in ItemManager (via IL2CPP type):\n");
                    foreach (var field in fields)
                    {
                        string typeName = field.FieldType.FullName ?? field.FieldType.Name;
                        sb.AppendLine($"  FIELD: {field.Name}");
                        sb.AppendLine($"    Type: {typeName}");
                        try
                        {
                            var value = field.GetValue((Il2CppSystem.Object)itemManagerInstance);
                            if (value != null)
                            {
                                string valueStr = value.ToString() ?? "<null>";
                                if (valueStr.Length > 300) valueStr = valueStr.Substring(0, 300) + "...";
                                sb.AppendLine($"    Value: {valueStr}");
                                sb.AppendLine($"    Value Type: {value.GetType().FullName}");
                                sb.AppendLine($"    Pointer: {GetPointer(value)}");
                            }
                            else { sb.AppendLine($"    Value: NULL"); }
                        }
                        catch (Exception ex) { sb.AppendLine($"    [ERROR reading value] {ex.Message}"); }
                        sb.AppendLine();
                    }
                }
                catch (Exception ex) { sb.AppendLine($"  [EXCEPTION] {ex.Message}"); }
            }
            else { sb.AppendLine("  [SKIP] itemManagerType or itemManagerInstance is null"); }
            sb.AppendLine("\n[STEP 4] DETAILED DIAGNOSTICS with ToString() detection...");
            try
            {
                var allObjects = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
                int containerCount = 0;
                foreach (var go in allObjects)
                {
                    if (go == null || !go.activeInHierarchy) continue;
                    if (!go.name.Contains("GameItem(Clone)")) continue;
                    var containerComp = go.GetComponent("GameItemContainer");
                    if (containerComp == null) continue;
                    containerCount++;
                    totalItemsFound++;
                    sb.AppendLine($"\n  === UI ITEM #{containerCount} ===");
                    sb.AppendLine($"  Path: {GetPath(go)}");
                    sb.AppendLine($"  InstanceID: {go.GetInstanceID()}");
                    try
                    {
                        var contType = containerComp.GetIl2CppType();
                        var currentField = contType.GetField("_current", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                        if (currentField != null)
                        {
                            var itemObj = currentField.GetValue(containerComp);
                            if (itemObj != null)
                            {
                                sb.AppendLine($"  Item Pointer: {GetPointer(itemObj)}");
                                bool isMissing = false;
                                if (_prefabField != null)
                                {
                                    try
                                    {
                                        var prefab = _prefabField.GetValue((Il2CppSystem.Object)itemObj);
                                        if (prefab != null)
                                        {
                                            string prefabStr = prefab.ToString() ?? "";
                                            sb.AppendLine($"  Prefab Pointer: {GetPointer(prefab)}");
                                            if (prefabStr.Contains("IsMissingItem : True", StringComparison.OrdinalIgnoreCase) ||
                                                prefabStr.Contains("IsMissingItem: True", StringComparison.OrdinalIgnoreCase) ||
                                                prefabStr.Contains("Name : Missing Item", StringComparison.OrdinalIgnoreCase))
                                            {
                                                isMissing = true;
                                                sb.AppendLine($"  [DETECTED] IsMissingItem = TRUE (via ToString)");
                                            }
                                            try
                                            {
                                                int idx = prefabStr.IndexOf("Item id (", StringComparison.OrdinalIgnoreCase);
                                                if (idx >= 0)
                                                {
                                                    int endIdx = prefabStr.IndexOf(")", idx);
                                                    if (endIdx > idx)
                                                    {
                                                        string itemId = prefabStr.Substring(idx + 9, endIdx - idx - 9);
                                                        sb.AppendLine($"  Item ID: {itemId}");
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            isMissing = true;
                                            sb.AppendLine($"  [DETECTED] _prefab is NULL");
                                        }
                                    }
                                    catch (Exception ex) { sb.AppendLine($"  [ERROR reading _prefab] {ex.Message}"); }
                                }
                                if (_gameIdProperty != null)
                                {
                                    try
                                    {
                                        var gid = _gameIdProperty.GetValue((Il2CppSystem.Object)itemObj, null);
                                        if (gid != null) sb.AppendLine($"  GameId: {gid.ToString()}");
                                    }
                                    catch { }
                                }
                                if (isMissing)
                                {
                                    missingItemsCount++;
                                    sb.AppendLine($"  *** MISSING ITEM DETECTED ***");
                                }
                                else { sb.AppendLine($"  [OK] Item is valid"); }
                            }
                            else { sb.AppendLine($"  _current is NULL (empty slot)"); }
                        }
                    }
                    catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}"); }
                }
                sb.AppendLine($"\n  [SUMMARY] Found {containerCount} GameItemContainers.");
            }
            catch (Exception ex) { sb.AppendLine($"  [EXCEPTION] {ex.Message}"); }
            sb.AppendLine("\n=========================================================================");
            sb.AppendLine($"FINAL COUNT: Total Items Scanned: {totalItemsFound} | Missing Items Detected: {missingItemsCount}");
            sb.AppendLine("=========================================================================");
            string filePath = Path.Combine(f14MissingItems, "Report.txt");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg($"Report saved to: {filePath}");
        }

        private string GetPointer(object obj)
        {
            if (obj == null) return "00000000";
            try
            {
                if (obj is Il2CppObjectBase il2cppObj) return il2cppObj.Pointer.ToString("X");
                var ptrField = obj.GetType().GetField("Pointer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (ptrField != null) return ((IntPtr)ptrField.GetValue(obj)).ToString("X");
            }
            catch { }
            return "UNKNOWN";
        }

        private string GetPath(GameObject go)
        {
            if (go == null) return "";
            string p = go.name;
            Transform par = go.transform.parent;
            int depth = 0;
            while (par != null && depth < 15)
            {
                p = par.name + "/" + p;
                par = par.parent;
                depth++;
            }
            return "/" + p;
        }

        private string DetectScene()
        {
            string name = "UNKNOWN";
            var transforms = UnityEngine.Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in transforms)
            {
                if (t != null && t.gameObject != null && t.parent != null && t.parent.gameObject.name == "Views" && t.gameObject.activeSelf)
                {
                    name = t.gameObject.name;
                    break;
                }
            }
            return name;
        }

        private string GetAbsPath(GameObject go)
        {
            if (go == null) return "";
            string p = go.name;
            Transform par = go.transform.parent;
            while (par != null)
            {
                if (par.name == "Views" || par.name == "Canvas" || par.name == "Live2D Models") break;
                p = par.name + "/" + p;
                par = par.transform.parent;
            }
            return "/" + p;
        }

        private List<ManagerData> ExtractManagers()
        {
            List<ManagerData> list = new List<ManagerData>();
            var all = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null) continue;
                var comp = go.GetComponent("TextureOverriderManager");
                if (comp != null)
                {
                    string id = null;
                    string path = GetAbsPath(go);
                    foreach (var kvp in VerifiedSceneToManagerId)
                    {
                        string sceneName = kvp.Key.Replace("/Views/", "");
                        bool pathMatch = path.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || path.StartsWith(kvp.Key + "/", StringComparison.OrdinalIgnoreCase);
                        bool nameMatch = go.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase);
                        if (pathMatch || nameMatch) { id = kvp.Value; break; }
                    }
                    if (id == null)
                    {
                        try
                        {
                            var il2cppType = comp.GetIl2CppType();
                            string[] possibleNames = { "id", "_id", "managerId", "ManagerId", "ID", "_ID" };
                            foreach (string fieldName in possibleNames)
                            {
                                var field = il2cppType.GetField(fieldName, Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    object val = field.GetValue(comp);
                                    if (val != null) { id = val.ToString(); break; }
                                }
                            }
                        }
                        catch { }
                    }
                    if (id == null) id = "UNKNOWN";
                    list.Add(new ManagerData { goName = go.name, path = path, revealedId = id });
                }
            }
            return list;
        }

        private void DumpManagers()
        {
            string path = Path.Combine(f4Managers, "Managers_Passport.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("CORE MANAGERS RUNTIME PASSPORT (FOLDER 4)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine("Component Type: TextureOverriderManager");
            sb.AppendLine("Purpose: Manage sprite overrides for scene-specific nodes");
            sb.AppendLine("Usage in Lua: TextureOverriderManager.GetTextureOverriderManager(\"ID\")");
            sb.AppendLine("=========================================================================\n");
            for (int i = 0; i < mgrCache.Count; i++)
            {
                var m = mgrCache[i];
                string context = "";
                if (m.revealedId == "" && m.goName.Contains("Changing")) context = " - Lady Annalie Fitting Room (Isolated Scene)";
                else if (m.revealedId == "Room") context = " - Room Anon Player";
                else if (m.revealedId == "Shop") context = " - Melissa Grocery (Isolated Scene)";
                else if (m.revealedId == "shower") context = " - Anon Player and Jun Bot Shower (Isolated Scene)";
                else if (m.revealedId == "room_missionary") context = " - Missionary Pose (Isolated Scene)";
                else if (m.revealedId == "Clothier") context = " - Clothier Shop Lady Annalie (Isolated Scene)";
                sb.AppendLine($"Instance #{i + 1}");
                sb.AppendLine($"  Component Type   : TextureOverriderManager");
                sb.AppendLine($"  Object Name      : \"{m.goName}\"{context}");
                sb.AppendLine($"  Absolute Path    : {m.path}");
                sb.AppendLine($"  REVEALED MGR ID  : \"{m.revealedId}\"");
                sb.AppendLine($"  Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager(\"{m.revealedId}\")");
                sb.AppendLine($"                     mgr:SetSprite(CurrentModGuid, \"PATH\", customSprite)");
                sb.AppendLine($"                     mgr:RestoreSprite(CurrentModGuid, \"PATH\", customSprite)");
                sb.AppendLine();
            }
            sb.AppendLine($"Total TextureOverriderManager instances: {mgrCache.Count}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 4: " + mgrCache.Count + " managers with Lua usage info.");
        }

        private void DumpSceneNodes(string scene)
        {
            string path = Path.Combine(f2Nodes, $"Scene_Nodes_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=========================================================================================");
            sb.AppendLine($"{scene.ToUpper()} SCENE DEEP STRUCTURE DUMP | GENERATED: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"=========================================================================================");
            GameObject root = GameObject.Find("/Views/" + scene) ?? GameObject.Find(scene);
            if (root != null) DumpTreeDetailed(root.transform, sb, 0);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 2: Scene nodes dumped.");
        }

        private void DumpTreeDetailed(Transform t, StringBuilder sb, int lvl)
        {
            if (t == null) return;
            GameObject go = t.gameObject;
            string indent = new string(' ', lvl * 2);
            sb.AppendLine($"{indent}▶ GameObject: \"{go.name}\" (ActiveInHierarchy: {go.activeInHierarchy}, ActiveSelf: {go.activeSelf})");
            Vector3 lp = t.localPosition;
            Vector3 lr = t.localEulerAngles;
            Vector3 ls = t.localScale;
            Vector3 wp = t.position;
            Vector3 ws = t.lossyScale;
            sb.AppendLine($"{indent}├─ Transform: LocalPosition=({lp.x:F2}, {lp.y:F2}, {lp.z:F2}), LocalRotation=({lr.x:F2}, {lr.y:F2}, {lr.z:F2}), LocalScale=({ls.x:F2}, {ls.y:F2}, {ls.z:F2})");
            sb.AppendLine($"{indent}├─ Transform World: Position=({wp.x:F2}, {wp.y:F2}, {wp.z:F2}), Scale=({ws.x:F2}, {ws.y:F2}, {ws.z:F2})");
            sb.AppendLine($"{indent}├─ [ALL ATTACHED COMPONENTS]:");
            var comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c != null)
                {
                    string ns = c.GetIl2CppType().Namespace ?? "Unknown";
                    sb.AppendLine($"{indent}│    ● {c.GetIl2CppType().Name} (Namespace: {ns})");
                }
            }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null && sr.sprite.texture != null)
            {
                sb.AppendLine($"{indent}├─ [[ SPRITE RENDERER DETECTED ]]:");
                sb.AppendLine($"{indent}│    ● Sorting Layer ID: {sr.sortingLayerID} | Name: {sr.sortingLayerName}");
                sb.AppendLine($"{indent}│    ● Sorting Order: {sr.sortingOrder}");
                sb.AppendLine($"{indent}│    ● Color RGBA: RGBA({sr.color.r:F3}, {sr.color.g:F3}, {sr.color.b:F3}, {sr.color.a:F3})");
                sb.AppendLine($"{indent}│    ● Sprite Name: \"{sr.sprite.name}\"");
                Rect rect = sr.sprite.rect;
                sb.AppendLine($"{indent}│    ● Sprite Rect: (x:{rect.x:F2}, y:{rect.y:F2}, width:{rect.width:F2}, height:{rect.height:F2})");
                sb.AppendLine($"{indent}│    ● Pixels Per Unit: {sr.sprite.pixelsPerUnit}");
                sb.AppendLine($"{indent}│    ● Texture Name: \"{sr.sprite.texture.name}\"");
                sb.AppendLine($"{indent}│    ● Texture Resolution: {sr.sprite.texture.width}x{sr.sprite.texture.height}");
                sb.AppendLine($"{indent}│    ● Texture Wrap Mode: {sr.sprite.texture.wrapMode}");
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                sb.AppendLine($"{indent}├─ [[ GENERIC RENDERER / MESH DETECTED ]]:");
                sb.AppendLine($"{indent}│    ● Renderer Type: MeshRenderer");
                sb.AppendLine($"{indent}│    ● Enabled: {mr.enabled}");
                sb.AppendLine($"{indent}│    ● Material Name: \"{mr.material.name}\"");
                sb.AppendLine($"{indent}│    ● Shader Name: \"{mr.material.shader.name}\"");
            }
            var psr = go.GetComponent("ParticleSystemRenderer");
            if (psr != null)
            {
                try
                {
                    var psrType = psr.GetType();
                    var matProp = psrType.GetProperty("material");
                    var mat = matProp?.GetValue(psr, null) as Material;
                    if (mat != null)
                    {
                        sb.AppendLine($"{indent}├─ [[ PARTICLE SYSTEM RENDERER DETECTED ]]:");
                        sb.AppendLine($"{indent}│    ● Renderer Type: ParticleSystemRenderer");
                        sb.AppendLine($"{indent}│    ● Material Name: \"{mat.name}\"");
                        sb.AppendLine($"{indent}│    ● Shader Name: \"{mat.shader.name}\"");
                        if (mat.mainTexture != null)
                        {
                            sb.AppendLine($"{indent}│    ● Main Texture Name: \"{mat.mainTexture.name}\"");
                            sb.AppendLine($"{indent}│    ● Main Texture Resolution: {mat.mainTexture.width}x{mat.mainTexture.height}");
                        }
                    }
                }
                catch { }
            }
            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTreeDetailed(t.GetChild(i), sb, lvl + 1);
            }
        }

        private void DumpViewsAndScenes(string scene)
        {
            string path = Path.Combine(f5Views, "Views_Scenes.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("VIEWS AND SCENES CONTAINERS (FOLDER 5)");
            sb.AppendLine("=========================================================================");
            var all = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null || !go.activeInHierarchy) continue;
                if (go.name.Contains("View") || go.name.Contains("Scene") || go.name.Contains("Holder") || go.name.Contains("Popup") || go.name.Contains("Dialog"))
                {
                    sb.AppendLine($"Container: \"{go.name}\" | InstanceID: {go.GetInstanceID()} | Path: {GetAbsPath(go)} | Active: {go.activeSelf}");
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 5: Views dumped.");
        }

        private void DumpSpriteInfo(string scene)
        {
            string path = Path.Combine(f7SpriteInfo, $"Sprite_Info_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"SPRITE INFO DUMP - SCENE: {scene} (FOLDER 7)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine("Format: InstanceID | NodePath | SpriteName | Resolution | PPU | Pivot | SortingLayer(ID) | Order | Color | TextureFile(WrapMode)");
            sb.AppendLine("=========================================================================");
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            int count = 0;
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                {
                    count++;
                    string colorStr = $"RGBA({sr.color.r:F2}, {sr.color.g:F2}, {sr.color.b:F2}, {sr.color.a:F2})";
                    string layerStr = $"{sr.sortingLayerName}({sr.sortingLayerID})";
                    string texStr = $"{sr.sprite.texture.name}({sr.sprite.texture.width}x{sr.sprite.texture.height}, {sr.sprite.texture.wrapMode})";
                    sb.AppendLine($"{go.GetInstanceID()} | {GetAbsPath(go)} | {sr.sprite.name} | {sr.sprite.rect.width}x{sr.sprite.rect.height} | PPU:{sr.sprite.pixelsPerUnit:F2} | Pivot:({sr.sprite.pivot.x:F2},{sr.sprite.pivot.y:F2}) | Layer:{layerStr} | Order:{sr.sortingOrder} | Color:{colorStr} | Tex:{texStr}");
                }
            }
            sb.AppendLine($"\nTotal sprites found: {count}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 7: Sprite info dumped.");
        }

        private void DumpWin32Interceptor(string scene)
        {
            string txtPath = Path.Combine(f8Win32, $"Win32_Active_Screen_Parsing_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("WIN32 UI & BOOLEAN INTERCEPTOR REPORT (FOLDER 8)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"ACTIVE SCENE CONTEXT: {scene}");
            sb.AppendLine("=========================================================================\n");
            string[] keywords = { "Nun", "Repair", "Clothier", "Order", "Popup", "Shop", "Cheat", "Item", "Changing", "Equipment", "SayDialog", "Priest", "SimplePopup", "Items", "Button", "List" };
            int containerCount = 0;
            var allObjects = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allObjects)
            {
                if (go == null || !go.activeInHierarchy) continue;
                bool match = false;
                foreach (var kw in keywords)
                {
                    if (go.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                        break;
                    }
                }
                if (match)
                {
                    containerCount++;
                    sb.AppendLine($"▶ INTERFACE CONTAINER #{containerCount}: \"{go.name}\" | InstanceID: {go.GetInstanceID()}");
                    sb.AppendLine($"├─ Absolute Path: {GetAbsPath(go)}");
                    sb.AppendLine($"├─ ActiveSelf: {go.activeSelf} | ActiveInHierarchy: {go.activeInHierarchy}");
                    var components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        sb.AppendLine($"├─ Component: {comp.GetIl2CppType().Name}");
                        try
                        {
                            var il2cppType = comp.GetIl2CppType();
                            var fields = il2cppType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                            foreach (var f in fields)
                            {
                                if (f.Name == "isWrapped" || f.Name == "pooledPtr" || f.Name == "klass") continue;
                                if (f.FieldType.FullName == "System.Boolean")
                                {
                                    try
                                    {
                                        object valObj = f.GetValue(comp);
                                        bool val = valObj != null && (bool)valObj;
                                        sb.AppendLine($"│    ├── [🚨 BOOLEAN] {f.Name} = {val}");
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }
                    sb.AppendLine();
                }
            }
            sb.AppendLine($"TOTAL INTERFACE CONTAINERS SCANNED: {containerCount}");
            File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg($"[OK] Folder 8: Win32 Screen Interceptor dumped ({containerCount} containers).");
        }

        private void DumpArchitectureMap()
        {
            string path = Path.Combine(f9Architecture, "Global_Architecture_Map.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("GLOBAL ARCHITECTURE MAP & SINGLETONS (FOLDER 9)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            string[] targetSingletons = { "ViewSingleton", "StorySingleton", "Live2DControllerSingleton", "SimpleCharacterSingleton", "SaveSingleton", "AchievementSingleton", "ModsSingleton", "ModUtilities", "TextureSingleton", "SoundSingleton", "CursorSingleton", "UiOverlay", "GameUtilities", "FungusEnumerators", "ConversationSingleton" };
            sb.AppendLine("[1] CORE SINGLETONS & MANAGERS");
            foreach (var singletonName in targetSingletons)
            {
                foreach (var go in allGo)
                {
                    if (go == null) continue;
                    if (go.name == singletonName || go.GetComponent(singletonName) != null)
                    {
                        sb.AppendLine($"▶ Found: {singletonName}");
                        sb.AppendLine($"  ├─ Path: {GetAbsPath(go)}");
                        sb.AppendLine($"  ├─ InstanceID: {go.GetInstanceID()}");
                        sb.AppendLine($"  ├─ Active: {go.activeInHierarchy}");
                        break;
                    }
                }
            }
            sb.AppendLine("\n[2] CONFIRMED MANAGER-LESS SCENES (Direct Injection Required)");
            string[] managerLessScenes = { "Cuddle Scene", "Scene 2", "Scene 1", "Scene 3", "Menu Scene", "PC Scene", "Outside Scene", "Developer Room Scene" };
            foreach (var sceneName in managerLessScenes)
            {
                var sceneGo = GameObject.Find("/Views/" + sceneName) ?? GameObject.Find(sceneName);
                if (sceneGo != null)
                {
                    sb.AppendLine($"▶ {sceneName}");
                    sb.AppendLine($"  ├─ Path: {GetAbsPath(sceneGo)}");
                    sb.AppendLine($"  ├─ InstanceID: {sceneGo.GetInstanceID()}");
                    sb.AppendLine($"  └─ Note: No TextureOverriderManager attached.");
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 9: Architecture Map generated.");
        }

        private void DumpPopupClonePopup(string scene)
        {
            string path = Path.Combine(f10Popup, $"Popup_ClonePopup_Report_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("POPUP & CLONE POPUP RUNTIME MAP (FOLDER 10)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"ACTIVE SCENE CONTEXT: {scene}");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            string[] keywords = { "Popup", "Dialog", "Menu", "Info", "Save", "Options", "Shop", "Clothier", "Changing", "Equipment", "Cuddle", "Shower", "Fishing", "Cooking", "OldMaid", "Developer", "Speech", "NunRepair", "Simple", "Items" };
            int popupCount = 0;
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                bool match = false;
                foreach (var kw in keywords)
                {
                    if (go.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                        break;
                    }
                }
                if (match)
                {
                    popupCount++;
                    sb.AppendLine($"▶ POPUP #{popupCount}: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    sb.AppendLine($"├─ InstanceID: {go.GetInstanceID()}");
                    sb.AppendLine($"├─ ActiveSelf: {go.activeSelf} | ActiveInHierarchy: {go.activeInHierarchy}");
                    var canvasGroup = go.GetComponent("UnityEngine.CanvasGroup");
                    if (canvasGroup != null)
                    {
                        try
                        {
                            var alphaProp = canvasGroup.GetType().GetProperty("alpha");
                            if (alphaProp != null)
                            {
                                float alpha = Convert.ToSingle(alphaProp.GetValue(canvasGroup, null));
                                sb.AppendLine($"├─ CanvasGroup.alpha: {alpha:F2}");
                            }
                        }
                        catch { }
                    }
                    var popupScript = go.GetComponent("PopupCloseScript") ?? go.GetComponent("StaticPopup") ?? go.GetComponent("SayDialogPopup") ?? go.GetComponent("ShopPopup") ?? go.GetComponent("NunRepairPopup");
                    if (popupScript != null)
                    {
                        sb.AppendLine($"├─ Popup Script: {popupScript.GetIl2CppType().Name}");
                    }
                    var children = go.GetComponentsInChildren<Transform>();
                    int childCount = 0;
                    foreach (var child in children)
                    {
                        if (child == go.transform) continue;
                        childCount++;
                        if (childCount > 10)
                        {
                            sb.AppendLine($"└─ ... and {children.Length - 10} more children");
                            break;
                        }
                        sb.AppendLine($"├─ Child: \"{child.gameObject.name}\" | Active: {child.gameObject.activeSelf} | ID: {child.gameObject.GetInstanceID()}");
                    }
                    sb.AppendLine();
                }
            }
            sb.AppendLine($"=========================================================================");
            sb.AppendLine($"TOTAL POPUPS DETECTED: {popupCount}");
            sb.AppendLine($"=========================================================================");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg($"[OK] Folder 10: {popupCount} popups dumped.");
        }

        private void DumpInventoryDeep(string scene)
        {
            string path = Path.Combine(f11Inventory, $"Inventory_Deep_Dump_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("INVENTORY DEEP DUMP - ITEM DATA & MISSING FLAGS (FOLDER 11)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"ACTIVE SCENE CONTEXT: {scene}");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            int itemCount = 0;
            int missingCount = 0;
            sb.AppendLine("[1] GAME ITEM CONTAINERS - DETAILED ITEM DATA");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                if (!go.name.Contains("GameItem(Clone)")) continue;
                var container = go.GetComponent("GameItemContainer");
                if (container == null) continue;
                itemCount++;
                sb.AppendLine($"▶ GameItem #{itemCount}: \"{go.name}\"");
                sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                sb.AppendLine($"├─ InstanceID: {go.GetInstanceID()}");
                sb.AppendLine($"├─ ActiveSelf: {go.activeSelf}");
                try
                {
                    var containerType = container.GetIl2CppType();
                    var fields = containerType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                    foreach (var f in fields)
                    {
                        if (f.Name == "isWrapped" || f.Name == "pooledPtr" || f.Name == "klass") continue;
                        try
                        {
                            object val = f.GetValue(container);
                            string valType = val != null ? val.GetType().Name : "NULL";
                            sb.AppendLine($"│    └─ [{containerType.Name}] {f.Name} = {valType}");
                            if (f.Name == "_current" && val != null)
                            {
                                try
                                {
                                    bool isMissing = false;
                                    string missingReason = "";
                                    if (_prefabField != null)
                                    {
                                        try
                                        {
                                            var prefab = _prefabField.GetValue((Il2CppSystem.Object)val);
                                            if (prefab == null)
                                            {
                                                isMissing = true;
                                                missingReason = "_prefab is NULL";
                                            }
                                            else
                                            {
                                                string prefabStr = prefab.ToString() ?? "";
                                                if (prefabStr.Contains("IsMissingItem : True", StringComparison.OrdinalIgnoreCase) ||
                                                    prefabStr.Contains("Name : Missing Item", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    isMissing = true;
                                                    missingReason = "IsMissingItem = True (via ToString)";
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    if (!isMissing && _gameIdProperty != null)
                                    {
                                        try
                                        {
                                            var gameId = _gameIdProperty.GetValue((Il2CppSystem.Object)val, null);
                                            if (gameId == null)
                                            {
                                                isMissing = true;
                                                missingReason = "GameId is NULL";
                                            }
                                            else
                                            {
                                                string gameIdStr = gameId.ToString();
                                                if (string.IsNullOrEmpty(gameIdStr) || gameIdStr == "00000000-0000-0000-0000-000000000000")
                                                {
                                                    isMissing = true;
                                                    missingReason = $"GameId is EMPTY ({gameIdStr})";
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    if (isMissing)
                                    {
                                        missingCount++;
                                        sb.AppendLine($"│         └─ [🚨 MISSING ITEM DETECTED] Reason: {missingReason}");
                                        if (_gameIdProperty != null)
                                        {
                                            try
                                            {
                                                var gid = _gameIdProperty.GetValue((Il2CppSystem.Object)val, null);
                                                if (gid != null) sb.AppendLine($"│         └─ GameId: {gid.ToString()}");
                                            }
                                            catch { }
                                        }
                                    }
                                    else { sb.AppendLine($"│         └─ [OK] Item is valid"); }
                                }
                                catch (Exception ex) { sb.AppendLine($"│         └─ [ERROR] Cannot read Item: {ex.Message}"); }
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex) { sb.AppendLine($"│    └─ [ERROR] Failed to read fields: {ex.Message}"); }
                sb.AppendLine();
            }
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"TOTAL GAME ITEMS FOUND: {itemCount}");
            sb.AppendLine($"TOTAL MISSING ITEMS: {missingCount}");
            sb.AppendLine("=========================================================================\n");
            sb.AppendLine("[2] INVENTORY LIST COMPONENTS");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                var itemList = go.GetComponent("ItemList") ?? go.GetComponent("AdvancedItemList") ?? go.GetComponent("CompoundItemList") ?? go.GetComponent("SortableItemList");
                if (itemList != null)
                {
                    sb.AppendLine($"▶ List: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    sb.AppendLine($"├─ Type: {itemList.GetIl2CppType().Name}");
                    try
                    {
                        var listType = itemList.GetIl2CppType();
                        var fields = listType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                        int fieldCount = 0;
                        foreach (var f in fields)
                        {
                            if (f.Name == "isWrapped" || f.Name == "pooledPtr" || f.Name == "klass") continue;
                            if (fieldCount >= 10) break;
                            try
                            {
                                object val = f.GetValue(itemList);
                                string valType = val != null ? val.GetType().Name : "NULL";
                                sb.AppendLine($"│    └─ [{listType.Name}] {f.Name} = {valType}");
                                fieldCount++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                    sb.AppendLine();
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg($"[OK] Folder 11: Deep inventory dump complete. Items: {itemCount}, Missing: {missingCount}.");
        }

        private void DumpGameState(string scene)
        {
            string path = Path.Combine(f12GameState, $"Game_State_Report_{scene.Replace(" ", "_")}.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("GAME STATE DUMP - FUNGUS, VARIABLES & SAVE DATA (FOLDER 12)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"ACTIVE SCENE CONTEXT: {scene}");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            sb.AppendLine("[1] FUNGUS FLOWCHART STATE");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null) continue;
                var flowchart = go.GetComponent("Fungus.Flowchart");
                if (flowchart != null)
                {
                    sb.AppendLine($"▶ Flowchart: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    sb.AppendLine($"├─ InstanceID: {go.GetInstanceID()}");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("[2] GAME VARIABLES (Fungus.GlobalVariables)");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null) continue;
                var globalVars = go.GetComponent("Fungus.GlobalVariables");
                if (globalVars != null)
                {
                    sb.AppendLine($"▶ GlobalVariables: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("[3] SAVE SINGLETON STATE");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null) continue;
                var saveSingleton = go.GetComponent("SaveSingleton");
                if (saveSingleton != null)
                {
                    sb.AppendLine($"▶ SaveSingleton: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    try
                    {
                        var saveType = saveSingleton.GetIl2CppType();
                        var fields = saveType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                        int fieldCount = 0;
                        foreach (var f in fields)
                        {
                            if (f.Name == "isWrapped" || f.Name == "pooledPtr" || f.Name == "klass") continue;
                            if (fieldCount >= 30) break;
                            try
                            {
                                object val = f.GetValue(saveSingleton);
                                string valType = val != null ? val.GetType().Name : "NULL";
                                sb.AppendLine($"│    └─ {f.Name} = {valType}");
                                fieldCount++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                    sb.AppendLine();
                }
            }
            sb.AppendLine("[4] NUN REPAIR POPUP STATE");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                if (!go.name.Contains("NunRepairPopup")) continue;
                sb.AppendLine($"▶ NunRepairPopup: \"{go.name}\"");
                sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                sb.AppendLine($"├─ InstanceID: {go.GetInstanceID()}");
                sb.AppendLine($"├─ ActiveSelf: {go.activeSelf}");
                sb.AppendLine();
            }
            sb.AppendLine("[5] ITEM MANAGER STATE");
            sb.AppendLine("-------------------------------------------------------------------------");
            foreach (var go in allGo)
            {
                if (go == null) continue;
                var itemManager = go.GetComponent("Item_ItemManager") ?? go.GetComponent("ItemManager");
                if (itemManager != null)
                {
                    sb.AppendLine($"▶ ItemManager: \"{go.name}\"");
                    sb.AppendLine($"├─ Path: {GetAbsPath(go)}");
                    sb.AppendLine($"├─ InstanceID: {go.GetInstanceID()}");
                    sb.AppendLine();
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 12: Game state dump complete.");
        }

        private void DumpItemManagerDebug()
        {
            string path = Path.Combine(f13ItemManager, "ItemManager_Debug.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=========================================================================");
            sb.AppendLine("ITEMMANAGER DEBUG - REAL SINGLETON SEARCH (FOLDER 13)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("=========================================================================\n");
            sb.AppendLine("[1] IL2CPP TYPE INFO");
            sb.AppendLine("-------------------------------------------------------------------------");
            sb.AppendLine($"Item type: {_itemType?.FullName ?? "NULL"}");
            sb.AppendLine($"ItemManager type: {_itemManagerType?.FullName ?? "NULL"}");
            sb.AppendLine($"GameItemContainer type: {_gameItemContainerType?.FullName ?? "NULL"}");
            sb.AppendLine($"CreateInstance() method: {(_createInstanceMethod != null ? "FOUND" : "NULL")}");
            sb.AppendLine($"RemoveItem() method: {(_removeItemMethod != null ? "FOUND" : "NULL")}");
            sb.AppendLine($"AllItemsReadonlyCollection() method: {(_getAllItemsMethod != null ? "FOUND" : "NULL")}");
            sb.AppendLine($"_prefab field: {(_prefabField != null ? "FOUND" : "NULL")}");
            sb.AppendLine($"GameId property: {(_gameIdProperty != null ? "FOUND" : "NULL")}");
            sb.AppendLine();
            sb.AppendLine("[2] REAL SINGLETON SEARCH (3 methods)");
            sb.AppendLine("-------------------------------------------------------------------------");
            object realManager = null;
            string foundMethod = "";
            if (_itemManagerType != null)
            {
                sb.AppendLine("Method 1: Static fields in ItemManager");
                try
                {
                    var staticFields = _itemManagerType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Static);
                    sb.AppendLine($"  Found {staticFields.Length} static fields:");
                    foreach (var field in staticFields)
                    {
                        try
                        {
                            sb.AppendLine($"    - {field.FieldType.Name} {field.Name}");
                            if (field.FieldType == _itemManagerType || field.FieldType.Name == "ItemManager" || (field.FieldType.FullName?.Contains("ItemManager") ?? false))
                            {
                                object value = field.GetValue(null);
                                if (value != null)
                                {
                                    realManager = value;
                                    foundMethod = $"Static field '{field.Name}'";
                                    sb.AppendLine($"      [!!!] FOUND REAL ITEM MANAGER!");
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            if (realManager == null)
            {
                sb.AppendLine("Method 2: GameObject component search");
                try
                {
                    var allObjects = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
                    int checkedCount = 0;
                    foreach (var go in allObjects)
                    {
                        if (go == null) continue;
                        checkedCount++;
                        try
                        {
                            var comp = go.GetComponent(_itemManagerType);
                            if (comp != null)
                            {
                                realManager = comp;
                                foundMethod = $"GameObject '{go.name}' at {GetAbsPath(go)}";
                                sb.AppendLine($"  [!!!] FOUND REAL ITEM MANAGER on GameObject '{go.name}'!");
                                break;
                            }
                        }
                        catch { }
                    }
                    sb.AppendLine($"  Checked {checkedCount} GameObjects");
                }
                catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            if (realManager == null)
            {
                sb.AppendLine("Method 3: SaveSingleton → SaveContainer chain");
                try
                {
                    var assemblies = Il2CppSystem.AppDomain.CurrentDomain.GetAssemblies();
                    Il2CppSystem.Type saveSingletonType = null;
                    foreach (var asm in assemblies)
                    {
                        try
                        {
                            saveSingletonType = asm.GetType("SaveSingleton") ?? asm.GetType("Save.SaveSingleton");
                            if (saveSingletonType != null) break;
                        }
                        catch { }
                    }
                    if (saveSingletonType != null)
                    {
                        sb.AppendLine($"  Found SaveSingleton: {saveSingletonType.FullName}");
                        var instanceField = saveSingletonType.GetField("Instance", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Static) ?? saveSingletonType.GetField("_instance", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Static);
                        if (instanceField != null)
                        {
                            object saveSingleton = instanceField.GetValue(null);
                            if (saveSingleton != null)
                            {
                                sb.AppendLine($"  [OK] Found SaveSingleton instance");
                                var saveContainerField = saveSingletonType.GetField("saveContainer", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance) ?? saveSingletonType.GetField("_saveContainer", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                                if (saveContainerField != null)
                                {
                                    object saveContainer = saveContainerField.GetValue((Il2CppSystem.Object)saveSingleton);
                                    if (saveContainer != null)
                                    {
                                        sb.AppendLine($"  [OK] Found SaveContainer");
                                        var containerType = saveContainer.GetType();
                                        var containerFields = containerType.GetFields((BindingFlags)(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance));
                                        foreach (var field in containerFields)
                                        {
                                            if (field.FieldType.FullName == _itemManagerType.FullName || field.FieldType.Name == "ItemManager")
                                            {
                                                object itemManager = field.GetValue(saveContainer);
                                                if (itemManager != null)
                                                {
                                                    realManager = itemManager;
                                                    foundMethod = $"SaveContainer.{field.Name}";
                                                    sb.AppendLine($"  [!!!] FOUND REAL ITEM MANAGER via SaveContainer.{field.Name}!");
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else { sb.AppendLine("  SaveSingleton type NOT FOUND"); }
                }
                catch (Exception ex) { sb.AppendLine($"  [ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            sb.AppendLine("=========================================================================");
            sb.AppendLine("[RESULT]");
            sb.AppendLine("=========================================================================");
            if (realManager != null)
            {
                sb.AppendLine($"✅ REAL ITEM MANAGER FOUND!");
                sb.AppendLine($"   Method: {foundMethod}");
                sb.AppendLine($"   Type: {realManager.GetType().FullName}");
            }
            else
            {
                sb.AppendLine("❌ REAL ITEM MANAGER NOT FOUND");
                sb.AppendLine("   All 3 search methods failed.");
                sb.AppendLine("   ItemManager may not be stored as a singleton.");
            }
            sb.AppendLine("=========================================================================\n");
            if (_itemManagerType != null)
            {
                sb.AppendLine("[3] ALL METHODS OF ITEMMANAGER");
                sb.AppendLine("-------------------------------------------------------------------------");
                try
                {
                    var methods = _itemManagerType.GetMethods(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                    int count = 0;
                    foreach (var m in methods)
                    {
                        try
                        {
                            count++;
                            var pars = m.GetParameters();
                            string parStr = "";
                            foreach (var p in pars) { parStr += (parStr.Length > 0 ? ", " : "") + p.ParameterType.Name + " " + p.Name; }
                            string staticStr = m.IsStatic ? "[static] " : "";
                            sb.AppendLine($"  {count}. {staticStr}{m.ReturnType.Name} {m.Name}({parStr})");
                        }
                        catch { }
                    }
                    sb.AppendLine($"Total methods: {count}");
                }
                catch (Exception ex) { sb.AppendLine($"[ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            if (_itemType != null)
            {
                sb.AppendLine("[4] ALL FIELDS OF ITEM (including backing fields)");
                sb.AppendLine("-------------------------------------------------------------------------");
                try
                {
                    var fields = _itemType.GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static);
                    int count = 0;
                    foreach (var f in fields)
                    {
                        count++;
                        string staticStr = f.IsStatic ? "[static] " : "";
                        sb.AppendLine($"  {count}. {staticStr}{f.FieldType.Name} {f.Name}");
                        if (count >= 100) { sb.AppendLine($"  ... (truncated at 100)"); break; }
                    }
                }
                catch (Exception ex) { sb.AppendLine($"[ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            if (_itemType != null)
            {
                sb.AppendLine("[5] ALL PROPERTIES OF ITEM");
                sb.AppendLine("-------------------------------------------------------------------------");
                try
                {
                    var props = _itemType.GetProperties(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                    int count = 0;
                    foreach (var p in props)
                    {
                        count++;
                        sb.AppendLine($"  {count}. {p.PropertyType.Name} {p.Name}");
                        if (count >= 100) break;
                    }
                }
                catch (Exception ex) { sb.AppendLine($"[ERROR] {ex.Message}"); }
                sb.AppendLine();
            }
            sb.AppendLine("[6] LIVE STATS (attempt to read items)");
            sb.AppendLine("-------------------------------------------------------------------------");
            if (_createInstanceMethod != null && _getAllItemsMethod != null)
            {
                try
                {
                    var mgr = _createInstanceMethod.Invoke(null, null);
                    if (mgr != null)
                    {
                        var collection = _getAllItemsMethod.Invoke((Il2CppSystem.Object)mgr, new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(0));
                        int count = 0;
                        int missing = 0;
                        if (collection is System.Collections.IEnumerable items)
                        {
                            foreach (var obj in items)
                            {
                                count++;
                                if (obj != null)
                                {
                                    if (_prefabField != null)
                                    {
                                        try
                                        {
                                            var prefab = _prefabField.GetValue((Il2CppSystem.Object)obj);
                                            if (prefab == null) missing++;
                                            else
                                            {
                                                string prefabStr = prefab.ToString() ?? "";
                                                if (prefabStr.Contains("IsMissingItem : True", StringComparison.OrdinalIgnoreCase) || prefabStr.Contains("Name : Missing Item", StringComparison.OrdinalIgnoreCase)) missing++;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        sb.AppendLine($"Total items in CreateInstance() manager: {count}");
                        sb.AppendLine($"Missing items detected: {missing}");
                        sb.AppendLine();
                        sb.AppendLine("NOTE: If count=0, CreateInstance() creates EMPTY manager.");
                        sb.AppendLine("      Real items are stored in singleton found via Method 1/2/3.");
                    }
                    else { sb.AppendLine("CreateInstance() returned NULL"); }
                }
                catch (Exception ex) { sb.AppendLine($"[ERROR] {ex.Message}"); }
            }
            else { sb.AppendLine("Required methods not found"); }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            MelonLogger.Msg("[OK] Folder 13: ItemManager Debug dump complete.");
        }

        private void DumpPngVram()
        {
            int saved = 0;
            var sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
            var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in sprites)
            {
                if (sp == null || string.IsNullOrEmpty(sp.name) || sp.texture == null) continue;
                if (sp.name.StartsWith("Unity") || sp.name.Contains("UISprite")) continue;
                if (done.Contains(sp.name)) continue;
                done.Add(sp.name);
                try
                {
                    Rect r = sp.textureRect;
                    int w = Mathf.RoundToInt(r.width);
                    int h = Mathf.RoundToInt(r.height);
                    if (w <= 0 || h <= 0) continue;
                    string safe = sp.name.Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                    string fp = Path.Combine(f1Png, safe + "_" + w + "x" + h + ".png");
                    RenderTexture rt = RenderTexture.GetTemporary(sp.texture.width, sp.texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                    Graphics.Blit(sp.texture, rt);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), w, h), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    byte[] data = ImageConversion.EncodeToPNG(tex);
                    if (data != null && data.Length > 0)
                    {
                        File.WriteAllBytes(fp, data);
                        saved++;
                    }
                    UnityEngine.Object.Destroy(tex);
                }
                catch { }
            }
            MelonLogger.Msg("[OK] Folder 1: " + saved + " PNG files saved.");
        }
    }
}
