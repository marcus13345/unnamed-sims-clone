using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour {

	[SerializeField]
	private bool noCursor = false;
	[SerializeField]
	private static Texture2D cursorTexture = null;
	private static CursorMode cursorMode = CursorMode.Auto;
	private static Vector2 hotSpot = new Vector2(10, 4);
	// Use this for initialization
	void Start () {
		if(cursorTexture == null) cursorTexture = Resources.Load<Texture2D>("Graphics/cursor/Hand");
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter()
	{
		if(!noCursor) Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
	}

	void OnMouseExit()
	{
		if(!noCursor) Cursor.SetCursor(null, Vector2.zero, cursorMode);
	}
}
