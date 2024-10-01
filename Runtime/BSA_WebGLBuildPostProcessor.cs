// Better Streaming Assets, Piotr Gwiazdowski <gwiazdorrr+github at gmail.com>, 2017
#if UNITY_EDITOR && UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Better.StreamingAssets
{
    class WebGLBuildPostProcessor : IPostprocessBuildWithReport
    {
        public const int CallbackOrder = 100000;

        public int callbackOrder => CallbackOrder;

        public void OnPostprocessBuild(BuildReport report)
        {
            // grab all the StreamingAsset files
            var buildRoot                    = PathUtil.ForceSlash(report.summary.outputPath);
            var streamingAssetsRootWithSlash = PathUtil.CombineSlash(buildRoot, "StreamingAssets/");
            
            var path = $"{streamingAssetsRootWithSlash}{BetterStreamingAssets.WebGLImpl.ListFileName}";

            var list = new BetterStreamingAssets.WebGLImpl.FileList();
            foreach (var buildFile in report.files)
            {
                var filePath = PathUtil.ForceSlash(buildFile.path);
                if (!filePath.StartsWith(streamingAssetsRootWithSlash, StringComparison.Ordinal))
                {
                    continue;
                }
                if (filePath.Equals(path, StringComparison.Ordinal))
                {
                    continue;
                }
                // leave the leading slash
                list.Files.Add(filePath.Substring(streamingAssetsRootWithSlash.Length - 1));
            }

            File.WriteAllText(path, JsonUtility.ToJson(list, true));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
#endif