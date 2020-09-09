using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YxRope;

public class TestLineRenderRope : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start ()
    {
        var mat = new Material(Shader.Find("Particles/Alpha Blended"));
        mat.SetColor("_TintColor", new Color32(0xFF, 0x00, 0x00, 0x22));
        Camera.main.transform.position = new Vector3(0.04f, -0.057f, -0.14f);
        Camera.main.nearClipPlane = 0.0001f;
        var rope = new GameObject("TestRope");
        rope.transform.position = Vector3.zero;
        var ctrl = rope.AddComponent<LineRenderRope>();
        ctrl.IterCount = 255;
        ctrl.Width = 0.005f;
        ctrl.Length = 0.1f;
        ctrl.Segment = 12;
        ctrl.GenAxis = Vector3.up * -1;
        ctrl.Mass = 1;
        ctrl.AngularDrag = 10;
        ctrl.IterCount = 255;
        ctrl.RenderScale = 0.5f;
        ctrl.Mat = mat;
        //
        var headHandler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headHandler.name = "HeadHandler";
        headHandler.transform.position = new Vector3(-0.04f, 0, 0);
        headHandler.transform.localScale = Vector3.one * 0.01f;

        var tailHandler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailHandler.name = "TailHandler";
        tailHandler.transform.position = new Vector3(0.04f, 0, 0);
        tailHandler.transform.localScale = Vector3.one * 0.01f;
        //
        ctrl.SetHeadHandler(headHandler.transform,true);
        ctrl.SetTailHandler(tailHandler.transform,false);

        yield return new WaitForSeconds(3);
        ctrl.SetTailHandler(tailHandler.transform, true);
        tailHandler.transform.position = new Vector3(0.04f, 0, 0);
        yield return new WaitForSeconds(3);
        ctrl.SetHeadHandler(headHandler.transform, false);
    }
}
