using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level
{
	public int NumCirclesToDestroy;

	public Dictionary<int, int> CircleSizes = new Dictionary<int, int>();

	public int NumTotalCircles
	{
		get
		{
			int num = 0;

			foreach (var entry in CircleSizes)
			{
				int numCircles = entry.Value;

				num += numCircles;
			}

			return num;
		}
	}
}

public class LevelManager : MonoBehaviour
{
	public static LevelManager instance;

	public List<float> circleSizes = new List<float>();

	private List<Level> _levels = new List<Level>();

	public int currentLevelNum = -1;
	public Level currentLevel = null;

	public List<BounceBall> balls = new List<BounceBall>();

	public GameObject ballPrefab;

	public GUIText doneText;
	public GUIText tryAgainText;
	public GUIText levelsFinishedText;
	public GUIText scoreText;
	public GUIText levelIntroText;
	public GUIText levelTargetText;
	public float levelIntroAlpha = 0.75f;
	public float levelTargetAlpha = 0.5f;

	public int numCirclesFrozen = 0;
	public int numCirclesGone = 0;

	public bool alreadyClicked = false;
	public bool tryAgain = false;

	public static float colliderRadius = 2.56f;

	void Awake()
	{
		instance = this;

		SetupLevels();

		//GameManager.DelayedAction(1.0f, () => { LoadLevel(); });
	}

	public void StartGame()
	{
		currentLevelNum = -1;
		currentLevel = null;
		numCirclesFrozen = 0;
		numCirclesGone = 0;
		alreadyClicked = false;
		tryAgain = false;

		LoadLevel();
	}

	public void NewCircleFrozen()
	{
		numCirclesFrozen++;
		RefreshScoreText();

		if (numCirclesFrozen == currentLevel.NumCirclesToDestroy)
		{
			StartResizingScoreText(false);
			GameManager.instance.audio.PlayOneShot(GameManager.instance.soundDone, 0.8f);
		}
	}

	public void NewCircleGone()
	{
		numCirclesGone++;

		if (numCirclesGone == numCirclesFrozen)
		{
			// level done

			bool anyBallRemoved = false;
			foreach (BounceBall ball in balls)
			{
				if (ball != null && ball.GetComponent<BallFreeze>().state != BallFreeze.State.FadingOut)
				{
					ball.GetComponent<BallFreeze>().StartSlowingDownAndExploding();
					anyBallRemoved = true;
				}
			}

			if (numCirclesFrozen >= currentLevel.NumCirclesToDestroy)
			{
				StartResizingScoreText(true);
			}
			else
			{
				tryAgain = true;
			}

			FadeText(scoreText, scoreText.color.a, 0, GameManager.instance.LevelTextFadeOutTime);

			if (!anyBallRemoved)
			{
				// there weren't any balls removed because all were gone, just load next level
				Debug.Log("NO BALLS REMOVED");
				LoadLevel();
			}
		}
		else if (numCirclesGone == balls.Count)
		{
			// all disappeared

			if (numCirclesFrozen < currentLevel.NumCirclesToDestroy)
			{
				// repeat level
				currentLevelNum--;
			}

			LoadLevel();
		}
	}

	private void StartResizingScoreText(bool inverse)
	{
		StartCoroutine(StartResizingScoreText_Coroutine(inverse));
	}

	private IEnumerator StartResizingScoreText_Coroutine(bool inverse)
	{
		Color origColorDone = doneText.color;
		Color destColorDone = doneText.color;

		Vector3 pos;

		if (inverse)
		{
			origColorDone.a = levelIntroAlpha;
			destColorDone.a = 0;
		}
		else
		{
			origColorDone.a = 0;
			destColorDone.a = levelIntroAlpha;
		}

		float elapsedTime = 0;
		while (elapsedTime < GameManager.instance.ScoreResizeTime)
		{
			yield return new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;

			float linear_t = elapsedTime / GameManager.instance.ScoreResizeTime;
			float t = BallFreeze.InvExp(linear_t, 0.01f);
			//t = BallFreeze.Hermite(t);

			if (inverse)
			{
				scoreText.fontSize = (int)Mathf.Lerp(GameManager.instance.ScoreBigSize, GameManager.instance.ScoreNormalSize, t);
			}
			else
			{
				scoreText.fontSize = (int)Mathf.Lerp(GameManager.instance.ScoreNormalSize, GameManager.instance.ScoreBigSize, t);

				pos = doneText.transform.position;
				pos.y = Mathf.Lerp(GameManager.instance.DoneTextStartY, GameManager.instance.DoneTextEndY, t);
				doneText.transform.position = pos;
			}

			doneText.color = Color.Lerp(origColorDone, destColorDone, t);

			//Vector3 pos = doneText.transform.position;
			//pos.y = Mathf.Lerp(origY, destY, linear_t);
			//doneText.transform.position = pos;
		}

		if (inverse)
		{
			pos = doneText.transform.position;
			pos.y = GameManager.instance.DoneTextStartY;
			doneText.transform.position = pos;
		}
	}

