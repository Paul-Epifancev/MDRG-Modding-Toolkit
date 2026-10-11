// =========================================================================
// MDRG Runtime Texture Injector — Professional Edition
// =========================================================================
// Author: Paul.Epifancev (#warlockforbugs)
// Discord: anon.jun
// Version: 1.0.4 (Release)
// 
// [EN] A professional-grade runtime texture injection system for MDRG.
//      Provides a clean GUI interface (F9) for replacing 2D sprites in 
//      the player's room and isolated scenes without modifying game files.
//      This is a DEBUG & PREVIEW tool — changes are applied in real-time
//      and reset when you exit the scene.
//
// [RU] Профессиональная система инъекции текстур в рантайме для MDRG.
//      Предоставляет чистый GUI-интерфейс (F9) для замены 2D-спрайтов
//      в комнате игрока и изолированных сценах без модификации файлов игры.
//      Это ОТЛАДОЧНЫЙ ИНСТРУМЕНТ — изменения применяются в реальном времени
//      и сбрасываются при выходе из сцены.
// =========================================================================

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MDRG_RuntimeTextureInjector.MainModClass), "MDRG Runtime Texture Injector", "1.0.4", "Paul.Epifancev #warlockforbugs (Discord: anon.jun)")]
[assembly: MelonGame(null, null)]

namespace MDRG_RuntimeTextureInjector
{
    public class MainModClass : MelonMod
    {
        private const int VK_F9 = 0x78;
        private static bool isExplorerOnline = false;
        private static bool showGuiWindow = false;
        private bool f9WasPressed = false;
        
        // [EN] 21 room nodes — relative paths for Transform.Find() lookup
        // [RU] 21 нод комнаты — относительные пути для поиска через Transform.Find()
        private static readonly string[] roomAtlasCells = new string[]
        {
            "back", "Bed", "couch", "Cupboard",
            "PC/PC desk", "PC/PC", "PC/Screen light", "PC/PC rgb",
            "TV/TV", "TV/TV light",
            "Window", "Light",
            "Morning Light", "Light Multiply",
            "Door", "Bathroom Door/Door", "Bathroom Door/Bathroom Door Light",
            "GhostBackground", "BatBanner", "SpiderPlush", "mat"
        };
        
        private GameObject[] cachedCellObjects = new GameObject[roomAtlasCells.Length];
        
        // Isolated scenes / Изолированные сцены
        private GameObject showerWallGo = null;
        private GameObject showerBackGo = null;
        private GameObject showerPropsGo = null;
        private GameObject showerZadForegroundGo = null;
        private GameObject cuddleBgGo = null;
        private GameObject cuddleBedGo = null;
        private GameObject cuddleLightGo = null;
        private GameObject missionaryBgGo = null;
        private GameObject missionaryLightGo = null;
        private GameObject analieBgGo = null;
        private GameObject analieBlinkGo = null;
        private GameObject analieDecorGo = null;
        private GameObject analieForeGo = null;
        private GameObject analieCatGo = null;
        private GameObject analieChangingBgGo = null;
        private GameObject melissaBgGo = null;
        private GameObject melissaForeGo = null;
        
        // [EN] Dynamic View Scenes — detected by keywords, labels are generic
        // [RU] Динамические сцены — обнаруживаются по ключевым словам, метки универсальные
        private GameObject view1Go = null;
        private GameObject view2Go = null;
        private GameObject view3Go = null;
        private GameObject view4Go = null;
        private GameObject view5Go = null;
        
        private UnityEngine.Canvas masterCanvasComponent = null;
        private GameObject selectedInjectionTarget = null;
        private string selectedTargetNameForUI = "NONE (Click a target below)";
        
        private Rect windowLayout = new Rect(30, 30, 550, 850);
        private static readonly List<string> screenLogBuffer = new List<string>();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int lStructSize = Marshal.SizeOf(typeof(OpenFileName));
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr hInstance = IntPtr.Zero;
            public string lpstrFilter = null;
            public string lpstrCustomFilter = null;
            public int nMaxCustFilter = 0;
            public int nFilterIndex = 0;
            public string lpstrFile = null;
            public int nMaxFile = 0;
            public string lpstrFileTitle = null;
            public int nMaxFileTitle = 0;
            public string lpstrInitialDir = null;
            public string lpstrTitle = null;
            public int Flags = 0;
            public short nFileOffset = 0;
            public short nFileExtension = 0;
            public string lpstrDefExt = null;
            public IntPtr lCustData = IntPtr.Zero;
            public IntPtr lpfnHook = IntPtr.Zero;
            public string lpTemplateName = null;
            public IntPtr pvReserved = IntPtr.Zero;
            public int dwReserved = 0;
            public int FlagsEx = 0;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("[MDRG Runtime Texture Injector] v1.0.4 Online. Professional Edition.");
            PushHudLog("SYSTEM RUNTIME SHIELD ONLINE.");
        }

