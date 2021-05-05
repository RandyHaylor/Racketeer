using System;
using UnityEngine;
using UnityEditor;

internal class FxShaderGUI : ShaderGUI
{

	public enum BlendMode
	{
		Additive,
		AdditiveMultiply,
		AdditiveSoft,		
		AlphaBlended,
		Blend,
		Multiply,
		MultiplyDouble,
		AlphaBlendedPremultiply
	}

	public enum NumberOfTexture
	{
        One = 0,
        Two = 1,
        Three = 2
	}

	public enum BlendTexture
	{
		MultiplyMultiply = 0,
		AddAdd = 1,
		MultiplyAdd = 2,
		AddMultiply = 3
	}

	public enum BlendAlpha
	{
		MultiplyMultiply = 0,
		AddAdd = 1,
		MultiplyAdd = 2,
		AddMultiply = 3
	}

	private static class Styles
	{
		public static string sRenderingMode = "Rendering Mode";
		public static readonly string[] sBlendNames = new string[] { "Additive","Additive Multiply","Additive Soft","Alpha Blended","Blend","Multiply","Multiply Double","Alpha Blended Premultiply"};

		public static string sNumbTex = "Number Of Texture";
		public static readonly string[] sNumbTexNames = Enum.GetNames (typeof (NumberOfTexture));

		public static string sBlendTex = "Blending Texture";
        public static readonly string[] sBlendTexNames2 = new string[] { "Multiply","Add"};
		public static readonly string[] sBlendTexNames3 = new string[] { "Multiply Multiply","Add Add","Multiply Add","Add Multiply"};

		public static string sBlendAlpha = "Blending Alpha";
        public static readonly string[] sBlendAlphaNames2 = new string[] { "Multiply","Add"};
		public static readonly string[] sBlendAlphaNames3 = new string[] { "Multiply Multiply","Add Add","Multiply Add","Add Multiply"};

		public static GUIContent sTintColor = new GUIContent ("Tint Color (RGBA) Style");

        public static GUIContent sDslTex = new GUIContent ("Dissolve");
		public static GUIContent sDisTex = new GUIContent ("Distortion");
		public static GUIContent sMainTex = new GUIContent ("Texture(RGBA)");
	}
	//Intersection
	public bool interFold;
    //Dissolve
    public bool dslTexFold;
    public bool dslTexAnim;
    public bool dslTexAnimEnable;
    //Distortion
	public bool disTexFold;
	public bool disTexAnim;
	public bool disTexAnimEnable;
    //Tex 1
	public bool firstTexFold;
	public bool firstTexAnim;
	public bool firstTexAnimEnable;
    //Tex 2
	public bool secondTexFold;
	public bool secondTexAnim;
	public bool secondTexAnimEnable;
    //Tex 3
	public bool thirdTexFold;
	public bool thirdTexAnim;
	public bool thirdTexAnimEnable;

	MaterialEditor m_MaterialEditor;

