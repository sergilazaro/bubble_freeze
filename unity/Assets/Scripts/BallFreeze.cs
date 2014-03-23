using UnityEngine;
using System.Collections;

public class BallFreeze : MonoBehaviour
{
	public enum State
	{
		FadingIn,
		Accelerating,
		Moving,
		Freezing,
		Frozen,
		FadingOut
	}

	public State state = State.FadingIn;

	public bool IsFrozen { get { return (state == State.Freezing || state == State.Frozen); } }

	private BounceBall bounceBall;

	void Awake()
	{
		bounceBall = GetComponent<BounceBall>();
	}

	void OnMouseDown()
	{
		if (state == State.Moving)
		{
			if (!LevelManager.instance.alreadyClicked)
			{
				LevelManager.instance.alreadyClicked = true;
				StartClicking();
				StartFreezing();

				GameManager.instance.audio.PlayOneShot(GameManager.instance.soundFreezeMain, 0.8f);
			}
		}
	}

	public void StartFreezing()
	{
		StartCoroutine(StartFreezing_Coroutine());
	}

	public void StartClicking()
	{
		StartCoroutine(StartClicking_Coroutine());
	}

	public void StartFadeOut()
	{
		StartCoroutine(StartFadeOut_Coroutine());
	}

	public void StartSlowingDownAndExploding()
	{
		StartCoroutine(StartSlowingDownAndFadeOut_Coroutine());
	}

	public void StartFadeInAndAccelerate()
	{
		StartCoroutine(StartFadeInAndAccelerate_Coroutine());
	}

	private IEnumerator StartSlowingDownAndFadeOut_Coroutine()
	{
		StartCoroutine(StartSlowingDown_Coroutine(2.0f));

		yield return new WaitForSeconds(GameManager.instance.BallSlowdownTime * 2.5f);

		StartCoroutine(StartFadeOut_Coroutine(4.0f));
	}

	private IEnumerator StartClicking_Coroutine()
	{
		float origScale = bounceBall.sprite.transform.localScale.x;
		float scaleDiff = GameManager.instance.BallClickSizeFactor * origScale - origScale;

		float elapsedTime = 0;
		while (elapsedTime < GameManager.instance.BallClickTime)
		{
			yield return new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;

			float t = elapsedTime / GameManager.instance.BallClickTime;
			float scale = origScale + scaleDiff * 0.5f * (1 - Mathf.Cos(t * 2 * Mathf.PI));

			bounceBall.sprite.transform.localScale = Vector3.one * scale;
		}

		bounceBall.sprite.transform.localScale = Vector3.one * origScale;
	}

	private IEnumerator StartFreezing_Coroutine()
	{
		if (state != State.Moving)
		{
			yield break;
		}

		state = State.Freezing;
		LevelManager.instance.NewCircleFrozen();

		// slow down
		{
			float origSpeed = bounceBall.speed;
			Color origColor = bounceBall.sprite.color;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.BallSlowdownTime)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / GameManager.instance.BallSlowdownTime;

				bounceBall.speed = Mathf.Lerp(origSpeed, 0, t);
				bounceBall.sprite.color = Color.Lerp(origColor, GameManager.instance.BallFrozenColor, t);
			}

			bounceBall.speed = 0;
		}
		
		state = State.Frozen;

		// remain frozen
		{
			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.BallRemainFrozenTime)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;
			}
		}

		StartFadeOut();
	}

	private IEnumerator StartSlowingDown_Coroutine(float timeFactor = 1.0f)
	{
		float origSpeed = bounceBall.speed;

		float elapsedTime = 0;
		while (elapsedTime < GameManager.instance.BallSlowdownTime * timeFactor)
		{
			yield return new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;

			float t = elapsedTime / (GameManager.instance.BallSlowdownTime * timeFactor);

			bounceBall.speed = Mathf.Lerp(origSpeed, 0, t);
		}

		bounceBall.speed = 0;
	}

	private IEnumerator StartFadeOut_Coroutine(float timeFactor = 1.0f)
	{
		if (state == State.FadingOut)
		{
			yield break;
		}

		state = State.FadingOut;
		bounceBall.isKinematic = true;

		// explode
		{
			float origScale = transform.localScale.x;
			Color origColor = bounceBall.sprite.color;
			Color destColor = origColor;
			destColor.a = 0;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.BallFadeOutTime * timeFactor)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / (GameManager.instance.BallFadeOutTime * timeFactor);
				t = InvExp(t, 0.001f);

				transform.localScale = Vector3.one * Mathf.Lerp(origScale, origScale * GameManager.instance.BallExplodingSizeFactor, t);
				bounceBall.sprite.color = Color.Lerp(origColor, destColor, t);
			}
		}

		LevelManager.instance.NewCircleGone();
		Destroy(gameObject);
	}

	private IEnumerator StartFadeInAndAccelerate_Coroutine(float timeFactor = 1.0f)
	{
		state = State.FadingIn;
		bounceBall.isKinematic = true;

		// fade in
		{
			float origScale = transform.localScale.x * GameManager.instance.BallExplodingSizeFactor;
			float destScale = transform.localScale.x;
			transform.localScale = Vector3.one * origScale;

			Color origColor = bounceBall.sprite.color;
			origColor.a = 0;
			Color destColor = bounceBall.sprite.color;
			bounceBall.sprite.color = origColor;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.BallFadeOutTime * timeFactor)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / (GameManager.instance.BallFadeOutTime * timeFactor);
				t = InvExp(t, 0.001f);

				transform.localScale = Vector3.one * Mathf.Lerp(origScale, destScale, t);
				bounceBall.sprite.color = Color.Lerp(origColor, destColor, t);
			}
		}

		state = State.Accelerating;
		bounceBall.isKinematic = false;

		// accelerate
		{
			float destSpeed = bounceBall.speed;
			float origSpeed = 0;
			Vector3 velocityNormalized = bounceBall.velocity.normalized;
			bounceBall.speed = origSpeed;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.BallFadeInTime * timeFactor)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / (GameManager.instance.BallFadeInTime * timeFactor);

				if (bounceBall.velocity.magnitude > float.Epsilon)
				{
					velocityNormalized = bounceBall.velocity.normalized;
				}

				bounceBall.velocity = velocityNormalized * Mathf.Lerp(origSpeed, destSpeed, t);
			}

			bounceBall.speed = destSpeed;
		}

		state = State.Moving;
	}

	public static float Hermite(float t)
	{
		return 3 * t * t - 2 * t * t * t;
	}

	public static float Exp(float t, float b = 0.1f)
	{
		return (Mathf.Pow(b, -t) - 1) / ((1.0f / b) - 1);
	}

	public static float InvExp(float t, float b = 0.1f)
	{
		return 1 - Exp(1 - t, b);
	}
}
