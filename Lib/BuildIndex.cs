using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModPackagerUI
{
    /// <summary>
    /// An entry in a build index.
    /// </summary>
    [JsonObject]
    internal class BuildIndexEntry
    {
        /// <summary>
        /// The full file name of the package to be downloaded.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// The SHA-1 checksum of the package.
        /// </summary>
        [JsonProperty]
        public string Checksum { get; set; }
    }

    /// <summary>
    /// The index information for a package build.
    /// </summary>
    [JsonObject]
    internal class BuildIndex
    {
        [JsonProperty(Required = Required.Always, PropertyName = "built_at")]
        public DateTime BuiltAt { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "entries")]
        public List<BuildIndexEntry> Entries { get; set; }
    }
}
