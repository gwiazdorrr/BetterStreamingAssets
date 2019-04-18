// Better Streaming Assets, Piotr Gwiazdowski <gwiazdorrr+github at gmail.com>, 2017

using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Better.StreamingAssets
{
    [TestFixture("Assets/StreamingAssets", false)]
    [TestFixture("BetterStreamingAssetsTest.apk", true)]
    public class BetterStreamingAssetsTests
    {
        private const int SizesCount = 2;
        private const int TexturesCount = 2;
        private const int BundlesTypesCount = 3;

        private const string TestDirName = "BSATest";
        private const string TestPath = "Assets/StreamingAssets/" + TestDirName;
        private const string TestResourcesPath = "Assets/Resources/" + TestDirName;
        private const int TestFiles = SizesCount * 2 + SizesCount * 2 * BundlesTypesCount + TexturesCount * BundlesTypesCount + TexturesCount;

        private static int[] SizesMB = new int[SizesCount] { 10, 50 };
        private static string[] BundlesLabels = new string[BundlesTypesCount] { "lzma", "lz4", "uncompressed" };
        private static BuildAssetBundleOptions[] BundlesOptions = new BuildAssetBundleOptions[BundlesTypesCount] { BuildAssetBundleOptions.None, BuildAssetBundleOptions.ChunkBasedCompression, BuildAssetBundleOptions.UncompressedAssetBundle };

        [MenuItem("Assets/Better Streaming Assets/Generate Test Data")]
        public static void GenerateTestData()
        {
            if (Directory.Exists(TestPath))
                Directory.Delete(TestPath, true);
            if (Directory.Exists(TestResourcesPath))
                Directory.Delete(TestResourcesPath, true);

            Directory.CreateDirectory(TestPath);
            Directory.CreateDirectory(TestResourcesPath);

            List<string> paths = new List<string>();

            try
            {
                var random = new System.Random(126556343);
                long mb = 1024 * 1024;
                foreach ( var size in SizesMB )
                {
                    var p = "Assets/raw_compressable_" + size.ToString("00") + "MB.bytes";
                    paths.Add(p);
                    CreateZeroFile(p, size * mb);
                    p = "Assets/raw_uncompressable_" + size.ToString("00") + "MB.bytes";
                    paths.Add(p);
                    CreateRandomFile(p, size * mb, random);
                }

                {
                    var tex = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);
                    tex.Apply();
                    var bytes = tex.EncodeToPNG();
                    File.WriteAllBytes("Assets/raw_tex_compressible.png", bytes);
                    paths.Add("Assets/raw_tex_compressible.png");
                }

                {
                    var tex = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);

                    var pixels = tex.GetPixels32();

                    byte[] buffer = new byte[4];

                    for ( int y = 0; y < tex.height; ++y )
                    {
                        for (int x = 0; x < tex.width; ++x)
                        {
                            random.NextBytes(buffer);
                            pixels[y * tex.width + x] = new Color32(buffer[0], buffer[1], buffer[2], buffer[3]);
                        }
                    }

                    tex.SetPixels32(pixels);
                    tex.Apply();

                    var bytes = tex.EncodeToPNG();
                    File.WriteAllBytes("Assets/raw_tex_uncompressible.png", bytes);
                    paths.Add("Assets/raw_tex_uncompressible.png");
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                // single bundles

                var tempDirPath = FileUtil.GetUniqueTempPathInProject();
                Directory.CreateDirectory(tempDirPath);

                try
                {
                    for ( int i = 0; i < BundlesLabels.Length; ++i )
                    {
                        // now create bundles!
                        var builds = paths.Select(x => new AssetBundleBuild()
                        {
                            assetBundleName = Path.GetFileNameWithoutExtension(x.Replace("raw_", "bundle_")),
                            assetBundleVariant = BundlesLabels[i],
                            assetNames = new[] { x }
                        }).ToArray();

                        BuildPipeline.BuildAssetBundles(tempDirPath, builds, BundlesOptions[i], BuildTarget.Android);
                    }

                    foreach ( var file in Directory.GetFiles(tempDirPath).Where(x => Path.GetFileName(x).StartsWith("bundle_") && Path.GetExtension(x) != ".manifest") )
                    {
                        File.Copy(file, Path.Combine(TestResourcesPath, Path.GetFileName(file) + ".bytes"));
                        File.Move(file, Path.Combine(TestPath, Path.GetFileName(file)));
                    }

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
                finally
                {
                    Directory.Delete(tempDirPath, true);
                }

                foreach ( var p in paths )
                {
                    var extension = ".bytes";
                    if ( Path.GetExtension(p) == ".png" )
                        extension = ".png";
                    File.Copy(p, Path.Combine(TestResourcesPath, Path.GetFileName(p) + extension));
                    File.Move(p, Path.Combine(TestPath, Path.GetFileName(p)));
                }
            }
            finally
            {
                foreach (var p in paths)
                {
                    if (File.Exists(p))
                        File.Delete(p);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }


        [MenuItem("Assets/Better Streaming Assets/Delete Test Data")]
        public static void DeleteTestData()
        {
            if (Directory.Exists(TestPath))
                Directory.Delete(TestPath, true);
            if (Directory.Exists(TestResourcesPath))
                Directory.Delete(TestResourcesPath, true);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        public BetterStreamingAssetsTests(string path, bool apkMode)
        {
            if (apkMode && !File.Exists(path))
                Assert.Inconclusive("Build for Android and name output: " + path);

            if ( apkMode )
            {
                BetterStreamingAssets.InitializeWithExternalApk(path);
            }
            else
            {
                BetterStreamingAssets.InitializeWithExternalDirectories(".", path);
            }
        }

        [Test]
        public void TestAssetBundleMatchRawData()
        {
            NeedsTestData();

            foreach (var size in SizesMB)
            {
                foreach (var format in new[] { "raw_uncompressable_{0:00}MB.bytes", "raw_uncompressable_{0:00}MB.bytes" })
                {
                    var name = string.Format(format, size);
                    var referenceBytes = BetterStreamingAssets.ReadAllBytes(TestDirName + "/" + name);
                    Assert.AreEqual(size * 1024 * 1024, referenceBytes.Length);

                    foreach (var suffix in BundlesLabels)
                    {
                        var bundleName = Path.GetFileNameWithoutExtension(name).Replace("raw_", "bundle_") + "." + suffix;

                        var bundle = BetterStreamingAssets.LoadAssetBundle(TestDirName + "/" + bundleName);
                        try
                        {
                            var textAsset = (TextAsset)bundle.LoadAllAssets()[0];
                            Assert.AreEqual(Path.GetFileNameWithoutExtension(name), textAsset.name, bundleName);

                            var bytes = textAsset.bytes;
                            Assert.Zero(memcmp(bytes, referenceBytes, bytes.Length), bundleName);
                        }
                        finally
                        {
                            bundle.Unload(true);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestReadAllBytesCompareWithProjectFiles()
        {
            var files = GetRealFiles("/", null, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var a = File.ReadAllBytes("Assets/StreamingAssets/" + f);
                var b = BetterStreamingAssets.ReadAllBytes(f);
                Assert.AreEqual(a.Length, b.Length);
                Assert.Zero(memcmp(a, b, a.Length));
            }

            Assert.Throws<FileNotFoundException>(() => BetterStreamingAssets.ReadAllBytes("FileThatShouldNotExist"));
        }

        //[Test]
        public void ReadAllBytesZeroFile()
        {
            NeedsTestData();

            foreach (var path in BetterStreamingAssets.GetFiles(TestDirName, "raw_compressable*", SearchOption.TopDirectoryOnly))
            {
                var bytes = BetterStreamingAssets.ReadAllBytes(path);
                for (int i = 0; i < bytes.Length; ++i)
                {
                    if (bytes[i] != 0)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        [Test]
        public void TestOpenReadCompareWithProjectFiles()
        {
            var files = GetRealFiles("/", null, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                using (var a = File.OpenRead("Assets/StreamingAssets/" + f))
                using (var b = BetterStreamingAssets.OpenRead(f))
                {
                    Assert.IsTrue(StreamsEqual(a, b), f);
                }
            }

            Assert.Throws<FileNotFoundException>(() => BetterStreamingAssets.OpenRead("FileThatShouldNotExist"));
        }

        [Test]
        public void TestDirectoriesInProjectExistInStreamingAssets()
        {
            var files = GetRealFiles("/", null, SearchOption.AllDirectories, dirs: true);
            foreach (var f in files)
            {
                Assert.IsTrue(BetterStreamingAssets.DirectoryExists(f), f);
            }

            Assert.IsFalse(BetterStreamingAssets.FileExists("DirectoryThatShouldNotExist"));
        }

        [Test]
        public void TestFilesInProjectExistInStreamingAssets()
        {
            var files = GetRealFiles("/", null, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                Assert.IsTrue(BetterStreamingAssets.FileExists(f));
            }

            Assert.IsFalse(BetterStreamingAssets.FileExists("FileThatShouldNotExist"));
        }

        [TestCase("/", "*.lz4", SearchOption.AllDirectories, SizesCount * 2)]
        [TestCase(".", null, SearchOption.AllDirectories, TestFiles)]
        [TestCase("/", null, SearchOption.AllDirectories, TestFiles)]
        [TestCase("Bundles", null, SearchOption.AllDirectories, 0)]
        [TestCase("///////", null, SearchOption.AllDirectories, TestFiles)]
        [TestCase("/.", null, SearchOption.AllDirectories, TestFiles)]
        [TestCase("/./././", null, SearchOption.AllDirectories, TestFiles)]
        [TestCase("Bundles/../Bundles", null, SearchOption.AllDirectories, 0)]
        public void TestFileListInProjectMatchesStreamingAssets(string dir, string pattern, SearchOption opt, int minCount)
        {
            NeedsTestData();
            TestGetFiles(dir, pattern, opt, minCount, int.MaxValue);
        }


        [TestCase(TestDirName, "*.lz4a", SearchOption.TopDirectoryOnly, 0)]
        [TestCase(TestDirName, "*.lz4", SearchOption.TopDirectoryOnly, SizesCount * 2 + TexturesCount)]
        [TestCase(TestDirName, "*.uncompressed", SearchOption.TopDirectoryOnly, SizesCount * 2 + TexturesCount)]
        [TestCase(TestDirName, "*.lzma", SearchOption.TopDirectoryOnly, SizesCount * 2 + TexturesCount)]
        [TestCase(TestDirName, "raw_compressable*", SearchOption.TopDirectoryOnly, SizesCount)]
        [TestCase(TestDirName, "raw_uncompressable*", SearchOption.TopDirectoryOnly, SizesCount)]
        [TestCase(TestDirName, "raw_*", SearchOption.TopDirectoryOnly, SizesCount * 2 + TexturesCount)]
        [TestCase(TestDirName, "gibberish", SearchOption.TopDirectoryOnly, 0)]
        [TestCase(TestDirName, "*", SearchOption.TopDirectoryOnly, TestFiles)]
        public void TestKnownFileListInProjectMatchesStreamingAssets(string dir, string pattern, SearchOption opt, int exactCount)
        {
            NeedsTestData();
            TestGetFiles(dir, pattern, opt, exactCount, exactCount);
        }

        [TestCase("/..")]
        [TestCase("..")]
        [TestCase("C:\\")]
        [TestCase("AAA/../..")]
        [TestCase("/AAA/../..")]
        [TestCase("*")]
        public void TestGetFilesThrow(string dir)
        {
            Assert.Throws<IOException>(() => BetterStreamingAssets.GetFiles(dir, null, SearchOption.TopDirectoryOnly));
        }

        private void TestGetFiles(string dir, string pattern, SearchOption opt, int minCount, int maxCount)
        {
            var files = GetRealFiles(dir, pattern, opt);
            var otherFiles = BetterStreamingAssets.GetFiles(dir, pattern, opt);

            System.Array.Sort(files);
            System.Array.Sort(otherFiles);

            Assert.AreEqual(files.Length, otherFiles.Length);

            Assert.GreaterOrEqual(files.Length, minCount);
            Assert.LessOrEqual(files.Length, maxCount);

            for (int i = 0; i < files.Length; ++i)
            {
                Assert.AreEqual(files[i], otherFiles[i]);
            }
        }

        private static string[] GetRealFiles(string nested, string pattern, SearchOption so, bool dirs = false)
        {
            var saDir = Path.GetFullPath("Assets/StreamingAssets/");
            var dir = Path.GetFullPath(saDir + nested);

            if (!Directory.Exists(dir))
                Assert.Inconclusive("Directory " + dir + " doesn't exist");

            List<string> files;

            if (dirs)
            {
                files = Directory.GetDirectories(dir, pattern ?? "*", so)
                    .ToList();
            }
            else
            {
                files = Directory.GetFiles(dir, pattern ?? "*", so)
                    .Where(x => Path.GetExtension(x) != ".meta")
                    .ToList();
            }

            var processedFiles = files.Select(x => x.Replace(saDir, string.Empty).Replace("\\", "/"))
                .ToArray();

            return processedFiles;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static bool StreamsEqual(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            byte[] buffer1 = new byte[bufferSize]; //buffer size
            byte[] buffer2 = new byte[bufferSize];
            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // You might replace the following with an efficient "memcmp"
                if (memcmp(buffer1, buffer2, count1) != 0)
                    return false;
            }
        }

        private static void CreateRandomFile(string path, long size, System.Random random)
        {
            var data = new byte[size];
            random.NextBytes(data);
            File.WriteAllBytes(path, data);
        }

        private static void CreateZeroFile(string path, long size)
        {
            File.WriteAllBytes(path, new byte[size]);
        }

        private void NeedsTestData()
        {
            if (!Directory.Exists(TestPath))
                Assert.Inconclusive("Test data not generated. Use \"Assets/Better Streaming Assets/Generate Test Data\" option");
        }
    }
}