	private void StartLevelText()
	{
		StartCoroutine(StartLevelText_Coroutine());
	}

	private IEnumerator StartLevelText_Coroutine()
	{
		// fade in
		{
			Color origColorIntro = levelIntroText.color;
			Color destColorIntro = levelIntroText.color;
			destColorIntro.a = levelIntroAlpha;
			Color origColorTarget = levelTargetText.color;
			Color destColorTarget = levelTargetText.color;
			destColorTarget.a = levelTargetAlpha;
			Color origColorTryagain = tryAgainText.color;
			Color destColorTryagain = tryAgainText.color;
			destColorTryagain.a = levelTargetAlpha;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.LevelTextFadeInTime)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / GameManager.instance.LevelTextFadeInTime;
				//t = BallFreeze.InvExp(t, 0.01f);

				levelIntroText.color = Color.Lerp(origColorIntro, destColorIntro, t);
				levelTargetText.color = Color.Lerp(origColorTarget, destColorTarget, t);

				if (tryAgain)
				{
					tryAgainText.color = Color.Lerp(origColorTryagain, destColorTryagain, t);
				}
			}
		}

		// wait
		yield return new WaitForSeconds(GameManager.instance.LevelTextStayTime);

		// fade out
		{
			Color origColorIntro = levelIntroText.color;
			Color destColorIntro = levelIntroText.color;
			destColorIntro.a = 0;
			Color origColorTarget = levelTargetText.color;
			Color destColorTarget = levelTargetText.color;
			destColorTarget.a = 0;
			Color origColorTryagain = tryAgainText.color;
			Color destColorTryagain = tryAgainText.color;
			destColorTryagain.a = 0;

			float elapsedTime = 0;
			while (elapsedTime < GameManager.instance.LevelTextFadeInTime)
			{
				yield return new WaitForEndOfFrame();
				elapsedTime += Time.deltaTime;

				float t = elapsedTime / GameManager.instance.LevelTextFadeInTime;
				t = BallFreeze.InvExp(t, 0.01f);

				levelIntroText.color = Color.Lerp(origColorIntro, destColorIntro, t);
				levelTargetText.color = Color.Lerp(origColorTarget, destColorTarget, t);

				if (tryAgain)
				{
					tryAgainText.color = Color.Lerp(origColorTryagain, destColorTryagain, t);
				}
			}
		}

		tryAgain = false;
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

	private void RefreshScoreText()
	{
		scoreText.text = "" + numCirclesFrozen + " / " + currentLevel.NumCirclesToDestroy;
	}

	private void SetupLevels()
	{
		// "tutorial"
		_levels.Add(new Level { NumCirclesToDestroy = 1, CircleSizes = { { 2, 1 } } });
		_levels.Add(new Level { NumCirclesToDestroy = 2, CircleSizes = { { 2, 10 } } });
		_levels.Add(new Level { NumCirclesToDestroy = 3, CircleSizes = { { 2, 6 } } });
		_levels.Add(new Level { NumCirclesToDestroy = 2, CircleSizes = { { 2, 3 } } });




		_levels.Add(new Level { NumCirclesToDestroy = 2, CircleSizes = { { 0, 2 } } }); //5
		_levels.Add(new Level { NumCirclesToDestroy = 4, CircleSizes = { { 2, 6 }, { 1, 3 }, { 0, 4 } } }); //5
		_levels.Add(new Level { NumCirclesToDestroy = 20, CircleSizes = { { 0, 30 }, { 1, 10 } } }); // 6
		_levels.Add(new Level { NumCirclesToDestroy = 20, CircleSizes = { { 0, 60 } } }); // 7
		_levels.Add(new Level { NumCirclesToDestroy = 3, CircleSizes = { { 1, 5 } } }); // 7
		_levels.Add(new Level { NumCirclesToDestroy = 75, CircleSizes = { { 0, 60 }, { 1, 20 } } }); // 8
		_levels.Add(new Level { NumCirclesToDestroy = 3, CircleSizes = { { 2, 3 } } }); // 9
		_levels.Add(new Level { NumCirclesToDestroy = 3, CircleSizes = { { 2, 1 }, { 0, 3 } } }); // 9
		_levels.Add(new Level { NumCirclesToDestroy = 4, CircleSizes = { { 1, 5 } } }); // 10
	}

	public void LoadLevel()
	{
		currentLevelNum++;
		//Debug.Log("curr lev num = " + currentLevelNum);
		//Debug.Log("_levels.Count = " + _levels.Count);

		if (currentLevelNum >= _levels.Count)
		{
			GameManager.instance.AllFinished();
			return;
		}

		currentLevel = _levels[currentLevelNum];

		alreadyClicked = false;
		numCirclesFrozen = 0;
		numCirclesGone = 0;
		RemoveAllBalls();

		levelIntroText.text = "Level " + (currentLevelNum);
		levelTargetText.text = "freeze " + currentLevel.NumCirclesToDestroy + " out of " + currentLevel.NumTotalCircles;

		StartLevelText();

		GameManager.DelayedAction(GameManager.instance.LevelTextFadeInTime + GameManager.instance.LevelTextFadeOutTime + GameManager.instance.LevelTextStayTime,
			() =>
			{
				bool wasGenerationPossible = false;
				for (int i = 0; i < 100; i++)
				{
					bool couldFitAllCircles = SpawnAllCircles(currentLevel.CircleSizes);

					if (couldFitAllCircles)
					{
						Debug.Log("generated level " + currentLevelNum + " with " + i + " retries");
						wasGenerationPossible = true;
						break;
					}
				}

				if (!wasGenerationPossible)
				{
					throw new System.Exception("couldn't generate level");
				}

				foreach (BounceBall ball in balls)
				{
					ball.GetComponent<BallFreeze>().StartFadeInAndAccelerate();
				}

				RefreshScoreText();

				FadeText(scoreText, 0, levelIntroAlpha, GameManager.instance.LevelTextFadeOutTime);
			}
		);
	}

	private void RemoveAllBalls()
	{
		foreach (BounceBall ball in balls)
		{
			if (ball != null)
			{
				Destroy(ball.gameObject);
			}
		}

		balls.Clear();
	}

	private bool SpawnAllCircles(Dictionary<int, int> circleSizes)
	{
		bool couldFitAllCircles = true;
		foreach (var entry in circleSizes)
		{
			int circleSize = entry.Key;
			int numCircles = entry.Value;

			//Debug.Log("entry " + circleSize + ", " + numCircles);

			for (int j = 0; j < numCircles; j++)
			{
				bool couldFitCircle = SpawnCircle(circleSize);

				if (!couldFitCircle)
				{
					couldFitAllCircles = false;
					break;
				}
			}

			if (!couldFitAllCircles)
			{
				RemoveAllBalls();
				break;
			}
		}

		return couldFitAllCircles;
	}

	private bool SpawnCircle(int circleSize)
	{
		float size = circleSizes[circleSize];
		float radius = size * colliderRadius;

		for (int i = 0; i < 100; i++)
		{
			Vector3 position = new Vector3(
				Random.Range(BounceManager.instance.left + radius, BounceManager.instance.right - radius),
				Random.Range(BounceManager.instance.bottom + radius, BounceManager.instance.top - radius),
				0
			);

			bool allClear = true;

			foreach (BounceBall ball2 in balls)
			{
				if (BounceManager.CircleOverlap(position, radius * 1.1f, ball2.transform.position, ball2.radius * 1.1f))
				{
					allClear = false;
					break;
				}
			}

			if (!allClear)
			{
				continue;
			}

			GameObject ballObj = (GameObject) GameObject.Instantiate(ballPrefab, position, Quaternion.identity);

			ballObj.transform.localScale = Vector3.one * size;

			BounceBall ball = ballObj.GetComponent<BounceBall>();

			ball.velocity = Random.insideUnitCircle.normalized * GameManager.instance.VelocityMagnitude;

			ball.sprite.color = GameManager.instance.ColorBall;
			ball.isKinematic = true;

			balls.Add(ball);

			return true;
		}

		//throw new System.Exception("couldn't fit the circle");
		return false;
	}
}
