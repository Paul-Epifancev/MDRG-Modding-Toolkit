Created by Paul.Epifancev #warlockforbugs (Discord: anon.jun)
=========================================================================
CORE MANAGERS RUNTIME PASSPORT
=========================================================================
Component Type: TextureOverriderManager
Purpose: Manage sprite overrides for scene-specific nodes
Usage in Lua: TextureOverriderManager.GetTextureOverriderManager("ID")
=========================================================================
INSTANCE #1 — FULL NODE ATLAS (21 ROOM NODES)

Manager ID     : "Room"
Absolute Path  : /Room Scene
Lua API        : TextureOverriderManager.RoomManager.SetSprite(PATH, sprite)
                 TextureOverriderManager.RoomManager.RestoreSprite(PATH, sprite)
=========================================================================

 #  | Category            | Node Name             | Path                                          | Original Size | Notes
----|---------------------|-----------------------|-----------------------------------------------|---------------|-----------------------------------------------------------------
 1  | Background          | back                  | /Room/back                                    | 1920x1080     | Main room background
 2  | Furniture           | Bed                   | /Room/Bed                                     | varies        |
 3  | Furniture           | couch                 | /Room/couch                                   | varies        |
 4  | Furniture           | Cupboard              | /Room/Cupboard                                | varies        |
 5  | Furniture           | PC desk               | /Room/PC/PC desk                              | varies        |
 6  | Tech                | PC                    | /Room/PC/PC                                   | varies        | Monitor
 7  | Tech                | Screen light          | /Room/PC/Screen light                         | varies        | PC screen glow
 8  | Tech                | PC rgb                | /Room/PC/PC rgb                               | varies        | RGB LED strip
 9  | Tech                | TV                    | /Room/TV/TV                                   | varies        |
10  | Tech                | TV light              | /Room/TV/TV light                             | varies        | 
11  | Windows & Lighting  | Window                | /Room/Window                                  | varies        | Main window sprite
12  | Windows & Lighting  | Light                 | /Room/Light                                   | small rect    | Window light — sub-texture of Window, a small rectangular beam piercing through the window
13  | Windows & Lighting  | Morning Light         | /Room/Morning Light                           | varies        | TIME-LOCKED NODE (see Lighting Mechanics below)
14  | Windows & Lighting  | Light Multiply        | /Room/Light Multiply                          | varies        | MASTER LIGHT (see Lighting Mechanics below)
15  | Windows & Lighting  | Bathroom Door Light   | /Room/Bathroom Door/Bathroom Door Light       | 110x1148      | Vertical light beam overlay; visible from the player's room when the bot Jun is showering alone
16  | Doors               | Door                  | /Room/Door                                    | varies        | Main entrance door
17  | Doors               | Bathroom Door         | /Room/Bathroom Door/Door                      | 126x1263      | Tall vertical door sprite (unusual aspect ratio ~1:10)
18  | Decor               | GhostBackground       | /Room/GhostBackground                         | varies        | Ghost decoration
19  | Decor               | BatBanner             | /Room/BatBanner                               | varies        | Bat banner
20  | Decor               | SpiderPlush           | /Room/SpiderPlush                             | varies        | Spider plush toy
21  | Decor               | mat                   | /Room/mat                                     | varies        | Rug

=========================================================================
LIGHTING SYSTEM MECHANICS (DEEP ENGINE ANALYSIS)
=========================================================================

[1] MORNING LIGHT — TIME-LOCKED NODE
    The "Morning Light" node is bound to the game's internal time-of-day cycle.
    The engine activates this sprite ONLY during morning hours (approx. 06:00–12:00).
    ⚠️ CRITICAL BEHAVIOR: During day or night, the item remains equipped but has 
    ZERO visual effect. This is NOT a bug — it is hardcoded native game behavior.

