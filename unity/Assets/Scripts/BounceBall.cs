using UnityEngine;
using System.Collections;

public class BounceBall : MonoBehaviour
{
	//public float speed;

	public Vector3 velocity;
	public Vector3 remainingTranslation;

	public bool isKinematic = false;

	public float speed
	{
		get
		{
			return velocity.magnitude;
		}
		set
		{
			velocity = velocity.normalized * value;
		}
	}

	private SpriteRenderer _sprite = null;

	public SpriteRenderer sprite
	{
		get
		{
			if (_sprite == null)
			{
				_sprite = transform.FindChild("sprite").GetComponent<SpriteRenderer>();
			}

			return _sprite;
		}
	}

	public float radius { get { return ((CircleCollider2D)collider2D).radius * transform.lossyScale.x; } }

	public bool translationDone = false;

	public void ComputeTranslation(float delta)
	{
		remainingTranslation = velocity * delta;

		translationDone = false;
	}

	public float GetBroadPhaseRadius(float delta)
	{
		return delta * speed + radius;
	}

	public void HasCollidedWith(BounceBall ball)
	{
		BallFreeze otherBallFreeze = ball.GetComponent<BallFreeze>();
		BallFreeze thisBallFreeze = this.GetComponent<BallFreeze>();

		if (!thisBallFreeze.IsFrozen && otherBallFreeze.IsFrozen)
		{

			thisBallFreeze.StartFreezing();

			GameManager.instance.audio.PlayOneShot(GameManager.instance.soundFreezeSub, 0.4f);
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(transform.position, radius);
	}
}