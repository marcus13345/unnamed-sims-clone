using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FamilyController : MonoBehaviour {

	[SerializeField]
	private LayerMask simLayer;
	
	[SerializeField]
	private Camera camera;
	
	[SerializeField]
	private Transform selectedSimObject;

	private GameObject selectedSim;

	void Update () {
		if (Input.GetMouseButtonDown(1)) { // right
			if (EventSystem.current.IsPointerOverGameObject()) return;

			RaycastHit hit;
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out hit, Mathf.Infinity, simLayer)) {
				GameObject obj = hit.collider.gameObject;
				Sim sim = obj.GetComponent<Sim>();
				if(sim == null) return;
				selectedSimObject.position = obj.transform.position + Vector3.up * 1.7f;
			}
		}
	}
}