[2] WINDOW LIGHT (NODE "Light") — SUB-TEXTURE OVERLAY
    The "Light" node (/Room/Light) is a sub-texture of the "Window" sprite.
    It renders as a small rectangular light beam that visually "pierces" through
    the main Window sprite, simulating sunlight breaking into the room.

[3] LIGHT MULTIPLY — MASTER LIGHT CONTROLLER
    The "Light Multiply" node is the MASTER LIGHT that governs ALL other lighting
    sources in the room, EXCEPT Morning Light (which is time-locked by the engine).
    
    🔓 BYPASS METHOD (SIMPLE & ELEGANT):
    To bypass the Morning Light time-lock and achieve uniform lighting 24/7:
    1. Take your custom light PNG (the one you want to see as the room's light).
    2. Make TWO copies of it.
    3. Rename the first copy to "LightMultiply.png".
    4. Rename the second copy to "MorningLight.png".
    5. Buy BOTH items in Annalie's Clothier shop and equip BOTH.
    
    Result: The game applies the SAME custom texture as both the master light 
    AND the morning light. The time-lock is effectively neutralized, providing 
    consistent lighting 24 hours a day. No engine tricks, no alpha textures — 
    just using the game's own logic by feeding it the same texture through two slots.

=========================================================================
NOTABLE SIZES & ASPECT RATIOS
=========================================================================

• Bathroom Door (126x1263) — Exceptionally tall vertical sprite. Aspect ratio 
  is approximately 1:10, which is unusual for Unity sprites. This is the 
  original game asset size, not a modding error.

• Bathroom Door Light (110x1148) — Slightly narrower and shorter than the door. 
  This is the light beam overlay that sits ON TOP of the door sprite. It is 
  visible from the player's own room (NOT from a separate shower scene) whenever 
  the bot Jun is showering alone. In all other game states, this node exists 
  in the hierarchy but is not rendered.

=========================================================================
INTEGRATION NOTES
=========================================================================
• All 21 nodes are handled exclusively via Lua API (TextureOverriderManager.RoomManager).
• All 21 scripts include the AddGenericItemToShop('clothier', itemX) call.
• The Lightswitch (LightswitchOn) is NOT included in this 21-node list. It is a 
  UI element (UnityEngine.UI.Image) and requires direct C# DLL injection via 
  MelonLoader, as TextureOverriderManager does not support Canvas UI sprites.
=========================================================================
_________________________________________________________________________

Instance #2: Lady Annalie Clothier Shop — Fitting Room Isolated Scene
Component Type   : TextureOverriderManager
Object Name      : "Changing Room Scene" - Lady Annalie Fitting Room (Isolated Scene)
Absolute Path    : /Changing Room Scene
REVEALED MGR ID  : ""
Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager("")
                   mgr:SetSprite(CurrentModGuid, "PATH", customSprite)
                   mgr:RestoreSprite(CurrentModGuid, "PATH", customSprite)

[EN] This mod modifies the specific scene taking place inside Lady Annalie's Clothier Shop.
[EN] It focuses exclusively on the Fitting Room area.
[EN] Please note that this is an isolated scene run by its own dedicated manager context.

do
    local POSSIBLE_IDS = { "", "" }
    local PATH_BACKGROUND = "/Background"
    local function ExecutionPipeline()
        local custom_bg = ModUtilities.GetAsSprite(CurrentModGuid, "AnnalieChangingRoom.png")
        while true do
            local intimacyManager = nil
            for _, id in ipairs(POSSIBLE_IDS) do
                local mgr = TextureOverriderManager.GetTextureOverriderManager(id)
                if mgr ~= nil then
                    intimacyManager = mgr
                    break
                end
            end
            if intimacyManager ~= nil then
                if custom_bg ~= nil then
                    intimacyManager:SetSprite(CurrentModGuid, PATH_BACKGROUND, custom_bg)
                end
                custom_bg = nil
                break
            end
            coroutine.yield()
        end
    end
    local co = coroutine.create(ExecutionPipeline)
    coroutine.resume(co)
end
_________________________________________________________________________

Instance #3: Shop Melissa Grocery
Component Type   : TextureOverriderManager
Object Name      : "Shop Scene" - Melissa Grocery (Isolated Scene)
Absolute Path    : /Shop Scene
REVEALED MGR ID  : "Shop"
Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager("Shop")
                   mgr:SetSprite(CurrentModGuid, "PATH", customSprite)
                   mgr:RestoreSprite(CurrentModGuid, "PATH", customSprite)

do
    local POSSIBLE_MANAGERS = { "Shop", "" }
    local PATH_BACKGROUND = "/Background"
    local PATH_FOREGROUND = "/Foreground"
    local function ExecutionPipeline()
        local custom_bg   = ModUtilities.GetAsSprite(CurrentModGuid, "shop.png")
        local custom_fore = ModUtilities.GetAsSprite(CurrentModGuid, "cashier_counter.png")
        while true do
            local activeManager = nil
            for _, id in ipairs(POSSIBLE_MANAGERS) do
                local mgr = TextureOverriderManager.GetTextureOverriderManager(id)
                if mgr ~= nil then
                    activeManager = mgr
                    break
                end
            end
            if activeManager ~= nil then
                if custom_bg   ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_BACKGROUND, custom_bg) end
                if custom_fore ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_FOREGROUND, custom_fore) end
                custom_bg   = nil
                custom_fore = nil
                break
            end
            coroutine.yield()
        end
    end
    local co = coroutine.create(ExecutionPipeline)
    coroutine.resume(co)
