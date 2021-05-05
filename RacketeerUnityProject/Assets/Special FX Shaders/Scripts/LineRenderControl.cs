using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class LineRenderControl : MonoBehaviour {

	public GameObject target;
	public Material mat;

	private LineRenderer lR;
	private Renderer rend;

	void Start() 
	{
		lR = GetComponent<LineRenderer> ();
		rend = GetComponent<Renderer>();
	}
	
	void Update() 
	{
		float dis = Vector3.Distance (transform.position, target.transform.position);
		lR.SetPosition (1, new Vector3 (0, 0, dis));

		mat.SetTextureScale("_DisTex", new Vector2 (dis,1));
		rend.material = mat;
		//rend.material.SetTextureScale ("_DisTex", new Vector2 (dis,1));
	

		transform.LookAt (target.transform.position);
	}
}
