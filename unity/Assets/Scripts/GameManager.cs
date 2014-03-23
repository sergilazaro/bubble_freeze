using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public enum State
	{
		MainMenu,
		Playing,
		Finished
	}

	public State state = State.Finished;

	public float ColorMainHue = 0.74f;
	//public Color ColorMainHue = new Color(0.14f, 0.38f, 0.38f);
	public Vector2 ColorWallSatVal = new Vector2(161.0f / 255.0f, 98.0f / 255.0f);
	public Vector2 ColorBackgroundSatVal = new Vector2(154.0f / 255.0f, 146.0f / 255.0f);
	public Vector2 ColorBallSatVal = new Vector2(69.0f / 255.0f, 223.0f / 255.0f);

	public Color ColorWall { get { return HSVtoRGB(ColorMainHue, ColorWallSatVal.x, ColorWallSatVal.y); } }
	public Color ColorBackground { get { return HSVtoRGB(ColorMainHue, ColorBackgroundSatVal.x, ColorBackgroundSatVal.y); } }
	public Color ColorBall { get { return HSVtoRGB(ColorMainHue, ColorBallSatVal.x, ColorBallSatVal.y); } }

	public float BallSlowdownTime = 1.0f;
	public float BallRemainFrozenTime = 1.0f;
	public float BallFadeInTime = 0.2f;
	public float BallFadeOutTime = 1.0f;
	public float BallClickTime = 0.5f;
	public float BallClickSizeFactor = 1.3f;
	public float BallExplodingSizeFactor = 1.5f;
	public Color BallFrozenColor = Color.white;

	public float LevelTextFadeInTime = 0.5f;
	public float LevelTextStayTime = 1.0f;
	public float LevelTextFadeOutTime = 0.5f;

	public float DoneTextStartY = 0.2f;
	public float DoneTextEndY = 0.25f;

	public int ScoreNormalSize = 40;
	public int ScoreBigSize = 60;
	public float ScoreResizeTime = 1.0f;

	public float ButtonAlpha = 0.25f;

	public bool VelocityRandomization = false;
	public float VelocityMagnitude = 2.0f;

	public GameObject background;
	public GameObject mainMenu;
	public LevelManager levelManager;

	//public AudioSource soundFreezeMain;
	//public AudioSource soundFreezeSub;

	public AudioClip soundFreezeMain;
	public AudioClip soundFreezeSub;
	public AudioClip soundDone;

	void Awake()
	{
		instance = this;

		// set up colors
		{
			Camera.main.backgroundColor = ColorWall;

			//foreach (BounceBall ball in Object.FindObjectsOfType<BounceBall>())
			//{
			//	ball.sprite.color = ColorBall;
			//}

			Color bgcolor = ColorBackground;
			bgcolor.a = 0;
			((SpriteRenderer)background.renderer).color = bgcolor;
		}

		FadeInMenu();

		//audio.Play();
	}

	public void TransitionToNewHue()
	{
		StartCoroutine(TransitionToNewHue_Coroutine());
	}

	private IEnumerator TransitionToNewHue_Coroutine()
	{
		Color origCamColor = Camera.main.backgroundColor;
		Color destCamColor = HSVtoRGB(ColorMainHue, ColorWallSatVal.x, ColorWallSatVal.y);
		Color origBgColor = ((SpriteRenderer)background.renderer).color;
		Color destBgColor = HSVtoRGB(ColorMainHue, ColorBackgroundSatVal.x, ColorBackgroundSatVal.y);

		float elapsedTime = 0;
		while (elapsedTime < LevelTextFadeInTime)
		{
			yield return new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;

			float t = elapsedTime / LevelTextFadeInTime;

			Camera.main.backgroundColor = Color.Lerp(origCamColor, destCamColor, t);
			((SpriteRenderer)background.renderer).color = Color.Lerp(origCamColor, destCamColor, t);
		}
	}

	public void StartGame()
	{
		if (state == State.MainMenu)
		{
			FadeOutMenuAndStart();
		}
	}

	private void FadeInMenu()
	{
		mainMenu.transform.Find("square").gameObject.SetActive(true);

		FadeText(mainMenu.transform.Find("title").GetComponent<GUIText>(), 0, levelManager.levelIntroAlpha, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("title shadow").GetComponent<GUIText>(), 0, 0.3f, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("by").GetComponent<GUIText>(), 0, levelManager.levelIntroAlpha, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("start").GetComponent<GUIText>(), 0, levelManager.levelIntroAlpha, LevelTextFadeInTime);
		FadeSprite(mainMenu.transform.Find("square").GetComponent<SpriteRenderer>(), 0, ButtonAlpha, LevelTextFadeInTime);

		DelayedAction(LevelTextFadeInTime, () => { state = State.MainMenu; });
	}

	private void FadeOutMenuAndStart()
	{
		FadeText(mainMenu.transform.Find("title").GetComponent<GUIText>(), levelManager.levelIntroAlpha, 0, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("title shadow").GetComponent<GUIText>(), 0.3f, 0, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("by").GetComponent<GUIText>(), levelManager.levelIntroAlpha, 0, LevelTextFadeInTime);
		FadeText(mainMenu.transform.Find("start").GetComponent<GUIText>(), levelManager.levelIntroAlpha, 0, LevelTextFadeInTime);
		FadeSprite(mainMenu.transform.Find("square").GetComponent<SpriteRenderer>(), ButtonAlpha * 0.5f, 0, LevelTextFadeInTime);

		FadeInBackground();

		DelayedAction(LevelTextFadeInTime, () =>
		{
			state = State.Playing;

			mainMenu.transform.Find("square").gameObject.SetActive(false);
			levelManager.StartGame();
		});
	}

	public void AllFinished()
	{
		state = State.Finished;

		FadeText(levelManager.levelsFinishedText, 0, levelManager.levelIntroAlpha, LevelTextFadeInTime);
		FadeOutBackground();

		DelayedAction(LevelTextFadeInTime * 5.0f, () =>
		{
			FadeText(levelManager.levelsFinishedText, levelManager.levelIntroAlpha, 0, LevelTextFadeInTime);

			DelayedAction(LevelTextFadeInTime, () =>
			{
				FadeInMenu();
			});
		});
	}

	public void FadeInBackground()
	{
		FadeSprite(background.GetComponent<SpriteRenderer>(), 0, 1, LevelTextFadeInTime);
	}

	public void FadeOutBackground()
	{
		FadeSprite(background.GetComponent<SpriteRenderer>(), 1, 0, LevelTextFadeInTime);
	}

	private void FadeText(GUIText text, float origAlpha, float destAlpha, float time)
	{
		StartCoroutine(FadeText_Coroutine(text, origAlpha, destAlpha, time));
	}

	private IEnumerator FadeText_Coroutine(GUIText text, float origAlpha, float destAlpha, float time)
	{
		// fade in
		{
			Color origColor = text.color;
			origColor.a = origAlpha;
			Color destColor = text.color;
			destColor.a = destAlpha;

			float elapsedTime = 0;
			while (elapsedTime < time)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / time;

				text.color = Color.Lerp(origColor, destColor, t);
			}
		}
	}

	private void FadeSprite(SpriteRenderer sprite, float origAlpha, float destAlpha, float time)
	{
		StartCoroutine(FadeSprite_Coroutine(sprite, origAlpha, destAlpha, time));
	}

	private IEnumerator FadeSprite_Coroutine(SpriteRenderer sprite, float origAlpha, float destAlpha, float time)
	{
		// fade in
		{
			Color origColor = sprite.color;
			origColor.a = origAlpha;
			Color destColor = sprite.color;
			destColor.a = destAlpha;

			float elapsedTime = 0;
			while (elapsedTime < time)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / time;

				sprite.color = Color.Lerp(origColor, destColor, t);
			}
		}
	}

	public static void DelayedAction(float delay, System.Action action)
	{
		GameManager.instance.StartDelayedAction(delay, action);
	}

	private void StartDelayedAction(float delay, System.Action action)
	{
		StartCoroutine(StartDelayedAction_Coroutine(delay, action));
	}

	private IEnumerator StartDelayedAction_Coroutine(float delay, System.Action action)
	{
		yield return new WaitForSeconds(delay);

		action.Invoke();
	}

