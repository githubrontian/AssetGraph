using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	public class IntegratedGUILoader : INodeOperation {
		public void Setup (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUILoader.Setup");
			ValidateLoadPath(
				node.LoaderLoadPath[target],
				node.GetLoaderFullLoadPath(target),
				() => {
					//can be empty
					//throw new NodeException(node.Name + ": Load Path is empty.", node.Id);
				}, 
				() => {
					throw new NodeException(node.Name + ": Directory not found: " + node.GetLoaderFullLoadPath(target), node.Id);
				}
			);

			Load(target, node, connectionsToOutput, Output);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUILoader.Run");
			Load(target, node, connectionsToOutput, Output);
			Profiler.EndSample();
		}

		void Load (BuildTarget target, 
			NodeData node, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

			if(connectionsToOutput == null) {
				return;
			}

			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;

			var outputSource = new List<AssetReference>();
			var targetFilePaths = FileUtility.GetAllFilePathsInFolder(node.GetLoaderFullLoadPath(target));

			foreach (var targetFilePath in targetFilePaths) {

				if(targetFilePath.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)) {
					continue;
				}

				// already contained into Assets/ folder.
				// imported path is Assets/SOMEWHERE_FILE_EXISTS.
				if (targetFilePath.StartsWith(assetsFolderPath)) {
					var relativePath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);

					var r = AssetReferenceDatabase.GetReference(relativePath);
					if(r != null) {
						outputSource.Add(AssetReferenceDatabase.GetReference(relativePath));
					}
					continue;
				}

				throw new NodeException(node.Name + ": Invalid Load Path. Path must start with Assets/", node.Name);
			}

			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}