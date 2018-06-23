using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class SpriteInfo
{
    public UISpriteData Data;
    public UIAtlas Atlas;
    public bool Selected;
    public SpriteInfo(UISpriteData data, UIAtlas atlas)
    {
        Data = data;
        Atlas = atlas;
        Selected = false;
    }
}

public class NGUIUnusedSpriteFound : EditorWindow
{
    #region Param
    private List<SpriteInfo> spriteInfoList;
    private Dictionary<string, SpriteInfo> spriteInfoDic;

    private string atlasPath;
    private string prefabPath;
    #endregion

    #region UI
    private static GUIStyle myStyle;
    [MenuItem("Assets/Noobdawn/检测无效图集")]
    static void ShowWindow()
    {
        NGUIUnusedSpriteFound window = GetWindow<NGUIUnusedSpriteFound>();
        window.Show();
        window.Init();
        window.CollectAllSpriteInfo();
        window.FilterAllSpriteInfo();
        window.name = "检测无效图集";
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.TextArea("比对结果仅供参考！\n代码中赋值的图标和表格里配置的图标不计入比对结果！\n请只删除那些确定不会再使用的图片，宁放过勿错过！\n对图集做出修改后请Save Project后再提交SVN。");
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        if (GUILayout.Button("删除选中Sprite",GUILayout.Height(30)))
        {
            DeleteAllSprite();
        }
        DrawSpriteInfo();
        GUILayout.EndVertical();
    }

