using UnityEngine;

namespace BeerMP;

internal class SceneLoader
{
	public static void LoadScene(GameScene scene)
	{
		if (scene != GameScene.Unknown)
		{
			Application.LoadLevel(scene.ToString());
		}
	}
}