        public static void PushHudLog(string message)
        {
            screenLogBuffer.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!isExplorerOnline)
            {
                isExplorerOnline = true;
                LoggerInstance.Msg("[MDRG Runtime Texture Injector] Matrix Synced. Press F9 to toggle controls.");
            }
            masterCanvasComponent = null;
            selectedInjectionTarget = null;
            selectedTargetNameForUI = "NONE (Click a target below)";
            ScanMainSceneAndSyncPipeline();
            PushHudLog($"SCENE_EVENT: Entered scene '{sceneName}'.");
        }

        public override void OnUpdate()
        {
            short f9State = GetAsyncKeyState(VK_F9);
            if ((f9State & 0x8000) != 0)
            {
                if (!f9WasPressed)
                {
                    f9WasPressed = true;
                    showGuiWindow = !showGuiWindow;
                    if (showGuiWindow)
                    {
                        ScanMainSceneAndSyncPipeline();
                    }
                }
            }
            else
            {
                f9WasPressed = false;
            }
        }

        private string GetGameObjectAbsolutePath(GameObject go)
        {
            if (go == null) return "";
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null) 
            { 
                if (parent.name == "Views" || parent.name == "Canvas") break; 
                path = parent.name + "/" + path; 
                parent = parent.transform.parent; 
            }
            return "/" + path.Replace("Views/", "");
        }

        // [EN] Dynamic scene detection via keyword matching
        // [RU] Динамическое обнаружение сцен по совпадению ключевых слов
        private GameObject FindViewScene(params string[] keywords)
        {
            var allGo = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go == null || !go.activeInHierarchy) continue;
                string name = go.name.ToLower();
                string path = GetGameObjectAbsolutePath(go).ToLower();
                
                int matches = 0;
                foreach (var kw in keywords)
                {
                    if (name.Contains(kw.ToLower()) || path.Contains(kw.ToLower()))
                    {
                        matches++;
                    }
                }
                
                if (matches >= 1 && go.GetComponentInChildren<SpriteRenderer>() != null)
                {
                    return go;
                }
            }
            return null;
        }

        private void ScanMainSceneAndSyncPipeline()
        {
            GameObject canvasGo = GameObject.Find("/Canvas") ?? GameObject.Find("Canvas");
            if (canvasGo != null)
            {
                Component comp = canvasGo.GetComponent(Il2CppType.Of<UnityEngine.Canvas>());
                if (comp != null) masterCanvasComponent = comp.TryCast<UnityEngine.Canvas>();
            }
            
            GameObject roomValidation = GameObject.Find("/Room") ?? GameObject.Find("Room");
            if (roomValidation != null)
            {
                for (int i = 0; i < roomAtlasCells.Length; i++)
                {
                    Transform tr = roomValidation.transform.Find(roomAtlasCells[i]);
                    cachedCellObjects[i] = (tr != null) ? tr.gameObject : null;
                }
            }
            
            // Isolated scenes / Изолированные сцены
            showerWallGo = GameObject.Find("/Views/Shower Scene/Center/Shower Wall") ?? GameObject.Find("Shower Wall");
            showerBackGo = GameObject.Find("/Views/Shower Scene/Back/Shower Back") ?? GameObject.Find("Shower Back");
            showerPropsGo = GameObject.Find("/Views/Shower Scene/Back/Shower Back Props") ?? GameObject.Find("Shower Back Props");
            showerZadForegroundGo = GameObject.Find("/Views/Shower Scene/Front/Foreground") ?? GameObject.Find("Foreground") ?? GameObject.Find("/Views/Shower Scene/Front/ZadForeground");
            cuddleBgGo = GameObject.Find("/Views/Cuddle Scene/Cuddle Background") ?? GameObject.Find("Cuddle Background");
            cuddleBedGo = GameObject.Find("/Views/Cuddle Scene/Cuddle Bed") ?? GameObject.Find("Cuddle Bed");
            cuddleLightGo = GameObject.Find("/Views/Cuddle Scene/Light Multiply") ?? GameObject.Find("Light Multiply");
            missionaryBgGo = GameObject.Find("/Missionary Image") ?? GameObject.Find("Missionary Image");
            missionaryLightGo = GameObject.Find("/Light Multiply") ?? GameObject.Find("Light Multiply");
            analieBgGo = GameObject.Find("/Views/Clothier Scene/Background") ?? GameObject.Find("/Views/Shop Scene/Background");
            analieBlinkGo = GameObject.Find("/Views/Clothier Scene/Blink");
            analieDecorGo = GameObject.Find("/Views/Clothier Scene/Halloween Decoration");
            analieForeGo = GameObject.Find("/Views/Clothier Scene/Foreground");
            analieCatGo = GameObject.Find("/Views/Clothier Scene/Halloween Cat");
            analieChangingBgGo = GameObject.Find("/Views/Changing Room Scene/Background");
            melissaBgGo = GameObject.Find("/Views/Shop Scene/Background");
            melissaForeGo = GameObject.Find("/Views/Shop Scene/Foreground");
            
            // [EN] Dynamic View Scenes — detected by keywords
            // [RU] Динамические сцены — обнаруживаются по ключевым словам
            view1Go = FindViewScene("Scene 1", "Microplastic", "Beach", "Image 1");
            view2Go = FindViewScene("Scene 2", "Temple", "Rich", "Image 2");
            view3Go = FindViewScene("Lobby", "FirstFloor", "Launch", "hdasiuo");
            view4Go = FindViewScene("Apartment", "hdasiuo", "BigDaddyHurt", "Room");
            view5Go = FindViewScene("Shop", "BigDaddyHurt", "ladyparts");
        }

        public override void OnGUI()
        {
            if (!showGuiWindow) return;
            windowLayout = GUI.Window(150, windowLayout, (GUI.WindowFunction)RenderInspectorControls, "MDRG Runtime Texture Injector v1.0.4");
        }

        private void RenderInspectorControls(int windowID)
        {
            GUI.Label(new Rect(20, 25, 480, 20), "Display Resolution: " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
            int currentY = 50;
            
            // =================================================================
            // MASTER CANVAS MANAGER / МЕНЕДЖЕР ГЛАВНОГО CANVAS
            // =================================================================
            GUI.Box(new Rect(15, currentY, 520, 65), "MASTER CANVAS MANAGER (UI_HIDER PIPELINE)");
            if (masterCanvasComponent != null)
            {
                bool isUiRendering = masterCanvasComponent.enabled;
                GUI.Label(new Rect(25, currentY + 20, 450, 20), "Master Canvas Status: " + (isUiRendering ? "ACTIVE" : "HIDDEN (HUD Toggle OK)"));
                if (GUI.Button(new Rect(25, currentY + 40, 500, 20), isUiRendering ? "ENGAGE HUD SUPPRESSION (HIDE)" : "RESTORE HUD RENDERING (SHOW)"))
                {
                    masterCanvasComponent.enabled = !isUiRendering;
                    PushHudLog($"UI_HIDER: Forced to {!isUiRendering}");
                }
            }
            currentY += 75;
            
            // =================================================================
            // UNIVERSAL INJECTOR / УНИВЕРСАЛЬНЫЙ ИНЖЕКТОР
            // =================================================================
            GUI.Box(new Rect(15, currentY, 520, 75), "UNIVERSAL RUNTIME TEXTURE INJECTOR (WIN32 API)");
            GUI.Label(new Rect(25, currentY + 20, 500, 20), "Target: " + selectedTargetNameForUI);
            if (GUI.Button(new Rect(25, currentY + 40, 500, 25), "OPEN WINDOWS EXPLORER & INJECT PNG TO TARGET"))
            {
                if (selectedInjectionTarget != null) OpenFileViaWin32API(selectedInjectionTarget);
            }
            currentY += 85;
            
            // =================================================================
            // SCENE BLOCKS — MUTUALLY EXCLUSIVE CHAIN
            // БЛОКИ СЦЕН — ВЗАИМОИСКЛЮЧАЮЩАЯ ЦЕПОЧКА
            // =================================================================
            // [EN] Correct order: Room → Clothier Shop → Changing Room → 
            //      Melissa Shop → Shower → Cuddle → Missionary → Dynamic View
            // [RU] Правильный порядок: Комната → Магазин Аннали → Примерочная →
            //      Магазин Мелиссы → Душ → Обнимашки → Миссионерская → Dynamic View
            // =================================================================
            
            GameObject roomValidation = GameObject.Find("/Room") ?? GameObject.Find("Room");
            
            // BLOCK 1: Room Scene (21 nodes) / БЛОК 1: Комната (21 нода)
            if (roomValidation != null && roomValidation.activeInHierarchy)
            {
                GUI.Box(new Rect(15, currentY, 520, 620), "ROOM SCENE — 21 NODES (Room Manager)");
                int slotY = currentY + 25;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── BACKGROUND & FURNITURE ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "back")) DirectSelectCell(0);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "Bed")) DirectSelectCell(1);
                if (GUI.Button(new Rect(345, slotY, 160, 22), "couch")) DirectSelectCell(2);
                slotY += 28;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Cupboard")) DirectSelectCell(3);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── PC ZONE ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "PC desk")) DirectSelectCell(4);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "PC")) DirectSelectCell(5);
                if (GUI.Button(new Rect(345, slotY, 160, 22), "PC rgb")) DirectSelectCell(7);
                slotY += 28;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Screen light")) DirectSelectCell(6);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── TV ZONE ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "TV")) DirectSelectCell(8);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "TV light")) DirectSelectCell(9);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── WINDOW & LIGHT ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Window")) DirectSelectCell(10);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "Window Light")) DirectSelectCell(11);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── LIGHTING SYSTEM ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Morning Light")) DirectSelectCell(12);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "Light Multiply")) DirectSelectCell(13);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── DOORS ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Door")) DirectSelectCell(14);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "Bathroom Door")) DirectSelectCell(15);
                if (GUI.Button(new Rect(345, slotY, 160, 22), "Bath.Door Light")) DirectSelectCell(16);
                slotY += 34;
                
                GUI.Label(new Rect(25, slotY, 500, 18), "─── DECORATION ───");
                slotY += 22;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "Ghost BG")) DirectSelectCell(17);
                if (GUI.Button(new Rect(185, slotY, 150, 22), "Bat Banner")) DirectSelectCell(18);
                if (GUI.Button(new Rect(345, slotY, 160, 22), "Spider Plush")) DirectSelectCell(19);
                slotY += 28;
                if (GUI.Button(new Rect(25, slotY, 150, 22), "mat")) DirectSelectCell(20);
                
                currentY += 630;
            }
            // BLOCK 2: Annalie Clothier Shop / БЛОК 2: Магазин Аннали
            else if (analieBlinkGo != null || analieCatGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 95), "ANNALIE CLOTHIER SHOP LAYER Overrides");
                int anY = currentY + 25;
                if (GUI.Button(new Rect(25, anY, 150, 25), "Annalie Background")) SetTargetNodeDirect(analieBgGo, "Background");
                if (GUI.Button(new Rect(185, anY, 150, 25), "Annalie Blink")) SetTargetNodeDirect(analieBlinkGo, "Blink");
                if (GUI.Button(new Rect(345, anY, 160, 25), "Annalie Decoration")) SetTargetNodeDirect(analieDecorGo, "Halloween Decoration");
                if (GUI.Button(new Rect(25, anY + 32, 230, 25), "Annalie Foreground")) SetTargetNodeDirect(analieForeGo, "Foreground");
                if (GUI.Button(new Rect(265, anY + 32, 240, 25), "Annalie Cat")) SetTargetNodeDirect(analieCatGo, "Halloween Cat");
                currentY += 105;
            }
            // BLOCK 3: Annalie Changing Room / БЛОК 3: Примерочная Аннали
            else if (analieChangingBgGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 75), "ANNALIE CHANGING ROOM OVERRIDE");
                if (GUI.Button(new Rect(25, currentY + 25, 500, 30), "Changing Room Background")) SetTargetNodeDirect(analieChangingBgGo, "Background");
                currentY += 85;
            }
            // BLOCK 4: Melissa Grocery Shop / БЛОК 4: Магазин Мелиссы
            else if (melissaBgGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 95), "MELISSA GROCERY SHOP LAYER Overrides");
                int melY = currentY + 25;
                if (GUI.Button(new Rect(25, melY, 240, 25), "Melissa Shop Background")) SetTargetNodeDirect(melissaBgGo, "Background");
                if (GUI.Button(new Rect(275, melY, 240, 25), "Melissa Cashier Counter")) SetTargetNodeDirect(melissaForeGo, "Foreground");
                currentY += 105;
            }
            // BLOCK 5: Shower Scene / БЛОК 5: Сцена душа
            else if (showerBackGo != null || showerWallGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 95), "SHOWER SCENE LAYER SELECTORS");
                int shY = currentY + 25;
                if (GUI.Button(new Rect(25, shY, 240, 25), "Shower Wall")) SetTargetNodeDirect(showerWallGo, "Shower Wall");
                if (GUI.Button(new Rect(275, shY, 240, 25), "Shower Back")) SetTargetNodeDirect(showerBackGo, "Shower Back");
                if (GUI.Button(new Rect(25, shY + 32, 240, 25), "Shower Props")) SetTargetNodeDirect(showerPropsGo, "Shower Back Props");
                if (GUI.Button(new Rect(275, shY + 32, 240, 25), "Foreground")) SetTargetNodeDirect(showerZadForegroundGo, "Foreground");
                currentY += 105;
            }
            // BLOCK 6: Cuddle Scene / БЛОК 6: Сцена обнимашек
            else if (cuddleBgGo != null || cuddleBedGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 95), "CUDDLE SCENE LAYER INJECTORS");
                int cuY = currentY + 25;
                if (GUI.Button(new Rect(25, cuY, 150, 25), "Cuddle Background")) SetTargetNodeDirect(cuddleBgGo, "Cuddle Background");
                if (GUI.Button(new Rect(185, cuY, 150, 25), "Cuddle Bed")) SetTargetNodeDirect(cuddleBedGo, "Cuddle Bed");
                if (GUI.Button(new Rect(345, cuY, 160, 25), "Light Multiply")) SetTargetNodeDirect(cuddleLightGo, "Light Multiply");
                currentY += 105;
            }
            // BLOCK 7: Missionary Scene / БЛОК 7: Миссионерская сцена
            else if (missionaryBgGo != null || missionaryLightGo != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 95), "MISSIONARY POSE LAYER INJECTORS");
                int miY = currentY + 25;
                if (GUI.Button(new Rect(25, miY, 240, 25), "Missionary Image")) SetTargetNodeDirect(missionaryBgGo, "Missionary Image");
                if (GUI.Button(new Rect(275, miY, 240, 25), "Light Multiply")) SetTargetNodeDirect(missionaryLightGo, "Light Multiply");
                currentY += 105;
            }
            // BLOCK 8: Dynamic View Scenes (LAST FALLBACK) / БЛОК 8: Dynamic View (ПОСЛЕДНИЙ FALLBACK)
            // [EN] These are scenes without fixed node paths, detected by keywords.
            //      Labels are generic (View 1, View 2, etc.) — no specific location names.
            // [RU] Это сцены без фиксированных путей к нодам, обнаруживаются по ключевым словам.
            //      Метки универсальные (View 1, View 2 и т.д.) — без конкретных названий локаций.
            else if (view1Go != null || view2Go != null || view3Go != null || view4Go != null || view5Go != null)
            {
                GUI.Box(new Rect(15, currentY, 520, 120), "DYNAMIC VIEW SCENES (AUTO-DETECT)");
                int vY = currentY + 25;
                
                if (view1Go != null && GUI.Button(new Rect(25, vY, 230, 25), "View 1")) SetTargetNodeDirect(view1Go, "View 1");
                if (view2Go != null && GUI.Button(new Rect(265, vY, 240, 25), "View 2")) SetTargetNodeDirect(view2Go, "View 2");
                
                vY += 32;
                if (view3Go != null && GUI.Button(new Rect(25, vY, 230, 25), "View 3")) SetTargetNodeDirect(view3Go, "View 3");
                if (view4Go != null && GUI.Button(new Rect(265, vY, 240, 25), "View 4")) SetTargetNodeDirect(view4Go, "View 4");
                
                vY += 32;
                if (view5Go != null && GUI.Button(new Rect(25, vY, 480, 25), "View 5")) SetTargetNodeDirect(view5Go, "View 5");
                
                currentY += 130;
            }

            // =================================================================
            // CLOSE BUTTON — DYNAMIC POSITIONING
            // КНОПКА ЗАКРЫТИЯ — ДИНАМИЧЕСКОЕ ПОЗИЦИОНИРОВАНИЕ
            // =================================================================
            int closeButtonY = currentY + 15;
            if (GUI.Button(new Rect(15, closeButtonY, 520, 35), "CLOSE SDK INSPECTOR")) 
            { 
                showGuiWindow = false; 
            }
            
            // [EN] Dynamic window height based on content
            // [RU] Динамическая высота окна в зависимости от контента
            windowLayout.height = closeButtonY + 55;
            
            GUI.DragWindow(new Rect(0, 0, windowLayout.width, windowLayout.height));
        }

        private void DirectSelectCell(int index)
        {
            GameObject cellGo = cachedCellObjects[index];
            string cellName = roomAtlasCells[index].Contains("/") 
                ? roomAtlasCells[index].Substring(roomAtlasCells[index].LastIndexOf('/') + 1) 
                : roomAtlasCells[index];
                
            if (cellGo != null)
            {
                selectedInjectionTarget = cellGo;
                selectedTargetNameForUI = cellName + " [READY]";
                PushHudLog($"[RAM RADAR] Target locked -> Node: '{cellName}'");
            }
            else
            {
                selectedTargetNameForUI = cellName + " [SLOT NOT ACTIVE]";
            }
        }

        private void SetTargetNodeDirect(GameObject go, string name)
        {
            if (go != null)
            {
                selectedInjectionTarget = go;
                selectedTargetNameForUI = name + " [READY]";
                PushHudLog($"[RAM RADAR] Target locked -> View Node: '{name}'");
            }
            else
            {
                selectedTargetNameForUI = name + " [NODE NOT FOUND]";
            }
        }

        private void OpenFileViaWin32API(GameObject targetGo)
        {
            if (targetGo == null) return;
            try
            {
                OpenFileName ofn = new OpenFileName();
                ofn.lpstrFilter = "PNG Image Files (*.png)\0*.png\0All Files (*.*)\0*.*\0";
                ofn.lpstrFile = new string(new char[256]);
                ofn.nMaxFile = ofn.lpstrFile.Length;
                ofn.lpstrFileTitle = new string(new char[256]);
                ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
                ofn.lpstrTitle = "Select Custom PNG Texture";
                ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800;
                if (GetOpenFileName(ofn))
                {
                    string filePath = ofn.lpstrFile.Trim('\0');
                    if (File.Exists(filePath))
                    {
                        PushHudLog($"[WIN32 BRIDGE] Selected -> '{Path.GetFileName(filePath)}'");
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        InjectTextureToComponent(targetGo, fileBytes);
                    }
                }
            }
            catch (Exception ex) { PushHudLog($"[ERR] Win32 bridge collapsed: {ex.Message}"); }
        }

        private void InjectTextureToComponent(GameObject targetGo, byte[] fileBytes)
        {
            if (targetGo == null || fileBytes == null) return;
            Texture2D runtimeTex = new Texture2D(2, 2);
            ImageConversion.LoadImage(runtimeTex, fileBytes);
            float targetPpu = 100f;
            if (targetGo.name.Contains("Cuddle Bed") || targetGo.name.Contains("Sleep")) targetPpu = 99.67949f;
            Sprite runtimeSprite = Sprite.Create(runtimeTex, new Rect(0f, 0f, runtimeTex.width, runtimeTex.height), new Vector2(0.5f, 0.5f), targetPpu);
            runtimeSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            
            Component rawRenderer = targetGo.GetComponentInChildren(Il2CppType.Of<SpriteRenderer>()) ?? targetGo.GetComponent(Il2CppType.Of<SpriteRenderer>());
            if (rawRenderer != null)
            {
                SpriteRenderer sr = rawRenderer.TryCast<SpriteRenderer>();
                if (sr != null) { sr.color = Color.white; sr.sprite = runtimeSprite; PushHudLog($"[VRAM SUCCESS] Injected -> '{targetGo.name}'"); return; }
            }
            
            Component uiComp = targetGo.GetComponentInChildren((Il2CppSystem.Type)"Image") ?? targetGo.GetComponent("Image");
            if (uiComp != null)
            {
                try {
                    var spriteProp = uiComp.GetType().GetProperty("sprite");
                    if (spriteProp != null) {
                        spriteProp.SetValue(uiComp, runtimeSprite, null);
                        PushHudLog($"[UI UNMANAGED SUCCESS] Injected -> '{targetGo.name}'");
                    }
                } catch { }
            }
        }
    }
}
