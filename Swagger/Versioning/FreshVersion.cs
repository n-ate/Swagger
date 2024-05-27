using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;

namespace n_ate.Swagger.Versioning
{
    /// <summary>
    ///
    /// </summary>
    public class FreshVersion : ApiVersion
    {
        /// <summary>
        /// The version route segment used for versioning an API. E.g. {api-version:FreshApiVersion}
        /// </summary>
        public const string ROUTE_KEY = $"{{api-version:{FreshVersionConstraint.TYPE_KEY}}}";

        private static readonly object _locker = new object();
        private static readonly Regex _majorMinorOnlyFormat = new Regex(@"^v?(?<major>[0-9]+)(\.(?<minor>[0-9]+))?$");
        private static readonly Regex _majorMinorOtherFormat = new Regex(@"v?(?<major>[0-9]+)(\.(?<minor>[0-9]+))?$");
        private static readonly Dictionary<string, FreshVersion> _versions = new Dictionary<string, FreshVersion>();
        private int? _hashCode;

        /// <summary>
        /// Classic Versioning
        /// </summary>
        private FreshVersion(int majorVersion, int minorVersion)
            : this(string.Empty, majorVersion, minorVersion)
        {
        }

        /// <summary>
        /// Named-only Versioning
        /// </summary>
        private FreshVersion(string nameOnlyVersion)
            : base(int.MaxValue - _versions.Count, int.MaxValue - _versions.Count) //generates a large version number for each unique version string.
        {
            Raw = nameOnlyVersion;
            Name = nameOnlyVersion;
        }

        /// <summary>
        /// Named and classic Versioning
        /// </summary>
        private FreshVersion(string name, int majorVersion, int minorVersion)
            : base(majorVersion, minorVersion/* + 1000*/)//TODO: replace default IAPIDescriptionGroupCollectionProvider with a custom provider that respects named version, so this "+ 1000" hack is unnecessary.
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name)) sb.Append($"{name}-");
            sb.Append($"v{majorVersion}");
            if (minorVersion != 0) sb.Append($".{minorVersion}");
            Raw = sb.ToString();
            Name = name;
        }

        public string Name { get; private set; } = string.Empty;
        public string Raw { get; private set; }

        /// <summary>
        /// Gets a CustomApiVersion for the version string.
        /// </summary>
        /// <param name="version">A version of any format. Can be a traditional format e.g. v1, v1.6, v22.8, 1.0, 2, 5. Can also be any alpha-numeric string</param>
        public static FreshVersion Get(string version)
        {
            version = version.ToLower();
            if (!_versions.ContainsKey(version))
            {
                lock (_locker)
                {
                    if (!_versions.ContainsKey(version))
                    {
                        var hasNumericVersion = _majorMinorOtherFormat.Match(version);
                        if (hasNumericVersion.Success)
                        {
                            var isOnlyNumericVersion = _majorMinorOnlyFormat.Match(version);
                            var majorText = hasNumericVersion.Groups["major"].Value;
                            var minorText = hasNumericVersion.Groups["minor"].Value;
                            var major = string.IsNullOrEmpty(majorText) ? 0 : Convert.ToInt32(majorText);
                            var minor = string.IsNullOrEmpty(minorText) ? 0 : Convert.ToInt32(minorText);
                            if (isOnlyNumericVersion.Success)
                            { //classic versioning
                                _versions[version] = new FreshVersion(major, minor);
                            }
                            else
                            {
                                var name = version.Substring(0, hasNumericVersion.Index).TrimEnd('-');
                                _versions[version] = new FreshVersion(name, major, minor); //non-standard versioning
                            }
                        }
                        else _versions[version] = new FreshVersion(version); //non-standard versioning
                    }
                }
            }
            return _versions[version]; //returns the same version object for the same version string
        }

        public static bool operator !=(FreshVersion a, FreshVersion b) => !(a == b);

        public static bool operator <(FreshVersion a, FreshVersion b)
        {
            if (a == b) return false;
            if (a.MajorVersion == b.MajorVersion)
            {
                if (a.MinorVersion == b.MinorVersion)
                {
                    var ordered = new[] { a.Name, b.Name };
                    Array.Sort(ordered);
                    return a.Name == ordered[1]; //a is last alphabetically..
                }
                else return a.MinorVersion < b.MinorVersion;
            }
            else return a.MajorVersion < b.MajorVersion;
        }

        public static bool operator ==(FreshVersion a, FreshVersion b) => a.Equals(b);

        public static bool operator >(FreshVersion a, FreshVersion b)
        {
            if (a == b) return false;
            if (a.MajorVersion == b.MajorVersion)
            {
                if (a.MinorVersion == b.MinorVersion)
                {
                    var ordered = new[] { a.Name, b.Name };
                    Array.Sort(ordered);
                    return a.Name == ordered[0]; //a is first alphabetically..
                }
                else return a.MinorVersion > b.MinorVersion;
            }
            else return a.MajorVersion > b.MajorVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is FreshVersion && Equals((FreshVersion)obj);
        }

        public override bool Equals(ApiVersion? other)
        {
            var that = other as FreshVersion;
            if (that is null) return false;
            return this.MajorVersion == that.MajorVersion && this.MinorVersion == that.MinorVersion && this.Name == that.Name;
        }

        public override int GetHashCode()
        {
            if (!_hashCode.HasValue) _hashCode = (Name, MajorVersion, MinorVersion).GetHashCode();
            return _hashCode.Value;
        }

        /// <returns></returns>
        public override string ToString(string? format, IFormatProvider? formatProvider) => Raw;
    }
}