end
_________________________________________________________________________

Instance #4: Anon Player and Jun Bot Shower (Isolated Scene)
Component Type   : TextureOverriderManager
Object Name      : "Shower Scene" - Anon Player and Jun Bot Shower (Isolated Scene)
Absolute Path    : /Shower Scene
REVEALED MGR ID  : "shower"
Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager("shower")
                   mgr:SetSprite(CurrentModGuid, "PATH", customSprite)
                   mgr:RestoreSprite(CurrentModGuid, "PATH", customSprite)

-- PROJECT "ELBRUS-BASE": CLASSIC AND SAFE TEXTURE INJECTOR FOR THE SHOWER SCENE
do
    local POSSIBLE_IDS = { "shower", "room_shower" }
    local PATH_FOREGROUND = "/Foreground/Shower Foreground"
    local PATH_CENTER     = "/Center/Shower Wall"
    local PATH_BACK       = "/Back/Shower Back"
    local PATH_PROPS      = "/Back/Shower Back Props"
    local function ExecutionPipeline()
        local custom_fore  = ModUtilities.GetAsSprite(CurrentModGuid, "Foreground.png")
        local custom_cent  = ModUtilities.GetAsSprite(CurrentModGuid, "Wall.png")
        local custom_back  = ModUtilities.GetAsSprite(CurrentModGuid, "Background.png")
        local custom_props = ModUtilities.GetAsSprite(CurrentModGuid, "BackgroundProps.png")
        while true do
            local intimacyManager = nil
            for _, id in ipairs(POSSIBLE_IDS) do
                local mgr = TextureOverriderManager.GetTextureOverriderManager(id)
                if mgr ~= nil then
                    intimacyManager = mgr
                    break
                end
            end
            if intimacyManager ~= nil then
                if custom_fore  ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_FOREGROUND, custom_fore) end
                if custom_cent  ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_CENTER, custom_cent) end
                if custom_back  ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_BACK, custom_back) end
                if custom_props ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_PROPS, custom_props) end
                custom_fore  = nil
                custom_cent  = nil
                custom_back  = nil
                custom_props = nil
                break
            end
            coroutine.yield()
        end
    end
    local co = coroutine.create(ExecutionPipeline)
    coroutine.resume(co)
end
_________________________________________________________________________

