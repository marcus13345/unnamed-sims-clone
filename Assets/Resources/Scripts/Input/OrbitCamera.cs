using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{

	private bool moving = false;

	[SerializeField]
	private Transform follow = null;

	[SerializeField]
	private float distance = 20;

	[SerializeField]
	private bool bypass = false;

	[SerializeField]
	private float rotateSpeed = .01f;

	[SerializeField]
	private float scrollSpeed = 5f;

	[SerializeField]
	private float scrollAcceleration = 1.1f;

	private Vector3 mousePosition = Vector3.zero;

	// Update is called once per frame
	void Update()
	{
		if (bypass) return;

		float dy = Input.GetAxis("Mouse ScrollWheel");
		dy *= -scrollSpeed;
		dy = Mathf.Pow(scrollAcceleration, dy);
		distance *= dy;

		if(Input.GetMouseButtonDown(2)) {
			moving = true;
			mousePosition = Input.mousePosition;
		}
		if(Input.GetMouseButtonUp(2)) moving = false;

		if(moving) {
			Vector3 moveBy = (Input.mousePosition - mousePosition) * rotateSpeed;
			Vector3 pitch = new Vector3(-moveBy.y, 0, 0);
			Vector3 yaw = new Vector3(0, moveBy.x, 0);
			transform.Rotate(pitch);
			transform.Rotate(yaw, Space.World);
			mousePosition = Input.mousePosition;
		}

		//transform.position = Vector3.up;
		Vector3 currentPoint = transform.position + (transform.forward * distance);
		Vector3 difference = follow.transform.position - currentPoint;
		transform.position += difference;
	}
}
