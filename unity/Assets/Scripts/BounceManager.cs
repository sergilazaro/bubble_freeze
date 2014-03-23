using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BounceManager : MonoBehaviour
{
	public static BounceManager instance;

	public float left, right, bottom, top;

	public enum Wall
	{
		Left,
		Right,
		Bottom,
		Top,
		None
	}

	void Awake()
	{
		instance = this;

		// resize background
		{
			Vector3 center = new Vector3((left + right) / 2, (bottom + top) / 2, 0);
			Vector3 extents = new Vector3((left - right), (bottom - top), 1);

			GameManager.instance.background.transform.position = center;
			GameManager.instance.background.transform.localScale = extents;
		}

		//ComputeBalls();
	}

	void FixedUpdate()
	{
		//ComputeBalls(); // inefficient but whatever
		ComputeTranslations(Time.fixedDeltaTime);
		ComputeCollisions(Time.fixedDeltaTime);
	}

	//public void ComputeBalls()
	//{
	//	balls = new List<BounceBall>();

	//	BounceBall[] ballObjs = Object.FindObjectsOfType<BounceBall>();

	//	foreach (BounceBall ballObj in ballObjs)
	//	{
	//		balls.Add(ballObj);
	//	}
	//}

	public void ComputeTranslations(float delta)
	{
		foreach (BounceBall ball in LevelManager.instance.balls)
		{
			ball.ComputeTranslation(delta);
		}
	}

	public void ComputeCollisions(float delta)
	{
		//Debug.Log(Time.realtimeSinceStartup + " ----- ");

		int maxNumIters = 100;
		while (true)
		{
			maxNumIters--;
			if (maxNumIters == 0)
			{
				throw new System.Exception("Too many iterations");
			}

			bool didWeFindAnyRemainingBalls = false;

			float closestCollisionTime = float.MaxValue;
			BounceBall closestBall = null;
			BounceBall closestBallOther = null;
			Wall closestWallOther = Wall.None;
			bool foundClosest = false;

			// find closest collision
			foreach (BounceBall ball in LevelManager.instance.balls)
			{
				if (ball.translationDone)
				{
					continue;
				}

				if (ball.isKinematic)
				{
					continue;
				}

				didWeFindAnyRemainingBalls = true;

				float closestCollisionTime2 = float.MaxValue;
				BounceBall closestBall2 = null;
				Wall closestWall2 = Wall.None;
				bool foundClosest2 = false;

				// go through every other ball
				foreach (BounceBall ball2 in LevelManager.instance.balls)
				{
					if (ball2 == ball || ball2.isKinematic)
					{
						continue;
					}

					if (!IsBroadPhaseCollisionPossible(ball, ball2, delta))
					{
						continue;
					}
					else
					{
						//Debug.Log(Time.realtimeSinceStartup + " broad passed");
					}

					float time = FindEarliestCollisionTime(ball, ball2);

					if (float.IsNaN(time))
					{
						//Debug.Log(Time.realtimeSinceStartup + " no collision found");
						// no collision found
						continue;
					}

					if (time < 0)
					{
						//Debug.Log(Time.realtimeSinceStartup + " collided \"back in time\", ignore");
						// collided "back in time", ignore
						continue;
					}

					if (time > delta)
					{
						//Debug.Log(Time.realtimeSinceStartup + " will collide too late (time=" + time + ", delta=" + delta + ")");
						//Debug.Break();
						// will collide too late
						continue;
					}

					if (time < closestCollisionTime2)
					{
						closestCollisionTime2 = time;
						closestBall2 = ball2;
						foundClosest2 = true;
					}
				}

				// also go through all walls
				{
					float leftTime = FindEarliestCollisionTimeLine1D(ball.transform.position.x - ball.radius, ball.velocity.x, left);
					float rightTime = FindEarliestCollisionTimeLine1D(ball.transform.position.x + ball.radius, ball.velocity.x, right);
					float bottomTime = FindEarliestCollisionTimeLine1D(ball.transform.position.y - ball.radius, ball.velocity.y, bottom);
					float topTime = FindEarliestCollisionTimeLine1D(ball.transform.position.y + ball.radius, ball.velocity.y, top);

					//Debug.Log(Time.realtimeSinceStartup + " wall times: " + leftTime + ", " + rightTime + ", " + bottomTime + ", " + topTime);
					//Debug.Log(Time.realtimeSinceStartup + " delta = " + delta);

					if (!float.IsNaN(leftTime) && leftTime > 0 && leftTime <= delta && leftTime < closestCollisionTime2)
					{
						closestCollisionTime2 = leftTime;
						closestBall2 = null;
						closestWall2 = Wall.Left;
						foundClosest2 = true;
					}
					if (!float.IsNaN(rightTime) && rightTime > 0 && rightTime <= delta && rightTime < closestCollisionTime2)
					{
						closestCollisionTime2 = rightTime;
						closestBall2 = null;
						closestWall2 = Wall.Right;
						foundClosest2 = true;
					}
					if (!float.IsNaN(bottomTime) && bottomTime > 0 && bottomTime <= delta && bottomTime < closestCollisionTime2)
					{
						closestCollisionTime2 = bottomTime;
						closestBall2 = null;
						closestWall2 = Wall.Bottom;
						foundClosest2 = true;
					}
					if (!float.IsNaN(topTime) && topTime > 0 && topTime <= delta && topTime < closestCollisionTime2)
					{
						closestCollisionTime2 = topTime;
						closestBall2 = null;
						closestWall2 = Wall.Top;
						foundClosest2 = true;
					}
				}

				if (!foundClosest2)
				{
					// move ball all the way
					ball.transform.position += ball.remainingTranslation;
					ball.translationDone = true;
					continue;
				}
				else
				{
					// it would collide at some point

					if (closestCollisionTime2 < closestCollisionTime)
					{
						closestCollisionTime = closestCollisionTime2;
						closestBall = ball;
						closestBallOther = closestBall2;
						closestWallOther = closestWall2;
						foundClosest = true;
					}
				}

			}

			if (!didWeFindAnyRemainingBalls)
			{
				break;
			}

			// collide and correct
			if (foundClosest)
			{
				if (closestBallOther != null && closestWallOther != Wall.None)
				{
					throw new System.Exception("should never happen");
				}
				if (closestBallOther == null && closestWallOther == Wall.None)
				{
					throw new System.Exception("should never happen");
				}

				if (closestBallOther != null)
				{
					// ball
					CollideAndCorrect(closestBall, closestBallOther, delta, closestCollisionTime);
				}
				else
				{
					// wall
					switch (closestWallOther)
					{
						case Wall.Left:
							CollideAndCorrect(closestBall, Vector3.right, delta, closestCollisionTime);
							break;

						case Wall.Right:
							CollideAndCorrect(closestBall, -Vector3.right, delta, closestCollisionTime);
							break;

						case Wall.Bottom:
							CollideAndCorrect(closestBall, Vector3.up, delta, closestCollisionTime);
							break;

						case Wall.Top:
							CollideAndCorrect(closestBall, -Vector3.up, delta, closestCollisionTime);
							break;

						default:
							throw new System.Exception("should never happen");
					}
				}
			}
		}
	}

	public static void CollideAndCorrect(BounceBall ball, Vector3 wallNormal, float delta, float collisionTime)
	{
		ball.transform.position += ball.velocity * collisionTime;

		Vector3 reflected = Vector3.Reflect(ball.velocity, wallNormal);

		// debug draw
		{
			Vector3 hitPoint = ball.transform.position + (-wallNormal).normalized * ball.radius;

			Debug.DrawRay(ball.transform.position, ball.velocity.normalized, Color.red);
			Debug.DrawRay(hitPoint, wallNormal, Color.white);
			Debug.DrawRay(ball.transform.position, reflected.normalized, Color.green);

			//Debug.Break();
		}

		ball.velocity = reflected.normalized * ball.speed;

		float remainingTime = delta - collisionTime;

		if (remainingTime < float.Epsilon)
		{
			// done
			ball.translationDone = true;
		}
		else
		{
			ball.ComputeTranslation(remainingTime);
		}
	}

	public static void CollideAndCorrect(BounceBall ball1, BounceBall ball2, float delta, float collisionTime)
	{
		ball1.transform.position += ball1.velocity * collisionTime;
		ball2.transform.position += ball2.velocity * collisionTime;

		Vector3 normal = (ball1.transform.position - ball2.transform.position).normalized;

		Vector3 reflected1 = Vector3.Reflect(ball1.velocity, normal);
		Vector3 reflected2 = Vector3.Reflect(ball2.velocity, normal);

		// debug draw
		{
			Vector3 hitPoint = ball2.transform.position + normal * ball2.radius;

			Debug.DrawRay(ball1.transform.position, ball1.velocity.normalized, Color.red);
			Debug.DrawRay(hitPoint, normal, Color.white);
			Debug.DrawRay(ball1.transform.position, reflected1.normalized, Color.green);

			Debug.DrawRay(ball2.transform.position, ball2.velocity.normalized, Color.red);
			Debug.DrawRay(hitPoint, normal, Color.white);
			Debug.DrawRay(ball2.transform.position, reflected2.normalized, Color.green);

			//Debug.Break();
		}

		ball1.velocity = reflected1.normalized * ball1.speed;
		ball2.velocity = reflected2.normalized * ball2.speed;

		float remainingTime = delta - collisionTime;

		if (remainingTime < float.Epsilon)
		{
			// done
			ball1.translationDone = true;
			ball2.translationDone = true;
		}
		else
		{
			ball1.ComputeTranslation(remainingTime);
			ball2.ComputeTranslation(remainingTime);
		}

		ball1.HasCollidedWith(ball2);
		ball2.HasCollidedWith(ball1);
	}

	public static float FindEarliestCollisionTimeLine1D(float pos, float vel, float line)
	{
		// pos + vel * t = line

		if (Mathf.Abs(vel) < float.Epsilon)
		{
			return float.NaN;
		}

		float t = (line - pos) / vel;

		return t;
	}

	// might return NaN if not found
	public static float FindEarliestCollisionTime(BounceBall ball1, BounceBall ball2)
	{
		float a = ball1.transform.position.x - ball2.transform.position.x;
		float b = ball1.velocity.x - ball2.velocity.x;
		float c = ball1.transform.position.y - ball2.transform.position.y;
		float d = ball1.velocity.y - ball2.velocity.y;
		float e = Mathf.Pow(ball1.radius + ball2.radius, 2);

		float denom = b * b + d * d;

		if (Mathf.Abs(denom) < Mathf.Epsilon)
		{
			// no collision
			return float.NaN;
		}

		float sqrt = Mathf.Sqrt((-a * a * d * d) + (2 * a * b * c * d) - (c * c * b * b) + (b * b * e) + (d * d * e));

		float t1 = (-sqrt - (a * b) - (c * d)) / denom;
		float t2 = (sqrt - (a * b) - (c * d)) / denom;

		return Mathf.Min(t1, t2);
	}

	public static bool IsBroadPhaseCollisionPossible(BounceBall ball1, BounceBall ball2, float delta)
	{
		return CircleOverlap(
			ball1.transform.position, ball1.GetBroadPhaseRadius(delta),
			ball2.transform.position, ball2.GetBroadPhaseRadius(delta)
			);
	}

	public static bool CircleOverlap(Vector3 center1, float radius1, Vector3 center2, float radius2)
	{
		return Vector3.Distance(center1, center2) <= (radius1 + radius2);
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;
		
		Vector3 center = new Vector3((left + right) / 2, (bottom + top) / 2, 0);
		Vector3 extents = new Vector3((left - right), (bottom - top), 1);

		Gizmos.DrawWireCube(center, extents);
	}
}