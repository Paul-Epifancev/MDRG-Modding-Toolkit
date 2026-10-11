using System;
using System.IO;
using System.Collections;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CuddleOverrideMod.MainModClass), "Cuddle Ultimate Bedroom Injector", "1.0.0", "Paul.Epifancev #warlockforbugs (Discord: anon.jun)")]
[assembly: MelonGame(null, null)]

namespace CuddleOverrideMod
{
	
	public class MainModClass : MelonMod
	{
		private string modsDirectory = "";
		private bool isCoroutineRunning = false;

	
		private Sprite customBgSprite = null;
		private Sprite customBedSprite = null;
		private Sprite customLightBgSprite = null; 

		public override void OnInitializeMelon()
		{
			modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
			LoadCustomTextures();
		}

		public override void OnUpdate()
		{
			if (!isCoroutineRunning)
			{
				isCoroutineRunning = true;
				MelonCoroutines.Start(CuddleRoomLightingPipeline());
			}
		}

		private void LoadCustomTextures()
		{
			try
			{
				string bgPath = Path.Combine(modsDirectory, "Wall_0.png");
				string bedPath = Path.Combine(modsDirectory, "Sleep.png");
				string lightBgPath = Path.Combine(modsDirectory, "CuddleLight_Background.png");

				if (File.Exists(bgPath))
				{
					byte[] data = File.ReadAllBytes(bgPath); Texture2D tex = new Texture2D(2, 2); ImageConversion.LoadImage(tex, data);
					customBgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
				}

				if (File.Exists(bedPath))
				{
					byte[] data = File.ReadAllBytes(bedPath); Texture2D tex = new Texture2D(2, 2); ImageConversion.LoadImage(tex, data);
					customBedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 99.67949f);
				}

				if (File.Exists(lightBgPath))
				{
					byte[] data = File.ReadAllBytes(lightBgPath); Texture2D tex = new Texture2D(2, 2); ImageConversion.LoadImage(tex, data);
					customLightBgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
				}
			}
			catch (Exception e)
			{
				LoggerInstance.Error("Failed to load custom textures bounds: " + e.Message);
			}
		}
		
		private IEnumerator CuddleRoomLightingPipeline()
		{
			while (true)
			{
				GameObject cuddleValidation = GameObject.Find("/Views/Cuddle Scene") 
										   ?? GameObject.Find("Cuddle Scene")
										   ?? GameObject.Find("Cuddle_Interact_Canvas");

				if (cuddleValidation != null && cuddleValidation.activeInHierarchy)
				{
					ForceInjectNodeContinuous("/Views/Cuddle Scene/Cuddle Background", "Cuddle Background", customBgSprite);
					ForceInjectNodeContinuous("/Views/Cuddle Scene/Cuddle Bed", "Cuddle Bed", customBedSprite);
					
					
					ForceInjectNodeContinuous("/Views/Cuddle Scene/Light Multiply", "Light Multiply", customLightBgSprite);
				}

				yield return null;
			}
		}

		private void ForceInjectNodeContinuous(string fullPath, string shortName, Sprite customSprite)
		{
			if (customSprite == null) return;

			GameObject go = GameObject.Find(fullPath) ?? GameObject.Find(shortName);
			if (go != null && go.activeInHierarchy)
			{
				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
				if (sr == null)
				{
					UnityEngine.Component rawRenderer = go.GetComponentInChildren(Il2CppType.Of<SpriteRenderer>());
					if (rawRenderer != null)
					{
						sr = rawRenderer.TryCast<SpriteRenderer>();
					}
				}

				if (sr != null)
				{
					
					sr.sprite = customSprite;
				}
			}
		}
	}
}
