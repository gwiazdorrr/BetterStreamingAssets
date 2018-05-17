# Better Streaming Assets

Better Streaming Assets is a plugin that lets you access Streaming Assets directly in an uniform and thread-safe way, with tiny overhead. Mostly beneficial for Android projects, where the alternatives are to use archaic and hugely inefficient WWW or embed data in Asset Bundles. API is based on Syste.IO.File and System.IO.Directory classes.

# Usage

Check examples below. Note that all the paths are relative to StreamingAssets directory. That is, if you have files

    <project>/Assets/StreamingAssets/foo.bar
    <project>/Assets/StreamingAssets/dir/foo.bar

You are expected to use following paths:

    foo.bar (or /foo.bar)
    dir/foo.bar (or /dir/foo.bar)

# Examples

Initialization (before first use, needs to be called on main thread):

    BetterStreamingAssets.Initialize();

Typical scenario, deserializing from Xml:

    public static Foo ReadFromXml(string path)
    {
        if ( !BetterStreamingAssets.FileExists(path) )
        {
            Debug.LogErrorFormat("Streaming asset not found: {0}", path);
            return null;
        }

        using ( var stream = BetterStreamingAssets.OpenRead(path) )
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Foo));
            return (Foo)serializer.Deserialize(stream);
        }
    }

Note that ReadFromXml can be called from any thread, as long as Foo's constructor doesn't make any UnityEngine calls.

Listing all Streaming Assets in with .xml extension:

    // all the xmls
    string[] paths = BetterStreamingAssets.GetFiles("\\", "*.xml", SearchOption.AllDirectories); 
    // just xmls in Config directory (and nested)
    string[] paths = BetterStreamingAssets.GetFiles("Config", "*.xml", SearchOption.AllDirectories); 

Checking if a directory exists:

    Debug.Asset( BetterStreamingAssets.DirectoryExists("Config") );

Ways of reading a file:

    // all at once
    byte[] data = BetterStreamingAssets.ReadAllBytes("Foo/bar.data");
    
    // as stream, last 10 bytes
    byte[] footer = new byte[10];
    using (var stream = BetterStreamingAssets.OpenRead("Foo/bar.data"))
    {
        stream.Seek(footer.Length, SeekOrigin.End);
        stream.Read(footer, 0, footer.Length);
    }
    
Asset bundles (again, main thread only):

    // synchronous
    var bundle = BetterStreamingAssets.LoadAssetBundle(path);
    // async
    var bundleOp = BetterStreamingAssets.LoadAssetBundleAsync(path);

 