Instance #5: Missionary Pose (Isolated Scene)
Component Type   : TextureOverriderManager
Object Name      : "Missionary Scene" - Missionary Pose (Isolated Scene)
Absolute Path    : /Missionary Scene
REVEALED MGR ID  : "room_missionary"
Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager("room_missionary")
                   mgr:SetSprite(CurrentModGuid, "PATH", customSprite)
                   mgr:RestoreSprite(CurrentModGuid, "PATH", customSprite)

-- PROJECT "ELBRUS": OPTIMIZED MISSIONARY SCENE OVERRIDE
do
    local MANAGER_ID = "room_missionary"
    local PATH_BACKGROUND = "/Missionary Image"
    local PATH_LIGHTING   = "/Light Multiply"
    local function ExecutionPipeline()
        local custom_bg    = ModUtilities.GetAsSprite(CurrentModGuid, "room_couch_missionary.png")
        local custom_light = ModUtilities.GetAsSprite(CurrentModGuid, "MissionaryLight.png")
        while true do
            local intimacyManager = TextureOverriderManager.GetTextureOverriderManager(MANAGER_ID)
            if intimacyManager ~= nil then
                if custom_bg    ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_BACKGROUND, custom_bg) end
                if custom_light ~= nil then intimacyManager:SetSprite(CurrentModGuid, PATH_LIGHTING, custom_light) end
                custom_bg    = nil
                custom_light = nil
                break
            end
            coroutine.yield()
        end
    end
    local co = coroutine.create(ExecutionPipeline)
    coroutine.resume(co)
end
_________________________________________________________________________

Instance #6: Clothier Shop Lady Annalie (Isolated Scene)
Component Type   : TextureOverriderManager
Object Name      : "Clothier Scene" - Clothier Shop Lady Annalie (Isolated Scene)
Absolute Path    : /Clothier Scene
REVEALED MGR ID  : "Clothier"
Lua Usage        : local mgr = TextureOverriderManager.GetTextureOverriderManager("Clothier")
                   mgr:SetSprite(CurrentModGuid, "PATH", customSprite)
                   mgr:RestoreSprite(CurrentModGuid, "PATH", customSprite)

do
    local POSSIBLE_MANAGERS = { "Clothier", "Shop" }
    local PATH_BACKGROUND = "/Background"
    local PATH_BLINK      = "/Blink"
    local PATH_DECOR      = "/Halloween Decoration"
    local PATH_FOREGROUND = "/Foreground"
    local PATH_CAT        = "/Halloween Cat"
    local function ExecutionPipeline()
        local custom_bg    = ModUtilities.GetAsSprite(CurrentModGuid, "Annalie_Background.png")
        local custom_blink = ModUtilities.GetAsSprite(CurrentModGuid, "Annalie_Blink.png")
        local custom_decor = ModUtilities.GetAsSprite(CurrentModGuid, "Annalie_Decoration.png")
        local custom_fore  = ModUtilities.GetAsSprite(CurrentModGuid, "Annalie_Foreground.png")
        local custom_cat   = ModUtilities.GetAsSprite(CurrentModGuid, "Annalie_Cat.png")
        while true do
            local activeManager = nil
            for _, id in ipairs(POSSIBLE_MANAGERS) do
                local mgr = TextureOverriderManager.GetTextureOverriderManager(id)
                if mgr ~= nil then
                    activeManager = mgr
                    break
                end
            end
            if activeManager ~= nil then
                if custom_bg    ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_BACKGROUND, custom_bg) end
                if custom_decor ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_DECOR, custom_decor) end
                if custom_blink ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_BLINK, custom_blink) end
                if custom_fore  ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_FOREGROUND, custom_fore) end
                if custom_cat   ~= nil then activeManager:SetSprite(CurrentModGuid, PATH_CAT, custom_cat) end
                custom_bg    = nil
                custom_decor = nil
                custom_blink = nil
                custom_fore  = nil
                custom_cat   = nil
                break
            end
            coroutine.yield()
        end
    end
    local co = coroutine.create(ExecutionPipeline)
    coroutine.resume(co)
end

Total TextureOverriderManager instances: 6
