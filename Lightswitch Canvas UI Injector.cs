using System;
using System.IO;
using System.Collections;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;


[assembly: MelonInfo(typeof(LightswitchOverrideMod.MainModClass), "Lightswitch Canvas UI Injector", "1.0.0", "Paul.Epifancev #warlockforbugs (Discord: anon.jun)")]
[assembly: MelonGame(null, null)]

namespace LightswitchOverrideMod
{
	public class MainModClass : MelonMod
	{
		private string modsDirectory = "";
		private bool isCoroutineRunning = false;

		private Sprite customSwitchOnSprite = null;
		private Sprite customSwitchOffSprite = null;

		public override void OnInitializeMelon()
		{
			modsDirectory = Path.GetDirectoryName(base.MelonAssembly.Assembly.Location);
			LoadCustomTextures();
		}

		public override void OnUpdate()
		{
			// ТВОЙ ШАБЛОН ОБНИМАШЕК: Автоматический бесконечный старт без кнопок
			if (!isCoroutineRunning)
			{
				isCoroutineRunning = true;
				MelonCoroutines.Start(RoomLightSwitchPipeline());
			}
		}
				private IEnumerator RoomLightSwitchPipeline()
		{
			while (true)
			{
				// Снайперски находим объект Canvas UI выключателя по истинному пути из твоего радара
				GameObject switchGo = GameObject.Find("/Room/LightSwitch") ?? GameObject.Find("LightSwitch");

				if (switchGo != null && switchGo.activeInHierarchy)
				{
					// Забираем нативный компонент картинки интерфейса через Of/TryCast
					Image uiImage = null;
					UnityEngine.Component rawRenderer = switchGo.GetComponentInChildren(Il2CppType.Of<Image>());
					
					if (rawRenderer != null)
					{
						uiImage = rawRenderer.TryCast<Image>();
					}

					// ПРИКАЗ ВЫПОЛНЕН: Намертво удерживаем текстуру кастомного рубильника в ОЗУ
					if (uiImage != null)
					{
						if (customSwitchOnSprite != null)
						{
							uiImage.sprite = customSwitchOnSprite;
						}
						
						// ИСПРАВЛЕНО: Полностью вырезана sizeDelta! Переводим картинку в стандартный режим Unity,
						// чтобы она аккуратно вписалась в нативные границы Шипа без раздувания на весь экран.
						uiImage.type = Image.Type.Simple;
						uiImage.preserveAspect = true;
						
						// Страховочный принудительный Apply для удержания видимости в Canvas UI
						uiImage.canvasRenderer.SetAlpha(1f);
					}
				}

				// Спим ровно 1 кадр по твоему регламенту обнимашек, разгружая FPS от фризов
				yield return null;
			}
		}

		private void LoadCustomTextures()
		{
			try
			{
				// Истинные оригинальные названия картинок из твоей папки Mods/
				string onPath = Path.Combine(modsDirectory, "LightswitchOn.png");
				string offPath = Path.Combine(modsDirectory, "LightswitchOff.png");

				if (File.Exists(onPath))
				{
					byte[] data = File.ReadAllBytes(onPath);
					Texture2D tex = new Texture2D(2, 2);
					ImageConversion.LoadImage(tex, data);
					customSwitchOnSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
					LoggerInstance.Msg("[SUCCESS] Custom LightswitchOn.png cached into C# memory.");
				}

				if (File.Exists(offPath))
				{
					byte[] data = File.ReadAllBytes(offPath);
					Texture2D tex = new Texture2D(2, 2);
					ImageConversion.LoadImage(tex, data);
					customSwitchOffSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
					LoggerInstance.Msg("[SUCCESS] Custom LightswitchOff.png cached into C# memory.");
				}
			}
			catch (Exception e)
			{
				LoggerInstance.Error("Failed to load custom UI textures: " + e.Message);
			}
		}
	}
}