	//Properties
	MaterialProperty blendMode = null;
	MaterialProperty rQueue = null;
	MaterialProperty numberTexture = null;
	MaterialProperty blendTex = null;
	MaterialProperty blendAlpha = null;
	MaterialProperty tintColor = null;
	MaterialProperty bri = null;
	MaterialProperty con = null;
	MaterialProperty softPart = null;
	//Intersection
	MaterialProperty interColor = null;	// ("Intersection Color(RGBA)", Color) = (0.7,0,1,1)
	MaterialProperty interEdge = null;	// ("Intersection Factor", Range(0.01,20)) = 3
	MaterialProperty interPow = null;	// ("Intersection Power", Range(0.01,20)) = 1
	MaterialProperty interAlpha = null;	//("Intersection Alpha", Float) = 0
	//Hiden Intersection
    MaterialProperty interFoldout = null;
    //Dissolve
    MaterialProperty dslTex = null;     //("Dissolve Texture", 2D) = "gray" {}
	MaterialProperty dslScale = null;
	MaterialProperty dslGl = null;		//("Global Dissolve", Float) = 0.0  
    MaterialProperty dslEC = null;		// ("Dissolve Edge Color", Color) = (1, 1, 1, 1)
    MaterialProperty dslEI = null;		// ("Dissolve Edge Intensity", Range(0,1)) = 1
    MaterialProperty dslER = null;		// ("Dissolve Edge Range", Range(0,0.2)) = 0.08
    MaterialProperty dslEA = null;		// ("Dissolve Edge Alpha", Range(0,1)) = 0
    MaterialProperty dslTAE = null;		//("Animation <1>", Float) = 0.0            //togle dissolveTextureAnimationEnable
    MaterialProperty biasDsl = null;	//("Distortion", Float) = 1
    MaterialProperty tileDsl = null;	//("Tile Dissolve Columns(X), Rows(Y), FPS(Z), Frame(W)", Vector) = (1,1,0,0)
    MaterialProperty panDsl = null;		//("Pan Dissolve (Speed(XY))", Vector) = (0,0,0,0)
    MaterialProperty rotDsl = null;		//("Rot Dissolve (Pivot(XY), Angle Speed(Z), Angle(W))", Vector) = (0.5,0.5,0,0)
	//Hiden Dissolve 
    MaterialProperty dslFoldout = null;
	MaterialProperty dslTA = null;
    //Distortion
	MaterialProperty disTexType = null;
	MaterialProperty disScale = null;
	MaterialProperty disTex = null;
	MaterialProperty biasDs = null;
	MaterialProperty biasMT = null;
	MaterialProperty biasMT2 = null;
	MaterialProperty biasMT3 = null;
	MaterialProperty dTAE = null;
   	MaterialProperty tileDs = null;
	MaterialProperty panDs = null;
	MaterialProperty rotDs = null;
	//Hiden Distortion
    MaterialProperty dFoldout = null;
	MaterialProperty dTA = null;
    //Tex 1
	MaterialProperty mainTex = null;
	MaterialProperty texScale = null;
	MaterialProperty invTex = null;
	MaterialProperty aTex = null;
	MaterialProperty invATex = null;
	MaterialProperty fTAE = null;
	MaterialProperty tileTex = null;
	MaterialProperty panTex = null;
	MaterialProperty rotTex = null;
	//Hiden Tex 1
	MaterialProperty fTexFoldout = null;
	MaterialProperty fTexAnimFoldout = null;
    //Tex 2
	MaterialProperty mainTex2 = null;
	MaterialProperty texScale2 = null;
	MaterialProperty invTex2 = null;
	MaterialProperty aTex2 = null;
	MaterialProperty invATex2 = null;
	MaterialProperty sTAE = null;
	MaterialProperty tileTex2 = null;
	MaterialProperty panTex2 = null;
	MaterialProperty rotTex2 = null;
	//Hiden Tex 2
	MaterialProperty sTexFoldout = null;
	MaterialProperty sTexAnimFoldout = null;
    //Tex 3
	MaterialProperty mainTex3 = null;
	MaterialProperty texScale3 = null;
	MaterialProperty invTex3 = null;
	MaterialProperty aTex3 = null;
	MaterialProperty invATex3 = null;
	MaterialProperty tTAE = null;
	MaterialProperty tileTex3 = null;
	MaterialProperty panTex3 = null;
	MaterialProperty rotTex3 = null;
    //Hiden Tex 3
	MaterialProperty tTexFoldout = null;
	MaterialProperty tTexAnimFoldout = null;