#region Color Utils

	public static float GetHueFromRGB(Color color)
	{
		float hue = 0;
		
		float r = color.r;
		float g = color.g;
		float b = color.b;
 
		float max = Mathf.Max(r, Mathf.Max(g, b));
 
		if (max <= 0)
		{
			return 0;
		}
 
		float min = Mathf.Min(r, Mathf.Min(g, b));
		float dif = max - min;
 
		if (max > min)
		{
			if (g == max)
			{
				hue = (b - r) / dif * 60f + 120f;
			}
			else if (b == max)
			{
				hue = (r - g) / dif * 60f + 240f;
			}
			else if (b > g)
			{
				hue = (g - b) / dif * 60f + 360f;
			}
			else
			{
				hue = (g - b) / dif * 60f;
			}
			if (hue < 0)
			{
				hue = hue + 360f;
			}
		}
		else
		{
			hue = 0;
		}

		hue *= 1f / 360f;
 
		return hue;
	}

	public static Vector4 RGBtoHSV(Color color)
	{
		Vector4 ret = new Vector4(0f, 0f, 0f, color.a);
 
		float r = color.r;
		float g = color.g;
		float b = color.b;
 
		float max = Mathf.Max(r, Mathf.Max(g, b));
 
		if (max <= 0)
		{
			return ret;
		}
 
		float min = Mathf.Min(r, Mathf.Min(g, b));
		float dif = max - min;
 
		if (max > min)
		{
			if (g == max)
			{
				ret.x = (b - r) / dif * 60f + 120f;
			}
			else if (b == max)
			{
				ret.x = (r - g) / dif * 60f + 240f;
			}
			else if (b > g)
			{
				ret.x = (g - b) / dif * 60f + 360f;
			}
			else
			{
				ret.x = (g - b) / dif * 60f;
			}
			if (ret.x < 0)
			{
				ret.x = ret.x + 360f;
			}
		}
		else
		{
			ret.x = 0;
		}
 
		ret.x *= 1f / 360f;
		ret.y = (dif / max) * 1f;
		ret.z = max;
 
		return ret;
	}

	public static Color HSVtoRGB(Vector3 hsv)
	{
		return HSVtoRGB(hsv.x, hsv.y, hsv.z);
	}

	public static Color HSVtoRGB(float h, float s, float v)
	{
		// taken from http://wiki.unity3d.com/index.php?title=HSBColor
		float r = v;
		float g = v;
		float b = v;

		if (s != 0)
		{
			float max = v;
			float dif = v * s;
			float min = v - dif;
 
			float h360 = h * 360f;
 
			if (h360 < 60f)
			{
				r = max;
				g = h360 * dif / 60f + min;
				b = min;
			}
			else if (h360 < 120f)
			{
				r = -(h360 - 120f) * dif / 60f + min;
				g = max;
				b = min;
			}
			else if (h360 < 180f)
			{
				r = min;
				g = max;
				b = (h360 - 120f) * dif / 60f + min;
			}
			else if (h360 < 240f)
			{
				r = min;
				g = -(h360 - 240f) * dif / 60f + min;
				b = max;
			}
			else if (h360 < 300f)
			{
				r = (h360 - 240f) * dif / 60f + min;
				g = min;
				b = max;
			}
			else if (h360 <= 360f)
			{
				r = max;
				g = min;
				b = -(h360 - 360f) * dif / 60 + min;
			}
			else
			{
				r = 0;
				g = 0;
				b = 0;
			}
		}
 
		return new Color(Mathf.Clamp01(r),Mathf.Clamp01(g),Mathf.Clamp01(b));
	}

#endregion
}
