// Better Streaming Assets, Piotr Gwiazdowski <gwiazdorrr+github at gmail.com>, 2017

using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using Better.StreamingAssets;
using System.IO;

public class PathUtilTests
{
    [TestCase(".", ExpectedResult = "/")]
    [TestCase("/", ExpectedResult = "/")]
    [TestCase("////////////", ExpectedResult = "/")]
    [TestCase("././././", ExpectedResult = "/")]
    [TestCase("...", ExpectedResult = "/...")]
    [TestCase("....", ExpectedResult = "/....")]
    [TestCase("AAA/..", ExpectedResult = "/")]
    [TestCase("AAA\\..", ExpectedResult = "/")]
    [TestCase("AAA\\..\\", ExpectedResult = "/")]
    [TestCase("AAA/....", ExpectedResult = "/AAA/....")]
    [TestCase("AAA/../", ExpectedResult = "/")]
    [TestCase("AAA/../AAA/..", ExpectedResult = "/")]
    [TestCase("AAA/.", ExpectedResult = "/AAA/")]
    [TestCase("AAA/", ExpectedResult = "/AAA/")]
    [TestCase("AAA//", ExpectedResult = "/AAA/")]
    [TestCase("AAA///", ExpectedResult = "/AAA/")]
    [TestCase("AAA", ExpectedResult = "/AAA")]
    [TestCase("AAA/./BBB", ExpectedResult = "/AAA/BBB")]
    [TestCase("AAA\\./BBB", ExpectedResult = "/AAA/BBB")]
    [TestCase("AAA/./BBB/../CCC/.", ExpectedResult = "/AAA/CCC/")]
    [TestCase("AAA..BBB/", ExpectedResult = "/AAA..BBB/")]
    [TestCase("AAA.BBB/", ExpectedResult = "/AAA.BBB/")]
    [TestCase("AAA.ext", ExpectedResult = "/AAA.ext")]
    [TestCase("AAA/BBB.ext", ExpectedResult = "/AAA/BBB.ext")]
    
    public string TestValidPath(string p)
    {
        return PathUtil.NormalizeRelativePath(p);
    }

    [TestCase("..")]
    [TestCase("AAA\\..\\..")]
    [TestCase("AAA/../..")]
    [TestCase("C:\\AAA")]
    public void TestInvalidPaths(string p)
    {
        Assert.Throws<IOException>(() => PathUtil.NormalizeRelativePath(p));
    }

    [TestCase("a", ExpectedResult = "a")]
    [TestCase("/", ExpectedResult = "/")]
    [TestCase("//", ExpectedResult = "/")]
    [TestCase("\\//", ExpectedResult = "/")]
    [TestCase("///", ExpectedResult = "/")]
    [TestCase("\\",ExpectedResult = "\\")]
    [TestCase("\\\\", ExpectedResult = "\\")]
    [TestCase("\\\\\\", ExpectedResult = "\\")]
    [TestCase("///a", ExpectedResult = "///a")]
    [TestCase("\\\\\\a", ExpectedResult = "\\\\\\a")]
    [TestCase("", ExpectedResult="")]
    [TestCase("a///", ExpectedResult = "a/")]
    [TestCase("a/", ExpectedResult = "a/")]
    public string TestFixTrailingDirectorySeparators(string p)
    {
        return PathUtil.FixTrailingDirectorySeparators(p);
    }

    [TestCase("a", "b", ExpectedResult="a/b")]
    [TestCase("", "b", ExpectedResult = "b")]
    [TestCase("a", "", ExpectedResult = "a")]
    [TestCase("a/", "b", ExpectedResult = "a/b")]
    [TestCase("", "", ExpectedResult = "")]
    [TestCase("a", "/b", ExpectedResult = "/b")]
    [TestCase("", "/b", ExpectedResult = "/b")]
    [TestCase("a/", "/b", ExpectedResult = "/b")]
    public string TestCombineSlash(string a, string b)
    {
        return PathUtil.CombineSlash(a, b);
    }

    [TestCase("")]
    [TestCase(null)]
    public void TestInvalidArguments(string p)
    {
        Assert.Throws<System.ArgumentException>(() => PathUtil.NormalizeRelativePath(p));
    }

    [Test]
    public void TestValidCharacters()
    {
        foreach (var c in Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()) )
        {
            if ( c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar )
                Assert.IsTrue(!PathUtil.IsValidCharacter(c), "For character {0}", (int)c);
        }

        // test some most common characters
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/\\_ ";
        foreach ( char c in alphabet + alphabet.ToLower())
        {
            Assert.IsTrue(PathUtil.IsValidCharacter(c), "For character {0}", (int)c);
        }
    }

    [TestCase("*", "", true)]
    [TestCase("*", "aaaa", true)]
    [TestCase("*", "*", true)]
    [TestCase("", "*", false)]
    [TestCase("a", "*", false)]
    [TestCase("aaaa", "*", false)]
    [TestCase("?", "", false)]
    [TestCase("?", "a", true)]
    [TestCase("?", "aa", false)]
    [TestCase("??", "aa", true)]
    [TestCase("??", "aaa", false)]
    [TestCase("foo", "foo", true)]
    [TestCase("foo", "fooo", false)]
    [TestCase("foo", "fo", false)]
    [TestCase("foo", "afooa", false)]
    [TestCase("foo", "afooa", false)]
    [TestCase("f?o", "f?o", true)]
    [TestCase("f?o", "fao", true)]
    [TestCase("f?o", "afo", false)]
    [TestCase(".*", "aaa", false)]
    [TestCase(".*", ".", true)]
    public void TestWildcardToRegex(string pattern, string match, bool expected)
    {
        Assert.AreEqual(expected, PathUtil.WildcardToRegex(pattern).IsMatch(match));
    }
}
