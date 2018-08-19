using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

	[SerializeField]
	private float speed = .1f;

	[SerializeField]
	private Transform camera = null;

	private float dx = 0;
	private float dz = 0;
	
	void Update () {
		Vector3 forward = new Vector3(camera.forward.x, 0, camera.forward.z).normalized;
		Vector3 left = -camera.right.normalized;

		if(Input.GetKey(KeyCode.A)) dx = speed;
		if(Input.GetKey(KeyCode.D)) dx = -speed;
		if(Input.GetKey(KeyCode.W)) dz = speed;
		if(Input.GetKey(KeyCode.S)) dz = -speed;

		transform.position += left * dx;
		transform.position += forward * dz;

		dx /= 1.1f;
		dz /= 1.1f;
	}
}
