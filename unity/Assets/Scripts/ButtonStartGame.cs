using UnityEngine;
using System.Collections;

public class ButtonStartGame : MonoBehaviour
{
	void OnMouseDown()
	{
		if (GameManager.instance.state == GameManager.State.MainMenu)
		{
			GameManager.instance.StartGame();

			SpriteRenderer sprite = this.GetComponent<SpriteRenderer>();

			Color color = sprite.color;
			color.a *= 0.5f;
			sprite.color = color;

			audio.Play();
		}
	}
}