	public void FindProperties (MaterialProperty[] props, Material material)
	{
		blendMode = FindProperty ("_Mode", props); //Debug.Log (blendMode.floatValue);
		rQueue = FindProperty ("_Queue", props);
		numberTexture = FindProperty ("_numberTex", props);
		blendTex = FindProperty ("_rgbc", props);
		blendAlpha = FindProperty ("_ac", props);
		tintColor = FindProperty ("_TintColor", props);
		bri = FindProperty("_Bri", props);
		con = FindProperty("_Con", props);
		softPart = FindProperty ("_InvFade", props);

        if (material.shader.name == "Special FX/FX - Intersection" || material.shader.name == "Special FX/FX Dissolve - Intersection" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection") 
		{
			interColor = FindProperty("_InterColor", props);
			interEdge = FindProperty("_InterEdge", props);
			interPow = FindProperty("_InterPow", props);
			interAlpha = FindProperty("_InterAlpha", props);
			//Hiden Intersection
            interFoldout = FindProperty ("_interT", props);
            if (interFoldout.floatValue > 0.5f)
				interFold = true;
			else
				interFold = false;
		}

        if (material.shader.name == "Special FX/FX Dissolve" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Dissolve - Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
        {
            dslTex = FindProperty("_DslTex", props);
			dslScale = FindProperty("_ScaleDsl", props);
			dslGl = FindProperty("_DslGl", props);
            dslEC = FindProperty("_DslEC", props);
            dslEI = FindProperty("_DslEI", props);
            dslER = FindProperty("_DslER", props);
            dslEA = FindProperty("_DslEA", props);
            dslTAE = FindProperty("_dslTAE", props);
            if(material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
                biasDsl = FindProperty("_BiasDsl", props);
            tileDsl = FindProperty("_TileDsl", props);
            panDsl = FindProperty("_PanDsl", props);
            rotDsl = FindProperty("_RotDsl", props);

			//Hiden
            dslFoldout = FindProperty ("_dslT", props);
            if (dslFoldout.floatValue > 0.5f)
				dslTexFold = true;
			else
				dslTexFold = false;
			
			dslTA = FindProperty ("_dslTA", props);
			if (dslTA.floatValue > 0.5f)
				dslTexAnim = true;
			else
				dslTexAnim = false;
			
			dslTAE = FindProperty ("_dslTAE", props);
			if (dslTAE.floatValue > 0.5f)
				dslTexAnimEnable = true;
			else
				dslTexAnimEnable = false;
        }

        if(material.shader.name == "Special FX/FX Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
		{
			disTexType = FindProperty ("_DisTexType", props);
			disScale = FindProperty("_ScaleDis", props);
			disTex = FindProperty("_DisTex", props);
			biasDs = FindProperty ("_BiasDs", props);
			biasMT = FindProperty ("_BiasMT", props);
			biasMT2 = FindProperty ("_BiasMT2", props);
			biasMT3 = FindProperty ("_BiasMT3", props);
			dTAE = FindProperty ("_dTAE", props);
			tileDs = FindProperty("_TileDs", props);
			panDs = FindProperty("_PanDs", props);
			rotDs = FindProperty ("_RotDs", props);

			//Hiden
            dFoldout = FindProperty ("_dT", props);
            if (dFoldout.floatValue > 0.5f)
				disTexFold = true;
			else
				disTexFold = false;
			
			dTA = FindProperty ("_dTA", props);
			if (dTA.floatValue > 0.5f)
				disTexAnim = true;
			else
				disTexAnim = false;
			
			dTAE = FindProperty ("_dTAE", props);
			if (dTAE.floatValue > 0.5f)
				disTexAnimEnable = true;
			else
				disTexAnimEnable = false;
		}

		//Tex 1
		mainTex = FindProperty ("_MainTex", props);
		texScale = FindProperty("_Scale", props);
		invTex = FindProperty ("_inv1", props);
		aTex = FindProperty ("_Alpha1", props);
		invATex = FindProperty ("_invAlpha1", props);
		tileTex = FindProperty ("_Tile1", props);
		panTex = FindProperty ("_Pan1", props);
		rotTex = FindProperty ("_Rot1", props);
		//Hiden Tex 1
		fTexFoldout = FindProperty ("_fT", props);
		if (fTexFoldout.floatValue > 0.5f)
			firstTexFold = true;
		else
			firstTexFold = false;
		
		fTexAnimFoldout = FindProperty ("_fTA", props);
		if (fTexAnimFoldout.floatValue > 0.5f)
			firstTexAnim = true;
		else
			firstTexAnim = false;
		
		fTAE = FindProperty ("_fTAE", props);
		if (fTAE.floatValue > 0.5f)
			firstTexAnimEnable = true;
		else
			firstTexAnimEnable = false;

		//Tex 2
		mainTex2 = FindProperty ("_MainTex2", props);
		texScale2 = FindProperty("_Scale2", props);
		invTex2 = FindProperty ("_inv2", props);
		aTex2 = FindProperty ("_Alpha2", props);
		invATex2 = FindProperty ("_invAlpha2", props);
		tileTex2 = FindProperty ("_Tile2", props);
		panTex2 = FindProperty ("_Pan2", props);
		rotTex2 = FindProperty ("_Rot2", props);
		//Hiden Tex 2
		sTexFoldout = FindProperty ("_sT", props);
		if (sTexFoldout.floatValue > 0.5f)
			secondTexFold = true;
		else
			secondTexFold = false;
		
		sTexAnimFoldout = FindProperty ("_sTA", props);
		if (sTexAnimFoldout.floatValue > 0.5f)
			secondTexAnim = true;
		else
			secondTexAnim = false;
		
		sTAE = FindProperty ("_sTAE", props);
		if (sTAE.floatValue > 0.5f)
			secondTexAnimEnable = true;
		else
			secondTexAnimEnable = false;

		//Tex 3
		mainTex3 = FindProperty ("_MainTex3", props);
		texScale3 = FindProperty("_Scale3", props);
		invTex3 = FindProperty ("_inv3", props);
		aTex3 = FindProperty ("_Alpha3", props);
		invATex3 = FindProperty ("_invAlpha3", props);
		tileTex3 = FindProperty ("_Tile3", props);
		panTex3 = FindProperty ("_Pan3", props);
		rotTex3 = FindProperty ("_Rot3", props);
		//Hiden Tex 3
		tTexFoldout = FindProperty ("_tT", props);
		if (tTexFoldout.floatValue > 0.5f)
			thirdTexFold = true;
		else
			thirdTexFold = false;
		
		tTexAnimFoldout = FindProperty ("_tTA", props);
		if (tTexAnimFoldout.floatValue > 0.5f)
			thirdTexAnim = true;
		else
			thirdTexAnim = false;
		
		tTAE = FindProperty ("_tTAE", props);
		if (tTAE.floatValue > 0.5f)
			thirdTexAnimEnable = true;
		else
			thirdTexAnimEnable = false;
	
	}//FindProperties

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		Material material = materialEditor.target as Material;
		FindProperties (props, material);
		m_MaterialEditor = materialEditor;

		ShaderPropertiesGUI (material);
	}

	//Draw UI <----
	public void ShaderPropertiesGUI (Material material)
	{
		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
            //Custom Interface
            Head(material);

            if (material.shader.name == "Special FX/FX - Intersection" || material.shader.name == "Special FX/FX Dissolve - Intersection" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
			{
				GUILayout.Space(7);
				Intersection(material);
			}

            if(material.shader.name == "Special FX/FX Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
            {
                GUILayout.Space(7);
                DisTexture(material);
            }

            if(material.shader.name == "Special FX/FX Dissolve" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Dissolve - Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
            {
                GUILayout.Space(7);
                DslTexture(material);
            }

            GUILayout.Space(7);
			FirstTexture(material);

			if(numberTexture.floatValue > 0.5)
			{
				GUILayout.Space(7);
				SecondTexture(material);
			}

			if(numberTexture.floatValue == 2)
			{
				GUILayout.Space(7);
				ThirdTexture(material);
			}
		}
		if (EditorGUI.EndChangeCheck())
		{
			foreach (var obj in blendMode.targets)
				MaterialChanged((Material)obj);
		}
	}//ShaderPropertiesGUI

	void PropertyUI(MaterialProperty property, string label)
	{
		m_MaterialEditor.ShaderProperty(property, label);
	}//PropertyUI

    public void Head(Material material)
    {
        BlendModePopup();
        PropertyUI(rQueue, "Render Queue");
        material.renderQueue = (int)rQueue.floatValue;
        NumberOfTexturePopup();

        if (numberTexture.floatValue > 0)
        {
            BlendTexturePopup();
            BlendAlphaPopup();
        }

        GUILayout.Space(7);

        m_MaterialEditor.ColorProperty(tintColor, "Tint Color (RGBA)");
		PropertyUI (bri, "Brightness");
		PropertyUI (con, "Contrast");
        PropertyUI(softPart, "Soft Particles Factor");
    }//Head

	public void Intersection(Material material)
	{
		// Intersection
		interFold = GUILayout.Toggle(interFold, "Intersection", EditorStyles.toolbarButton);
		if (interFold) 
		{
            interFoldout.floatValue = 1f;
            PropertyUI(interColor, "Color(RGBA)");
            PropertyUI(interEdge, "Factor");
            PropertyUI(interPow, "Power");
            PropertyUI(interAlpha, "Alpha");
		}
        else interFoldout.floatValue = 0f;
	}

    public void DslTexture(Material material)
    {
        // Dsl Texture
        dslTexFold = GUILayout.Toggle(dslTexFold, "Dissolve Texture", EditorStyles.toolbarButton);
        if(dslTexFold)
        {
            dslFoldout.floatValue = 1f;
            m_MaterialEditor.TexturePropertySingleLine(Styles.sDslTex, dslTex);
            m_MaterialEditor.TextureScaleOffsetProperty(dslTex);
            
            PropertyUI(dslGl, "Global Dissolve");
            PropertyUI(dslEC, "Edge Color");
            PropertyUI(dslEI, "Edge Intensity");
            PropertyUI(dslER, "Edge Range");
            PropertyUI(dslEA, "Edge Alpha");
			
			if (material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
                PropertyUI(biasDsl, "Distortion");

			PropertyUI(dslScale, "Scale(XY), PivotXY(ZW)");
			GUILayout.BeginHorizontal();
            dslTexAnimEnable = GUILayout.Toggle(dslTexAnimEnable, "", GUILayout.Width(13));
            dslTexAnim = GUILayout.Toggle(dslTexAnim, "Animation", EditorStyles.foldout);
            GUILayout.EndHorizontal();
            
            if(dslTexAnim)
                dslTA.floatValue = 1f;
            else dslTA.floatValue = 0f;

			if (dslTexAnimEnable)
            {
                dslTAE.floatValue = 1f;
                if(dslTexAnim)
                {
                    m_MaterialEditor.VectorProperty(tileDsl, "Tile: Columns(X), Rows(Y), FPS(Z), Frame(W)");
                    m_MaterialEditor.VectorProperty(panDsl, "Panoraming: Speed(XY)");
                    m_MaterialEditor.VectorProperty(rotDsl, "Rotation: Pivot(XY), Angle Speed(Z), Angle(W)");
                }
            }
            else
            {
                dslTAE.floatValue = 0f;
                if(dslTexAnim)
                    GUILayout.Label ("Animation Disable", EditorStyles.helpBox);
            }
            
        }
        else dslFoldout.floatValue = 0f; //Dissolve Texture
    }//Dissolve

	public void DisTexture(Material material)
	{
		// Dis Texture
		disTexFold = GUILayout.Toggle(disTexFold, "Distortion Texture", EditorStyles.toolbarButton);
		if(disTexFold)
		{
            dFoldout.floatValue = 1f;

			PropertyUI(disTexType, "Texture Type");
			m_MaterialEditor.TexturePropertySingleLine(Styles.sDisTex, disTex);
			m_MaterialEditor.TextureScaleOffsetProperty(disTex);
			PropertyUI(biasDs, "Global Distortion");

			PropertyUI(disScale, "Scale(XY), PivotXY(ZW)");
			GUILayout.BeginHorizontal();
				disTexAnimEnable = GUILayout.Toggle(disTexAnimEnable, "", GUILayout.Width(13));
				disTexAnim = GUILayout.Toggle(disTexAnim, "Animation", EditorStyles.foldout);
			GUILayout.EndHorizontal();
			
			if(disTexAnim)
				dTA.floatValue = 1f;
			else dTA.floatValue = 0f;

			if (disTexAnimEnable)
			{
				dTAE.floatValue = 1f;
				if(disTexAnim)
				{
					m_MaterialEditor.VectorProperty(tileDs, "Tile: Columns(X), Rows(Y), FPS(Z), Frame(W)");
					m_MaterialEditor.VectorProperty(panDs, "Panoraming: Speed(XY)");
					m_MaterialEditor.VectorProperty(rotDs, "Rotation: Pivot(XY), Angle Speed(Z), Angle(W)");
				}
			}
			else
			{
				dTAE.floatValue = 0f;
				if(disTexAnim)
					GUILayout.Label ("Animation Disable", EditorStyles.helpBox);
			}
			
		}
        else dFoldout.floatValue = 0f;//Diss Texture
	}//DisTexture

	public void FirstTexture(Material material)
	{
		// First Texture
		firstTexFold = GUILayout.Toggle(firstTexFold, "First Texture", EditorStyles.toolbarButton);
		if(firstTexFold)
		{
			fTexFoldout.floatValue = 1f;
			m_MaterialEditor.TexturePropertySingleLine(Styles.sMainTex, mainTex);
			m_MaterialEditor.TextureScaleOffsetProperty(mainTex);
			PropertyUI(invTex, "Invert Texture(RGB)");
			PropertyUI(aTex, "Alpha(A) [On - Off]");
			PropertyUI(invATex, "Invert Alpha(A)");
			
            if(material.shader.name == "Special FX/FX Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
				PropertyUI(biasMT, "Distortion");

			PropertyUI(texScale, "Scale(XY), PivotXY(ZW)");
			GUILayout.BeginHorizontal();
				firstTexAnimEnable = GUILayout.Toggle(firstTexAnimEnable, "", GUILayout.Width(13));
				firstTexAnim = GUILayout.Toggle(firstTexAnim, "Animation", EditorStyles.foldout);
			GUILayout.EndHorizontal();
			
			if(firstTexAnim)
				fTexAnimFoldout.floatValue = 1f;
			else fTexAnimFoldout.floatValue = 0f;

			if (firstTexAnimEnable)
			{
				fTAE.floatValue = 1f;
				if(firstTexAnim)
				{
					m_MaterialEditor.VectorProperty(tileTex, "Tile: Columns(X), Rows(Y), FPS(Z), Frame(W)");
					m_MaterialEditor.VectorProperty(panTex, "Panoraming: Speed(XY)");
					m_MaterialEditor.VectorProperty(rotTex, "Rotation: Pivot(XY), Angle Speed(Z), Angle(W)");
				}
			}
			else
			{
				fTAE.floatValue = 0f;
				if(firstTexAnim)
					GUILayout.Label ("Animation Disable", EditorStyles.helpBox);
			}
			
		}
		else fTexFoldout.floatValue = 0f; //First Texture
	}//FirstTexture

	public void SecondTexture(Material material)
	{
		// Second Texture
		secondTexFold = GUILayout.Toggle(secondTexFold, "Second Texture", EditorStyles.toolbarButton);
		if(secondTexFold)
		{
			sTexFoldout.floatValue = 1f;
			m_MaterialEditor.TexturePropertySingleLine(Styles.sMainTex, mainTex2);
			m_MaterialEditor.TextureScaleOffsetProperty(mainTex2);
			PropertyUI(invTex2, "Invert Texture(RGB)");
			PropertyUI(aTex2, "Alpha(A) [On - Off]");
			PropertyUI(invATex2, "Invert Alpha(A)");
			
			if (material.shader.name == "Special FX/FX Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
				PropertyUI(biasMT2, "Distortion");

			PropertyUI(texScale2, "Scale(XY), PivotXY(ZW)");
			GUILayout.BeginHorizontal();
				secondTexAnimEnable = GUILayout.Toggle(secondTexAnimEnable, "", GUILayout.Width(13));
				secondTexAnim = GUILayout.Toggle(secondTexAnim, "Animation", EditorStyles.foldout);
			GUILayout.EndHorizontal(); 
			
			if(secondTexAnim)
				sTexAnimFoldout.floatValue = 1f;
			else sTexAnimFoldout.floatValue = 0f;

			if (secondTexAnimEnable)
			{
				sTAE.floatValue = 1f;
				if(secondTexAnim)
				{
					m_MaterialEditor.VectorProperty(tileTex2, "Tile: Columns(X), Rows(Y), FPS(Z), Frame(W)");
					m_MaterialEditor.VectorProperty(panTex2, "Panoraming: Speed(XY)");
					m_MaterialEditor.VectorProperty(rotTex2, "Rotation: Pivot(XY), Angle Speed(Z), Angle(W)");
				}
			}
			else
			{
				sTAE.floatValue = 0f;
				if(secondTexAnim)
					GUILayout.Label ("Animation Disable", EditorStyles.helpBox);
			}
			
		}
		else sTexFoldout.floatValue = 0f; //Second Texture
	}//SecondTexture

	public void ThirdTexture(Material material)
	{
		// Third Texture
		thirdTexFold = GUILayout.Toggle(thirdTexFold, "Third Texture", EditorStyles.toolbarButton);
		if(thirdTexFold)
		{
			tTexFoldout.floatValue = 1f;
			m_MaterialEditor.TexturePropertySingleLine(Styles.sMainTex, mainTex3);
			m_MaterialEditor.TextureScaleOffsetProperty(mainTex3);
			PropertyUI(invTex3, "Invert Texture(RGB)");
			PropertyUI(aTex3, "Alpha(A) [On - Off]");
			PropertyUI(invATex3, "Invert Alpha(A)");
			
			if (material.shader.name == "Special FX/FX Distortion" || material.shader.name == "Special FX/FX Dissolve Distortion" || material.shader.name == "Special FX/FX Distortion -  Intersection" || material.shader.name == "Special FX/FX Dissolve Distortion - Intersection")
				PropertyUI(biasMT3, "Distortion");

			PropertyUI(texScale3, "Scale(XY), PivotXY(ZW)");
			GUILayout.BeginHorizontal();
				thirdTexAnimEnable = GUILayout.Toggle(thirdTexAnimEnable, "", GUILayout.Width(13));
				thirdTexAnim = GUILayout.Toggle(thirdTexAnim, "Animation", EditorStyles.foldout);
			GUILayout.EndHorizontal();
			
			if(thirdTexAnim)
				tTexAnimFoldout.floatValue = 1f;
			else tTexAnimFoldout.floatValue = 0f;

			if (thirdTexAnimEnable)
			{
				tTAE.floatValue = 1f;
				if(thirdTexAnim)
				{
					m_MaterialEditor.VectorProperty(tileTex3, "Tile: Columns(X), Rows(Y), FPS(Z), Frame(W)");
					m_MaterialEditor.VectorProperty(panTex3, "Panoraming: Speed(XY)");
					m_MaterialEditor.VectorProperty(rotTex3, "Rotation: Pivot(XY), Angle Speed(Z), Angle(W)");
				}
			}
			else
			{
				tTAE.floatValue = 0f;
				if(thirdTexAnim)
					GUILayout.Label ("Animation Disable", EditorStyles.helpBox);
			}
			
		}
		else tTexFoldout.floatValue = 0f; //Third Texture
	}//ThirdTexture


	void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = (BlendMode)blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = (BlendMode)EditorGUILayout.Popup(Styles.sRenderingMode, (int)mode, Styles.sBlendNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
			blendMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}//BlendModePopup

	void NumberOfTexturePopup()
	{
		EditorGUI.showMixedValue = numberTexture.hasMixedValue;
		var nTex = (NumberOfTexture)numberTexture.floatValue;
		
		EditorGUI.BeginChangeCheck();
		nTex = (NumberOfTexture)EditorGUILayout.Popup(Styles.sNumbTex, (int)nTex, Styles.sNumbTexNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Number Of Texture");
			numberTexture.floatValue = (float)nTex;
		}
		
		EditorGUI.showMixedValue = false;
	}//NumberOfTexturePopup

	void BlendTexturePopup()
	{
		EditorGUI.showMixedValue = blendTex.hasMixedValue;
		var blend = (BlendTexture)blendTex.floatValue;
		
		EditorGUI.BeginChangeCheck();

        if(numberTexture.floatValue == 1)
            blend = (BlendTexture)EditorGUILayout.Popup(Styles.sBlendTex, (int)blend, Styles.sBlendTexNames2);
        if(numberTexture.floatValue == 2)
		    blend = (BlendTexture)EditorGUILayout.Popup(Styles.sBlendTex, (int)blend, Styles.sBlendTexNames3);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Blend Texture");
			blendTex.floatValue = (float)blend;
		}
		
		EditorGUI.showMixedValue = false;
	}//BlendTexturePopup

	void BlendAlphaPopup()
	{
		EditorGUI.showMixedValue = blendAlpha.hasMixedValue;
		var mode = (BlendAlpha)blendAlpha.floatValue;
		
		EditorGUI.BeginChangeCheck();
        if(numberTexture.floatValue == 1)
            mode = (BlendAlpha)EditorGUILayout.Popup(Styles.sBlendAlpha, (int)mode, Styles.sBlendAlphaNames2);
        if(numberTexture.floatValue == 2)
		    mode = (BlendAlpha)EditorGUILayout.Popup(Styles.sBlendAlpha, (int)mode, Styles.sBlendAlphaNames3);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Blend Alpha");
			blendAlpha.floatValue = (float)mode;
		}
		
		EditorGUI.showMixedValue = false;
	}//BlendAlphaPopup

	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Additive:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
				break;
			case BlendMode.AdditiveMultiply:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				break;
			case BlendMode.AdditiveSoft:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
				break;
			case BlendMode.AlphaBlended:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				break;
			case BlendMode.Blend:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
				break;
			case BlendMode.Multiply:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
				break;
			case BlendMode.MultiplyDouble:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
				break;
			case BlendMode.AlphaBlendedPremultiply:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				break;
		}
	}//SetupMaterialWithBlendMode

	static void MaterialChanged(Material material)
	{
		// Handle Blending modes
		SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

	}

	static void Dbg(Material mat)
	{
		string matTagQ = "Queue";
		string matTagRT = "RenderType";
		string resultRT = mat.GetTag (matTagRT, true, "Nothing");
		string resultQ = mat.GetTag (matTagQ, true, "Nothing");
		if (resultRT == "Nothing" && resultQ == "Nothing")
			Debug.LogError(matTagQ +"or"+ matTagRT + " not found in " + mat.shader.name);
		else
			Debug.Log("Tag found! >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>   " + "Queue = " +resultQ+ " --/-- RenderType = " + resultRT);
	}


}//Class