    public Vector2 mListScrollPos;
    //public bool mIsListExpand;
    public bool mIsDicExpand;
    private void DrawSpriteInfo()
    {
        if (spriteInfoList == null) return;
        mListScrollPos = EditorGUILayout.BeginScrollView(mListScrollPos);
        //mIsListExpand = EditorGUILayout.Foldout(mIsListExpand, "图片列表");
        //if (mIsListExpand)
        //{
        //    foreach (var info in spriteInfoList)
        //    {
        //        EditorGUILayout.BeginHorizontal(GUILayout.Width(380));
        //        GUILayout.Space(30);
        //        GUILayout.Label(info.Atlas.name + " - " + info.Data.name);
        //        //info.Selected = EditorGUILayout.ToggleLeft(info.SpriteName, info.Selected, GUILayout.Width(240));
        //        EditorGUILayout.EndHorizontal();
        //    }
        //}
        mIsDicExpand = EditorGUILayout.Foldout(mIsDicExpand, "比对结果");
        if (mIsDicExpand)
        {
            int i = 0;
            int size = 80;
            int padded = 90;
            int columns = Mathf.FloorToInt(Screen.width / padded);
            if (columns < 1) columns = 1;
            GUILayout.Space(100);
            foreach (var info in spriteInfoDic)
            {
                #region 图标绘制
                int y = i / columns;
                int x = i - columns * y;
                Texture2D tex = info.Value.Atlas.texture as Texture2D;
                Rect rect = new Rect(10f + x * (padded), 20 + y * (padded + 20), size, size);
                Rect uv  = new Rect(info.Value.Data.x, info.Value.Data.y, info.Value.Data.width, info.Value.Data.height);
                uv = NGUIMath.ConvertToTexCoords(uv, tex.width, tex.height);
                float scaleX = rect.width / uv.width;
                float scaleY = rect.height / uv.height;
                //把图标拉伸到正常大小
                float aspect = (scaleY / scaleX) / ((float)tex.height / tex.width);
                Rect clipRect = rect;
                if (aspect != 1f)
                {
                    if (aspect < 1f)
                    {
                        // The sprite is taller than it is wider
                        float padding = 80 * (1f - aspect) * 0.5f;
                        clipRect.xMin += padding;
                        clipRect.xMax -= padding;
                    }
                    else
                    {
                        // The sprite is wider than it is taller
                        float padding = 80 * (1f - 1f / aspect) * 0.5f;
                        clipRect.yMin += padding;
                        clipRect.yMax -= padding;
                    }
                }
                //画出外框
                if (info.Value.Selected)
                    NGUIEditorTools.DrawOutline(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), Color.green);
                else
                    NGUIEditorTools.DrawOutline(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), Color.grey);
                //画出按钮
                if (GUI.Button(rect, ""))
                {
                    info.Value.Selected = !info.Value.Selected;
                }
                GUI.DrawTextureWithTexCoords(clipRect, tex, uv);
                #endregion
                #region 文字绘制和空白留出
                GUI.Label(new Rect(rect.x, rect.y + size, rect.width, rect.height), info.Value.Data.name);
                GUI.Label(new Rect(rect.x, rect.y + size + 10, rect.width, rect.height), info.Value.Atlas.name);
                if (x == 0)
                {
                    GUILayout.Space(100);
                }
                #endregion
                i++;
            }
        }
        EditorGUILayout.EndScrollView();
    }
    #endregion

    private void Init()
    {
        spriteInfoList = new List<SpriteInfo>();
        spriteInfoDic = new Dictionary<string, SpriteInfo>();
        atlasPath = Application.dataPath + "/Game/Atlas/Static";
        prefabPath = Application.dataPath + "/Game/UIPrefab";
    }

    private void CollectAllSpriteInfo()
    {
        spriteInfoList.Clear();
        var atlasFiles = Directory.GetFiles(atlasPath, "*.prefab", SearchOption.AllDirectories);
        Debug.Log("检测到" + atlasFiles.Length + "个图集");
        foreach (string atlasFile in atlasFiles)
        {
            UIAtlas atlas = AssetDatabase.LoadAssetAtPath<GameObject>(atlasFile.Substring(atlasFile.IndexOf("Assets"))).GetComponent<UIAtlas>();
            if (atlas == null)
            {
                Debug.LogError(atlasFile + "不是个有效的图集");
                continue;
            }
            foreach(var v in atlas.spriteList)
            {
                spriteInfoList.Add(new SpriteInfo(v, atlas));
            }
        }
        Debug.Log("总计：" + spriteInfoList.Count + "个Sprite");
    }

    private void FilterAllSpriteInfo()
    {
        spriteInfoDic.Clear();
        int pro = 0;
        int count = spriteInfoList.Count;
        EditorUtility.DisplayProgressBar("", "", (float)pro / count);
        foreach (var v in spriteInfoList)
        {
            if (spriteInfoDic.ContainsKey(v.Data.name))
            {
                Debug.LogError("SpriteName重复：" + v.Data.name);
                continue;
            }
            spriteInfoDic.Add(v.Data.name, v);
        }
        var prefabFiles = Directory.GetFiles(prefabPath, "*.prefab", SearchOption.AllDirectories);
        Debug.Log("有" + prefabFiles.Length + "个Prefab待比对");
        foreach (string prefabFile in prefabFiles)
        {
            pro++;
            EditorUtility.DisplayProgressBar("检测", prefabFile, (float)pro / count);
            FilterObjectRef(prefabFile.Substring(prefabFile.IndexOf("Assets")));
        }
        EditorUtility.ClearProgressBar();

    }

    void FilterObjectRef(string assetPath)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        if (null == go)
        {
            return;
        }

        // check Sprite
        UISprite[] sprites = go.GetComponentsInChildren<UISprite>(true);
        foreach (UISprite sprite in sprites)
        {
            if (null == sprite.atlas)
            {
                continue;
            }

            if (spriteInfoDic.ContainsKey(sprite.spriteName))
            {
                spriteInfoDic.Remove(sprite.spriteName);
            }
        }
    }

    void DeleteAllSprite()
    {
        if (EditorUtility.DisplayDialog("警告", "这步操作无法撤销，将直接改变图集，是否确认！", "取消", "我确认这些修改是必须的"))
            return;
        foreach(var kv in spriteInfoDic)
        {
            if (kv.Value.Selected)
            {
                Debug.Log("删除" + kv.Value.Atlas.name + " - " + kv.Value.Data.name);
                DeleteSprite(kv.Value);
            }
        }
        CollectAllSpriteInfo();
        FilterAllSpriteInfo();
    }

    void DeleteSprite (SpriteInfo info)
	{
		if (this == null) return;
		List<UIAtlasMaker.SpriteEntry> sprites = new List<UIAtlasMaker.SpriteEntry>();
		UIAtlasMaker.ExtractSprites(info.Atlas, sprites);

		for (int i = sprites.Count; i > 0; )
		{
			UIAtlasMaker.SpriteEntry ent = sprites[--i];
			if (ent.name == info.Data.name)
				sprites.RemoveAt(i);
		}
		UIAtlasMaker.UpdateAtlas(info.Atlas, sprites);
		NGUIEditorTools.RepaintSprites();
	}
}
