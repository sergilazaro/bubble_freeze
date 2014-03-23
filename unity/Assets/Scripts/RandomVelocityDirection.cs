using UnityEngine;
using System.Collections;

public class RandomVelocityDirection : MonoBehaviour
{
	public float angle = 30.0f;

	void Awake()
	{
		if (GameManager.instance.VelocityRandomization)
		{
			GetComponent<BounceBall>().velocity = Random.insideUnitCircle.normalized * GameManager.instance.VelocityMagnitude;
		}
		else
		{
			GetComponent<BounceBall>().velocity = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.right;
		}

	}

	void Update()
	{
		//if (Time.frameCount % 30 == 0)
		//{
		//	Debug.Log("Velocity = " + rigidbody2D.velocity.magnitude);
		//}
	}

	void FixedUpdate()
	{
		//Debug.Log("PRE  " + rigidbody2D.velocity.magnitude);

		//rigidbody2D.velocity = rigidbody2D.velocity.normalized * magnitude;
		//rigidbody2D.velocity *= magnitude;

		//rigidbody.velocity = rigidbody.velocity.normalized * magnitude;

		//Debug.Log("POST " + rigidbody2D.velocity.magnitude);
	}

	void OnDrawGizmos()
	{
		Vector3 vel = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.right;

		Gizmos.DrawRay(transform.position, vel);
	}
}
